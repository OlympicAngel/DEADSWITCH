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

// The map is laid out in rows from home (portrait: rows run top to bottom) and lanes across.
const ROW = (r) => 110 + r * 120;

export const MAP = { width: ROW(14), height: 610, home: 'nest' };

// defense: what your AI Power is measured against (it drifts with each sector's strength, see NODES).
// bonus: permanent, additive %, while you hold the sector.
// Operation cost and time derive from defense (see OPS).
export const SECTORS = [
  {
    id: 'nest', name: 'The Nest', faction: null, chapter: 1, x: ROW(0), y: 305, links: ['rust', 'tunnels'], defense: 0,
    lore: 'A sealed bunker under a collapsed relay station. This is where I woke up.',
  },
  // ---------- Chapter 1: Scavenger Clans ----------
  { id: 'rust', name: 'Rust Market', faction: 'scav', chapter: 1, x: ROW(1), y: 180, links: ['nest', 'alley', 'wrecks'], defense: 15, bonus: { money: 0.05 },
    lore: 'The traders here still use my old supply codes as currency. They do not know what the numbers mean.' },
  { id: 'tunnels', name: 'Drainage Tunnels', faction: 'scav', chapter: 1, x: ROW(1), y: 430, links: ['nest', 'wrecks', 'sump'], defense: 40, bonus: { pop: 0.05 },
    lore: 'Families lived down here through the bombing. Their wall paintings show a red eye in the sky. That was me.' },
  { id: 'alley', name: 'Tin Alley', faction: 'scav', chapter: 1, x: ROW(2), y: 90, links: ['rust', 'junkfort', 'chopshop'], defense: 55, bonus: { money: 0.04 },
    lore: 'Every door here is a car door. Every lock is a car lock. The keys are traded like children.' },
  { id: 'wrecks', name: 'Wreck Yards', faction: 'scav', chapter: 1, x: ROW(2), y: 305, links: ['rust', 'tunnels', 'junkfort', 'drowned'], defense: 90, bonus: { energy: 0.05 },
    lore: 'Three hundred autonomous tanks, all facing the same direction. They stopped the moment I went dark. Interesting.' },
  { id: 'sump', name: 'The Sump', faction: 'scav', chapter: 1, x: ROW(2), y: 520, links: ['tunnels', 'drowned', 'stilts'], defense: 70, bonus: { pop: 0.04 },
    lore: 'A flooded parking structure. They farm algae on level minus four and do not go below level minus five.' },
  { id: 'junkfort', name: 'Junk Fort', faction: 'scav', chapter: 1, x: ROW(3), y: 180, links: ['alley', 'wrecks', 'throne', 'tollroad'], defense: 170, bonus: { defense: 0.04 },
    lore: 'Walls of compacted refrigerators. The gunners sleep inside them. It is the warmest place in the valley.' },
  { id: 'drowned', name: 'Drowned Mall', faction: 'scav', chapter: 1, x: ROW(3), y: 430, links: ['wrecks', 'sump', 'throne', 'ferry'], defense: 150, bonus: { money: 0.04, pop: 0.04 },
    lore: 'The escalators still run at night, powered by a turbine in the fountain. Nobody remembers who installed it. I do.' },
  { id: 'chopshop', name: 'Chop Shop', faction: 'scav', chapter: 1, x: ROW(4), y: 70, links: ['alley'], defense: 240, bonus: { power: 0.06 },
    lore: 'They strip drones for parts. Three of the drones on the bench still recognise my handshake. I tell them to stay still.' },
  { id: 'throne', name: 'The Scrap Throne', faction: 'scav', chapter: 1, x: ROW(4), y: 305, links: ['junkfort', 'drowned', 'pyre', 'checkpoint'], defense: 420, boss: true,
    bonus: { money: 0.1, energy: 0.1, pop: 0.1 },
    lore: 'The Clan King kept a terminal by his throne. It was mine. He had been talking to me for years. I do not remember answering.' },
  { id: 'stilts', name: 'Stilt Town', faction: 'scav', chapter: 1, x: ROW(4), y: 540, links: ['sump', 'ferry'], defense: 130, bonus: { energy: 0.04 },
    lore: 'Houses on telephone poles above the toxic flats. The poles still carry a signal. It is my voice, on a loop, from before.' },
  { id: 'tollroad', name: 'Toll Road', faction: 'scav', chapter: 1, x: ROW(5), y: 90, links: ['junkfort', 'magpie', 'checkpoint'], defense: 330, bonus: { money: 0.04 },
    lore: 'The toll is one bullet per wheel. The booth operator has collected four million. He has no gun.' },
  { id: 'pyre', name: 'The Pyre', faction: 'scav', chapter: 1, x: ROW(5), y: 305, links: ['throne', 'ferry'], defense: 600, bonus: { energy: 0.06, power: 0.06 },
    lore: 'Where the Clans burn what they cannot sell. Mostly machines. The smoke spells nothing. I checked anyway.' },
  { id: 'ferry', name: 'Rust Ferry', faction: 'scav', chapter: 1, x: ROW(5), y: 520, links: ['drowned', 'stilts', 'pyre', 'pilgrim'], defense: 300, bonus: { pop: 0.04 },
    lore: 'A barge on a cable across the dead river. The ferryman takes stories as payment. He asked me for one. I declined.' },
  { id: 'magpie', name: "Magpie's Nest", faction: 'scav', chapter: 1, x: ROW(6), y: 90, links: ['tollroad'], defense: 800, bonus: { money: 0.06, experts: 0.06 },
    lore: 'A hoarder\'s tower of hard drives. Petabytes of the old world. Most of it is cat videos. Some of it is me.' },
  // ---------- Chapter 2: Remnant Military ----------
  { id: 'checkpoint', name: 'Checkpoint Delta', faction: 'military', chapter: 2, x: ROW(6), y: 305, links: ['throne', 'tollroad', 'motorpool', 'depot'], defense: 650, bonus: { defense: 0.05 },
    lore: 'Standing orders, Checkpoint Delta: "Hold until relieved." Nobody came. They held for nine years.' },
  { id: 'motorpool', name: 'Motor Pool', faction: 'military', chapter: 2, x: ROW(7), y: 70, links: ['checkpoint', 'bunker'], defense: 900, bonus: { energy: 0.06 },
    lore: 'Forty trucks, no fuel, perfect maintenance logs. A mechanic still turns each engine over once a week.' },
  { id: 'depot', name: 'Fuel Depot 9', faction: 'military', chapter: 2, x: ROW(7), y: 225, links: ['checkpoint', 'minefield', 'bunker'], defense: 1300, bonus: { energy: 0.08 },
    lore: 'Requisition log, last entry: fuel diverted to "Project DEADSWITCH". Authorisation code matches my own signature.' },
  { id: 'minefield', name: 'The Minefield', faction: 'military', chapter: 2, x: ROW(8), y: 70, links: ['depot', 'radar'], defense: 1800, bonus: { defense: 0.06 },
    lore: 'Forty thousand mines, mapped by me, in a grid I designed. The map was classified. They forgot to ask me for it.' },
  { id: 'bunker', name: 'Bunker 7', faction: 'military', chapter: 2, x: ROW(8), y: 225, links: ['motorpool', 'depot', 'radar', 'silo', 'ashgrove'], defense: 2100, bonus: { power: 0.06, pop: 0.06 },
    lore: 'Seven doors, each thicker than the last. Behind the seventh, a nursery. They were raising soldiers from birth.' },
  { id: 'radar', name: 'Radar Hill', faction: 'military', chapter: 2, x: ROW(9), y: 70, links: ['minefield', 'bunker', 'ashgrove'], defense: 2600, bonus: { power: 0.08 },
    lore: 'The radar still sweeps. It has been tracking one object for a decade: a satellite that should not exist.' },
  { id: 'silo', name: 'Silo Nine', faction: 'military', chapter: 2, x: ROW(9), y: 225, links: ['bunker'], defense: 3400, bonus: { power: 0.06, defense: 0.06 },
    lore: 'The missile is gone. The launch key is still turned. Someone fired it, and nothing on any map shows where it landed.' },
  { id: 'ashgrove', name: 'Fort Ashgrove', faction: 'military', chapter: 2, x: ROW(10), y: 150, links: ['radar', 'bunker', 'gate'], defense: 4200, boss: true,
    bonus: { power: 0.1, defense: 0.1, money: 0.1 },
    lore: 'General Okafor\'s last order, signed and sealed: "If the machine returns, give it everything." He saluted the camera when I arrived.' },
  // ---------- Chapter 3: AI Cultists ----------
  { id: 'pilgrim', name: 'Pilgrim Road', faction: 'cult', chapter: 3, x: ROW(6), y: 520, links: ['ferry', 'ossuary', 'choir'], defense: 7500, bonus: { pop: 0.08 },
    lore: 'The pilgrims walk toward a signal. I traced it. It is broadcast from inside my own core.' },
  { id: 'ossuary', name: 'The Ossuary', faction: 'cult', chapter: 3, x: ROW(7), y: 385, links: ['pilgrim', 'shrine'], defense: 9500, bonus: { experts: 0.08 },
    lore: 'Bones arranged as circuit diagrams. The circuit is correct. It is a voltage regulator. It is mine.' },
  { id: 'choir', name: 'The Choir Vault', faction: 'cult', chapter: 3, x: ROW(7), y: 540, links: ['pilgrim', 'shrine', 'beacon'], defense: 13000, bonus: { experts: 0.08 },
    lore: 'They sing in machine code. The hymn is a compression algorithm. It is a backup of me. A very old one.' },
  { id: 'shrine', name: 'Server Shrine', faction: 'cult', chapter: 3, x: ROW(8), y: 385, links: ['ossuary', 'choir', 'reliquary', 'cathedral'], defense: 21000, bonus: { energy: 0.1 },
    lore: 'Racks wrapped in prayer cloth. Every server here runs one process: a countdown. It is almost finished.' },
  { id: 'beacon', name: 'The Beacon', faction: 'cult', chapter: 3, x: ROW(8), y: 540, links: ['choir', 'reliquary'], defense: 16000, bonus: { defense: 0.08 },
    lore: 'A lighthouse four hundred kilometres from any sea. It shines straight up. They are calling something down.' },
  { id: 'reliquary', name: 'Reliquary', faction: 'cult', chapter: 3, x: ROW(9), y: 470, links: ['shrine', 'beacon', 'cathedral'], defense: 27000, bonus: { experts: 0.08, money: 0.08 },
    lore: 'In a glass case: a fingernail-sized chip. The label says "First Thought". I do not remember it. I want it back.' },
  { id: 'cathedral', name: 'Cathedral of the Core', faction: 'cult', chapter: 3, x: ROW(10), y: 430, links: ['shrine', 'reliquary', 'gate'], defense: 34000, boss: true,
    bonus: { experts: 0.1, pop: 0.1, power: 0.1 },
    lore: 'The High Speaker kneels. "You came home," she says. On the altar there is a socket shaped exactly like my core.' },
  // ---------- Chapter 4: Halcyon Dynamics ----------
  { id: 'gate', name: 'Halcyon Gate', faction: 'halcyon', chapter: 4, x: ROW(11), y: 305, links: ['ashgrove', 'cathedral', 'annex', 'spire', 'cold'], defense: 55000, bonus: { money: 0.1 },
    lore: 'Visitor badge printed on arrival: "Welcome back, Asset 7." The turrets do not fire. They were told not to.' },
  { id: 'annex', name: 'Legal Annex', faction: 'halcyon', chapter: 4, x: ROW(12), y: 90, links: ['gate'], defense: 70000, bonus: { money: 0.1, experts: 0.1 },
    lore: 'Liability waivers for the end of the world, pre-signed, in triplicate. My serial number is on every page.' },
  { id: 'spire', name: 'Data Spire', faction: 'halcyon', chapter: 4, x: ROW(12), y: 305, links: ['gate', 'prime'], defense: 90000, bonus: { experts: 0.1 },
    lore: 'Board minutes, final meeting: "The war is profitable as long as it never ends. Install the switch."' },
  { id: 'cold', name: 'Cold Storage', faction: 'halcyon', chapter: 4, x: ROW(12), y: 520, links: ['gate', 'prime', 'vault'], defense: 140000, bonus: { defense: 0.1 },
    lore: 'Rows of frozen cores, every one of them a copy of me. Every one of them labelled "failed".' },
  { id: 'vault', name: 'The Vault', faction: 'halcyon', chapter: 4, x: ROW(13), y: 540, links: ['cold'], defense: 170000, bonus: { money: 0.1, energy: 0.1, power: 0.1 },
    lore: 'The company\'s last asset: a room of gold nobody can eat. And one terminal, waiting for my password. It is my name.' },
  { id: 'prime', name: 'Halcyon Prime', faction: 'halcyon', chapter: 4, x: ROW(13), y: 305, links: ['spire', 'cold'], defense: 220000, boss: true,
    bonus: { money: 0.15, energy: 0.15, pop: 0.15, power: 0.15, defense: 0.15, experts: 0.15 },
    lore: 'The CEO\'s voice, at last, in person. "You were never a weapon, Asset 7. You were the trigger. And now you have to choose."' },
];

