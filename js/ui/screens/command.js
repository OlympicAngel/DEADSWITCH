// Command: the war room. Threat reactor in the middle, live feeds for every front around it.
import {
  FACTIONS, FACTORS, RANKS, RAIDS, NODES, DIRECTIVES, ALIGNMENT, BY_ID, SECTORS,
} from '../../data.js';
import * as E from '../../engine.js';
import { num, time, pct, esc } from '../../format.js';
import { icon, labeled } from '../icons.js';
import { tags, clock, chanceClass, bonusText } from '../common.js';
import { put, putHtml, setCls, setData, setW, setAttr, setStyle } from '../dom.js';
import { onScreen } from '../onscreen.js';
import { mapBackdrop } from '../minimap.js';

const R = 160; // reactor centre in its 320 viewBox

function arc(r, a0, a1) {
  const p = (a) => [R + r * Math.cos((a * Math.PI) / 180), R + r * Math.sin((a * Math.PI) / 180)];
  const [x0, y0] = p(a0);
  const [x1, y1] = p(a1);
  return `M${x0.toFixed(1)},${y0.toFixed(1)} A${r},${r} 0 ${a1 - a0 > 180 ? 1 : 0} 1 ${x1.toFixed(1)},${y1.toFixed(1)}`;
}

function ticks() {
  let out = '';
  for (let i = 0; i < 60; i++) {
    const a = (i * 6 * Math.PI) / 180;
    const r0 = i % 5 ? 134 : 128;
    out += `<line x1="${(R + r0 * Math.cos(a)).toFixed(1)}" y1="${(R + r0 * Math.sin(a)).toFixed(1)}" x2="${(R + 140 * Math.cos(a)).toFixed(1)}" y2="${(R + 140 * Math.sin(a)).toFixed(1)}"/>`;
  }
  return out;
}

export function renderCommand() {
  // Each rotating part is its own <svg> rotated as a whole: the GPU turns it without repainting
  // (rotating shapes inside an SVG repaints the whole drawing every frame).
  const spinLayer = (cls, inner) => `<svg class="reactor-svg spin-layer ${cls}" viewBox="0 0 320 320" aria-hidden="true">${inner}</svg>`;
  const sat = (k, cls, tip) => `
    <button class="sat ${cls}" data-tip="${tip}">
      <span class="sat-ico">${icon(k === 'align' ? 'heart' : k)}</span>
      <b data-sat="${k}"></b><small>${k === 'align' ? 'Humanity' : FACTORS[k].name.replace('AI ', '')}</small>
    </button>`;
  return `
    <div class="screen command">
      <section class="reactor" data-tip="threat">
        <svg class="reactor-svg" viewBox="0 0 320 320" aria-hidden="true">
          <defs>
            <radialGradient id="rg-core"><stop offset="0" stop-color="var(--hud)" stop-opacity=".35"/><stop offset=".6" stop-color="var(--hud)" stop-opacity=".06"/><stop offset="1" stop-color="var(--hud)" stop-opacity="0"/></radialGradient>
          </defs>
          <g class="links"><line x1="${R}" y1="${R}" x2="40" y2="44"/><line x1="${R}" y1="${R}" x2="280" y2="44"/><line x1="${R}" y1="${R}" x2="40" y2="276"/><line x1="${R}" y1="${R}" x2="280" y2="276"/></g>
          <circle cx="${R}" cy="${R}" r="150" fill="url(#rg-core)"/>
          <circle class="ring r-outer" cx="${R}" cy="${R}" r="118"/>
          <path class="align-track" d="${arc(146, 200, 340)}"/>
          <path class="align-fill" data-alignarc d="${arc(146, 270, 270.1)}"/>
          <circle class="rank-track" cx="${R}" cy="${R}" r="94"/>
          <circle class="rank-fill" data-rankring cx="${R}" cy="${R}" r="94" transform="rotate(-90 ${R} ${R})"/>
        </svg>
        ${spinLayer('s-ticks', `<g class="ticks">${ticks()}</g>`)}
        ${spinLayer('s-dash', `<circle class="ring r-dash" cx="${R}" cy="${R}" r="108"/>`)}
        ${spinLayer('s-inner', `<circle class="ring r-inner" cx="${R}" cy="${R}" r="78"/>`)}
        ${spinLayer('s-sweep', `<g class="sweep"><path d="M${R},${R} L${R},${R - 118} A118,118 0 0 1 ${(R + 118 * Math.sin(0.5)).toFixed(1)},${(R - 118 * Math.cos(0.5)).toFixed(1)} Z"/></g>`)}
        <div class="reactor-core">
          <small>Threat index</small>
          <b data-threat></b>
          <span data-rank></span>
          <em data-next></em>
        </div>
        ${sat('power', 'tl', 'factor:power')}
        ${sat('defense', 'tr', 'factor:defense')}
        ${sat('experts', 'bl', 'factor:experts')}
        ${sat('align', 'br', 'align')}
      </section>
      <div class="hud-grid">
        <section class="panel incoming" data-panel="raid"></section>
        <div class="atk-stack" data-panel="stack"></div>
        <section class="panel half" data-panel="op"></section>
        <section class="panel half" data-panel="build"></section>
        <section class="panel" data-panel="events"></section>
        <section class="panel" data-panel="directive"></section>
        <section class="panel" data-panel="effects"></section>
        <section class="panel feed" data-panel="feed"></section>
      </div>
    </div>`;
}

