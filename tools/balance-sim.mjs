// Headless balance simulation: a bot plays several seeded runs and reports averaged milestone times.
// Usage: node tools/balance-sim.mjs [hours=24] [runs=5]
//
// The bot plays like a thinking player rather than a busy one. Every decision is a number:
// - prices scrip, energy and people against each other the way the converters do, so costs, rates
//   and rewards of different shapes compare directly
// - builds towards the next AI Core: the store that has to hold it first, then the Core, and while
//   the Core waits on base development, whatever lifts the gate soonest. Everything else is ranked
//   by how long it takes to pay for itself, with the shops judged on what they put on the shelf
// - saves for that step once it is in reach, and lets cheap buys past the reserve meanwhile
// - runs a converter only on an input that is going spare, since every conversion loses on the trade
// - buys what is short: Defense as an attack closes, Power when the map is out of reach, best value
//   otherwise, and never more than a share of the bank on one scare
// - launches operations on the best ground it can hold
// - weighs every transmission answer, Humanity included, at what it is worth to this base
// It does not play towards the daily board: the orders are cleared or not by how it plays anyway,
// which is what they are for, and chasing them would measure the bot rather than the game.
import { BALANCE, BUILDINGS, BY_ID, ITEMS, ITEM_BY_ID, SHOP_TABS, SECTORS, RANKS, RESOURCE_KEYS, FACTOR_KEYS, CONVERSION, ALIGNMENT } from '../js/data.js';
import * as E from '../js/engine.js';

const HOURS = Number(process.argv[2] || 24);
const RUNS = Number(process.argv[3] || 5);
const sum = (c) => Object.values(c).reduce((a, b) => a + b, 0);
const fmt = (t) => (t == null ? '   -   ' : `${Math.floor(t / 3600)}h${String(Math.floor((t % 3600) / 60)).padStart(2, '0')}m`.padStart(7));

