// Small shared render helpers.
import { RESOURCES, FACTORS, BY_ID } from '../data.js';
import { caps } from '../engine.js';
import { num, pct } from '../format.js';

export function resTag(r, amount, sign = '') {
  return `<span class="tag t-${r}">${sign}${num(amount)}&#8202;${RESOURCES[r].icon}</span>`;
}

export function tags(obj, sign = '') {
  return Object.entries(obj || {}).filter(([, v]) => v).map(([r, v]) => resTag(r, v, sign)).join(' ');
}

export function reqText(req) {
  return Object.entries(req).map(([id, l]) => `${BY_ID[id].name} Lv ${l}`).join(', ');
}

export function costChips(cost) {
  return Object.entries(cost).map(([r, v]) =>
    `<span class="chip c-${r}" data-res="${r}"><span class="ic">${RESOURCES[r].icon}</span><span data-t>${num(v)}</span></span>`).join('');
}

export function setChips(s, chips, cost) {
  const c = caps(s);
  for (const chip of chips) {
    const r = chip.dataset.res;
    const v = cost[r];
    chip.querySelector('[data-t]').textContent = num(v);
    chip.classList.toggle('short', s.res[r] + 1e-9 < v);
    chip.classList.toggle('over', v > c[r]);
    chip.title = v > c[r] ? `Exceeds your ${RESOURCES[r].name} cap. Expand storage.` : '';
  }
}

// "+10% Scrip production" / "+5% AI Power"
export function bonusText(bonus) {
  return Object.entries(bonus || {}).map(([k, v]) => {
    const name = RESOURCES[k] ? RESOURCES[k].name : FACTORS[k].name;
    return `+${pct(v)} ${name}`;
  }).join(', ');
}

export function chanceClass(p) {
  return p >= 0.8 ? 'good' : p >= 0.5 ? 'fair' : 'bad';
}