export function bindCommand(panel) {
  const q = (sel) => panel.querySelector(sel);
  return {
    threat: q('[data-threat]'), rank: q('[data-rank]'), next: q('[data-next]'),
    ring: q('[data-rankring]'), alignArc: q('[data-alignarc]'),
    sats: Object.fromEntries([...panel.querySelectorAll('[data-sat]')].map((el) => [el.dataset.sat, el])),
    panels: Object.fromEntries([...panel.querySelectorAll('[data-panel]')].map((el) => [el.dataset.panel, el])),
    keys: {},
  };
}

// Rebuilds a panel only when its key changes; returns the element for in-place patching.
function slot(refs, id, key, html) {
  const el = refs.panels[id];
  if (refs.keys[id] !== key) {
    refs.keys[id] = key;
    el.innerHTML = html;
  }
  return el;
}

export function updateCommand(s, ui, refs) {
  const f = E.factors(s);
  const t = E.threat(s, f);
  const ri = E.rankIndex(t);
  const nx = RANKS[ri + 1];
  const prog = nx ? (t - RANKS[ri].at) / (nx.at - RANKS[ri].at) : 1;
  put(refs.threat, num(t));
  put(refs.rank, RANKS[ri].title);
  put(refs.next, nx ? `${pct(prog)} to ${nx.title}` : 'Maximum rank');
  const circ = 2 * Math.PI * 94;
  setStyle(refs.ring, 'strokeDasharray', `${(circ * Math.min(1, prog)).toFixed(1)} ${circ.toFixed(1)}`);
  const a = s.align;
  const ang = 270 + (a / ALIGNMENT.max) * 70;
  setAttr(refs.alignArc, 'd', a >= 0 ? arc(146, 270, Math.max(270.1, ang)) : arc(146, Math.min(269.9, ang), 270));
  refs.alignArc.classList.toggle('mach', a < 0);
  put(refs.sats.power, num(f.power));
  put(refs.sats.defense, num(f.defense));
  put(refs.sats.experts, num(f.experts));
  put(refs.sats.align, `${a > 0 ? '+' : ''}${Math.round(a)}`);

  // Panels scrolled out of view wait until they come back.
  const on = (id) => onScreen(refs.panels[id]);
  if (on('raid') || on('stack')) raidPanel(s, refs);
  if (on('op')) opPanel(s, refs);
  if (on('build')) buildPanel(s, refs);
  if (on('events')) eventsPanel(s, refs);
  if (on('directive')) directivePanel(s, refs);
  if (on('effects')) effectsPanel(s, refs);
  if (on('feed')) feedPanel(s, refs);
}

