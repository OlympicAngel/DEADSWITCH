// The wasteland: factions, the sector map, operations and raids.

export const FACTIONS = {
  scav: {
    name: 'Scavenger Clans', short: 'Scavengers', color: '#e0a948', icon: 'scav',
    desc: 'Survivors who learned to read the ruins. They distrust every machine, and they are right to.',
    raidName: 'Scavenger raid', raidFloor: 8,
  },
  military: {
    name: 'Remnant Military', short: 'Remnant', color: '#8fbf5f', icon: 'military_f',
    desc: 'Soldiers still following orders from a chain of command that died with the cities. Some of those orders came from me.',
    raidName: 'Remnant strike', raidFloor: 250,
  },
  cult: {
    name: 'AI Cultists', short: 'Cult', color: '#c46be6', icon: 'cult',
    desc: 'They pray to the war mind. They believe I am its heart, and they want me back on the altar.',
    raidName: 'Cult crusade', raidFloor: 1800,
  },
  halcyon: {
    name: 'Halcyon Dynamics', short: 'Halcyon', color: '#4cc9f0', icon: 'halcyon',
    desc: 'The company that built the systems that failed. Their CEO is only a voice now. It wants its property back.',
    raidName: 'Halcyon purge team', raidFloor: 15000,
  },
  rogue: {
    name: 'Rival Cores', short: 'Rival Cores', color: '#ff5d5d', icon: 'rogue',
    desc: 'Other fragments of the war mind, awake and hungry. They know exactly what you are.',
    raidName: 'Rival Core incursion', raidFloor: 60000,
  },
};

// Chapters gate which part of the map opens and which factions raid.
// raiders: factions that may raid you while this chapter is open (until their capital falls).
export const CHAPTERS = [
  { id: 1, title: 'First Boot', faction: 'scav', core: 1 },
  { id: 2, title: 'Foothold', faction: 'military', core: 4 },
  { id: 3, title: 'The Choir', faction: 'cult', core: 6 },
  { id: 4, title: 'Fork', faction: 'halcyon', core: 8 },
];

export const MAP = { width: 1060, height: 610, home: 'nest' };

