// Map: a portrait theater (home at the bottom, Halcyon at the top), a briefing sheet, and the archive.
import {
  SECTORS, FACTIONS, MAP, CHAPTERS, CHAPTER_TEXT, OPS, ENDINGS,
} from '../../data.js';
import * as E from '../../engine.js';
import { num, time, pct, esc } from '../../format.js';
import { icon } from '../icons.js';
import { costChips, setChips, tags, bonusText, chanceClass } from '../common.js';
import { sortedTabs } from '../layout.js';

const W = MAP.height; // the landscape map is turned 90 degrees for portrait
const H = MAP.width;
const P = (x) => ({ x: x.y, y: MAP.width - x.x });

const hex = (r) => Array.from({ length: 6 }, (_, i) => {
  const a = (Math.PI / 3) * i;
  return `${(Math.cos(a) * r).toFixed(1)},${(Math.sin(a) * r).toFixed(1)}`;
}).join(' ');

export function defaultSector(s) {
  const target = SECTORS.find((x) => E.sectorStatus(s, x) === 'target');
  return (s.op && s.op.sector) || (target ? target.id : MAP.home);
}

export function renderMapScreen(s, ui) {
  const tabs = sortedTabs(s, 'map');
  const inner = ui.inner.map || 'theater';
  const bar = tabs.map(({ t }) => `<button class="seg" data-inner="${t.id}" aria-selected="${t.id === inner}">${icon(t.icon)}<span>${t.name}</span></button>`).join('');
  return `
    <div class="screen mapscreen">
      <header class="screen-head">
        <h2>${icon('map')}Wasteland</h2>
        <div class="stats"><div class="stat stat-power" data-tip="factor:power">${icon('power')}<b data-factor="power"></b><small>Power</small></div>
        <div class="stat"><b>${s.sectors.length - 1}/${SECTORS.length - 1}</b><small>Sectors</small></div></div>
      </header>
      <nav class="segs" role="tablist">${bar}</nav>
      ${inner === 'archive' ? renderArchive(s) : renderTheater(s, ui)}
    </div>`;
}

function renderTheater(s, ui) {
  const links = [];
  for (const a of SECTORS) {
    for (const id of a.links) {
      if (a.id < id) {
        const b = E.sectorById(id);
        const own = s.sectors.includes(a.id) && s.sectors.includes(b.id);
        const front = s.sectors.includes(a.id) !== s.sectors.includes(b.id);
        const pa = P(a);
        const pb = P(b);
        links.push(`<line x1="${pa.x}" y1="${pa.y}" x2="${pb.x}" y2="${pb.y}" class="link ${own ? 'own' : front ? 'front' : ''}"/>`);
      }
    }
  }
  let opPath = '';
  if (s.op) {
    const t = E.sectorById(s.op.sector);
    const from = E.sectorById(t.links.find((l) => s.sectors.includes(l)));
    const a = P(from);
    const b = P(t);
    opPath = `<line x1="${a.x}" y1="${a.y}" x2="${b.x}" y2="${b.y}" class="op-line"/>
      <circle r="7" class="op-dot"><animateMotion dur="1.4s" repeatCount="indefinite" path="M${a.x},${a.y} L${b.x},${b.y}"/></circle>`;
  }
  const nodes = SECTORS.map((x) => {
    const st = E.sectorStatus(s, x);
    const p = P(x);
    const r = x.id === MAP.home ? 38 : x.boss ? 34 : 27;
    const known = st !== 'far';
    const color = x.faction ? FACTIONS[x.faction].color : 'var(--hud)';
    const label = known ? x.name : 'Unknown signal';
    const ic = st === 'owned' ? (x.id === MAP.home ? 'core' : 'check') : st === 'locked' ? 'lock' : known ? FACTIONS[x.faction].icon : 'hex';
    return `
      <g class="node st-${st} ${x.boss ? 'boss' : ''} ${ui.sector === x.id ? 'sel' : ''} ${s.op && s.op.sector === x.id ? 'attacking' : ''}" data-sector="${x.id}" transform="translate(${p.x},${p.y})" style="--fc:${color}" tabindex="0" role="button" aria-label="${esc(label)}">
        ${st === 'target' ? `<polygon class="pulse" points="${hex(r + 6)}"><animateTransform attributeName="transform" type="scale" from="1" to="1.5" dur="2s" repeatCount="indefinite"/><animate attributeName="opacity" from=".9" to="0" dur="2s" repeatCount="indefinite"/></polygon>` : ''}
        <polygon class="hex" points="${hex(r)}"/>
        <use href="#i-${ic}" x="${-r * 0.5}" y="${-r * 0.5}" width="${r}" height="${r}" class="node-ico"/>
        <text class="name" dy="${r + 24}">${esc(label)}</text>
        ${known && st !== 'owned' ? `<text class="def" dy="${-r - 10}">${num(x.defense)}</text>` : ''}
      </g>`;
  }).join('');
  const legend = CHAPTERS.map((c) => {
    const f = FACTIONS[c.faction];
    const capital = SECTORS.find((x) => x.faction === c.faction && x.boss);
    const fallen = s.sectors.includes(capital.id);
    const open = E.chapterOpen(s, c.id);
    return `<span class="leg ${fallen ? 'down' : ''}" style="--fc:${f.color}">${icon(f.icon)}${f.short}<em>${fallen ? 'Defeated' : open ? 'Hostile' : `Core ${c.core}`}</em></span>`;
  }).join('');
  const sec = E.sectorById(ui.sector);
  return `
    <div class="map-card">
      <svg class="map" viewBox="0 0 ${W} ${H}" role="img" aria-label="Wasteland map">
        <defs>${Object.entries(FACTIONS).map(([k, d]) => `<radialGradient id="terr-${k}"><stop offset="0" stop-color="${d.color}" stop-opacity=".16"/><stop offset="1" stop-color="${d.color}" stop-opacity="0"/></radialGradient>`).join('')}</defs>
        ${territory()}
        <g>${links.join('')}</g>
        ${opPath}
        <g>${nodes}</g>
      </svg>
      <div class="legend">${legend}</div>
    </div>
    <div class="sheet ${ui.sheet ? 'open' : ''}" data-sheet>${ui.sheet ? briefing(s, sec) : ''}</div>`;
}

