import test from 'node:test';
import assert from 'node:assert/strict';
import * as E from '../js/engine.js';
import { ITEM_BY_ID, BY_ID, EVENTS, BALANCE, NODES, AGGR, CLANS, FACTIONS, SECTORS } from '../js/data.js';

test('offline catch-up lands builds mid-stretch and matches live play at offline rates', () => {
  const live = E.newState();
  const away = E.newState();
  live.levels.daemon = 1;
  away.levels.daemon = 1;
  E.startBuild(live, 'scrapyard');
  E.startBuild(away, 'scrapyard');
  const { graceSeconds } = BALANCE.offline;
  const eff = E.offlineLimits(away).efficiency;
  for (let i = 0; i < 6000; i++) {
    E.step(live, 0.1, true, false, i < graceSeconds * 10 ? 1 : eff);
  }
  E.catchUp(away, 600);
  assert.equal(away.levels.scrapyard, 2);
  assert.ok(Math.abs(live.res.money - away.res.money) / live.res.money < 0.01);
});

test('converters never drive a resource below zero or above its cap', () => {
  const s = E.newState();
  Object.assign(s.levels, { core: 3, generator: 10, fabricator: 10, exchange: 5 });
  s.res = { money: 5, energy: 3, pop: 1 };
  E.catchUp(s, 3600);
  const caps = E.caps(s);
  for (const r of ['money', 'energy', 'pop']) {
    assert.ok(s.res[r] >= 0 && s.res[r] <= caps[r], r);
  }
});

test('bulk price equals buying one at a time, and max is affordable', () => {
  const a = E.newState();
  a.levels.barracks = 1;
  a.res.money = 1e6;
  a.res.pop = 1e6;
  const bulk = E.itemCost(a, ITEM_BY_ID.militia, 10).money;
  let single = 0;
  for (let i = 0; i < 10; i++) {
    single += E.itemCost(a, ITEM_BY_ID.militia, 1).money;
    E.buyItem(a, 'militia', 1);
  }
  assert.ok(Math.abs(bulk - single) <= 10);
  const n = E.maxAffordable(a, ITEM_BY_ID.militia);
  assert.ok(E.buyItem(a, 'militia', n));
});

test('old saves missing new fields still load', () => {
  const s = E.migrate({ res: { money: 5 }, levels: { core: 2 } });
  assert.equal(s.levels.core, 2);
  assert.equal(s.res.energy, 0 + E.newState().res.energy);
  assert.deepEqual(s.items, {});
});

test('being away never brings more than one raid', () => {
  const s = E.newState(3);
  Object.assign(s.levels, { core: 4, daemon: 1 });
  E.catchUp(s, 24 * 3600);
  assert.equal(s.stats.raidsWon + s.stats.raidsLost, 1);
  assert.ok(s.raid, 'the next raid is scheduled and waits for the player');
});

test('same seed and inputs give the same war outcomes', () => {
  const run = () => {
    const s = E.newState(42);
    Object.assign(s.levels, { core: 3, armory: 1 });
    s.items.rifles = 6;
    s.res.money = 1e5;
    s.res.energy = 100;
    E.launchOp(s, 'rust');
    E.catchUp(s, 3 * 3600);
    return JSON.stringify([s.sectors, s.stats, s.rng]);
  };
  assert.equal(run(), run());
});

test('a lost raid demands orders, and silence applies the default choice', () => {
  const s = E.newState(5);
  Object.assign(s.levels, { core: 2, battery: 2, generator: 2, daemon: 1 });
  s.raidsStarted = true;
  s.raid = { faction: 'scav', strength: 1e6, remaining: 1, total: 1 };
  E.step(s, 1);
  assert.equal(s.stats.raidsLost, 1);
  assert.ok(s.events.length >= 1 && E.eventById(s.events[0].id).aftermath);
  const inst = s.events[0];
  E.catchUp(s, inst.left + 1);
  assert.ok(!s.events.some((x) => x.uid === inst.uid));
  assert.equal(s.stats.expired, 1);
});

test('the AI Core only upgrades once the base is developed', () => {
  const s = E.newState(9);
  s.res = { money: 1e6, energy: 1e5, pop: 1e4 };
  Object.assign(s.levels, { battery: 3, habitat: 3 });
  assert.equal(E.buildingStatus(s, BY_ID.core), 'gated');
  for (const b of ['scrapyard', 'solar', 'shelter', 'barracks']) {
    s.levels[b] = 4;
  }
  assert.equal(E.buildingStatus(s, BY_ID.core), 'ready');
});

