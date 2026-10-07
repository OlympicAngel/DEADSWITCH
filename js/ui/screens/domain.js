// Domain screens (Economy, Military, Research): inner tabs of buildings and the units they unlock.
// Lists show everything unlocked plus only the next locked entry, faded, so progress is visible but calm.
import {
  RESOURCES, FACTORS, BUILDINGS, BY_ID, ITEMS, SHOP_TABS, BALANCE,
} from '../../data.js';
import * as E from '../../engine.js';
import { num, time, pct, esc } from '../../format.js';
import { icon } from '../icons.js';
import {
  resTag, factorTag, costChips, setChips, reqText, named, clock, chanceClass,
} from '../common.js';
import { DOMAINS, sortedTabs } from '../layout.js';

export const BUY_MODES = [1, 10, 'max'];

export function renderDomain(s, ui, domain) {
  const d = DOMAINS[domain];
  const tabs = sortedTabs(s, domain);
  if (!tabs.some((x) => x.t.id === ui.inner[domain])) {
    ui.inner[domain] = tabs[0].t.id;
  }
  const tab = d.tabs.find((t) => t.id === ui.inner[domain]);
  const tabBar = tabs.map(({ t, st }) => `
    <button class="seg st-${st}" data-inner="${t.id}" aria-selected="${t.id === tab.id}">
      ${icon(st === 'locked' ? 'lock' : t.icon)}<span>${t.name}</span>
    </button>`).join('');
  const factors = d.factors.map((k) => `
    <div class="stat stat-${k}" data-tip="factor:${k}">${icon(k)}<b data-factor="${k}"></b><small>${FACTORS[k].name.replace('AI ', '')}</small></div>`).join('');
  const raidBar = domain === 'military' ? '<div class="raid-mini" data-raidmini hidden></div>' : '';
  return `
    <div class="screen domain dom-${domain}">
      <header class="screen-head">
        <h2>${icon(domain)}${d.title}</h2>
        <div class="stats">${factors}</div>
      </header>
      ${raidBar}
      <nav class="segs" role="tablist">${tabBar}</nav>
      <div class="list">${tab.kinds ? kindList(s, tab) : shopList(s, ui, tab)}</div>
    </div>`;
}

// ---------- buildings ----------

function visibleBuildings(s, kinds) {
  const all = BUILDINGS.filter((b) => kinds.includes(b.kind));
  const open = all.filter((b) => E.meetsReq(s, b.req));
  const next = all.filter((b) => !E.meetsReq(s, b.req)).sort((a, b) => (a.req.core || 0) - (b.req.core || 0))[0];
  return { open, next };
}

function kindList(s, tab) {
  const { open, next } = visibleBuildings(s, tab.kinds);
  return open.map((b) => buildingCard(s, b)).join('') + (next ? lockedCard(next.id, next.name, reqText(next.req)) : '');
}

function lockedCard(key, name, req) {
  return `
    <article class="card locked">
      <div class="tile">${icon(key)}<span class="lock">${icon('lock')}</span></div>
      <div class="body"><h3>${name}</h3><p class="req">${icon('lock')}Requires ${req}</p></div>
    </article>`;
}

function buildingCard(s, b) {
  const lvl = E.level(s, b.id);
  const max = E.maxLevel(s, b);
  const conv = b.kind === 'converter' && lvl > 0;
  return `
    <article class="card k-${b.kind}" data-card="${b.id}">
      <div class="card-top">
        <div class="tile" data-tip="text" data-tip-text="${esc(b.desc)}">${icon(b.id)}<span class="badge">${lvl}</span></div>
        <div class="body">
          <h3>${b.name} <small>Lv ${lvl}<span>/${max}</span></small></h3>
          <div class="effect">${effectHtml(s, b, lvl)}</div>
        </div>
        ${conv ? `<button class="icon-btn small ${s.paused[b.id] ? 'paused' : ''}" data-act="pause" data-id="${b.id}" aria-label="${s.paused[b.id] ? 'Resume' : 'Pause'}" aria-pressed="${!!s.paused[b.id]}">${icon(s.paused[b.id] ? 'play' : 'stop')}</button>` : ''}
      </div>
      ${lvl < max ? `<div class="card-bot"><div class="costs">${costChips(E.buildingCost(s, b))}</div>
        <button class="btn primary" data-act="build" data-id="${b.id}"><span data-l></span><small data-s></small></button></div>`
        : `<div class="card-bot"><p class="req">${b.id === 'core' ? 'Maximum level' : `${icon('core')}Raise the AI Core to go higher`}</p></div>`}
      <div class="progress"><i></i></div>
    </article>`;
}

