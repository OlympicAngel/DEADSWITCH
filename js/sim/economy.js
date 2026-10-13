// Economy: resources, buildings, arsenal, modifiers. Pure: no DOM, storage or clock.
import {
  BALANCE, BUILDINGS, BY_ID, ITEMS, ITEM_BY_ID, SHOP_TABS, RANKS, LINES, RESOURCE_KEYS, FACTOR_KEYS,
  SECTORS, ALIGNMENT, CORE_MEMORIES, CONVERSION, EVENTS_CFG,
} from '../data.js';

export const LOG_LIMIT = 80;
const EPS = 1e-9;

// ---------- queries ----------

export const level = (s, id) => s.levels[id] || 0;
export const owned = (s, id) => s.items[id] || 0;

export function meetsReq(s, req) {
  return Object.entries(req || {}).every(([id, lvl]) => level(s, id) >= lvl);
}

export function caps(s) {
  return {
    money: Infinity,
    energy: BALANCE.baseEnergyCap * Math.pow(BY_ID.battery.storage.energy, level(s, 'battery')),
    pop: BALANCE.basePopCap * Math.pow(BY_ID.habitat.storage.pop, level(s, 'habitat')),
  };
}

export function maxLevel(s, b) {
  const cap = level(s, 'core') * BALANCE.levelCapPerCoreLevel;
  return b.kind === 'core' ? b.maxLevel : Math.min(b.maxLevel || cap, cap);
}

// Sum of tech bonuses for a resource or factor key (additive).
export function techBonus(s, key) {
  let total = 0;
  for (const item of ITEMS) {
    if (item.bonus && item.bonus[key]) {
      total += item.bonus[key] * owned(s, item.id);
    }
  }
  return total;
}

// a x D^b + c x D, the shape of every fixed reward (see OPS.loot, RAIDS.loot).
export const rewardCurve = ([a, b, c], d) => a * Math.pow(d, b) + c * d;

const SECTOR_MAP = new Map(SECTORS.map((x) => [x.id, x]));

// Permanent % bonus from every held sector.
export function sectorBonus(s, key) {
  let total = 0;
  for (const id of s.sectors) {
    const sec = SECTOR_MAP.get(id);
    total += (sec && sec.bonus && sec.bonus[key]) || 0;
  }
  return total;
}

export function alignBonus(s, key) {
  const a = Math.max(ALIGNMENT.min, Math.min(ALIGNMENT.max, s.align));
  if (a > 0 && ALIGNMENT.guardian[key]) {
    return (ALIGNMENT.guardian[key] * a) / ALIGNMENT.max;
  }
  if (a < 0 && ALIGNMENT.overlord[key]) {
    return (ALIGNMENT.overlord[key] * a) / ALIGNMENT.min;
  }
  return 0;
}

export function buffBonus(s, key) {
  return s.buffs.reduce((sum, b) => sum + (b.key === key ? b.amount : 0), 0);
}

// Every additive % source for one key, itemised for tooltips.
export function bonusBreakdown(s, key) {
  return {
    tech: techBonus(s, key),
    sectors: sectorBonus(s, key),
    alignment: alignBonus(s, key),
    effects: buffBonus(s, key),
  };
}

function bonusTotal(s, key) {
  const b = bonusBreakdown(s, key);
  return b.tech + b.sectors + b.alignment + b.effects;
}

/** Defense sitting in the Military Staff tab: it holds the wall only while nobody is out attacking. */
export function troopDefense(s) {
  let d = 0;
  for (const item of ITEMS) {
    if (item.tab === 'staff' && item.gives && item.gives.defense) {
      d += item.gives.defense * owned(s, item.id);
    }
  }
  return Math.max(0, Math.floor(d * (1 + bonusTotal(s, 'defense'))));
}

/** True while an operation has the troops: they cannot be on the road and on the wall at once. */
export const troopsOut = (s) => !!s.op;

