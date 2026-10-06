import test from 'node:test';
import assert from 'node:assert/strict';
import * as E from '../js/engine.js';
import { ITEM_BY_ID } from '../js/data.js';

test('offline catch-up lands builds mid-stretch and matches live play closely', () => {
  const live = E.newState();
  const away = E.newState();
  E.startBuild(live, 'scrapyard');
  E.startBuild(away, 'scrapyard');
  for (let i = 0; i < 6000; i++) {
    E.step(live, 0.1);
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