function effectHtml(s, b, lvl) {
  const f = E.factors(s);
  const mult = (r) => E.prodMultiplier(s, r, f);
  const next = lvl + 1;
  if (b.kind === 'producer') {
    const [[r, per]] = Object.entries(b.produces);
    return `${resTag(r, per * lvl * mult(r), '+')}<span class="per">/s</span><span class="to">${icon('next')}${num(per * next * mult(r))}</span>`;
  }
  if (b.kind === 'converter') {
    const n = Math.max(1, lvl);
    const ins = Object.entries(b.consumes).map(([r, v]) => resTag(r, v * n, '−')).join('');
    const outs = Object.entries(b.produces).map(([r, v]) => resTag(r, v * n * mult(r), '+')).join('');
    return `${ins}<span class="arrow">${icon('next')}</span>${outs}<span class="per">/s</span>${lvl ? '<span class="eff" data-eff></span>' : ''}`;
  }
  if (b.kind === 'storage') {
    const [[r, m]] = Object.entries(b.storage);
    const base = r === 'energy' ? BALANCE.baseEnergyCap : BALANCE.basePopCap;
    return `<span class="lbl">Cap</span>${resTag(r, base * Math.pow(m, lvl))}<span class="to">${icon('next')}${num(base * Math.pow(m, next))}</span>`;
  }
  if (b.kind === 'core') {
    const cap = BALANCE.levelCapPerCoreLevel;
    const unlocks = BUILDINGS.filter((x) => x.req.core === next);
    return `<span class="lbl">Building cap</span><b>Lv ${lvl * cap}</b><span class="to">${icon('next')}${next * cap}</span>
      ${unlocks.length ? `<div class="unl">${icon('up')}Unlocks ${unlocks.map((x) => named(x.id, x.name)).join(' ')}</div>` : ''}`;
  }
  const shop = SHOP_TABS.find((t) => t.id === b.shop);
  const unlocks = ITEMS.filter((i) => i.req[b.id] === next);
  const disc = 1 - Math.pow(1 - BALANCE.unlockerDiscountPerLevel, Math.max(0, lvl - 1));
  if (!lvl) {
    return `<span class="lbl">Opens</span><b>${shop.name}</b>
      ${unlocks.length ? `<div class="unl">${icon('up')}Unlocks ${unlocks.map((x) => named(x.id, x.name)).join(' ')}</div>` : ''}`;
  }
  return `<span class="lbl">${shop.name} prices</span><b class="good-t">−${pct(disc)}</b>
    ${unlocks.length ? `<div class="unl">${icon('up')}Next level unlocks ${unlocks.map((x) => named(x.id, x.name)).join(' ')}</div>` : ''}`;
}

// ---------- shop ----------

function shopList(s, ui, tab) {
  const unlocker = BY_ID[tab.unlocker];
  const lvl = E.level(s, unlocker.id);
  const head = E.meetsReq(s, unlocker.req) ? buildingCard(s, unlocker) : lockedCard(unlocker.id, unlocker.name, reqText(unlocker.req));
  const items = ITEMS.filter((i) => i.tab === tab.shop);
  const open = lvl ? items.filter((i) => E.itemUnlocked(s, i)) : [];
  const next = items.find((i) => !E.itemUnlocked(s, i));
  const modes = BUY_MODES.map((m) => `<button data-mode="${m}" aria-pressed="${ui.buyMode === m}">${m === 'max' ? 'MAX' : '×' + m}</button>`).join('');
  const shopName = SHOP_TABS.find((t) => t.id === tab.shop).name;
  return `
    ${head}
    <div class="shop-head">
      <h3>${shopName}${tab.shop === 'staff' ? '<small>Staff die in lost battles</small>' : '<small>Each unit costs more than the last</small>'}</h3>
      ${open.length ? `<div class="modes">${modes}</div>` : ''}
    </div>
    ${!lvl ? `<p class="hint">${icon('info')}Build the ${named(unlocker.id, unlocker.name)} to unlock ${shopName}.</p>` : ''}
    ${open.map((i) => itemRow(s, i)).join('')}
    ${next ? lockedCard(next.id, next.name, reqText(next.req)) : ''}`;
}

