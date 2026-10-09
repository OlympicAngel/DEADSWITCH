// The developer panel is how the game is measured, so two rules about it are worth guarding:
// editing a field must keep the field's type, and every focus the UI sets must have an inspector.
import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { getPath, setPath } from '../js/ui/debug/tree.js';
import { INSPECTORS } from '../js/ui/debug/inspect.js';

test('editing a state field keeps the type it had', () => {
  const s = { res: { money: 10 }, name: 'ARC', paused: { solar: false }, op: null };
  assert.ok(setPath(s, 'res.money', '250.5'));
  assert.equal(s.res.money, 250.5);
  assert.ok(!setPath(s, 'res.money', 'abc')); // a number field never takes nonsense
  assert.equal(s.res.money, 250.5);
  assert.ok(setPath(s, 'paused.solar', 'true'));
  assert.equal(s.paused.solar, true);
  assert.ok(setPath(s, 'name', 'BOT'));
  assert.equal(getPath(s, 'name'), 'BOT');
  assert.ok(!setPath(s, 'res.gold', '5')); // no inventing fields
  assert.ok(!setPath(s, 'nope.deep', '5'));
});

test('every focus the UI sets has an inspector (AGENTS.md: wire new features to the panel)', () => {
  const dir = new URL('../js/ui/', import.meta.url).pathname;
  const files = [];
  const walk = (d) => fs.readdirSync(d, { withFileTypes: true }).forEach((e) => {
    if (e.isDirectory()) walk(path.join(d, e.name));
    else if (e.name.endsWith('.js')) files.push(path.join(d, e.name));
  });
  walk(dir);
  const kinds = new Set();
  for (const f of files) {
    for (const m of fs.readFileSync(f, 'utf8').matchAll(/setFocus\(\s*'([a-z]+)'/g)) kinds.add(m[1]);
  }
  assert.ok(kinds.size >= 5, [...kinds].join());
  for (const k of kinds) {
    assert.ok(INSPECTORS[k], `focus kind "${k}" has no inspector in js/ui/debug/inspect.js`);
  }
});
