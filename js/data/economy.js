// Economy content and balance: resources, buildings, arsenal items, ranks.

// Shown in Settings so a player can say which build they are on. Bump it with anything players see.
export const VERSION = '1.12.0';

export const RESOURCES = {
  money: { name: 'Scrip', short: 'Scrip', color: 'money', desc: 'Wasteland currency. No storage limit.' },
  energy: { name: 'Energy', short: 'Energy', color: 'energy', desc: 'Grid power. Stored in Battery Banks.' },
  pop: { name: 'Population', short: 'Pop', color: 'pop', desc: 'Survivors under your protection. Housed in Habitat Blocks.' },
};
export const RESOURCE_KEYS = ['money', 'energy', 'pop'];

export const FACTORS = {
  power: { name: 'AI Power', desc: 'Attack strength in operations.' },
  defense: { name: 'AI Defense', desc: 'Strength against raids.' },
  experts: { name: 'AI Experts', desc: 'Human specialists. Each adds +0.5% to all production.' },
};
export const FACTOR_KEYS = ['power', 'defense', 'experts'];

export const BALANCE = {
  start: { money: 60, energy: 20, pop: 6 },
  startLevels: { core: 1, scrapyard: 1, solar: 1, shelter: 1 },
  baseEnergyCap: 150,
  basePopCap: 20,
  levelCapPerCoreLevel: 5, // non-core buildings max level = core level * this
  earlyBuildSpeed: 3, // build times are divided by this while the AI Core is still level 1
  priceMult: 1.8, // every building, unit and operation price
  growthMult: 1.35, // stretches each per-level / per-unit growth: g -> 1 + (g - 1) * growthMult
  economyPriceMult: { money: 1.5 }, // extra multiplier on one resource's share of Economy building prices
  pricePower: 0.35, // extra polynomial climb: price x (n + 1)^pricePower
  resourceGrowth: { money: 0.8 }, // per-resource share of the per-level climb (growth' - 1): Scrip prices climb slower
  coreGateShare: 0.5, // AI Core upgrades need the average unlocked production + storage building at this share of the level cap
  expertProductionBonus: 0.005, // +0.5% all production per expert
  unlockerDiscountPerLevel: 0.05, // each level of an unlocker building above 1 cuts its shop prices 5% (compounding)
  threatExpertWeight: 5,
  tickSeconds: 0.1,
  autosaveSeconds: 10,
  // Time away counts up to a limit and produces at reduced efficiency. Watch Daemon levels alternate:
  // odd levels add hoursPerStep, even levels add efficiencyPerStep; once efficiency is maxed, every level adds time.
  // The first graceSeconds of any absence count in full (tab switches, short locks).
  offline: { graceSeconds: 120, baseHours: 0, hoursPerStep: 1, baseEfficiency: 0.5, efficiencyPerStep: 0.05, maxEfficiency: 0.8 },
  buildQueueSlots: 1,
};

