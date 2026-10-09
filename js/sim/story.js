// Story systems: choice events, directives, chapters, alignment and timed effects.
import {
  EVENTS, EVENTS_CFG, RAIDS, FACTORS, DIRECTIVES, CHAPTERS, CHAPTER_TEXT, ALIGNMENT, BUILDINGS, BY_ID, ITEMS, FACTIONS, CLANS, LESSONS,
} from '../data.js';
import {
  level, factors, rawFactors, threat, grossRate, canAfford, grant, giveItems, owned, say, caps, loseLevel, loseUnits, unitsLost, projectDefense,
  buildingStatus,
} from './economy.js';
import { delayRaid, activeRaiders, startSiege, addGrudge, borders, shiftStrength, stirAggression, spawnAssault, loseSector, sectorById } from './war.js';
import { stirClan } from './clans.js';
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

// Average share of income that ordinary events consume: for every event the player can currently get,
// the mean net cost of its choices (cost minus gain, in seconds of production, after scaling), spread
// over the mean time between events.
export function eventDrain(s) {
  const core = level(s, 'core');
  const pool = EVENTS.filter((e) => !e.aftermath && !e.threat && !e.urgent && e.minCore <= core);
  const interval = (EVENTS_CFG.intervalMin + EVENTS_CFG.intervalMax) / 2;
  const out = {};
  for (const r of ['money', 'energy', 'pop']) {
    if (!pool.length) {
      out[r] = 0;
      continue;
    }
    const perEvent = pool.reduce((sum, e) => sum + e.choices.reduce((a, c) =>
      a + ((c.cost && c.cost[r]) || 0) * EVENTS_CFG.costScale - ((c.gain && c.gain[r]) || 0) * EVENTS_CFG.gainScale, 0) / e.choices.length, 0) / pool.length;
    out[r] = Math.min(0.9, Math.max(0, perEvent / interval));
  }
  return out;
}

// Fills {a}/{b} with the building names picked for this event instance.
export function fillText(inst, str) {
  const p = inst.params || {};
  return String(str)
    .replace(/\{(a|b)\}/g, (_, k) => (BY_ID[p[k]] ? BY_ID[p[k]].name : 'facility'))
    .replace(/\{faction\}/g, () => (FACTIONS[p.faction] ? FACTIONS[p.faction].name : 'raiders'))
    .replace(/\{strength\}/g, () => String(Math.round(p.strength || 0)))
    .replace(/\{(node|held)\}/g, (_, k) => (sectorById(p[k]) ? sectorById(p[k]).name : 'the border'));
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
    units[tab] = unitsLost(s, tab, share);
  }
  const callOff = choice.cancelSiege && s.sieges.some((x) => x.uid === inst.uid);
  // A price can never exceed storage, or the choice could not exist.
  const cost = scaled(s, choice.cost, EVENTS_CFG.costScale);
  for (const r of Object.keys(cost)) {
    cost[r] = Math.min(cost[r], Math.floor(c[r]));
  }
  const align = choice.align ? Math.round(choice.align * EVENTS_CFG.alignScale) : 0;
  const p = inst.params || {};
  // Buffs last a stretch rolled when the event fired (EVENTS_CFG.buffStretch).
  const buff = choice.buff ? { ...choice.buff, duration: Math.round(choice.buff.duration * (p.buffStretch || 1)) } : null;
  // Effects on the sectors named in the event.
  const strength = choice.strength ? { id: p.node, delta: choice.strength } : null;
  const assault = choice.assault && p.node ? { from: p.node, target: p.held, delay: choice.assault } : null;
  const cede = choice.cede && p.held ? p.held : null;
  return { cost, gain, lose, levels, units, callOff, align, grudge: choice.grudge || null, buff, strength, assault, cede, clearMarks: choice.clearMarks && p.held ? p.held : null };
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
    const id = pick(s, pool, 'event');
    params[k] = id;
    pool.splice(pool.indexOf(id), 1);
  }
  return params;
}

const hasUnits = (s, tab) => ITEMS.some((i) => i.tab === tab && owned(s, i.id) > 0);