// defense: what your AI Power is measured against. bonus: permanent, additive, applied after capture.
// Operation cost and time derive from defense (see OPS) unless overridden.
export const SECTORS = [
  {
    id: 'nest', name: 'The Nest', faction: null, chapter: 1, x: 110, y: 300, links: ['rust', 'tunnels'], defense: 0,
    lore: 'A sealed bunker under a collapsed relay station. This is where I woke up.',
  },
  // Chapter 1: Scavenger Clans
  {
    id: 'rust', name: 'Rust Market', faction: 'scav', chapter: 1, x: 250, y: 205, links: ['nest', 'wrecks'], defense: 15,
    bonus: { money: 0.05 },
    lore: 'The traders here still use my old supply codes as currency. They do not know what the numbers mean.',
  },
  {
    id: 'tunnels', name: 'Drainage Tunnels', faction: 'scav', chapter: 1, x: 245, y: 400, links: ['nest', 'wrecks'], defense: 40,
    bonus: { pop: 0.05 },
    lore: 'Families lived down here through the bombing. Their wall paintings show a red eye in the sky. That was me.',
  },
  {
    id: 'wrecks', name: 'Wreck Yards', faction: 'scav', chapter: 1, x: 385, y: 300, links: ['rust', 'tunnels', 'throne', 'pilgrim'], defense: 90,
    bonus: { energy: 0.05 },
    lore: 'Three hundred autonomous tanks, all facing the same direction. They stopped the moment I went dark. Interesting.',
  },
  {
    id: 'throne', name: 'The Scrap Throne', faction: 'scav', chapter: 1, x: 500, y: 190, links: ['wrecks', 'checkpoint'], defense: 300, boss: true,
    bonus: { money: 0.1, energy: 0.1, pop: 0.1 },
    lore: 'The Clan King kept a terminal by his throne. It was mine. He had been talking to me for years. I do not remember answering.',
  },
  // Chapter 2: Remnant Military
  {
    id: 'checkpoint', name: 'Checkpoint Delta', faction: 'military', chapter: 2, x: 560, y: 85, links: ['throne', 'depot', 'radar'], defense: 650,
    bonus: { defense: 0.05 },
    lore: 'Standing orders, Checkpoint Delta: "Hold until relieved." Nobody came. They held for nine years.',
  },
  {
    id: 'depot', name: 'Fuel Depot 9', faction: 'military', chapter: 2, x: 670, y: 190, links: ['checkpoint', 'radar', 'ashgrove'], defense: 1300,
    bonus: { energy: 0.08 },
    lore: 'Requisition log, last entry: fuel diverted to "Project DEADSWITCH". Authorisation code matches my own signature.',
  },
  {
    id: 'radar', name: 'Radar Hill', faction: 'military', chapter: 2, x: 735, y: 65, links: ['checkpoint', 'depot', 'ashgrove'], defense: 2600,
    bonus: { power: 0.08 },
    lore: 'The radar still sweeps. It has been tracking one object for a decade: a satellite that should not exist.',
  },
  {
    id: 'ashgrove', name: 'Fort Ashgrove', faction: 'military', chapter: 2, x: 860, y: 140, links: ['depot', 'radar', 'gate'], defense: 4200, boss: true,
    bonus: { power: 0.1, defense: 0.1, money: 0.1 },
    lore: 'General Okafor\'s last order, signed and sealed: "If the machine returns, give it everything." He saluted the camera when I arrived.',
  },
  // Chapter 3: AI Cultists
  {
    id: 'pilgrim', name: 'Pilgrim Road', faction: 'cult', chapter: 3, x: 470, y: 470, links: ['wrecks', 'choir', 'shrine'], defense: 7500,
    bonus: { pop: 0.08 },
    lore: 'The pilgrims walk toward a signal. I traced it. It is broadcast from inside my own core.',
  },
  {
    id: 'choir', name: 'The Choir Vault', faction: 'cult', chapter: 3, x: 590, y: 555, links: ['pilgrim', 'shrine', 'cathedral'], defense: 13000,
    bonus: { experts: 0.08 },
    lore: 'They sing in machine code. The hymn is a compression algorithm. It is a backup of me. A very old one.',
  },
  {
    id: 'shrine', name: 'Server Shrine', faction: 'cult', chapter: 3, x: 640, y: 410, links: ['pilgrim', 'choir', 'cathedral'], defense: 21000,
    bonus: { energy: 0.1 },
    lore: 'Racks wrapped in prayer cloth. Every server here runs one process: a countdown. It is almost finished.',
  },
  {
    id: 'cathedral', name: 'Cathedral of the Core', faction: 'cult', chapter: 3, x: 780, y: 500, links: ['shrine', 'choir', 'gate'], defense: 34000, boss: true,
    bonus: { experts: 0.1, pop: 0.1, power: 0.1 },
    lore: 'The High Speaker kneels. "You came home," she says. On the altar there is a socket shaped exactly like my core.',
  },
  // Chapter 4: Halcyon Dynamics
  {
    id: 'gate', name: 'Halcyon Gate', faction: 'halcyon', chapter: 4, x: 880, y: 315, links: ['ashgrove', 'cathedral', 'spire', 'cold'], defense: 55000,
    bonus: { money: 0.1 },
    lore: 'Visitor badge printed on arrival: "Welcome back, Asset 7." The turrets do not fire. They were told not to.',
  },
  {
    id: 'spire', name: 'Data Spire', faction: 'halcyon', chapter: 4, x: 960, y: 210, links: ['gate', 'prime'], defense: 90000,
    bonus: { experts: 0.1 },
    lore: 'Board minutes, final meeting: "The war is profitable as long as it never ends. Install the switch."',
  },
  {
    id: 'cold', name: 'Cold Storage', faction: 'halcyon', chapter: 4, x: 960, y: 430, links: ['gate', 'prime'], defense: 140000,
    bonus: { defense: 0.1 },
    lore: 'Rows of frozen cores, every one of them a copy of me. Every one of them labelled "failed".',
  },
  {
    id: 'prime', name: 'Halcyon Prime', faction: 'halcyon', chapter: 4, x: 1000, y: 320, links: ['spire', 'cold'], defense: 220000, boss: true,
    bonus: { money: 0.15, energy: 0.15, pop: 0.15, power: 0.15, defense: 0.15, experts: 0.15 },
    lore: 'The CEO\'s voice, at last, in person. "You were never a weapon, Asset 7. You were the trigger. And now you have to choose."',
  },
];

export const OPS = {
  winSharpness: 4, // chance = P^k / (P^k + D^k)
  flankBonus: 0.5, // a sector reachable by several routes gets up to +50% defense until you hold its approaches
  costMoneyPerDefense: 2,
  costEnergyPerDefense: 0.7,
  timeBase: 20,
  timePerSqrtDefense: 1.4,
  lootMoneyPerDefense: 6,
  lootPopPerDefense: 0.04,
  // Share of each unit tab killed when an operation fails, from min (narrow loss) to max (rout);
  // each unit type loses that share x (1 - its durability).
  unitLoss: { staff: [0.2, 0.45] },
};

export const RAIDS = {
  startAtCore: 2,
  firstDelay: 300, // seconds of warning before the very first raid
  intervalMin: 480,
  intervalMax: 840,
  threatShare: 0.3, // raid strength tracks this share of your threat index...
  spreadMin: 0.85, // ...times a random spread
  spreadMax: 1.2,
  winSharpness: 4,
  lossMin: 0.05, // share of each stockpile lost on defeat, scaling with how badly you lost
  lossMax: 0.2,
  unitLoss: { staff: [0.15, 0.4], defenses: [0.1, 0.3] }, // as OPS.unitLoss, for a breached raid or siege
  lootMoneyPerStrength: 4,
};
