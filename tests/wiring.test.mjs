// Wiring tests: the things that break without anyone noticing. A module that no test imports can sit
// broken for weeks; an icon key with a typo quietly renders a blank hexagon; a lesson that points at
// a selector nobody renders any more strands the player behind an overlay that will not move on.
import test from 'node:test';
import assert from 'node:assert/strict';
import { readdirSync, statSync, readFileSync } from 'node:fs';
import { ICONS } from '../js/ui/icons.js';
import { SPRITE } from '../js/ui/icon-sprite.js';
import { LESSONS } from '../js/data.js';

const files = (dir) => readdirSync(dir).flatMap((f) => {
  const p = `${dir}/${f}`;
  return statSync(p).isDirectory() ? files(p) : p.endsWith('.js') ? [p] : [];
});
const SOURCES = files('js');

test('every module loads: no syntax errors, no broken imports, no work at load time', async () => {
  for (const f of SOURCES) {
    // main.js is the host entry point and wires itself to the browser as it loads.
    if (f === 'js/main.js') continue;
    await import(`../${f}`);
  }
});

test('every icon the UI asks for exists, so none of them renders as a blank hexagon', () => {
  const missing = new Set();
  for (const f of SOURCES) {
    for (const m of readFileSync(f, 'utf8').matchAll(/\b(?:icon|labeled)\(\s*'([a-z_0-9]+)'/g)) {
      if (!ICONS[m[1]]) missing.add(`${m[1]} (${f})`);
    }
  }
  assert.deepEqual([...missing], []);
});

test('every icon key is in the built sprite, so rebuilding it was not forgotten', () => {
  const absent = Object.keys(ICONS).filter((k) => !SPRITE.includes(`id="i-${k}"`));
  assert.deepEqual(absent, [], 'run tools/build-icons.mjs');
});

test('lessons point at something real and can always be finished', () => {
  const ids = new Set();
  const markup = SOURCES.map((f) => readFileSync(f, 'utf8')).join('\n');
  for (const l of LESSONS) {
    assert.ok(!ids.has(l.id), `duplicate lesson id ${l.id}`);
    ids.add(l.id);
    assert.ok(l.steps.length, `${l.id} has no steps`);
    for (const [i, step] of l.steps.entries()) {
      const where = `${l.id} step ${i + 1}`;
      assert.ok(step.say && step.say.length > 10, `${where} says nothing`);
      assert.ok(step.done === 'tap' || step.done === 'read' || typeof step.done === 'object', `${where} cannot be finished`);
      assert.ok(step.done !== 'tap' || step.at, `${where} waits for a tap on nothing`);
      if (!step.at) continue;
      // The selector has to match something the UI actually writes.
      const token = step.at.replace(/[[\]"']/g, '').split('=').pop().replace(/^\./, '');
      assert.ok(markup.includes(token), `${where} points at ${step.at}, which nothing renders`);
    }
  }
});

test('the AI picks the most synthetic voice on offer, never a lifelike one', async () => {
  // The picker runs once per session off whatever the device has, so a bad choice is silent.
  globalThis.speechSynthesis = {
    getVoices: () => [
      { name: 'Google US English', voiceURI: 'Google US English', lang: 'en-US', localService: false },
      { name: 'Microsoft Aria Online (Natural)', voiceURI: 'aria', lang: 'en-US', localService: false },
      { name: 'Samantha', voiceURI: 'Samantha', lang: 'en-US', localService: true },
      { name: 'eSpeak Deutsch', voiceURI: 'espeak-de', lang: 'de-DE', localService: true },
      { name: 'Google Deutsch', voiceURI: 'Google Deutsch', lang: 'de-DE', localService: false },
    ],
    addEventListener: () => {},
  };
  const { voiceName } = await import('../js/ui/voice.js');
  assert.match(voiceName(), /eSpeak Deutsch/);
  delete globalThis.speechSynthesis;
});

test('no handler reads e.currentTarget after awaiting: by then the event has stopped dispatching', () => {
  // It reads null, which throws on the next property access and takes the whole loop down.
  const bad = [];
  for (const f of SOURCES) {
    const src = readFileSync(f, 'utf8');
    for (const m of src.matchAll(/currentTarget/g)) {
      const start = Math.max(src.lastIndexOf('onclick', m.index), src.lastIndexOf('addEventListener', m.index));
      const handler = src.slice(start, m.index);
      if (start >= 0 && handler.includes('async') && handler.includes('await')) {
        bad.push(`${f}: ${src.slice(start, start + 60).split('\n')[0]}`);
      }
    }
  }
  assert.deepEqual(bad, []);
});
