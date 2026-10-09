// What the debugger shows for the thing the player is looking at, one entry per focus kind.
// Rows are [label, read(s)] so the panel can patch their values every frame instead of rebuilding.
// Actions are [label, run(game)] and may change state freely.
import * as E from '../../engine.js';
import { FACTIONS, BY_ID, ITEM_BY_ID, MAP, NODES, SECTORS } from '../../data.js';

const n2 = (v) => (Number.isFinite(v) ? v.toFixed(2) : '-');
const pc = (v) => `${(v * 100).toFixed(1)}%`;
const secs = (v) => (v == null ? '-' : `${v.toFixed(1)}s`);

// ---------- per kind ----------

function sector(s, id) {
  const sec = E.sectorById(id);
  if (!sec) return null;
  const mine = () => s.sectors.includes(id);
  const node = () => s.nodes[id] || {};
  return {
    title: `${sec.name} · ${sec.faction ? FACTIONS[sec.faction].short : 'yours'}${sec.boss ? ' · capital' : ''}`,
    rows: [
      ['id / chapter', () => `${sec.id} / ${sec.chapter}`],
      ['status', () => E.sectorStatus(s, sec) + (mine() ? ' (held)' : '')],
      ['base defense', () => sec.defense],
      ['live defense', () => E.sectorDefense(s, sec)],
      ['strength ×', () => n2(E.nodeStrength(s, id))],
      ['clan support', () => `${n2(E.flank(s, sec).bonus)} (${E.flank(s, sec).held}/${E.flank(s, sec).approaches} taken)`],
      ['aggression', () => `${n2(E.aggression(s, id))} (stored ${n2(node().a || 0)})`],
      ['looks taken', () => node().seen || 0],
      ['breaches', () => `${E.breaches(s, id)} / ${NODES.breachesToFall}`],
      ['assault strength', () => E.assaultStrength(s, id)],
      ['op odds / cost', () => `${pc(E.opChance(s, sec))} · ${JSON.stringify(E.opCost(sec))}`],
      ['op time', () => secs(E.opTime(sec))],
      ['links', () => sec.links.join(' ')],
    ],
    json: () => ({ sector: sec, node: s.nodes[id] || null }),
    actions: [
      ['take', (g) => { if (!g.state.sectors.includes(id)) g.state.sectors.push(id); }],
      ['release', (g) => { g.state.sectors = g.state.sectors.filter((x) => x !== id); }],
      ['win op now', (g) => { E.launchOp(g.state, id); if (g.state.op) g.state.op.remaining = 0.001; }],
      ['str +25%', (g) => E.shiftStrength(g.state, id, 0.25)],
      ['str −25%', (g) => E.shiftStrength(g.state, id, -0.25)],
      ['aggr +0.5', (g) => E.shiftAggression(g.state, id, 0.5)],
      ['aggr −0.5', (g) => E.shiftAggression(g.state, id, -0.5)],
      ['clear breaches', (g) => { if (g.state.nodes[id]) g.state.nodes[id].marks = 0; }],
      ['assault me', (g) => {
        const target = E.sectorById(id).links.find((l) => g.state.sectors.includes(l) && l !== MAP.home);
        if (target) E.spawnAssault(g.state, { from: id, target });
      }],
    ],
  };
}

function building(s, id) {
  const b = BY_ID[id];
  if (!b) return null;
  return {
    title: `${b.name} · ${b.kind}`,
    rows: [
      ['level / cap', () => `${E.level(s, id)} / ${E.maxLevel(s, b)}`],
      ['status', () => E.buildingStatus(s, b)],
      ['next cost', () => JSON.stringify(E.buildingCost(s, b))],
      ['build time', () => secs(E.buildTime(s, b))],
      ['produces', () => JSON.stringify(b.produces || {})],
      ['consumes', () => JSON.stringify(b.consumes || {})],
      ['paused', () => !!s.paused[id]],
    ],
    json: () => b,
    actions: [
      ['+1 level', (g) => { g.state.levels[id] = (g.state.levels[id] || 0) + 1; }],
      ['−1 level', (g) => { g.state.levels[id] = Math.max(0, (g.state.levels[id] || 0) - 1); }],
      ['+10 levels', (g) => { g.state.levels[id] = (g.state.levels[id] || 0) + 10; }],
      ['finish build', (g) => { if (g.state.build) g.state.build.remaining = 0.001; }],
      ['toggle pause', (g) => E.togglePause(g.state, id)],
    ],
  };
}

function item(s, id) {
  const it = ITEM_BY_ID[id];
  if (!it) return null;
  return {
    title: `${it.name} · ${it.tab}`,
    rows: [
      ['owned', () => E.owned(s, id)],
      ['unlocked', () => E.itemUnlocked(s, it)],
      ['next cost', () => JSON.stringify(E.itemCost(s, it))],
      ['max affordable', () => E.maxAffordable(s, it)],
      ['gives', () => JSON.stringify(it.gives || {})],
      ['durability', () => it.durability || 0],
    ],
    json: () => it,
    actions: [
      ['+1', (g) => { g.state.items[id] = (g.state.items[id] || 0) + 1; }],
      ['+25', (g) => { g.state.items[id] = (g.state.items[id] || 0) + 25; }],
      ['−1', (g) => { g.state.items[id] = Math.max(0, (g.state.items[id] || 0) - 1); }],
      ['zero', (g) => { g.state.items[id] = 0; }],
    ],
  };
}

