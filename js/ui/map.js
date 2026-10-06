// Operations tab: the wasteland map and the selected sector's briefing.
import { SECTORS, FACTIONS, MAP, CHAPTERS, OPS } from '../data.js';
import * as E from '../engine.js';
import { num, time, pct, esc } from '../format.js';
import { costChips, setChips, tags, bonusText, chanceClass } from './common.js';

const hex = (r) => Array.from({ length: 6 }, (_, i) => {
  const a = (Math.PI / 3) * i + Math.PI / 6;
  return `${(Math.cos(a) * r).toFixed(1)},${(Math.sin(a) * r).toFixed(1)}`;
}).join(' ');

export function defaultSector(s) {
  const target = SECTORS.find((x) => E.sectorStatus(s, x) === 'target');
  return (s.op && s.op.sector) || (target ? target.id : MAP.home);
}

function territory() {
  return Object.keys(FACTIONS).filter((f) => f !== 'rogue').map((f) => {
    const list = SECTORS.filter((x) => x.faction === f);
    const cx = list.reduce((a, x) => a + x.x, 0) / list.length;
    const cy = list.reduce((a, x) => a + x.y, 0) / list.length;
    return `<circle cx="${cx}" cy="${cy}" r="170" fill="url(#terr-${f})"/>`;
  }).join('');
}

export function renderMap(s, ui) {
  const sel = ui.sector;
  const links = [];
  for (const a of SECTORS) {
    for (const id of a.links) {
      if (a.id < id) {
        const b = E.sectorById(id);
        const own = s.sectors.includes(a.id) && s.sectors.includes(b.id);
        const front = s.sectors.includes(a.id) !== s.sectors.includes(b.id);
        links.push(`<line x1="${a.x}" y1="${a.y}" x2="${b.x}" y2="${b.y}" class="link ${own ? 'own' : front ? 'front' : ''}"/>`);
      }
    }
  }
  let opPath = '';
  if (s.op) {
    const t = E.sectorById(s.op.sector);
    const from = E.sectorById(t.links.find((l) => s.sectors.includes(l)));
    opPath = `<g class="op-run"><line x1="${from.x}" y1="${from.y}" x2="${t.x}" y2="${t.y}" class="op-line"/>
      <circle r="6" class="op-dot"><animateMotion dur="1.6s" repeatCount="indefinite" path="M${from.x},${from.y} L${t.x},${t.y}"/></circle></g>`;
  }
  const nodes = SECTORS.map((x) => {
    const st = E.sectorStatus(s, x);
    const r = x.boss ? 30 : x.id === MAP.home ? 32 : 22;
    const known = st !== 'far';
    const color = x.faction ? FACTIONS[x.faction].color : 'var(--accent)';
    const label = known ? x.name : 'Unknown signal';
    const icon = st === 'owned' ? (x.id === MAP.home ? '◉' : '⚑') : st === 'locked' ? '🔒' : known ? FACTIONS[x.faction].sigil : '?';
    return `
      <g class="node st-${st} ${x.boss ? 'boss' : ''} ${sel === x.id ? 'sel' : ''} ${s.op && s.op.sector === x.id ? 'attacking' : ''}" data-sector="${x.id}" transform="translate(${x.x},${x.y})" style="--fc:${color}" tabindex="0" role="button" aria-label="${esc(label)}">
        ${st === 'target' ? `<polygon class="pulse" points="${hex(r + 6)}"><animateTransform attributeName="transform" type="scale" from="1" to="1.45" dur="2s" repeatCount="indefinite"/><animate attributeName="opacity" from=".8" to="0" dur="2s" repeatCount="indefinite"/></polygon>` : ''}
        <polygon class="hex" points="${hex(r)}"/>
        <text class="sig" dy="${x.boss ? 7 : 5}">${icon}</text>
        <text class="name" dy="${r + 18}">${esc(label)}</text>
        ${known && st !== 'owned' ? `<text class="def" dy="${-r - 8}">⬢ ${num(x.defense)}</text>` : ''}
      </g>`;
  }).join('');
  return `
    <div class="ops">
      <div class="map-card">
        <div class="map-wrap">
          <svg class="map" viewBox="0 0 ${MAP.width} ${MAP.height}" role="img" aria-label="Wasteland map">
            <defs>
              ${Object.entries(FACTIONS).map(([f, d]) => `<radialGradient id="terr-${f}"><stop offset="0" stop-color="${d.color}" stop-opacity=".13"/><stop offset="1" stop-color="${d.color}" stop-opacity="0"/></radialGradient>`).join('')}
            </defs>
            <g class="terrain">${territory()}</g>
            <g>${links.join('')}</g>
            ${opPath}
            <g>${nodes}</g>
          </svg>
        </div>
        <div class="legend">${legend(s)}</div>
      </div>
      <aside class="briefing" id="briefing">${briefing(s, E.sectorById(sel))}</aside>
    </div>`;
}

