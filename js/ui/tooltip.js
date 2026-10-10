// Tooltips for anything marked data-tip. Hover on desktop, tap on touch. Content is built live from state.
import {
  RESOURCES, FACTORS, BUILDINGS, BY_ID, BALANCE, RANKS, RAIDS, ALIGNMENT,
} from '../data.js';
import * as E from '../engine.js';
import { num, rate, time, pct, esc } from '../format.js';
import { icon } from './icons.js';

export function createTips(game) {
  const tip = document.createElement('div');
  tip.className = 'tip';
  tip.setAttribute('role', 'tooltip');
  document.body.appendChild(tip);
  let target = null;
  let timer = null;

  const row = (k, v, cls = '') => `<div class="tip-row ${cls}"><span>${k}</span><b>${v}</b></div>`;
  const head = (ic, title, sub = '') => `<div class="tip-head">${icon(ic)}<div><b>${title}</b>${sub ? `<small>${sub}</small>` : ''}</div></div>`;

  function bonusRows(s, key) {
    const b = E.bonusBreakdown(s, key);
    const out = [];
    if (RESOURCES[key]) {
      const ex = E.factors(s).experts * BALANCE.expertProductionBonus;
      if (ex) out.push(row(`${icon('experts')}Experts`, '+' + pct(ex)));
    }
    if (b.tech) out.push(row(`${icon('research')}Tech`, '+' + pct(b.tech)));
    if (b.sectors) out.push(row(`${icon('map')}Territory`, '+' + pct(b.sectors)));
    if (b.alignment) out.push(row(`${icon('heart')}Alignment`, '+' + pct(b.alignment)));
    if (b.effects) out.push(row(`${icon('spark')}Effects`, (b.effects > 0 ? '+' : '−') + pct(Math.abs(b.effects)), b.effects < 0 ? 'neg' : ''));
    return out.join('');
  }

  function resTip(s, r) {
    const c = E.caps(s)[r];
    const f = game.flows;
    const net = f ? f.prod[r] - f.cons[r] : 0;
    const mult = E.prodMultiplier(s, r);
    const lines = [];
    for (const b of BUILDINGS) {
      const lvl = E.level(s, b.id);
      if (!lvl) continue;
      if (b.produces && b.produces[r]) {
        const eff = b.kind === 'converter' ? (f && f.eff[b.id]) ?? 0 : 1;
        lines.push(eff > 0.001 ? row(`${icon(b.id)}${b.name}`, '+' + num(b.produces[r] * lvl * mult * eff) + '/s') : row(`${icon(b.id)}${b.name}`, s.paused[b.id] ? 'paused' : 'idle', 'warn'));
      }
      if (b.consumes && b.consumes[r]) {
        const eff = (f && f.eff[b.id]) ?? 0;
        const cm = E.prodMultiplier(s, Object.keys(b.produces)[0]);
        if (eff > 0.001) lines.push(row(`${icon(b.id)}${b.name}`, '−' + num(b.consumes[r] * lvl * cm * eff) + '/s', 'neg'));
      }
    }
    let eta = '';
    if (Number.isFinite(c)) {
      if (s.res[r] >= c * 0.999) eta = row('Storage', 'FULL: expand it', 'warn');
      else if (net > 0) eta = row('Full in', time((c - s.res[r]) / net));
    }
    if (net < 0 && s.res[r] > 0) eta += row('Empty in', time(s.res[r] / -net), 'neg');
    return head(r, RESOURCES[r].name, Number.isFinite(c) ? `${num(s.res[r])} / ${num(c)}` : num(s.res[r]))
      + `<p>${RESOURCES[r].desc}</p>` + row('Net', rate(net), net < 0 ? 'neg' : '') + eta
      + (lines.length ? `<div class="tip-sec">Sources</div>${lines.join('')}` : '')
      + (mult !== 1 ? `<div class="tip-sec">Bonuses</div>${bonusRows(s, r)}` : '');
  }

  function factorTip(s, k) {
    const raw = E.rawFactors(s)[k];
    const val = E.factors(s)[k];
    const extra = k === 'experts' ? row('Production bonus', '+' + pct(val * BALANCE.expertProductionBonus)) : '';
    const what = { power: 'Attack strength in operations.', defense: 'Strength against raids.', experts: `+${pct(BALANCE.expertProductionBonus)} production each.` }[k];
    // Troops cannot be on the road and on the wall at once, so say what is missing while they are out.
    const away = k === 'defense' && E.troopsOut(s) ? row('Troops on the operation', '\u2212' + num(E.troopDefense(s)), 'neg') : '';
    return head(k, FACTORS[k].name, num(val)) + `<p>${what}</p>` + row('From units', num(raw)) + away + extra
      + (val !== raw ? `<div class="tip-sec">Bonuses</div>${bonusRows(s, k)}` : '');
  }

  function costTip(s, r, amt) {
    const have = s.res[r];
    const c = E.caps(s)[r];
    const f = game.flows;
    const net = f ? f.prod[r] - f.cons[r] : 0;
    let status;
    if (amt > c) {
      const store = r === 'energy' ? 'battery' : 'habitat';
      status = row('Exceeds storage', `Upgrade ${icon(store)}${BY_ID[store].name}`, 'warn');
    } else if (have >= amt) {
      status = row('Status', 'Affordable', 'ok');
    } else if (net > 0) {
      status = row('Short by', num(amt - have), 'neg') + row('Ready in', time((amt - have) / net));
    } else {
      status = row('Short by', num(amt - have), 'neg') + row('Ready in', 'never at this rate', 'neg');
    }
    return head(r, RESOURCES[r].name) + `<p>${RESOURCES[r].desc}</p>` + row('Cost', num(amt)) + row('You have', num(have)) + status;
  }

  function threatTip(s) {
    const t = E.threat(s);
    const ri = E.rankIndex(t);
    const next = RANKS[ri + 1];
    return head('threat', 'Threat index', RANKS[ri].title)
      + `<p>Power + Defense + Experts × ${BALANCE.threatExpertWeight}. Raid strength scales with it.</p>`
      + row('Threat', num(t)) + (next ? row('Next rank', `${next.title} at ${num(next.at)}`) : '');
  }

  function alignTip(s) {
    const a = Math.round(s.align);
    const g = ['pop', 'experts'].map((k) => E.alignBonus(s, k)).filter(Boolean);
    const o = ['power', 'energy'].map((k) => E.alignBonus(s, k)).filter(Boolean);
    return head('heart', 'Humanity', `${E.alignmentLabel(a)} (${a > 0 ? '+' : ''}${a})`)
      + `<p>Shifted by your orders. Guardian: up to +${pct(ALIGNMENT.guardian.pop)} Population and Experts. Overlord: up to +${pct(ALIGNMENT.overlord.power)} Power and +${pct(ALIGNMENT.overlord.energy)} Energy.</p>`
      + (g.length ? row('Guardian bonus', '+' + pct(g[0]) + ' Pop & Experts', 'ok') : '')
      + (o.length ? row('Overlord bonus', '+' + pct(o[0]) + ' Power, +' + pct(o[1] || 0) + ' Energy', 'neg') : '');
  }

  function content(el) {
    const s = game.state;
    const spec = el.dataset.tip;
    const [kind, a] = spec.split(':');
    if (kind === 'res') return resTip(s, a);
    if (kind === 'factor') return factorTip(s, a);
    if (kind === 'cost') return costTip(s, a, Number(el.dataset.amt));
    if (kind === 'threat') return threatTip(s);
    if (kind === 'align') return alignTip(s);
    return `<p>${esc(el.dataset.tipText || '')}</p>`;
  }

  // An open <dialog> sits in the top layer, above anything in <body>; popups for its contents live inside it.
  function mount(el, near) {
    const host = near.closest('dialog[open]') || document.body;
    if (el.parentNode !== host) host.appendChild(el);
  }

  function place() {
    if (!target || !document.body.contains(target)) {
      hide();
      return;
    }
    mount(tip, target);
    tip.innerHTML = content(target);
    const r = target.getBoundingClientRect();
    const t = tip.getBoundingClientRect();
    const pad = 8;
    let left = r.left + r.width / 2 - t.width / 2;
    left = Math.max(pad, Math.min(window.innerWidth - t.width - pad, left));
    let top = r.bottom + 8;
    if (top + t.height > window.innerHeight - pad) {
      top = r.top - t.height - 8;
    }
    // Never past any screen edge, even when neither side of the target has room.
    top = Math.max(pad, Math.min(window.innerHeight - t.height - pad, top));
    tip.style.left = left + 'px';
    tip.style.top = top + 'px';
  }

  function show(el) {
    target = el;
    tip.classList.add('on');
    place();
    clearInterval(timer);
    timer = setInterval(place, 500);
  }

  function hide() {
    target = null;
    tip.classList.remove('on');
    clearInterval(timer);
  }

  document.addEventListener('pointerover', (e) => {
    if (e.pointerType !== 'mouse') return;
    const el = e.target.closest('[data-tip]');
    if (el && el !== target) show(el);
    else if (!el && target) hide();
  });
  // Tapping a bare icon names it in one word; this wins over the surrounding tooltip.
  const label = document.createElement('div');
  label.className = 'icon-label';
  document.body.appendChild(label);
  let labelTimer = null;
  function showLabel(el) {
    hide();
    mount(label, el);
    label.textContent = el.dataset.label;
    label.classList.add('on');
    const r = el.getBoundingClientRect();
    const w = label.offsetWidth;
    label.style.left = Math.max(6, Math.min(window.innerWidth - w - 6, r.left + r.width / 2 - w / 2)) + 'px';
    const above = r.top - label.offsetHeight - 6;
    label.style.top = (above >= 6 ? above : r.bottom + 6) + 'px';
    clearTimeout(labelTimer);
    labelTimer = setTimeout(() => label.classList.remove('on'), 1600);
  }

  document.addEventListener('click', (e) => {
    const lbl = e.target.closest && e.target.closest('[data-label]');
    if (lbl && !e.target.closest('button, .topbar')) {
      e.stopPropagation();
      showLabel(lbl);
      return;
    }
    label.classList.remove('on');
    const el = e.target.closest('[data-tip]');
    if (el && !e.target.closest('button') && el !== target) {
      show(el);
    } else if (!el || el === target) {
      if (e.pointerType !== 'mouse' || !el) hide();
    }
  });
  window.addEventListener('scroll', hide, { passive: true, capture: true });

  return { hide };
}