test('sectors are fortified by linked sectors of their own clan, until you take them', () => {
  const s = E.newState(2);
  const sec = E.sectorById('ashgrove');
  const clan = sec.links.filter((l) => E.sectorById(l).faction === sec.faction);
  assert.ok(clan.length >= 2);
  assert.equal(E.flank(s, sec).bonus, 0.5);
  s.sectors.push(clan[0]);
  assert.ok(Math.abs(E.flank(s, sec).bonus - 0.5 * (clan.length - 1) / clan.length) < 1e-9);
  s.sectors.push(...clan);
  assert.equal(E.flank(s, sec).bonus, 0);
});

test('a threat schedules its attack the moment it arrives; paying is what calls it off', () => {
  const s = E.newState(6);
  Object.assign(s.levels, { core: 2, works: 1, scrapyard: 4, solar: 4 });
  s.items.barricades = 5;
  s.res = { money: 1e6, energy: 1e5, pop: 500 };
  s.raidsStarted = true;
  const ev = E.eventById('ultimatum');
  // Firing the order puts the attack on the board straight away, at the deadline.
  s.eventTimer = 0;
  s.recentEvents = EVENTS.filter((e) => e.id !== 'ultimatum' && !e.aftermath && !e.urgent).map((e) => e.id);
  while (!s.events.some((x) => x.id === 'ultimatum')) {
    s.eventTimer = 0;
    E.step(s, 0.01, false, true);
  }
  const inst = s.events.find((x) => x.id === 'ultimatum');
  const siege = s.sieges.find((x) => x.uid === inst.uid);
  assert.ok(siege, 'the attack is visible before anything is decided');
  assert.ok(Math.abs(siege.remaining - ev.deadline) < 1);
  // Paying turns it back; nothing else in the queue is touched.
  assert.ok(E.resolveEvent(s, inst.uid, 0));
  assert.equal(s.sieges.length, 0);
});

test('a threat nobody answers simply arrives', () => {
  const s = E.newState(7);
  Object.assign(s.levels, { core: 2, works: 1 });
  s.items.barricades = 2;
  s.events = [{ uid: 9, id: 'ultimatum', left: 0.5, total: 1800, params: { faction: 'scav', strength: 9999 } }];
  E.startSiege(s, 'scav', 9999, 0.5, 9);
  const lost = s.stats.raidsLost;
  E.step(s, 1);
  assert.ok(!s.events.some((x) => x.uid === 9), 'the order ran out without deciding anything');
  assert.equal(s.stats.expired, 0, 'declining a bill is not a missed order');
  assert.ok(s.stats.raidsLost >= lost + 1, 'the attack landed at its deadline');
});

test('conversion never creates value: any converter round trip loses resources', () => {
  const s = E.newState(8);
  Object.assign(s.levels, { core: 6, generator: 5, fabricator: 5 });
  s.items.engineers = 300; // large production bonus must not turn the loop profitable
  s.res = { money: 5000, energy: 0, pop: 10 };
  for (const b of ['scrapyard', 'solar', 'shelter']) s.levels[b] = 0;
  const before = s.res.money + s.res.energy;
  for (let i = 0; i < 600; i++) E.step(s, 1);
  assert.ok(s.res.money + s.res.energy < before);
});

test('every loss option costs something: small armies still lose at least one unit', () => {
  const s = E.newState(10);
  s.items = { rifles: 3, militia: 2 };
  const inst = { uid: 1, id: 'looting', left: 100, total: 100, params: {} };
  const ev = E.eventById('looting');
  for (const ch of ev.choices) {
    const out = E.choiceOutcome(s, inst, ch);
    assert.ok(Object.values(out.units).some((n) => n >= 1), ch.label);
  }
});

test('durable units lose less, never more than 70% protected, and a loss still takes one unit', () => {
  const s = E.newState(11);
  s.items = { militia: 100, legion: 100 };
  const plan = E.lossPlan(s, 'staff', 0.4);
  assert.equal(plan.militia, 40);
  assert.equal(plan.legion, 22);
  for (const id of Object.keys(ITEM_BY_ID)) assert.ok((ITEM_BY_ID[id].durability || 0) <= 0.7, id);
  s.items = { legion: 1 };
  assert.deepEqual(E.lossPlan(s, 'staff', 0.1), { legion: 1 });
});

test('defeat losses grow with the strength gap, up to the cap', () => {
  const rule = { base: 0.2, cap: 0.45 };
  assert.equal(E.lossShare(rule, 100, 100), 0.2);
  assert.ok(Math.abs(E.lossShare(rule, 100, 200) - 0.4) < 1e-9);
  assert.equal(E.lossShare(rule, 100, 1000), 0.45);
  assert.equal(E.lossShare(rule, 0, 10), 0.45);
});

