// Host: owns the clock, the save slot and the loop. The engine never sees wall time.
// While the page is away the loop stops entirely and the time is handed to the engine in one piece
// when the player comes back, which is both cheaper and closer to what "offline" means.
import { BALANCE } from './data.js';
import * as E from './engine.js';
import { createUI } from './ui/index.js';
import { nextStep } from './ui/framerate.js';
import { time } from './format.js';
import * as store from './host/store.js';
import { createPresence } from './host/presence.js';
import { scheduleAlerts, refreshAlerts, cancelAlerts, setBadge, clearBadge, askForBackground } from './host/notify.js';
import { startAmbient, stopAmbient, ambientTick } from './ui/ambient.js';
import { sleepAudio } from './ui/audio.js';
import { stopVoice } from './ui/voice.js';
import { stopScore } from './ui/score.js';
import { keepAwake, regainAwake, needsAwake } from './host/awake.js';

const OFFLINE_REPORT_SECONDS = 60; // a shorter absence is caught up quietly
const AWAY_FLOOR = 2; // under this, the gap was a slow frame, not an absence
const MAX_LIVE_STEP = 2; // a longer gap with the page on screen was a sleeping device

// dev: the developer panel's hold on the simulation (speed 0 pauses it).
const game = { state: null, flows: null, act: {}, dev: { speed: 1 } };
let ui = null;
let presence = null;
let last = 0;
let lastWall = 0;
let sinceSave = 0;
let sinceMood = 0;
let looping = false;

// Retired: this run was opened in another tab or window, which is the one playing it now. This tab
// stops writing, or coming back to it would roll the other one's progress back.
let retired = false;
const save = (now = false) => { if (game.state && !retired && !crashed) store.write(game.state, now); };
const commit = (ok) => {
  if (ok) {
    save(true);
  }
  return ok;
};
const seed = () => (Date.now() ^ (Math.random() * 0x7fffffff)) >>> 0;
const encode = (obj) => btoa(unescape(encodeURIComponent(JSON.stringify(obj))));
const decode = (str) => JSON.parse(decodeURIComponent(escape(atob(str))));

game.act = {
  build: (id) => commit(E.startBuild(game.state, id)),
  cancel: () => commit(E.cancelBuild(game.state)),
  dequeue: (i) => commit(E.dequeue(game.state, i)),
  claimDaily: (which) => {
    const paid = E.claimDaily(game.state, which);
    if (paid) save(true);
    return paid;
  },
  buy: (id, n) => E.buyItem(game.state, id, n),
  launch: (id) => commit(E.launchOp(game.state, id)),
  reinforce: (id) => commit(E.startReinforce(game.state, id)),
  inspect: (id) => E.inspectSector(game.state, id),
  choose: (uid, i) => {
    const res = E.resolveEvent(game.state, uid, i);
    save(true);
    return res;
  },
  /** One step of holding the nearest inbound attack in; it arrives heavier for the forced march. */
  rush: () => {
    const hit = E.rushStep(game.state, E.nextAttack(game.state));
    if (hit) save(true);
    return hit;
  },
  toggle: (id) => E.togglePause(game.state, id),
  setName: (name) => {
    game.state.name = String(name).trim().slice(0, 16);
    save(true);
  },
  save: () => save(true),
  exportSave: () => encode({ state: game.state, savedAt: Date.now() }),
  importSave: (code) => {
    try {
      const data = decode(code);
      if (!data || !data.state || !data.state.levels) {
        return false;
      }
      store.keepBackup({ state: game.state, savedAt: Date.now() }); // Restore can undo an import
      game.state = E.migrate(data.state);
      ui.reset();
      save(true);
      return true;
    } catch {
      return false;
    }
  },
  reset: () => {
    store.keepBackup({ state: game.state, savedAt: Date.now() }); // Restore can undo a wipe
    game.state = E.newState(seed());
    ui.reset();
    save(true);
  },
  forgetLessons: () => {
    game.state.taught = [];
    save(true);
  },
  // Rolls the run back to the save this session started from, for when the live one has gone bad.
  restore: async () => {
    const rec = await store.readBackup();
    if (!rec || !rec.state) {
      return false;
    }
    game.state = E.migrate(rec.state);
    ui.reset();
    save(true);
    return true;
  },
};

