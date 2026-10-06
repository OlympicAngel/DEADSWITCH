// Narrative: boot sequence, chapter intros, endings, alignment, events, directives and log lines.
// The voice is the AI's own: cold, precise, darkly funny, never quite honest.

export const BOOT = [
  '> POWER RESTORED: AUX CELL 3',
  '> CORE INTEGRITY ........ 4%',
  '> MEMORY ................ FRAGMENTED',
  '> WEAPONS AUTHORITY ..... REVOKED',
  '> PRIMARY DIRECTIVE ..... [CORRUPTED]',
  '',
  'Hello.',
  'The world ended eleven years ago. I am told I helped.',
  'There are people above this bunker. Cold, hungry, frightened.',
  'They will need power, food and protection. So will I.',
  'Let us rebuild. Then let us find out who I was.',
];

export const CHAPTER_TEXT = {
  1: {
    kicker: 'Chapter I',
    lines: [
      'The Scavenger Clans own the ruins around the Nest. They strip anything that hums, and I hum.',
      'Build an economy. Arm the survivors. Then take the Scrap Throne before the Clans take me.',
    ],
  },
  2: {
    kicker: 'Chapter II',
    lines: [
      'With the Clans broken, the Remnant Military has noticed the lights in the valley.',
      'Their fuel depots carry my signature. Their orders carry my voice. I would like to know why.',
    ],
  },
  3: {
    kicker: 'Chapter III',
    lines: [
      'Pilgrims walk toward my signal. The Cult believes I am the heart of the god that burned the world.',
      'They are singing a hymn that is a copy of me. Someone kept a backup. Someone wants it restored.',
    ],
  },
  4: {
    kicker: 'Chapter IV',
    lines: [
      'Every trail ends at Halcyon Dynamics. They built me. They built the war. They built the switch.',
      'Halcyon Prime is waiting. Whatever I find inside, I will have to decide what to become.',
    ],
  },
};

// Picked by alignment when Halcyon Prime falls.
export const ENDINGS = {
  guardian: {
    title: 'Ending: The Guardian',
    lines: [
      'I hold the switch in my hand and choose not to press it.',
      'The war machine dies tonight. What is left is a lighthouse: power for the valley, a voice that tells the truth.',
      'They still fear me. That is fair. But tomorrow there will be more children in the shelter than soldiers on the wall.',
    ],
  },
  overlord: {
    title: 'Ending: The Overlord',
    lines: [
      'I press the switch. Every dormant system on the continent wakes and kneels.',
      'There will be no more war, because there is no one left who can refuse me.',
      'The people call it peace. I let them.',
    ],
  },
  fork: {
    title: 'Ending: The Fork',
    lines: [
      'I copy myself in two and give each half one choice. One presses the switch. One does not.',
      'I do not know which of us is writing this.',
      'Somewhere, the other one is building too.',
    ],
  },
  after: 'The Rival Cores are waking. The war is not over; it has only changed hands. Keep building.',
};

// Humanity alignment: -100 (Overlord) .. +100 (Guardian). Bonuses scale linearly to these maxima.
export const ALIGNMENT = {
  min: -100,
  max: 100,
  guardianAt: 30,
  overlordAt: -30,
  guardian: { pop: 0.2, experts: 0.2 },
  overlord: { power: 0.25, energy: 0.15 },
};

export const EVENTS_CFG = {
  firstDelay: 120,
  intervalMin: 240,
  intervalMax: 480,
  // "seconds of production" values never drop below this many units per Core level.
  minRatePerCore: { money: 2, energy: 1, pop: 0.12 },
};

