// Generates the sector web: positions around The Nest, faction territories that interlock, and links
// that never cross. Prints a JSON layout { id: { x, y, links } } to paste into js/data/world.js.
// Usage: node tools/map-layout.mjs [seed]
const SEED = Number(process.argv[2] || 7);
let x = SEED >>> 0;
const rnd = () => { x = (x + 0x6d2b79f5) >>> 0; let t = Math.imul(x ^ (x >>> 15), x | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };

// Sectors by faction, nearest-to-home first; the capital is last in each list.
const FACTIONS = {
  scav: ['rust', 'tunnels', 'alley', 'sump', 'wrecks', 'stilts', 'drowned', 'glass', 'junkfort', 'chopshop', 'ferry', 'tollroad', 'rat', 'pyre', 'magpie', 'throne'],
  military: ['checkpoint', 'motorpool', 'medic', 'depot', 'kilo', 'minefield', 'comms', 'bunker', 'radar', 'silo', 'ashgrove'],
  cult: ['pilgrim', 'candle', 'ossuary', 'martyr', 'choir', 'cloister', 'beacon', 'shrine', 'reliquary', 'cathedral'],
  halcyon: ['gate', 'hub', 'annex', 'wing', 'dronefab', 'spire', 'cold', 'vault', 'prime'],
};
const CAPITAL = { scav: 'throne', military: 'ashgrove', cult: 'cathedral', halcyon: 'prime' };
// Each faction's heartland angle (degrees); territories overlap at their edges.
const HOME_ANGLE = { scav: null, military: 30, cult: 150, halcyon: 270 };

const MIN = 235; // minimum distance between sectors
const pts = [{ id: 'nest', f: null, x: 0, y: 0 }];
const far = (px, py) => pts.every((p) => Math.hypot(p.x - px, p.y - py) >= MIN);
function place(id, f, rMin, rMax, ang, spread) {
  for (let tries = 0; tries < 4000; tries++) {
    const r = rMin + rnd() * (rMax - rMin);
    const a = ang == null ? rnd() * 360 : ang + (rnd() - 0.5) * spread;
    const px = Math.cos((a * Math.PI) / 180) * r;
    const py = Math.sin((a * Math.PI) / 180) * r;
    if (far(px, py)) { pts.push({ id, f, x: px, y: py }); return; }
    if (tries % 400 === 399) { rMax += 40; spread += 10; }
  }
  throw new Error('no room for ' + id);
}
// Outposts sit outside their faction's heartland, so territories interlock.
const OUTPOSTS = { scav: ['pyre', 'magpie'], military: ['medic', 'comms'], cult: ['candle', 'martyr'], halcyon: ['hub', 'wing'] };
const outpost = (id) => Object.values(OUTPOSTS).some((l) => l.includes(id));
// Capitals are the cities: placed first, in the middle of their heartland, so the rest gathers round.
place('throne', 'scav', 720, 800, 210, 30);
place('ashgrove', 'military', 860, 960, HOME_ANGLE.military, 20);
place('cathedral', 'cult', 860, 960, HOME_ANGLE.cult, 20);
place('prime', 'halcyon', 900, 1000, HOME_ANGLE.halcyon, 20);
// Scavengers ring the Nest; the others push in from their heartlands; outposts land anywhere mid-range.
FACTIONS.scav.filter((id) => id !== 'throne' && !outpost(id)).forEach((id, i) => place(id, 'scav', 240 + i * 28, 300 + i * 36, null, 360));
for (const f of Object.keys(OUTPOSTS)) OUTPOSTS[f].forEach((id) => place(id, f, 480, 720, null, 360));
for (const f of ['military', 'cult', 'halcyon']) {
  const list = FACTIONS[f].filter((id) => id !== CAPITAL[f] && !outpost(id));
  list.forEach((id, i) => place(id, f, 560 + i * 60, 640 + i * 75, HOME_ANGLE[f], 80 + i * 4));
}


// Link budget per sector: mostly 2-3, a few 1 and 4-5; capitals 2, the Nest 4.
const cap = {};
for (const p of pts) {
  const r = rnd();
  cap[p.id] = r < 0.1 ? 1 : r < 0.47 ? 2 : r < 0.82 ? 3 : r < 0.95 ? 4 : 5;
}
cap.nest = 4;
for (const c of Object.values(CAPITAL)) cap[c] = 5;

const links = Object.fromEntries(pts.map((p) => [p.id, new Set()]));
const edges = [];
const cross = (a, b, c, d) => {
  const o = (p, q, r) => Math.sign((q.x - p.x) * (r.y - p.y) - (q.y - p.y) * (r.x - p.x));
  if ([a, b].includes(c) || [a, b].includes(d)) return false;
  return o(a, b, c) !== o(a, b, d) && o(c, d, a) !== o(c, d, b);
};
// Two links leaving one sector at almost the same angle would draw on top of each other.
const tooClose = (a, b, c, d) => {
  const shared = a === c || a === d ? a : b === c || b === d ? b : null;
  if (!shared) return false;
  const o1 = shared === a ? b : a;
  const o2 = shared === c ? d : c;
  const ang = Math.abs(Math.atan2(o1.y - shared.y, o1.x - shared.x) - Math.atan2(o2.y - shared.y, o2.x - shared.x));
  return Math.min(ang, 2 * Math.PI - ang) < (16 * Math.PI) / 180;
};
const ok = (a, b) => !edges.some(([c, d]) => cross(a, b, c, d) || tooClose(a, b, c, d));
const add = (a, b) => { links[a.id].add(b.id); links[b.id].add(a.id); edges.push([a, b]); };
const pairs = [];
for (let i = 0; i < pts.length; i++) for (let j = i + 1; j < pts.length; j++) {
  const d = Math.hypot(pts[i].x - pts[j].x, pts[i].y - pts[j].y);
  if (d < MIN * 2.1) pairs.push([d, pts[i], pts[j]]);
}
pairs.sort((a, b) => a[0] - b[0]);
// Cities first: each capital links to its 5 nearest reachable neighbours (they may exceed their own budget by one).
for (const c of Object.values(CAPITAL)) {
  const me = pts.find((p) => p.id === c);
  // Own faction first (a city sits in its own heartland), nearest first; anything within reach.
  const near = pts.filter((p) => p !== me && Math.hypot(p.x, p.y) > 420).map((p) => [Math.hypot(p.x - me.x, p.y - me.y), p])
    .filter(([d]) => d < MIN * 2.4)
    .sort((a, b) => (b[1].f === me.f) - (a[1].f === me.f) || a[0] - b[0]);
  for (const [, p] of near) {
    if (links[c].size >= 5) break;
    if (links[p.id].size <= cap[p.id] && !Object.values(CAPITAL).includes(p.id) && ok(me, p)) add(me, p);
  }
}
for (const [, a, b] of pairs) {
  if (links[a.id].size < cap[a.id] && links[b.id].size < cap[b.id] && ok(a, b)) add(a, b);
}
// Connect everything: join components with the shortest non-crossing link (budget may stretch to 5).
const comp = () => {
  const seen = new Set(['nest']); const q = ['nest'];
  while (q.length) for (const n of links[q.shift()]) if (!seen.has(n)) { seen.add(n); q.push(n); }
  return seen;
};
for (let guard = 0; guard < 200; guard++) {
  const reach = comp();
  if (reach.size === pts.length) break;
  const best = pairs.find(([, a, b]) => reach.has(a.id) !== reach.has(b.id) && links[a.id].size < 5 && links[b.id].size < 5 && ok(a, b));
  if (!best) throw new Error('cannot connect');
  add(best[1], best[2]);
}
// Every chapter must be reachable through earlier-or-same chapters only.
const CH = { scav: 1, military: 2, cult: 3, halcyon: 4 };
const ch = (id) => (id === 'nest' ? 0 : CH[pts.find((p) => p.id === id).f]);
for (let c = 1; c <= 4; c++) {
  for (let guard = 0; guard < 100; guard++) {
    const seen = new Set(['nest']); const q = ['nest'];
    while (q.length) for (const n of links[q.shift()]) if (!seen.has(n) && ch(n) <= c) { seen.add(n); q.push(n); }
    const missing = pts.filter((p) => ch(p.id) === c && !seen.has(p.id));
    if (!missing.length) break;
    const best = pairs.find(([, a, b]) => (seen.has(a.id) && missing.includes(b)) || (seen.has(b.id) && missing.includes(a)));
    if (!best || !ok(best[1], best[2])) throw new Error('chapter ' + c + ' unreachable: ' + missing.map((m) => m.id));
    add(best[1], best[2]);
  }
}
// The first directive points at Rust Market: it must border the Nest. Swap it with the nearest Scavenger there.
if (!links.nest.has('rust')) {
  const nb = [...links.nest].filter((id) => FACTIONS.scav.includes(id)).sort((a, b) => {
    const pa = pts.find((p) => p.id === a); const pb = pts.find((p) => p.id === b);
    return Math.hypot(pa.x, pa.y) - Math.hypot(pb.x, pb.y);
  })[0];
  const A = pts.find((p) => p.id === 'rust'); const B = pts.find((p) => p.id === nb);
  [A.id, B.id] = [B.id, A.id];
  const la = links.rust; const lb = links[nb];
  links.rust = lb; links[nb] = la;
  for (const s of Object.values(links)) {
    const hasA = s.has('rust'); const hasB = s.has(nb);
    s.delete('rust'); s.delete(nb);
    if (hasA) s.add(nb);
    if (hasB) s.add('rust');
  }
}
const out = {};
for (const p of pts) out[p.id] = { x: Math.round(p.x), y: Math.round(p.y), links: [...links[p.id]] };
const deg = pts.map((p) => links[p.id].size);
console.error('capitals', Object.values(CAPITAL).map((c) => c + ':' + links[c].size + '/' + [...links[c]].filter((l) => pts.find((p) => p.id === l).f === pts.find((p) => p.id === c).f).length).join(' '), 'sectors', pts.length, 'links', edges.length, 'degree histogram', [1, 2, 3, 4, 5, 6].map((d) => `${d}:${deg.filter((x) => x === d).length}`).join(' '));
console.log(JSON.stringify(out));
