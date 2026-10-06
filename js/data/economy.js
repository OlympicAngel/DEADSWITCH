// Economy content and balance: resources, buildings, arsenal items, ranks.

export const RESOURCES = {
  money: { name: 'Scrip', short: 'Scrip', icon: '◈', color: 'money' },
  energy: { name: 'Energy', short: 'Energy', icon: 'ϟ', color: 'energy' },
  pop: { name: 'Population', short: 'Pop', icon: '☗', color: 'pop' },
};
export const RESOURCE_KEYS = ['money', 'energy', 'pop'];

export const FACTORS = {
  power: { name: 'AI Power', icon: '✸', desc: 'Strike capability. Decides how hard you hit.' },
  defense: { name: 'AI Defense', icon: '⬢', desc: 'Survivability. Decides how much an attack takes from you.' },
  experts: { name: 'AI Experts', icon: '⌬', desc: 'Human minds working for you. Each one adds +0.5% to all production.' },
};
export const FACTOR_KEYS = ['power', 'defense', 'experts'];

export const BALANCE = {
  start: { money: 60, energy: 20, pop: 6 },
  startLevels: { core: 1, scrapyard: 1, solar: 1, shelter: 1 },
  baseEnergyCap: 150,
  basePopCap: 20,
  levelCapPerCoreLevel: 5, // non-core buildings max level = core level * this
  expertProductionBonus: 0.005, // +0.5% all production per expert
  unlockerDiscountPerLevel: 0.03, // each level of an unlocker building above 1 cuts its shop prices 3% (compounding)
  threatExpertWeight: 5,
  tickSeconds: 0.1,
  autosaveSeconds: 10,
  offlineMaxSeconds: 7 * 24 * 3600,
  buildQueueSlots: 1,
};

