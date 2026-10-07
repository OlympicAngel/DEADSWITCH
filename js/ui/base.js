// Base tab: buildings grouped by role.
import { RESOURCES, BUILDINGS, BY_ID, ITEMS, SHOP_TABS, BALANCE } from '../data.js';
import * as E from '../engine.js';
import { num, time, pct } from '../format.js';
import { resTag, reqText, costChips, setChips } from './common.js';

const GROUPS = [
  { kind: 'core', title: 'Command' },
  { kind: 'producer', title: 'Production', hint: 'Generate resources for free.' },
  { kind: 'converter', title: 'Conversion', hint: 'Turn one resource into another. They throttle themselves when input runs dry or output is full.' },
  { kind: 'storage', title: 'Storage', hint: 'Energy and population are capped. Expand storage to afford bigger purchases.' },
  { kind: 'unlocker', title: 'Military', hint: 'Open the arsenal. Each level unlocks more and makes that tab cheaper.' },
];

export function renderBase(s) {
  return GROUPS.map((g) => `
    <section class="group">
      <header class="group-head"><h2>${g.title}</h2>${g.hint ? `<p>${g.hint}</p>` : ''}</header>
      <div class="grid">${BUILDINGS.filter((b) => b.kind === g.kind).map((b) => renderCard(s, b)).join('')}</div>
    </section>`).join('');
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
  return `
    <article class="card k-${b.kind}" data-card="${b.id}">
      <div class="card-head">
        <span class="ic">${b.icon}</span>
        <div><h3>${b.name}</h3><span class="lvl">Lv <b>${lvl}</b><span class="max"> / ${max}</span></span></div>
      </div>
      <p class="desc">${b.desc}</p>
      <div class="effect">${effectHtml(s, b, lvl)}</div>
      ${lvl < max ? `<div class="costs">${costChips(E.buildingCost(s, b))}</div>` : ''}
      <div class="actions">
        <button class="btn primary" data-act="build" data-id="${b.id}"></button>
        ${b.kind === 'converter' && lvl > 0 ? `<button class="btn ghost" data-act="pause" data-id="${b.id}" aria-pressed="${!!s.paused[b.id]}">${s.paused[b.id] ? 'Resume' : 'Pause'}</button>` : ''}
      </div>
      <div class="progress"><i></i></div>
    </article>`;
}

function effectHtml(s, b, lvl) {
  const f = E.factors(s);
  const mult = (r) => E.prodMultiplier(s, r, f);
  const next = lvl + 1;
  if (b.kind === 'producer') {
    const [[r, per]] = Object.entries(b.produces);
    return `<span class="row"><span>Output</span><span>${resTag(r, per * lvl * mult(r), '+')}/s <em>→ ${num(per * next * mult(r))}</em></span></span>`;
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
    return `<span class="row"><span>${RESOURCES[r].name} cap</span><span>${num(base * Math.pow(m, lvl))} <em>→ ${num(base * Math.pow(m, next))}</em></span></span>`;
  }
  if (b.kind === 'core') {
    const cap = BALANCE.levelCapPerCoreLevel;
    const unlocks = BUILDINGS.filter((x) => x.req.core === next).map((x) => x.name);
    return `<span class="row"><span>Building cap</span><span>Lv ${lvl * cap} <em>→ ${next * cap}</em></span></span>
      ${unlocks.length ? `<span class="row"><span>Next unlocks</span><span class="unl">${unlocks.join(', ')}</span></span>` : ''}`;
  }
  const tab = SHOP_TABS.find((t) => t.id === b.shop);
  const unlocks = ITEMS.filter((i) => i.req[b.id] === next).map((i) => i.name);
  const disc = 1 - Math.pow(1 - BALANCE.unlockerDiscountPerLevel, Math.max(0, lvl - 1));
  return `<span class="row"><span>${tab.name} prices</span><span>−${pct(disc)}</span></span>
    ${unlocks.length ? `<span class="row"><span>Next unlocks</span><span class="unl">${unlocks.join(', ')}</span></span>` : ''}`;
}

export function bindBase(panel) {
  return [...panel.querySelectorAll('[data-card]')].map((el) => ({
    b: BY_ID[el.dataset.card],
    el,
    btn: el.querySelector('[data-act="build"]'),
    chips: [...el.querySelectorAll('.chip')],
    eff: el.querySelector('[data-eff]'),
    bar: el.querySelector('.progress i'),
  }));
}

export function updateBase(s, cards, flows) {
  for (const card of cards) {
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
}
