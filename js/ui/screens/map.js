// Map: an open-world theater you drag around (the Nest at the centre), a briefing sheet, and the archive.
import {
  SECTORS, FACTIONS, MAP, CHAPTERS, CHAPTER_TEXT, OPS, NODES, ENDINGS,
} from '../../data.js';
import * as E from '../../engine.js';
import { num, time, pct, esc } from '../../format.js';
import { icon } from '../icons.js';
import { costChips, setChips, tags, bonusText, bonusChips, chanceClass, clock } from '../common.js';
import { sortedTabs } from '../layout.js';
import { put, setCls, setW } from '../dom.js';

const P = (x) => ({ x: x.x, y: x.y });
const REVEAL_DEPTH = 2; // rings of sectors shown beyond held territory
const SCALE = 0.42; // screen px per map unit
// Node radius in map units (drawn larger than the old column map, since the world is zoomed out).
const nodeR = (x) => (x.id === MAP.home ? 52 : x.boss ? 48 : 38);
const PAD = 170; // map units around the outermost sectors
// World bounds (map units) from every sector, so the world never changes size as it is revealed.
const WORLD = (() => {
  const xs = SECTORS.map((x) => x.x);
  const ys = SECTORS.map((x) => x.y);
  const x0 = Math.min(...xs) - PAD;
  const y0 = Math.min(...ys) - PAD;
  return { x0, y0, w: Math.max(...xs) + PAD - x0, h: Math.max(...ys) + PAD - y0 };
})();
const px = (p) => ({ x: (p.x - WORLD.x0) * SCALE, y: (p.y - WORLD.y0) * SCALE });
// Where the world sits in the viewport (px); kept across rebuilds so the view stays put.
let pan = null;

// Steps from held territory to every sector, through map links.
function distances(s) {
  const dist = Object.fromEntries(s.sectors.map((id) => [id, 0]));
  const queue = [...s.sectors];
  while (queue.length) {
    const id = queue.shift();
    for (const l of E.sectorById(id).links) {
      if (dist[l] === undefined) {
        dist[l] = dist[id] + 1;
        queue.push(l);
      }
    }
  }
  return dist;
}

const hex = (r) => Array.from({ length: 6 }, (_, i) => {
  const a = (Math.PI / 3) * i;
  return `${(Math.cos(a) * r).toFixed(1)},${(Math.sin(a) * r).toFixed(1)}`;
}).join(' ');

// Pixel box (in world px) around the revealed sectors, padded: how far the map can be dragged.
function shownBounds(shown) {
  const ps = shown.map((x) => px(x));
  const m = 140;
  return [Math.min(...ps.map((p) => p.x)) - m, Math.min(...ps.map((p) => p.y)) - m, Math.max(...ps.map((p) => p.x)) + m, Math.max(...ps.map((p) => p.y)) + m].map(Math.round).join(',');
}

// Moves the world so that pan stays inside the revealed area; writes the transform.
function applyPan(view, world) {
  const [x0, y0, x1, y1] = world.dataset.bounds.split(',').map(Number);
  const vw = view.clientWidth;
  const vh = view.clientHeight;
  const clamp = (v, lo, hi) => (lo > hi ? (lo + hi) / 2 : Math.min(hi, Math.max(lo, v)));
  pan.x = clamp(pan.x, vw - x1, -x0);
  pan.y = clamp(pan.y, vh - y1, -y0);
  world.style.transform = `translate3d(${Math.round(pan.x)}px, ${Math.round(pan.y)}px, 0)`;
}

// Centres a sector in the map view (above the briefing sheet when it is open).
export function centerOn(root, id) {
  const view = root.querySelector('[data-mapview]');
  const world = root.querySelector('[data-world]');
  const sec = E.sectorById(id);
  if (!view || !world || !sec) return;
  const sheet = root.querySelector('.sheet.open');
  const vr = view.getBoundingClientRect();
  const room = sheet ? Math.max(120, sheet.getBoundingClientRect().top - vr.top) : view.clientHeight;
  const p = px(sec);
  pan = { x: view.clientWidth / 2 - p.x, y: Math.min(view.clientHeight, room) / 2 - p.y };
  applyPan(view, world);
}

