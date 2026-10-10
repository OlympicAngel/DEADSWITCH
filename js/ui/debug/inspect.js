// What the debugger shows for the thing the player is looking at, one entry per focus kind.
// Rows are [label, read(s)] so the panel can patch their values every frame instead of rebuilding.
// Actions are [label, run(game)] and may change state freely.
import * as E from '../../engine.js';
import { FACTIONS, BY_ID, ITEM_BY_ID, MAP, NODES, CLANS, LESSONS, SECTORS, TUTORIAL, CORE_MEMORIES } from '../../data.js';
import { ambientMood, moodOf } from '../ambient.js';
import { volume } from '../audio.js';
import { voiceName, say as speak } from '../voice.js';
import { awakeHeld } from '../../host/awake.js';
import { storageUse } from '../../host/store.js';

// The storage figures come from a promise, so they are read once and shown from here.
const store = { kept: false, used: 0, quota: 0 };
storageUse().then((r) => Object.assign(store, r));

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
      ['clan stance', () => (sec.faction ? E.stanceList(s, sec.faction).map((x) => x.id).join(' ') || 'none' : '-')],
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
      ['choices', live((i, e) => e.choices.map((c) => JSON.stringify(E.choiceOutcome(s, i, c))).join('\n'))],
      ['hidden aggr', live((i, e) => e.choices.map((c) => JSON.stringify(c.aggr || {})).join(' / '))],
    ],
    json: () => ({ inst: inst(), event: ev() }),
    actions: [
      ['take A', (g) => E.resolveEvent(g.state, uid, 0)],
      ['take B', (g) => E.resolveEvent(g.state, uid, 1)],
      ['siege now', (g) => { const x = g.state.sieges.find((y) => y.uid === uid); if (x) x.remaining = 0.001; }],
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
      ['overrun', () => `${E.overrunRisk(s, find())} (x${NODES.overrunRatio} defense)`],
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

// A clan's profile: its four traits, the matrix cells they land in, and what that multiplies.
function clanOf(s, faction) {
  const fac = FACTIONS[faction];
  if (!fac) return null;
  const c = () => E.clan(s, faction);
  const prof = () => E.clanProfile(s, faction);
  const ctx = () => E.clanContext(s, faction);
  const own = SECTORS.filter((x) => x.faction === faction);
  // Forces a stance: every trait it names is moved into range, and the map facts it needs are granted.
  const force = (st) => (g) => {
    const t = E.clan(g.state, faction);
    for (const [k, [lo, hi = 1]] of Object.entries(st.when)) {
      if (t[k] !== undefined) t[k] = Math.min(hi, Math.max(lo, lo === 0 ? hi - 0.02 : lo + 0.02));
      if (k === 'capital' && lo >= 1) {
        const cap = own.find((x) => x.boss);
        if (cap && !g.state.sectors.includes(cap.id)) g.state.sectors.push(cap.id);
      }
      if (k === 'held') {
        for (const x of own.slice(0, Math.ceil(lo * own.length) + 1)) {
          if (!g.state.sectors.includes(x.id)) g.state.sectors.push(x.id);
        }
      }
    }
  };
  return {
    title: `${fac.name} · clan profile`,
    rows: [
      ['fury / fear', () => `${n2(c().fury)} / ${n2(c().fear)}`],
      ['order / greed', () => `${n2(c().order)} / ${n2(c().greed)}`],
      ['baseline mood', () => JSON.stringify(fac.mood)],
      ['held / capital', () => `${pc(ctx().held)} / ${ctx().capital}`],
      ['awake / raiding', () => `${E.clanAwake(s, faction)} / ${E.activeRaiders(s).includes(faction)}`],
      ['stances', () => prof().stances.map((x) => x.id).join(' ') || 'none'],
      ['weight / retake', () => `${n2(prof().weight)} / ${n2(prof().retake)}`],
      ['strength / tempo', () => `${n2(prof().strength)} / ${n2(prof().tempo)}`],
      ['growth / support', () => `${n2(prof().growth)} / ${n2(prof().support)}`],
      ['raid / attrition', () => `${n2(prof().raid)} / ${n2(prof().attrition)}`],
      ['plunder', () => prof().plunder],
      ['sectors left', () => `${own.filter((x) => !s.sectors.includes(x.id)).length} / ${own.length}`],
    ],
    json: () => ({ clan: s.clans[faction], profile: E.clanProfile(s, faction) }),
    actions: [
      ['calm', (g) => Object.assign(E.clan(g.state, faction), fac.mood)],
      ...CLANS.stances.map((st) => [st.id, force(st)]),
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
      ['clan stances', () => Object.keys(FACTIONS).map((f) => `${f}:${E.stanceList(s, f).length}`).join(' ')],
      ['lesson due', () => (E.lessonDue(s) ? E.lessonDue(s).id : '-') + ` (taught ${s.taught.length})`],
      ['scripted', () => `${TUTORIAL.target} def ${E.sectorDefense(s, E.sectorById(TUTORIAL.target))} · retake ${['armed', 'inbound', 'done'][s.scripted.retake || 0]}`],
      ['frontier leaks', () => Object.keys(FACTIONS).map((f) => `${f}:${E.earlyTargets(s, f).filter((id) => E.sectorOpen(s, E.sectorById(id))).join('/') || '-'}`).join(' ')],
      ['music mood', () => `${moodOf(s)} (bed ${ambientMood()}) · sfx ${Math.round(volume('sfx') * 100)}% music ${Math.round(volume('music') * 100)}% voice ${Math.round(volume('voice') * 100)}%`],
      ['speaks as', () => voiceName()],
      ['screen awake', () => (awakeHeld() ? 'held' : 'free')],
      ['storage', () => (store.kept ? 'persisted' : 'evictable') + ` · ${(store.used / 1048576).toFixed(1)}MB of ${(store.quota / 1048576).toFixed(0)}MB`],
      ['inbox', () => s.inbox.map((x) => x.kind).join(' ') || 'empty'],
      ['day seed / rolls', () => `${E.daySeed(s)} · ${Object.entries(s.rolls).map(([k, n]) => `${k}:${n}`).join(' ') || 'none'}`],
      ['next raid / assault roll', () => `${E.peek(s, 'raid').toFixed(3)} / ${E.peek(s, 'assault').toFixed(3)}`],
    ],
    json: () => ({ screen: id }),
    actions: [
      ['forget lessons', (g) => { g.state.taught = []; }],
      ['teach nothing', (g) => { g.state.taught = LESSONS.map((l) => l.id); }],
      ['run scripted retake', (g) => { g.state.scripted.retake = 0; g.state.assault = null; E.scriptedRetake(g.state); }],
      ['cut: core', (g) => g.state.inbox.push({ kind: 'core', level: E.level(g.state, 'core'), text: CORE_MEMORIES[Math.max(2, E.level(g.state, 'core'))] || 'A fragment with nothing in it.' })],
      ['cut: chapter', (g) => g.state.inbox.push({ kind: 'chapter', id: Math.min(4, g.state.chapter + 1) })],
      ['cut: wipe', (g) => g.state.inbox.push({ kind: 'wipe', faction: 'scav' })],
      ['cut: fragment', (g) => g.state.inbox.push({ kind: 'memory', sector: 'rust' })],
      ['say a line', () => speak('All stations. This is the Nest. Hold what you have.')],
      ['skip scripted retake', (g) => { g.state.scripted.retake = 2; }],
    ],
  };
}

export const INSPECTORS = { sector, building, item, order, attack, screen, clan: clanOf };

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
