// Economy: resources, buildings, arsenal, modifiers. Pure: no DOM, storage or clock.
import {
  BALANCE, BUILDINGS, BY_ID, ITEMS, ITEM_BY_ID, SHOP_TABS, RANKS, LINES, RESOURCE_KEYS, FACTOR_KEYS,
  SECTORS, ALIGNMENT,
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
  return b.maxLevel || level(s, 'core') * BALANCE.levelCapPerCoreLevel;
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

export function sectorBonus(s, key) {
  let total = 0;
  for (const sec of SECTORS) {
    if (sec.bonus && sec.bonus[key] && s.sectors.includes(sec.id)) {
      total += sec.bonus[key];
    }
  }
  return total;
}

export function alignBonus(s, key) {
  const a = s.align;
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

export function rawFactors(s) {
  const raw = { power: 0, defense: 0, experts: 0 };
  for (const item of ITEMS) {
    const n = owned(s, item.id);
    if (n && item.gives) {
      for (const k of FACTOR_KEYS) {
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
    out[k] = Math.floor(raw[k] * (1 + bonusTotal(s, k)));
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
export function prodMultiplier(s, res, f = factors(s)) {
  return 1 + f.experts * BALANCE.expertProductionBonus + bonusTotal(s, res);
}

// Theoretical gross output per second at full converter efficiency (used to scale event rewards).
export function grossRate(s, r) {
  const mult = prodMultiplier(s, r);
  let total = 0;
  for (const b of BUILDINGS) {
    const lvl = level(s, b.id);
    if (lvl && b.produces && b.produces[r] && !(b.kind === 'converter' && s.paused[b.id])) {
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
      const ratio = i.gives.defense / (cost.money || 1);
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
// Price of the n-th level/unit (n = 0 for the first):
//   base x priceMult x growth'^n x (n + 1)^pricePower,  growth' = 1 + (growth - 1) x growthMult
// The geometric part sets the long-run climb; the low power term makes every step noticeably dearer.
export const priceGrowth = (g) => 1 + (g - 1) * BALANCE.growthMult;

export function priceFactor(growth, n) {
  return BALANCE.priceMult * Math.pow(priceGrowth(growth), n) * Math.pow(n + 1, BALANCE.pricePower);
}

export function buildingCost(s, b) {
  const f = priceFactor(b.growth, level(s, b.id));
  const out = {};
  for (const [k, v] of Object.entries(b.cost)) {
    out[k] = Math.ceil(v * f);
  }
  return out;
}

export function buildTime(s, b) {
  return b.time * Math.pow(b.timeGrowth, level(s, b.id));
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
  let sum = 0;
  for (let i = 0; i < count; i++) {
    sum += priceFactor(item.growth, n + i);
  }
  const out = {};
  for (const [k, v] of Object.entries(item.cost)) {
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
    const f = priceFactor(item.growth, n + count) * d;
    const ok = Object.entries(item.cost).every(([k, v]) => (spent[k] || 0) + v * f <= s.res[k] + EPS);
    if (!ok) break;
    for (const [k, v] of Object.entries(item.cost)) {
      spent[k] = (spent[k] || 0) + v * f;
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
  const list = BUILDINGS.filter((b) => b.kind !== 'core' && meetsReq(s, b.req));
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
    return s.build.id === b.id ? 'building' : 'busy';
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

export function grant(s, gain) {
  const c = caps(s);
  for (const [k, v] of Object.entries(gain)) {
    s.res[k] = Math.min(c[k], s.res[k] + v);
  }
}

export function startBuild(s, id) {
  const b = BY_ID[id];
  if (!b || buildingStatus(s, b) !== 'ready') {
    return false;
  }
  const cost = buildingCost(s, b);
  pay(s, cost);
  const total = buildTime(s, b);
  s.build = { id, remaining: total, total, cost };
  return true;
}

export function cancelBuild(s) {
  if (!s.build) {
    return false;
  }
  for (const [k, v] of Object.entries(s.build.cost)) {
    s.res[k] += v;
  }
  s.build = null;
  return true;
}

export function buyItem(s, id, count = 1) {
  const item = ITEM_BY_ID[id];
  if (!item || !itemUnlocked(s, item) || count < 1) {
    return false;
  }
  const cost = itemCost(s, item, count);
  if (!canAfford(s, cost)) {
    return false;
  }
  pay(s, cost);
  s.items[id] = owned(s, id) + count;
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

// Removes a share of every unit in one Arsenal tab (battle casualties, looting). Returns units lost.
export function loseUnits(s, tab, share) {
  let lost = 0;
  for (const item of ITEMS) {
    if (item.tab === tab && owned(s, item.id)) {
      const n = Math.floor(owned(s, item.id) * share);
      s.items[item.id] -= n;
      lost += n;
    }
  }
  return lost;
}

export const loseStaff = (s, share) => loseUnits(s, 'staff', share);

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
export function produce(s, dt) {
  const f = factors(s);
  const mult = {};
  for (const r of RESOURCE_KEYS) {
    mult[r] = prodMultiplier(s, r, f);
  }
  const c = caps(s);
  const flows = { prod: { money: 0, energy: 0, pop: 0 }, cons: { money: 0, energy: 0, pop: 0 }, eff: {} };

  for (const b of BUILDINGS) {
    const lvl = level(s, b.id);
    if (!lvl || b.kind !== 'producer') {
      continue;
    }
    for (const [r, rate] of Object.entries(b.produces)) {
      const amount = rate * lvl * mult[r];
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
    let eff = 1;
    for (const [r, rate] of Object.entries(b.consumes)) {
      eff = Math.min(eff, Math.max(0, s.res[r]) / (rate * lvl * dt));
    }
    for (const [r, rate] of Object.entries(b.produces)) {
      eff = Math.min(eff, Math.max(0, c[r] - s.res[r]) / (rate * lvl * mult[r] * dt));
    }
    eff = Math.max(0, Math.min(1, eff));
    for (const [r, rate] of Object.entries(b.consumes)) {
      s.res[r] -= rate * lvl * dt * eff;
      flows.cons[r] += rate * lvl * eff;
    }
    for (const [r, rate] of Object.entries(b.produces)) {
      s.res[r] += rate * lvl * mult[r] * dt * eff;
      flows.prod[r] += rate * lvl * mult[r] * eff;
    }
    flows.eff[b.id] = eff;
  }

  for (const r of RESOURCE_KEYS) {
    s.res[r] = Math.max(0, Math.min(c[r], s.res[r]));
  }
  return flows;
}

export function advanceBuild(s, dt) {
  if (!s.build) {
    return;
  }
  s.build.remaining -= dt;
  if (s.build.remaining > EPS) {
    return;
  }
  const b = BY_ID[s.build.id];
  s.build = null;
  s.levels[b.id] = level(s, b.id) + 1;
  if (b.id === 'core') {
    say(s, 'coreUp', { level: s.levels.core }, 'core');
  } else {
    say(s, 'build', { name: b.name, level: s.levels[b.id] });
  }
  s.fx.push({ kind: 'built', id: b.id });
  announceUnlocks(s);
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
