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
import { scheduleAlerts, cancelAlerts, setBadge, clearBadge } from './host/notify.js';
import { startAmbient, stopAmbient, ambientTick } from './ui/ambient.js';

const OFFLINE_REPORT_SECONDS = 60; // a shorter absence is caught up quietly
const AWAY_FLOOR = 2; // under this, the gap was a slow frame, not an absence
const MAX_LIVE_STEP = 2; // a longer gap with the page on screen was a sleeping device

// dev: the developer panel's hold on the simulation (speed 0 pauses it).
const game = { state: null, flows: null, act: {}, dev: { speed: 1 } };
let ui = null;
let presence = null;
let last = 0;
let sinceSave = 0;
let sinceMood = 0;
let looping = false;

const save = (now = false) => store.write(game.state, now);
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
  buy: (id, n) => E.buyItem(game.state, id, n),
  launch: (id) => commit(E.launchOp(game.state, id)),
  inspect: (id) => E.inspectSector(game.state, id),
  choose: (uid, i) => {
    const res = E.resolveEvent(game.state, uid, i);
    save(true);
    return res;
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
      game.state = E.migrate(data.state);
      ui.reset();
      save(true);
      return true;
    } catch {
      return false;
    }
  },
  reset: () => {
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
let crashed = false;
function crash(detail) {
  if (crashed) {
    return;
  }
  crashed = true;
  stopLoop();
  try {
    store.write(game.state, true);
  } catch { /* the save is as safe as it is going to get */ }
  if (ui) {
    ui.crash(detail);
  }
}
addEventListener('error', (e) => crash(e.message));
addEventListener('unhandledrejection', (e) => crash(e.reason && e.reason.message));

// Hands a stretch of absence to the engine in one piece and reports on it if it was worth reporting.
// Nothing ran without a Watch Daemon, so there is nothing to report and nothing to interrupt with.
function catchUp(seconds) {
  const report = E.catchUp(game.state, seconds);
  game.flows = report.flows;
  if (seconds > OFFLINE_REPORT_SECONDS && !report.dark) {
    E.say(game.state, 'welcomeBack', { time: time(report.away) });
    ui.showOffline(report);
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

function advance() {
  const now = performance.now();
  const real = (now - last) / 1000;
  last = now;
  if (real > MAX_LIVE_STEP && game.dev.speed > 0) {
    // On screen, but nothing ran for seconds: the device was asleep. That is time away.
    catchUp(real * game.dev.speed);
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
  }
  // The music reads the state once a second: what is inbound, what is pending, how full we are.
  sinceMood += real;
  if (sinceMood >= 1) {
    sinceMood = 0;
    ambientTick(game.state);
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
  if (looping) {
    return;
  }
  looping = true;
  last = performance.now();
  due = last;
  setTimeout(loop, 0);
}

const stopLoop = () => { looping = false; };

async function boot() {
  const saved = await store.read();
  game.state = saved && saved.state ? E.migrate(saved.state) : E.newState(seed());
  store.keepBackup(saved); // the record that booted is known good; keep it for the rest of the session
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
      stopAmbient();
      save(true);
      store.flush();
      scheduleAlerts(game.state);
      setBadge(game.state);
    },
    onReturn: (awayMs) => {
      cancelAlerts();
      clearBadge();
      if (awayMs / 1000 > AWAY_FLOOR) {
        catchUp(awayMs / 1000);
      }
      startLoop();
      startAmbient(game.state);
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
  // A browser only starts audio from a gesture, so the bed waits for the first tap of the session.
  addEventListener('pointerdown', () => startAmbient(game.state), { once: true });
  // Exposed for debugging from the browser console.
  window.deadswitch = game;
}

if ('serviceWorker' in navigator) {
  addEventListener('load', () => navigator.serviceWorker.register('./sw.js').catch(() => {}));
}
boot();
