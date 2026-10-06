// UI shell: header, status strip, tabs and the frame loop. Panels rebuild only on structural change;
// numbers, affordability and timers are patched in place every frame through cached refs.
import {
  RESOURCES, RESOURCE_KEYS, FACTORS, FACTOR_KEYS, ITEMS, RANKS, BY_ID, SECTORS, BALANCE, FACTIONS, RAIDS, DIRECTIVES, ALIGNMENT,
} from '../data.js';
import * as E from '../engine.js';
import { num, rate, time, pct, esc } from '../format.js';
import { tags, chanceClass } from './common.js';
import { renderBase, bindBase, updateBase } from './base.js';
import { renderArsenal, bindArsenal, updateArsenal, buyCount } from './arsenal.js';
import { renderMap, bindMap, updateMap, defaultSector } from './map.js';
import { renderArchive } from './archive.js';
import { createModals } from './modals.js';
import { sfx } from './sfx.js';
import { floatText, flash, burst } from './fx.js';

const TABS = [
  { id: 'base', name: 'Base' },
  { id: 'arsenal', name: 'Arsenal' },
  { id: 'ops', name: 'Operations' },
  { id: 'archive', name: 'Archive' },
];

export function createUI(root, game) {
  const ui = {
    tab: 'base', shopTab: 'weapons', buyMode: 1, sector: null,
    key: '', refs: null, logSeq: -1, slotKeys: {}, raidKey: null, dots: {},
  };

  root.innerHTML = `
    <header class="top">
      <div class="brand">
        <span class="logo" aria-hidden="true"><i></i></span>
        <div><h1>DEADSWITCH</h1><div class="rank" id="rank"></div></div>
      </div>
      <div class="meters">
        <div class="meter threat" title="Threat = Power + Defense + Experts × ${BALANCE.threatExpertWeight}. Raids grow with it.">
          <span class="label">Threat</span><b id="threat">0</b>
          <div class="bar"><i id="rankBar"></i></div>
          <span class="next" id="rankNext"></span>
        </div>
        <div class="meter align" title="Your choices shape what I become. Guardian: up to +${pct(ALIGNMENT.guardian.pop)} Population and Experts. Overlord: up to +${pct(ALIGNMENT.overlord.power)} Power and +${pct(ALIGNMENT.overlord.energy)} Energy.">
          <span class="label">Humanity</span><b id="alignLabel"></b>
          <div class="align-bar"><i id="alignMark"></i></div>
          <span class="next"><span>Overlord</span><span>Guardian</span></span>
        </div>
      </div>
      <button class="icon-btn" data-act="menu" aria-label="Settings">☰</button>
    </header>
    <section class="resources">
      ${RESOURCE_KEYS.map((r) => `
        <div class="res res-${r}" id="res-${r}">
          <span class="ic">${RESOURCES[r].icon}</span>
          <div class="res-body">
            <span class="res-name">${RESOURCES[r].name}</span>
            <span class="res-val"><b data-v></b><small data-cap></small></span>
            <span class="res-rate" data-rate></span>
          </div>
          <div class="fill"><i data-fill></i></div>
        </div>`).join('')}
    </section>
    <section class="factors">
      ${FACTOR_KEYS.map((f) => `
        <div class="factor f-${f}" id="factor-${f}" title="${esc(FACTORS[f].desc)}">
          <span class="ic">${FACTORS[f].icon}</span><span class="f-name">${FACTORS[f].name}</span><b></b>
        </div>`).join('')}
    </section>
    <div class="buffs" id="buffs"></div>
    <section class="status">
      <div class="slot" id="slot-build"></div>
      <div class="slot" id="slot-op"></div>
      <div class="slot raid" id="slot-raid"></div>
    </section>
    <section class="mission">
      <div class="directive" id="directive"></div>
      <button class="transmission" id="transmission" data-act="event" hidden></button>
    </section>
    <nav class="tabs" role="tablist">
      ${TABS.map((t) => `<button role="tab" data-tab="${t.id}">${t.name}<i class="dot" hidden></i></button>`).join('')}
    </nav>
    <div class="layout">
      <main id="panel"></main>
      <aside class="side-log"><h2>System log</h2><ol class="log"></ol></aside>
    </div>
    <div class="toasts" id="toasts" aria-live="polite"></div>
    <dialog id="modal"></dialog>
  `;

  const $ = (q) => root.querySelector(q);
  const panel = $('#panel');
  const modals = createModals($('#modal'), game, () => render());
  const resEls = Object.fromEntries(RESOURCE_KEYS.map((r) => {
    const el = $(`#res-${r}`);
    return [r, { el, v: el.querySelector('[data-v]'), cap: el.querySelector('[data-cap]'), rate: el.querySelector('[data-rate]'), fill: el.querySelector('[data-fill]') }];
  }));

  // ---------- input ----------

  root.addEventListener('click', (e) => {
    const t = e.target.closest('[data-act], [data-tab], [data-shop], [data-mode], [data-sector]');
    if (!t || t.disabled || t.closest('dialog')) {
      return;
    }
    const s = game.state;
    if (t.dataset.tab) {
      ui.tab = t.dataset.tab;
      ui.dots[ui.tab] = false;
      sfx.click();
    } else if (t.dataset.shop) {
      ui.shopTab = t.dataset.shop;
      sfx.click();
    } else if (t.dataset.mode) {
      ui.buyMode = t.dataset.mode === 'max' ? 'max' : Number(t.dataset.mode);
    } else if (t.dataset.sector) {
      ui.sector = t.dataset.sector;
      sfx.click();
    } else {
      act(t, s);
    }
    render();
  });
  root.addEventListener('keydown', (e) => {
    if ((e.key === 'Enter' || e.key === ' ') && e.target.dataset && e.target.dataset.sector) {
      e.preventDefault();
      e.target.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    }
  });

  function act(t, s) {
    const id = t.dataset.id;
    switch (t.dataset.act) {
      case 'build':
        if (game.act.build(id)) {
          sfx.click();
        }
        break;
      case 'cancel':
        game.act.cancel();
        break;
      case 'pause':
        game.act.toggle(id);
        break;
      case 'buy': {
        const item = ITEMS.find((i) => i.id === id);
        const n = buyCount(s, ui, item);
        const before = E.factors(s);
        if (game.act.buy(id, n)) {
          sfx.buy();
          const after = E.factors(s);
          const gained = FACTOR_KEYS.filter((k) => after[k] > before[k]).map((k) => `+${num(after[k] - before[k])} ${FACTORS[k].icon}`);
          floatText(t, gained.join('  ') || `+${n}`, 'ft-' + (item.gives ? Object.keys(item.gives)[0] : 'tech'));
        }
        break;
      }
      case 'launch':
        if (game.act.launch(id)) {
          sfx.launch();
        }
        break;
      case 'event':
        modals.showEvent();
        break;
      case 'fortify':
        ui.tab = 'arsenal';
        ui.shopTab = E.level(s, 'works') ? 'defenses' : 'staff';
        break;
      case 'goto-ops':
        ui.tab = 'ops';
        break;
      case 'menu':
        modals.openMenu();
        break;
      default:
    }
  }

  // ---------- structure ----------

  function render() {
    const s = game.state;
    if (!ui.sector || !E.sectorById(ui.sector)) {
      ui.sector = defaultSector(s);
    }
    root.querySelectorAll('.tabs [data-tab]').forEach((b) => {
      b.setAttribute('aria-selected', String(b.dataset.tab === ui.tab));
      b.querySelector('.dot').hidden = !ui.dots[b.dataset.tab];
    });
    root.dataset.tab = ui.tab;
    const key = JSON.stringify([ui.tab, ui.shopTab, ui.buyMode, ui.sector, s.levels, s.items, s.paused,
      s.build && s.build.id, s.sectors, s.op && s.op.sector, s.chapter, s.ending, s.stats, Math.round(s.align)]);
    if (key !== ui.key) {
      ui.key = key;
      const scroll = window.scrollY;
      const oldWrap = panel.querySelector('.map-wrap');
      const mapScroll = oldWrap ? oldWrap.scrollLeft : null;
      if (ui.tab === 'base') {
        panel.innerHTML = renderBase(s);
        ui.refs = bindBase(panel);
      } else if (ui.tab === 'arsenal') {
        panel.innerHTML = renderArsenal(s, ui);
        ui.refs = bindArsenal(panel);
      } else if (ui.tab === 'ops') {
        panel.innerHTML = renderMap(s, ui);
        ui.refs = bindMap(panel);
        centerMap(mapScroll);
      } else {
        panel.innerHTML = renderArchive(s);
        ui.refs = null;
      }
      window.scrollTo(0, scroll);
      ui.logSeq = -1;
    }
    update();
  }

  // Narrow screens scroll the map sideways: keep the scroll position, or start on the selection.
  function centerMap(prev) {
    const wrap = panel.querySelector('.map-wrap');
    if (wrap.scrollWidth <= wrap.clientWidth) {
      return;
    }
    if (prev !== null) {
      wrap.scrollLeft = prev;
      return;
    }
    const node = panel.querySelector('.node.sel');
    const w = wrap.getBoundingClientRect();
    const n = node.getBoundingClientRect();
    wrap.scrollLeft = n.left - w.left + n.width / 2 - w.width / 2;
  }

  // ---------- per frame ----------

  function update() {
    const s = game.state;
    const flows = game.flows;
    updateHeader(s, flows);
    updateBuild(s);
    updateOp(s);
    updateRaid(s);
    updateMission(s);
    if (ui.tab === 'base') {
      updateBase(s, ui.refs, flows);
    } else if (ui.tab === 'arsenal') {
      updateArsenal(s, ui, ui.refs);
    } else if (ui.tab === 'ops') {
      updateMap(s, ui, ui.refs);
    }
    updateLog(s);
    drainFx(s);
    modals.pump();
  }

  function updateHeader(s, flows) {
    const c = E.caps(s);
    for (const r of RESOURCE_KEYS) {
      const el = resEls[r];
      el.v.textContent = num(s.res[r]);
      el.cap.textContent = Number.isFinite(c[r]) ? ' / ' + num(c[r]) : '';
      const net = flows ? flows.prod[r] - flows.cons[r] : 0;
      const full = Number.isFinite(c[r]) && s.res[r] >= c[r] * 0.999;
      el.rate.textContent = full ? 'FULL' : rate(net);
      el.rate.className = 'res-rate ' + (full ? 'is-full' : net < -0.005 ? 'is-neg' : '');
      el.fill.style.width = Number.isFinite(c[r]) ? pct(s.res[r] / c[r]) : '0';
    }
    const f = E.factors(s);
    for (const k of FACTOR_KEYS) {
      $(`#factor-${k} b`).textContent = num(f[k]);
    }
    const t = E.threat(s, f);
    const ri = E.rankIndex(t);
    $('#threat').textContent = num(t);
    $('#rank').textContent = RANKS[ri].title;
    const next = RANKS[ri + 1];
    $('#rankBar').style.width = next ? pct((t - RANKS[ri].at) / (next.at - RANKS[ri].at)) : '100%';
    $('#rankNext').textContent = next ? `Next: ${next.title} at ${num(next.at)}` : 'Maximum threat';
    const a = Math.round(s.align);
    $('#alignLabel').textContent = `${E.alignmentLabel(a)} ${a > 0 ? '+' : ''}${a}`;
    $('#alignLabel').className = a >= ALIGNMENT.guardianAt ? 'hum' : a <= ALIGNMENT.overlordAt ? 'mach' : '';
    $('#alignMark').style.left = pct((a - ALIGNMENT.min) / (ALIGNMENT.max - ALIGNMENT.min));
    const buffs = s.buffs.map((b) => `<span class="buff ft-${b.key}">${esc(b.label)} · +${pct(b.amount)} ${(RESOURCES[b.key] || FACTORS[b.key]).name.replace('AI ', '')} · ${time(b.remaining)}</span>`).join('');
    const bEl = $('#buffs');
    if (bEl.innerHTML !== buffs) {
      bEl.innerHTML = buffs;
    }
  }

  // Rebuilds a status slot only when its key changes, then patches its timer.
  function slot(id, key, html) {
    const el = $(id);
    if (ui.slotKeys[id] !== key) {
      ui.slotKeys[id] = key;
      el.innerHTML = html;
    }
    return el;
  }

  function updateBuild(s) {
    if (!s.build) {
      slot('#slot-build', 'idle', '<span class="s-label">Builder</span><span class="s-main idle">Idle</span><span class="s-sub">Pick an upgrade in Base.</span>').dataset.state = 'idle';
      return;
    }
    const name = BY_ID[s.build.id].name;
    const el = slot('#slot-build', s.build.id, `<span class="s-label">Builder</span><span class="s-main">${name} → Lv ${E.level(s, s.build.id) + 1}</span>
      <span class="s-sub"><span data-t></span> <button class="link-btn" data-act="cancel" title="Cancel and refund">Cancel</button></span><div class="bar"><i data-b></i></div>`);
    el.dataset.state = 'busy';
    el.querySelector('[data-t]').textContent = time(s.build.remaining);
    el.querySelector('[data-b]').style.width = pct(1 - s.build.remaining / s.build.total);
  }

  function updateOp(s) {
    if (!s.op) {
      const any = SECTORS.some((x) => E.sectorStatus(s, x) === 'target');
      slot('#slot-op', 'idle' + any, `<span class="s-label">Operation</span><span class="s-main idle">${any ? 'Standing by' : 'No targets in range'}</span>
        <span class="s-sub">${any ? '<button class="link-btn" data-act="goto-ops">Open the map</button>' : 'Raise your AI Core to open new fronts.'}</span>`).dataset.state = 'idle';
      return;
    }
    const sec = E.sectorById(s.op.sector);
    const el = slot('#slot-op', s.op.sector, `<span class="s-label">Operation</span><span class="s-main" style="color:${FACTIONS[sec.faction].color}">⚔ ${sec.name}</span>
      <span class="s-sub"><span data-t></span> · <span data-c></span></span><div class="bar"><i data-b></i></div>`);
    el.dataset.state = 'busy';
    const p = E.opChance(s, sec);
    el.querySelector('[data-t]').textContent = time(s.op.remaining);
    el.querySelector('[data-c]').textContent = pct(p) + ' success';
    el.querySelector('[data-c]').className = 'chance-' + chanceClass(p);
    el.querySelector('[data-b]').style.width = pct(1 - s.op.remaining / s.op.total);
  }

  function updateRaid(s) {
    const raid = s.raid;
    const key = raid ? raid.faction + raid.strength : '';
    if (key && ui.raidKey !== null && key !== ui.raidKey) {
      sfx.alarm();
      toast(`${FACTIONS[raid.faction].raidName} spotted. Arrives in ${time(raid.remaining)}.`, 'bad');
    }
    ui.raidKey = key;
    if (!raid) {
      const quiet = E.level(s, 'core') < RAIDS.startAtCore;
      slot('#slot-raid', 'none' + quiet, `<span class="s-label">Threat</span><span class="s-main idle">${quiet ? 'Quiet' : 'No hostiles'}</span>
        <span class="s-sub">${quiet ? `Raids begin at AI Core Lv ${RAIDS.startAtCore}.` : 'Scouts see nothing. For now.'}</span>`).dataset.state = 'idle';
      return;
    }
    const f = FACTIONS[raid.faction];
    const el = slot('#slot-raid', key, `<span class="s-label">Incoming</span><span class="s-main" style="color:${f.color}">${f.sigil} ${f.raidName}</span>
      <span class="s-sub"><span data-t></span> · <span data-c></span></span>
      <span class="s-sub small">Strength ${num(raid.strength)} vs your Defense <span data-d></span> <button class="link-btn" data-act="fortify">Fortify</button></span>
      <div class="bar"><i data-b></i></div>`);
    const p = E.raidChance(s);
    el.dataset.state = p < 0.5 ? 'danger' : 'busy';
    el.classList.toggle('imminent', raid.remaining < 60);
    el.querySelector('[data-t]').textContent = time(raid.remaining);
    el.querySelector('[data-c]').textContent = pct(p) + ' to hold';
    el.querySelector('[data-c]').className = 'chance-' + chanceClass(p);
    el.querySelector('[data-d]').textContent = num(E.factors(s).defense);
    el.querySelector('[data-b]').style.width = pct(1 - raid.remaining / raid.total);
  }

  function updateMission(s) {
    const d = E.currentDirective(s);
    const dEl = $('#directive');
    if (!d) {
      slot('#directive', 'done', '<span class="s-label">Directives</span><span class="d-text">All directives complete. The wasteland is yours to shape.</span>');
    } else {
      const [have, need] = E.directiveProgress(s, d);
      slot('#directive', 'd' + s.directive, `<span class="s-label">Directive ${s.directive + 1} / ${DIRECTIVES.length}</span>
        <span class="d-text">${esc(d.text)}</span><span class="d-reward">Reward ${tags(d.reward, '+')}</span>
        <div class="bar"><i data-b></i></div>`);
      dEl.querySelector('[data-b]').style.width = pct(Math.min(1, have / need));
    }
    const tr = $('#transmission');
    const ev = s.event && E.eventById(s.event);
    tr.hidden = !ev;
    if (ev && tr.dataset.id !== ev.id) {
      tr.dataset.id = ev.id;
      tr.innerHTML = `<span class="blink">●</span><span><small>Incoming transmission</small>${esc(ev.title)}</span>`;
    }
    if (!ev) {
      tr.dataset.id = '';
    }
  }

  function updateLog(s) {
    if (ui.logSeq === s.lineSeq) {
      return;
    }
    const fresh = ui.logSeq < 0 ? [] : s.log.slice(-Math.max(0, s.lineSeq - ui.logSeq));
    ui.logSeq = s.lineSeq;
    const html = s.log.slice().reverse().map((l) => `<li class="tone-${l.tone}"><time>${time(l.t)}</time><span>${esc(l.text)}</span></li>`).join('');
    root.querySelectorAll('.log').forEach((el) => { el.innerHTML = html; });
    for (const l of fresh) {
      if (['unlock', 'item', 'rank', 'core'].includes(l.tone)) {
        toast(l.text, l.tone);
      }
      if (l.tone === 'item' && ui.tab !== 'arsenal') {
        ui.dots.arsenal = true;
      }
    }
  }

  function drainFx(s) {
    for (const fx of s.fx.splice(0)) {
      if (fx.kind === 'built') {
        sfx.build();
        const card = panel.querySelector(`[data-card="${fx.id}"]`);
        flash(card);
        burst(card, 'var(--accent-2)');
      } else if (fx.kind === 'event') {
        sfx.event();
        toast('Incoming transmission. Tap it when you are ready.', 'story');
      } else if (fx.kind === 'directive') {
        sfx.win();
        flash($('#directive'));
        toast(`Directive complete: ${fx.text}`, 'good');
      }
    }
    if (s.sectors.length !== ui.sectorCount) {
      if (ui.sectorCount !== undefined) {
        ui.dots.archive = ui.tab !== 'archive';
      }
      ui.sectorCount = s.sectors.length;
    }
  }

  function toast(text, tone = 'info') {
    const el = document.createElement('div');
    el.className = 'toast tone-' + tone;
    el.textContent = text;
    const box = $('#toasts');
    box.appendChild(el);
    while (box.children.length > 4) {
      box.firstChild.remove();
    }
    setTimeout(() => el.classList.add('out'), 3800);
    setTimeout(() => el.remove(), 4400);
  }

  return {
    render,
    showOffline: (report) => modals.showOffline(report),
    reset: () => { ui.key = ''; ui.sector = null; ui.slotKeys = {}; ui.logSeq = -1; },
  };
}
