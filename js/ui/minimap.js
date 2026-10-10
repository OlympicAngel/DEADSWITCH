// The map drawn behind whatever is about a place: the same hexes, links and clan colours as the
// theater, at the theater's own scale. Static and inert: nothing to drag or tap.
import { FACTIONS, MAP } from '../data.js';
import * as E from '../engine.js';
import { esc } from '../format.js';
import { hex, nodeR, shownSectors } from './screens/map.js';

// Crosshair over the sector under attack, in the attacker's colour.
function ring(sec, r, color) {
  return `<g class="mini-ring" style="--fc:${color}" transform="translate(${sec.x},${sec.y})">
    <circle r="${r}"/><path d="M0,${-r - 18} V${-r - 4} M0,${r + 4} V${r + 18} M${-r - 18},0 H${-r - 4} M${r + 4},0 H${r + 18}"/></g>`;
}

function node(s, sec, framed, label, zoom) {
  const st = E.sectorStatus(s, sec);
  const mine = st === 'owned';
  const r = nodeR(sec) * (framed ? zoom : zoom * 0.7);
  const f = E.factionOf(s, sec.id);
  const color = f ? FACTIONS[f].color : 'var(--hud)';
  const ic = mine ? (sec.id === MAP.home ? 'core' : 'check') : st === 'far' ? 'hex' : FACTIONS[f].icon;
  const name = st === 'far' ? 'Unknown' : sec.name;
  return `<g class="mini-node st-${st} ${framed ? 'framed' : 'dim'}" transform="translate(${sec.x},${sec.y})" style="--fc:${color}">
    <polygon class="hex" points="${hex(r)}"/>
    <use href="#i-${ic}" x="${-r * 0.5}" y="${-r * 0.5}" width="${r}" height="${r}" class="node-ico"/>
    ${framed && label ? `<text class="name" dy="${r + 40}">${esc(name)}</text>` : ''}</g>`;
}

// ---------- backdrop ----------

// A panel's or dialog's own window onto the theater: the real map at its own scale, anchored so the
// sectors it is about sit in the top right, where no text goes. Everything else is just scenery.
// The box is always as wide as its host, so the scale is fixed and the anchor never drifts.
const WINDOW = { w: 1060, h: 1500 }; // map units behind a backdrop, a little wider than the theater shows
const ANCHOR = { modal: { right: 270, top: 450 }, panel: { right: 250, top: 172 } }; // units to the focal sector
const WIDE = 2.1; // a cut scene is the whole screen, so it shows more of the theater than a panel does

/** @param {object} opts beam: [fromId, toId]; cls: which host it sits in: modal, panel or scene. */
export function mapBackdrop(s, ids, { beam = null, cls = 'modal' } = {}) {
  const scene = cls === 'scene';
  const focus = ids.map((id) => E.sectorById(id)).filter(Boolean);
  if (!focus.length) {
    return '';
  }
  // A panel or dialog puts the focal sector in the top right, where no text goes. A cut scene is
  // the whole screen, so it centres it and pulls back to show the ground around it.
  const w = WINDOW.w * (scene ? WIDE : 1);
  const h = WINDOW.h * (scene ? WIDE : 1);
  const anchor = scene ? { right: w / 2, top: h / 2 } : (ANCHOR[cls] || ANCHOR.modal);
  const x0 = focus[0].x + anchor.right - w;
  const y0 = focus[0].y - anchor.top;
  const near = (x) => x.x > x0 - 300 && x.x < x0 + w + 300 && x.y > y0 - 300 && x.y < y0 + h + 300;
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
  const fromFac = from && E.factionOf(s, from.id);
  const color = fromFac ? FACTIONS[fromFac].color : 'var(--bad)';
  const arrow = from && to
    ? `<line x1="${from.x}" y1="${from.y}" x2="${to.x}" y2="${to.y}" class="mini-beam" style="--fc:${color}"/>${ring(to, nodeR(to) + 22, color)}`
    : '';
  // The top right corner is fixed whatever shape the dialog takes, so the sectors never drift under the text.
  return `<div class="map-bg ${cls}" aria-hidden="true"><svg viewBox="${x0} ${y0} ${w} ${h}" preserveAspectRatio="${scene ? 'xMidYMid slice' : 'xMaxYMin slice'}">
    <g class="mini-links">${links.join('')}</g>${arrow}
    ${[...shown.values()].map((sec) => node(s, sec, focus.includes(sec), sec === focus[0], 1)).join('')}</svg></div>`;
}
