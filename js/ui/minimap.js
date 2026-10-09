// A close-up of the corner of the map an attack or an order is about: the same hexes, links and clan
// colours as the theater, framed on the sectors that matter. Static and inert: nothing to drag or tap.
import { FACTIONS, MAP } from '../data.js';
import * as E from '../engine.js';
import { esc } from '../format.js';
import { hex, nodeR, shownSectors } from './screens/map.js';

const PAD = 110; // map units of room around the framed sectors

const ZOOM = 1.6; // framed sectors are drawn larger than on the map: this is a close-up, not a copy

/**
 * @param {object} s game state
 * @param {string[]} ids the sectors the snapshot is about; the frame is built around these
 * @param {object} opts beam: [fromId, toId] draws an attack line; label: names the framed sectors;
 *   cls: which frame size (see .map-shot in the stylesheet)
 */
export function miniMap(s, ids, { beam = null, label = true, cls = '' } = {}) {
  const focus = ids.map((id) => E.sectorById(id)).filter(Boolean);
  if (!focus.length) {
    return '';
  }
  // Neighbours are drawn for context and clipped by the frame, so the close-up reads like the map.
  const shown = new Map(focus.map((x) => [x.id, x]));
  for (const sec of focus) {
    for (const l of sec.links) {
      if (!shown.has(l) && E.sectorById(l)) shown.set(l, E.sectorById(l));
    }
  }
  const xs = focus.map((x) => x.x);
  const ys = focus.map((x) => x.y);
  const x0 = Math.min(...xs) - PAD;
  const y0 = Math.min(...ys) - PAD;
  const w = Math.max(...xs) + PAD - x0;
  const h = Math.max(...ys) + PAD - y0;
  const links = [];
  for (const a of shown.values()) {
    for (const id of a.links) {
      const b = shown.get(id);
      if (b && a.id < id) {
        const own = s.sectors.includes(a.id) && s.sectors.includes(b.id);
        const front = s.sectors.includes(a.id) !== s.sectors.includes(b.id);
        links.push(`<line x1="${a.x}" y1="${a.y}" x2="${b.x}" y2="${b.y}" class="link ${own ? 'own' : front ? 'front' : ''}"/>`);
      }
    }
  }
  const from = beam && E.sectorById(beam[0]);
  const to = beam && E.sectorById(beam[1]);
  const color = from && from.faction ? FACTIONS[from.faction].color : 'var(--bad)';
  const arrow = from && to
    ? `<line x1="${from.x}" y1="${from.y}" x2="${to.x}" y2="${to.y}" class="mini-beam" style="--fc:${color}"/>
       ${ring(to, nodeR(to) * ZOOM + 24, color)}`
    : '';
  const nodes = [...shown.values()].map((sec) => node(s, sec, focus.includes(sec), label, ZOOM)).join('');
  const names = focus.map((x) => x.name).join(' → ');
  // The frame takes the shape of what it holds, so nothing is cropped and no space is wasted.
  return `<div class="map-shot ${cls}" style="aspect-ratio:${Math.round(w)}/${Math.round(h)}">
    <svg class="minimap" viewBox="${x0} ${y0} ${w} ${h}" role="img" aria-label="${esc(names)}">
    <g class="mini-links">${links.join('')}</g>${arrow}${nodes}</svg></div>`;
}

// Crosshair over the sector under attack, in the attacker's colour.
function ring(sec, r, color) {
  return `<g class="mini-ring" style="--fc:${color}" transform="translate(${sec.x},${sec.y})">
    <circle r="${r}"/><path d="M0,${-r - 18} V${-r - 4} M0,${r + 4} V${r + 18} M${-r - 18},0 H${-r - 4} M${r + 4},0 H${r + 18}"/></g>`;
}

function node(s, sec, framed, label, zoom) {
  const st = E.sectorStatus(s, sec);
  const mine = st === 'owned';
  const r = nodeR(sec) * (framed ? zoom : zoom * 0.7);
  const color = sec.faction ? FACTIONS[sec.faction].color : 'var(--hud)';
  const ic = mine ? (sec.id === MAP.home ? 'core' : 'check') : st === 'far' ? 'hex' : FACTIONS[sec.faction].icon;
  const name = st === 'far' ? 'Unknown' : sec.name;
  return `<g class="mini-node st-${st} ${framed ? 'framed' : 'dim'}" transform="translate(${sec.x},${sec.y})" style="--fc:${color}">
    <polygon class="hex" points="${hex(r)}"/>
    <use href="#i-${ic}" x="${-r * 0.5}" y="${-r * 0.5}" width="${r}" height="${r}" class="node-ico"/>
    ${framed && label ? `<text class="name" dy="${r + 40}">${esc(name)}</text>` : ''}</g>`;
}

// ---------- backdrop ----------

// A dialog's own window onto the theater: the real map at its own scale, anchored so the sectors the
// dialog is about sit in the top right, where no text goes. Everything else is just scenery.
const WINDOW = { w: 1060, h: 1500 }; // map units behind a modal, a little wider than the theater shows
const ANCHOR = { right: 270, top: 450 }; // units from the top right corner to the focal sector

export function mapBackdrop(s, ids, { beam = null } = {}) {
  const focus = ids.map((id) => E.sectorById(id)).filter(Boolean);
  if (!focus.length) {
    return '';
  }
  // The first sector is the focal point; the rest of the map falls where it really is around it.
  const x0 = focus[0].x + ANCHOR.right - WINDOW.w;
  const y0 = focus[0].y - ANCHOR.top;
  const near = (x) => x.x > x0 - 300 && x.x < x0 + WINDOW.w + 300 && x.y > y0 - 300 && x.y < y0 + WINDOW.h + 300;
  const shown = new Map(shownSectors(s).filter(near).map((x) => [x.id, x]));
  for (const sec of focus) {
    shown.set(sec.id, sec);
  }
  const links = [];
  for (const a of shown.values()) {
    for (const id of a.links) {
      const b = shown.get(id);
      if (b && a.id < id) {
        const own = s.sectors.includes(a.id) && s.sectors.includes(b.id);
        const front = s.sectors.includes(a.id) !== s.sectors.includes(b.id);
        links.push(`<line x1="${a.x}" y1="${a.y}" x2="${b.x}" y2="${b.y}" class="link ${own ? 'own' : front ? 'front' : ''}"/>`);
      }
    }
  }
  const from = beam && E.sectorById(beam[0]);
  const to = beam && E.sectorById(beam[1]);
  const color = from && from.faction ? FACTIONS[from.faction].color : 'var(--bad)';
  const arrow = from && to
    ? `<line x1="${from.x}" y1="${from.y}" x2="${to.x}" y2="${to.y}" class="mini-beam" style="--fc:${color}"/>${ring(to, nodeR(to) + 22, color)}`
    : '';
  // The top right corner is fixed whatever shape the dialog takes, so the sectors never drift under the text.
  return `<div class="modal-map" aria-hidden="true"><svg viewBox="${x0} ${y0} ${WINDOW.w} ${WINDOW.h}" preserveAspectRatio="xMaxYMin slice">
    <g class="mini-links">${links.join('')}</g>${arrow}
    ${[...shown.values()].map((sec) => node(s, sec, focus.includes(sec), sec === focus[0], 1)).join('')}</svg></div>`;
}