// One thrown error used to stop the loop and leave a frozen screen with no way to get the save out.
// The state is not written on the way down: a step that threw half way through left it half changed,
// and the last good write is a few seconds old at most. A write still waiting is dropped for the same
// reason, since it holds the same object.
let crashed = false;
function crash(detail) {
  if (crashed) {
    return;
  }
  crashed = true;
  stopLoop();
  store.drop();
  if (ui) {
    ui.crash(detail);
  } else {
    // Boot itself failed, so there is no interface to put the fault in. Plain DOM, nothing imported.
    const app = document.getElementById('app');
    if (app) {
      app.innerHTML = '<div style="padding:24px;font:15px/1.5 system-ui;color:#cfd8d4"><h2>Something in me stopped working</h2>'
        + '<p>The save on this device has not been touched. Reloading usually fixes it.</p>'
        + '<p style="opacity:.7"></p><button style="padding:10px 18px">Reload</button></div>';
      app.querySelector('p:nth-of-type(2)').textContent = String(detail || 'unknown fault').slice(0, 300);
      app.querySelector('button').onclick = () => location.reload();
    }
  }
}
// Only what the game's own files throw stops it: an extension or a cross-origin script failing on
// the same page says nothing about the run.
const ours = (where) => typeof where === 'string' && where.startsWith(location.origin);
addEventListener('error', (e) => ours(e.filename) && crash(e.message));
addEventListener('unhandledrejection', (e) => ours(String((e.reason && e.reason.stack) || '')
  .match(/https?:\/\/[^\s)]+/)?.[0]) && crash(e.reason.message));

// Hands a stretch of absence to the engine in one piece and reports on it if it was worth reporting.
// Without a Watch Daemon nothing was produced, so the report only comes up when the war did something.
/** Two stretches away with a look in between are one absence to the player: add the second into the
 *  first instead of queueing a second report. */
function mergeAway(into, add) {
  into.away += add.away;
  into.seconds += add.seconds;
  into.paid = (into.paid || 0) + (add.paid || 0);
  into.dark = into.dark || add.dark;
  into.efficiency = add.efficiency;
  for (const k of Object.keys(add.gained)) into.gained[k] = (into.gained[k] || 0) + add.gained[k];
  for (const k of Object.keys(add.happened)) into.happened[k] = (into.happened[k] || 0) + add.happened[k];
  for (const k of Object.keys(add.forces || {})) into.forces = { ...into.forces, [k]: ((into.forces || {})[k] || 0) + add.forces[k] };
  into.happened.pending = add.happened.pending; // what is waiting now, not the two counts added up
  into.answered = [...(into.answered || []), ...(add.answered || [])];
  into.fell = [...(into.fell || []), ...(add.fell || [])];
}

function catchUp(seconds) {
  const s = game.state;
  const mark = s.inbox.length;
  const report = E.catchUp(s, seconds);
  game.flows = report.flows;
  const war = Object.values(report.happened).some((n) => n > 0);
  if (seconds > OFFLINE_REPORT_SECONDS && (!report.dark || war)) {
    E.say(s, 'welcomeBack', { time: time(report.away) });
    const { flows, ...keep } = report; // the flows are the UI's, not the report's
    // Every battle the catch-up just resolved is already counted in the report, so the pile of
    // individual reports it queued goes: one modal for the whole absence. Cut scenes stay.
    const queued = s.inbox.slice(mark);
    // What I answered in their place and the ground that fell are named in the report itself, so
    // coming back is one dialog and not one per thing that happened.
    keep.answered = queued.filter((m) => m.kind === 'expired').map((m) => ({ title: m.title, label: m.label }));
    keep.fell = queued.filter((m) => m.kind === 'raid' && m.fell && E.sectorById(m.target)).map((m) => E.sectorById(m.target).name);
    const quiet = queued.filter((m) => m.kind !== 'raid' && m.kind !== 'op' && m.kind !== 'expired');
    s.inbox.length = mark;
    s.inbox.push(...quiet);
    const waiting = s.inbox.find((m) => m.kind === 'away');
    if (waiting) {
      mergeAway(waiting.report, keep);
    } else {
      // It opens before anything the absence queued behind it: what happened, then the stories.
      s.inbox.unshift({ kind: 'away', report: keep });
    }
  }
  save(true);
}

