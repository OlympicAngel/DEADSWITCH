// Story systems: choice events, directives, chapters, alignment and timed effects.
import {
  EVENTS, EVENTS_CFG, DIRECTIVES, CHAPTERS, CHAPTER_TEXT, ALIGNMENT, BUILDINGS, BY_ID, ITEMS, FACTIONS,
} from '../data.js';
import {
  level, factors, threat, grossRate, canAfford, grant, giveItems, owned, say, caps, loseLevel, loseUnits, projectDefense,
} from './economy.js';
import { delayRaid, activeRaiders, startSiege, addGrudge } from './war.js';
import { range, pick, rand } from './rng.js';

const EVENT_BY_ID = Object.fromEntries(EVENTS.map((e) => [e.id, e]));
export const eventById = (id) => EVENT_BY_ID[id];
const RECENT_EVENTS = 8;
const PICKABLE = ['producer', 'converter', 'storage', 'unlocker'];

// ---------- events ----------

// Converts "seconds of production" into units, with a floor so early events still matter.
export function eventValue(s, r, seconds) {
  const floor = EVENTS_CFG.minRatePerCore[r] * Math.max(1, level(s, 'core'));
  return Math.ceil(seconds * Math.max(grossRate(s, r), floor));
}

function scaled(s, obj, mult = 1) {
  const out = {};
  for (const [r, sec] of Object.entries(obj || {})) {
    out[r] = eventValue(s, r, sec * mult);
  }
  return out;
}

// Fills {a}/{b} with the building names picked for this event instance.
export function fillText(inst, str) {
  const p = inst.params || {};
  return String(str)
    .replace(/\{(a|b)\}/g, (_, k) => (BY_ID[p[k]] ? BY_ID[p[k]].name : 'facility'))
    .replace(/\{faction\}/g, () => (FACTIONS[p.faction] ? FACTIONS[p.faction].name : 'raiders'))
    .replace(/\{strength\}/g, () => String(Math.round(p.strength || 0)));
}

// Concrete consequences of one choice, for both the UI preview and resolution.
export function choiceOutcome(s, inst, choice) {
  const lose = {};
  for (const [r, share] of Object.entries(choice.lose || {})) {
    lose[r] = Math.floor(s.res[r] * Math.min(0.9, share * EVENTS_CFG.loseScale));
  }
  const gain = scaled(s, choice.gain, EVENTS_CFG.gainScale);
  const c = caps(s);
  for (const r of Object.keys(gain)) {
    gain[r] = Math.min(gain[r], Math.floor(c[r]));
  }
  const levels = (choice.loseLevel || []).map((k) => inst.params && inst.params[k]).filter((id) => id && level(s, id) > 0);
  const units = {};
  for (const [tab, share] of Object.entries(choice.loseUnits || {})) {
    units[tab] = ITEMS.filter((i) => i.tab === tab).reduce((n, i) => n + Math.floor(owned(s, i.id) * share), 0);
  }
  const siege = choice.siege && inst.params ? { faction: inst.params.faction, strength: inst.params.strength, at: inst.left } : null;
  // A price can never exceed storage, or the choice could not exist.
  const cost = scaled(s, choice.cost, EVENTS_CFG.costScale);
  for (const r of Object.keys(cost)) {
    cost[r] = Math.min(cost[r], Math.floor(c[r]));
  }
  const align = choice.align ? Math.round(choice.align * EVENTS_CFG.alignScale) : 0;
  return { cost, gain, lose, levels, units, siege, align, grudge: choice.grudge || null };
}

export function canChoose(s, inst, choice) {
  return canAfford(s, choiceOutcome(s, inst, choice).cost);
}

export const pendingEvent = (s) => s.events[0] || null;

function pickBuildings(s, keys) {
  const pool = BUILDINGS.filter((b) => PICKABLE.includes(b.kind) && level(s, b.id) > 0).map((b) => b.id);
  if (pool.length < keys.length) {
    return null;
  }
  const params = {};
  for (const k of keys) {
    const id = pick(s, pool);
    params[k] = id;
    pool.splice(pool.indexOf(id), 1);
  }
  return params;
}