function legend(s) {
  return CHAPTERS.map((c) => {
    const f = FACTIONS[c.faction];
    const capital = SECTORS.find((x) => x.faction === c.faction && x.boss);
    const fallen = s.sectors.includes(capital.id);
    const open = E.chapterOpen(s, c.id);
    const state = fallen ? 'Defeated' : open ? 'Hostile' : `AI Core Lv ${c.core}`;
    return `<span class="leg ${fallen ? 'down' : ''}" style="--fc:${f.color}"><i></i>${f.short}<em>${state}</em></span>`;
  }).join('');
}

function briefing(s, sec) {
  const st = E.sectorStatus(s, sec);
  const f = sec.faction ? FACTIONS[sec.faction] : null;
  const head = `
    <header class="brief-head" style="--fc:${f ? f.color : 'var(--accent)'}">
      <span class="sig">${st === 'far' ? '?' : f ? f.sigil : '◉'}</span>
      <div><h2>${st === 'far' ? 'Unknown signal' : sec.name}</h2>
      <span class="fac">${st === 'far' ? 'Unscouted' : f ? f.name + (sec.boss ? ' · Capital' : '') : 'Your base'}</span></div>
      <span class="pill st-${st}">${{ owned: 'Controlled', target: 'In range', locked: 'Locked', far: 'Out of range' }[st]}</span>
    </header>`;
  if (st === 'owned') {
    return `${head}
      ${sec.bonus ? `<div class="brief-rows"><span class="row"><span>Bonus</span><span class="good-t">${bonusText(sec.bonus)}</span></span></div>` : ''}
      <blockquote class="lore">${esc(sec.lore)}</blockquote>`;
  }
  if (st === 'far') {
    return `${head}<p class="muted">The signal is too faint. Capture a neighbouring sector to scout it.</p>`;
  }
  const ch = E.chapterOf(sec.chapter);
  const running = s.op && s.op.sector === sec.id;
  return `${head}
    ${f ? `<p class="muted small">${esc(f.desc)}</p>` : ''}
    <div class="brief-rows">
      <span class="row"><span>Enemy defense</span><span>⬢ ${num(sec.defense)}</span></span>
      <span class="row"><span>Your AI Power</span><span data-power></span></span>
      <span class="row"><span>Success chance</span><span data-chance></span></span>
      <div class="odds"><i data-odds></i></div>
      <span class="row"><span>Duration</span><span>${time(E.opTime(sec))}</span></span>
      <span class="row"><span>Spoils</span><span>${tags(E.opLoot(sec), '+')}</span></span>
      <span class="row"><span>Permanent bonus</span><span class="good-t">${bonusText(sec.bonus)}</span></span>
      ${sec.boss ? `<span class="row"><span>Capital</span><span class="good-t">Ends ${f.short} raids</span></span>` : ''}
      <span class="row"><span>If it fails</span><span class="bad-t">Lose ${pct(OPS.staffLossOnDefeat)} of Military Staff</span></span>
    </div>
    ${st === 'locked'
      ? `<p class="locked-note">Opens in ${ch.title} (Chapter ${ch.id}) at AI Core Lv ${ch.core}.</p>`
      : `<div class="costs">${costChips(E.opCost(sec))}</div>
         ${running ? '<div class="op-progress"><span data-optime></span><div class="bar"><i data-opbar></i></div></div>' : ''}
         <button class="btn primary wide launch" data-act="launch" data-id="${sec.id}"></button>`}`;
}

export function bindMap(panel) {
  const b = panel.querySelector('#briefing');
  return {
    power: b.querySelector('[data-power]'),
    chance: b.querySelector('[data-chance]'),
    odds: b.querySelector('[data-odds]'),
    chips: [...b.querySelectorAll('.chip')],
    btn: b.querySelector('.launch'),
    optime: b.querySelector('[data-optime]'),
    opbar: b.querySelector('[data-opbar]'),
  };
}

export function updateMap(s, ui, refs) {
  const sec = E.sectorById(ui.sector);
  if (!refs.power) {
    return;
  }
  const p = E.opChance(s, sec);
  refs.power.textContent = '✸ ' + num(E.factors(s).power);
  refs.chance.textContent = pct(p);
  refs.chance.className = 'chance-' + chanceClass(p);
  refs.odds.style.width = pct(p);
  refs.odds.className = 'odds-' + chanceClass(p);
  if (refs.chips.length) {
    setChips(s, refs.chips, E.opCost(sec));
  }
  if (refs.btn) {
    const running = s.op && s.op.sector === sec.id;
    refs.btn.disabled = !E.canLaunch(s, sec);
    refs.btn.textContent = running ? 'Operation under way' : s.op ? 'Another operation is running' : p < 0.5 ? `Launch anyway (${pct(p)})` : 'Launch operation';
    refs.btn.classList.toggle('risky', p < 0.5);
  }
  if (refs.optime && s.op) {
    refs.optime.textContent = 'Arrives in ' + time(s.op.remaining);
    refs.opbar.style.width = pct(1 - s.op.remaining / s.op.total);
  }
}
