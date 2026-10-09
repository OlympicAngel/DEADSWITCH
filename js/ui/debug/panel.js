// Developer panel: a floating window over the game that shows everything the UI hides and lets it all
// be changed. Switched on in Settings, remembered per browser. It lives in the top layer (popover), so
// it stays readable over dialogs, and it only updates while it is open.
import * as E from '../../engine.js';
import * as DATA from '../../data.js';
import { icon } from '../icons.js';
import { put } from '../dom.js';
import { getFocus, focusSeq, setFocus } from '../focus.js';
import { inspect, worldRows } from './inspect.js';
import { treeHtml, getPath, setPath } from './tree.js';

const KEY = 'deadswitch.dev';
const POS = 'deadswitch.devpos';
const TABS = ['focus', 'state', 'time', 'world', 'data'];
const SPEEDS = [0, 0.5, 1, 5, 25, 100]; // × real time; 0 pauses the simulation
const SKIPS = [['+1m', 60], ['+10m', 600], ['+1h', 3600], ['+6h', 21600], ['+24h', 86400]];
const SKIP_STEP = 1; // seconds per simulated step while skipping, so timers land in order

const read = (k, d) => {
  try {
    return localStorage.getItem(k) ?? d;
  } catch {
    return d;
  }
};
const write = (k, v) => {
  try {
    localStorage.setItem(k, v);
  } catch {
    // storage blocked: the setting just will not stick
  }
};

export const devEnabled = () => read(KEY, '0') === '1';

