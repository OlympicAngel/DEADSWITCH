// Domain screens (Economy, Military, Research): inner tabs of buildings and the units they unlock.
// Lists show everything unlocked plus only the next locked entry, faded, so progress is visible but calm.
import {
  RESOURCES, FACTORS, BUILDINGS, BY_ID, ITEMS, SHOP_TABS, BALANCE,
} from '../../data.js';
import * as E from '../../engine.js';
import { num, time, pct, esc } from '../../format.js';
import { icon, labeled } from '../icons.js';
import {
  resTag, factorTag, costChips, setChips, reqText, named, clock, chanceClass, bonusText,
} from '../common.js';
import { DOMAINS, sortedTabs, tabReq, readyCount } from '../layout.js';
import { put, putHtml, setCls, setData, setW } from '../dom.js';
import { onScreen } from '../onscreen.js';

export const BUY_MODES = [1, 10, 'max'];
const SHOP_ICONS = { weapons: 'power', defenses: 'defense', staff: 'militia', experts: 'experts', tech: 'lab' };

export function renderDomain(s, ui, domain) {
  const d = DOMAINS[domain];
  const tabs = sortedTabs(s, domain);
  if (!tabs.some((x) => x.t.id === ui.inner[domain])) {
    ui.inner[domain] = tabs[0].t.id;
  }
  const tab = d.tabs.find((t) => t.id === ui.inner[domain]);
  const tabBar = tabs.map(({ t, st }) => `
    <button class="seg st-${st}" data-inner="${t.id}" aria-selected="${t.id === tab.id}" ${st === 'locked' ? `data-req="${tabReq(s, t)}" aria-disabled="true"` : ''}>
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
  return open.map((b) => buildingCard(s, b)).join('') + (next ? lockedCard(next.id, next.name, next.req) : '');
}

// Locked entries link to whatever they wait on: tapping one jumps there and highlights it.
function lockedCard(key, name, req) {
  const [reqId] = Object.keys(req);
  return `
    <article class="card locked" data-req="${reqId}" role="button" tabindex="0">
      <div class="tile">${icon(key)}<span class="lock">${icon('lock')}</span></div>
      <div class="body"><h3>${name}</h3><p class="req">${icon('lock')}Requires ${reqText(req)}</p></div>
      <span class="req-go">${icon('next')}</span>
    </article>`;
}

function buildingCard(s, b) {
  const lvl = E.level(s, b.id);
  const max = E.maxLevel(s, b);
  const conv = b.kind === 'converter' && lvl > 0;
  const core = b.kind === 'core';
  const unbuilt = lvl === 0;
  const res = Object.keys(b.produces || b.storage || {})[0]; // tints the card by the resource it makes or stores
  return `
    <article class="card k-${b.kind} ${unbuilt ? 'unbuilt' : ''} ${conv && s.paused[b.id] ? 'switched-off' : ''}" data-card="${b.id}" ${res ? `data-res="${res}"` : ''}>
      ${unbuilt ? `<span class="unbuilt-tag">${icon('unlock')}Not built</span>` : ''}
      ${b.kind === 'unlocker' ? `<span class="facility-tag">${icon('economy')}Facility</span>` : ''}
      ${core ? '<div class="core-glow"></div>' : ''}
      <div class="card-top">
        <div class="tile">${core ? '<i class="tile-ring"></i>' : ''}${icon(b.id)}${unbuilt ? `<span class="badge lockb">${icon('unlock')}</span>` : `<span class="badge">${lvl}</span>`}</div>
        <div class="body">
          <h3>${b.name} ${unbuilt ? '' : `<small>Lv ${lvl}<span>/${max}</span></small>`}</h3>
          <p class="desc">${esc(b.desc)}</p>
        </div>
        ${conv ? `<button class="power-switch ${s.paused[b.id] ? 'off' : 'on'}" data-act="pause" data-id="${b.id}" role="switch" aria-checked="${!s.paused[b.id]}" aria-label="${b.name} power">
          <span class="ps-track"><i class="ps-knob"></i></span><span class="ps-label">${s.paused[b.id] ? 'OFF' : 'ON'}</span></button>` : ''}
      </div>
      ${effectHtml(s, b, lvl, max)}
      ${lvl < max ? `<div class="card-bot"><div class="costs">${costChips(E.buildingCost(s, b))}</div>
        <button class="btn primary ${unbuilt ? 'unlock' : ''}" data-act="build" data-id="${b.id}">${unbuilt ? icon('unlock') : ''}<span data-l></span><small data-s></small></button></div>`
        : `<div class="card-bot"><p class="req">${core ? 'Maximum level' : `${icon('core')}Level capped by the AI Core`}</p></div>`}
      <div class="progress"><i></i></div>
    </article>`;
}

// Current level vs next level, side by side.
function nowNext(label, now, next, lvl, max, extra = '') {
  if (!lvl) {
    return `<div class="nn"><span class="nn-lbl">${label}</span><div class="nn-col nxt"><small>On unlock</small><b>${next}</b></div></div>${extra}`;
  }
  const showNext = lvl < max;
  return `
    <div class="nn">
      <span class="nn-lbl">${label}</span>
      <div class="nn-col"><small>Now</small><b>${now}</b></div>
      ${showNext ? `<span class="nn-arrow">${icon('next')}</span><div class="nn-col nxt"><small>Next</small><b>${next}</b></div>` : ''}
    </div>${extra}`;
}

const unlockLine = (list, lvl) => (list.length ? `<div class="unl">${icon('up')}${lvl ? 'Lv up unlocks' : 'Includes'} ${list.map((x) => named(x.id, x.name)).join(' ')}</div>` : '');

function effectHtml(s, b, lvl, max) {
  const f = E.factors(s);
  const mult = (r) => E.prodMultiplier(s, r, f);
  const next = lvl + 1;
  if (b.kind === 'producer') {
    const [[r, per]] = Object.entries(b.produces);
    return nowNext('Output', lvl ? `${resTag(r, per * lvl * mult(r), '+')}<em>/s</em>` : '—', `${resTag(r, per * next * mult(r), '+')}<em>/s</em>`, lvl, max);
  }
  if (b.kind === 'converter') {
    const cm = mult(Object.keys(b.produces)[0]);
    const flow = (n) => Object.entries(b.consumes).map(([r, v]) => resTag(r, v * n * cm, '−')).join('')
      + `<span class="arrow">${icon('next')}</span>`
      + Object.entries(b.produces).map(([r, v]) => resTag(r, v * n * mult(r), '+')).join('') + '<em>/s</em>';
    return `<div class="nn conv">
        <span class="nn-lbl">Per second</span>
        ${lvl ? `<div class="nn-row"><small>Now</small><b>${flow(lvl)}</b><span class="eff" data-eff></span></div>` : ''}
        ${lvl < max ? `<div class="nn-row nxt"><small>${lvl ? 'Next' : 'Unlock'}</small><b>${flow(next)}</b></div>` : ''}
      </div>`;
  }
  if (b.kind === 'storage') {
    const [[r, m]] = Object.entries(b.storage);
    const base = r === 'energy' ? BALANCE.baseEnergyCap : BALANCE.basePopCap;
    return nowNext(`${RESOURCES[r].name} cap`, resTag(r, base * Math.pow(m, lvl)), resTag(r, base * Math.pow(m, next)), lvl, max);
  }
  if (b.kind === 'offline') {
    const away = (n) => { const o = E.offlineLimits(s, n); return `${icon('clock')}${time(o.seconds)}<em>at</em>${pct(o.efficiency)}`; };
    return nowNext('Time away', away(lvl), away(next), lvl, max);
  }
  if (b.kind === 'core') {
    const cap = BALANCE.levelCapPerCoreLevel;
    const gate = lvl < max ? `
      <div class="gate" data-tip="text" data-tip-text="Average level of unlocked production and storage buildings, counting unbuilt ones as 0.">
        <div class="gate-row"><span class="nn-lbl">Base development</span><b data-gate-v></b></div>
        <div class="gate-bar"><i data-gate-bar></i></div>
      </div>` : '';
    return nowNext('Building cap', `Lv ${lvl * cap}`, `Lv ${next * cap}`, lvl, max, gate + unlockLine(BUILDINGS.filter((x) => x.req.core === next), lvl));
  }
  const shop = SHOP_TABS.find((t) => t.id === b.shop);
  const disc = (l) => 1 - Math.pow(1 - BALANCE.unlockerDiscountPerLevel, Math.max(0, l - 1));
  const unl = unlockLine(ITEMS.filter((i) => i.req[b.id] === next), lvl);
  if (!lvl) {
    return nowNext('Opens', '', `${icon(SHOP_ICONS[b.shop] || 'hex')}${shop.name}`, lvl, max, unl);
  }
  return nowNext(`${shop.name} prices`, lvl ? `<span class="good-t">−${pct(disc(lvl))}</span>` : 'Closed', `<span class="good-t">−${pct(disc(next))}</span>`, lvl, max, unl);
}

// ---------- shop ----------

function shopList(s, ui, tab) {
  const unlocker = BY_ID[tab.unlocker];
  const lvl = E.level(s, unlocker.id);
  const head = E.meetsReq(s, unlocker.req) ? buildingCard(s, unlocker) : lockedCard(unlocker.id, unlocker.name, unlocker.req);
  const items = ITEMS.filter((i) => i.tab === tab.shop);
  const open = lvl ? items.filter((i) => E.itemUnlocked(s, i)) : [];
  const next = items.find((i) => !E.itemUnlocked(s, i));
  const modes = BUY_MODES.map((m) => `<button data-mode="${m}" aria-pressed="${ui.buyMode === m}">${m === 'max' ? 'MAX' : '×' + m}</button>`).join('');
  const shopName = SHOP_TABS.find((t) => t.id === tab.shop).name;
  return `
    ${head}
    <div class="shop-head">
      <h3>${shopName}</h3>
      ${open.length ? `<div class="modes">${modes}</div>` : ''}
    </div>
    ${tab.shop === 'staff' && open.length ? `<p class="shop-note ${E.troopsOut(s) ? 'on' : ''}">${icon('defense')}${E.troopsOut(s)
      ? `Out on the operation: \u2212${num(E.troopDefense(s))} Defense until they are back.`
      : 'Troops add no Defense while an operation is running.'}</p>` : ''}
    ${open.map((i) => itemRow(s, i)).join('')}
    ${next ? lockedCard(next.id, next.name, next.req) : ''}`;
}

// Gain for the selected purchase size: "+4.1 each" at ×1, "+41 for ×10" otherwise.
function givesHtml(s, item, n = 1) {
  const per = n > 1 ? `<span class="per">for ×${n}</span>` : '<span class="per">each</span>';
  if (item.bonus) {
    return Object.entries(item.bonus).map(([k, v]) => `<span class="tag t-${k}">${labeled(k)}+${pct(v * n)}</span>`).join('') + per;
  }
  const f = E.factors(s);
  const raw = E.rawFactors(s);
  return Object.entries(item.gives).map(([k, v]) => factorTag(k, Math.round(v * n * (raw[k] ? f[k] / raw[k] : 1)))).join('') + per;
}

function itemRow(s, item) {
  return `
    <article class="card item" data-item="${item.id}">
      <div class="card-top">
        <div class="tile">${icon(item.id)}<span class="badge">×${num(E.owned(s, item.id))}</span></div>
        <div class="body"><h3>${item.name}</h3><div class="effect" data-gives>${givesHtml(s, item)}</div>${item.durability ? `<span class="tag t-dur" data-tip="text" data-tip-text="Losses of this unit in defeats are ${pct(item.durability)} lower.">${icon('durability')}${pct(item.durability)} durability</span>` : ''}</div>
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
      btn: el.querySelector('[data-act="buy"]'), label: el.querySelector('[data-l]'), gives: el.querySelector('[data-gives]'), n: 1,
      chips: [...el.querySelectorAll('.chip')],
    })),
    factors: [...panel.querySelectorAll('[data-factor]')],
    raid: panel.querySelector('[data-raidmini]'),
    segs: [...panel.querySelectorAll('.seg[data-inner]')],
  };
}

