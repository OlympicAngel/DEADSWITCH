// DOM layer. Structure is rebuilt only when something structural changes (levels, counts, tabs);
// numbers, affordability and progress are patched in place every frame through cached refs.
import {
  RESOURCES, RESOURCE_KEYS, FACTORS, FACTOR_KEYS, BUILDINGS, BY_ID, ITEMS, SHOP_TABS, RANKS, BALANCE,
} from './data.js';
import * as E from './engine.js';
import { num, rate, time, pct, esc } from './format.js';

const GROUPS = [
  { kind: 'core', title: 'Command' },
  { kind: 'producer', title: 'Production', hint: 'Generate resources for free.' },
  { kind: 'converter', title: 'Conversion', hint: 'Turn one resource into another. They throttle themselves when input runs dry or output is full.' },
  { kind: 'storage', title: 'Storage', hint: 'Energy and population are capped. Expand storage to afford bigger purchases.' },
  { kind: 'unlocker', title: 'Military', hint: 'Open the arsenal. Each level unlocks more and makes that tab cheaper.' },
];

const BUY_MODES = [1, 10, 'max'];

export function createUI(root, game) {
  const ui = {
    tab: 'base',
    shopTab: 'weapons',
    buyMode: 1,
    structureKey: '',
    cards: [],
    rows: [],
    logSeq: -1,
    newArsenal: false,
  };

  root.innerHTML = `
    <header class="top">
      <div class="brand">
        <span class="logo" aria-hidden="true">◉</span>
        <div>
          <h1>DEADSWITCH</h1>
          <div class="rank" id="rank"></div>
        </div>
      </div>
      <div class="threat" title="Threat = Power + Defense + Experts × ${BALANCE.threatExpertWeight}">
        <span class="label">Threat index</span>
        <b id="threat">0</b>
        <div class="bar"><i id="rankBar"></i></div>
        <span class="next" id="rankNext"></span>
      </div>
      <button class="icon-btn" id="menuBtn" aria-label="Menu">☰</button>
    </header>
    <section class="resources" id="resources">
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
        <div class="factor f-${f}" title="${esc(FACTORS[f].desc)}">
          <span class="ic">${FACTORS[f].icon}</span>
          <span class="f-name">${FACTORS[f].name}</span>
          <b id="factor-${f}">0</b>
        </div>`).join('')}
    </section>
    <section class="queue" id="queue"></section>
    <nav class="tabs" role="tablist">
      <button role="tab" data-tab="base">Base</button>
      <button role="tab" data-tab="arsenal">Arsenal<i class="dot" id="arsenalDot"></i></button>
      <button role="tab" data-tab="log" class="only-narrow">Log</button>
    </nav>
    <div class="layout">
      <main id="panel"></main>
      <aside class="side-log">
        <h2>System log</h2>
        <ol class="log" id="sideLog"></ol>
      </aside>
    </div>
    <div class="toasts" id="toasts" aria-live="polite"></div>
    <dialog id="modal"></dialog>
  `;

  const $ = (sel) => root.querySelector(sel);
  const panel = $('#panel');
  const modal = $('#modal');
  const resEls = Object.fromEntries(RESOURCE_KEYS.map((r) => {
    const el = $(`#res-${r}`);
    return [r, { el, v: el.querySelector('[data-v]'), cap: el.querySelector('[data-cap]'), rate: el.querySelector('[data-rate]'), fill: el.querySelector('[data-fill]') }];
  }));

  // ---------- events ----------

  root.addEventListener('click', (e) => {
    const t = e.target.closest('[data-act], [data-tab], [data-shop], [data-mode]');
    if (!t || t.disabled) {
      return;
    }
    if (t.dataset.tab) {
      ui.tab = t.dataset.tab;
      if (ui.tab === 'arsenal') {
        ui.newArsenal = false;
      }
    } else if (t.dataset.shop) {
      ui.shopTab = t.dataset.shop;
    } else if (t.dataset.mode) {
      ui.buyMode = t.dataset.mode === 'max' ? 'max' : Number(t.dataset.mode);
    } else {
      const act = t.dataset.act;
      const id = t.dataset.id;
      if (act === 'build') {
        game.act.build(id);
      } else if (act === 'cancel') {
        game.act.cancel();
      } else if (act === 'pause') {
        game.act.toggle(id);
      } else if (act === 'buy') {
        const item = ITEMS.find((i) => i.id === id);
        game.act.buy(id, buyCount(item));
      } else if (act === 'menu') {
        openMenu();
      }
    }
    render();
  });
  $('#menuBtn').dataset.act = 'menu';

  function buyCount(item) {
    if (ui.buyMode === 'max') {
      return Math.max(1, E.maxAffordable(game.state, item));
    }
    return ui.buyMode;
  }

  // ---------- structure ----------

  function render() {
    const s = game.state;
    const key = JSON.stringify([ui.tab, ui.shopTab, ui.buyMode, s.levels, s.items, s.paused, s.build && s.build.id]);
    root.querySelectorAll('[data-tab]').forEach((b) => b.setAttribute('aria-selected', String(b.dataset.tab === ui.tab)));
    root.dataset.tab = ui.tab;
    if (key !== ui.structureKey) {
      ui.structureKey = key;
      ui.cards = [];
      ui.rows = [];
      if (ui.tab === 'base') {
        panel.innerHTML = renderBase(s);
        bindCards();
      } else if (ui.tab === 'arsenal') {
        panel.innerHTML = renderArsenal(s);
        bindRows();
      } else {
        panel.innerHTML = '<ol class="log" id="mainLog"></ol>';
      }
      ui.logSeq = -1;
    }
    update();
  }

  function renderBase(s) {
    return GROUPS.map((g) => {
      const list = BUILDINGS.filter((b) => b.kind === g.kind);
      return `
        <section class="group">
          <header class="group-head"><h2>${g.title}</h2>${g.hint ? `<p>${g.hint}</p>` : ''}</header>
          <div class="grid">${list.map((b) => renderCard(s, b)).join('')}</div>
        </section>`;
    }).join('');
  }

  function reqText(req) {
    return Object.entries(req).map(([id, l]) => `${BY_ID[id].name} Lv ${l}`).join(', ');
  }

  function renderCard(s, b) {
    const lvl = E.level(s, b.id);
    if (!E.meetsReq(s, b.req)) {
      return `
        <article class="card locked">
          <div class="card-head"><span class="ic">${b.icon}</span><div><h3>${b.name}</h3><span class="lvl">Locked</span></div></div>
          <p class="req">Requires ${reqText(b.req)}</p>
        </article>`;
    }
    const max = E.maxLevel(s, b);
    const cost = E.buildingCost(s, b);
    return `
      <article class="card k-${b.kind}" data-card="${b.id}">
        <div class="card-head">
          <span class="ic">${b.icon}</span>
          <div><h3>${b.name}</h3><span class="lvl">Lv <b>${lvl}</b><span class="max"> / ${max}</span></span></div>
        </div>
        <p class="desc">${b.desc}</p>
        <div class="effect">${effectHtml(s, b, lvl)}</div>
        ${lvl < max ? `<div class="costs">${costChips(cost)}</div>` : ''}
        <div class="actions">
          <button class="btn primary" data-act="build" data-id="${b.id}"></button>
          ${b.kind === 'converter' && lvl > 0 ? `<button class="btn ghost" data-act="pause" data-id="${b.id}" aria-pressed="${!!s.paused[b.id]}">${s.paused[b.id] ? 'Resume' : 'Pause'}</button>` : ''}
        </div>
        <div class="progress"><i></i></div>
      </article>`;
  }

  function resTag(r, amount, sign = '') {
    return `<span class="tag t-${r}">${sign}${num(amount)} ${RESOURCES[r].icon}</span>`;
  }

  function effectHtml(s, b, lvl) {
    const f = E.factors(s);
    const mult = (r) => E.prodMultiplier(s, r, f);
    const nextLvl = lvl + 1;
    if (b.kind === 'producer') {
      const [[r, per]] = Object.entries(b.produces);
      return `<span class="row"><span>Output</span><span>${resTag(r, per * lvl * mult(r), '+')}/s <em>→ ${num(per * nextLvl * mult(r))}</em></span></span>`;
    }
    if (b.kind === 'converter') {
      const ins = Object.entries(b.consumes).map(([r, v]) => resTag(r, v * Math.max(1, lvl), '−')).join(' ');
      const outs = Object.entries(b.produces).map(([r, v]) => resTag(r, v * Math.max(1, lvl) * mult(r), '+')).join(' ');
      return `<span class="row"><span>${lvl ? 'Per second' : 'At Lv 1'}</span><span>${ins} → ${outs}</span></span>
        ${lvl ? '<span class="row"><span>Running</span><span class="eff" data-eff></span></span>' : ''}`;
    }
    if (b.kind === 'storage') {
      const [[r, m]] = Object.entries(b.storage);
      const base = r === 'energy' ? BALANCE.baseEnergyCap : BALANCE.basePopCap;
      return `<span class="row"><span>${RESOURCES[r].name} cap</span><span>${num(base * Math.pow(m, lvl))} <em>→ ${num(base * Math.pow(m, nextLvl))}</em></span></span>`;
    }
    if (b.kind === 'core') {
      const cap = BALANCE.levelCapPerCoreLevel;
      const unlocks = BUILDINGS.filter((x) => x.req.core === nextLvl).map((x) => x.name);
      return `<span class="row"><span>Building cap</span><span>Lv ${lvl * cap} <em>→ ${nextLvl * cap}</em></span></span>
        ${unlocks.length ? `<span class="row"><span>Next unlocks</span><span class="unl">${unlocks.join(', ')}</span></span>` : ''}`;
    }
    const tab = SHOP_TABS.find((t) => t.id === b.shop);
    const unlocks = ITEMS.filter((i) => i.req[b.id] === nextLvl).map((i) => i.name);
    const disc = 1 - Math.pow(1 - BALANCE.unlockerDiscountPerLevel, Math.max(0, lvl - 1));
    return `<span class="row"><span>${tab.name} prices</span><span>−${pct(disc)}</span></span>
      ${unlocks.length ? `<span class="row"><span>Next unlocks</span><span class="unl">${unlocks.join(', ')}</span></span>` : ''}`;
  }

  function costChips(cost) {
    return Object.entries(cost).map(([r, v]) =>
      `<span class="chip c-${r}" data-res="${r}" data-amt="${v}"><span class="ic">${RESOURCES[r].icon}</span><span data-t>${num(v)}</span></span>`).join('');
  }

  function bindCards() {
    panel.querySelectorAll('[data-card]').forEach((el) => {
      const b = BY_ID[el.dataset.card];
      ui.cards.push({
        b, el,
        btn: el.querySelector('[data-act="build"]'),
        chips: [...el.querySelectorAll('.chip')],
        eff: el.querySelector('[data-eff]'),
        bar: el.querySelector('.progress i'),
      });
    });
  }

  function renderArsenal(s) {
    const tabs = SHOP_TABS.map((t) => {
      const open = E.level(s, t.unlocker) > 0;
      return `<button data-shop="${t.id}" aria-selected="${t.id === ui.shopTab}" class="${open ? '' : 'is-locked'}">${open ? '' : '<span aria-hidden="true">🔒 </span>'}${t.name}</button>`;
    }).join('');
    const tab = SHOP_TABS.find((t) => t.id === ui.shopTab);
    const unlocker = BY_ID[tab.unlocker];
    let body;
    if (!E.level(s, tab.unlocker)) {
      body = `<div class="empty"><span class="ic">${unlocker.icon}</span><p>Build the <b>${unlocker.name}</b> to open ${tab.name}.</p>
        ${E.meetsReq(s, unlocker.req) ? '<button class="btn" data-tab="base">Go to Base</button>' : `<p class="req">Requires ${reqText(unlocker.req)}</p>`}</div>`;
    } else {
      body = ITEMS.filter((i) => i.tab === tab.id).map((i) => renderItem(s, i)).join('');
    }
    const modes = BUY_MODES.map((m) => `<button data-mode="${m}" aria-pressed="${ui.buyMode === m}">${m === 'max' ? 'Max' : '×' + m}</button>`).join('');
    return `
      <div class="shop-tabs">${tabs}</div>
      <div class="shop-bar"><p>Every unit costs more than the last. ${unlocker.name} levels cut prices here.</p><div class="modes">${modes}</div></div>
      <div class="items">${body}</div>`;
  }

  function givesText(s, item) {
    if (item.bonus) {
      return Object.entries(item.bonus).map(([k, v]) => {
        const name = RESOURCES[k] ? RESOURCES[k].name + ' production' : FACTORS[k].name;
        return `+${pct(v)} ${name}`;
      }).join(', ');
    }
    return Object.entries(item.gives).map(([k, v]) => `<span class="ft ft-${k}">+${num(v * (1 + E.techBonus(s, k)))} ${FACTORS[k].name.replace('AI ', '')}</span>`).join(' ');
  }

  function renderItem(s, item) {
    if (!E.itemUnlocked(s, item)) {
      return `<div class="item locked"><span class="ic">${item.icon}</span><div class="item-main"><h3>${item.name}</h3><p class="req">Requires ${reqText(item.req)}</p></div></div>`;
    }
    const n = E.owned(s, item.id);
    return `
      <div class="item" data-item="${item.id}">
        <span class="ic">${item.icon}</span>
        <div class="item-main">
          <h3>${item.name} <span class="owned">×${n}</span></h3>
          <p class="gives">${givesText(s, item)} <span class="each">each</span></p>
          <div class="costs">${costChips(E.itemCost(s, item, 1))}</div>
        </div>
        <button class="btn primary buy" data-act="buy" data-id="${item.id}">Buy</button>
      </div>`;
  }

  function bindRows() {
    panel.querySelectorAll('[data-item]').forEach((el) => {
      ui.rows.push({
        item: ITEMS.find((i) => i.id === el.dataset.item),
        el,
        btn: el.querySelector('.buy'),
        chips: [...el.querySelectorAll('.chip')],
      });
    });
  }

  // ---------- per-frame patching ----------

  function setChips(s, chips, cost) {
    const c = E.caps(s);
    for (const chip of chips) {
      const r = chip.dataset.res;
      const v = cost[r];
      chip.querySelector('[data-t]').textContent = num(v);
      chip.classList.toggle('short', s.res[r] + 1e-9 < v);
      chip.classList.toggle('over', v > c[r]);
      chip.title = v > c[r] ? `Exceeds your ${RESOURCES[r].name} cap. Expand storage.` : '';
    }
  }

  function update() {
    const s = game.state;
    const flows = game.flows;
    const c = E.caps(s);
    for (const r of RESOURCE_KEYS) {
      const el = resEls[r];
      el.v.textContent = num(s.res[r]);
      el.cap.textContent = Number.isFinite(c[r]) ? ' / ' + num(c[r]) : '';
      const net = flows ? flows.prod[r] - flows.cons[r] : 0;
      const full = Number.isFinite(c[r]) && s.res[r] >= c[r] * 0.999;
      el.rate.textContent = full ? 'FULL' : rate(net);
      el.rate.className = 'res-rate ' + (full ? 'is-full' : net < -1e-9 ? 'is-neg' : '');
      el.fill.style.width = Number.isFinite(c[r]) ? pct(s.res[r] / c[r]) : '0';
    }
    const f = E.factors(s);
    for (const k of FACTOR_KEYS) {
      root.querySelector(`#factor-${k}`).textContent = num(f[k]);
    }
    const t = E.threat(s, f);
    const ri = E.rankIndex(t);
    $('#threat').textContent = num(t);
    $('#rank').textContent = RANKS[ri].title;
    const next = RANKS[ri + 1];
    $('#rankBar').style.width = next ? pct((t - RANKS[ri].at) / (next.at - RANKS[ri].at)) : '100%';
    $('#rankNext').textContent = next ? `Next: ${next.title} at ${num(next.at)}` : 'Maximum threat';

    updateQueue(s);

    for (const card of ui.cards) {
      const st = E.buildingStatus(s, card.b);
      const lvl = E.level(s, card.b.id);
      setChips(s, card.chips, E.buildingCost(s, card.b));
      const verb = lvl ? 'Upgrade' : 'Build';
      const labels = {
        ready: `${verb} · ${time(E.buildTime(s, card.b))}`,
        poor: `${verb} · ${time(E.buildTime(s, card.b))}`,
        storage: 'Expand storage',
        busy: 'Builder busy',
        building: 'Building…',
        maxed: card.b.id === 'core' ? 'Max level' : 'Upgrade AI Core',
      };
      card.btn.textContent = labels[st];
      card.btn.disabled = st !== 'ready';
      card.el.dataset.status = st;
      if (card.eff && flows) {
        const eff = flows.eff[card.b.id] ?? 0;
        card.eff.textContent = s.paused[card.b.id] ? 'Paused' : pct(eff);
        card.eff.className = 'eff ' + (s.paused[card.b.id] ? 'is-paused' : eff < 0.999 ? 'is-low' : 'is-ok');
      }
      if (st === 'building') {
        card.bar.style.width = pct(1 - s.build.remaining / s.build.total);
      }
    }

    for (const row of ui.rows) {
      const count = ui.buyMode === 'max' ? Math.max(1, E.maxAffordable(s, row.item)) : ui.buyMode;
      const cost = E.itemCost(s, row.item, count);
      setChips(s, row.chips, cost);
      const ok = E.canAfford(s, cost);
      row.btn.disabled = !ok;
      row.btn.textContent = count > 1 ? `Buy ×${count}` : 'Buy';
      row.el.classList.toggle('affordable', ok);
    }

    updateLog(s);
    $('#arsenalDot').hidden = !ui.newArsenal;
  }

  function updateQueue(s) {
    const q = $('#queue');
    if (!s.build) {
      if (q.dataset.state !== 'idle') {
        q.dataset.state = 'idle';
        q.innerHTML = '<span class="q-label">Builder idle</span><span class="q-hint">Pick an upgrade in Base.</span>';
      }
      return;
    }
    const b = BY_ID[s.build.id];
    if (q.dataset.state !== b.id) {
      q.dataset.state = b.id;
      q.innerHTML = `<span class="q-label">Building</span><span class="q-name">${b.icon} ${b.name} → Lv ${E.level(s, b.id) + 1}</span>
        <span class="q-time" data-qt></span><div class="bar"><i data-qb></i></div>
        <button class="btn ghost small" data-act="cancel" title="Cancel and refund">Cancel</button>`;
    }
    q.querySelector('[data-qt]').textContent = time(s.build.remaining);
    q.querySelector('[data-qb]').style.width = pct(1 - s.build.remaining / s.build.total);
  }

  function updateLog(s) {
    if (ui.logSeq === s.lineSeq) {
      return;
    }
    const fresh = ui.logSeq < 0 ? [] : s.log.slice(-(s.lineSeq - ui.logSeq));
    ui.logSeq = s.lineSeq;
    const html = s.log.slice().reverse().map((l) => `<li class="tone-${l.tone}"><time>${time(l.t)}</time><span>${esc(l.text)}</span></li>`).join('');
    root.querySelectorAll('.log').forEach((el) => { el.innerHTML = html; });
    for (const l of fresh) {
      if (l.tone !== 'info') {
        toast(l.text, l.tone);
        if (l.tone === 'item' && ui.tab !== 'arsenal') {
          ui.newArsenal = true;
        }
      }
    }
  }

  function toast(text, tone = 'info') {
    const el = document.createElement('div');
    el.className = 'toast tone-' + tone;
    el.textContent = text;
    $('#toasts').appendChild(el);
    setTimeout(() => el.classList.add('out'), 3600);
    setTimeout(() => el.remove(), 4200);
  }

  // ---------- dialogs ----------

  function openModal(html) {
    modal.innerHTML = `<div class="modal-body">${html}</div>`;
    modal.showModal();
    modal.querySelectorAll('[data-close]').forEach((b) => b.addEventListener('click', () => modal.close()));
  }

  function showOffline(report) {
    const gains = RESOURCE_KEYS.map((r) => `<li><span class="ic t-${r}">${RESOURCES[r].icon}</span> ${RESOURCES[r].name}<b>${report.gained[r] >= 0 ? '+' : '−'}${num(Math.abs(report.gained[r]))}</b></li>`).join('');
    openModal(`
      <h2>While you were away</h2>
      <p class="muted">${time(report.seconds)} passed. The base kept running.</p>
      <ul class="gains">${gains}</ul>
      <button class="btn primary wide" data-close>Resume</button>`);
  }

  function openMenu() {
    openModal(`
      <h2>Settings</h2>
      <p class="muted">Progress saves to this browser every ${BALANCE.autosaveSeconds}s and when you leave.</p>
      <div class="menu-actions">
        <button class="btn" id="mSave">Save now</button>
        <button class="btn" id="mExport">Export save</button>
        <button class="btn" id="mImport">Import save</button>
        <button class="btn danger" id="mReset">Wipe progress</button>
      </div>
      <textarea id="mText" rows="4" placeholder="Save code" spellcheck="false" hidden></textarea>
      <p class="muted" id="mMsg"></p>
      <button class="btn primary wide" data-close>Close</button>`);
    const msg = modal.querySelector('#mMsg');
    const text = modal.querySelector('#mText');
    modal.querySelector('#mSave').onclick = () => { game.act.save(); msg.textContent = 'Saved.'; };
    modal.querySelector('#mExport').onclick = async () => {
      text.hidden = false;
      text.value = game.act.exportSave();
      text.select();
      try {
        await navigator.clipboard.writeText(text.value);
        msg.textContent = 'Copied to clipboard.';
      } catch {
        msg.textContent = 'Copy the code above.';
      }
    };
    modal.querySelector('#mImport').onclick = () => {
      if (text.hidden || !text.value.trim()) {
        text.hidden = false;
        text.value = '';
        text.focus();
        msg.textContent = 'Paste a save code, then press Import again.';
        return;
      }
      msg.textContent = game.act.importSave(text.value.trim()) ? 'Imported.' : 'That code is not a valid save.';
      ui.structureKey = '';
      render();
    };
    let armed = false;
    modal.querySelector('#mReset').onclick = (e) => {
      if (!armed) {
        armed = true;
        e.target.textContent = 'Tap again to wipe';
        return;
      }
      game.act.reset();
      ui.structureKey = '';
      modal.close();
      render();
    };
  }

  return { render, update, toast, showOffline };
}
