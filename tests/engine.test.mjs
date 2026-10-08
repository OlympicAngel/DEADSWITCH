import test from 'node:test';
import assert from 'node:assert/strict';
import * as E from '../js/engine.js';
import { ITEM_BY_ID, BY_ID, EVENTS, BALANCE, NODES } from '../js/data.js';

test('offline catch-up lands builds mid-stretch and matches live play at offline rates', () => {
  const live = E.newState();
  const away = E.newState();
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
  Object.assign(s.levels, { core: 4 });
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
  Object.assign(s.levels, { core: 2, battery: 2, generator: 2 });
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

test('multi-route sectors are fortified until every approach is held', () => {
  const s = E.newState(2);
  const tunnels = E.sectorById('tunnels'); // approached from Drowned Mall and The Sump
  s.sectors = ['nest', 'drowned'];
  assert.equal(E.flank(s, tunnels).bonus, 0.5);
  s.sectors.push('sump');
  assert.equal(E.flank(s, tunnels).bonus, 0);
  assert.equal(E.flank(s, E.sectorById('rust')).bonus, 0);
});

test('refusing a threat locks a siege to the deadline; silence brings it at once', () => {
  const s = E.newState(6);
  Object.assign(s.levels, { core: 2, works: 1 });
  s.items.barricades = 5;
  const ev = E.eventById('ultimatum');
  s.events = [{ uid: 1, id: 'ultimatum', left: 1000, total: 1800, params: { faction: 'scav', strength: 999 } }];
  E.resolveEvent(s, 1, ev.choices.findIndex((c) => c.siege));
  assert.equal(s.sieges.length, 1);
  assert.equal(Math.round(s.sieges[0].remaining), 1000);
  s.events = [{ uid: 2, id: 'ultimatum', left: 0.5, total: 1800, params: { faction: 'scav', strength: 999 } }];
  const lost = s.stats.raidsLost;
  E.step(s, 1);
  assert.ok(s.stats.raidsLost >= lost + 1, 'the expired threat attacked immediately');
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
  const r = E.catchUp(s, 10 * 3600);
  assert.equal(r.seconds, o.baseHours * 3600);
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
  s.items = {}; // no defense: every assault breaches
  for (let i = 0; i < NODES.breachesToFall; i++) {
    E.spawnAssault(s, { from: 'alley', target: 'rust' }, 1);
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
