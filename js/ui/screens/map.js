// Map: an open-world theater you drag around (the Nest at the centre), a briefing sheet, and the archive.
import {
  SECTORS, FACTIONS, MAP, CHAPTERS, OPS, NODES,
} from '../../data.js';
import * as E from '../../engine.js';
import { num, time, pct, esc } from '../../format.js';
import { icon } from '../icons.js';
import { costChips, setChips, tags, bonusText, bonusChips, chanceClass, clock } from '../common.js';
import { put, setCls, setW } from '../dom.js';

const P = (x) => ({ x: x.x, y: x.y });
const REVEAL_DEPTH = 2; // rings of sectors shown beyond held territory
const SCALE = 0.42; // screen px per map unit
// Node radius in map units (drawn larger than the old column map, since the world is zoomed out).
export const nodeR = (x) => (x.id === MAP.home ? 52 : x.boss ? 48 : 38);
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
// Where the world sits in the viewport (px) and how far it is zoomed; kept across rebuilds.
let pan = null;
let zoom = 1;
const ZOOM = { min: 0.5, max: 1.5, step: 1.25 };

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

export const hex = (r) => Array.from({ length: 6 }, (_, i) => {
  const a = (Math.PI / 3) * i;
  return `${(Math.cos(a) * r).toFixed(1)},${(Math.sin(a) * r).toFixed(1)}`;
}).join(' ');

// An attack beam between two sectors: a glowing dashed line with pulses running along it. The strip is
// rotated into place and its dashes and pulses only move by transform, so nothing repaints per frame.
const BEAM_DASH = 46; // px of one dash plus gap: how far the dashes slide per cycle
function beam(a, b, color) {
  const pa = px(a);
  const pb = px(b);
  const len = Math.hypot(pb.x - pa.x, pb.y - pa.y);
  const ang = (Math.atan2(pb.y - pa.y, pb.x - pa.x) * 180) / Math.PI;
  const dots = [0, 0.4, 0.8].map((d) => `<i style="animation-delay:${d}s"></i>`).join('');
  return `<div class="fx-beam" style="left:${pa.x}px;top:${pa.y}px;width:${len}px;transform:rotate(${ang}deg);--fc:${color};--run:${len}px">
    <svg viewBox="${-BEAM_DASH} -9 ${len + BEAM_DASH} 18" style="width:${len + BEAM_DASH}px;margin-left:${-BEAM_DASH}px;--slide:${BEAM_DASH}px" aria-hidden="true">
      <line x1="${-BEAM_DASH}" y1="0" x2="${len}" y2="0" class="beam-glow"/>
      <line x1="${-BEAM_DASH}" y1="0" x2="${len}" y2="0" class="beam-line"/>
    </svg>${dots}</div>`;
}

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
  pan.x = clamp(pan.x, vw - x1 * zoom, -x0 * zoom);
  pan.y = clamp(pan.y, vh - y1 * zoom, -y0 * zoom);
  world.style.transform = `translate3d(${Math.round(pan.x)}px, ${Math.round(pan.y)}px, 0) scale(${zoom})`;
}

/** Zooms around a point of the viewport, keeping whatever sits under it in place. */
function setZoom(view, world, z, cx, cy) {
  const next = Math.min(ZOOM.max, Math.max(ZOOM.min, z));
  if (next === zoom) return;
  pan.x = cx - ((cx - pan.x) / zoom) * next;
  pan.y = cy - ((cy - pan.y) / zoom) * next;
  zoom = next;
  applyPan(view, world);
}

// Centres a sector in the map view (above the briefing sheet when it is open), gliding there.
export function centerOn(root, id, animate = true) {
  const view = root.querySelector('[data-mapview]');
  const world = root.querySelector('[data-world]');
  const sec = E.sectorById(id);
  if (!view || !world || !sec) return;
  const sheet = root.querySelector('.sheet.open');
  const vr = view.getBoundingClientRect();
  const room = sheet ? Math.max(120, sheet.getBoundingClientRect().top - vr.top) : view.clientHeight;
  const p = px(sec);
  pan = { x: view.clientWidth / 2 - p.x * zoom, y: Math.min(view.clientHeight, room) / 2 - p.y * zoom };
  // Glide there, so it is clear the map moved rather than cut. Dragging never gets the transition.
  if (animate) {
    world.classList.add('gliding');
    world.addEventListener('transitionend', () => world.classList.remove('gliding'), { once: true });
    void world.offsetWidth; // the browser has to see where the map is now, or there is nothing to glide from
  }
  applyPan(view, world);
}

