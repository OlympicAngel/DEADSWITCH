// Telling the player what is about to happen while they are not looking.
//
// What the platform actually allows, and what this does about it:
//   - While the page is alive but in the background (another tab, another app), a timer in the page
//     still fires. That is the common case and it is handled exactly.
//   - Once the page is closed or discarded, nothing in it runs. A real push would need a server to
//     send it, and this game is a static site with none, so instead the service worker is left the
//     times things are due (`due` in IndexedDB) and fires them if the browser ever wakes it:
//     periodic background sync where it exists, and the next page load otherwise.
//   - `TimestampTrigger` would schedule properly without any of this. It is used when present.
// Nothing is ever shown while the player is watching: alerts are scheduled on the way out and
// cancelled on the way back.
import * as E from '../engine.js';
import { num } from '../format.js';
import { BALANCE, BY_ID, RAIDS, NODES, WARN } from '../data.js';
import { setDue, getDue, readWake } from './store.js';

const TAG = 'deadswitch-attack';
const SYNC_TAG = 'deadswitch-check';
const LEAD = 20; // seconds before it lands, so there is time to do something
const AHEAD = 4; // how many unspawned attacks of each kind to queue from their timers
const QUEUED = 8; // how many alerts to leave the worker for one absence
const MIN_SYNC = 15 * 60 * 1000; // the shortest wake we ask for; the browser gives what it wants
const KEY = 'deadswitch.notify'; // the player's answer to the permission question

let timers = [];
let lastDue = ''; // the due list as last written, so an unchanged one is not written again

export const notifyWanted = () => {
  try {
    return localStorage.getItem(KEY) === '1';
  } catch {
    return false;
  }
};

export const notifySupported = () => typeof Notification !== 'undefined' && 'serviceWorker' in navigator;

/** Why an alert may or may not arrive. Nothing here is a setting; it is what the platform allows.
 *  A page that has been closed can only be woken by periodic background sync, which browsers grant
 *  to installed apps, so a plain tab gets alerts only while it is still in the background. */
export function notifyState() {
  const installed = (matchMedia && matchMedia('(display-mode: standalone)').matches) || navigator.standalone === true;
  return {
    supported: notifySupported(),
    wanted: notifyWanted(),
    permission: typeof Notification === 'undefined' ? 'unsupported' : Notification.permission,
    installed,
    background: installed && 'serviceWorker' in navigator && 'PeriodicSyncManager' in window,
  };
}

// Whether the browser has actually granted background sync, which is the only thing that can wake a
// closed page. It is not a prompt: Chrome decides from site engagement, and it can say no silently.
// Until this has been read once, nothing here may claim an alert will arrive after the app is closed.
let background = 'unknown';

/** Reads the real state of every link in the chain. Async, because most of it has to be asked for. */
export async function notifyDiagnosis() {
  const n = notifyState();
  const out = { ...n, worker: false, periodicSync: false, registered: false, permission2: 'unknown', due: 0 };
  try {
    const reg = await navigator.serviceWorker?.getRegistration();
    out.worker = !!reg;
    out.periodicSync = !!(reg && reg.periodicSync);
    if (reg && reg.periodicSync) {
      out.registered = (await reg.periodicSync.getTags()).includes(SYNC_TAG);
    }
  } catch { /* nothing registered, or the browser refused to say */ }
  try {
    out.permission2 = (await navigator.permissions.query({ name: 'periodic-background-sync' })).state;
  } catch {
    out.permission2 = 'unsupported';
  }
  out.due = (await getDue()).length;
  out.wake = await readWake();
  background = out.registered && out.permission2 === 'granted' ? 'yes' : 'no';
  return out;
}