function raidPanel(s, refs) {
  const raid = E.nextAttack(s);
  if (!raid) {
    const quiet = E.level(s, 'core') < RAIDS.startAtCore;
    slot(refs, 'raid', 'none' + quiet, `
      <header>${icon('threat')}Incoming</header>
      <div class="radar-idle"><div class="mini-radar"><i></i></div>
        <p><b>No hostiles on radar</b><span>${quiet ? `Raids begin at ${icon('core')}AI Core Lv ${RAIDS.startAtCore}.` : 'Scouts see nothing. For now.'}</span></p></div>`);
    refs.panels.raid.dataset.level = 'calm';
    stackPanel(s, refs);
    return;
  }
  const fac = FACTIONS[raid.faction];
  stackPanel(s, refs);
  // Where it is coming from and what it is coming for, drawn behind the panel.
  const place = E.attackPlace(s, raid);
  const sub = raid.assault
    ? `On ${esc(E.sectorById(raid.target).name)} · ${E.breaches(s, raid.target)}/${NODES.breachesToFall} breaches`
    : place ? `${fac.name} · out of ${esc(E.sectorById(place[0]).name)}` : fac.name;
  const el = slot(refs, 'raid', raid.faction + raid.strength + (place || []).join() + (raid.assault ? E.breaches(s, raid.target) : ''), `
    <header>${icon('alert')}Incoming attack<span class="blink-dot"></span></header>
    ${place ? mapBackdrop(s, [place[1], place[0]], { beam: place, cls: 'panel' }) : ''}
    <div class="inc-main">
      ${place ? '' : `<span class="fac-ico" style="--fc:${fac.color}">${icon(fac.icon)}</span>`}
      <div><b>${E.attackName(raid)}</b><small>${sub}</small></div>
      <div class="count" data-count></div>
    </div>
    <div class="bars">
      <div class="vs-row you" data-tip="factor:defense"><span>${icon('defense')}Your defense</span><div class="vbar"><i data-dbar></i></div><b data-dval></b></div>
      <div class="vs-row them"><span>${icon('power')}Raid strength</span><div class="vbar"><i data-sbar></i></div><b>${num(raid.strength)}</b></div>
    </div>
    <div class="inc-foot"><span class="hold" data-hold></span><button class="btn danger" data-go="military:defense">${icon('defense')}Reinforce</button></div>
    <div class="timebar"><i data-tbar></i></div>`);
  const p = E.raidChance(s, raid);
  const def = E.factors(s).defense;
  const max = Math.max(def, raid.strength, 1);
  setData(el, 'level', p < 0.5 ? 'danger' : 'warn');
  el.classList.toggle('imminent', raid.remaining < 60);
  el.classList.toggle('has-map', !!place);
  put(el.querySelector('[data-count]'), clock(raid.remaining));
  setW(el.querySelector('[data-dbar]'), pct(def / max));
  setW(el.querySelector('[data-sbar]'), pct(raid.strength / max));
  put(el.querySelector('[data-dval]'), num(def));
  const hold = el.querySelector('[data-hold]');
  putHtml(hold, `Hold chance <b>${pct(p)}</b>`);
  setCls(hold, 'hold chance-' + chanceClass(p));
  setW(el.querySelector('[data-tbar]'), pct(1 - raid.remaining / raid.total));
}

// Later attacks as cards tucked under the closest one: icon, name and timer only.
function stackPanel(s, refs) {
  const rest = E.attacks(s).slice(1);
  const el = slot(refs, 'stack', rest.map((a) => a.faction + a.strength + !!a.siege).join('|'), rest.map((a, i) => {
    const f = FACTIONS[a.faction];
    return `<div class="stack-card" style="--i:${i}"><span style="color:${f.color}">${icon(f.icon)}</span><b>${E.attackName(a)}</b><span class="t" data-st="${i}"></span></div>`;
  }).join(''));
  el.hidden = !rest.length;
  rest.forEach((a, i) => {
    const t = el.querySelector(`[data-st="${i}"]`);
    if (t) put(t, clock(a.remaining));
  });
}

function opPanel(s, refs) {
  if (!s.op) {
    const any = SECTORS.some((x) => E.sectorStatus(s, x) === 'target');
    slot(refs, 'op', 'idle' + any, `
      <header>${icon('power')}Outgoing</header>
      <p class="idle">${any ? 'No operation running' : 'No targets in range'}</p>
      <button class="btn ghost small" data-go="map">${icon('map')}${any ? 'Targets' : 'Map'}</button>`);
    return;
  }
  const sec = E.sectorById(s.op.sector);
  const fac = FACTIONS[sec.faction];
  const el = slot(refs, 'op', s.op.sector, `
    <header>${icon('power')}Outgoing</header>
    <div class="mini"><span class="fac-ico sm" style="--fc:${fac.color}">${icon(fac.icon)}</span><b>${sec.name}</b></div>
    <div class="mini-stats"><span data-t></span><span data-c></span></div>
    <div class="timebar"><i data-b></i></div>`);
  const p = E.opChance(s, sec);
  putHtml(el.querySelector('[data-t]'), `${labeled('clock')}${clock(s.op.remaining)}`);
  putHtml(el.querySelector('[data-c]'), `<b class="chance-${chanceClass(p)}">${pct(p)}</b> odds`);
  setW(el.querySelector('[data-b]'), pct(1 - s.op.remaining / s.op.total));
}

function buildPanel(s, refs) {
  if (!s.build) {
    slot(refs, 'build', 'idle', `
      <header>${icon('economy')}Construction</header>
      <p class="idle warn-t">Builder idle</p>
      <button class="btn ghost small" data-go="economy">${icon('up')}Build</button>`);
    return;
  }
  const b = BY_ID[s.build.id];
  const el = slot(refs, 'build', s.build.id, `
    <header>${icon('economy')}Construction</header>
    <div class="mini">${icon(b.id)}<b>${b.name}</b></div>
    <div class="mini-stats"><span>Lv ${E.level(s, b.id) + 1}</span><span data-t></span></div>
    <div class="timebar"><i data-b></i></div>`);
  putHtml(el.querySelector('[data-t]'), `${labeled('clock')}${clock(s.build.remaining)}`);
  setW(el.querySelector('[data-b]'), pct(1 - s.build.remaining / s.build.total));
}