function territory() {
  return Object.keys(FACTIONS).filter((f) => f !== 'rogue').map((f) => {
    const list = SECTORS.filter((x) => x.faction === f).map(P);
    const cx = list.reduce((a, x) => a + x.x, 0) / list.length;
    const cy = list.reduce((a, x) => a + x.y, 0) / list.length;
    return `<circle cx="${cx}" cy="${cy}" r="200" fill="url(#terr-${f})"/>`;
  }).join('');
}

function briefing(s, sec) {
  const st = E.sectorStatus(s, sec);
  const f = sec.faction ? FACTIONS[sec.faction] : null;
  const head = `
    <header class="brief-head" style="--fc:${f ? f.color : 'var(--hud)'}">
      <span class="fac-ico">${icon(st === 'far' ? 'hex' : f ? f.icon : 'core')}</span>
      <div><h3>${st === 'far' ? 'Unknown signal' : sec.name}</h3>
      <small>${st === 'far' ? 'Unscouted' : f ? f.name + (sec.boss ? ' · Capital' : '') : 'Your base'}</small></div>
      <button class="icon-btn small" data-act="close-sheet" aria-label="Close">${icon('close')}</button>
    </header>`;
  if (st === 'owned') {
    return `${head}${sec.bonus ? `<div class="rows"><div class="r"><span>${icon('trend')}Bonus</span><b class="good-t">${bonusText(sec.bonus)}</b></div></div>` : ''}
      <blockquote class="lore">${esc(sec.lore)}</blockquote>`;
  }
  if (st === 'far') {
    return `${head}<p class="muted">The signal is too faint. Capture a neighbouring sector to scout it.</p>`;
  }
  const ch = E.chapterOf(sec.chapter);
  const running = s.op && s.op.sector === sec.id;
  return `${head}
    <div class="duel">
      <div data-tip="factor:power"><small>${icon('power')}Your power</small><b data-power></b></div>
      <div class="odds-ring"><b data-chance></b><small>odds</small></div>
      <div><small>${icon('defense')}Their defense</small><b>${num(sec.defense)}</b></div>
    </div>
    <div class="oddsbar"><i data-odds></i></div>
    <div class="rows">
      <div class="r"><span>${icon('clock')}Duration</span><b>${time(E.opTime(sec))}</b></div>
      <div class="r"><span>${icon('spark')}Spoils</span><b>${tags(E.opLoot(sec), '+')}</b></div>
      <div class="r"><span>${icon('trend')}Permanent</span><b class="good-t">${bonusText(sec.bonus)}</b></div>
      ${sec.boss ? `<div class="r"><span>${icon('stop')}Capital</span><b class="good-t">Ends ${f.short} raids</b></div>` : ''}
      <div class="r"><span>${icon('skull')}If it fails</span><b class="bad-t">−${pct(OPS.staffLossOnDefeat)} troops</b></div>
    </div>
    ${st === 'locked'
      ? `<p class="hint">${icon('lock')}Opens in ${ch.title} (Chapter ${ch.id}) at ${icon('core')}AI Core Lv ${ch.core}.</p>`
      : `<div class="launch-row"><div class="costs">${costChips(E.opCost(sec))}</div>
         <button class="btn primary launch" data-act="launch" data-id="${sec.id}">${icon('power')}<span data-l></span></button></div>
         ${running ? '<div class="timebar"><i data-opbar></i></div>' : ''}`}`;
}