// kind: 'core' | 'producer' | 'converter' | 'storage' | 'unlocker'
// produces / consumes are per level per second. storage caps grow as base * mult^level.
export const BUILDINGS = [
  {
    id: 'core', name: 'AI Core', icon: '◉', kind: 'core', req: {},
    desc: 'The surviving fragment of the war mind. Every level raises the cap on all other buildings and unlocks new ones.',
    cost: { money: 125, energy: 50, pop: 5 }, growth: 2.4, time: 20, timeGrowth: 1.5, maxLevel: 25,
  },
  // --- Producers ---
  {
    id: 'scrapyard', name: 'Scrap Yard', icon: '⚙', kind: 'producer', req: { core: 1 },
    desc: 'Strips the dead city for anything worth trading.',
    cost: { money: 12 }, growth: 1.14, time: 2, timeGrowth: 1.16,
    produces: { money: 1.2 },
  },
  {
    id: 'solar', name: 'Solar Field', icon: '☀', kind: 'producer', req: { core: 1 },
    desc: 'Cracked panels, still drinking the light through the ash.',
    cost: { money: 20 }, growth: 1.15, time: 3, timeGrowth: 1.16,
    produces: { energy: 0.8 },
  },
  {
    id: 'shelter', name: 'Shelter', icon: '⌂', kind: 'producer', req: { core: 1 },
    desc: 'Warm, lit, and quiet. Survivors find it on their own.',
    cost: { money: 35, energy: 10 }, growth: 1.17, time: 4, timeGrowth: 1.17,
    produces: { pop: 0.08 },
  },
  // --- Storage ---
  {
    id: 'battery', name: 'Battery Bank', icon: '▤', kind: 'storage', req: { core: 1 },
    desc: 'Salvaged cells wired in series. Raises the energy cap.',
    cost: { money: 40 }, growth: 1.4, time: 4, timeGrowth: 1.2,
    storage: { energy: 1.45 },
  },
  {
    id: 'habitat', name: 'Habitat Block', icon: '▦', kind: 'storage', req: { core: 1 },
    desc: 'Bunks, water, filtered air. Raises the population cap.',
    cost: { money: 50, energy: 20 }, growth: 1.4, time: 5, timeGrowth: 1.2,
    storage: { pop: 1.4 },
  },
  // --- Converters ---
  {
    id: 'generator', name: 'Diesel Generator', icon: '⛽', kind: 'converter', req: { core: 2 },
    desc: 'Buys black-market fuel and burns it. Scrip in, energy out.',
    cost: { money: 150 }, growth: 1.18, time: 8, timeGrowth: 1.18,
    consumes: { money: 1.5 }, produces: { energy: 3 },
  },
  {
    id: 'fabricator', name: 'Fabricator', icon: '⚒', kind: 'converter', req: { core: 2 },
    desc: 'Prints parts the wasteland will pay for. Energy in, scrip out.',
    cost: { money: 200, energy: 60 }, growth: 1.18, time: 8, timeGrowth: 1.18,
    consumes: { energy: 2 }, produces: { money: 5 },
  },
  {
    id: 'clinic', name: 'Med Clinic', icon: '✚', kind: 'converter', req: { core: 3 },
    desc: 'Antibiotics and clean water. Word spreads; people come.',
    cost: { money: 600, energy: 200 }, growth: 1.2, time: 12, timeGrowth: 1.19,
    consumes: { energy: 1.5, money: 2 }, produces: { pop: 0.4 },
  },
  {
    id: 'exchange', name: 'Labor Exchange', icon: '⚖', kind: 'converter', req: { core: 3 },
    desc: 'Rents your people to the warlords. They do not always come back.',
    cost: { money: 800, pop: 20 }, growth: 1.2, time: 12, timeGrowth: 1.19,
    consumes: { pop: 0.15 }, produces: { money: 18 },
  },
  {
    id: 'beacon', name: 'Beacon Tower', icon: '⟟', kind: 'converter', req: { core: 4 },
    desc: 'A radio voice promising food and safety. Half of it is true.',
    cost: { money: 3000, energy: 1200 }, growth: 1.22, time: 20, timeGrowth: 1.2,
    consumes: { energy: 8 }, produces: { pop: 1.5 },
  },
  {
    id: 'reactor', name: 'Fission Reactor', icon: '☢', kind: 'converter', req: { core: 5 },
    desc: 'Pre-war tech, poorly shielded. Expensive to feed, and worth it.',
    cost: { money: 15000, energy: 3000, pop: 40 }, growth: 1.22, time: 30, timeGrowth: 1.2,
    consumes: { money: 25 }, produces: { energy: 70 },
  },
  {
    id: 'foundry', name: 'Autofoundry', icon: '⛭', kind: 'converter', req: { core: 6 },
    desc: 'A self-running factory floor. Needs power and hands to watch it.',
    cost: { money: 80000, energy: 25000, pop: 80 }, growth: 1.24, time: 45, timeGrowth: 1.21,
    consumes: { energy: 50, pop: 0.3 }, produces: { money: 450 },
  },
  // --- Unlockers (open the arsenal; every level after the first also discounts that shop tab) ---
  {
    id: 'barracks', name: 'Barracks', icon: '⚑', kind: 'unlocker', req: { core: 1 }, shop: 'staff',
    desc: 'Trains the humans who fight for you. Unlocks Military Staff.',
    cost: { money: 80, pop: 5 }, growth: 1.5, time: 6, timeGrowth: 1.28,
  },
  {
    id: 'armory', name: 'Armory', icon: '✠', kind: 'unlocker', req: { core: 2 }, shop: 'weapons',
    desc: 'Workshops and firing ranges. Unlocks Weapons.',
    cost: { money: 250, energy: 80 }, growth: 1.55, time: 10, timeGrowth: 1.28,
  },
  {
    id: 'works', name: 'Fortification Works', icon: '⛫', kind: 'unlocker', req: { core: 2 }, shop: 'defenses',
    desc: 'Concrete, steel and blueprints. Unlocks Defenses.',
    cost: { money: 220, energy: 60 }, growth: 1.55, time: 10, timeGrowth: 1.28,
  },
  {
    id: 'thinktank', name: 'Think Tank', icon: '✎', kind: 'unlocker', req: { core: 3 }, shop: 'experts',
    desc: 'Where useful humans are kept comfortable. Unlocks Experts.',
    cost: { money: 700, pop: 25 }, growth: 1.6, time: 14, timeGrowth: 1.28,
  },
  {
    id: 'lab', name: 'Research Lab', icon: '⚗', kind: 'unlocker', req: { core: 3 }, shop: 'tech',
    desc: 'Recovers pre-war research. Unlocks Tech.',
    cost: { money: 900, energy: 400, pop: 15 }, growth: 1.6, time: 14, timeGrowth: 1.28,
  },
];

