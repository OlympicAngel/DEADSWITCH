// Headless balance simulation: a bot plays several seeded runs and reports averaged milestone times.
// Usage: node tools/balance-sim.mjs [hours=24] [runs=5]
//
// The bot plays like a reasonable, active player with every current system in play:
// - builds: AI Core whenever the base-development gate allows it, otherwise the cheapest ready building
// - arsenal: steady spending; when an attack (raid, siege or vengeance) is inbound with a weak hold
//   chance it pours scrip into defense and troops
// - map: launches operations at >= 80% odds (fortified multi-route sectors included)
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

function play(seed) {
  const s = E.newState(seed);
  s.name = 'bot';
  const roll = dice(seed * 7919);
  const marks = {};
  const mark = (key, t) => {
    if (marks[key] === undefined) marks[key] = t;
  };
  const tally = { sieges: 0, grudges: 0, tributes: 0, assaultsHeld: 0, assaultsBreached: 0, sectorsLost: 0, lootMin: [], salvageMin: [] };

  for (let t = 0; t < HOURS * 3600; t++) {
    E.step(s, 1, false, true);

    if (!s.build) {
      // Converters only lose value now; a sensible player builds them only to fix a shortage.
      const ready = BUILDINGS.filter((b) => E.buildingStatus(s, b) === 'ready' && (b.kind !== 'converter' || shortOf(s, b)));
      const pick = ready.find((b) => b.id === 'core') || ready.sort((a, b) => sum(E.buildingCost(s, a)) - sum(E.buildingCost(s, b)))[0];
      if (pick) E.startBuild(s, pick.id);
    }

    const atk = E.nextAttack(s);
    const scared = atk && E.raidChance(s, atk) < 0.85;
    const items = ITEMS.filter((i) => E.itemUnlocked(s, i) && (!scared || i.tab === 'defenses' || i.tab === 'staff'))
      .sort((a, b) => sum(E.itemCost(s, a)) - sum(E.itemCost(s, b)));
    for (const i of items.slice(0, 3)) {
      const c = E.itemCost(s, i);
      if (E.canAfford(s, c) && (c.money || 0) < s.res.money * (scared ? 0.8 : 0.4)) E.buyItem(s, i.id);
    }

    if (!s.op) {
      const target = SECTORS.find((x) => E.sectorStatus(s, x) === 'target' && E.opChance(s, x) > 0.8 && E.canLaunch(s, x));
      if (target) E.launchOp(s, target.id);
    }

    for (const inst of s.events.slice()) {
      const ev = E.eventById(inst.id);
      const ok = ev.choices.map((c, i) => i).filter((i) => E.canChoose(s, inst, ev.choices[i]));
      let idx;
      if (ev.threat) {
        const pay = ev.choices.findIndex((c) => c.cost);
        const fight = ev.choices.findIndex((c) => c.siege);
        idx = ok.includes(pay) ? pay : roll() < 0.5 ? fight : ok.find((i) => i !== fight) ?? fight;
        if (idx === pay) tally.tributes++;
        if (idx === fight) tally.sieges++;
      } else if (ok.length) {
        idx = ok[Math.floor(roll() * ok.length)];
      } else {
        continue; // nothing affordable: wait, and let the deadline decide
      }
      if (ev.choices[idx].grudge) tally.grudges++;
      E.resolveEvent(s, inst.uid, idx);
    }

    const inc = Math.max(1, E.grossRate(s, 'money')) * 60;
    for (const m of s.inbox.splice(0)) {
      if (m.kind === 'op' && m.win && !m.retaken) tally.lootMin.push((m.loot.money || 0) / inc);
      if (m.kind === 'raid' && m.win) tally.salvageMin.push(m.loot.money / inc);
      if (m.kind === 'raid' && m.assault) {
        if (m.win) tally.assaultsHeld++; else tally.assaultsBreached++;
        if (m.fell) tally.sectorsLost++;
      }
      if (m.kind === 'op' && m.win) mark('op:' + m.sector, t);
      if (m.kind === 'ending') mark('ending', t);
    }
    mark('core' + E.level(s, 'core'), t);
    mark('rank' + s.rank, t);
  }
  const f = E.factors(s);
  return { marks, tally, stats: s.stats, factors: f, threat: E.threat(s, f), align: s.align, sectors: s.sectors.length - 1, terr: E.territory(s, 'money') / Math.max(1, E.grossRate(s, 'money')) };
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
console.log(`  Assaults held ${mean((r) => r.tally.assaultsHeld).toFixed(1)}  breached ${mean((r) => r.tally.assaultsBreached).toFixed(1)}  sectors lost ${mean((r) => r.tally.sectorsLost).toFixed(1)}`);
console.log(`  Capture loot ${mean((r) => avg(r.tally.lootMin) || 0).toFixed(1)} min of Scrip income, held-attack salvage ${mean((r) => avg(r.tally.salvageMin) || 0).toFixed(1)} min; territory ${(100 * mean((r) => r.terr)).toFixed(0)}% of Scrip income at the end`);
console.log(`  Events ${mean((r) => r.stats.events).toFixed(0)}  threats paid ${mean((r) => r.tally.tributes).toFixed(1)}  sieges taken ${mean((r) => r.tally.sieges).toFixed(1)}  grudges ${mean((r) => r.tally.grudges).toFixed(1)}  missed deadlines ${mean((r) => r.stats.expired || 0).toFixed(1)}`);