export function createDebug(game, rerender) {
  const el = document.createElement('div');
  el.className = 'dbg';
  el.setAttribute('popover', 'manual');
  el.innerHTML = `
    <header class="dbg-bar" data-ddrag>
      <b>${icon('dev')}DEV</b>
      <span class="dbg-head" data-dhead></span>
      <button class="dbg-x" data-dact="fold" aria-label="Fold">_</button>
      <button class="dbg-x" data-dact="off" aria-label="Close">×</button>
    </header>
    <nav class="dbg-tabs">${TABS.map((t) => `<button data-dtab="${t}">${t}</button>`).join('')}</nav>
    <div class="dbg-body" data-dbody></div>
    <footer class="dbg-foot"><span data-dfoot></span></footer>`;
  document.body.append(el);

  const body = el.querySelector('[data-dbody]');
  const head = el.querySelector('[data-dhead]');
  const foot = el.querySelector('[data-dfoot]');
  const state = { tab: read('deadswitch.devtab', 'focus'), open: new Set(['res', 'levels']), key: '', seq: 0 };
  let refs = [];
  let actions = [];
  let shown = false;

  // ---------- window chrome ----------

  const place = () => {
    const [x, y] = read(POS, '').split(',').map(Number);
    if (Number.isFinite(x) && Number.isFinite(y)) {
      el.style.left = `${Math.max(0, Math.min(innerWidth - 60, x))}px`;
      el.style.top = `${Math.max(0, Math.min(innerHeight - 40, y))}px`;
      el.style.right = 'auto';
    }
  };
  el.querySelector('[data-ddrag]').addEventListener('pointerdown', (e) => {
    if (e.target.closest('[data-dact]')) return;
    const r = el.getBoundingClientRect();
    const dx = e.clientX - r.left;
    const dy = e.clientY - r.top;
    const move = (m) => {
      el.style.left = `${Math.max(0, Math.min(innerWidth - 60, m.clientX - dx))}px`;
      el.style.top = `${Math.max(0, Math.min(innerHeight - 40, m.clientY - dy))}px`;
      el.style.right = 'auto';
    };
    const up = () => {
      removeEventListener('pointermove', move);
      removeEventListener('pointerup', up);
      write(POS, `${parseInt(el.style.left, 10)},${parseInt(el.style.top, 10)}`);
    };
    addEventListener('pointermove', move);
    addEventListener('pointerup', up);
  });

  // Nothing may cover the panel. A modal dialog owns the top layer, so while one is open the panel
  // rides inside it; otherwise it sits in the top layer itself.
  function perch() {
    const host = document.querySelector('dialog[open]') || document.body;
    if (el.parentElement === host) return;
    el.hidePopover?.();
    host.append(el);
    if (host === document.body) el.showPopover?.();
  }

  function show(on) {
    write(KEY, on ? '1' : '0');
    shown = on;
    el.classList.toggle('on', on);
    el.classList.toggle('folded', read('deadswitch.devfold', '0') === '1');
    if (on) {
      place();
      perch();
      build(true);
    } else {
      el.hidePopover?.();
      el.remove();
      document.body.append(el);
      game.dev.speed = 1;
    }
  }

  // ---------- rendering ----------

  const rowsHtml = (rows) => rows.map(([label], i) => `<div class="dbg-row"><span class="dbg-k">${label}</span><b class="dbg-v" data-row="${i}"></b></div>`).join('');
  const btns = (list, act) => `<div class="dbg-btns">${list.map((b, i) => `<button data-dact="${act}" data-i="${i}">${b}</button>`).join('')}</div>`;

  function focusBody(s) {
    const info = inspect(s, getFocus());
    actions = info.actions;
    return `<div class="dbg-title">${info.title}</div>${rowsHtml(info.rows)}
      ${actions.length ? btns(actions.map((a) => a[0]), 'run') : ''}
      <details class="dbg-json"><summary>raw</summary><pre>${JSON.stringify(info.json(), null, 1)}</pre></details>`;
  }

  function stateBody(s) {
    const quick = [
      ['+1k all', (g) => { for (const r of Object.keys(g.state.res)) g.state.res[r] += 1000; }],
      ['fill caps', (g) => { const c = E.caps(g.state); for (const r of Object.keys(g.state.res)) g.state.res[r] = Number.isFinite(c[r]) ? c[r] : g.state.res[r] + 1e5; }],
      ['core +1', (g) => { g.state.levels.core = (g.state.levels.core || 1) + 1; }],
      ['all buildings +5', (g) => { for (const b of DATA.BUILDINGS) g.state.levels[b.id] = (g.state.levels[b.id] || 0) + 5; }],
      ['+10 of every unit', (g) => { for (const i of DATA.ITEMS) g.state.items[i.id] = (g.state.items[i.id] || 0) + 10; }],
      ['align −100', (g) => { g.state.align = -100; }],
      ['align 0', (g) => { g.state.align = 0; }],
      ['align +100', (g) => { g.state.align = 100; }],
      ['reseed', (g) => { g.state.rng = (Math.random() * 0xffffffff) >>> 0; }],
      ['wipe nodes', (g) => { g.state.nodes = {}; }],
    ];
    actions = quick;
    return `${btns(quick.map((q) => q[0]), 'run')}
      <div class="dbg-note">Every field writes straight into the save.</div>
      ${treeHtml(s, state.open)}`;
  }

  function timeBody(s) {
    const acts = [
      ...SKIPS.map(([label, secs]) => [label, (g) => skip(g, secs)]),
      ['offline 1h', (g) => E.catchUp(g.state, 3600)],
      ['offline 8h', (g) => E.catchUp(g.state, 28800)],
      ['order now', (g) => { g.state.eventTimer = 0.001; }],
      ['raid now', (g) => { g.state.raidTimer = 0.001; g.state.raidsStarted = true; }],
      ['assault now', (g) => { g.state.assaultTimer = 0.001; }],
      ['land attack', (g) => { const a = E.nextAttack(g.state); if (a) a.remaining = 0.001; }],
      ['finish build', (g) => { if (g.state.build) g.state.build.remaining = 0.001; }],
      ['finish op', (g) => { if (g.state.op) g.state.op.remaining = 0.001; }],
    ];
    actions = acts;
    const speed = `<div class="dbg-btns speeds">${SPEEDS.map((v) => `<button data-dspeed="${v}" class="${game.dev.speed === v ? 'on' : ''}">${v === 0 ? 'pause' : `×${v}`}</button>`).join('')}</div>`;
    const t1 = (v) => (Number.isFinite(v) ? `${v.toFixed(1)}s` : String(v));
    const timers = [
      ['playTime', () => t1(s.playTime)],
      ['eventTimer', () => t1(s.eventTimer)],
      ['raidTimer', () => t1(s.raidTimer)],
      ['assaultTimer', () => t1(s.assaultTimer)],
      ['offlineRaids', () => s.offlineRaids],
      ['build', () => (s.build ? `${s.build.id} ${s.build.remaining.toFixed(1)}s` : '-')],
      ['op', () => (s.op ? `${s.op.sector} ${s.op.remaining.toFixed(1)}s` : '-')],
      ['raid', () => (s.raid ? `${s.raid.faction} ${s.raid.remaining.toFixed(1)}s` : '-')],
      ['assault', () => (s.assault ? `${s.assault.from}→${s.assault.target} ${s.assault.remaining.toFixed(1)}s` : '-')],
      ['sieges', () => s.sieges.map((x) => `${x.faction} ${x.remaining.toFixed(0)}s`).join(', ') || '-'],
      ['buffs', () => s.buffs.map((b) => `${b.key}${b.amount > 0 ? '+' : ''}${b.amount} ${b.remaining.toFixed(0)}s`).join(', ') || '-'],
      ['offline limit', () => `${E.offlineLimits(s).seconds}s at ${(E.offlineLimits(s).efficiency * 100).toFixed(0)}%`],
    ];
    refs.pending = timers;
    return `${speed}${btns(acts.map((a) => a[0]), 'run')}<div class="dbg-note">Skipping runs the real loop ${SKIP_STEP}s at a time.</div>${rowsHtml(timers)}`;
  }

  function worldBody(s) {
    actions = [];
    const rows = worldRows(s).sort((a, b) => b.aggr - a.aggr);
    return `<div class="dbg-note">aggr = live (stored + pressure) · w = assault weight</div>
      <table class="dbg-tab"><thead><tr><th>sector</th><th>fac</th><th>def</th><th>×</th><th>aggr</th><th>w</th><th>br</th></tr></thead><tbody>
      ${rows.map((r) => `<tr data-dsector="${r.id}" class="${r.mine ? 'mine' : ''}"><td>${r.name}</td><td>${r.faction}</td><td>${r.def || '-'}</td><td>${r.m.toFixed(2)}</td><td>${r.aggr.toFixed(2)}</td><td>${r.weight ? r.weight.toFixed(2) : '-'}</td><td>${r.marks || ''}</td></tr>`).join('')}
      </tbody></table>`;
  }

  function dataBody() {
    actions = [];
    const consts = Object.fromEntries(Object.entries(DATA).filter(([k]) => k === k.toUpperCase()));
    return `<div class="dbg-note">Read-only: every balance number in js/data.</div>${treeHtml(consts, state.open, { path: 'D', editable: false })}`;
  }

  // Rebuilds the body; cheap enough for a dev tool, so it runs on tab, focus and expand changes only.
  function build(force = false) {
    const s = game.state;
    const key = [state.tab, getFocus().kind, getFocus().id, focusSeq(), state.seq, [...state.open].join('|')].join('/');
    if (!force && key === state.key) return;
    state.key = key;
    refs = [];
    const html = { focus: focusBody, state: stateBody, time: timeBody, world: worldBody, data: dataBody }[state.tab](s);
    body.innerHTML = html;
    for (const b of el.querySelectorAll('[data-dtab]')) {
      b.classList.toggle('on', b.dataset.dtab === state.tab);
    }
    const live = state.tab === 'focus' ? inspect(s, getFocus()).rows : state.tab === 'time' ? refs.pending || [] : [];
    refs = [...body.querySelectorAll('[data-row]')].map((x) => ({ el: x, get: live[Number(x.dataset.row)] && live[Number(x.dataset.row)][1] }));
    refs.inputs = [...body.querySelectorAll('.dbg-in')];
    body.scrollTop = 0;
  }

  // ---------- actions ----------

  function after() {
    game.act.save();
    state.key = '';
    rerender();
  }

  /** Runs the live loop in small steps, so every timer fires in the right order. */
  function skip(g, seconds) {
    for (let t = 0; t < seconds; t += SKIP_STEP) {
      g.flows = E.step(g.state, Math.min(SKIP_STEP, seconds - t), false, true);
    }
  }

  el.addEventListener('click', (e) => {
    const t = e.target.closest('[data-dact], [data-dtab], [data-dspeed], [data-open], [data-dsector]');
    if (!t) return;
    if (t.dataset.dtab) {
      state.tab = t.dataset.dtab;
      write('deadswitch.devtab', state.tab);
      build(true);
      return;
    }
    if (t.dataset.dspeed !== undefined) {
      game.dev.speed = Number(t.dataset.dspeed);
      build(true);
      return;
    }
    if (t.dataset.open) {
      const p = t.dataset.open;
      state.open.has(p) ? state.open.delete(p) : state.open.add(p);
      build(true);
      return;
    }
    if (t.dataset.dsector) {
      setFocus('sector', t.dataset.dsector);
      state.tab = 'focus';
      build(true);
      return;
    }
    const act = t.dataset.dact;
    if (act === 'off') {
      show(false);
      return;
    }
    if (act === 'fold') {
      el.classList.toggle('folded');
      write('deadswitch.devfold', el.classList.contains('folded') ? '1' : '0');
      return;
    }
    if (act === 'run') {
      const run = actions[Number(t.dataset.i)];
      if (run) {
        run[1](game);
        after();
        build(true);
      }
    }
  });

  // Fields write on Enter or when they lose focus.
  body.addEventListener('change', (e) => {
    const input = e.target.closest('.dbg-in');
    if (!input) return;
    const root = state.tab === 'state' ? game.state : null;
    if (root && setPath(root, input.dataset.path, input.value)) {
      after();
    }
    input.classList.toggle('bad', !root);
  });

  // ---------- per frame ----------

  let worldAt = 0;
  function update() {
    if (!shown) return;
    perch();
    // The world table has no live rows; rebuild it once a second instead of ten times.
    if (state.tab === 'world' && performance.now() - worldAt > 1000) {
      worldAt = performance.now();
      build(true);
    }
    if (el.classList.contains('folded')) {
      putHead();
      return;
    }
    try {
      build();
    } catch (err) {
      body.innerHTML = `<div class="dbg-note">inspector failed: ${err.message}</div>`;
      refs = [];
    }
    const s = game.state;
    // A row may read something that has just been resolved away; the panel must never break the game.
    for (const r of refs) {
      if (!r.get) continue;
      try {
        put(r.el, String(r.get(s)));
      } catch (err) {
        put(r.el, `! ${err.message}`);
      }
    }
    for (const input of refs.inputs || []) {
      if (document.activeElement !== input) {
        const v = getPath(s, input.dataset.path);
        const str = v === null || v === undefined ? String(v) : String(v);
        if (input.value !== str) input.value = str;
      }
    }
    putHead();
  }

  function putHead() {
    const f = getFocus();
    const s = game.state;
    put(head, `${f.kind}:${f.id ?? '-'}`);
    put(foot, `t ${Math.round(s.playTime)}s · ×${game.dev.speed} · rng ${s.rng} · v${s.v} · ev ${s.events.length}`);
  }

  if (devEnabled()) show(true);
  return { update, show, isOn: () => shown, toggle: () => show(!shown) };
}