function givesHtml(s, item) {
  if (item.bonus) {
    return Object.entries(item.bonus).map(([k, v]) => `<span class="tag t-${k}">${icon(k)}+${pct(v)}</span>`).join('') + '<span class="per">each</span>';
  }
  const f = E.factors(s);
  const raw = E.rawFactors(s);
  return Object.entries(item.gives).map(([k, v]) => factorTag(k, v * (raw[k] ? f[k] / raw[k] : 1))).join('') + '<span class="per">each</span>';
}

function itemRow(s, item) {
  const bonusDesc = item.bonus ? 'Permanent bonus, stacks with every unit.' : `Adds ${Object.entries(item.gives).map(([k, v]) => `${v} ${FACTORS[k].name}`).join(' and ')} per unit.`;
  return `
    <article class="card item" data-item="${item.id}">
      <div class="card-top">
        <div class="tile" data-tip="text" data-tip-text="${esc(bonusDesc)}">${icon(item.id)}<span class="badge">×${num(E.owned(s, item.id))}</span></div>
        <div class="body"><h3>${item.name}</h3><div class="effect">${givesHtml(s, item)}</div></div>
      </div>
      <div class="card-bot"><div class="costs">${costChips(E.itemCost(s, item, 1))}</div>
        <button class="btn primary" data-act="buy" data-id="${item.id}"><span data-l>Buy</span></button></div>
    </article>`;
}

// ---------- binding & per-frame ----------

export function bindDomain(panel) {
  return {
    cards: [...panel.querySelectorAll('[data-card]')].map((el) => ({
      b: BY_ID[el.dataset.card], el,
      btn: el.querySelector('[data-act="build"]'),
      label: el.querySelector('[data-l]'), sub: el.querySelector('[data-s]'),
      chips: [...el.querySelectorAll('.chip')],
      eff: el.querySelector('[data-eff]'),
      bar: el.querySelector('.progress i'),
    })),
    rows: [...panel.querySelectorAll('[data-item]')].map((el) => ({
      item: ITEMS.find((i) => i.id === el.dataset.item), el,
      btn: el.querySelector('[data-act="buy"]'), label: el.querySelector('[data-l]'),
      chips: [...el.querySelectorAll('.chip')],
    })),
    factors: [...panel.querySelectorAll('[data-factor]')],
    raid: panel.querySelector('[data-raidmini]'),
  };
}

export function buyCount(s, ui, item) {
  return ui.buyMode === 'max' ? Math.max(1, E.maxAffordable(s, item)) : ui.buyMode;
}

const LABELS = { storage: 'Need storage', busy: 'Builder busy', building: 'Building', maxed: 'Max' };

export function updateDomain(s, ui, refs, flows) {
  const f = E.factors(s);
  for (const el of refs.factors) {
    el.textContent = num(f[el.dataset.factor]);
  }
  if (refs.raid) {
    refs.raid.hidden = !s.raid;
    if (s.raid) {
      const p = E.raidChance(s);
      refs.raid.className = 'raid-mini odds-' + chanceClass(p);
      refs.raid.innerHTML = `${icon('alert')}<span>Raid in <b>${clock(s.raid.remaining)}</b></span><span>Strength <b>${num(s.raid.strength)}</b></span><span>Hold <b>${pct(p)}</b></span>`;
    }
  }
  for (const c of refs.cards) {
    const st = E.buildingStatus(s, c.b);
    c.el.dataset.status = st;
    if (c.btn) {
      setChips(s, c.chips, E.buildingCost(s, c.b));
      const lvl = E.level(s, c.b.id);
      c.btn.disabled = st !== 'ready';
      c.label.textContent = LABELS[st] || (lvl ? 'Upgrade' : 'Build');
      c.sub.textContent = st === 'building' ? clock(s.build.remaining) : st === 'ready' || st === 'poor' ? time(E.buildTime(s, c.b)) : '';
    }
    if (c.eff && flows) {
      const eff = flows.eff[c.b.id] ?? 0;
      c.eff.textContent = s.paused[c.b.id] ? 'PAUSED' : pct(eff);
      c.eff.className = 'eff ' + (s.paused[c.b.id] ? 'is-paused' : eff < 0.999 ? 'is-low' : 'is-ok');
    }
    c.bar.style.width = st === 'building' ? pct(1 - s.build.remaining / s.build.total) : '0';
  }
  for (const r of refs.rows) {
    const n = buyCount(s, ui, r.item);
    const cost = E.itemCost(s, r.item, n);
    setChips(s, r.chips, cost);
    const ok = E.canAfford(s, cost);
    r.btn.disabled = !ok;
    r.label.textContent = n > 1 ? `Buy ×${n}` : 'Buy';
    r.el.classList.toggle('affordable', ok);
  }
}