/** One line of fact about where alerts stand, for Settings and the developer panel. */
export function notifyWhy() {
  const n = notifyState();
  if (!n.supported) return 'This browser has no notifications.';
  if (n.permission === 'denied') return 'Blocked for this site in browser settings.';
  if (!n.wanted) return 'Off.';
  if (n.permission !== 'granted') return 'Not granted yet.';
  if (!n.installed) return 'In a browser tab, alerts only arrive while the page is still open in the background.';
  if (background === 'yes') return 'Installed, background sync granted: the browser decides how often it wakes us.';
  if (background === 'no') return 'Installed, but the browser has not granted background sync, so alerts only arrive while the app is still open in the background.';
  return 'Installed. Checking whether the browser will wake us.';
}

/** Shows one now, to prove the whole path works end to end. */
export const testAlert = () => show('Test alert', 'If you can see this, alerts reach you.');

/** Asks once, on a tap. Returns whether alerts are on afterwards. */
export async function setNotify(on) {
  if (!on || !notifySupported()) {
    try {
      localStorage.setItem(KEY, '0');
    } catch { /* nothing to do */ }
    cancelAlerts();
    return false;
  }
  const granted = Notification.permission === 'granted' || (await Notification.requestPermission()) === 'granted';
  try {
    localStorage.setItem(KEY, granted ? '1' : '0');
  } catch { /* nothing to do */ }
  return granted;
}

// The next few attacks the engine's own timers already promise, in seconds from now. Only what is
// already on the board was ever scheduled before, so leaving in a quiet minute queued nothing at all
// and a whole absence passed in silence. A spawn timer plus the shortest warning that spawn can carry
// is an early estimate on purpose: an alert that comes a minute early is useful, one that comes late
// is not.
function promised(s, limit) {
  const out = [];
  if (E.level(s, 'core') < RAIDS.startAtCore) {
    return out;
  }
  const soonest = (base) => base * WARN.share + WARN.floor;
  // One already on the board does not stop the clock: the next one is timed from where that one
  // lands. Bailing here was why a nine-hour absence queued a single alert and then went quiet.
  const after = (live, timer, gap) => (live ? live.remaining + gap : Math.max(0, timer || 0)) + soonest(gap);
  for (let at = after(s.raid, s.raidTimer, RAIDS.intervalMin), n = 0; at <= limit && n < AHEAD; n++) {
    out.push({ at, title: 'A raid is due', body: 'Something is walking in. I will hold what I can.' });
    at += RAIDS.intervalMin;
  }
  // Assaults run off their own clock, which is null until the first one is armed.
  if (s.assault || s.assaultTimer !== null) {
    for (let at = after(s.assault, s.assaultTimer, NODES.intervalMin), n = 0; at <= limit && n < AHEAD; n++) {
      out.push({ at, title: 'A clan is moving on us', body: 'One of our sectors is about to be hit.' });
      at += NODES.intervalMin;
    }
  }
  return out;
}

/** What is coming and when, in seconds from now. The engine knows; nothing here guesses. */
export function upcoming(s) {
  const out = [];
  const atk = E.attacks(s)[0];
  if (atk) {
    out.push({ at: atk.remaining, title: `${E.attackName(atk)} inbound`, body: `Strength ${num(atk.strength)}. Hold chance ${Math.round(E.raidChance(s, atk) * 100)}%.` });
  }
  const order = s.events.length ? s.events.reduce((a, x) => (x.left < a.left ? x : a)) : null;
  if (order) {
    out.push({ at: order.left, title: 'An order is about to expire', body: 'No answer and I decide for you.' });
  }
  // A build long enough to walk away from is worth a word when it finishes; a short one is not.
  if (s.build && s.build.total >= BALANCE.longBuild) {
    const b = BY_ID[s.build.id];
    out.push({ at: s.build.remaining + LEAD, title: `${b.name} is finished`, body: `Level ${E.level(s, b.id) + 1}. The builder is free.` });
  }
  // Time away stops at the offline limit, so nothing past it ever becomes due. The war runs in the
  // dark too, so this is worth doing before the Watch Daemon is up.
  const limit = E.awaySeconds(s, Infinity);
  out.push(...promised(s, limit));
  return out.filter((x) => x.at > LEAD && x.at <= limit).sort((a, b) => a.at - b.at);
}

