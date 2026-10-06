// Engine entry point: state lifecycle and the simulation step. Pure: the host passes elapsed seconds in.
import { BALANCE, MAP, EVENTS_CFG } from './data.js';
import { produce, advanceBuild, unlockedKeys, say, LOG_LIMIT } from './sim/economy.js';
import { advanceOp, advanceRaids } from './sim/war.js';
import { advanceEvents, advanceBuffs, checkDirectives, checkChapters } from './sim/story.js';

export * from './sim/economy.js';
export * from './sim/war.js';
export * from './sim/story.js';
export { odds } from './sim/rng.js';

export const SAVE_VERSION = 2;
const EPS = 1e-9;
const INBOX_LIMIT = 20;

export function newState(seed = 1) {
  const s = {
    v: SAVE_VERSION,
    rng: seed >>> 0,
    res: { ...BALANCE.start },
    levels: { ...BALANCE.startLevels },
    items: {},
    paused: {},
    build: null,
    sectors: [MAP.home],
    op: null,
    raid: null,
    raidTimer: 0,
    raidsStarted: false,
    offlineRaids: 0,
    event: null,
    eventTimer: EVENTS_CFG.firstDelay,
    recentEvents: [],
    buffs: [],
    align: 0,
    directive: 0,
    chapter: 0,
    ending: null,
    stats: { raidsWon: 0, raidsLost: 0, opsWon: 0, opsLost: 0, events: 0 },
    inbox: [{ kind: 'boot' }],
    fx: [],
    log: [],
    seen: [],
    rank: 0,
    lineSeq: 0,
    playTime: 0,
  };
  s.seen = unlockedKeys(s);
  say(s, 'boot');
  checkChapters(s);
  return s;
}

// Fills fields added after a save was written, so old saves keep loading.
export function migrate(raw) {
  const base = newState(raw && raw.rng ? raw.rng : 1);
  if (!raw || typeof raw !== 'object') {
    return base;
  }
  const s = { ...base, ...raw };
  s.res = { ...base.res, ...raw.res };
  s.levels = { ...raw.levels };
  s.items = { ...raw.items };
  s.paused = { ...raw.paused };
  s.stats = { ...base.stats, ...raw.stats };
  s.sectors = Array.isArray(raw.sectors) ? raw.sectors : base.sectors;
  s.buffs = Array.isArray(raw.buffs) ? raw.buffs : [];
  s.recentEvents = Array.isArray(raw.recentEvents) ? raw.recentEvents : [];
  s.log = Array.isArray(raw.log) ? raw.log.slice(-LOG_LIMIT) : base.log;
  s.seen = Array.isArray(raw.seen) ? raw.seen : unlockedKeys(s);
  s.inbox = Array.isArray(raw.inbox) ? raw.inbox : [];
  s.fx = [];
  if (!raw.v || raw.v < 2) {
    // v1 saves predate chapters: announce whatever is already open, skip the boot intro.
    s.chapter = 0;
    s.inbox = [];
    checkChapters(s);
  }
  s.v = SAVE_VERSION;
  return s;
}

// Advances the world by dt seconds. Returns per-second flows for the UI.
export function step(s, dt, offline = false) {
  const flows = produce(s, dt);
  advanceBuild(s, dt);
  advanceOp(s, dt);
  advanceRaids(s, dt, offline);
  advanceEvents(s, dt);
  advanceBuffs(s, dt);
  checkChapters(s);
  checkDirectives(s);
  if (s.inbox.length > INBOX_LIMIT) {
    s.inbox.splice(0, s.inbox.length - INBOX_LIMIT);
  }
  s.playTime += dt;
  return flows;
}

// Runs a long stretch (offline time) in bounded steps so timers land mid-way.
export function catchUp(s, seconds) {
  const total = Math.min(seconds, BALANCE.offlineMaxSeconds);
  const before = { ...s.res };
  const stepSize = Math.max(1, total / 20000);
  let left = total;
  let flows = null;
  s.offlineRaids = 0;
  while (left > EPS) {
    let dt = Math.min(stepSize, left);
    // Land exactly on timer completions so new levels and captures count for the rest of the stretch.
    for (const t of [s.build, s.op]) {
      if (t && t.remaining > EPS && t.remaining < dt) {
        dt = t.remaining;
      }
    }
    flows = step(s, dt, true);
    left -= dt;
  }
  s.offlineRaids = 0;
  const gained = {};
  for (const r of Object.keys(s.res)) {
    gained[r] = s.res[r] - before[r];
  }
  return { seconds: total, gained, flows };
}
