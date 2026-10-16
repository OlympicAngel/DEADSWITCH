// Story systems: choice events, directives, chapters, alignment and timed effects.
import {
  EVENTS, EVENTS_CFG, RAIDS, FACTORS, DIRECTIVES, DAILY, DAILY_TASKS, CHAPTERS, CHAPTER_TEXT, ALIGNMENT, BUILDINGS, BY_ID, ITEMS, FACTIONS, CLANS, LESSONS, RESOURCE_KEYS, FACTOR_KEYS, SECTORS,
} from '../data.js';
import {
  level, factors, threatFactors, rawFactors, threat, grossRate, canAfford, grant, giveItems, owned, say, caps, loseLevel, loseUnits, unitsLost, lossPlan, lossValue, projectDefense,
  buildingStatus,
} from './economy.js';
import { delayRaid, reinforceBlock, attacks, activeRaiders, startSiege, addGrudge, borders, shiftStrength, stirAggression, spawnAssault, loseSector, sectorById, factionOf, breaches, opLoot, resetPush, sectorStatus } from './war.js';
import { stirClan } from './clans.js';
import { range, pick, rand, mix } from './rng.js';

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
/** A daily order's line for a count: "Capture 1 sector", "Capture 2 sectors". */
export function dailyText(task, n, show = String) {
  return task.text.replace('{n}', show(n)).replace(/\{s\}/g, n === 1 ? '' : 's');
}