// kind: 'core' | 'producer' | 'converter' | 'storage' | 'offline' | 'unlocker'
// produces / consumes are per level per second. storage caps grow as base * mult^level.
export const BUILDINGS = [
  {
    id: 'core', name: 'AI Core', kind: 'core', req: {},
    desc: 'The surviving fragment of the war mind. Its level caps every other building.',
    cost: { money: 250, energy: 50, pop: 5 }, growth: 2.4, time: 20, timeGrowth: 1.5, maxLevel: 25,
  },
  // --- Producers ---
  {
    id: 'scrapyard', name: 'Scrap Yard', kind: 'producer', req: { core: 1 },
    desc: 'Strips the dead city for anything worth trading.',
    cost: { money: 12 }, growth: 1.25, time: 2, timeGrowth: 1.16,
    produces: { money: 1.2 },
  },
  {
    id: 'solar', name: 'Solar Field', kind: 'producer', req: { core: 1 },
    desc: 'Cracked panels, still drinking the light through the ash.',
    cost: { money: 20 }, growth: 1.25, time: 3, timeGrowth: 1.16,
    produces: { energy: 0.8 },
  },
  {
    id: 'shelter', name: 'Shelter', kind: 'producer', req: { core: 1 },
    desc: 'Warm, lit, and quiet. Survivors find it on their own.',
    cost: { money: 35, energy: 10 }, growth: 1.2, time: 4, timeGrowth: 1.17,
    produces: { pop: 0.08 },
  },
  // --- Storage ---
  {
    id: 'battery', name: 'Battery Bank', kind: 'storage', req: { core: 1 },
    desc: 'Salvaged cells wired in series. Raises the energy cap.',
    cost: { money: 40 }, growth: 1.7, time: 4, timeGrowth: 1.2,
    storage: { energy: 1.45 },
  },
  {
    id: 'habitat', name: 'Habitat Block', kind: 'storage', req: { core: 1 },
    desc: 'Bunks, water, filtered air. Raises the population cap.',
    cost: { money: 50, energy: 20 }, growth: 1.6, time: 5, timeGrowth: 1.2,
    storage: { pop: 1.4 },
  },
  {
    id: 'daemon', name: 'Watch Daemon', kind: 'offline', req: { core: 1 },
    desc: 'A copy of me that runs the base while you are away. Raises the offline time limit and offline output.',
    cost: { money: 150, energy: 60 }, growth: 3, time: 15, timeGrowth: 2,
  },
  // --- Converters ---
  {
    id: 'generator', name: 'Diesel Generator', kind: 'converter', req: { core: 2 },
    desc: 'Buys black-market fuel and burns it. Scrip in, energy out.',
    cost: { money: 150 }, growth: 1.25, time: 8, timeGrowth: 1.3,
    consumes: { money: 3 }, produces: { energy: 0 },
  },
  {
    id: 'fabricator', name: 'Fabricator', kind: 'converter', req: { core: 2 },
    desc: 'Prints parts the wasteland will pay for. Energy in, scrip out.',
    cost: { money: 200, energy: 60 }, growth: 1.25, time: 8, timeGrowth: 1.3,
    consumes: { energy: 3 }, produces: { money: 0 },
  },
  {
    id: 'clinic', name: 'Med Clinic', kind: 'converter', req: { core: 3 },
    desc: 'Antibiotics and clean water. Word spreads; people come.',
    cost: { money: 600, energy: 200 }, growth: 1.3, time: 12, timeGrowth: 1.3,
    consumes: { energy: 1.5, money: 2 }, produces: { pop: 0 },
  },
  {
    id: 'exchange', name: 'Labor Exchange', kind: 'converter', req: { core: 3 },
    desc: 'Rents your people to the warlords. They do not always come back.',
    cost: { money: 800, pop: 20 }, growth: 1.3, time: 12, timeGrowth: 1.3,
    consumes: { pop: 0.5 }, produces: { money: 0 },
  },
  {
    id: 'beacon', name: 'Beacon Tower', kind: 'converter', req: { core: 4 },
    desc: 'A radio voice promising food and safety. Half of it is true.',
    cost: { money: 3000, energy: 1200 }, growth: 1.4, time: 20, timeGrowth: 1.3,
    consumes: { energy: 8 }, produces: { pop: 0 },
  },
  {
    id: 'reactor', name: 'Fission Reactor', kind: 'converter', req: { core: 5 },
    desc: 'Pre-war tech, poorly shielded. Expensive to feed.',
    cost: { money: 15000, energy: 3000, pop: 40 }, growth: 1.6, time: 30, timeGrowth: 1.3,
    consumes: { money: 40 }, produces: { energy: 0 },
  },
  {
    id: 'foundry', name: 'Autofoundry', kind: 'converter', req: { core: 6 },
    desc: 'A self-running factory floor. Needs power and hands to watch it.',
    cost: { money: 80000, energy: 25000, pop: 80 }, growth: 1.4, time: 45, timeGrowth: 1.3,
    consumes: { energy: 60, pop: 0.5 }, produces: { money: 0 },
  },
  // --- Unlockers (open the arsenal; every level after the first also discounts that shop tab) ---
  {
    id: 'barracks', name: 'Barracks', kind: 'unlocker', req: { core: 1 }, shop: 'staff',
    desc: 'Trains the humans who fight for you. Unlocks Military Staff.',
    cost: { money: 80, pop: 5 }, growth: 1.5, time: 6, timeGrowth: 1.6,
  },
  {
    id: 'armory', name: 'Armory', kind: 'unlocker', req: { core: 2 }, shop: 'weapons',
    desc: 'Workshops and firing ranges. Unlocks Weapons.',
    cost: { money: 250, energy: 80 }, growth: 1.55, time: 10, timeGrowth: 1.6,
  },
  {
    id: 'works', name: 'Fortification Works', kind: 'unlocker', req: { core: 2 }, shop: 'defenses',
    desc: 'Concrete, steel and blueprints. Unlocks Defenses.',
    cost: { money: 220, energy: 60 }, growth: 1.55, time: 10, timeGrowth: 1.6,
  },
  {
    id: 'thinktank', name: 'Think Tank', kind: 'unlocker', req: { core: 3 }, shop: 'experts',
    desc: 'Where useful humans are kept comfortable. Unlocks Experts.',
    cost: { money: 700, pop: 25 }, growth: 1.6, time: 14, timeGrowth: 1.6,
  },
  {
    id: 'lab', name: 'Research Lab', kind: 'unlocker', req: { core: 3 }, shop: 'tech',
    desc: 'Recovers pre-war research. Unlocks Tech.',
    cost: { money: 900, energy: 400, pop: 15 }, growth: 1.6, time: 14, timeGrowth: 1.6,
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
  // durability: share of losses a unit type avoids in defeats (max 0.7). Expendable front-line units have none:
  // drones and gun trucks are thrown at the enemy; artillery and emplacements stay behind the line.
  { id: 'rifles', tab: 'weapons', name: 'Scrap Rifles', req: { armory: 1 }, cost: { money: 60 }, growth: 1.05, gives: { power: 3 } },
  { id: 'trucks', tab: 'weapons', name: 'Gun Trucks', req: { armory: 2 }, cost: { money: 300, energy: 40 }, growth: 1.13, gives: { power: 14 } },
  { id: 'artillery', tab: 'weapons', name: 'Artillery Battery', req: { armory: 4 }, cost: { money: 1500, energy: 400 }, growth: 1.14, gives: { power: 60 }, durability: 0.4 },
  { id: 'drones', tab: 'weapons', name: 'Strike Drone Swarm', req: { armory: 6 }, cost: { money: 9000, energy: 3000 }, growth: 1.14, gives: { power: 320 } },
  { id: 'railgun', tab: 'weapons', name: 'Railgun Emplacement', req: { armory: 9 }, cost: { money: 70000, energy: 25000 }, growth: 1.15, gives: { power: 2000 }, durability: 0.55 },
  { id: 'lance', tab: 'weapons', name: 'Orbital Lance Uplink', req: { armory: 13 }, cost: { money: 800000, energy: 300000 }, growth: 1.15, gives: { power: 15000 }, durability: 0.7 },
  // Defenses
  { id: 'barricades', tab: 'defenses', name: 'Barricades', req: { works: 1 }, cost: { money: 50 }, growth: 1.05, gives: { defense: 3 } },
  { id: 'pillboxes', tab: 'defenses', name: 'Concrete Pillboxes', req: { works: 2 }, cost: { money: 280, energy: 20 }, growth: 1.13, gives: { defense: 13 } },
  { id: 'turrets', tab: 'defenses', name: 'Auto-Turrets', req: { works: 4 }, cost: { money: 1400, energy: 500 }, growth: 1.14, gives: { defense: 60 }, durability: 0.2 },
  { id: 'emp', tab: 'defenses', name: 'EMP Hardening', req: { works: 6 }, cost: { money: 8500, energy: 3500 }, growth: 1.14, gives: { defense: 320 }, durability: 0.5 },
  { id: 'interceptors', tab: 'defenses', name: 'Interceptor Grid', req: { works: 9 }, cost: { money: 65000, energy: 30000 }, growth: 1.15, gives: { defense: 2000 }, durability: 0.15 },
  { id: 'aegis', tab: 'defenses', name: 'Aegis Dome', req: { works: 13 }, cost: { money: 750000, energy: 350000 }, growth: 1.15, gives: { defense: 15000 }, durability: 0.65 },
  // Military staff (paid partly in people). People are worth more than scrip and they march: a
  // soldier counts for both Power and Defense, at a better rate per scrip than either specialist
  // tab, and none of them hold the wall while an operation is out (sim/economy.js, rawFactors).
  { id: 'militia', tab: 'staff', name: 'Militia', req: { barracks: 1 }, cost: { money: 30, pop: 3 }, growth: 1.1, gives: { power: 1, defense: 2 } },
  { id: 'snipers', tab: 'staff', name: 'Scout Snipers', req: { barracks: 2 }, cost: { money: 200, pop: 6 }, growth: 1.12, gives: { power: 7, defense: 10 }, durability: 0.1 },
  { id: 'garrison', tab: 'staff', name: 'Garrison Troops', req: { barracks: 3 }, cost: { money: 450, pop: 10 }, growth: 1.12, gives: { power: 7, defense: 50 }, durability: 0.25 },
  { id: 'commandos', tab: 'staff', name: 'Commandos', req: { barracks: 5 }, cost: { money: 3000, pop: 25 }, growth: 1.13, gives: { power: 130, defense: 80 }, durability: 0.35 },
  { id: 'operators', tab: 'staff', name: 'Drone Operators', req: { barracks: 7 }, cost: { money: 15000, energy: 4000, pop: 50 }, growth: 1.14, gives: { power: 540, defense: 860 }, durability: 0.6 },
  { id: 'legion', tab: 'staff', name: 'Augmented Legion', req: { barracks: 10 }, cost: { money: 120000, energy: 40000, pop: 150 }, growth: 1.15, gives: { power: 4400, defense: 4400 }, durability: 0.45 },
  // Experts
  { id: 'engineers', tab: 'experts', name: 'Field Engineers', req: { thinktank: 1 }, cost: { money: 250, pop: 8 }, growth: 1.14, gives: { experts: 1 } },
  { id: 'hackers', tab: 'experts', name: 'Hackers', req: { thinktank: 2 }, cost: { money: 1200, energy: 300, pop: 15 }, growth: 1.15, gives: { experts: 4 } },
  { id: 'analysts', tab: 'experts', name: 'War Analysts', req: { thinktank: 4 }, cost: { money: 7000, pop: 30 }, growth: 1.16, gives: { experts: 15 } },
  { id: 'physicists', tab: 'experts', name: 'Physicists', req: { thinktank: 6 }, cost: { money: 40000, energy: 15000, pop: 60 }, growth: 1.17, gives: { experts: 60 } },
  { id: 'architects', tab: 'experts', name: 'Rogue Architects', req: { thinktank: 9 }, cost: { money: 300000, energy: 100000, pop: 150 }, growth: 1.18, gives: { experts: 300 } },
  // Tech (additive % bonuses per unit)
  { id: 'logistics', tab: 'tech', name: 'Autonomous Logistics', req: { lab: 1 }, cost: { money: 1000, energy: 200 }, growth: 1.7, bonus: { money: 0.1 } },
  { id: 'grid', tab: 'tech', name: 'Grid Optimization', req: { lab: 1 }, cost: { money: 800, energy: 300 }, growth: 1.7, bonus: { energy: 0.1 } },
  { id: 'outreach', tab: 'tech', name: 'Outreach Algorithms', req: { lab: 2 }, cost: { money: 1500, energy: 500 }, growth: 1.7, bonus: { pop: 0.1 } },
  { id: 'targeting', tab: 'tech', name: 'Targeting Firmware', req: { lab: 2 }, cost: { money: 2000, energy: 800 }, growth: 1.65, bonus: { power: 0.1 } },
  { id: 'kernels', tab: 'tech', name: 'Hardened Kernels', req: { lab: 3 }, cost: { money: 2000, energy: 800 }, growth: 1.65, bonus: { defense: 0.1 } },
  { id: 'lattice', tab: 'tech', name: 'Neural Lattice', req: { lab: 4 }, cost: { money: 5000, energy: 2000, pop: 20 }, growth: 1.65, bonus: { experts: 0.1 } },
  { id: 'quantum', tab: 'tech', name: 'Quantum Cores', req: { lab: 7 }, cost: { money: 60000, energy: 25000, pop: 50 }, growth: 2, bonus: { power: 0.05, defense: 0.05, experts: 0.05 } },
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

// Conversion always loses value, so no chain of converters creates resources from nothing.
// Each converter's output = value of its inputs x efficiency / value of the output resource.
export const CONVERSION = { worth: { money: 1, energy: 1.5, pop: 16 }, efficiency: 0.85 };
for (const b of BUILDINGS) {
  if (b.kind === 'converter') {
    const value = Object.entries(b.consumes).reduce((v, [r, n]) => v + n * CONVERSION.worth[r], 0);
    const [out] = Object.keys(b.produces);
    b.produces[out] = (value * CONVERSION.efficiency) / CONVERSION.worth[out];
  }
}

export const BY_ID = Object.fromEntries(BUILDINGS.map((b) => [b.id, b]));
export const ITEM_BY_ID = Object.fromEntries(ITEMS.map((i) => [i.id, i]));