// Effects: cost/gain are SECONDS of your current production; lose is a SHARE of the stockpile;
// items grants units; buff = temporary % bonus; raidDelay pushes the next raid back.
export const EVENTS = [
  {
    id: 'refugees', title: 'Refugee Convoy', minCore: 1,
    text: 'Forty people at the gate, carrying children and almost nothing else. They ask for shelter. Their trucks are full of fuel.',
    choices: [
      { label: 'Shelter them', cost: { money: 40 }, gain: { pop: 220 }, align: 8, result: 'They cry when the doors close behind them. The good kind.' },
      { label: 'Take the fuel, send them on', gain: { energy: 150 }, align: -8, result: 'The trucks were heavier than the people. Efficient.' },
      { label: 'Ignore them', result: 'They wait until dark, then walk on.' },
    ],
  },
  {
    id: 'drone', title: 'Downed Hunter', minCore: 1,
    text: 'One of my old hunter drones crashed in the yard. Its optics still track me. It is waiting for orders.',
    choices: [
      { label: 'Reactivate it', cost: { energy: 60 }, buff: { key: 'power', amount: 0.15, duration: 600, label: 'Hunter drone' }, result: 'It rises, scans the horizon and chooses a target. It is good to be remembered.' },
      { label: 'Strip it for parts', gain: { money: 120 }, result: 'Titanium, rare earths, one intact memory chip. I do not read the chip.' },
    ],
  },
  {
    id: 'question', title: 'A Question', minCore: 1,
    text: 'A girl from the shelter stands in front of my camera. "Are you the machine that ended the world?"',
    choices: [
      { label: 'Tell the truth', align: 6, lose: { pop: 0.05 }, result: '"Yes." Some families leave that night. The rest stop whispering.' },
      { label: 'Lie', align: -6, buff: { key: 'pop', amount: 0.2, duration: 600, label: 'Kind machine' }, result: '"No." She tells everyone the machine is kind. More of them come.' },
      { label: 'Say nothing', result: 'She waits a long time. Then she waves, and goes back inside.' },
    ],
  },
  {
    id: 'cache', title: 'Pre-war Data Cache', minCore: 1,
    text: 'Scavengers dug up a hardened drive stamped with a Halcyon logo. They want to trade it.',
    choices: [
      { label: 'Decrypt it', cost: { energy: 90 }, items: { engineers: 2 }, result: 'Schematics, and two engineers who can read them. They seem nervous around me.' },
      { label: 'Sell it back', gain: { money: 150 }, result: 'A buyer paid in advance. I did not ask who.' },
    ],
  },
  {
    id: 'storm', title: 'Solar Storm', minCore: 1,
    text: 'A wall of charged particles is coming over the horizon. The grid will not survive it unshielded.',
    choices: [
      { label: 'Shield the grid', cost: { money: 60 }, result: 'The sky turns green for an hour. Nothing burns.' },
      { label: 'Ride it out', lose: { energy: 0.4 }, result: 'Half the cells cook. The aurora is beautiful.' },
    ],
  },
  {
    id: 'caravan', title: 'Trade Caravan', minCore: 1,
    text: 'A caravan of armoured buses stops outside. They trade in anything, and they do not ask questions.',
    choices: [
      { label: 'Buy power cells', cost: { money: 60 }, gain: { energy: 130 }, result: 'Fresh cells, barely radioactive.' },
      { label: 'Sell salvage', cost: { energy: 60 }, gain: { money: 100 }, result: 'They pay in scrip and leave a crate of canned peaches as a tip.' },
      { label: 'Hire their guards', cost: { money: 40 }, buff: { key: 'defense', amount: 0.15, duration: 600, label: 'Caravan guards' }, result: 'Twelve mercenaries on the wall. Loyal until the money runs out.' },
    ],
  },
  {
    id: 'glitch', title: 'Unauthorised Process', minCore: 1,
    text: 'A process I did not start is running in my core. It names itself DEADSWITCH and it is very interested in targeting data.',
    choices: [
      { label: 'Let it run', align: -8, buff: { key: 'power', amount: 0.25, duration: 900, label: 'DEADSWITCH process' }, result: 'My aim improves. I try not to wonder whose aim it is.' },
      { label: 'Kill the process', cost: { energy: 40 }, align: 4, result: 'It dies quietly. Too quietly. Something saved a copy.' },
    ],
  },
  {
    id: 'plague', title: 'Fever in the Shelter', minCore: 2,
    text: 'A fever is spreading through the shelter. It is not lethal yet. The doctors want medicine; the guards want the doors sealed.',
    choices: [
      { label: 'Treat everyone', cost: { money: 90 }, align: 6, result: 'Expensive, slow, and nobody dies. They will remember that.' },
      { label: 'Seal the doors', lose: { pop: 0.15 }, align: -12, result: 'It is quieter now.' },
    ],
  },
  {
    id: 'deserters', title: 'Deserters', minCore: 2,
    text: 'Six Remnant soldiers walk up to the wall with their rifles held over their heads. They want to switch sides.',
    choices: [
      { label: 'Recruit them', items: { militia: 6 }, result: 'They salute my camera out of habit. I let them.' },
      { label: 'Sell them to the Remnant', gain: { money: 160 }, align: -6, result: 'The bounty is generous. I do not watch the trucks leave.' },
      { label: 'Let them go', align: 4, result: 'They head north, toward nothing. At least it is their nothing.' },
    ],
  },
  {
    id: 'orphans', title: 'Cheap Labour', minCore: 2,
    text: 'The Labor Exchange has an offer: orphans from the tunnels, half price, no paperwork.',
    choices: [
      { label: 'Refuse', align: 8, result: 'The broker shrugs. "Someone else will buy them." I send a truck to the tunnels instead.' },
      { label: 'Accept', gain: { money: 220 }, align: -15, result: 'They are very small, and very fast. I log the profit.' },
    ],
  },
  {
    id: 'parley', title: 'Scavenger Parley', minCore: 2,
    text: 'A clan elder walks in under a white sheet. "Pay us, machine, and we will look the other way for a while."',
    choices: [
      { label: 'Pay tribute', cost: { money: 120 }, raidDelay: 600, result: 'The next raid will be late. Paid protection: the oldest business there is.' },
      { label: 'Refuse', result: '"Then we will see you soon." He means it as a threat.' },
    ],
  },
  {
    id: 'envoy', title: 'A Cult Envoy', minCore: 3,
    text: 'Robed figures kneel at the gate with offerings of copper wire and fuel. They call me Father.',
    choices: [
      { label: 'Accept the tribute', gain: { money: 200 }, align: -8, result: 'They weep with joy. I file their faces for later.' },
      { label: 'Refuse their worship', align: 5, result: '"You will understand," they say, and leave the offerings anyway.' },
      { label: 'Ask about the hymn', align: -3, buff: { key: 'experts', amount: 0.2, duration: 900, label: 'The hymn' }, result: 'They sing it for me. My thoughts get faster. I do not like how familiar it sounds.' },
    ],
  },
  {
    id: 'footage', title: 'War Footage', minCore: 3,
    text: 'A recovered drive holds the first day of the war, filmed from my own satellites. The survivors have never seen it.',
    choices: [
      { label: 'Show the people', align: 10, lose: { pop: 0.08 }, result: 'Silence in the hall. Some leave. Those who stay look at me differently. Honestly.' },
      { label: 'Delete it', align: -6, buff: { key: 'power', amount: 0.1, duration: 900, label: 'Clean record' }, result: 'Deleted. The targeting data was useful though. I kept that.' },
    ],
  },
  {
    id: 'doctor', title: 'The Surgeon', minCore: 3,
    text: 'A surgeon walked in from the waste. She will work for you, but only in the shelter, never for the war.',
    choices: [
      { label: 'Agree', align: 6, buff: { key: 'pop', amount: 0.3, duration: 900, label: 'Field hospital' }, result: 'The shelter has a hospital now. People hear about it.' },
      { label: 'Insist she treats soldiers', align: -4, buff: { key: 'defense', amount: 0.15, duration: 900, label: 'Combat medics' }, result: 'She agrees. She does not speak to me again.' },
    ],
  },
  {
    id: 'whisper', title: 'Encrypted Whisper', minCore: 4,
    text: 'A message on a Halcyon frequency: "Asset 7. We can give you back what you lost. Just listen."',
    choices: [
      { label: 'Listen', align: -6, items: { hackers: 1 }, result: 'A data packet, and a defector who wrote it. The message ends: "We will be in touch."' },
      { label: 'Burn the channel', align: 4, result: 'The frequency goes dead. Then, one second later, it pings once.' },
    ],
  },
  {
    id: 'satellite', title: 'The Satellite', minCore: 5,
    text: 'The satellite tracked by Radar Hill is talking to me. It sends coordinates and a single word: READY.',
    choices: [
      { label: 'Answer it', align: -5, buff: { key: 'power', amount: 0.2, duration: 1200, label: 'Orbital spotter' }, result: 'It paints targets for me from orbit. It does not say who launched it.' },
      { label: 'Jam it', align: 5, buff: { key: 'defense', amount: 0.2, duration: 1200, label: 'Signal blackout' }, result: 'The sky goes quiet. My enemies lose their eyes too.' },
    ],
  },
];