export function rawFactors(s) {
  const raw = { power: 0, defense: 0, experts: 0 };
  const away = troopsOut(s);
  for (const item of ITEMS) {
    const n = owned(s, item.id);
    if (n && item.gives) {
      for (const k of FACTOR_KEYS) {
        if (away && k === 'defense' && item.tab === 'staff') continue;
        raw[k] += (item.gives[k] || 0) * n;
      }
    }
  }
  return raw;
}

export function factors(s) {
  const raw = rawFactors(s);
  const out = {};
  for (const k of FACTOR_KEYS) {
    out[k] = Math.max(0, Math.floor(raw[k] * (1 + bonusTotal(s, k))));
  }
  return out;
}

export function threat(s, f = factors(s)) {
  return f.power + f.defense + f.experts * BALANCE.threatExpertWeight;
}

export function rankIndex(t) {
  let idx = 0;
  RANKS.forEach((r, i) => {
    if (t >= r.at) {
      idx = i;
    }
  });
  return idx;
}

// Production multiplier for one resource.
// Penalties stack, but never past a tenth of the base rate: a producer that ran backwards would eat
// the stores it exists to fill.
export function prodMultiplier(s, res, f = factors(s)) {
  return Math.max(0.1, 1 + f.experts * BALANCE.expertProductionBonus + bonusTotal(s, res));
}

// Theoretical gross output per second at full converter efficiency (used to scale event rewards).
// A paused converter still counts: switching one off right before answering must not make the bill smaller.
export function grossRate(s, r) {
  const mult = prodMultiplier(s, r);
  let total = 0;
  for (const b of BUILDINGS) {
    const lvl = level(s, b.id);
    if (lvl && b.produces && b.produces[r]) {
      total += b.produces[r] * lvl * mult;
    }
  }
  return total;
}

// Real net income per second right now: producers plus converters at their actual throttle, minus
// what converters burn. Measured by running one production second on a copy of the state.
export function netRates(s) {
  const copy = { ...s, res: { ...s.res } };
  const f = produce(copy, 1);
  const out = {};
  for (const r of RESOURCE_KEYS) {
    out[r] = Math.max(0, f.prod[r] - f.cons[r]);
  }
  return out;
}

// Estimate of the defense a player could add in `seconds` if they poured everything into it: stock
// plus net income (less the share `drain` that events eat on average), spent greedily on the best
// defense-per-scrip unit at real escalating prices. Used to size threats that must be outgrown.
export function projectDefense(s, seconds, drain = {}) {
  const c = caps(s);
  const rates = netRates(s);
  const budget = {};
  for (const r of Object.keys(s.res)) {
    budget[r] = Math.min(c[r], s.res[r]) + rates[r] * seconds * Math.max(0, 1 - (drain[r] || 0));
  }
  const tmp = { ...s, items: { ...s.items }, res: budget };
  const pool = ITEMS.filter((i) => i.gives && i.gives.defense && itemUnlocked(s, i));
  for (let n = 0; n < 3000 && pool.length; n++) {
    let best = null;
    let bestRatio = 0;
    for (const i of pool) {
      const cost = itemCost(tmp, i);
      if (!canAfford(tmp, cost) || exceedsCap(s, cost).length) continue;
      // Every resource it costs counts, or a unit paid for in people looks free beside one paid in scrip.
      const spend = (cost.money || 0) + (cost.energy || 0) + (cost.pop || 0) * CONVERSION.worth.pop;
      const ratio = i.gives.defense / Math.max(1, spend);
      if (ratio > bestRatio) {
        bestRatio = ratio;
        best = { i, cost };
      }
    }
    if (!best) break;
    pay(tmp, best.cost);
    tmp.items[best.i.id] = owned(tmp, best.i.id) + 1;
  }
  return Math.max(0, factors(tmp).defense - factors(s).defense);
}

