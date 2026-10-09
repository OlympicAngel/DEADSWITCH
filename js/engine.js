// Engine entry point: state lifecycle and the simulation step. Pure: the host passes elapsed seconds in.
import { BALANCE, MAP, EVENTS_CFG, BY_ID, FACTIONS } from './data.js';
import { produce, advanceBuild, unlockedKeys, say, offlineLimits, LOG_LIMIT } from './sim/economy.js';
import { advanceOp, advanceRaids, advanceSieges, advanceAssaults, advanceNodes, sectorById } from './sim/war.js';
import { advanceEvents, advanceBuffs, checkDirectives, checkChapters, eventById } from './sim/story.js';
import { advanceClans, clan } from './sim/clans.js';

export * from './sim/economy.js';
export * from './sim/war.js';
export * from './sim/story.js';
export * from './sim/clans.js';
export { odds } from './sim/rng.js';

export const SAVE_VERSION = 3;
const EPS = 1e-9;
const INBOX_LIMIT = 20;

export function newState(seed = 1) {
  const s = {
    v: SAVE_VERSION,
    rng: seed >>> 0,
    name: '',
    res: { ...BALANCE.start },
    levels: { ...BALANCE.startLevels },
    items: {},
    paused: {},
    build: null,
    sectors: [MAP.home],
    taken: [], // sectors ever captured (loot and memories pay once)
    // per sector: { m: strength multiplier (enemy), marks: breached assaults (yours), a: aggression, seen: times inspected }
    nodes: {},
    // per faction: its four traits and the stances they currently add up to (see sim/clans.js)
    clans: {},
    op: null,
    raid: null,
    assault: null,
    assaultTimer: null,
    sieges: [],
    grudges: [],
    raidTimer: 0,
    raidsStarted: false,
    offlineRaids: 0,
    events: [],
    eventSeq: 0,
    eventTimer: EVENTS_CFG.firstDelay,
    recentEvents: [],
    buffs: [],
    align: 0,
    directive: 0,
    chapter: 0,
    ending: null,
    stats: { raidsWon: 0, raidsLost: 0, opsWon: 0, opsLost: 0, events: 0, expired: 0 },
    inbox: [{ kind: 'boot' }],
    fx: [],
    log: [],
    seen: [],
    rank: 0,
    lineSeq: 0,
    playTime: 0,
  };
  s.seen = unlockedKeys(s);
  for (const f of Object.keys(FACTIONS)) {
    clan(s, f);
  }
  say(s, 'boot');
  checkChapters(s);
  // The boot sequence already introduces chapter 1.
  s.inbox = s.inbox.filter((m) => m.kind !== 'chapter');
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
  // Drop references to content that no longer exists, and repair corrupt numbers.
  for (const k of Object.keys(s.res)) {
    s.res[k] = Number.isFinite(Number(s.res[k])) ? Math.max(0, Number(s.res[k])) : base.res[k];
  }
  if (s.build && !BY_ID[s.build.id]) {
    s.build = null;
  }
  if (s.op && !sectorById(s.op.sector)) {
    s.op = null;
  }
  if (s.raid && !FACTIONS[s.raid.faction]) {
    s.raid = null;
  }
  s.grudges = (Array.isArray(raw.grudges) ? raw.grudges : []).filter((x) => x && FACTIONS[x.faction]);
  s.sieges = (Array.isArray(raw.sieges) ? raw.sieges : []).filter((x) => x && FACTIONS[x.faction] && Number.isFinite(x.remaining));
  s.nodes = {};
  for (const [id, n] of Object.entries(raw.nodes && typeof raw.nodes === 'object' ? raw.nodes : {})) {
    if (sectorById(id) && n && Number.isFinite(n.m)) s.nodes[id] = { m: n.m, marks: Number(n.marks) || 0, a: Number(n.a) || 0, seen: Number(n.seen) || 0 };
  }
  s.clans = {};
  for (const f of Object.keys(FACTIONS)) {
    const old = (raw.clans || {})[f] || {};
    const c = clan(s, f);
    for (const k of Object.keys(c)) {
      if (Number.isFinite(old[k])) c[k] = Math.min(1, Math.max(0, old[k]));
    }
    c.stances = Array.isArray(old.stances) ? old.stances : [];
  }
  // Stance effects on my economy are re-added by advanceClans; drop any saved copy so none doubles up.
  s.buffs = s.buffs.filter((b) => !b.clan);
  const a = raw.assault;
  s.assault = a && sectorById(a.from) && sectorById(a.target) && FACTIONS[a.faction] && Number.isFinite(a.remaining) ? a : null;
  s.assaultTimer = Number.isFinite(raw.assaultTimer) ? raw.assaultTimer : null;
  // v2 kept one pending event id; v3 keeps a queue of instances with deadlines.
  if (typeof raw.event === 'string' && eventById(raw.event) && !Array.isArray(raw.events)) {
    const ev = eventById(raw.event);
    s.events = [{ uid: 1, id: ev.id, left: ev.deadline, total: ev.deadline, params: {} }];
    s.eventSeq = 1;
  }
  delete s.event;
  s.events = (Array.isArray(s.events) ? s.events : []).filter((x) => x && eventById(x.id) && Number.isFinite(x.left));
  s.sectors = s.sectors.filter((id) => sectorById(id));
  // Saves from before retaking existed: everything held was captured once already.
  s.taken = Array.isArray(raw.taken) ? raw.taken.filter((id) => sectorById(id)) : s.sectors.filter((id) => id !== MAP.home);
  s.inbox = s.inbox.filter((m) => m && m.kind && (m.kind !== 'op' || sectorById(m.sector)) && (m.kind !== 'raid' || FACTIONS[m.faction]));
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
// active: the player is at the console right now (host-reported); urgent events need it.
export function step(s, dt, offline = false, active = false, speed = 1) {
  const flows = produce(s, dt, speed);
  advanceBuild(s, dt);
  advanceOp(s, dt);
  advanceRaids(s, dt, offline);
  advanceAssaults(s, dt, offline);
  advanceNodes(s, dt);
  advanceClans(s, dt);
  advanceEvents(s, dt, offline, active && !offline);
  advanceSieges(s, dt, offline);
  advanceBuffs(s, dt);
  checkChapters(s);
  checkDirectives(s);
  if (s.inbox.length > INBOX_LIMIT) {
    s.inbox.splice(0, s.inbox.length - INBOX_LIMIT);
  }
  s.playTime += dt;
  return flows;
}

// Runs a stretch of time away in bounded steps so timers land mid-way. Only offlineLimits(s).seconds
// of an absence count, at offlineLimits(s).efficiency of normal output; its first graceSeconds count
// in full. `already` is time already counted in this same absence (a background tab catches up in chunks).
export function catchUp(s, seconds, already = 0) {
  const limits = offlineLimits(s);
  const total = Math.max(0, Math.min(seconds, limits.seconds - already));
  const grace = Math.max(0, Math.min(total, BALANCE.offline.graceSeconds - already));
  const before = { ...s.res };
  const stepSize = Math.max(1, total / 20000);
  let left = total;
  let flows = null;
  s.offlineRaids = 0;
  while (left > EPS) {
    let dt = Math.min(stepSize, left);
    const done = total - left;
    if (done < grace) dt = Math.min(dt, grace - done);
    // Land exactly on timer completions so new levels and captures count for the rest of the stretch.
    for (const t of [s.build, s.op, ...s.sieges]) {
      if (t && t.remaining > EPS && t.remaining < dt) {
        dt = t.remaining;
      }
    }
    flows = step(s, dt, true, false, done < grace ? 1 : limits.efficiency);
    left -= dt;
  }
  s.offlineRaids = 0;
  const gained = {};
  for (const r of Object.keys(s.res)) {
    gained[r] = s.res[r] - before[r];
  }
  return { seconds: total, away: seconds, efficiency: limits.efficiency, gained, flows };
}
