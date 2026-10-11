// Headless balance simulation: a bot plays several seeded runs and reports averaged milestone times.
// Usage: node tools/balance-sim.mjs [hours=24] [runs=5]
//
// The bot plays like a reasonable, active player with every current system in play:
// - builds: AI Core whenever the base-development gate allows it, otherwise the cheapest ready building
// - arsenal: steady spending; when an attack (raid, siege or vengeance) is inbound with a weak hold
//   chance it pours scrip into defense and troops
// - map: launches operations at >= 80% odds (fortified multi-route sectors included), never while an attack it is struggling to hold is inbound, since the troops march out with it
// - events: picks a random affordable choice, so outcomes average across all options over the runs;
//   threats are paid when affordable, otherwise refused (siege) or the cheapest give-up option
import { BUILDINGS, ITEMS, SECTORS, RANKS } from '../js/data.js';
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

// A converter is worth building when its output is the resource holding the next Core upgrade back.
function shortOf(s, b) {
  const [out] = Object.keys(b.produces);
  const need = E.buildingCost(s, BUILDINGS.find((x) => x.id === 'core'))[out] || 0;
  return s.res[out] < need * 0.5 && Object.keys(b.consumes).every((r) => s.res[r] > need);
}

const SAFE_ODDS = 0.92; // a player reinforces until an inbound attack looks survivable
const SHOP_EVERY = 10; // seconds of game time between shopping trips
const PANIC_BUYS = 80; // how many units one panic can add before the player gives up on it
const EXPERT_WORTH = 30; // one Expert lifts every rate, so it is worth far more than its point
const CORE_SOON = 180; // seconds of income: closer than this to the next Core and the player saves
const POP_WORTH = 12; // people are not free: price them against scrip (data/economy.js CONVERSION)

/** Value per scrip of one more of this unit. `want` is what the player is shopping for. */
function value(s, item, want) {
  const g = item.gives || {};
  // Experts are worth more than their one point: they lift everything the base makes.
  const good = want === 'defense' ? (g.defense || 0)
    : (g.power || 0) + (g.defense || 0) + (g.experts || 0) * EXPERT_WORTH;
  if (!good) return 0;
  const c = E.itemCost(s, item);
  if (!E.canAfford(s, c) || E.exceedsCap(s, c).length) return 0;
  return good / Math.max(1, (c.money || 0) + (c.energy || 0) + (c.pop || 0) * POP_WORTH);
}

/** The best buy on the shelf right now, or null when nothing is affordable. */
function bestBuy(s, want) {
  let best = null;
  let top = 0;
  for (const i of ITEMS) {
    if (!E.itemUnlocked(s, i) || !i.gives) continue;
    const v = value(s, i, want);
    if (v > top) {
      top = v;
      best = i;
    }
  }
  return best;
}

/** What to hold back for the next AI Core level. A player only saves when the Core is nearly in
 *  reach (CORE_SOON of income away); the rest of the time, sitting on scrip buys nothing. */
function coreReserve(s) {
  const core = BUILDINGS.find((b) => b.id === 'core');
  if (!['ready', 'storage', 'busy', 'gated'].includes(E.buildingStatus(s, core))) {
    return {};
  }
  const cost = E.buildingCost(s, core);
  const rates = E.netRates(s);
  const near = Object.entries(cost).every(([r, need]) => s.res[r] + (rates[r] || 0) * CORE_SOON >= need);
  return near ? cost : {};
}

// One shopping trip. Under a bad attack the player empties the account into Defense until the odds
// look survivable; otherwise they spend whatever is over the next Core level on the best value going.
function shop(s, atk) {
  // The wall first, and only as far as it has to go.
  for (let n = 0; atk && n < PANIC_BUYS && E.raidChance(s, atk) < SAFE_ODDS; n++) {
    const buy = bestBuy(s, 'defense');
    if (!buy) break;
    E.buyItem(s, buy.id);
  }
  // Then whatever is still over the next Core level goes on the best value on the shelf.
  const keep = coreReserve(s);
  for (let n = 0; n < 12; n++) {
    const buy = bestBuy(s, 'threat');
    if (!buy) break;
    const c = E.itemCost(s, buy);
    // Never spend the Core out of reach: that is the one purchase that opens everything else.
    if (Object.entries(keep).some(([r, need]) => s.res[r] - (c[r] || 0) < need)) break;
    E.buyItem(s, buy.id);
  }
}