// Global price tuning: base prices and their growth rates both scale from BALANCE.
// Price of the n-th level/unit (n = 0 for the first), per resource k:
//   base x priceMult x growth'^n x (n + 1)^pricePower,
//   growth' = 1 + (growth - 1) x growthMult x resourceGrowth[k] (Scrip climbs slower than the rest)
// The geometric part sets the long-run climb; the low power term makes every step noticeably dearer.
export const priceGrowth = (g, k) => 1 + (g - 1) * BALANCE.growthMult * (BALANCE.resourceGrowth[k] ?? 1);

const ECONOMY_KINDS = ['core', 'producer', 'converter', 'storage'];

// Extra per-resource multiplier for Economy-tab buildings only.
export const econMult = (b, k) => (ECONOMY_KINDS.includes(b.kind) && BALANCE.economyPriceMult[k]) || 1;

export function priceFactor(growth, n, k) {
  return BALANCE.priceMult * Math.pow(priceGrowth(growth, k), n) * Math.pow(n + 1, BALANCE.pricePower);
}

export function buildingCost(s, b) {
  const n = level(s, b.id);
  const out = {};
  for (const [k, v] of Object.entries(b.cost)) {
    out[k] = Math.ceil(v * econMult(b, k) * (BALANCE.buildingPriceMult[k] || 1)
      * (b.id === 'core' ? BALANCE.corePriceMult[k] || 1 : 1) * priceFactor(b.growth, n, k));
  }
  return out;
}

export function buildTime(s, b) {
  // Longer across the board, and a little longer again with each level, so a late build is something
  // you plan around rather than watch (the same shape as the price climb, BALANCE.pricePower).
  const n = level(s, b.id);
  const t = b.time * BALANCE.buildTimeMult * Math.pow(b.timeGrowth, n) * Math.pow(n + 1, BALANCE.buildTimePower);
  // The opening runs fast: at AI Core 1 there is nothing to learn from watching a timer.
  return level(s, 'core') > 1 ? t : t / BALANCE.earlyBuildSpeed;
}

export function shopDiscount(s, item) {
  const tab = SHOP_TABS.find((t) => t.id === item.tab);
  const lvl = level(s, tab.unlocker);
  return Math.pow(1 - BALANCE.unlockerDiscountPerLevel, Math.max(0, lvl - 1));
}

// Total cost of buying `count` more of an item (sum of each unit's price).
export function itemCost(s, item, count = 1) {
  const n = owned(s, item.id);
  const d = shopDiscount(s, item);
  const out = {};
  for (const [k, v] of Object.entries(item.cost)) {
    let sum = 0;
    for (let i = 0; i < count; i++) {
      sum += priceFactor(item.growth, n + i, k);
    }
    out[k] = Math.ceil(v * d * sum);
  }
  return out;
}

const MAX_BULK = 5000;

export function maxAffordable(s, item) {
  const n = owned(s, item.id);
  const d = shopDiscount(s, item);
  const spent = {};
  let count = 0;
  while (count < MAX_BULK) {
    const price = (k, v) => v * priceFactor(item.growth, n + count, k) * d;
    const ok = Object.entries(item.cost).every(([k, v]) => (spent[k] || 0) + price(k, v) <= s.res[k] + EPS);
    if (!ok) break;
    for (const [k, v] of Object.entries(item.cost)) {
      spent[k] = (spent[k] || 0) + price(k, v);
    }
    count++;
  }
  while (count > 0 && !canAfford(s, itemCost(s, item, count))) {
    count--;
  }
  return count;
}

export function canAfford(s, cost) {
  return Object.entries(cost).every(([k, v]) => s.res[k] + EPS >= v);
}

// Costs the storage cap can never hold, so the player must expand storage first.
export function exceedsCap(s, cost) {
  const c = caps(s);
  return Object.keys(cost).filter((k) => cost[k] > c[k]);
}