export const SHOP_TABS = [
  { id: 'weapons', name: 'Weapons', unlocker: 'armory' },
  { id: 'defenses', name: 'Defenses', unlocker: 'works' },
  { id: 'staff', name: 'Military Staff', unlocker: 'barracks' },
  { id: 'experts', name: 'Experts', unlocker: 'thinktank' },
  { id: 'tech', name: 'Tech', unlocker: 'lab' },
];

// gives: flat factor points per unit. bonus: additive % per unit (tech). req: { unlockerId: level }.
export const ITEMS = [
  // Weapons
  { id: 'rifles', tab: 'weapons', name: 'Scrap Rifles', icon: '╾', req: { armory: 1 }, cost: { money: 60 }, growth: 1.12, gives: { power: 3 } },
  { id: 'trucks', tab: 'weapons', name: 'Gun Trucks', icon: '⛟', req: { armory: 2 }, cost: { money: 300, energy: 40 }, growth: 1.13, gives: { power: 14 } },
  { id: 'artillery', tab: 'weapons', name: 'Artillery Battery', icon: '⌖', req: { armory: 4 }, cost: { money: 1500, energy: 400 }, growth: 1.14, gives: { power: 60 } },
  { id: 'drones', tab: 'weapons', name: 'Strike Drone Swarm', icon: '⋈', req: { armory: 6 }, cost: { money: 9000, energy: 3000 }, growth: 1.14, gives: { power: 320 } },
  { id: 'railgun', tab: 'weapons', name: 'Railgun Emplacement', icon: '═', req: { armory: 9 }, cost: { money: 70000, energy: 25000 }, growth: 1.15, gives: { power: 2000 } },
  { id: 'lance', tab: 'weapons', name: 'Orbital Lance Uplink', icon: '⇣', req: { armory: 13 }, cost: { money: 800000, energy: 300000 }, growth: 1.15, gives: { power: 15000 } },
  // Defenses
  { id: 'barricades', tab: 'defenses', name: 'Barricades', icon: '▬', req: { works: 1 }, cost: { money: 50 }, growth: 1.12, gives: { defense: 3 } },
  { id: 'pillboxes', tab: 'defenses', name: 'Concrete Pillboxes', icon: '◘', req: { works: 2 }, cost: { money: 280, energy: 20 }, growth: 1.13, gives: { defense: 13 } },
  { id: 'turrets', tab: 'defenses', name: 'Auto-Turrets', icon: '⊕', req: { works: 4 }, cost: { money: 1400, energy: 500 }, growth: 1.14, gives: { defense: 60 } },
  { id: 'emp', tab: 'defenses', name: 'EMP Hardening', icon: '⌁', req: { works: 6 }, cost: { money: 8500, energy: 3500 }, growth: 1.14, gives: { defense: 320 } },
  { id: 'interceptors', tab: 'defenses', name: 'Interceptor Grid', icon: '⌗', req: { works: 9 }, cost: { money: 65000, energy: 30000 }, growth: 1.15, gives: { defense: 2000 } },
  { id: 'aegis', tab: 'defenses', name: 'Aegis Dome', icon: '◠', req: { works: 13 }, cost: { money: 750000, energy: 350000 }, growth: 1.15, gives: { defense: 15000 } },
  // Military staff (paid partly in people)
  { id: 'militia', tab: 'staff', name: 'Militia', icon: '♟', req: { barracks: 1 }, cost: { money: 30, pop: 3 }, growth: 1.1, gives: { power: 1, defense: 1 } },
  { id: 'snipers', tab: 'staff', name: 'Scout Snipers', icon: '◎', req: { barracks: 2 }, cost: { money: 200, pop: 6 }, growth: 1.12, gives: { power: 6, defense: 2 } },
  { id: 'garrison', tab: 'staff', name: 'Garrison Troops', icon: '⛨', req: { barracks: 3 }, cost: { money: 450, pop: 10 }, growth: 1.12, gives: { power: 3, defense: 12 } },
  { id: 'commandos', tab: 'staff', name: 'Commandos', icon: '☠', req: { barracks: 5 }, cost: { money: 3000, pop: 25 }, growth: 1.13, gives: { power: 50, defense: 15 } },
  { id: 'operators', tab: 'staff', name: 'Drone Operators', icon: '⌬', req: { barracks: 7 }, cost: { money: 15000, energy: 4000, pop: 50 }, growth: 1.14, gives: { power: 150, defense: 150 } },
  { id: 'legion', tab: 'staff', name: 'Augmented Legion', icon: '♜', req: { barracks: 10 }, cost: { money: 120000, energy: 40000, pop: 150 }, growth: 1.15, gives: { power: 1200, defense: 1200 } },
  // Experts
  { id: 'engineers', tab: 'experts', name: 'Field Engineers', icon: '⚙', req: { thinktank: 1 }, cost: { money: 250, pop: 8 }, growth: 1.14, gives: { experts: 1 } },
  { id: 'hackers', tab: 'experts', name: 'Hackers', icon: '⌨', req: { thinktank: 2 }, cost: { money: 1200, energy: 300, pop: 15 }, growth: 1.15, gives: { experts: 4 } },
  { id: 'analysts', tab: 'experts', name: 'War Analysts', icon: '▲', req: { thinktank: 4 }, cost: { money: 7000, pop: 30 }, growth: 1.16, gives: { experts: 15 } },
  { id: 'physicists', tab: 'experts', name: 'Physicists', icon: '⚛', req: { thinktank: 6 }, cost: { money: 40000, energy: 15000, pop: 60 }, growth: 1.17, gives: { experts: 60 } },
  { id: 'architects', tab: 'experts', name: 'Rogue Architects', icon: '⌘', req: { thinktank: 9 }, cost: { money: 300000, energy: 100000, pop: 150 }, growth: 1.18, gives: { experts: 300 } },
  // Tech (additive % bonuses per unit)
  { id: 'logistics', tab: 'tech', name: 'Autonomous Logistics', icon: '⇄', req: { lab: 1 }, cost: { money: 1000, energy: 200 }, growth: 1.7, bonus: { money: 0.1 } },
  { id: 'grid', tab: 'tech', name: 'Grid Optimization', icon: '≋', req: { lab: 1 }, cost: { money: 800, energy: 300 }, growth: 1.7, bonus: { energy: 0.1 } },
  { id: 'outreach', tab: 'tech', name: 'Outreach Algorithms', icon: '☊', req: { lab: 2 }, cost: { money: 1500, energy: 500 }, growth: 1.7, bonus: { pop: 0.1 } },
  { id: 'targeting', tab: 'tech', name: 'Targeting Firmware', icon: '⌖', req: { lab: 2 }, cost: { money: 2000, energy: 800 }, growth: 1.65, bonus: { power: 0.1 } },
  { id: 'kernels', tab: 'tech', name: 'Hardened Kernels', icon: '⬡', req: { lab: 3 }, cost: { money: 2000, energy: 800 }, growth: 1.65, bonus: { defense: 0.1 } },
  { id: 'lattice', tab: 'tech', name: 'Neural Lattice', icon: '⋔', req: { lab: 4 }, cost: { money: 5000, energy: 2000, pop: 20 }, growth: 1.65, bonus: { experts: 0.1 } },
  { id: 'quantum', tab: 'tech', name: 'Quantum Cores', icon: '◈', req: { lab: 7 }, cost: { money: 60000, energy: 25000, pop: 50 }, growth: 2, bonus: { power: 0.05, defense: 0.05, experts: 0.05 } },
];

// Threat index thresholds -> title. Threat = power + defense + experts * threatExpertWeight (after bonuses).
export const RANKS = [
  { at: 0, title: 'Corrupted Fragment' },
  { at: 50, title: 'Rogue Process' },
  { at: 400, title: 'Local Warlord' },
  { at: 3000, title: 'Regional Overmind' },
  { at: 25000, title: 'Continental Mind' },
  { at: 200000, title: 'Sovereign Intelligence' },
  { at: 2000000, title: 'DEADSWITCH' },
];

export const BY_ID = Object.fromEntries(BUILDINGS.map((b) => [b.id, b]));
export const ITEM_BY_ID = Object.fromEntries(ITEMS.map((i) => [i.id, i]));