export function fillText(inst, str) {
  const p = inst.params || {};
  return String(str)
    .replace(/\{(a|b)\}/g, (_, k) => (BY_ID[p[k]] ? BY_ID[p[k]].name : 'facility'))
    .replace(/\{faction\}/g, () => (FACTIONS[p.faction] ? FACTIONS[p.faction].name : 'raiders'))
    .replace(/\{strength\}/g, () => String(Math.round(p.strength || 0)))
    // A name that opens with "The" drops to "the" inside a sentence: "along the Sump line", not "the The".
    .replace(/(\bthe )?\{(node|held|aim)\}/gi, (m, the, k, at, all) => {
      const name = sectorById(p[k]) ? sectorById(p[k]).name : 'the border';
      const opens = at === 0 || /[.!?:]["\u201d]?\s*$/.test(all.slice(0, at));
      return (the && /^the /i.test(name) ? '' : the || '') + (opens ? name : name.replace(/^The /, 'the '));
    })
    .replace(/^the /, 'The ');
}

// Concrete consequences of one choice, for both the UI preview and resolution.
export function choiceOutcome(s, inst, choice, expired = false) {
  // An order that came in while nobody was listening asks for more and pays more. An answer that
  // costs me Humanity is cheap by nature, so the one that buys it back is discounted and pays better.
  const big = inst.big ? EVENTS_CFG.offlineScale : 1;
  // What came in while nobody was listening hits as hard as it pays, answered or not.
  const harm = big;
  const good = choice.align > 0;
  const lose = {};
  for (const [r, share] of Object.entries(choice.lose || {})) {
    lose[r] = Math.floor(s.res[r] * Math.min(expired ? EVENTS_CFG.expiredLoseCap : 0.9, share * EVENTS_CFG.loseScale * harm));
  }
  // What an order pays is paid in full: it overfills the stores rather than vanishing into them.
  const gain = scaled(s, choice.gain, EVENTS_CFG.gainScale * big * (good ? EVENTS_CFG.goodGain : 1));
  const c = caps(s);
  const levels = (choice.loseLevel || []).map((k) => inst.params && inst.params[k]).filter((id) => id && level(s, id) > 0);
  const units = {};
  const unitWorth = {}; // what those troops are worth as Power / Defense, so the order can say so
  for (const [tab, share] of Object.entries(choice.loseUnits || {})) {
    units[tab] = unitsLost(s, tab, share * harm);
    unitWorth[tab] = lossValue(s, lossPlan(s, tab, share * harm));
  }
  const callOff = choice.cancelSiege && s.sieges.some((x) => x.uid === inst.uid);
  // A price can never exceed storage, or the choice could not exist.
  const cost = scaled(s, choice.cost, EVENTS_CFG.costScale * big * (good ? EVENTS_CFG.goodCost : 1));
  for (const r of Object.keys(cost)) {
    cost[r] = Math.min(cost[r], Math.floor(c[r]));
  }
  // Humanity moves further in both directions out there, and further again towards Guardian:
  // walking back from what the war makes of a machine has to be worth the walk.
  const align = choice.align
    ? Math.round(choice.align * EVENTS_CFG.alignScale * (choice.align > 0 ? big * EVENTS_CFG.goodScale : harm))
    : 0;
  const p = inst.params || {};
  // Buffs last a stretch rolled when the event fired; a penalty outlives a bonus (EVENTS_CFG).
  const buff = choice.buff
    ? { ...choice.buff, duration: Math.round(choice.buff.duration * ((choice.buff.amount < 0 ? p.badStretch : p.buffStretch) || 1)) }
    : null;
  // Effects on the sectors named in the event.
  const strength = choice.strength ? { id: p.node, delta: choice.strength } : null;
  const assault = choice.assault && p.node ? { from: p.node, target: p.held, delay: choice.assault } : null;
  const cede = choice.cede && p.held ? p.held : null;
  // Handing ground over is a sale, and the price is what that ground is worth, not a flat tribute.
  // It is sold once: ground taken back and handed over again pays nothing the second time.
  if (cede && !(s.ceded || []).includes(cede)) {
    for (const [r, v] of Object.entries(opLoot(sectorById(cede)))) {
      gain[r] = (gain[r] || 0) + Math.ceil(v * EVENTS_CFG.cedeShare);
    }
  }
  return { cost, gain, lose, levels, units, unitWorth, callOff, align, grudge: choice.grudge || null, buff, strength, assault, cede, clearMarks: choice.clearMarks && p.held ? p.held : null };
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
  if (ev.border) {
    const heals = ev.choices.some((c) => c.clearMarks);
    if (!borders(s, true).some((b) => !heals || breaches(s, b.target) > 0)) {
      return false;
    }
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
/** Some transmissions only reach a machine the wasteland already has an opinion about. */
export function inBand(s, ev) {
  return !ev.band || ev.band === alignmentLabel(s.align).toLowerCase();
}

function spawn(s, ev, front, big = false) {
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
    const grown = threatFactors(s).defense + projectDefense(s, ev.deadline / 2, eventDrain(s));
    const floor = FACTIONS[params.faction].raidBase;
    params.strength = Math.ceil(Math.max(floor, grown) * range(s, ev.threat.min, ev.threat.max, 'event'));
  }
  if (ev.border) {
    // A border between one of your sectors and an enemy one; the event is about those two. Clans whose
    // chapter is still sealed come up now and then: an order is one of the few things that wakes them.
    // An order that offers to seal breaches has to find a sector with breaches, or it is offering
    // nothing and should not have been sent.
    const heals = ev.choices.some((c) => c.clearMarks);
    // Some traffic only makes sense from a clan with clans in it (a feast, a warlord, a ledger).
    const all = borders(s, true).filter((b) => (!heals || breaches(s, b.target) > 0)
      && (!ev.factions || ev.factions.includes(factionOf(s, b.from))));
    if (!all.length) return false;
    const open = all.filter((b) => !b.locked);
    const b = pick(s, open.length && rand(s, 'event') >= EVENTS_CFG.lockedBorderChance ? open : all, 'event');
    params.node = b.from;
    params.held = b.target;
  }
  params.buffStretch = range(s, EVENTS_CFG.buffStretch[0], EVENTS_CFG.buffStretch[1], 'event');
  params.badStretch = range(s, EVENTS_CFG.badStretch[0], EVENTS_CFG.badStretch[1], 'event');
  if (s.events.length >= EVENTS_CFG.maxPending) {
    // A full queue only makes room for a breach's crisis, and never by dropping a standing bill:
    // a threat whose order vanished could no longer be paid off.
    const drop = front && s.events.slice().reverse().find((x) => !EVENT_BY_ID[x.id].threat);
    if (!drop) {
      return false;
    }
    s.events = s.events.filter((x) => x !== drop);
  }
  const inst = { uid: ++s.eventSeq, id: ev.id, left: ev.deadline, total: ev.deadline, params, big: big || undefined };
  if (front) {
    s.events.unshift(inst);
  } else {
    s.events.push(inst);
  }
  // A threat's force is on its way from the moment it is announced: it shows among the inbound
  // attacks with its own countdown, and only paying turns it back.
  if (ev.threat) {
    // The force picks its target now, so the announcement can name the place it is walking to.
    inst.params.aim = startSiege(s, inst.params.faction, inst.params.strength, ev.deadline, inst.uid, ev.title).target;
    say(s, 'siege', { faction: FACTIONS[inst.params.faction].name, target: sectorById(inst.params.aim).name, strength: inst.params.strength }, 'bad');
  }
  s.recentEvents.push(ev.id);
  if (s.recentEvents.length > RECENT_EVENTS) {
    s.recentEvents.shift();
  }
  s.fx.push({ kind: 'event', uid: inst.uid, aftermath: !!ev.aftermath });
  return true;
}

// Called when a raid breaks through: the player has to choose what to lose.
export function spawnAftermath(s, rout, offline = false) {
  let n = rout ? EVENTS_CFG.aftermathOnRout : EVENTS_CFG.aftermathOnDefeat;
  // A crisis out of a breach is still an order arriving, so it spends the same budget as any other:
  // without this a night of breaches queued a pile of them and they all opened the moment we got back.
  if (offline) {
    n = Math.min(n, s.offlineEventCap - s.offlineEvents);
  }
  const pool = EVENTS.filter((e) => e.aftermath && !s.events.some((x) => x.id === e.id));
  while (n > 0 && pool.length) {
    const ev = pick(s, pool, 'event');
    pool.splice(pool.indexOf(ev), 1);
    if (spawn(s, ev, true, offline)) {
      say(s, 'aftermath', { title: ev.title }, 'bad');
      if (offline) {
        s.offlineEvents++;
      }
      n--;
    }
  }
}

// Deadlines always run; new story events only arrive while the player is here.
// Urgent events need the player at the console (active), never offline.
export function advanceEvents(s, dt, offline, active = false) {
  // A threat is a bill for an attack still on the road. Once that attack has landed (met early or on
  // time) there is nothing left to pay for, so the order goes with it.
  s.events = s.events.filter((x) => !EVENT_BY_ID[x.id].threat || s.sieges.some((g) => g.uid === x.uid));
  for (const inst of s.events.slice()) {
    // An urgent order only arrives to someone watching; if they put the phone down, its two minutes
    // wait for them rather than running out in the dark.
    if (offline && EVENT_BY_ID[inst.id].urgent) continue;
    // A threat's bill waits as long as its attack does: an absence that has had its share of attacks
    // holds the siege at the gate, and the order to pay it off stays open with it.
    if (offline && EVENT_BY_ID[inst.id].threat && s.offlineRaids >= s.offlineRaidCap) continue;
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
  if (waiting) {
    return;
  }
  // The wasteland calls while nobody is listening too, a few times at most (offlineOrders). What
  // comes in out there asks for more and pays more, and if it runs out of time I answer it myself.
  if (offline && s.offlineEvents >= s.offlineEventCap) {
    return;
  }
  if (s.directive < EVENTS_CFG.quietDirective) {
    return;
  }
  s.eventTimer -= dt;
  if (s.eventTimer > 0) {
    return;
  }
  s.eventTimer = range(s, EVENTS_CFG.intervalMin, EVENTS_CFG.intervalMax, 'event');
  const core = level(s, 'core');
  const open = EVENTS.filter((e) => !e.aftermath && (!e.urgent || active) && e.minCore <= core && inBand(s, e));
  // Recent ones wait their turn, but never so many that a small early pool runs dry.
  const recent = s.recentEvents.slice(-Math.min(RECENT_EVENTS, Math.floor(open.length / 2)));
  const pool = open.filter((e) => !recent.includes(e.id));
  while (pool.length) {
    const ev = weighted(s, pool);
    if (spawn(s, ev, false, offline)) {
      if (offline) {
        s.offlineEvents++;
      }
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

/** The difference a decision made to what the base is and what it makes. Only losses are listed:
 *  this is the bill, and it is what the player will want to go and put right. */
function toll(s, was) {
  const now = { ...rawFactors(s), ...Object.fromEntries(RESOURCE_KEYS.map((r) => [r, grossRate(s, r)])) };
  const out = {};
  for (const k of Object.keys(was)) {
    const gone = was[k] - now[k];
    if (gone > (FACTOR_KEYS.includes(k) ? 0.5 : 0.005)) {
      out[k] = gone;
    }
  }
  return Object.keys(out).length ? out : null;
}

export function resolveEvent(s, uid, index, expired = false) {
  const inst = s.events.find((x) => x.uid === uid);
  const ev = inst && EVENT_BY_ID[inst.id];
  const choice = ev && ev.choices[index];
  if (!choice || (!expired && !canChoose(s, inst, choice))) {
    return null;
  }
  const out = choiceOutcome(s, inst, choice, expired);
  // What the base was worth before the decision, so the report can say what it actually cost rather
  // than only what was taken: a building level is a rate, troops are Power and Defense.
  const was = { ...rawFactors(s), ...Object.fromEntries(RESOURCE_KEYS.map((r) => [r, grossRate(s, r)])) };
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
  // The same share the preview showed: the away premium on anything that came in while away.
  const harm = inst.big ? EVENTS_CFG.offlineScale : 1;
  for (const [tab, share] of Object.entries(choice.loseUnits || {})) {
    loseUnits(s, tab, share * harm);
  }
  if (out.align) {
    s.align = Math.max(ALIGNMENT.min, Math.min(ALIGNMENT.max, s.align + out.align));
  }
  if (out.grudge) {
    out.grudgeRoll = addGrudge(s, out.grudge);
    say(s, 'grudge', { faction: FACTIONS[out.grudge.faction].name }, 'bad');
  }
  if (out.buff) {
    // The same order twice refreshes its effect instead of stacking a second copy of it.
    const same = s.buffs.find((b) => !b.clan && b.label === out.buff.label && b.key === out.buff.key);
    if (same) same.remaining = Math.max(same.remaining, out.buff.duration);
    else s.buffs.push({ ...out.buff, remaining: out.buff.duration });
  }
  if (out.strength && sectorById(out.strength.id) && !s.sectors.includes(out.strength.id)) {
    shiftStrength(s, out.strength.id, out.strength.delta);
  }
  // Aggression is hidden, so it is applied straight from the choice and never shown in the preview.
  if (choice.aggr && inst.params && inst.params.node) {
    stirAggression(s, inst.params.node, choice.aggr);
    const sec = sectorById(inst.params.node);
    resetPush(s, inst.params.node); // being talked about changes what they were doing
    stirClan(s, factionOf(s, sec.id), { fury: ((choice.aggr.clan || 0) + (choice.aggr.node || 0)) * CLANS.interceptShare });
  }
  if (out.clearMarks && s.nodes[out.clearMarks]) {
    s.nodes[out.clearMarks].marks = 0;
  }
  if (out.assault && !s.assault && !s.sectors.includes(out.assault.from) && s.sectors.includes(out.assault.target)) {
    spawnAssault(s, { from: out.assault.from, target: out.assault.target }, out.assault.delay);
  }
  if (out.cede && s.sectors.includes(out.cede)) {
    if (!s.ceded.includes(out.cede)) s.ceded.push(out.cede);
    loseSector(s, out.cede, factionOf(s, inst.params.node) || sectorById(inst.params.node).faction);
  }
  if (choice.raidDelay) {
    delayRaid(s, choice.raidDelay, ev.title);
  }
  if (out.callOff) {
    // The force was already on its way; paying is what turns it back.
    s.sieges = s.sieges.filter((x) => x.uid !== inst.uid);
    say(s, 'siegeOff', { faction: (FACTIONS[(inst.params || {}).faction] || { name: 'They' }).name });
  }
  s.events = s.events.filter((x) => x !== inst);
  s.stats.events++;
  if (!expired) s.stats.answered = (s.stats.answered || 0) + 1; // what the player answered, for the daily board
  const label = fillText(inst, choice.label).replace(/[.!?]+$/, ''); // quoted in a line that carries its own stop
  const result = fillText(inst, choice.result);
  if (expired) {
    s.stats.expired = (s.stats.expired || 0) + 1;
    say(s, 'eventExpired', { title: fillText(inst, ev.title), label }, 'bad');
    s.inbox.push({ kind: 'expired', id: ev.id, choice: index, title: fillText(inst, ev.title), label, result, out, toll: toll(s, was) });
  }
  say(s, 'event', { title: fillText(inst, ev.title), result }, 'story');
  return { ev, inst, choice, out, label, result, toll: toll(s, was) };
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

// ---------- daily orders ----------
// The engine has no clock, so the host hands in the calendar day (js/main.js). Everything below is
// pure: the same seed and the same day give the same orders, on any device and however many times
// the app is closed and opened.

/** The orders standing for one day: a pure hash of the save's seed and that day. */
export function dailyFor(s, day) {
  // Nothing on the board that the war cannot give today: no captures without a sector in reach, no
  // attacks to hold once nobody is left to send them.
  const can = { target: SECTORS.some((x) => sectorStatus(s, x) === 'target'), raiders: activeRaiders(s).length > 0 };
  const open = DAILY_TASKS.filter((t) => (!t.req || Object.entries(t.req).every(([id, n]) => level(s, id) >= n))
    && (!t.needs || can[t.needs]));
  const want = Math.min(open.length, DAILY.count + (level(s, 'core') >= DAILY.countAtCore ? 1 : 0));
  const left = open.slice();
  const out = [];
  for (let i = 0; i < want; i++) {
    const t = left.splice(mix(s.rng, day, i) % left.length, 1)[0];
    const [lo, hi] = t.n;
    out.push({ id: t.id, need: lo + (mix(s.rng, day, i, 7) % (hi - lo + 1)), from: s.stats[t.stat] || 0, done: false, claimed: false });
  }
  return out;
}

export const dailyTask = (id) => DAILY_TASKS.find((t) => t.id === id);

/** How far one order has got, counted from where its stat stood when the day started. */
export function dailyProgress(s, o) {
  const t = dailyTask(o.id);
  return [Math.max(0, (s.stats[t.stat] || 0) - o.from), o.need];
}

/** What one order pays: seconds of production, so it is worth clearing at Core 8 as at Core 2. */
export function dailyReward(s, weight = 1) {
  const mult = weight * (1 + Math.min(s.daily.streak, DAILY.streakMax) * DAILY.streakStep);
  return { money: eventValue(s, 'money', DAILY.seconds * mult), energy: eventValue(s, 'energy', DAILY.seconds * mult * DAILY.energyShare) };
}

/** Turns the day over when the host says it has. A day skipped entirely ends the streak. */
export function rollDaily(s, day) {
  if (!Number.isFinite(day) || s.daily.day === day) {
    return false;
  }
  const gap = day - s.daily.day;
  // A day or two backwards is a flight west or a clock nudged by hand: the board stays as it is, or
  // it could be dealt and paid again. A clock that was years ahead and got put right deals afresh.
  if (gap < 0 && gap >= -2) {
    return false;
  }
  const cleared = s.daily.tasks.length && s.daily.tasks.every((o) => o.done);
  // Nothing earned is lost to the turn of the day: what was left uncollected pays now, and says so.
  const left = claimDaily(s, 'all');
  if (left) s.fx.push({ kind: 'daily', auto: true, reward: left });
  s.daily.streak = s.daily.day && cleared && gap > 0 && gap <= DAILY.grace + 1 ? s.daily.streak + 1 : 0;
  s.daily.best = Math.max(s.daily.best || 0, s.daily.streak);
  s.daily.day = day;
  s.daily.tasks = dailyFor(s, day);
  s.daily.paid = false;
  s.daily.cleared = false;
  return true;
}

/** Marks whatever finished since the last look; its pay waits to be collected. Called every tick. */
export function checkDaily(s) {
  for (const o of s.daily.tasks) {
    const [have, need] = dailyProgress(s, o);
    if (o.done || have < need) {
      continue;
    }
    o.done = true;
    const task = dailyTask(o.id);
    say(s, 'daily', { text: dailyText(task, o.need) }, 'good');
    s.fx.push({ kind: 'daily', text: dailyText(task, o.need), reward: dailyReward(s, task.weight) });
  }
  if (boardCleared(s) && !s.daily.cleared) {
    s.daily.cleared = true;
    const n = s.daily.streak + 1;
    say(s, 'dailyAll', { n: n === 1 ? 'the first day' : `${n} days` }, 'good');
    s.fx.push({ kind: 'daily', text: 'Every order cleared', reward: dailyReward(s, DAILY.bonusShare), all: true });
  }
}

const boardCleared = (s) => s.daily.tasks.length > 0 && s.daily.tasks.every((o) => o.done);

/** What is waiting to be collected: finished orders, and the board itself once all of them are. */
export function dailyReady(s) {
  return s.daily.tasks.filter((o) => o.done && !o.claimed).length + (boardCleared(s) && !s.daily.paid ? 1 : 0);
}

/** Collects one finished order (its index), the cleared board ('board'), or everything ready ('all').
 *  The pay is sized when it is collected, on what the base makes then. Returns what was paid, or null. */
export function claimDaily(s, which) {
  const paid = {};
  const add = (r) => {
    grant(s, r);
    for (const [k, v] of Object.entries(r)) paid[k] = (paid[k] || 0) + v;
  };
  s.daily.tasks.forEach((o, i) => {
    if (o.done && !o.claimed && (which === 'all' || which === i)) {
      o.claimed = true;
      add(dailyReward(s, dailyTask(o.id).weight));
    }
  });
  if ((which === 'all' || which === 'board') && boardCleared(s) && !s.daily.paid) {
    s.daily.paid = true;
    add(dailyReward(s, DAILY.bonusShare));
  }
  return Object.keys(paid).length ? paid : null;
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
  if (cond.daily && s.daily.tasks.length < cond.daily) return false;
  if (cond.coreReady && buildingStatus(s, BY_ID.core) !== 'ready') return false;
  if (cond.scripted && !Object.entries(cond.scripted).every(([k, n]) => (s.scripted[k] || 0) >= n)) return false;
  if (cond.directive !== undefined && s.directive !== cond.directive) return false;
  // The lesson points at the reinforce button on the attack shown first, so that one has to be able to take help.
  if (cond.reinforceable && (!attacks(s)[0] || reinforceBlock(s, attacks(s)[0].target))) return false;
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
  s.lessonAt = null;
}

/** Where a running lesson has got to, so a reload picks it up there. */
export function markLessonStep(s, id, at) {
  s.lessonAt = { id, at };
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