// True when every effect of every choice would actually do something right now: no buff on a factor or
// income the player does not have, no loss of stock or units they do not own, no grudge from a faction
// that is not raiding, no raid delay before raids start. Otherwise another event fires instead.
export function applicable(s, ev) {
  if (ev.border && !borders(s, true).length) {
    return false;
  }
  if (ev.needsUnits && !ev.needsUnits.every((tab) => hasUnits(s, tab))) {
    return false;
  }
  const raw = rawFactors(s);
  const raiding = level(s, 'core') >= RAIDS.startAtCore && activeRaiders(s);
  return ev.choices.every((c) => {
    if (c.buff && !(FACTORS[c.buff.key] ? raw[c.buff.key] > 0 : grossRate(s, c.buff.key) > 0)) return false;
    if (Object.entries(c.lose || {}).some(([r, share]) => s.res[r] * share * EVENTS_CFG.loseScale < 1)) return false;
    if (Object.keys(c.loseUnits || {}).some((tab) => !hasUnits(s, tab))) return false;
    if (Object.keys(c.cost || {}).some((r) => grossRate(s, r) <= 0 && s.res[r] <= 0)) return false;
    if (c.grudge && !(raiding && raiding.includes(c.grudge.faction))) return false;
    if (c.raidDelay && !(raiding && raiding.length)) return false;
    return true;
  });
}

// Creates a pending event instance; returns false when the event cannot take effect or its picks are impossible.
function spawn(s, ev, front) {
  if (!applicable(s, ev)) {
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
    params.faction = pick(s, raiders, 'event');
    const grown = factors(s).defense + projectDefense(s, ev.deadline / 2, eventDrain(s));
    const floor = FACTIONS[params.faction].raidFloor;
    params.strength = Math.ceil(Math.max(floor, grown) * range(s, ev.threat.min, ev.threat.max, 'event'));
  }
  if (ev.border) {
    // A border between one of your sectors and an enemy one; the event is about those two. Clans whose
    // chapter is still sealed come up now and then: an order is one of the few things that wakes them.
    const all = borders(s, true);
    if (!all.length) return false;
    const open = all.filter((b) => !b.locked);
    const b = pick(s, open.length && rand(s, 'event') >= EVENTS_CFG.lockedBorderChance ? open : all, 'event');
    params.node = b.from;
    params.held = b.target;
  }
  params.buffStretch = range(s, EVENTS_CFG.buffStretch[0], EVENTS_CFG.buffStretch[1], 'event');
  const inst = { uid: ++s.eventSeq, id: ev.id, left: ev.deadline, total: ev.deadline, params };
  if (front) {
    s.events.unshift(inst);
  } else {
    s.events.push(inst);
  }
  s.events.length = Math.min(s.events.length, EVENTS_CFG.maxPending);
  // A threat's force is on its way from the moment it is announced: it shows among the inbound
  // attacks with its own countdown, and only paying turns it back. (Not if the queue dropped it.)
  if (ev.threat && s.events.includes(inst)) {
    startSiege(s, inst.params.faction, inst.params.strength, ev.deadline, inst.uid);
    say(s, 'siege', { faction: FACTIONS[inst.params.faction].name, strength: inst.params.strength }, 'bad');
  }
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
    const ev = pick(s, pool, 'event');
    pool.splice(pool.indexOf(ev), 1);
    if (spawn(s, ev, true)) {
      say(s, 'aftermath', { title: ev.title }, 'bad');
      n--;
    }
  }
}

