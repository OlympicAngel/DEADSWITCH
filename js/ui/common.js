// Shared render helpers: resource tags, cost chips, requirement text, odds colours.
import { RESOURCES, FACTORS, BY_ID } from '../data.js';
import { caps } from '../engine.js';
import { num, pct } from '../format.js';
import { icon, labeled } from './icons.js';
import { put } from './dom.js';

// "+120 [coins]" coloured by resource.
export function resTag(r, amount, sign = '') {
  return `<span class="tag t-${r}">${labeled(r)}${sign}${num(amount)}</span>`;
}

export function tags(obj, sign = '') {
  return Object.entries(obj || {}).filter(([, v]) => v).map(([r, v]) => resTag(r, v, sign)).join('');
}

export function factorTag(k, amount, sign = '+') {
  return `<span class="tag t-${k}">${labeled(k)}${sign}${num(amount)}</span>`;
}

// Name with its icon, used everywhere a building or unit is mentioned.
export function named(key, name, cls = '') {
  return `<span class="named ${cls}">${icon(key)}${name}</span>`;
}

export function reqText(req) {
  return Object.entries(req).map(([id, l]) => `${named(id, BY_ID[id].name)} Lv ${l}`).join(', ');
}

// Cost chips carry data-tip so tapping one explains the shortfall.
export function costChips(cost) {
  return Object.entries(cost).map(([r, v]) =>
    `<span class="chip c-${r}" data-res="${r}" data-tip="cost:${r}" data-amt="${v}">${labeled(r)}<span data-t>${num(v)}</span></span>`).join('');
}

export function setChips(s, chips, cost) {
  const c = caps(s);
  for (const chip of chips) {
    const r = chip.dataset.res;
    const v = cost[r] || 0;
    chip.dataset.amt = v;
    put(chip.querySelector('[data-t]'), num(v));
    chip.classList.toggle('short', s.res[r] + 1e-9 < v);
    chip.classList.toggle('over', v > c[r]);
  }
}

// "+10% Scrip production" / "+5% AI Power"; negatives render as penalties.
export function bonusText(bonus) {
  return Object.entries(bonus || {}).map(([k, v]) => {
    const name = RESOURCES[k] ? RESOURCES[k].name : FACTORS[k].name;
    return `${v >= 0 ? '+' : '−'}${pct(Math.abs(v))} ${name}`;
  }).join(', ');
}

// Bonus as a wrap-friendly row of icon chips: [coins +10%] [bolt +10%] ...
export function bonusChips(bonus) {
  return `<span class="bonus-chips">${Object.entries(bonus || {}).map(([k, v]) =>
    `<span class="bchip t-${k}">${labeled(k)}${v >= 0 ? '+' : '−'}${pct(Math.abs(v))}</span>`).join('')}</span>`;
}

export function chanceClass(p) {
  return p >= 0.8 ? 'good' : p >= 0.5 ? 'fair' : 'bad';
}

// mm:ss clock for countdowns that should feel urgent.
export function clock(seconds) {
  const s = Math.max(0, Math.ceil(seconds));
  const h = Math.floor(s / 3600);
  const m = Math.floor((s % 3600) / 60);
  const sec = String(s % 60).padStart(2, '0');
  return h ? `${h}:${String(m).padStart(2, '0')}:${sec}` : `${m}:${sec}`;
}
