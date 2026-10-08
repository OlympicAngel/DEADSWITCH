// Navigation structure: bottom tabs and the inner tabs of each domain screen.
// Shop tabs pair an unlocker building with the units it sells, so every aspect lives in one place.
import { BUILDINGS, BY_ID } from '../data.js';
import * as E from '../engine.js';

export const NAV = [
  { id: 'economy', name: 'Economy', icon: 'economy' },
  { id: 'military', name: 'Military', icon: 'military' },
  { id: 'command', name: 'Command', icon: 'command', main: true },
  { id: 'research', name: 'Research', icon: 'research' },
  { id: 'map', name: 'Map', icon: 'map' },
];

export const DOMAINS = {
  economy: {
    title: 'Economy', factors: [],
    tabs: [
      { id: 'production', name: 'Production', icon: 'trend', kinds: ['core', 'producer'] },
      { id: 'conversion', name: 'Conversion', icon: 'exchange', kinds: ['converter'] },
      { id: 'storage', name: 'Storage', icon: 'battery', kinds: ['storage', 'offline'] },
    ],
  },
  military: {
    title: 'Military', factors: ['power', 'defense'],
    tabs: [
      { id: 'offense', name: 'Offense', icon: 'power', unlocker: 'armory', shop: 'weapons' },
      { id: 'defense', name: 'Defense', icon: 'defense', unlocker: 'works', shop: 'defenses' },
      { id: 'troops', name: 'Troops', icon: 'militia', unlocker: 'barracks', shop: 'staff' },
    ],
  },
  research: {
    title: 'Research', factors: ['experts'],
    tabs: [
      { id: 'experts', name: 'Experts', icon: 'experts', unlocker: 'thinktank', shop: 'experts' },
      { id: 'tech', name: 'Tech', icon: 'lab', unlocker: 'lab', shop: 'tech' },
    ],
  },
  map: {
    title: 'Map', factors: ['power'],
    tabs: [
      { id: 'theater', name: 'Theater', icon: 'map' },
      { id: 'archive', name: 'Archive', icon: 'book' },
    ],
  },
};

// open: usable now. available: can be built now. locked: requirement not met.
export function tabStatus(s, tab) {
  if (tab.unlocker) {
    if (E.level(s, tab.unlocker) > 0) return 'open';
    return E.meetsReq(s, BY_ID[tab.unlocker].req) ? 'available' : 'locked';
  }
  if (tab.kinds) {
    return BUILDINGS.some((b) => tab.kinds.includes(b.kind) && E.meetsReq(s, b.req)) ? 'open' : 'locked';
  }
  return 'open';
}

// The building a locked tab is waiting on (its first unmet requirement).
export function tabReq(s, tab) {
  if (tab.unlocker) {
    return Object.keys(BY_ID[tab.unlocker].req).find((id) => E.level(s, id) < BY_ID[tab.unlocker].req[id]) || null;
  }
  if (tab.kinds) {
    const next = BUILDINGS.filter((b) => tab.kinds.includes(b.kind)).sort((a, b) => (a.req.core || 0) - (b.req.core || 0))[0];
    return next ? Object.keys(next.req)[0] : null;
  }
  return null;
}

// A domain whose every inner tab is locked is locked itself; returns the building to unlock it, or null.
export function domainReq(s, domain) {
  const d = DOMAINS[domain];
  if (!d || d.tabs.some((t) => tabStatus(s, t) !== 'locked')) {
    return null;
  }
  const t = d.tabs.find((x) => x.unlocker) || d.tabs[0];
  return tabReq(s, t);
}

// Buildings in a tab that can be built or upgraded right now.
export function readyCount(s, tab) {
  const list = tab.unlocker ? [BY_ID[tab.unlocker]] : tab.kinds ? BUILDINGS.filter((b) => tab.kinds.includes(b.kind)) : [];
  return list.filter((b) => E.buildingStatus(s, b) === 'ready').length;
}

export function domainReady(s, domain) {
  const d = DOMAINS[domain];
  return !!d && d.tabs.some((t) => readyCount(s, t) > 0);
}

const ORDER = { open: 0, available: 1, locked: 2 };

// Unlocked first, locked last; declared order breaks ties.
export function sortedTabs(s, domain) {
  return DOMAINS[domain].tabs.map((t, i) => ({ t, i, st: tabStatus(s, t) }))
    .sort((a, b) => ORDER[a.st] - ORDER[b.st] || a.i - b.i);
}

// Where a building or a shop item lives, for directive "Go" buttons and alerts.
export function locate(kind, id) {
  for (const [domain, d] of Object.entries(DOMAINS)) {
    for (const t of d.tabs) {
      if (kind === 'building' && (t.unlocker === id || (t.kinds && t.kinds.includes(BY_ID[id].kind)))) {
        return { domain, tab: t.id };
      }
      if (kind === 'shop' && t.shop === id) {
        return { domain, tab: t.id };
      }
    }
  }
  return { domain: 'command', tab: null };
}