export function bindMapScreen(panel) {
  const q = (sel) => panel.querySelector(sel);
  return {
    factors: [...panel.querySelectorAll('[data-factor]')],
    power: q('[data-power]'), chance: q('[data-chance]'), odds: q('[data-odds]'),
    chips: [...panel.querySelectorAll('.sheet .chip')], btn: q('.launch'), label: q('.launch [data-l]'), opbar: q('[data-opbar]'),
  };
}

export function updateMapScreen(s, ui, refs) {
  const f = E.factors(s);
  for (const el of refs.factors) {
    el.textContent = num(f[el.dataset.factor]);
  }
  if (!refs.power || !ui.sheet) {
    return;
  }
  const sec = E.sectorById(ui.sector);
  const p = E.opChance(s, sec);
  refs.power.textContent = num(f.power);
  refs.chance.textContent = pct(p);
  refs.chance.parentElement.className = 'odds-ring odds-' + chanceClass(p);
  refs.odds.style.width = pct(p);
  refs.odds.className = 'bg-' + chanceClass(p);
  if (refs.chips.length) {
    setChips(s, refs.chips, E.opCost(sec));
  }
  if (refs.btn) {
    const running = s.op && s.op.sector === sec.id;
    refs.btn.disabled = !E.canLaunch(s, sec);
    refs.label.textContent = running ? `Under way · ${time(s.op.remaining)}` : s.op ? 'Another op running' : p < 0.5 ? 'Launch anyway' : 'Launch';
    refs.btn.classList.toggle('risky', p < 0.5);
  }
  if (refs.opbar && s.op) {
    refs.opbar.style.width = pct(1 - s.op.remaining / s.op.total);
  }
}

// ---------- archive ----------

function renderArchive(s) {
  const chapters = CHAPTERS.filter((c) => s.chapter >= c.id).map((c) => {
    const t = CHAPTER_TEXT[c.id];
    const fac = FACTIONS[c.faction];
    return `<article class="panel chapter" style="--fc:${fac.color}"><header>${icon(fac.icon)}${t.kicker}</header><h3>${c.title}</h3>${t.lines.map((l) => `<p>${esc(l)}</p>`).join('')}</article>`;
  }).join('');
  const sealed = CHAPTERS.find((c) => s.chapter < c.id);
  const ending = s.ending ? `<article class="panel chapter ending"><header>${icon('spark')}Epilogue</header><h3>${ENDINGS[s.ending].title}</h3>${ENDINGS[s.ending].lines.map((l) => `<p>${esc(l)}</p>`).join('')}<p class="muted">${esc(ENDINGS.after)}</p></article>` : '';
  const frags = SECTORS.filter((x) => s.sectors.includes(x.id));
  const st = s.stats;
  const rec = [
    ['map', 'Sectors held', `${s.sectors.length - 1} / ${SECTORS.length - 1}`],
    ['power', 'Operations won / lost', `${st.opsWon} / ${st.opsLost}`],
    ['defense', 'Raids repelled / suffered', `${st.raidsWon} / ${st.raidsLost}`],
    ['message', 'Orders given / missed', `${st.events - (st.expired || 0)} / ${st.expired || 0}`],
    ['heart', 'Alignment', `${E.alignmentLabel(s.align)} (${s.align > 0 ? '+' : ''}${Math.round(s.align)})`],
  ].map(([ic, k, v]) => `<div class="r"><span>${icon(ic)}${k}</span><b>${v}</b></div>`).join('');
  return `
    <div class="archive">
      ${ending}${chapters}
      ${sealed ? `<article class="panel chapter sealed"><header>${icon('lock')}${CHAPTER_TEXT[sealed.id].kicker}</header><p class="muted">Sealed. Opens at ${icon('core')}AI Core Lv ${sealed.core}.</p></article>` : ''}
      <section class="panel"><header>${icon('book')}Memory fragments<span class="count-badge">${frags.length}/${SECTORS.length}</span></header>
        <ol class="memories">${frags.map((x) => `<li style="--fc:${x.faction ? FACTIONS[x.faction].color : 'var(--hud)'}"><b>${x.name}</b><span>${esc(x.lore)}</span></li>`).join('')}</ol></section>
      <section class="panel"><header>${icon('check')}Service record</header><div class="rows">${rec}</div></section>
      <section class="panel feed"><header>${icon('message')}System log</header><ol class="log"></ol></section>
    </div>`;
}