const STORE = { energy: 'battery', pop: 'habitat' };

// A cost above the storage cap turns the button into a link to the storage building that raises it.
function setStorageLink(s, btn, cost, normalAct) {
  const res = cost && E.exceedsCap(s, cost)[0];
  if (res && STORE[res]) {
    btn.dataset.act = 'storage';
    btn.dataset.target = STORE[res];
    btn.classList.add('to-storage');
  } else {
    btn.dataset.act = normalAct;
    btn.classList.remove('to-storage');
  }
}

export function buyCount(s, ui, item) {
  return ui.buyMode === 'max' ? Math.max(1, E.maxAffordable(s, item)) : ui.buyMode;
}

const LABELS = { storage: 'Need storage', busy: 'Builder busy', building: 'Building', maxed: 'Max', gated: 'Locked' };

export function updateDomain(s, ui, refs, flows) {
  const tabs = DOMAINS[ui.screen].tabs;
  for (const seg of refs.segs) {
    const t = tabs.find((x) => x.id === seg.dataset.inner);
    seg.classList.toggle('has-ready', readyCount(s, t) > 0);
  }
  const f = E.factors(s);
  for (const el of refs.factors) {
    put(el, num(f[el.dataset.factor]));
    // Defense here is short by whatever marched out; the tooltip says how much.
    el.parentElement.classList.toggle('away', el.dataset.factor === 'defense' && E.troopsOut(s));
  }
  if (refs.raid) {
    const atk = E.nextAttack(s);
    refs.raid.hidden = !atk;
    if (atk) {
      const p = E.raidChance(s, atk);
      setCls(refs.raid, 'raid-mini odds-' + chanceClass(p));
      putHtml(refs.raid, `${icon('alert')}<span>${esc(E.attackName(atk))} in <b>${clock(atk.remaining)}</b></span><span>Strength <b>${num(atk.strength)}</b></span><span>Hold <b>${pct(p)}</b></span>`);
    }
  }
  for (const c of refs.cards) {
    if (!onScreen(c.el)) continue;
    const st = E.buildingStatus(s, c.b);
    setData(c.el, 'status', st);
    if (c.btn) {
      setChips(s, c.chips, E.buildingCost(s, c.b));
      const lvl = E.level(s, c.b.id);
      c.btn.disabled = st !== 'ready' && st !== 'storage';
      setStorageLink(s, c.btn, st === 'storage' ? E.buildingCost(s, c.b) : null, 'build');
      put(c.label, LABELS[st] || (lvl ? 'Upgrade' : 'Unlock'));
      put(c.sub, st === 'building' ? clock(s.build.remaining) : st === 'ready' || st === 'poor' ? time(E.buildTime(s, c.b)) : '');
    }
    if (c.eff && flows) {
      const eff = flows.eff[c.b.id] ?? 0;
      put(c.eff, s.paused[c.b.id] ? 'OFF' : pct(eff));
      setCls(c.eff, 'eff ' + (s.paused[c.b.id] ? 'is-paused' : eff < 0.999 ? 'is-low' : 'is-ok'));
    }
    setW(c.bar, st === 'building' ? pct(1 - s.build.remaining / s.build.total) : '0');
    const gv = c.el.querySelector('[data-gate-v]');
    if (gv) {
      const g = E.coreGate(s);
      put(gv, `Avg Lv ${g.avg.toFixed(1)} / ${g.need.toFixed(1)}`);
      setCls(gv, g.open ? 'good-t' : 'warn-t');
      setW(c.el.querySelector('[data-gate-bar]'), pct(Math.min(1, g.avg / g.need)));
      setCls(c.el.querySelector('[data-gate-bar]'), g.open ? 'bg-good' : '');
    }
  }
  for (const r of refs.rows) {
    if (!onScreen(r.el)) continue;
    const n = buyCount(s, ui, r.item);
    if (n !== r.n) {
      r.n = n;
      putHtml(r.gives, givesHtml(s, r.item, n));
    }
    const cost = E.itemCost(s, r.item, n);
    setChips(s, r.chips, cost);
    const ok = E.canAfford(s, cost);
    const over = !ok && E.exceedsCap(s, cost).length > 0;
    r.btn.disabled = !ok && !over;
    put(r.label, over ? 'Need storage' : n > 1 ? `Buy ×${n}` : 'Buy');
    setStorageLink(s, r.btn, over ? cost : null, 'buy');
    r.el.classList.toggle('affordable', ok);
  }
}