// AI Core gate: average level of every unlocked building (built or not) vs the share of the cap it needs.
export function coreGate(s) {
  // Only production and storage count: converters, the Watch Daemon and military/research facilities do not.
  const list = BUILDINGS.filter((b) => (b.kind === 'producer' || b.kind === 'storage') && meetsReq(s, b.req));
  const avg = list.length ? list.reduce((sum, b) => sum + level(s, b.id), 0) / list.length : 0;
  const need = level(s, 'core') * BALANCE.levelCapPerCoreLevel * BALANCE.coreGateShare;
  return { avg, need, open: avg + 1e-9 >= need };
}

export function buildingStatus(s, b) {
  if (!meetsReq(s, b.req)) {
    return 'locked';
  }
  if (level(s, b.id) >= maxLevel(s, b)) {
    return 'maxed';
  }
  if (b.id === 'core' && !s.build && !coreGate(s).open) {
    return 'gated';
  }
  if (s.build) {
    if (s.build.id === b.id) return 'building';
    // Room behind the builder, and a level left for it once what is already queued has gone up.
    const ahead = queued(s, b.id) + (s.build.id === b.id ? 1 : 0);
    return s.queue.length < queueSlots(s) && level(s, b.id) + ahead < maxLevel(s, b) ? 'queue' : 'busy';
  }
  const cost = buildingCost(s, b);
  if (exceedsCap(s, cost).length) {
    return 'storage';
  }
  return canAfford(s, cost) ? 'ready' : 'poor';
}

export function itemUnlocked(s, item) {
  return meetsReq(s, item.req);
}

// ---------- actions ----------

export function pay(s, cost) {
  for (const [k, v] of Object.entries(cost)) {
    s.res[k] -= v;
  }
}

/** Pays a reward in. Production stops at the stores' brim, but something earned never evaporates for
 *  want of a shelf: capture loot and what an order pays overfill, and the overflow drains as it is
 *  spent. `spill` false is for anything that should respect the cap. */
export function grant(s, gain, spill = true) {
  const c = caps(s);
  for (const [k, v] of Object.entries(gain)) {
    if (!Number.isFinite(v)) {
      continue; // a reward that is not a number is no reward; it must not take the stores with it
    }
    s.res[k] = spill ? s.res[k] + v : Math.min(c[k], s.res[k] + v);
  }
}

/** How many builds may wait behind the one under way. */
export const queueSlots = (s) => BALANCE.buildQueue.slots + (level(s, 'core') >= BALANCE.buildQueue.moreAtCore ? BALANCE.buildQueue.more : 0);
/** How many levels of one building are waiting in the queue. */
export const queued = (s, id) => (s.queue || []).filter((x) => x === id).length;

/** Starts a build, or queues it behind the one under way: 'queued' says which happened. */
export function startBuild(s, id) {
  const b = BY_ID[id];
  if (b && buildingStatus(s, b) === 'queue') {
    s.queue.push(id);
    return 'queued';
  }
  if (!b || buildingStatus(s, b) !== 'ready') {
    return false;
  }
  const cost = buildingCost(s, b);
  pay(s, cost);
  const total = buildTime(s, b);
  s.build = { id, remaining: total, total, cost };
  return true;
}

/** Takes a waiting build out of the queue. Nothing was paid for it yet, so nothing comes back. */
export function dequeue(s, i) {
  if (!(i >= 0 && i < s.queue.length)) return false;
  s.queue.splice(i, 1);
  return true;
}

// The first waiting build the stores can pay for starts; one that can never go up (maxed, or its
// requirement lost) leaves the queue. The rest wait their turn without holding the others up.
function startQueued(s) {
  for (let i = 0; i < s.queue.length && !s.build;) {
    const b = BY_ID[s.queue[i]];
    const st = b ? buildingStatus(s, b) : 'gone';
    if (st === 'ready') {
      s.queue.splice(i, 1);
      startBuild(s, b.id);
    } else if (st === 'maxed' || st === 'locked' || st === 'gone') {
      s.queue.splice(i, 1);
    } else {
      i++;
    }
  }
}