// Drag to move around the map, pinch or wheel to zoom; a drag never counts as a tap on a sector.
function bindPan(root) {
  const view = root.querySelector('[data-mapview]');
  const world = root.querySelector('[data-world]');
  if (!view || !world) return;
  if (!pan) {
    pan = { x: 0, y: 0 };
    centerOn(root, MAP.home, false);
  } else {
    applyPan(view, world);
  }
  /** Pointer position inside the viewport. */
  const at = (e) => {
    const r = view.getBoundingClientRect();
    return { x: e.clientX - r.left, y: e.clientY - r.top };
  };
  const points = new Map(); // live pointers, so two of them can pinch
  let drag = null;
  let pinch = null;
  let moved = false;
  view.addEventListener('pointerdown', (e) => {
    if (e.button !== undefined && e.button > 0) return;
    points.set(e.pointerId, at(e));
    if (points.size === 2) {
      const [a, b] = [...points.values()];
      pinch = { dist: Math.hypot(a.x - b.x, a.y - b.y) || 1, zoom };
      drag = null;
      moved = true; // a pinch must not land as a tap on a sector
      return;
    }
    drag = { id: e.pointerId, x: e.clientX, y: e.clientY, px: pan.x, py: pan.y };
    moved = false;
  });
  view.addEventListener('pointermove', (e) => {
    if (points.has(e.pointerId)) points.set(e.pointerId, at(e));
    if (pinch && points.size >= 2) {
      const [a, b] = [...points.values()];
      setZoom(view, world, (pinch.zoom * Math.hypot(a.x - b.x, a.y - b.y)) / pinch.dist, (a.x + b.x) / 2, (a.y + b.y) / 2);
      return;
    }
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
  const end = (e) => {
    points.delete(e.pointerId);
    if (points.size < 2) pinch = null;
    drag = null;
    view.classList.remove('dragging');
  };
  view.addEventListener('pointerup', end);
  view.addEventListener('pointercancel', end);
  view.addEventListener('wheel', (e) => {
    e.preventDefault();
    const p = at(e);
    setZoom(view, world, zoom * (e.deltaY < 0 ? 1.1 : 1 / 1.1), p.x, p.y);
  }, { passive: false });
  for (const b of root.querySelectorAll('[data-zoom]')) {
    b.addEventListener('pointerdown', (e) => e.stopPropagation());
    b.addEventListener('click', () => {
      const step = b.dataset.zoom === 'in' ? ZOOM.step : 1 / ZOOM.step;
      setZoom(view, world, zoom * step, view.clientWidth / 2, view.clientHeight / 2);
    });
  }
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
  return `
    <div class="screen mapscreen">
      <header class="screen-head">
        <h2>${icon('map')}Wasteland</h2>
        <div class="stats"><div class="stat stat-power" data-tip="factor:power">${icon('power')}<b data-factor="power"></b><small>Power</small></div>
        <div class="stat"><b>${s.sectors.length - 1}/${SECTORS.length - 1}</b><small>Sectors</small></div></div>
      </header>
      ${s.op ? `<button class="op-banner" data-sector="${s.op.sector}">${icon('power')}<span>Attacking <b>${E.sectorById(s.op.sector).name}</b></span><b data-opbanner></b><i class="op-banner-bar" data-opbannerbar></i></button>` : ''}
      ${renderTheater(s, ui)}
    </div>`;
}

/** The sector the map should open on while something is happening: an attack first, then our strike. */
export function liveSector(s) {
  const atk = E.attacks(s).map((a) => E.attackPlace(s, a)).find(Boolean);
  if (atk) return atk[1];
  return s.op ? s.op.sector : null;
}

/** Every sector the player can see: held territory plus REVEAL_DEPTH rings around it. */
export function shownSectors(s) {
  const dist = distances(s);
  return SECTORS.filter((x) => dist[x.id] !== undefined && dist[x.id] <= REVEAL_DEPTH);
}

function renderTheater(s, ui) {
  const shown = shownSectors(s);
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
  // Every inbound attack that still has a place is drawn like your own strike, in its clan's colour.
  const inbound = E.attacks(s).map((a) => {
    const place = E.attackPlace(s, a);
    return place && isShown(place[0]) && isShown(place[1])
      ? { from: E.sectorById(place[0]), target: E.sectorById(place[1]), color: FACTIONS[a.faction].color } : null;
  }).filter(Boolean);
  const atk = inbound[0] || null;
  // Your strike comes from every held sector linked to the target.
  const opTarget = s.op ? E.sectorById(s.op.sector) : null;
  const beams = (opTarget ? opTarget.links.filter((l) => s.sectors.includes(l)).map((id) => beam(P(E.sectorById(id)), P(opTarget), 'var(--warn)')).join('') : '')
    + inbound.map((a) => beam(P(a.from), P(a.target), a.color)).join('');
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
  // Crosshair over a sector under attack: one layer, rotated whole, in the attacker's colour.
  const crosshair = (p, color) => fxAt(p, 86, 'fx-ring', '<circle r="66" class="ret-ring"/><path d="M0,-86 V-64 M0,64 V86 M-86,0 H-64 M64,0 H86" class="ret-ticks"/>', `--rc:${color}`);
  const rings = (opTarget ? crosshair(P(opTarget), 'var(--bad)') : '') + (atk ? crosshair(P(atk.target), atk.color) : '');
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
        ${known && st !== 'owned' ? defLabel(s, x, r) + stancePin(s, x, r) : ''}
        ${st === 'owned' && E.breaches(s, x.id) ? `<text class="def breach" dy="${-r - 10}">${E.breaches(s, x.id)}/${NODES.breachesToFall}</text>` : ''}
      </g>`;
  }).join('');
  const legend = CHAPTERS.map((c) => {
    const f = FACTIONS[c.faction];
    const capital = SECTORS.find((x) => x.faction === c.faction && x.boss);
    const fallen = s.sectors.includes(capital.id);
    const open = E.chapterOpen(s, c.id);
    const stance = E.stanceList(s, c.faction)[0];
    const state = open && stance ? stance.name : fallen ? 'Capital taken' : open ? 'Hostile' : `Core ${c.core}`;
    return `<span class="leg ${fallen ? 'down' : ''}" style="--fc:${f.color}">${icon(f.icon)}${f.short}<em>${state}</em></span>`;
  }).join('');
  const sec = E.sectorById(ui.sector);
  return `
    <div class="map-card">
      <div class="map-viewport" data-mapview>
      <div class="map-world" data-world style="width:${WORLD.w * SCALE}px;height:${WORLD.h * SCALE}px" data-bounds="${shownBounds(shown)}">
      <div class="map-fx under">${marches}${beams}${pulses}</div>
      <svg class="map" viewBox="${WORLD.x0} ${WORLD.y0} ${WORLD.w} ${WORLD.h}" role="img" aria-label="Wasteland map">
        <defs>${Object.entries(FACTIONS).map(([k, d]) => `<radialGradient id="terr-${k}"><stop offset="0" stop-color="${d.color}" stop-opacity=".16"/><stop offset="1" stop-color="${d.color}" stop-opacity="0"/></radialGradient>`).join('')}</defs>
        ${territory(shown)}
        <g>${links.join('')}</g>
        <g>${nodes}</g>
        ${opTarget ? retTimer(P(opTarget), 'data-optimer') : ''}
        ${atk ? retTimer(P(atk.target), 'data-asltimer') : ''}
      </svg>
      <div class="map-fx over">${rings}</div>
      </div>
      <div class="map-zoom">
        <button class="icon-btn small" data-zoom="in" aria-label="Zoom in">${icon('zoomin')}</button>
        <button class="icon-btn small" data-zoom="out" aria-label="Zoom out">${icon('zoomout')}</button>
      </div>
      </div>
      <div class="legend">${legend}</div>
    </div>
    <div class="sheet ${ui.sheet ? 'open' : ''}" data-sheet>${ui.sheet ? briefing(s, sec) : ''}</div>`;
}

// Live countdown over a sector under attack (the crosshair around it is a GPU layer).
function retTimer(p, attr) {
  return `<text class="ret-time" x="${p.x}" y="${p.y - 96}" ${attr}></text>`;
}

// What this sector's clan is doing, as one icon pinned to its hex (sim/clans.js names them).
function stancePin(s, x, r) {
  const st = x.faction && E.stanceList(s, x.faction)[0];
  if (!st) return '';
  return `<g class="stance-pin" transform="translate(${(r * 0.92).toFixed(0)},${(-r * 0.92).toFixed(0)})">
    <circle r="15"/><use href="#i-${st.icon}" x="-9" y="-9" width="18" height="18"/></g>`;
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

// What this sector's clan is doing right now (sim/clans.js); nothing while it is holding its posture.
function postureRow(s, sec) {
  const stances = sec.faction ? E.stanceList(s, sec.faction) : [];
  if (!stances.length) return '';
  return `<div class="r" data-clan="${sec.faction}" data-tip="text" data-tip-text="${esc(stances.map((x) => `${x.name}: ${x.desc}`).join(' '))}"><span>${icon(FACTIONS[sec.faction].icon)}${esc(FACTIONS[sec.faction].short)}</span><b class="bad-t">${esc(stances.map((x) => x.name).join(' · '))}</b></div>`;
}

// Clan support: linked sectors of the same faction that still stand.
function fortRow(s, sec) {
  const f = E.flank(s, sec);
  if (!f.approaches) return '';
  const left = f.approaches - f.held;
  return `<div class="r" data-tip="text" data-tip-text="Defense +${pct(OPS.flankBonus)} while every linked ${esc(FACTIONS[sec.faction].short)} sector stands, falling to 0 as you take them."><span>${icon('defense')}Clan support</span><b class="${f.bonus ? 'bad-t' : 'good-t'}">${left}/${f.approaches} · ${f.bonus ? '+' + pct(f.bonus) + ' defense' : 'no bonus'}</b></div>`;
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
      ${sec.id !== MAP.home ? `<div class="rows"><div class="r" data-tip="text" data-tip-text="Breached assaults on this sector. It falls at ${NODES.breachesToFall}, or on the first breach by an attack ${NODES.overrunRatio} times your AI Defense."><span>${icon('alert')}Breaches</span><b class="${br ? 'bad-t' : 'good-t'}">${br} / ${NODES.breachesToFall}</b></div></div>` : ''}
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
      ${postureRow(s, sec)}
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
    optimer: q('[data-optimer]'), asltimer: q('[data-asltimer]'), opbanner: q('[data-opbanner]'), opbannerbar: q('[data-opbannerbar]'),
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
  if (refs.asltimer && s.assault) {
    put(refs.asltimer, clock(s.assault.remaining));
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