// Deadlines always run; new story events only arrive while the player is here.
// Urgent events need the player at the console (active), never offline.
export function advanceEvents(s, dt, offline, active = false) {
  for (const inst of s.events.slice()) {
    inst.left -= dt;
    if (inst.left <= 0) {
      const ev = EVENT_BY_ID[inst.id];
      if (ev.def === null) {
        // Nothing is decided by silence: what it warned about was scheduled when it arrived. Not
        // paying a bill is not a missed order, so it does not count as one.
        s.events = s.events.filter((x) => x !== inst);
        say(s, 'threatIgnored', { title: fillText(inst, ev.title) }, 'bad');
      } else {
        resolveEvent(s, inst.uid, ev.def, true);
      }
    }
  }
  // One dilemma at a time, but a threat is a standing bill rather than a question: leaving it unpaid
  // must not stop the rest of the war from talking to me.
  const waiting = s.events.some((x) => !EVENT_BY_ID[x.id].aftermath && !EVENT_BY_ID[x.id].threat);
  if (offline || waiting) {
    return;
  }
  s.eventTimer -= dt;
  if (s.eventTimer > 0) {
    return;
  }
  s.eventTimer = range(s, EVENTS_CFG.intervalMin, EVENTS_CFG.intervalMax, 'event');
  const core = level(s, 'core');
  const pool = EVENTS.filter((e) => !e.aftermath && (!e.urgent || active) && e.minCore <= core && !s.recentEvents.includes(e.id));
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
  const w = (e) => e.weight || (e.threat ? EVENTS_CFG.threatWeight : 1);
  let r = rand(s, 'event') * pool.reduce((a, e) => a + w(e), 0);
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
  if (out.buff) {
    s.buffs.push({ ...out.buff, remaining: out.buff.duration });
  }
  if (out.strength && sectorById(out.strength.id) && !s.sectors.includes(out.strength.id)) {
    shiftStrength(s, out.strength.id, out.strength.delta);
  }
  // Aggression is hidden, so it is applied straight from the choice and never shown in the preview.
  if (choice.aggr && inst.params && inst.params.node) {
    stirAggression(s, inst.params.node, choice.aggr);
    const sec = sectorById(inst.params.node);
    stirClan(s, sec.faction, { fury: ((choice.aggr.clan || 0) + (choice.aggr.node || 0)) * CLANS.interceptShare });
  }
  if (out.clearMarks && s.nodes[out.clearMarks]) {
    s.nodes[out.clearMarks].marks = 0;
  }
  if (out.assault && !s.sectors.includes(out.assault.from) && s.sectors.includes(out.assault.target)) {
    spawnAssault(s, { from: out.assault.from, target: out.assault.target }, out.assault.delay);
  }
  if (out.cede && s.sectors.includes(out.cede)) {
    loseSector(s, out.cede, sectorById(inst.params.node).faction);
  }
  if (choice.raidDelay) {
    delayRaid(s, choice.raidDelay);
  }
  if (out.callOff) {
    // The force was already on its way; paying is what turns it back.
    s.sieges = s.sieges.filter((x) => x.uid !== inst.uid);
    say(s, 'siegeOff', { faction: FACTIONS[inst.params.faction].name });
  }
  s.events = s.events.filter((x) => x !== inst);
  s.stats.events++;
  const label = fillText(inst, choice.label);
  const result = fillText(inst, choice.result);
  if (expired) {
    s.stats.expired = (s.stats.expired || 0) + 1;
    say(s, 'eventExpired', { title: fillText(inst, ev.title), label }, 'bad');
    s.inbox.push({ kind: 'expired', id: ev.id, choice: index, title: fillText(inst, ev.title), label, result, out });
  }
  say(s, 'event', { title: fillText(inst, ev.title), result }, 'story');
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
    if (b.hold) continue; // a stance holds its own effect: sim/clans.js adds and removes it
    b.remaining -= dt;
    if (b.remaining <= 0) {
      say(s, 'buffEnd', { label: b.label });
    }
  }
  s.buffs = s.buffs.filter((b) => b.hold || b.remaining > 0);
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

// ---------- lessons ----------

/** Whether the state has reached what a lesson (or a step) is waiting for. */
export function reached(s, cond) {
  if (!cond || typeof cond !== 'object') {
    return false;
  }
  if (cond.level && !Object.entries(cond.level).every(([id, n]) => level(s, id) >= n)) return false;
  if (cond.items && !Object.entries(cond.items).every(([id, n]) => owned(s, id) >= n)) return false;
  if (cond.sectors && s.sectors.length - 1 < cond.sectors) return false;
  if (cond.events && s.events.length < cond.events) return false;
  if (cond.coreReady && buildingStatus(s, BY_ID.core) !== 'ready') return false;
  if (cond.scripted && !Object.entries(cond.scripted).every(([k, n]) => (s.scripted[k] || 0) >= n)) return false;
  return true;
}

/** The next lesson owed to the player, or null. One at a time, in the order they are written. */
export function lessonDue(s) {
  return LESSONS.find((l) => !s.taught.includes(l.id) && reached(s, l.when)) || null;
}

export function markTaught(s, id) {
  if (!s.taught.includes(id)) {
    s.taught.push(id);
  }
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
