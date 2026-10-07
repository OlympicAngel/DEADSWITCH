// Icon vocabulary: semantic key -> Tabler icon name (MIT, tabler.io). The sprite in icon-sprite.js is
// generated from this list by tools/build-icons.mjs; add a key here, then rerun the script.
import { SPRITE } from './icon-sprite.js';

export const ICONS = {
  // resources and factors
  money: 'coins', energy: 'bolt', pop: 'users',
  power: 'sword', defense: 'shield', experts: 'brain', threat: 'radar-2',
  // navigation
  command: 'radar', economy: 'building-factory-2', military: 'swords', research: 'flask', map: 'map-2',
  // buildings
  core: 'cpu', scrapyard: 'recycle', solar: 'solar-panel', shelter: 'home', battery: 'battery-4', habitat: 'building',
  generator: 'gas-station', fabricator: 'hammer', clinic: 'first-aid-kit', exchange: 'scale', beacon: 'antenna',
  reactor: 'radioactive', foundry: 'building-factory', barracks: 'flag', armory: 'crosshair', works: 'wall',
  thinktank: 'bulb', lab: 'flask-2',
  // weapons
  rifles: 'target-arrow', trucks: 'truck', artillery: 'bomb', drones: 'drone', railgun: 'rocket', lance: 'satellite',
  // defenses
  barricades: 'barrier-block', pillboxes: 'building-castle', turrets: 'focus-2', emp: 'wave-sine', interceptors: 'shield-bolt', aegis: 'umbrella',
  // staff
  militia: 'user', snipers: 'eye', garrison: 'shield-half', commandos: 'skull', operators: 'device-gamepad-2', legion: 'robot',
  // experts
  engineers: 'tool', hackers: 'terminal-2', analysts: 'chart-line', physicists: 'atom', architects: 'compass',
  // tech
  logistics: 'route', grid: 'plug-connected', outreach: 'broadcast', targeting: 'focus', kernels: 'lock', lattice: 'network', quantum: 'atom-2',
  // factions
  scav: 'tools', military_f: 'star', cult: 'eye', halcyon: 'diamond', rogue: 'cpu',
  // ui
  settings: 'settings', clock: 'clock', alert: 'alert-triangle', lock: 'lock', check: 'check', close: 'x', next: 'chevron-right',
  up: 'arrow-up', down: 'arrow-down', fire: 'flame', heart: 'heart', trend: 'trending-up', hourglass: 'hourglass', message: 'message',
  book: 'book', sound: 'volume', mute: 'volume-off', vibrate: 'device-mobile-vibration', export: 'download', import: 'upload',
  trash: 'trash', city: 'building-skyscraper', bell: 'bell-ringing', hex: 'hexagon', spark: 'sparkles', stop: 'hand-stop',
  play: 'player-play', info: 'info-circle',
};

let mounted = false;

export function mountIcons() {
  if (mounted) {
    return;
  }
  mounted = true;
  const holder = document.createElement('div');
  holder.style.display = 'none';
  holder.innerHTML = SPRITE;
  document.body.prepend(holder);
}

// Inline icon: <svg class="i"><use href="#i-key"/></svg>
export function icon(key, cls = '') {
  return `<svg class="i ${cls}" aria-hidden="true"><use href="#i-${ICONS[key] ? key : 'hex'}"/></svg>`;
}
