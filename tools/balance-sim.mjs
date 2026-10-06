// Greedy bot that plays the economy headlessly and prints milestone times. Usage: node tools/balance-sim.mjs [hours]
import { BUILDINGS, ITEMS, RANKS } from '../js/data.js';
import * as E from '../js/engine.js';

const hours = Number(process.argv[2] || 24);
const s = E.newState();
const fmt = (t) => `${Math.floor(t / 3600)}h${String(Math.floor((t % 3600) / 60)).padStart(2, '0')}m`;
const sum = (c) => Object.values(c).reduce((a, b) => a + b, 0);
let lastCore = 1;
let lastRank = 0;
for (let t = 0; t < hours * 3600; t++) {
  E.step(s, 1);
  if (!s.build) {
    const ready = BUILDINGS.filter((b) => E.buildingStatus(s, b) === 'ready');
    const core = ready.find((b) => b.id === 'core');
    const pick = core || ready.sort((a, b) => sum(E.buildingCost(s, a)) - sum(E.buildingCost(s, b)))[0];
    if (pick) E.startBuild(s, pick.id);
  }
  // Spend at most half of scrip on items so buildings keep progressing.
  const items = ITEMS.filter((i) => E.itemUnlocked(s, i)).sort((a, b) => sum(E.itemCost(s, a)) - sum(E.itemCost(s, b)));
  for (const i of items.slice(0, 3)) {
    const c = E.itemCost(s, i);
    if (E.canAfford(s, c) && (c.money || 0) < s.res.money * 0.5) E.buyItem(s, i.id);
  }
  if (E.level(s, 'core') !== lastCore) { lastCore = E.level(s, 'core'); console.log(fmt(t), 'core', lastCore); }
  if (s.rank !== lastRank) { lastRank = s.rank; console.log(fmt(t), 'rank', RANKS[s.rank].title); }
}
const f = E.factors(s);
console.log('end', fmt(hours * 3600), JSON.stringify(s.levels), JSON.stringify(f), 'res', Object.fromEntries(Object.entries(s.res).map(([k, v]) => [k, Math.round(v)])));
console.log('caps', JSON.stringify(E.caps(s)));