test('events only fire when every effect can happen; urgent ones only for an active player', () => {
  const s = E.newState(12);
  const glitch = E.eventById('glitch'); // buffs Power, penalises Experts
  s.items = { rifles: 5 };
  assert.equal(E.applicable(s, glitch), false);
  s.items.engineers = 1;
  assert.equal(E.applicable(s, glitch), true);

  const t = E.newState(13);
  t.levels.core = 2;
  t.eventTimer = 0;
  for (let i = 0; i < 2000 && !t.events.some((x) => E.eventById(x.id).urgent); i++) {
    t.events = [];
    t.eventTimer = 0;
    E.step(t, 1);
  }
  assert.ok(!t.events.some((x) => E.eventById(x.id).urgent));
  const urgent = E.eventById('override');
  assert.equal(urgent.deadline, 120);
});

test('no event choice charges the same resource twice', () => {
  for (const ev of EVENTS) {
    for (const c of ev.choices) {
      const keys = [c.cost, c.lose, c.gain].flatMap((o) => Object.keys(o || {}));
      assert.equal(new Set(keys).size, keys.length, `${ev.id}: ${c.label}`);
    }
  }
});

test('time away counts up to a limit at reduced output; the Watch Daemon raises both', () => {
  const o = BALANCE.offline;
  const s = E.newState(14);
  // No Watch Daemon: an absence counts for the grace window only, and nothing accrues past it.
  const dark = E.catchUp(s, 10 * 3600);
  assert.equal(dark.seconds, o.graceSeconds);
  assert.equal(dark.dark, true);
  s.levels.daemon = 1;
  const r = E.catchUp(s, 10 * 3600);
  assert.equal(r.seconds, (o.baseHours + o.hoursPerStep) * 3600);
  assert.equal(r.efficiency, o.baseEfficiency);
  s.levels.daemon = 3; // levels alternate: time, efficiency, time
  const lim = E.offlineLimits(s);
  assert.equal(lim.seconds, (o.baseHours + 2 * o.hoursPerStep) * 3600);
  assert.ok(Math.abs(lim.efficiency - (o.baseEfficiency + o.efficiencyPerStep)) < 1e-9);
  assert.ok(E.offlineLimits(s, 1000).efficiency <= o.maxEfficiency);
});

test('a held sector falls after enough breached assaults; strength moves within its bounds', () => {
  const s = E.newState(15);
  s.levels.core = 3;
  s.sectors.push('rust', 'tunnels');
  s.taken.push('rust', 'tunnels');
  s.levels.works = 1;
  s.items = { barricades: 40 }; // enough defense that it is breached but never overrun
  for (let i = 0; i < NODES.breachesToFall; i++) {
    E.spawnAssault(s, { from: 'alley', target: 'rust' }, 1);
    s.assault.strength = Math.ceil(E.factors(s).defense * NODES.overrunRatio) - 1;
    E.step(s, 2);
  }
  assert.ok(!s.sectors.includes('rust'));
  E.shiftStrength(s, 'alley', 100);
  assert.equal(E.nodeStrength(s, 'alley'), NODES.strengthMax);
  E.shiftStrength(s, 'alley', -100);
  assert.equal(E.nodeStrength(s, 'alley'), NODES.strengthMin);
  const old = E.migrate({ ...s, nodes: undefined, taken: undefined, assault: undefined });
  assert.deepEqual(old.nodes, {});
  assert.ok(old.taken.includes('tunnels'));
});

test('every event id is unique, so a pending event resolves with its own choices', () => {
  assert.equal(new Set(EVENTS.map((e) => e.id)).size, EVENTS.length);
});

test('aggression rises with what you do to a sector, spreads, and cools off', () => {
  const s = E.newState(3);
  const sec = E.sectorById('ashgrove');
  const clanMate = SECTORS.find((x) => x.faction === sec.faction && x.id !== sec.id && !sec.links.includes(x.id));
  E.stirAggression(s, sec.id, { node: 0.4, clan: 0.1, near: 0.2 });
  assert.ok(Math.abs(s.nodes[sec.id].a - 0.4) < 1e-9);
  assert.ok(Math.abs(s.nodes[clanMate.id].a - 0.1) < 1e-9);
  const linked = sec.links.filter((l) => E.sectorById(l).faction && !s.sectors.includes(l));
  assert.ok(linked.length && linked.every((l) => s.nodes[l].a >= 0.2 - 1e-9));
  // Looking at a sector counts, but each look after the first counts for less.
  E.inspectSector(s, sec.id);
  const first = s.nodes[sec.id].a - 0.4;
  E.inspectSector(s, sec.id);
  assert.ok(s.nodes[sec.id].a - 0.4 - first < first);
  // It never leaves its bounds and drifts back to calm while left alone.
  E.shiftAggression(s, sec.id, 99);
  assert.equal(s.nodes[sec.id].a, AGGR.max);
  E.advanceNodes(s, (AGGR.max / AGGR.calmPerHour) * 3600);
  assert.equal(s.nodes[sec.id].a, 0);
});