// Bot's own dice, separate from the game RNG so choices do not shift game rolls.
function dice(seed) {
  let x = seed >>> 0;
  return () => {
    x = (x + 0x6d2b79f5) >>> 0;
    let t = Math.imul(x ^ (x >>> 15), x | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

// ---------- what things are worth to this player, right now ----------

const SAFE_ODDS = 0.92; // reinforce until an inbound attack looks survivable
const SHOP_EVERY = 10; // seconds of game time between shopping trips
const THINK_EVERY = 30; // and between the slower decisions: what to build, what to run, where to attack
const PANIC_WINDOW = 180; // seconds before an attack lands that the player starts reinforcing
const PANIC_SHARE = 0.6; // and no more than this share of the bank goes into one of them
const PANIC_BUYS = 80; // how many units one panic can add before the player gives up on it
const CORE_SOON = 1800; // seconds of income: this close to the next step up and the player saves for it
const RUN_ABOVE = 0.6; // a converter only burns an input whose store is fuller than this
const OP_ODDS = 0.72; // the odds a player will take on a sector worth having
const SPILL_SOON = 900; // a store this close to overflowing is worth enlarging now
const GATE_PULL = 0.2; // how much cheaper a building looks while the AI Core waits on base development
const TIER_DONE = 25; // units of one tier owned before the shelf above it is worth unlocking
const CHEAP_SHARE = 0.25; // a build this small beside what we are saving for is bought anyway
const HORIZON = 600; // seconds of future income a permanent gain is priced over

// Scrip, energy and people are priced against each other the way the converters do it, so costs and
// rates of different shapes can be compared in one number.
const worth = (c) => RESOURCE_KEYS.reduce((a, k) => a + (c[k] || 0) * CONVERSION.worth[k], 0);
const price = worth;

/** One Expert lifts every rate for good, so it is priced at what that lift makes over a while. */
const expertWorth = (s) => worth(E.netRates(s)) * BALANCE.expertProductionBonus * HORIZON + 1;

/** What the base makes per second, counted in one currency. */
function income(s) {
  const r = E.netRates(s);
  return worth(r);
}

// ---------- building: what pays for itself soonest ----------

/** Seconds of the base's whole income one building level costs; the yardstick the other scores use. */
const costSeconds = (s, b) => price(E.buildingCost(s, b)) / Math.max(1, income(s));

/** Seconds for a producer level to pay back what it costs. Lower is better; Infinity never pays. */
function payback(s, b) {
  const gain = worth(Object.fromEntries(Object.entries(b.produces || {}).map(([r, v]) => [r, v * E.prodMultiplier(s, r) * (E.level(s, b.id) ? BALANCE.levelStep : 1)])));
  return gain > 0 ? price(E.buildingCost(s, b)) / gain : Infinity;
}

/** A converter always loses on the trade, so it pays back only on income that would otherwise spill. */
function converterPayback(s, b) {
  const full = choking(s);
  const saved = Object.entries(b.consumes || {}).reduce((a, [r, v]) => a + (full.includes(r) ? worth({ [r]: v * E.levelRate(E.level(s, b.id)) || v }) : 0), 0);
  return saved > 0 ? price(E.buildingCost(s, b)) / (saved * CONVERSION.efficiency) : Infinity;
}

/** Storage makes nothing: it pays back on the income a full store would otherwise throw away. */
function storagePayback(s, b) {
  const c = E.caps(s);
  const r = E.netRates(s);
  let saved = 0;
  for (const k of Object.keys(b.storage || {})) {
    const rate = r[k] || 0;
    if (rate <= 0 || !Number.isFinite(c[k]) || (c[k] - s.res[k]) / rate > SPILL_SOON) {
      continue;
    }
    saved += worth({ [k]: rate });
  }
  return saved > 0 ? price(E.buildingCost(s, b)) / saved : Infinity;
}

/** An unlocker opens better shelves: worth most when its tab is empty or we have outgrown its top unit. */
function unlockerPayback(s, b) {
  const tab = SHOP_TABS.find((x) => x.unlocker === b.id);
  const mine = tab ? ITEMS.filter((i) => i.tab === tab.id) : [];
  const unlocked = mine.filter((i) => E.itemUnlocked(s, i));
  if (!unlocked.length) {
    return costSeconds(s, b) * 0.4; // a shelf we cannot reach at all is the cheapest thing to fix
  }
  const top = unlocked[unlocked.length - 1];
  const saturated = unlocked.length < mine.length && E.owned(s, top.id) >= TIER_DONE;
  return costSeconds(s, b) * (saturated ? 0.6 : 4);
}

/** A resource is choking when the stores are full and something is still trying to fill them. */
function choking(s) {
  const c = E.caps(s);
  const r = E.netRates(s);
  return RESOURCE_KEYS.filter((k) => Number.isFinite(c[k]) && s.res[k] >= c[k] * 0.98 && (r[k] || 0) > 0);
}

/** The build the base is working towards: the next AI Core, or the store that has to hold it first. */
function goal(s, b = BY_ID.core, seen = new Set()) {
  const status = E.buildingStatus(s, b);
  if (seen.has(b.id) || ['locked', 'maxed', 'gated', 'building'].includes(status)) {
    return null;
  }
  seen.add(b.id);
  const cost = E.buildingCost(s, b);
  for (const k of E.exceedsCap(s, cost)) {
    const store = BUILDINGS.find((x) => x.storage && x.storage[k]);
    const step = store && goal(s, store, seen);
    if (step) {
      return step;
    }
  }
  return { building: b, cost };
}

/** What to build next, by what it is actually for. Nothing is built just because it is cheap. */
function nextBuild(s) {
  const keep = coreReserve(s);
  const ready = BUILDINGS.filter((b) => {
    if (E.buildingStatus(s, b) !== 'ready') {
      return false;
    }
    // The base buildings the Core's gate waits on are never held back by the reserve, and neither is
    // anything small enough next to it to be worth the delay: a shop that pays for itself in minutes
    // is not the reason the next Core is late.
    if (b.id === 'core' || b.kind === 'producer' || b.kind === 'storage') {
      return true;
    }
    const c = E.buildingCost(s, b);
    if (price(c) <= price(keep) * CHEAP_SHARE) {
      return true;
    }
    return !Object.entries(keep).some(([r, need]) => s.res[r] - (c[r] || 0) < need);
  });
  if (!ready.length) {
    return null;
  }
  // Then whatever the Core is waiting on, which is the Core itself or the store that will hold it.
  const aim = goal(s);
  if (aim && ready.includes(aim.building)) {
    return aim.building;
  }
  // Storage too when a store is already spilling: income that cannot be held is income thrown away.
  const full = choking(s);
  const spilling = full.length && ready.find((b) => Object.keys(b.storage || {}).some((r) => full.includes(r)));
  if (spilling) {
    return spilling;
  }
  // Otherwise whatever pays for itself soonest, with the Core's gate pulling the base up behind it:
  // while the Core is waiting on base development, the buildings that count towards it are worth
  // far more than their own output, and the ones furthest behind the average count the most.
  const gate = E.coreGate(s);
  const gated = !gate.open && E.buildingStatus(s, BY_ID.core) === 'gated';
  let best = null;
  let bestScore = Infinity;
  for (const b of ready) {
    let score = b.kind === 'converter' ? converterPayback(s, b)
      : b.kind === 'storage' ? storagePayback(s, b)
        : b.kind === 'unlocker' ? unlockerPayback(s, b)
          : b.kind === 'offline' ? costSeconds(s, b) * 1.5
            : payback(s, b);
    const counts = b.kind === 'producer' || b.kind === 'storage';
    if (gated && counts) {
      score = Math.min(score, costSeconds(s, b)) * (E.level(s, b.id) <= gate.avg ? GATE_PULL : GATE_PULL * 3);
    }
    if (score < bestScore) {
      bestScore = score;
      best = b;
    }
  }
  // Nothing pays for itself yet: keep the ladder moving on the cheapest thing that lifts the Core's gate.
  if (!best) {
    best = ready.filter((b) => b.kind === 'producer' || b.kind === 'storage')
      .sort((x, y) => price(E.buildingCost(s, x)) - price(E.buildingCost(s, y)))[0] || null;
  }
  return best;
}

// ---------- converters: run them only while they are worth running ----------

// Every conversion loses a share of what goes in, so one is only ever worth running on a resource
// that is going spare: a store near its brim, or scrip over what the next build is being saved for.
function runsAtAProfit(s, b) {
  const c = E.caps(s);
  const keep = coreReserve(s);
  const spare = (k) => (Number.isFinite(c[k]) ? s.res[k] > c[k] * RUN_ABOVE : s.res[k] > (keep[k] || 0));
  const room = (k) => !Number.isFinite(c[k]) || s.res[k] < c[k] * 0.95;
  return Object.keys(b.consumes || {}).every(spare) && Object.keys(b.produces || {}).every(room);
}

/** Turns each converter on or off to match that, the way a player watches the bars. */
function tendConverters(s) {
  for (const b of BUILDINGS) {
    if (b.kind !== 'converter' || !E.level(s, b.id)) {
      continue;
    }
    if (runsAtAProfit(s, b) === !!s.paused[b.id]) {
      E.togglePause(s, b.id);
    }
  }
}

// ---------- the arsenal: buy what is actually short ----------

/** Value per scrip of one more of this unit, for what the player is shopping for. */
function value(s, item, want) {
  const g = item.gives || {};
  const good = want === 'defense' ? (g.defense || 0)
    : want === 'power' ? (g.power || 0)
      : (g.power || 0) + (g.defense || 0) + (g.experts || 0) * expertWorth(s);
  if (!good) return 0;
  const c = E.itemCost(s, item);
  if (!E.canAfford(s, c) || E.exceedsCap(s, c).length) return 0;
  return good / Math.max(1, price(c));
}

/** Tech pays a share of everything for ever, so it is priced against what it lifts. */
function techValue(s, item) {
  if (!item.bonus) return 0;
  const c = E.itemCost(s, item);
  if (!E.canAfford(s, c) || E.exceedsCap(s, c).length) return 0;
  const f = E.factors(s);
  const lift = Object.entries(item.bonus).reduce((a, [k, v]) => a + v * (FACTOR_KEYS.includes(k)
    ? (f[k] || 0) * (k === 'experts' ? expertWorth(s) : 1)
    : E.grossRate(s, k) * 60), 0);
  return lift / Math.max(1, price(c));
}

/** The best thing on the shelf right now, or null when nothing is affordable. */
function bestBuy(s, want) {
  let best = null;
  let top = 0;
  for (const i of ITEMS) {
    if (!E.itemUnlocked(s, i)) continue;
    const v = i.bonus ? techValue(s, i) : value(s, i, want);
    if (v > top) {
      top = v;
      best = i;
    }
  }
  return best;
}

/** What to hold back for the next step up, and only once it is near enough to be worth saving for. */
function coreReserve(s) {
  const aim = goal(s);
  if (!aim) {
    return {};
  }
  // With every producer at the Core's level cap there is nothing else left to grow: the step up is
  // the only way forward, so the player saves for it however long it takes.
  const stalled = BUILDINGS.every((b) => b.kind !== 'producer' || !E.meetsReq(s, b.req) || E.level(s, b.id) >= E.maxLevel(s, b));
  if (stalled) {
    return aim.cost;
  }
  const rates = E.netRates(s);
  const near = Object.entries(aim.cost).every(([r, need]) => s.res[r] + (rates[r] || 0) * CORE_SOON >= need);
  return near ? aim.cost : {};
}

// One shopping trip. The wall first and only as far as it has to go, then whatever is over the next
// Core level goes on whatever is holding the player back: Power when the map is out of reach,
// Defense when the next attack is not, and the best value going when neither is pressing.
function shop(s, atk) {
  // The wall is topped up as the attack comes in, not the whole time it is on the clock: scrip spent
  // early is scrip the base never got, and the raiders only grow with what we are holding.
  const close = atk && atk.remaining <= PANIC_WINDOW;
  const floor = s.res.money * (1 - PANIC_SHARE); // one panic never empties the bank
  for (let n = 0; close && n < PANIC_BUYS && s.res.money > floor && E.raidChance(s, atk) < SAFE_ODDS; n++) {
    const buy = bestBuy(s, 'defense');
    if (!buy) break;
    E.buyItem(s, buy.id);
  }
  const want = wantsMost(s, atk);
  const keep = coreReserve(s);
  for (let n = 0; n < 12; n++) {
    const buy = bestBuy(s, want);
    if (!buy) break;
    const c = E.itemCost(s, buy);
    if (Object.entries(keep).some(([r, need]) => s.res[r] - (c[r] || 0) < need)) break;
    E.buyItem(s, buy.id);
  }
}

/** Power, Defense, or anything: whichever is the thing standing in the way this minute. */
function wantsMost(s, atk) {
  if (atk && E.raidChance(s, atk) < 0.97) {
    return 'defense';
  }
  // Nothing on the map within reach means the offensive has stalled, and that is what to fix.
  const reachable = SECTORS.some((x) => E.sectorStatus(s, x) === 'target' && E.opChance(s, x) > OP_ODDS);
  return reachable ? 'any' : 'power';
}

// ---------- the map: take the ground worth taking ----------

/** What a sector is worth having, against what the attempt risks. */
function opScore(s, sec) {
  const odds = E.opChance(s, sec);
  if (odds < OP_ODDS || !E.canLaunch(s, sec)) {
    return 0;
  }
  const first = !s.taken.includes(sec.id);
  const loot = first ? price(E.opLoot(sec)) : 0;
  // The permanent bonus is the real prize: it pays for as long as the ground is held.
  const bonus = Object.entries(sec.bonus || {}).reduce((a, [k, v]) => a + v * (FACTOR_KEYS.includes(k)
    ? (E.factors(s)[k] || 0) * (k === 'experts' ? expertWorth(s) : 1)
    : E.grossRate(s, k) * 600), 0);
  const cost = price(E.opCost(sec)) + E.opTime(sec) * income(s) * 0.25;
  return (loot + bonus * 4) * odds / Math.max(1, cost);
}

/** The best target on the board, or null when nothing is worth the troops. */
function bestTarget(s) {
  let best = null;
  let top = 0;
  for (const sec of SECTORS) {
    if (E.sectorStatus(s, sec) !== 'target') continue;
    const v = opScore(s, sec);
    if (v > top) {
      top = v;
      best = sec;
    }
  }
  return best;
}

// ---------- transmissions: weigh the answers instead of guessing ----------

/** What the base is worth standing where it stands: its income over a while, plus what it can field. */
const standing = (s) => worth(E.netRates(s)) * HORIZON + E.threat(s);

/** What a shift in Humanity is worth: the bonuses it turns on or off, either side of neutral. */
function alignWorth(s, delta) {
  if (!delta) {
    return 0;
  }
  const after = { ...s, res: { ...s.res }, align: Math.max(ALIGNMENT.min, Math.min(ALIGNMENT.max, s.align + delta)) };
  return standing(after) - standing(s);
}

/** What an answer is worth, all in. Positive is a gain; the worst answer still scores least badly. */
function answerScore(s, inst, choice) {
  const o = E.choiceOutcome(s, inst, choice);
  let v = price(o.gain) - price(o.cost) - price(o.lose);
  // Units are worth what they contribute, levels what they produce.
  for (const [tab, n] of Object.entries(o.units)) {
    const item = ITEMS.find((i) => i.tab === tab);
    if (n && item) v -= n * price(E.itemCost(s, item)) * 0.6;
  }
  for (const id of o.levels) {
    const b = BY_ID[id];
    v -= worth(b.produces || {}) * 600 + price(E.buildingCost(s, b)) * 0.5;
  }
  const buff = o.buff;
  if (buff) {
    const base = FACTOR_KEYS.includes(buff.key) ? (E.factors(s)[buff.key] || 0) : E.grossRate(s, buff.key) * 60;
    v += buff.amount * base * (buff.duration / 60);
  }
  v += alignWorth(s, o.align);
  if (o.callOff) v += price(E.opCost(SECTORS[0])) + income(s) * 300; // an attack turned back is worth the tribute
  if (o.cede) v -= price(E.opLoot(E.sectorById(o.cede))) * 2;
  if (o.grudge) v -= income(s) * 240;
  if (choice.items) v += Object.entries(choice.items).reduce((a, [id, n]) => a + n * price(E.itemCost(s, ITEM_BY_ID[id])), 0);
  return v;
}

/** The answer a thinking player gives: the best of the ones they can afford. */
function answer(s, inst) {
  const ev = E.eventById(inst.id);
  let best = -Infinity;
  let pick = -1;
  ev.choices.forEach((c, i) => {
    if (!E.canChoose(s, inst, c)) return;
    const v = answerScore(s, inst, c);
    if (v > best) {
      best = v;
      pick = i;
    }
  });
  return pick;
}

function play(seed) {
  const s = E.newState(seed);
  s.name = 'bot';
  const roll = dice(seed * 7919);
  const marks = {};
  const snap = {}; // AI Defense the first second each Core level was reached
  const mark = (key, t) => {
    if (marks[key] === undefined) marks[key] = t;
  };
  const tally = { byClan: {}, dailies: 0, rushed: 0, ops: 0, align: 0, picked: 0, pricedOut: 0, sieges: 0, grudges: 0, tributes: 0, assaultsHeld: 0, assaultsBreached: 0, raidBreaches: 0, sectorsLost: 0, lootMin: [], salvageMin: [] };
  const ignored = new Set(); // threats the bot decided to take on the chin, counted once each

  for (let t = 0; t < HOURS * 3600; t++) {
    // The engine has no clock, so the host deals the daily board; here a day of play is a day.
    E.rollDaily(s, Math.floor(t / 86400) + 1);
    if (t % THINK_EVERY === 0) E.claimDaily(s, 'all'); // finished orders are collected when the player looks in
    E.step(s, 1, false, true);

    const atk = E.nextAttack(s);

    // The slow decisions are not made every second: what to build, what to leave running, where to go.
    if (t % THINK_EVERY === 0) {
      tendConverters(s);
      if (!s.build) {
        const b = nextBuild(s);
        if (b) E.startBuild(s, b.id);
      }
      // An operation takes nothing off the wall, so the only question is whether the target is worth it.
      const target = !s.op && bestTarget(s);
      if (target) {
        E.launchOp(s, target.id);
        tally.ops++;
      }
    }
    // Shopping, the way a player shops: not every second, and not the cheapest thing on the shelf.
    if (t % SHOP_EVERY === 0) {
      shop(s, atk);
    }

    for (const inst of s.events.slice()) {
      const ev = E.eventById(inst.id);
      const ok = ev.choices.map((c, i) => i).filter((i) => E.canChoose(s, inst, ev.choices[i]));
      let idx;
      if (ev.threat) {
        // A threat's attack is already on the board. Paying is the only answer, and it is worth it
        // only when the force coming is one we would not hold: otherwise the tribute buys nothing.
        const pay = ev.choices.findIndex((c) => c.cost);
        const siege = s.sieges.find((x) => x.uid === inst.uid);
        const worthPaying = siege && E.raidChance(s, siege) < 0.8;
        if (!ok.includes(pay) || !worthPaying) {
          if (!ignored.has(inst.uid)) {
            ignored.add(inst.uid);
            tally.sieges++;
          }
          continue;
        }
        idx = pay;
        tally.tributes++;
      } else {
        idx = answer(s, inst); // weighed, not guessed
        if (idx < 0) continue; // nothing affordable: wait, and let the deadline decide
      }
      if (ev.choices[idx].grudge) tally.grudges++;
      tally.align += E.choiceOutcome(s, inst, ev.choices[idx]).align;
      tally.picked++;
      if (ok.length < ev.choices.length) tally.pricedOut += ev.choices.length - ok.length;
      E.resolveEvent(s, inst.uid, idx);
    }

    const inc = Math.max(1, E.grossRate(s, 'money')) * 60;
    for (const m of s.inbox.splice(0)) {
      if (m.kind === 'op' && m.win && !m.retaken) tally.lootMin.push((m.loot.money || 0) / inc);
      if (m.kind === 'raid' && m.win) tally.salvageMin.push(m.loot.money / inc);
      if (m.kind === 'raid' && m.assault) {
        if (m.win) tally.assaultsHeld++; else tally.assaultsBreached++;
      }
      if (m.kind === 'raid' && !m.assault && m.breaches) tally.raidBreaches++;
      if (m.kind === 'raid') tally.byClan[m.faction] = (tally.byClan[m.faction] || 0) + 1;
      if (m.fell) tally.sectorsLost++;
      if (m.kind === 'op' && m.win) mark('op:' + m.sector, t);
      if (m.kind === 'ending') mark('ending', t);
    }
    tally.dailies = s.daily.tasks.filter((o) => o.done).length;
    const lvl = E.level(s, 'core');
    if (snap['core' + lvl] === undefined) snap['core' + lvl] = Math.round(E.factors(s).defense);
    mark('core' + lvl, t);
    mark('rank' + s.rank, t);
  }
  const f = E.factors(s);
  return { marks, tally, stats: s.stats, factors: f, threat: E.threat(s, f), align: s.align, sectors: s.sectors.length - 1, levels: { ...s.levels }, snap };
}

const runs = [];
for (let r = 0; r < RUNS; r++) {
  runs.push(play(101 + r * 37));
}

const avg = (xs) => (xs.length ? xs.reduce((a, b) => a + b, 0) / xs.length : null);
function row(label, key) {
  const ts = runs.map((r) => r.marks[key]).filter((x) => x !== undefined);
  if (!ts.length) {
    console.log(`${label.padEnd(26)} not reached`);
    return;
  }
  const reach = ts.length === runs.length ? '' : `  (${ts.length}/${runs.length} runs)`;
  console.log(`${label.padEnd(26)} avg ${fmt(avg(ts))}  min ${fmt(Math.min(...ts))}  max ${fmt(Math.max(...ts))}${reach}`);
}

console.log(`DEADSWITCH balance: ${RUNS} runs x ${HOURS}h\n`);
console.log('AI Core');
for (let l = 2; l <= 12; l++) {
  if (runs.some((r) => r.marks['core' + l] !== undefined)) row(`  Core ${l}`, 'core' + l);
}
console.log('Capitals');
for (const x of SECTORS.filter((x) => x.boss)) row(`  ${x.name}`, 'op:' + x.id);
row('  Ending', 'ending');
console.log('Ranks');
for (let i = 1; i < RANKS.length; i++) {
  if (runs.some((r) => r.marks['rank' + i] !== undefined)) row(`  ${RANKS[i].title}`, 'rank' + i);
}

const mean = (fn) => avg(runs.map(fn));
const n = (x) => (x >= 1e6 ? (x / 1e6).toFixed(1) + 'M' : x >= 1e3 ? (x / 1e3).toFixed(1) + 'K' : Math.round(x));
console.log(`\nEnd of run (averages)`);
console.log(`  Power ${n(mean((r) => r.factors.power))}  Defense ${n(mean((r) => r.factors.defense))}  Experts ${n(mean((r) => r.factors.experts))}  Threat ${n(mean((r) => r.threat))}`);
console.log(`  Sectors held ${mean((r) => r.sectors).toFixed(1)}  Humanity ${mean((r) => r.align).toFixed(0)}`);
const share = (w, l) => (w + l ? `${Math.round((100 * w) / (w + l))}% held` : '-');
{
  // stats.raidsWon/raidsLost count every attack on us; assaults are split out below.
  const w = mean((r) => r.stats.raidsWon - r.tally.assaultsHeld);
  const l = mean((r) => r.stats.raidsLost - r.tally.assaultsBreached);
  console.log(`  Raids/sieges held ${w.toFixed(1)}  lost ${l.toFixed(1)}  (${share(w, l)})`);
}
console.log(`  Operations won ${mean((r) => r.stats.opsWon).toFixed(1)}  lost ${mean((r) => r.stats.opsLost).toFixed(1)}`);
console.log(`  Assaults held ${mean((r) => r.tally.assaultsHeld).toFixed(1)}  breached ${mean((r) => r.tally.assaultsBreached).toFixed(1)}  (${share(mean((r) => r.tally.assaultsHeld), mean((r) => r.tally.assaultsBreached))})  raids that cracked a wall ${mean((r) => r.tally.raidBreaches).toFixed(1)}  sectors lost ${mean((r) => r.tally.sectorsLost).toFixed(1)}`);
{
  // Who the attacks came from, as a share of all of them: one clan should not hold the whole war.
  const by = {};
  for (const r of runs) for (const [f, n] of Object.entries(r.tally.byClan)) by[f] = (by[f] || 0) + n;
  const all = Object.values(by).reduce((a, b) => a + b, 0) || 1;
  console.log(`  Attacks by clan ${Object.entries(by).sort((a, b) => b[1] - a[1]).map(([f, n]) => `${f} ${Math.round((100 * n) / all)}%`).join('  ')}`);
}
console.log(`  Capture loot ${mean((r) => avg(r.tally.lootMin) || 0).toFixed(1)} min of Scrip income, held-attack salvage ${mean((r) => avg(r.tally.salvageMin) || 0).toFixed(1)} min`);
console.log(`  Humanity from choices ${mean((r) => r.tally.align).toFixed(0)} over ${mean((r) => r.tally.picked).toFixed(0)} answers, ${mean((r) => r.tally.pricedOut).toFixed(0)} options unaffordable`);
console.log('  levels ' + JSON.stringify(runs[0].levels));
console.log('  defense at each Core ' + JSON.stringify(Object.fromEntries(Object.keys(runs[0].snap).map((k) => [k, Math.round(mean((r) => r.snap[k] || 0))]))));
console.log(`  Daily orders cleared ${mean((r) => r.tally.dailies).toFixed(1)} of the day's board`);
console.log(`  Events ${mean((r) => r.stats.events).toFixed(0)}  threats paid ${mean((r) => r.tally.tributes).toFixed(1)}  sieges taken ${mean((r) => r.tally.sieges).toFixed(1)}  grudges ${mean((r) => r.tally.grudges).toFixed(1)}  missed deadlines ${mean((r) => r.stats.expired || 0).toFixed(1)}`);
