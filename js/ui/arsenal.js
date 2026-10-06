// Arsenal tab: escalating-price units in five shop tabs.
import { RESOURCES, FACTORS, BY_ID, ITEMS, SHOP_TABS } from '../data.js';
import * as E from '../engine.js';
import { num, pct } from '../format.js';
import { reqText, costChips, setChips } from './common.js';

export const BUY_MODES = [1, 10, 'max'];

export function renderArsenal(s, ui) {
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
    <div class="shop-bar"><p>Every unit costs more than the last. ${unlocker.name} levels cut prices here.${tab.id === 'staff' ? ' Staff can die in lost battles.' : ''}</p><div class="modes">${modes}</div></div>
    <div class="items">${body}</div>`;
}

function givesText(s, item) {
  if (item.bonus) {
    return Object.entries(item.bonus).map(([k, v]) => {
      const name = RESOURCES[k] ? RESOURCES[k].name + ' production' : FACTORS[k].name;
      return `+${pct(v)} ${name}`;
    }).join(', ');
  }
  const f = E.factors(s);
  const raw = E.rawFactors(s);
  return Object.entries(item.gives).map(([k, v]) => {
    const mult = raw[k] ? f[k] / raw[k] : 1;
    return `<span class="ft ft-${k}">+${num(v * mult)} ${FACTORS[k].name.replace('AI ', '')}</span>`;
  }).join(' ');
}

function renderItem(s, item) {
  if (!E.itemUnlocked(s, item)) {
    return `<div class="item locked"><span class="ic">${item.icon}</span><div class="item-main"><h3>${item.name}</h3><p class="req">Requires ${reqText(item.req)}</p></div></div>`;
  }
  return `
    <div class="item" data-item="${item.id}">
      <span class="ic">${item.icon}</span>
      <div class="item-main">
        <h3>${item.name} <span class="owned">×${E.owned(s, item.id)}</span></h3>
        <p class="gives">${givesText(s, item)} <span class="each">each</span></p>
        <div class="costs">${costChips(E.itemCost(s, item, 1))}</div>
      </div>
      <button class="btn primary buy" data-act="buy" data-id="${item.id}">Buy</button>
    </div>`;
}

export function bindArsenal(panel) {
  return [...panel.querySelectorAll('[data-item]')].map((el) => ({
    item: ITEMS.find((i) => i.id === el.dataset.item),
    el,
    btn: el.querySelector('.buy'),
    chips: [...el.querySelectorAll('.chip')],
  }));
}

export function buyCount(s, ui, item) {
  return ui.buyMode === 'max' ? Math.max(1, E.maxAffordable(s, item)) : ui.buyMode;
}

export function updateArsenal(s, ui, rows) {
  for (const row of rows) {
    const count = buyCount(s, ui, row.item);
    const cost = E.itemCost(s, row.item, count);
    setChips(s, row.chips, cost);
    const ok = E.canAfford(s, cost);
    row.btn.disabled = !ok;
    row.btn.textContent = count > 1 ? `Buy ×${count}` : 'Buy';
    row.el.classList.toggle('affordable', ok);
  }
}