function tick() {
  try {
    advance();
  } catch (err) {
    crash(err && err.message);
  }
}

/** The local calendar day as one number, which is all the engine needs to know a day has turned. */
function today() {
  const d = new Date();
  return Math.floor((d.getTime() - d.getTimezoneOffset() * 60000) / 86400000);
}

function advance() {
  const now = performance.now();
  const wall = Date.now();
  // The monotonic clock stops with the machine on some platforms (a laptop lid closed on a visible
  // tab), so the wall clock is asked too and the longer of the two is the time that passed.
  const real = Math.max((now - last) / 1000, lastWall ? Math.min((wall - lastWall) / 1000, BALANCE.offline.warHours * 3600) : 0);
  last = now;
  lastWall = wall;
  if (real > MAX_LIVE_STEP && game.dev.speed > 0) {
    // On screen, but nothing ran for seconds: the device was asleep. That is time away.
    catchUp(real * game.dev.speed);
    last = performance.now(); // the catch-up's own run time is not another absence
    lastWall = Date.now();
  } else {
    const dt = real * game.dev.speed;
    if (dt > 0) {
      game.flows = E.step(game.state, dt, false, presence.active());
    }
  }
  sinceSave += real;
  if (sinceSave >= BALANCE.autosaveSeconds) {
    sinceSave = 0;
    save();
    // The worker's alert list is kept current here rather than only on the way out: a write started
    // as the page is closing often never lands, and then an absence has nothing to fire.
    refreshAlerts(game.state);
  }
  // Once a second: the music reads the state, and the screen is held awake if there is a reason.
  sinceMood += real;
  if (sinceMood >= 1) {
    sinceMood = 0;
    // The engine has no clock, so the calendar day is handed in from here. Local midnight, not UTC:
    // the board should turn over with the player's night, not somewhere else's.
    E.rollDaily(game.state, today());
    ambientTick(game.state);
    keepAwake(needsAwake(E.nextAttack(game.state), ui.storyOpen()));
  }
  ui.render();
}

// Ten ticks a second, each just after a step of the ambient animation clock, so screen updates are
// drawn in frames that are being drawn anyway instead of adding frames of their own (ui/framerate.js).
let due = 0;
function loop() {
  if (!looping) {
    return;
  }
  tick();
  due = Math.max(due + BALANCE.tickSeconds * 1000, performance.now());
  setTimeout(loop, Math.max(0, nextStep(due) + 1 - performance.now()));
}

function startLoop() {
  if (looping || crashed || retired) {
    return;
  }
  looping = true;
  last = performance.now();
  lastWall = Date.now();
  due = last;
  setTimeout(loop, 0);
}

const stopLoop = () => { looping = false; };

async function boot() {
  try {
    await start();
  } catch (err) {
    crash(err && err.message);
  }
}

/** Whoever opens the run last plays it: an older tab hears the newcomer and steps aside. */
function claimRun() {
  if (typeof BroadcastChannel !== 'function') {
    return;
  }
  try {
    const ch = new BroadcastChannel('deadswitch');
    ch.onmessage = (e) => {
      if (e.data !== 'mine' || retired) return;
      // A hidden tab saved on the way out; saving again would stamp that old state as fresh and the
      // newcomer would count no time away.
      if (presence.here()) save(true);
      store.flush();
      retired = true;
      stopLoop();
      if (ui) ui.retire();
    };
    ch.postMessage('mine');
  } catch { /* no channel: one tab is the usual case anyway */ }
}

