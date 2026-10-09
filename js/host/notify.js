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
import { setDue } from './store.js';

const TAG = 'deadswitch-attack';
const LEAD = 20; // seconds before it lands, so there is time to do something
const KEY = 'deadswitch.notify'; // the player's answer to the permission question

let timers = [];

export const notifyWanted = () => {
  try {
    return localStorage.getItem(KEY) === '1';
  } catch {
    return false;
  }
};

export const notifySupported = () => typeof Notification !== 'undefined' && 'serviceWorker' in navigator;

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
  // Time away stops at the offline limit, so nothing past it ever becomes due. Without a Watch
  // Daemon that limit is the grace window, which leaves nothing to warn about.
  const limit = E.offlineLimits(s).seconds;
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
export function scheduleAlerts(s) {
  cancelAlerts();
  if (!notifyWanted() || Notification.permission !== 'granted') {
    return;
  }
  const next = upcoming(s).slice(0, 2);
  for (const n of next) {
    const ms = (n.at - LEAD) * 1000;
    // A page kept alive in the background can do this properly.
    timers.push(setTimeout(() => show(n.title, n.body), ms));
  }
  // And leave the worker what it needs in case the page is gone by then.
  setDue(next.map((n) => ({ title: n.title, body: n.body, at: Date.now() + (n.at - LEAD) * 1000 })));
  navigator.serviceWorker?.ready
    .then((reg) => reg.periodicSync?.register('deadswitch-check', { minInterval: 30 * 60 * 1000 }))
    .catch(() => {});
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

export function cancelAlerts() {
  timers.forEach(clearTimeout);
  timers = [];
  setDue([]);
  navigator.serviceWorker?.ready
    .then((reg) => reg.getNotifications({ tag: TAG }).then((list) => list.forEach((n) => n.close())))
    .catch(() => {});
}