function order(s, uid) {
  const inst = () => s.events.find((x) => x.uid === uid);
  const ev = () => (inst() ? E.eventById(inst().id) : null);
  if (!inst()) return null;
  // The order can be answered or expire while the panel is open, so every row re-checks it is still there.
  const live = (read) => () => (inst() && ev() ? read(inst(), ev()) : 'gone');
  return {
    title: `${E.fillText(inst(), ev().title)} · ${inst().id}`,
    rows: [
      ['deadline', live((i) => `${secs(i.left)} of ${secs(i.total)}`)],
      ['default choice', live((i, e) => e.def)],
      ['urgent / weight', live((i, e) => `${!!e.urgent} / ${e.weight || (e.threat ? 'threat' : 1)}`)],
      ['params', live((i) => JSON.stringify(i.params))],
      ['choice A', live((i, e) => JSON.stringify(E.choiceOutcome(s, i, e.choices[0])))],
      ['choice B', live((i, e) => JSON.stringify(E.choiceOutcome(s, i, e.choices[1])))],
      ['hidden aggr', live((i, e) => e.choices.map((c) => JSON.stringify(c.aggr || {})).join(' / '))],
    ],
    json: () => ({ inst: inst(), event: ev() }),
    actions: [
      ['take A', (g) => E.resolveEvent(g.state, uid, 0)],
      ['take B', (g) => E.resolveEvent(g.state, uid, 1)],
      ['expire now', (g) => { const i = g.state.events.find((x) => x.uid === uid); if (i) i.left = 0.001; }],
      ['drop', (g) => { g.state.events = g.state.events.filter((x) => x.uid !== uid); }],
    ],
  };
}

// Raids, sieges and assaults all resolve through the same machinery.
function attack(s, key) {
  const find = () => E.attacks(s).find((a) => attackKey(a) === key) || E.nextAttack(s);
  const a = find();
  if (!a) return null;
  return {
    title: E.attackName(a),
    rows: [
      ['kind', () => (find().assault ? 'assault' : find().siege ? 'siege' : 'raid')],
      ['faction', () => find().faction],
      ['strength', () => Math.round(find().strength)],
      ['lands in', () => secs(find().remaining)],
      ['hold chance', () => pc(E.raidChance(s, find()))],
      ['your defense', () => Math.round(E.factors(s).defense)],
      ['from → target', () => (find().from ? `${find().from} → ${find().target}` : '-')],
      ['vengeance', () => !!find().vengeance],
    ],
    json: () => find(),
    actions: [
      ['land now', (g) => { const x = find(); if (x) x.remaining = 0.001; }],
      ['×2 strength', (g) => { const x = find(); if (x) x.strength *= 2; }],
      ['÷2 strength', (g) => { const x = find(); if (x) x.strength /= 2; }],
      ['cancel', (g) => {
        const x = find();
        if (!x) return;
        if (x.assault) g.state.assault = null;
        else if (x.siege) g.state.sieges = g.state.sieges.filter((y) => y !== x);
        else g.state.raid = null;
      }],
    ],
  };
}

export const attackKey = (a) => (a.assault ? `assault:${a.from}` : a.siege ? `siege:${a.faction}` : `raid:${a.faction}`);

// The screen itself: what the player is on, and the timers that decide what interrupts them.
function screen(s, id) {
  return {
    title: `screen ${id}`,
    rows: [
      ['play time', () => secs(s.playTime)],
      ['core / chapter', () => `${E.level(s, 'core')} / ${s.chapter}`],
      ['power / defense', () => `${Math.round(E.factors(s).power)} / ${Math.round(E.factors(s).defense)}`],
      ['threat / rank', () => `${Math.round(E.threat(s))} · ${E.rankIndex(E.threat(s))}`],
      ['next order in', () => secs(s.eventTimer)],
      ['next raid in', () => secs(s.raidTimer)],
      ['next assault in', () => secs(s.assaultTimer)],
      ['pending orders', () => s.events.length],
      ['net income /s', () => Object.entries(E.netRates(s)).map(([r, v]) => `${r} ${v.toFixed(2)}`).join(' ')],
      ['caps', () => JSON.stringify(E.caps(s))],
      ['sectors held', () => `${s.sectors.length - 1} · align ${Math.round(s.align)}`],
      ['event drain', () => JSON.stringify(E.eventDrain(s))],
      ['directive', () => (E.currentDirective(s) ? `${s.directive}: ${E.currentDirective(s).text}` : 'all done')],
      ['events seen', () => s.recentEvents.join(' ') || '-'],
    ],
    json: () => ({ screen: id }),
    actions: [],
  };
}

export const INSPECTORS = { sector, building, item, order, attack, screen };

/** The inspector for a focus, falling back to the screen one. */
export function inspect(s, focus) {
  const make = INSPECTORS[focus.kind] || INSPECTORS.screen;
  return make(s, focus.id, focus.data) || INSPECTORS.screen(s, focus.id);
}

// Rows the WORLD tab shows for every sector: the hidden numbers first.
export function worldRows(s) {
  return SECTORS.map((x) => ({
    id: x.id,
    name: x.name,
    mine: s.sectors.includes(x.id),
    faction: x.faction || '-',
    def: x.faction && !s.sectors.includes(x.id) ? E.sectorDefense(s, x) : 0,
    m: E.nodeStrength(s, x.id),
    aggr: E.aggression(s, x.id),
    stored: (s.nodes[x.id] && s.nodes[x.id].a) || 0,
    seen: (s.nodes[x.id] && s.nodes[x.id].seen) || 0,
    marks: E.breaches(s, x.id),
    weight: x.faction && !s.sectors.includes(x.id) ? 1 + E.aggression(s, x.id) : 0,
  }));
}