function play(seed) {
  const s = E.newState(seed);
  s.name = 'bot';
  const roll = dice(seed * 7919);
  const marks = {};
  const mark = (key, t) => {
    if (marks[key] === undefined) marks[key] = t;
  };
  const tally = { align: 0, picked: 0, pricedOut: 0, sieges: 0, grudges: 0, tributes: 0, assaultsHeld: 0, assaultsBreached: 0, raidBreaches: 0, sectorsLost: 0, lootMin: [], salvageMin: [] };
  const ignored = new Set(); // threats the bot decided to take on the chin, counted once each

  for (let t = 0; t < HOURS * 3600; t++) {
    E.step(s, 1, false, true);

    if (!s.build) {
      // Converters only lose value now; a sensible player builds them only to fix a shortage.
      const ready = BUILDINGS.filter((b) => E.buildingStatus(s, b) === 'ready' && (b.kind !== 'converter' || shortOf(s, b)));
      const pick = ready.find((b) => b.id === 'core') || ready.sort((a, b) => sum(E.buildingCost(s, a)) - sum(E.buildingCost(s, b)))[0];
      if (pick) E.startBuild(s, pick.id);
    }

    const atk = E.nextAttack(s);
    const scared = atk && E.raidChance(s, atk) < SAFE_ODDS;
    // Shopping, the way a player shops: not every second, and not the cheapest thing on the shelf.
    if (t % SHOP_EVERY === 0) {
      shop(s, atk);
    }

    // Troops leave the wall with the operation, so the bot does not launch into an attack it is
    // already struggling to hold.
    if (!s.op && !scared) {
      const target = SECTORS.find((x) => E.sectorStatus(s, x) === 'target' && E.opChance(s, x) > 0.8 && E.canLaunch(s, x));
      if (target) E.launchOp(s, target.id);
    }

    for (const inst of s.events.slice()) {
      const ev = E.eventById(inst.id);
      const ok = ev.choices.map((c, i) => i).filter((i) => E.canChoose(s, inst, ev.choices[i]));
      let idx;
      if (ev.threat) {
        // A threat's attack is already on the board: paying is the only answer, and it is not always
        // affordable. Ignoring it is not a choice to make, it is one to live with.
        const pay = ev.choices.findIndex((c) => c.cost);
        if (!ok.includes(pay) || roll() < 0.4) {
          if (!ignored.has(inst.uid)) {
            ignored.add(inst.uid);
            tally.sieges++;
          }
          continue;
        }
        idx = pay;
        tally.tributes++;
      } else if (ok.length) {
        idx = ok[Math.floor(roll() * ok.length)];
      } else {
        continue; // nothing affordable: wait, and let the deadline decide
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
      if (m.fell) tally.sectorsLost++;
      if (m.kind === 'op' && m.win) mark('op:' + m.sector, t);
      if (m.kind === 'ending') mark('ending', t);
    }
    mark('core' + E.level(s, 'core'), t);
    mark('rank' + s.rank, t);
  }
  const f = E.factors(s);
  return { marks, tally, stats: s.stats, factors: f, threat: E.threat(s, f), align: s.align, sectors: s.sectors.length - 1, levels: { ...s.levels } };
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
console.log(`  Raids/sieges won ${mean((r) => r.stats.raidsWon).toFixed(1)}  lost ${mean((r) => r.stats.raidsLost).toFixed(1)}`);
console.log(`  Operations won ${mean((r) => r.stats.opsWon).toFixed(1)}  lost ${mean((r) => r.stats.opsLost).toFixed(1)}`);
console.log(`  Assaults held ${mean((r) => r.tally.assaultsHeld).toFixed(1)}  breached ${mean((r) => r.tally.assaultsBreached).toFixed(1)}  raids that cracked a wall ${mean((r) => r.tally.raidBreaches).toFixed(1)}  sectors lost ${mean((r) => r.tally.sectorsLost).toFixed(1)}`);
console.log(`  Capture loot ${mean((r) => avg(r.tally.lootMin) || 0).toFixed(1)} min of Scrip income, held-attack salvage ${mean((r) => avg(r.tally.salvageMin) || 0).toFixed(1)} min`);
console.log(`  Humanity from choices ${mean((r) => r.tally.align).toFixed(0)} over ${mean((r) => r.tally.picked).toFixed(0)} answers, ${mean((r) => r.tally.pricedOut).toFixed(0)} options unaffordable`);
console.log('  levels ' + JSON.stringify(runs[0].levels));
console.log(`  Events ${mean((r) => r.stats.events).toFixed(0)}  threats paid ${mean((r) => r.tally.tributes).toFixed(1)}  sieges taken ${mean((r) => r.tally.sieges).toFixed(1)}  grudges ${mean((r) => r.tally.grudges).toFixed(1)}  missed deadlines ${mean((r) => r.stats.expired || 0).toFixed(1)}`);