test('a sector you hold is never stirred, and the angrier border attacks more often', () => {
  const s = E.newState(4);
  const held = E.sectorById(s.sectors[0]);
  E.stirAggression(s, held.id, { node: 1 });
  assert.equal(E.aggression(s, held.id), 0);
  assert.ok(!s.nodes[held.id]);
  // A sector you hold with two enemy neighbours: only one of them is angry.
  const mine = SECTORS.find((x) => x.links.filter((l) => E.sectorById(l).faction).length >= 2 && x.faction);
  s.sectors.push(mine.id);
  s.levels.core = 9;
  const [angry, calm] = mine.links.filter((l) => E.sectorById(l).faction && l !== mine.id);
  E.shiftAggression(s, angry, AGGR.max);
  E.shiftAggression(s, calm, AGGR.min);
  const from = { [angry]: 0, [calm]: 0 };
  for (let i = 0; i < 400; i++) {
    s.assault = null;
    const a = E.spawnAssault(s);
    if (a && from[a.from] !== undefined) from[a.from]++;
  }
  assert.ok(from[angry] > from[calm] * 2, `${from[angry]} vs ${from[calm]}`);
});

test('an assault far over your defense takes the sector on the first breach', () => {
  const s = E.newState(31);
  s.levels.core = 3;
  s.levels.works = 1;
  s.items = { barricades: 20 };
  s.sectors.push('rust');
  s.taken.push('rust');
  // Just under the ratio: a foothold, nothing more.
  E.spawnAssault(s, { from: 'drowned', target: 'rust' }, 1);
  s.assault.strength = Math.ceil(E.factors(s).defense * NODES.overrunRatio) - 1;
  E.step(s, 2);
  assert.ok(s.sectors.includes('rust'));
  assert.equal(E.breaches(s, 'rust'), 1);
  // Over it: the sector is gone, and they hold it as hard as they took it.
  E.spawnAssault(s, { from: 'drowned', target: 'rust' }, 1);
  s.assault.strength = Math.ceil(E.factors(s).defense * NODES.overrunRatio) * 20;
  E.step(s, 2);
  assert.ok(!s.sectors.includes('rust'));
  const report = s.inbox.filter((m) => m.kind === 'raid').pop();
  assert.ok(report.overrun && report.fell);
  assert.ok(Math.abs(E.nodeStrength(s, 'rust') - NODES.takenMax) < 1e-3);
});

test('ground you have held once is yours to retake, sealed chapter or not', () => {
  const s = E.newState(32);
  s.levels.core = 2;
  const sec = E.sectorById('checkpoint'); // Remnant, opens at Core 4
  s.sectors.push('glass');
  assert.ok(!E.chapterOpen(s, sec.chapter));
  assert.equal(E.sectorStatus(s, sec), 'locked');
  s.sectors.push('checkpoint');
  s.taken.push('checkpoint');
  // A clan whose chapter is sealed can still take it back...
  s.items = {};
  E.spawnAssault(s, { from: 'kilo', target: 'checkpoint' }, 1);
  E.step(s, 2);
  assert.ok(!s.sectors.includes('checkpoint'));
  // ...and it stays a target afterwards, because it was mine once.
  assert.equal(E.sectorStatus(s, sec), 'target');
});

test('a clan profile is every stance its traits land in, multiplied together', () => {
  const s = E.newState(33);
  Object.assign(E.clan(s, 'scav'), { fury: 0.9, fear: 0.9, order: 0.8, greed: 0 });
  const ids = E.stanceList(s, 'scav').map((x) => x.id);
  assert.ok(['retaliate', 'vendetta', 'zealots', 'conscript'].every((x) => ids.includes(x)), ids.join());
  const expected = CLANS.stances.filter((x) => ids.includes(x.id)).reduce((a, x) => a * (x.strength || 1), 1);
  assert.ok(Math.abs(E.clanProfile(s, 'scav').strength - expected) < 1e-9);
  // Back at its baseline mood the clan does nothing in particular.
  Object.assign(E.clan(s, 'scav'), FACTIONS.scav.mood);
  assert.deepEqual(E.stanceList(s, 'scav').map((x) => x.id), []);
});