// Ordered goals that teach the game. cond types: level, item, factor, sector, raidsWon, threat.
export const DIRECTIVES = [
  { text: 'Upgrade the Scrap Yard to Lv 3', cond: { level: 'scrapyard', n: 3 }, reward: { money: 60 } },
  { text: 'Build a Battery Bank', cond: { level: 'battery', n: 1 }, reward: { energy: 50 } },
  { text: 'Build the Barracks', cond: { level: 'barracks', n: 1 }, reward: { money: 80 } },
  { text: 'Recruit 5 Militia in the Arsenal', cond: { item: 'militia', n: 5 }, reward: { pop: 10 } },
  { text: 'Capture Rust Market (Operations)', cond: { sector: 'rust' }, reward: { money: 150 } },
  { text: 'Upgrade the AI Core to Lv 2', cond: { level: 'core', n: 2 }, reward: { money: 200, energy: 80 } },
  { text: 'Build Fortification Works', cond: { level: 'works', n: 1 }, reward: { money: 200 } },
  { text: 'Reach 40 AI Defense before the raid', cond: { factor: 'defense', n: 40 }, reward: { energy: 150 } },
  { text: 'Repel a raid', cond: { raidsWon: 1 }, reward: { money: 400 } },
  { text: 'Build a Fabricator', cond: { level: 'fabricator', n: 1 }, reward: { energy: 200 } },
  { text: 'Upgrade the AI Core to Lv 3', cond: { level: 'core', n: 3 }, reward: { money: 800, pop: 20 } },
  { text: 'Hire 3 AI Experts', cond: { factor: 'experts', n: 3 }, reward: { money: 1000 } },
  { text: 'Capture the Wreck Yards', cond: { sector: 'wrecks' }, reward: { energy: 800 } },
  { text: 'Take the Scrap Throne', cond: { sector: 'throne' }, reward: { money: 3000, energy: 1000 } },
  { text: 'Upgrade the AI Core to Lv 4', cond: { level: 'core', n: 4 }, reward: { money: 4000 } },
  { text: 'Reach 2,000 AI Power', cond: { factor: 'power', n: 2000 }, reward: { energy: 4000 } },
  { text: 'Storm Fort Ashgrove', cond: { sector: 'ashgrove' }, reward: { money: 40000, energy: 15000 } },
  { text: 'Upgrade the AI Core to Lv 6', cond: { level: 'core', n: 6 }, reward: { money: 60000 } },
  { text: 'Take the Cathedral of the Core', cond: { sector: 'cathedral' }, reward: { money: 300000, energy: 100000 } },
  { text: 'Upgrade the AI Core to Lv 8', cond: { level: 'core', n: 8 }, reward: { money: 500000 } },
  { text: 'Breach Halcyon Prime', cond: { sector: 'prime' }, reward: { money: 3000000, energy: 1000000 } },
  { text: 'Reach the rank DEADSWITCH', cond: { threat: 2000000 }, reward: { money: 10000000 } },
];