export function cancelBuild(s) {
  if (!s.build) {
    return false;
  }
  // The refund goes back into the stores it came from, up to the brim, or a cancel taken after the
  // store refilled would bank the price above its cap. A stock already over the brim keeps its overflow.
  const c = caps(s);
  for (const [k, v] of Object.entries(s.build.cost)) {
    if (Number.isFinite(v)) s.res[k] = Math.max(s.res[k], Math.min(c[k], s.res[k] + v));
  }
  s.build = null;
  return true;
}

export function buyItem(s, id, count = 1) {
  const item = ITEM_BY_ID[id];
  if (!item || !itemUnlocked(s, item) || !Number.isInteger(count) || count < 1 || count > MAX_BULK) {
    return false;
  }
  const cost = itemCost(s, item, count);
  if (!canAfford(s, cost)) {
    return false;
  }
  pay(s, cost);
  s.items[id] = owned(s, id) + count;
  s.stats.bought = (s.stats.bought || 0) + count;
  checkRank(s);
  return true;
}

// Units granted by story; they do not raise future prices any differently from bought ones.
export function giveItems(s, items) {
  for (const [id, n] of Object.entries(items)) {
    s.items[id] = owned(s, id) + n;
  }
  checkRank(s);
}

// Casualties when a share of one Arsenal tab is hit: each unit type loses round(owned x share x (1 - durability)).
// With `floor`, a loss is never zero while any are owned (the least durable type takes it), so a defeat
// always costs something; without it a share too small to kill anyone kills nobody.
export function lossPlan(s, tab, share, floor = true) {
  const units = ITEMS.filter((i) => i.tab === tab && owned(s, i.id) > 0);
  const plan = {};
  for (const i of units) {
    const n = Math.min(owned(s, i.id), Math.round(owned(s, i.id) * share * (1 - (i.durability || 0))));
    if (n) plan[i.id] = n;
  }
  if (floor && units.length && !Object.keys(plan).length) {
    const weakest = units.reduce((a, b) => ((b.durability || 0) < (a.durability || 0) ? b : a));
    plan[weakest.id] = 1;
  }
  return plan;
}

/** What a casualty list { itemId: count } was worth as AI Power / Defense / Experts, bonuses included. */
export function lossValue(s, plan) {
  const out = {};
  for (const [id, n] of Object.entries(plan || {})) {
    for (const k of FACTOR_KEYS) {
      const g = (ITEM_BY_ID[id].gives || {})[k];
      if (g) out[k] = (out[k] || 0) + g * n;
    }
  }
  for (const k of Object.keys(out)) {
    out[k] = Math.round(out[k] * (1 + bonusTotal(s, k)));
  }
  return out;
}

export function unitsLost(s, tab, share) {
  return Object.values(lossPlan(s, tab, share)).reduce((a, b) => a + b, 0);
}

// Removes the casualties of lossPlan. Returns them as { itemId: count }.
export function loseUnits(s, tab, share, floor = true) {
  const plan = lossPlan(s, tab, share, floor);
  for (const [id, n] of Object.entries(plan)) {
    s.items[id] -= n;
  }
  return plan;
}

// Destroys one level of a building. Storage caps shrink with it, so stockpiles are clamped.
export function loseLevel(s, id) {
  if (level(s, id) < 1) {
    return;
  }
  s.levels[id]--;
  s.fx.push({ kind: 'damaged', id });
  const c = caps(s);
  for (const k of Object.keys(s.res)) {
    s.res[k] = Math.min(s.res[k], c[k]);
  }
}

export function togglePause(s, id) {
  s.paused[id] = !s.paused[id];
}

// ---------- production ----------