test('taking a capital turns the clan to taking it back', () => {
  const s = E.newState(34);
  Object.assign(s.levels, { core: 3, armory: 1, battery: 8, habitat: 6 });
  s.res = { money: 1e7, energy: 1e6, pop: 1e5 };
  s.items = { rifles: 4000 };
  s.sectors.push('junkfort');
  const before = E.clan(s, 'scav').fury;
  assert.ok(E.launchOp(s, 'throne'));
  s.op.remaining = 0.001;
  E.step(s, 1);
  assert.ok(s.sectors.includes('throne'), 'the capital falls');
  assert.ok(E.clan(s, 'scav').fury > before);
  assert.ok(E.stanceList(s, 'scav').some((x) => x.id === 'reclaim'));
  // Their raids stop, but the clan keeps attacking the ground it lost.
  assert.ok(!E.activeRaiders(s).includes('scav'));
  assert.ok(E.clanProfile(s, 'scav').retake > 1);
});

test('stance effects on the economy are rebuilt on load, never saved twice', () => {
  const s = E.newState(35);
  s.levels.core = 6;
  s.sectors.push('candle', 'pilgrim'); // enough of the Cult held for a blockade
  Object.assign(E.clan(s, 'cult'), { fury: 0.1, fear: 0.1, order: 0.9, greed: 0.95 });
  E.advanceClans(s, 0.001);
  const buff = s.buffs.find((b) => b.clan === 'cult');
  assert.ok(buff && buff.hold && buff.stance === 'blockade');
  E.advanceBuffs(s, 1e6); // a held effect never times out
  assert.ok(s.buffs.includes(buff));
  const back = E.migrate(JSON.parse(JSON.stringify(s)));
  assert.ok(!back.buffs.some((b) => b.clan));
  E.advanceClans(back, 0.001);
  assert.equal(back.buffs.filter((b) => b.clan === 'cult').length, 1);
  assert.ok(Math.abs(back.clans.cult.greed - 0.95) < 1e-4);
});

test('a defeat always costs someone; a win only when it was close enough to', () => {
  const s = E.newState(36);
  s.levels.barracks = 1;
  s.items = { militia: 100 };
  assert.deepEqual(E.lossPlan(s, 'staff', 0.0001, false), {});
  assert.deepEqual(E.lossPlan(s, 'staff', 0.0001), { militia: 1 });
  assert.deepEqual(E.lossPlan(s, 'staff', 0.1, false), { militia: 10 });
});

test('rolls are a pure function of the save, so a reload keeps the same future', () => {
  const s = E.newState(99);
  Object.assign(s.levels, { core: 4 });
  E.catchUp(s, 3600);
  // What each system will roll next is readable without taking it...
  const ahead = ['raid', 'assault', 'event'].map((k) => E.peek(s, k));
  const reloaded = E.migrate(JSON.parse(JSON.stringify(s)));
  assert.deepEqual(['raid', 'assault', 'event'].map((k) => E.peek(reloaded, k)), ahead);
  // ...and one system rolling never moves another's.
  E.rand(s, 'event');
  assert.equal(E.peek(s, 'raid'), ahead[0]);
  assert.notEqual(E.peek(s, 'event'), ahead[2]);
  // A new day of play turns the seed over, so nothing repeats.
  const later = { ...s, playTime: s.playTime + 86400 };
  assert.notEqual(E.peek(later, 'raid'), E.peek(s, 'raid'));
});

test('lessons are owed once each, in order, as the state reaches them', () => {
  const s = E.newState(41);
  assert.equal(E.lessonDue(s).id, 'first', 'the first lesson is owed from the first second');
  E.markTaught(s, 'first');
  assert.equal(E.lessonDue(s), null, 'nothing else is owed yet');
  s.levels.barracks = 1;
  assert.equal(E.lessonDue(s).id, 'arsenal');
  E.markTaught(s, 'arsenal');
  s.levels.core = 2;
  assert.equal(E.lessonDue(s).id, 'war', 'a new system unlocking brings its own lesson');
  // What is taught survives a save, so nothing is ever explained twice.
  const back = E.migrate(JSON.parse(JSON.stringify(s)));
  assert.deepEqual(back.taught, ['first', 'arsenal']);
});