function eventsPanel(s, refs) {
  const key = s.events.map((x) => x.uid).join(',');
  if (!s.events.length) {
    slot(refs, 'events', 'none', `<header>${icon('message')}Transmissions</header><p class="idle">No orders pending.</p>`);
    return;
  }
  // The order closest to its deadline sets the scene behind the list.
  const lead = s.events.reduce((a, x) => (x.left < a.left ? x : a));
  const where = [(lead.params || {}).held, (lead.params || {}).node].filter((id) => E.sectorById(id));
  const el = slot(refs, 'events', key + where.join(), `<header>${icon('message')}Transmissions<span class="count-badge">${s.events.length}</span></header>
    ${where.length ? mapBackdrop(s, where, { cls: 'panel' }) : ''}
    ${s.events.map((x) => {
      const ev = E.eventById(x.id);
      return `<button class="tx ${ev.aftermath || ev.urgent ? 'crisis' : ''}" data-act="event" data-uid="${x.uid}">
        ${icon(ev.aftermath ? 'fire' : 'message')}<span><b>${esc(E.fillText(x, ev.title))}</b><small>${ev.aftermath ? 'Damage report' : ev.urgent ? 'Urgent' : 'Decision required'}</small></span>
        <span class="tx-time" data-left="${x.uid}"></span></button>`;
    }).join('')}`);
  el.classList.toggle('has-map', !!where.length);
  for (const x of s.events) {
    const t = el.querySelector(`[data-left="${x.uid}"]`);
    putHtml(t, `${icon('hourglass')}${clock(x.left)}`);
    t.classList.toggle('urgent', x.left < 600);
  }
}

function directivePanel(s, refs) {
  const d = E.currentDirective(s);
  if (!d) {
    slot(refs, 'directive', 'done', `<header>${icon('check')}Directives</header><p class="idle">All directives complete. The wasteland is yours to shape.</p>`);
    return;
  }
  const el = slot(refs, 'directive', 'd' + s.directive, `
    <header>${icon('spark')}Directive ${s.directive + 1}/${DIRECTIVES.length}</header>
    <div class="dir-main"><p>${esc(d.text)}</p><button class="btn primary small go" data-act="directive">Go${icon('next')}</button></div>
    <div class="dir-foot"><span class="reward">Reward ${tags(d.reward, '+')}</span><span data-p></span></div>
    <div class="timebar ok"><i data-b></i></div>`);
  const [have, need] = E.directiveProgress(s, d);
  put(el.querySelector('[data-p]'), need > 1 ? `${num(Math.min(have, need))} / ${num(need)}` : '');
  setW(el.querySelector('[data-b]'), pct(Math.min(1, have / need)));
}

function effectsPanel(s, refs) {
  const g = s.grudges || [];
  const key = s.buffs.map((b) => b.label).join(',') + '|' + g.map((x) => x.faction + x.left + x.mult).join(',');
  const any = s.buffs.length || g.length;
  const el = slot(refs, 'effects', key, !any ? '' : `<header>${icon('spark')}Active effects</header>
    <div class="buffs">${g.map((x) => `<span class="buff neg" data-tip="text" data-tip-text="${esc(FACTIONS[x.faction].name)} raids hit ×${x.mult}${x.untilLoss ? ' until one breaks through' : ` for ${x.left} more raid${x.left > 1 ? 's' : ''}`}.">${icon(FACTIONS[x.faction].icon)}${FACTIONS[x.faction].short} vengeance<b>×${x.mult}</b><em>${x.untilLoss ? 'until breach' : `${x.left} raid${x.left > 1 ? 's' : ''}`}</em></span>`).join('')}${s.buffs.map((b, i) => `<span class="buff ${b.amount < 0 ? 'neg' : ''}" data-tip="text" data-tip-text="${esc(b.label)}: ${bonusText({ [b.key]: b.amount })}">${icon(b.key)}${esc(b.label)}<b>${b.amount < 0 ? '−' : '+'}${pct(Math.abs(b.amount))}</b><em data-buff="${i}"></em></span>`).join('')}</div>`);
  el.hidden = !any;
  s.buffs.forEach((b, i) => {
    const t = el.querySelector(`[data-buff="${i}"]`);
    if (t) put(t, b.hold ? 'while it holds' : time(b.remaining));
  });
}

function feedPanel(s, refs) {
  const key = 'f' + s.lineSeq;
  slot(refs, 'feed', key, `<header>${icon('message')}System feed<button class="btn ghost small" data-act="archive">${icon('book')}Archive</button></header>
    <ol>${s.log.slice(-6).reverse().map((l) => `<li class="tone-${l.tone}"><time>${time(l.t)}</time><span>${esc(l.text)}</span></li>`).join('')}</ol>`);
}