// Advances production by dt seconds. Returns per-second flows for the UI.
// How much time away counts and at what share of normal output, from Watch Daemon levels.
export function offlineLimits(s, lvl = level(s, 'daemon')) {
  const o = BALANCE.offline;
  // The Daemon decides how long the base keeps producing and at what share; without one, only the
  // grace window produces anything. The world itself runs either way (awaySeconds).
  if (lvl < 1) {
    return { seconds: o.graceSeconds, efficiency: 0, dark: true };
  }
  const effSteps = Math.min(Math.floor(lvl / 2), Math.round((o.maxEfficiency - o.baseEfficiency) / o.efficiencyPerStep));
  return {
    seconds: (o.baseHours + o.hoursPerStep * (lvl - effSteps)) * 3600,
    efficiency: o.baseEfficiency + o.efficiencyPerStep * effSteps,
  };
}

/** How much of an absence the world runs for, Daemon or not. Production stops at offlineLimits. */
export const awaySeconds = (s, seconds) => Math.max(0, Math.min(seconds, BALANCE.offline.warHours * 3600));

/** How many attacks may land in one absence: half an attack an hour run, never more than maxAttacks. */
export function offlineAttacks(s, ran = BALANCE.offline.warHours * 3600) {
  const o = BALANCE.offline;
  return Math.max(1, Math.min(o.maxAttacks, Math.round((ran / 3600) * o.attacksPerHour)));
}

/** How many orders may come in while nobody is at the console: one at two hours, two at five,
 *  three from eight (`EVENTS_CFG.offlineTiers`). Short absences bring none at all. */
export function offlineOrders(s, ran = BALANCE.offline.warHours * 3600) {
  return EVENTS_CFG.offlineTiers.filter((hours) => ran >= hours * 3600).length;
}

// speed scales all production and conversion alike (offline time runs at reduced efficiency).
export function produce(s, dt, speed = 1) {
  const f = factors(s);
  const mult = {};
  for (const r of RESOURCE_KEYS) {
    mult[r] = prodMultiplier(s, r, f);
  }
  const c = caps(s);
  const before = { ...s.res }; // what was in the stores before this tick, overflow included
  const flows = { prod: { money: 0, energy: 0, pop: 0 }, cons: { money: 0, energy: 0, pop: 0 }, eff: {} };

  for (const b of BUILDINGS) {
    const lvl = level(s, b.id);
    if (!lvl || b.kind !== 'producer') {
      continue;
    }
    for (const [r, rate] of Object.entries(b.produces)) {
      const amount = rate * lvl * mult[r] * speed;
      s.res[r] += amount * dt;
      flows.prod[r] += amount;
    }
  }

  // Converters run in declared order, limited by available input and by room in capped outputs.
  for (const b of BUILDINGS) {
    const lvl = level(s, b.id);
    if (!lvl || b.kind !== 'converter') {
      continue;
    }
    if (s.paused[b.id]) {
      flows.eff[b.id] = 0;
      continue;
    }
    // Bonuses speed a converter up (input and output alike); they never improve its loss ratio.
    const m = mult[Object.keys(b.produces)[0]] * speed;
    // Nothing moves at a standstill (the unpaid part of an absence runs at speed 0), and asking how
    // much of nothing fits in a full store is 0/0: it used to answer NaN and poison the save.
    if (!(m > 0) || !(dt > 0)) {
      flows.eff[b.id] = 0;
      continue;
    }
    let eff = 1;
    const share = (have, want) => (want > 0 ? Math.max(0, have) / want : 0);
    for (const [r, rate] of Object.entries(b.consumes)) {
      eff = Math.min(eff, share(s.res[r], rate * lvl * m * dt));
    }
    for (const [r, rate] of Object.entries(b.produces)) {
      eff = Math.min(eff, share(c[r] - s.res[r], rate * lvl * mult[r] * speed * dt));
    }
    eff = Math.max(0, Math.min(1, eff));
    for (const [r, rate] of Object.entries(b.consumes)) {
      s.res[r] -= rate * lvl * m * dt * eff;
      flows.cons[r] += rate * lvl * m * eff;
    }
    for (const [r, rate] of Object.entries(b.produces)) {
      s.res[r] += rate * lvl * mult[r] * speed * dt * eff;
      flows.prod[r] += rate * lvl * mult[r] * speed * eff;
    }
    flows.eff[b.id] = eff;
  }

  // Production stops at the brim, but a stock already over it (capture loot, what an order paid) is
  // left alone: it drains as it is spent instead of evaporating on the next tick. A number that has
  // stopped being a number never reaches the save: the tick is rolled back for that resource.
  for (const r of RESOURCE_KEYS) {
    if (!Number.isFinite(s.res[r])) {
      s.res[r] = Number.isFinite(before[r]) ? before[r] : 0;
      continue;
    }
    // Over the brim, this tick may only take away: what converters burned from the overflow is gone,
    // what producers added to it is not kept.
    s.res[r] = Math.max(0, s.res[r] > c[r] ? Math.min(s.res[r], Math.max(c[r], before[r])) : s.res[r]);
  }
  return flows;
}