// Drag to move around the map; a drag never counts as a tap on a sector.
function bindPan(root) {
  const view = root.querySelector('[data-mapview]');
  const world = root.querySelector('[data-world]');
  if (!view || !world) return;
  if (!pan) {
    pan = { x: 0, y: 0 };
    centerOn(root, MAP.home);
  } else {
    applyPan(view, world);
  }
  let drag = null;
  let moved = false;
  view.addEventListener('pointerdown', (e) => {
    if (e.button !== undefined && e.button > 0) return;
    drag = { id: e.pointerId, x: e.clientX, y: e.clientY, px: pan.x, py: pan.y };
    moved = false;
  });
  view.addEventListener('pointermove', (e) => {
    if (!drag || e.pointerId !== drag.id) return;
    const dx = e.clientX - drag.x;
    const dy = e.clientY - drag.y;
    if (!moved && Math.hypot(dx, dy) < 6) return;
    if (!moved) {
      moved = true;
      view.setPointerCapture(e.pointerId);
      view.classList.add('dragging');
    }
    pan.x = drag.px + dx;
    pan.y = drag.py + dy;
    applyPan(view, world);
  });
  const end = () => {
    drag = null;
    view.classList.remove('dragging');
  };
  view.addEventListener('pointerup', end);
  view.addEventListener('pointercancel', end);
  view.addEventListener('click', (e) => {
    if (moved) {
      e.stopPropagation();
      e.preventDefault();
      moved = false;
    }
  }, true);
}

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
      ${s.op ? `<button class="op-banner" data-sector="${s.op.sector}">${icon('power')}<span>Attacking <b>${E.sectorById(s.op.sector).name}</b></span><b data-opbanner></b><i class="op-banner-bar" data-opbannerbar></i></button>` : ''}
      ${inner === 'archive' ? renderArchive(s) : renderTheater(s, ui)}
    </div>`;
}

function renderTheater(s, ui) {
  const dist = distances(s);
  const shown = SECTORS.filter((x) => dist[x.id] !== undefined && dist[x.id] <= REVEAL_DEPTH);
  const isShown = (id) => shown.some((x) => x.id === id);
  const links = [];
  const fronts = [];
  for (const a of shown) {
    for (const id of a.links) {
      if (a.id < id && isShown(id)) {
        const b = E.sectorById(id);
        const own = s.sectors.includes(a.id) && s.sectors.includes(b.id);
        const front = s.sectors.includes(a.id) !== s.sectors.includes(b.id);
        const pa = P(a);
        const pb = P(b);
        if (front) {
          // Marching front lines are drawn in the GPU layer (see marchLine).
          fronts.push([pa, pb]);
        } else {
          links.push(`<line x1="${pa.x}" y1="${pa.y}" x2="${pb.x}" y2="${pb.y}" class="link ${own ? 'own' : ''}"/>`);
        }
      }
    }
  }
  let opPath = '';
  if (s.op) {
    // The strike comes from every held sector linked to the target.
    const t = E.sectorById(s.op.sector);
    const b = P(t);
    opPath = t.links.filter((l) => s.sectors.includes(l)).map((id) => {
      const a = P(E.sectorById(id));
      const path = `M${a.x},${a.y} L${b.x},${b.y}`;
      const dots = [0, 0.45, 0.9].map((d) => `<circle r="8" class="op-dot"><animateMotion dur="1.35s" begin="${d}s" repeatCount="indefinite" path="${path}"/></circle>`).join('');
      return `<line x1="${a.x}" y1="${a.y}" x2="${b.x}" y2="${b.y}" class="op-glow"/>
        <line x1="${a.x}" y1="${a.y}" x2="${b.x}" y2="${b.y}" class="op-line"/>${dots}`;
    }).join('');
  }
  // Pulses and the reticle ring live in small HTML layers over/under the SVG, animated with
  // transform/opacity on the GPU; animating them inside the SVG repaints the whole map every frame.
  const W = WORLD.w;
  const L = (x) => ((x - WORLD.x0) / W) * 100;
  const T = (y) => ((y - WORLD.y0) / WORLD.h) * 100;
  const fxAt = (p, R, cls, inner, style = '') => `<div class="${cls}" style="left:${L(p.x)}%;top:${T(p.y)}%;width:${(2 * R / W) * 100}%;margin:-${(R / W) * 100}% 0 0 -${(R / W) * 100}%;${style}"><svg viewBox="${-R} ${-R} ${2 * R} ${2 * R}" aria-hidden="true">${inner}</svg></div>`;
  const pulses = shown.filter((x) => E.sectorStatus(s, x) === 'target').map((x) => {
    const r = nodeR(x);
    return fxAt(P(x), r + 8, 'fx-pulse', `<polygon class="pulse" points="${hex(r + 8)}"/>`, `--fc:${x.faction ? FACTIONS[x.faction].color : 'var(--hud)'}`);
  }).join('');
  // A dashed line whose dashes travel along it: the strip is rotated into place and its contents
  // slide by two dash periods per cycle, which looks the same as animating the dash offset.
  const marchLine = ([a, b]) => {
    const len = Math.hypot(b.x - a.x, b.y - a.y);
    const ang = (Math.atan2(b.y - a.y, b.x - a.x) * 180) / Math.PI;
    return `<div class="fx-march" style="left:${L(a.x)}%;top:${T(a.y)}%;width:${(len / W) * 100}%;aspect-ratio:${len}/10;margin-top:-${(5 / W) * 100}%;transform:rotate(${ang}deg)"><svg viewBox="-32 -5 ${len + 32} 10" style="width:${((len + 32) / len) * 100}%;margin-left:-${(32 / len) * 100}%;--slide:${(32 / (len + 32)) * 100}%" aria-hidden="true"><line x1="-32" y1="0" x2="${len}" y2="0" class="link front"/></svg></div>`;
  };
  const marches = fronts.map(marchLine).join('');
  const ring = s.op ? fxAt(P(E.sectorById(s.op.sector)), 74, 'fx-ring', '<circle r="66" class="ret-ring"/>') : '';
  // An incoming assault: a static line from the attacking sector to its target.
  const a = s.assault;
  const assaultLine = a && isShown(a.from) && isShown(a.target)
    ? (() => { const f = P(E.sectorById(a.from)); const t = P(E.sectorById(a.target)); return `<line x1="${f.x}" y1="${f.y}" x2="${t.x}" y2="${t.y}" class="assault-line"/>`; })()
    : '';
  const nodes = shown.map((x) => {
    const st = E.sectorStatus(s, x);
    const p = P(x);
    const r = nodeR(x);
    const known = st !== 'far';
    const color = x.faction ? FACTIONS[x.faction].color : 'var(--hud)';
    const label = known ? x.name : 'Unknown signal';
    const ic = st === 'owned' ? (x.id === MAP.home ? 'core' : 'check') : st === 'locked' ? 'lock' : known ? FACTIONS[x.faction].icon : 'hex';
    return `
      <g class="node st-${st} ${x.boss ? 'boss' : ''} ${ui.sector === x.id ? 'sel' : ''} ${s.op && s.op.sector === x.id ? 'attacking' : ''}" data-sector="${x.id}" transform="translate(${p.x},${p.y})" style="--fc:${color}" tabindex="0" role="button" aria-label="${esc(label)}">
        <polygon class="hex" points="${hex(r)}"/>
        <use href="#i-${ic}" x="${-r * 0.5}" y="${-r * 0.5}" width="${r}" height="${r}" class="node-ico"/>
        <text class="name" dy="${r + 34}">${esc(label)}</text>
        ${known && st !== 'owned' ? defLabel(s, x, r) : ''}
        ${st === 'owned' && E.breaches(s, x.id) ? `<text class="def breach" dy="${-r - 10}">${E.breaches(s, x.id)}/${NODES.breachesToFall}</text>` : ''}
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
      <div class="map-viewport" data-mapview>
      <div class="map-world" data-world style="width:${WORLD.w * SCALE}px;height:${WORLD.h * SCALE}px" data-bounds="${shownBounds(shown)}">
      <div class="map-fx under">${marches}${pulses}</div>
      <svg class="map" viewBox="${WORLD.x0} ${WORLD.y0} ${WORLD.w} ${WORLD.h}" role="img" aria-label="Wasteland map">
        <defs>${Object.entries(FACTIONS).map(([k, d]) => `<radialGradient id="terr-${k}"><stop offset="0" stop-color="${d.color}" stop-opacity=".16"/><stop offset="1" stop-color="${d.color}" stop-opacity="0"/></radialGradient>`).join('')}</defs>
        ${territory(shown)}
        <g>${links.join('')}</g>
        ${opPath}
        ${assaultLine}
        <g>${nodes}</g>
        ${s.op ? reticle(P(E.sectorById(s.op.sector))) : ''}
      </svg>
      <div class="map-fx over">${ring}</div>
      </div>
      </div>
      <div class="legend">${legend}</div>
    </div>
    <div class="sheet ${ui.sheet ? 'open' : ''}" data-sheet>${ui.sheet ? briefing(s, sec) : ''}</div>`;
}

// Crosshair over the sector under attack, with the live countdown.
function reticle(p) {
  return `<g class="reticle" transform="translate(${p.x},${p.y})">
    <path d="M0,-82 V-62 M0,62 V82 M-82,0 H-62 M62,0 H82" class="ret-ticks"/>
    <text class="ret-time" dy="-92" data-optimer></text>
  </g>`;
}

// Defense above an enemy sector: ▲ fortified by approaches, ↑/↓ stronger/weaker than its base.
function defLabel(s, x, r) {
  const fort = E.flank(s, x).bonus;
  const m = E.nodeStrength(s, x.id);
  const drift = m > 1.005 ? ' ↑' : m < 0.995 ? ' ↓' : '';
  return `<text class="def ${fort ? 'fort' : ''} ${drift ? (m > 1 ? 'up' : 'down') : ''}" dy="${-r - 10}">${num(E.sectorDefense(s, x))}${fort ? ' ▲' : ''}${drift}</text>`;
}

// Current strength against the sector's base defense.
function strengthRow(s, sec) {
  const m = E.nodeStrength(s, sec.id);
  if (Math.abs(m - 1) < 0.005) return '';
  return `<div class="r" data-tip="text" data-tip-text="Base defense ${num(sec.defense)}. Grows ${pct(NODES.growthPerHour)} an hour up to ×${NODES.strengthMax}, +${pct(NODES.opLossGain)} for each operation it repels, −${pct(NODES.defendWinCut)} for each assault of its that fails."><span>${icon('trend')}Strength</span><b class="${m > 1 ? 'bad-t' : 'good-t'}">×${m.toFixed(2)}</b></div>`;
}

// Shown when the target has several approaches.
function fortRow(s, sec) {
  const f = E.flank(s, sec);
  if (f.approaches < 2) return '';
  return `<div class="r" data-tip="text" data-tip-text="Defense +${pct(OPS.flankBonus)} while one approach is held, falling to 0 when all ${f.approaches} are held."><span>${icon('defense')}Approaches held</span><b class="${f.bonus ? 'bad-t' : 'good-t'}">${f.held}/${f.approaches} · ${f.bonus ? '+' + pct(f.bonus) + ' defense' : 'no bonus'}</b></div>`;
}

// A faint glow of each faction's colour under its sectors (territories interlock).
function territory(shown) {
  return shown.filter((x) => x.faction).map((x) => `<circle cx="${x.x}" cy="${x.y}" r="190" fill="url(#terr-${x.faction})"/>`).join('');
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
    const br = E.breaches(s, sec.id);
    return `${head}${sec.bonus ? `<div class="perm"><span>${icon('trend')}Permanent bonus</span>${bonusChips(sec.bonus)}</div>` : ''}
      ${sec.id !== MAP.home ? `<div class="rows"><div class="r" data-tip="text" data-tip-text="Breached assaults on this sector. It falls at ${NODES.breachesToFall}."><span>${icon('alert')}Breaches</span><b class="${br ? 'bad-t' : 'good-t'}">${br} / ${NODES.breachesToFall}</b></div></div>` : ''}
      <blockquote class="lore">${esc(sec.lore)}</blockquote>`;
  }
  if (st === 'far') {
    return `${head}<p class="muted">Out of range. Revealed when a neighbouring sector is held.</p>`;
  }
  const ch = E.chapterOf(sec.chapter);
  const running = s.op && s.op.sector === sec.id;
  return `${head}
    <div class="duel">
      <div data-tip="factor:power"><small>${icon('power')}Your power</small><b data-power></b></div>
      <div class="odds-ring"><b data-chance></b><small>odds</small></div>
      <div><small>${icon('defense')}Their defense</small><b>${num(E.sectorDefense(s, sec))}</b></div>
    </div>
    <div class="oddsbar"><i data-odds></i></div>
    <div class="rows">
      ${fortRow(s, sec)}
      <div class="r"><span>${icon('clock')}Duration</span><b>${time(E.opTime(sec))}</b></div>
      ${strengthRow(s, sec)}
      <div class="r"><span>${icon('spark')}Spoils</span><b>${s.taken.includes(sec.id) ? '<span class="muted">Taken before</span>' : tags(E.opLoot(sec), '+')}</b></div>
      <div class="perm"><span>${icon('trend')}Permanent bonus</span>${bonusChips(sec.bonus)}</div>
      ${E.borders(s).some((b) => b.from === sec.id) ? `<div class="r" data-tip="text" data-tip-text="Typical strength of its assaults on your bordering sectors."><span>${icon('power')}Its assaults</span><b class="bad-t">~${num(E.assaultStrength(s, sec.id))}</b></div>` : ''}
      ${sec.boss ? `<div class="r"><span>${icon('stop')}Capital</span><b class="good-t">Ends ${f.short} raids</b></div>` : ''}
      <div class="r"><span>${icon('skull')}If it fails</span><b class="bad-t" data-loss data-tip="text" data-tip-text="Share of troops and weapons lost if the operation fails. Grows with their defense over your power. Durability lowers it per unit."></b></div>
    </div>
    ${st === 'locked'
      ? `<p class="hint">${icon('lock')}Opens in ${ch.title} (Chapter ${ch.id}) at ${icon('core')}AI Core Lv ${ch.core}.</p>`
      : `<div class="launch-row"><div class="costs">${costChips(E.opCost(sec))}</div>
         <button class="btn primary launch" data-act="launch" data-id="${sec.id}">${icon('power')}<span data-l></span></button></div>
         ${running ? '<div class="timebar"><i data-opbar></i></div>' : ''}`}`;
}

export function bindMapScreen(panel) {
  const q = (sel) => panel.querySelector(sel);
  bindPan(panel);
  return {
    factors: [...panel.querySelectorAll('[data-factor]')],
    power: q('[data-power]'), loss: q('[data-loss]'), chance: q('[data-chance]'), odds: q('[data-odds]'),
    chips: [...panel.querySelectorAll('.sheet .chip')], btn: q('.launch'), label: q('.launch [data-l]'), opbar: q('[data-opbar]'),
    optimer: q('[data-optimer]'), opbanner: q('[data-opbanner]'), opbannerbar: q('[data-opbannerbar]'),
  };
}

export function updateMapScreen(s, ui, refs) {
  const f = E.factors(s);
  for (const el of refs.factors) {
    put(el, num(f[el.dataset.factor]));
  }
  if (s.op) {
    const left = clock(s.op.remaining);
    if (refs.optimer) put(refs.optimer, left);
    if (refs.opbanner) put(refs.opbanner, left);
    if (refs.opbannerbar) setW(refs.opbannerbar, pct(1 - s.op.remaining / s.op.total));
  }
  if (!refs.power || !ui.sheet) {
    return;
  }
  const sec = E.sectorById(ui.sector);
  const p = E.opChance(s, sec);
  put(refs.power, num(f.power));
  put(refs.chance, pct(p));
  if (refs.loss) {
    const d = E.sectorDefense(s, sec);
    put(refs.loss, `−${pct(E.lossShare(OPS.unitLoss.staff, f.power, d))} troops · −${pct(E.lossShare(OPS.unitLoss.weapons, f.power, d))} weapons`);
  }
  setCls(refs.chance.parentElement, 'odds-ring odds-' + chanceClass(p));
  setW(refs.odds, pct(p));
  setCls(refs.odds, 'bg-' + chanceClass(p));
  if (refs.chips.length) {
    setChips(s, refs.chips, E.opCost(sec));
  }
  if (refs.btn) {
    const running = s.op && s.op.sector === sec.id;
    refs.btn.disabled = !E.canLaunch(s, sec);
    put(refs.label, running ? `Under way · ${time(s.op.remaining)}` : s.op ? 'Another op running' : p < 0.5 ? 'Launch anyway' : 'Launch');
    refs.btn.classList.toggle('risky', p < 0.5);
  }
  if (refs.opbar && s.op) {
    setW(refs.opbar, pct(1 - s.op.remaining / s.op.total));
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