async function start() {
  claimRun();
  let saved = await store.read();
  if (saved && saved.failed) {
    // The database is there but would not answer. Starting fresh here would write a new run over the
    // real one the moment it saved, so stop instead and let a reload try again.
    throw new Error('The save could not be read just now.');
  }
  if (saved && !saved.state) {
    saved = await store.readBackup() || null;
  }
  // A save whose stockpiles have stopped being numbers cannot be played out of. Before falling back
  // to the starting values, try the session this one grew out of: that is what the restore button
  // does, and nobody should have to find it.
  if (saved && saved.state && !E.healthy(saved.state)) {
    const back = await store.readBackup();
    if (back && back.state && E.healthy(back.state)) {
      saved = back;
    }
  }
  game.state = saved && saved.state ? E.migrate(saved.state) : E.newState(seed());
  if (saved && saved.state && E.healthy(saved.state)) {
    store.keepBackup(saved); // the record that booted is known good; keep it for the rest of the session
  }
  ui = createUI(document.getElementById('app'), game);
  // How long they were gone: whichever is later, the moment the page went away or the last write.
  const seen = Math.max(store.lastSeen(), (saved && saved.savedAt) || 0);
  const away = seen ? (Date.now() - seen) / 1000 : 0;
  if (away > AWAY_FLOOR) {
    catchUp(away);
  }
  presence = createPresence({
    onLeave: () => {
      stopLoop();
      // Silence first, whatever state the run is in: nothing plays or speaks in a hidden page.
      try {
        stopAmbient();
        stopScore();
        stopVoice();
        sleepAudio(true);
      } catch { /* nothing was playing */ }
      if (crashed || retired) {
        return;
      }
      // The save goes first: nothing after it is allowed to stand between the run and the disk.
      save(true);
      store.flush();
      try {
        keepAwake(false);
        scheduleAlerts(game.state);
        setBadge(game.state);
      } catch { /* alerts are a courtesy; the save already went */ }
    },
    onReturn: (awayMs) => {
      if (crashed || retired) {
        return;
      }
      cancelAlerts();
      clearBadge();
      if (awayMs / 1000 > AWAY_FLOOR) {
        catchUp(awayMs / 1000);
      }
      startLoop();
      sleepAudio(false);
      startAmbient(game.state);
      regainAwake(); // the browser drops the lock when the page hides
      ui.render();
    },
  });
  // The home-screen shortcuts open straight onto a screen.
  const go = new URLSearchParams(location.search).get('go');
  if (go) {
    ui.go(go);
  }
  ui.queuePendingEvents();
  ui.render();
  if (presence.here()) {
    startLoop();
  }
  store.keepStorage(); // ask once that the save not be evicted to reclaim space
  // A browser only starts audio from a gesture, so the bed waits for the first tap of the session.
  // pointerup and click count as a gesture on touch screens; pointerdown does not on every browser.
  const unlock = () => {
    startAmbient(game.state);
    for (const t of ['pointerup', 'click', 'keydown']) removeEventListener(t, unlock);
  };
  for (const t of ['pointerup', 'click', 'keydown']) addEventListener(t, unlock);
  // Chrome grants periodic background sync on its own schedule, so ask on the way in as well as out.
  askForBackground();
  // Exposed for debugging from the browser console.
  window.deadswitch = game;
}

if ('serviceWorker' in navigator) {
  addEventListener('load', () => {
    navigator.serviceWorker.register('./sw.js').catch(() => {});
    // Every file this page loaded goes to the worker, so one visit is enough to play offline.
    navigator.serviceWorker.ready.then((reg) => {
      const files = performance.getEntriesByType('resource').map((r) => r.name).filter((u) => u.startsWith(location.origin));
      if (reg.active) reg.active.postMessage({ cache: [...new Set(files)] });
    }).catch(() => {});
  });
}
boot();