// Creates a pending event instance; returns false when its building picks are impossible.
function spawn(s, ev, front) {
  const hasUnits = (tab) => ITEMS.some((i) => i.tab === tab && owned(s, i.id) > 0);
  if (ev.needsUnits && !ev.needsUnits.every(hasUnits)) {
    return false;
  }
  const params = ev.pick ? pickBuildings(s, ev.pick) : {};
  if (!params) {
    return false;
  }
  if (ev.threat) {
    // A force sized to beat what the player could build up in half the decision window.
    const raiders = activeRaiders(s);
    if (!raiders.length) {
      return false;
    }
    params.faction = pick(s, raiders);
    const grown = factors(s).defense + projectDefense(s, ev.deadline / 2);
    const floor = FACTIONS[params.faction].raidFloor;
    params.strength = Math.ceil(Math.max(floor, grown) * range(s, ev.threat.min, ev.threat.max));
  }
  const inst = { uid: ++s.eventSeq, id: ev.id, left: ev.deadline, total: ev.deadline, params };
  if (front) {
    s.events.unshift(inst);
  } else {
    s.events.push(inst);
  }
  s.events.length = Math.min(s.events.length, EVENTS_CFG.maxPending);
  s.recentEvents.push(ev.id);
  if (s.recentEvents.length > RECENT_EVENTS) {
    s.recentEvents.shift();
  }
  s.fx.push({ kind: 'event', uid: inst.uid, aftermath: !!ev.aftermath });
  return true;
}

// Called when a raid breaks through: the player has to choose what to lose.
export function spawnAftermath(s, rout) {
  let n = rout ? EVENTS_CFG.aftermathOnRout : EVENTS_CFG.aftermathOnDefeat;
  const pool = EVENTS.filter((e) => e.aftermath && !s.events.some((x) => x.id === e.id));
  while (n > 0 && pool.length) {
    const ev = pick(s, pool);
    pool.splice(pool.indexOf(ev), 1);
    if (spawn(s, ev, true)) {
      say(s, 'aftermath', { title: ev.title }, 'bad');
      n--;
    }
  }
}

// Deadlines always run; new story events only arrive while the player is here.
export function advanceEvents(s, dt, offline) {
  for (const inst of s.events.slice()) {
    inst.left -= dt;
    if (inst.left <= 0) {
      const ev = EVENT_BY_ID[inst.id];
      resolveEvent(s, inst.uid, ev.def, true);
    }
  }
  if (offline || s.events.some((x) => !EVENT_BY_ID[x.id].aftermath)) {
    return;
  }
  s.eventTimer -= dt;
  if (s.eventTimer > 0) {
    return;
  }
  s.eventTimer = range(s, EVENTS_CFG.intervalMin, EVENTS_CFG.intervalMax);
  const core = level(s, 'core');
  const pool = EVENTS.filter((e) => !e.aftermath && e.minCore <= core && !s.recentEvents.includes(e.id));
  while (pool.length) {
    const ev = weighted(s, pool);
    if (spawn(s, ev, false)) {
      return;
    }
    pool.splice(pool.indexOf(ev), 1);
  }
}

// Threats come up more often than ordinary dilemmas.
function weighted(s, pool) {
  const w = (e) => (e.threat ? EVENTS_CFG.threatWeight : 1);
  let r = rand(s) * pool.reduce((a, e) => a + w(e), 0);
  for (const e of pool) {
    r -= w(e);
    if (r <= 0) return e;
  }
  return pool[pool.length - 1];
}