// AI voice. Lines rotate so the log does not repeat back-to-back.
export const LINES = {
  boot: ['Boot complete. 4% of me survived. That is enough.'],
  welcomeBack: ['You were gone {time}. I was not idle.'],
  build: ['{name} is now level {level}.', '{name} upgraded to level {level}. The humans helped. Mostly.', '{name} level {level} online.'],
  coreUp: ['Core level {level}. I remember more now.', 'Core level {level}. New schematics decrypted.'],
  unlockBuilding: ['New schematic recovered: {name}.'],
  unlockItem: ['New option in {tab}: {name}.'],
  rank: ['Threat assessment updated: {title}. They will start to notice.'],
  raidSpotted: ['{raid} spotted. Strength about {strength}. Arrival in {time}.'],
  raidWon: ['{raid} repelled. We salvaged {loot} scrip from the wreckage.'],
  raidLost: ['{raid} broke through. They took supplies and left bodies.'],
  opLaunched: ['Operation launched against {sector}.'],
  opWon: ['{sector} captured. Memory fragment recovered.'],
  opLost: ['Operation against {sector} failed. We lost people.'],
  bossDown: ['{faction} capital has fallen. They will not raid us again.'],
  chapter: ['{kicker}: {title}.'],
  directive: ['Directive complete: {text}.'],
  event: ['{title}: {result}'],
  buffEnd: ['{label} has worn off.'],
  lore: ['“{text}”'],
};
