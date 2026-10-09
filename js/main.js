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
import { scheduleAlerts, cancelAlerts } from './host/notify.js';

const OFFLINE_REPORT_SECONDS = 60; // a shorter absence is caught up quietly
const AWAY_FLOOR = 2; // under this, the gap was a slow frame, not an absence
const MAX_LIVE_STEP = 2; // a longer gap with the page on screen was a sleeping device

// dev: the developer panel's hold on the simulation (speed 0 pauses it).
const game = { state: null, flows: null, act: {}, dev: { speed: 1 } };
let ui = null;
let presence = null;
let last = 0;
let sinceSave = 0;
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
};

// Hands a stretch of absence to the engine in one piece and reports on it if it was worth reporting.
function catchUp(seconds) {
  const report = E.catchUp(game.state, seconds);
  game.flows = report.flows;
  if (seconds > OFFLINE_REPORT_SECONDS) {
    E.say(game.state, 'welcomeBack', { time: time(report.seconds) });
    ui.showOffline(report);
  }
  save(true);
}

function tick() {
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
      save(true);
      store.flush();
      scheduleAlerts(game.state);
    },
    onReturn: (awayMs) => {
      cancelAlerts();
      if (awayMs / 1000 > AWAY_FLOOR) {
        catchUp(awayMs / 1000);
      }
      startLoop();
      ui.render();
    },
  });
  ui.queuePendingEvents();
  ui.render();
  if (presence.here()) {
    startLoop();
  }
  // Exposed for debugging from the browser console.
  window.deadswitch = game;
}

if ('serviceWorker' in navigator) {
  addEventListener('load', () => navigator.serviceWorker.register('./sw.js').catch(() => {}));
}
boot();