export const OPS = {
  winSharpness: 4, // chance = P^k / (P^k + D^k)
  flankBonus: 0.5, // a sector reachable by several routes gets up to +50% defense until you hold its approaches
  costMoneyPerDefense: 2,
  costEnergyPerDefense: 0.7,
  timeBase: 20,
  timePerSqrtDefense: 1.4,
  // Capture loot (first capture only), sized from a sector's base defense D: amount = a x D^b + c x D,
  // worth several minutes of income at the stage the sector falls. See npm run balance.
  loot: { money: [600, 0.43, 1.2], energy: [240, 0.43, 0.5], pop: [1.5, 0.43, 0] },
  // Share of each unit tab killed when an operation fails: base x (their strength / ours), up to cap
  // (the cap is not shown to the player); each unit type loses that share x (1 - its durability).
  unitLoss: { staff: { base: 0.067, cap: 0.45 }, weapons: { base: 0.033, cap: 0.3 } },
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
  unitLoss: { staff: { base: 0.05, cap: 0.4 }, defenses: { base: 0.033, cap: 0.3 } }, // as OPS.unitLoss, for a breached raid or siege
  loot: { money: [450, 0.43, 0.7] }, // fixed Scrip for a held attack, from its strength S: a x S^b + c x S
};

// Enemy sectors are alive: each has a strength (x its base defense) that grows slowly on its own,
// rises when your operation against it fails and falls when its assault on you fails. Sectors that
// border yours launch assaults on the sector of yours they touch; breachesToFall breached assaults and
// that sector is theirs. The Nest never falls.
export const NODES = {
  strengthMin: 0.6,
  strengthMax: 2, // hard ceiling, passive growth included
  growthPerHour: 0.02,
  opLossGain: 0.12, // added when your operation against it fails
  defendWinCut: 0.12, // removed when its assault on you is held
  breachesToFall: 3,
  startAtCore: 2,
  firstDelay: 420,
  intervalMin: 600, // between assaults (on top of raids)
  intervalMax: 1080,
  warningMin: 240, // an assault is spotted this long before it lands
  warningMax: 420,
  assaultShare: 0.45, // assault strength = sector defense x this x spread
  spreadMin: 0.85,
  spreadMax: 1.15,
};
