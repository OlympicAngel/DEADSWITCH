// Greedy bot that plays the whole game headlessly and prints milestone times.
// Usage: node tools/balance-sim.mjs [hours] [seed]
import { BUILDINGS, ITEMS, RANKS, SECTORS } from '../js/data.js';
import * as E from '../js/engine.js';

const hours = Number(process.argv[2] || 24);
const s = E.newState(Number(process.argv[3] || 7));
const fmt = (t) => `${Math.floor(t / 3600)}h${String(Math.floor((t % 3600) / 60)).padStart(2, '0')}m`;
const sum = (c) => Object.values(c).reduce((a, b) => a + b, 0);
const seen = new Set();
const mark = (t, key, text) => {
  if (!seen.has(key)) {
    seen.add(key);
    console.log(fmt(t).padEnd(7), text);
  }
};

for (let t = 0; t < hours * 3600; t++) {
  E.step(s, 1);
  if (!s.build) {
    const ready = BUILDINGS.filter((b) => E.buildingStatus(s, b) === 'ready');
    const pick = ready.find((b) => b.id === 'core') || ready.sort((a, b) => sum(E.buildingCost(s, a)) - sum(E.buildingCost(s, b)))[0];
    if (pick) {
      E.startBuild(s, pick.id);
    }
  }
  // Lean on defense while the incoming raid looks dangerous.
  const scared = s.raid && E.raidChance(s) < 0.85;
  const items = ITEMS.filter((i) => E.itemUnlocked(s, i) && (!scared || i.tab === 'defenses' || i.tab === 'staff'))
    .sort((a, b) => sum(E.itemCost(s, a)) - sum(E.itemCost(s, b)));
  for (const i of items.slice(0, 3)) {
    const c = E.itemCost(s, i);
    if (E.canAfford(s, c) && (c.money || 0) < s.res.money * (scared ? 0.8 : 0.4)) {
      E.buyItem(s, i.id);
    }
  }
  if (!s.op) {
    const target = SECTORS.find((x) => E.sectorStatus(s, x) === 'target' && E.opChance(s, x) > 0.8 && E.canLaunch(s, x));
    if (target) {
      E.launchOp(s, target.id);
    }
  }
  for (const inst of s.events.slice()) {
    const ev = E.eventById(inst.id);
    const idx = ev.choices.findIndex((c) => E.canChoose(s, inst, c));
    E.resolveEvent(s, inst.uid, idx < 0 ? ev.def : idx);
  }
  for (const m of s.inbox.splice(0)) {
    if (m.kind === 'op' && m.win) mark(t, 'op' + m.sector, `captured ${m.sector}`);
    if (m.kind === 'raid') mark(t, 'raid' + (s.stats.raidsWon + s.stats.raidsLost), `raid ${m.win ? 'won ' : 'LOST'} str ${m.strength} vs def ${m.defense} (${Math.round(m.chance * 100)}%)`);
    if (m.kind === 'ending') mark(t, 'end', `ENDING ${m.key}`);
  }
  mark(t, 'core' + E.level(s, 'core'), `core ${E.level(s, 'core')}`);
  mark(t, 'rank' + s.rank, `rank ${RANKS[s.rank].title}`);
}
const f = E.factors(s);
console.log('end', fmt(hours * 3600), 'factors', JSON.stringify(f), 'align', s.align, 'stats', JSON.stringify(s.stats));
console.log('sectors', s.sectors.join(' '));