export function resolveEvent(s, uid, index, expired = false) {
  const inst = s.events.find((x) => x.uid === uid);
  const ev = inst && EVENT_BY_ID[inst.id];
  const choice = ev && ev.choices[index];
  if (!choice || (!expired && !canChoose(s, inst, choice))) {
    return null;
  }
  const out = choiceOutcome(s, inst, choice);
  for (const [r, v] of Object.entries(out.cost)) {
    s.res[r] = Math.max(0, s.res[r] - v);
  }
  grant(s, out.gain);
  for (const [r, v] of Object.entries(out.lose)) {
    s.res[r] -= v;
  }
  if (choice.items) {
    giveItems(s, choice.items);
  }
  for (const id of out.levels) {
    loseLevel(s, id);
  }
  for (const [tab, share] of Object.entries(choice.loseUnits || {})) {
    loseUnits(s, tab, share);
  }
  if (out.align) {
    s.align = Math.max(ALIGNMENT.min, Math.min(ALIGNMENT.max, s.align + out.align));
  }
  if (out.grudge) {
    out.grudgeRoll = addGrudge(s, out.grudge);
    say(s, 'grudge', { faction: FACTIONS[out.grudge.faction].name }, 'bad');
  }
  if (choice.buff) {
    s.buffs.push({ ...choice.buff, remaining: choice.buff.duration });
  }
  if (choice.raidDelay) {
    delayRaid(s, choice.raidDelay);
  }
  if (out.siege) {
    // Refusing locks the attack to the original deadline; silence brings it immediately.
    startSiege(s, out.siege.faction, out.siege.strength, expired ? 0 : out.siege.at);
    say(s, 'siege', { faction: FACTIONS[out.siege.faction].name, strength: out.siege.strength }, 'bad');
  }
  s.events = s.events.filter((x) => x !== inst);
  s.stats.events++;
  const label = fillText(inst, choice.label);
  const result = fillText(inst, choice.result);
  if (expired) {
    s.stats.expired = (s.stats.expired || 0) + 1;
    say(s, 'eventExpired', { title: ev.title, label }, 'bad');
    s.inbox.push({ kind: 'expired', id: ev.id, choice: index, title: ev.title, label, result, out });
  }
  say(s, 'event', { title: ev.title, result }, 'story');
  return { ev, inst, choice, out, label, result };
}

export function alignmentLabel(a) {
  if (a >= ALIGNMENT.guardianAt) {
    return 'Guardian';
  }
  if (a <= ALIGNMENT.overlordAt) {
    return 'Overlord';
  }
  return 'Undecided';
}

// ---------- timed effects ----------

export function advanceBuffs(s, dt) {
  for (const b of s.buffs) {
    b.remaining -= dt;
    if (b.remaining <= 0) {
      say(s, 'buffEnd', { label: b.label });
    }
  }
  s.buffs = s.buffs.filter((b) => b.remaining > 0);
}

// ---------- directives ----------

export function directiveProgress(s, d) {
  const c = d.cond;
  if (c.level) {
    return [level(s, c.level), c.n];
  }
  if (c.item) {
    return [owned(s, c.item), c.n];
  }
  if (c.factor) {
    return [factors(s)[c.factor], c.n];
  }
  if (c.sector) {
    return [s.sectors.includes(c.sector) ? 1 : 0, 1];
  }
  if (c.raidsWon) {
    return [s.stats.raidsWon, c.raidsWon];
  }
  if (c.threat) {
    return [threat(s), c.threat];
  }
  return [0, 1];
}

export const currentDirective = (s) => DIRECTIVES[s.directive] || null;

export function checkDirectives(s) {
  const d = currentDirective(s);
  if (!d) {
    return;
  }
  const [have, need] = directiveProgress(s, d);
  if (have < need) {
    return;
  }
  grant(s, d.reward);
  s.directive++;
  say(s, 'directive', { text: d.text }, 'good');
  s.fx.push({ kind: 'directive', text: d.text, reward: d.reward });
}

// ---------- chapters ----------

export function checkChapters(s) {
  for (const ch of CHAPTERS) {
    if (ch.id > s.chapter && level(s, 'core') >= ch.core) {
      s.chapter = ch.id;
      say(s, 'chapter', { kicker: CHAPTER_TEXT[ch.id].kicker, title: ch.title }, 'rank');
      s.inbox.push({ kind: 'chapter', id: ch.id });
    }
  }
}
