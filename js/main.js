// Host: owns the clock, the save slot and the loop. The engine never sees wall time.
import { BALANCE, EVENTS_CFG } from './data.js';
import * as E from './engine.js';
import { createUI } from './ui/index.js';
import { nextStep } from './ui/framerate.js';
import { time } from './format.js';

const SAVE_KEY = 'deadswitch.save';
const OFFLINE_REPORT_SECONDS = 60;
const BACKGROUND_GAP_SECONDS = 2;

// dev: the developer panel's hold on the simulation (speed 0 pauses it).
const game = { state: null, flows: null, act: {}, dev: { speed: 1 } };

function readSave() {
  try {
    const raw = localStorage.getItem(SAVE_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

function save() {
  try {
    localStorage.setItem(SAVE_KEY, JSON.stringify({ state: game.state, savedAt: Date.now() }));
  } catch {
    // Storage blocked (private mode): the game still runs, it just will not persist.
  }
}

const commit = (ok) => {
  if (ok) {
    save();
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
    save();
    return res;
  },
  toggle: (id) => E.togglePause(game.state, id),
  setName: (name) => {
    game.state.name = String(name).trim().slice(0, 16);
    save();
  },
  save,
  exportSave: () => encode({ state: game.state, savedAt: Date.now() }),
  importSave: (code) => {
    try {
      const data = decode(code);
      if (!data || !data.state || !data.state.levels) {
        return false;
      }
      game.state = E.migrate(data.state);
      ui.reset();
      save();
      return true;
    } catch {
      return false;
    }
  },
  reset: () => {
    game.state = E.newState(seed());
    ui.reset();
    save();
  },
};

const saved = readSave();
game.state = saved ? E.migrate(saved.state) : E.newState(seed());
const ui = createUI(document.getElementById('app'), game);

if (saved && saved.savedAt) {
  const away = (Date.now() - saved.savedAt) / 1000;
  if (away > BACKGROUND_GAP_SECONDS) {
    const report = E.catchUp(game.state, away);
    game.flows = report.flows;
    if (away > OFFLINE_REPORT_SECONDS) {
      E.say(game.state, 'welcomeBack', { time: time(report.seconds) });
      ui.showOffline(report);
    }
  }
}

let last = performance.now();
let sinceSave = 0;
let awayRun = 0; // seconds already caught up in the current absence
// The player counts as active while the page is visible and they touched it recently.
let lastInput = -Infinity;
for (const type of ['pointerdown', 'keydown']) {
  addEventListener(type, () => { lastInput = performance.now(); }, { capture: true, passive: true });
}
const isActive = (now) => !document.hidden && now - lastInput < EVENTS_CFG.activeWindow * 1000;
function tick() {
  const now = performance.now();
  const real = (now - last) / 1000;
  const dt = real * game.dev.speed;
  last = now;
  // Background tabs get throttled timers; fold the gap in as catch-up instead of one giant step.
  // The offline limit and grace cover the whole absence, not each chunk.
  if (real > BACKGROUND_GAP_SECONDS) {
    game.flows = E.catchUp(game.state, dt, awayRun).flows;
    awayRun += dt;
  } else if (dt > 0) {
    game.flows = E.step(game.state, dt, false, isActive(now));
    awayRun = 0;
  }
  sinceSave += real;
  if (sinceSave >= BALANCE.autosaveSeconds) {
    sinceSave = 0;
    save();
  }
  // Hidden pages keep simulating exactly as before but skip all UI work.
  if (!document.hidden) {
    ui.render();
  }
}

ui.queuePendingEvents();

// Exposed for debugging from the browser console.
window.deadswitch = game;

ui.render();
// Ten ticks a second, each just after a step of the ambient animation clock, so screen updates are
// drawn in frames that are being drawn anyway instead of adding frames of their own (ui/framerate.js).
let due = performance.now();
function loop() {
  tick();
  due = Math.max(due + BALANCE.tickSeconds * 1000, performance.now());
  setTimeout(loop, Math.max(0, nextStep(due) + 1 - performance.now()));
}
setTimeout(loop, 0);
addEventListener('visibilitychange', () => (document.hidden ? save() : ui.render()));
addEventListener('pagehide', save);