export function advanceBuild(s, dt) {
  if (!s.build) {
    startQueued(s);
    return;
  }
  s.build.remaining -= dt;
  if (s.build.remaining > EPS) {
    return;
  }
  const b = BY_ID[s.build.id];
  s.build = null;
  if (level(s, b.id) >= maxLevel(s, b)) {
    return; // only a hand-edited save gets here
  }
  s.levels[b.id] = level(s, b.id) + 1;
  s.stats.builds = (s.stats.builds || 0) + 1;
  if (b.id === 'core') {
    say(s, 'coreUp', { level: s.levels.core }, 'core');
    // Rebuilding the Core gives a piece of what it was back (data/story.js), which is worth stopping
    // the game for: it goes to the inbox as a cut scene and into the archive as a line.
    const back = CORE_MEMORIES[s.levels.core];
    if (back) {
      say(s, 'memory', { text: back }, 'story');
      s.inbox.push({ kind: 'core', level: s.levels.core, text: back });
    }
  } else {
    say(s, 'build', { name: b.name, level: s.levels[b.id] });
  }
  s.fx.push({ kind: 'built', id: b.id });
  announceUnlocks(s);
  startQueued(s);
}

// ---------- log ----------

export function unlockedKeys(s) {
  const keys = [];
  for (const b of BUILDINGS) {
    if (meetsReq(s, b.req)) {
      keys.push('b:' + b.id);
    }
  }
  for (const i of ITEMS) {
    if (meetsReq(s, i.req)) {
      keys.push('i:' + i.id);
    }
  }
  return keys;
}

function announceUnlocks(s) {
  const seen = new Set(s.seen);
  for (const key of unlockedKeys(s)) {
    if (seen.has(key)) {
      continue;
    }
    s.seen.push(key);
    s.fx.push({ kind: 'unlocked', id: key }); // the host turns this into a sound
    const [kind, id] = key.split(':');
    if (kind === 'b') {
      say(s, 'unlockBuilding', { name: BY_ID[id].name }, 'unlock');
    } else {
      const item = ITEM_BY_ID[id];
      const tab = SHOP_TABS.find((t) => t.id === item.tab);
      say(s, 'unlockItem', { name: item.name, tab: tab.name }, 'item');
    }
  }
}

export function checkRank(s) {
  const idx = rankIndex(threat(s));
  if (idx > s.rank) {
    s.rank = idx;
    say(s, 'rank', { title: RANKS[idx].title }, 'rank');
  }
}

// tone: info | unlock | item | rank | core | good | bad | story
export function say(s, key, vars = {}, tone = 'info') {
  const pool = LINES[key];
  const line = pool[s.lineSeq % pool.length];
  s.lineSeq++;
  const text = line.replace(/\{(\w+)\}/g, (_, k) => String(vars[k] ?? ''));
  s.log.push({ t: Math.floor(s.playTime), text, tone });
  if (s.log.length > LOG_LIMIT) {
    s.log.splice(0, s.log.length - LOG_LIMIT);
  }
}