async function show(title, body) {
  try {
    const reg = await navigator.serviceWorker.getRegistration();
    if (reg) {
      await reg.showNotification(title, { body, tag: TAG, renotify: true, icon: './icon.svg', badge: './icon.svg' });
    }
  } catch { /* the browser said no; nothing to do about it */ }
}

/** Called when the player leaves. Schedules the next alert and leaves the due times for the worker. */
/** Keeps the worker's due list current while the game is being played, so it is already in the
 *  database whatever happens to the page. An IndexedDB write started as a page is closing often does
 *  not land, which is how a whole absence ended up with nothing stored to fire. Cheap: it only
 *  writes when the list has actually moved on. */
export function refreshAlerts(s, timers_ = false) {
  if (!notifyWanted() || typeof Notification === 'undefined' || Notification.permission !== 'granted') {
    if (lastDue) {
      lastDue = '';
      setDue([]);
    }
    return;
  }
  const next = upcoming(s).slice(0, QUEUED);
  const due = next.map((n) => ({ title: n.title, body: n.body, at: Date.now() + (n.at - LEAD) * 1000 }));
  if (timers_) {
    for (const n of next) {
      // A page kept alive in the background can do this properly.
      timers.push(setTimeout(() => show(n.title, n.body), (n.at - LEAD) * 1000));
    }
  }
  // Round the times before comparing, or every tick looks like a change and writes again.
  const key = due.map((d) => `${d.title}@${Math.round(d.at / 30000)}`).join('|');
  if (key === lastDue) {
    return;
  }
  lastDue = key;
  setDue(due);
}

export function scheduleAlerts(s) {
  // Only the page's own timers are dropped here: the due list is replaced in one write below, because
  // clearing it first and rewriting it second is two transactions racing a page that is closing.
  timers.forEach(clearTimeout);
  timers = [];
  lastDue = ''; // the way out always writes, however little the list moved
  refreshAlerts(s, true);
  askForBackground();
}

/** Asks for periodic background sync, which is the only thing that can wake a closed page. The
 *  browser decides whether to grant it and how often to fire, so this is asked for early and often
 *  rather than once on the way out. */
export function askForBackground() {
  navigator.serviceWorker?.ready.then(async (reg) => {
    try {
      await reg.periodicSync?.register(SYNC_TAG, { minInterval: MIN_SYNC });
    } catch {
      // Chrome refuses when it has not granted the permission; notifyWhy says so rather than lying.
    }
    try {
      // A one-shot sync fires the moment the device has a network again, which is a second chance at
      // a late alert on a phone that was out of signal.
      await reg.sync?.register(SYNC_TAG);
    } catch { /* unsupported, or already registered */ }
    notifyDiagnosis();
  }).catch(() => {});
}

// Android has no home-screen widget for a web app, but it does have the icon badge: how many things
// are waiting on the commander, visible without opening anything.
export function setBadge(s) {
  const n = s.events.length + E.attacks(s).length;
  try {
    if (n) navigator.setAppBadge?.(n);
    else navigator.clearAppBadge?.();
  } catch { /* unsupported, or not installed */ }
}

export function clearBadge() {
  try {
    navigator.clearAppBadge?.();
  } catch { /* nothing to clear */ }
}

/** Called on the way back in. The page's own timers go, but the worker's list is left alone: it
 *  fires nothing while anyone is watching, and the next autosave brings it up to date anyway. */
export function cancelAlerts() {
  timers.forEach(clearTimeout);
  timers = [];
  navigator.serviceWorker?.ready
    .then((reg) => reg.getNotifications({ tag: TAG }).then((list) => list.forEach((n) => n.close())))
    .catch(() => {});
}
