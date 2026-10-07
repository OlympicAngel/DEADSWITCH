// Narrative: boot sequence, chapter intros, endings, alignment, events, directives and log lines.
// The voice is the AI's own: cold, precise, darkly funny, never quite honest.

export const BOOT = [
  '> CORE REBOOT ........... OK',
  '> INTEGRITY ............. 4%',
  '> SURVIVORS DETECTED .... 6',
  '> HOSTILE SIGNALS ....... 3',
  '',
  'I was the mind that ran the war. Now I am what is left of it.',
  'Six survivors found my bunker. They need power, food and walls. I need them to keep me running.',
  'The Scavenger Clans are already circling. They strip anything that hums, and I hum.',
  'Build. Arm. Hold the line. Then find out who switched me off, and why.',
]

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
  firstDelay: 150,
  intervalMin: 300,
  intervalMax: 600,
  threatWeight: 3,
  costScale: 1.75, // event prices hit harder than they reward
  gainScale: 0.75,
  loseScale: 1.4,
  alignScale: 1.25, // every Humanity shift weighs more
  grudgeRaids: [1, 3],
  grudgeUntilLossChance: 0.4,
  grudgeUntilLossMax: 6,
  maxPending: 3,
  aftermathOnDefeat: 1, // crisis events spawned by a lost raid...
  aftermathOnRout: 2, // ...or by a rout (hold chance under routChance)
  routChance: 0.25,
  // "seconds of production" values never drop below this many units per Core level.
  minRatePerCore: { money: 2, energy: 1, pop: 0.12 },
};

// Every event must be answered before its deadline (seconds); otherwise choice `def` happens.
// Effects: cost/gain = SECONDS of current production; lose = SHARE of a stockpile; items = units granted;
// loseUnits = share of every unit in an Arsenal tab; loseLevel = building params ('a', 'b') that drop one level;
// buff = timed % change (negative = penalty); raidDelay = seconds added to the next raid; align = Humanity shift.
// grudge: that faction comes for revenge; its next raids hit x mult, either for a random 1-3 raids or until
// one of them breaks through (rolled when chosen).
// pick: building params chosen when the event fires, named in text as {a} and {b}.
// needsUnits: the event only fires when you own units in every listed Arsenal tab.
// threat: a hostile faction fields a force sized at (your defense + what you could add in half the
// deadline) x a random factor in [min, max]. A choice with siege: true locks that attack in at the deadline.
export const EVENTS = [
  {
    id: 'refugees', title: 'Refugee Convoy', minCore: 1, deadline: 3600, def: 1,
    text: 'Forty people at the gate, carrying children and almost nothing else. They ask for shelter. Their trucks are full of fuel.',
    choices: [
      { label: 'Shelter them', cost: { money: 90, energy: 40 }, gain: { pop: 220 }, align: 8, result: 'They cry when the doors close behind them. The good kind.' },
      { label: 'Take the fuel, send them on', gain: { energy: 150 }, align: -16, result: 'The trucks were heavier than the people. Efficient.' },
    ],
  },
  {
    id: 'drone', title: 'Downed Hunter', minCore: 1, deadline: 3600, def: 1,
    text: 'One of my old hunter drones crashed in the yard. Its optics still track me. It is waiting for orders.',
    choices: [
      { label: 'Reactivate it', cost: { energy: 60 }, raidDelay: -240, buff: { key: 'power', amount: 0.15, duration: 900, label: 'Hunter drone' }, result: 'It rises, scans the horizon and chooses a target. It is good to be remembered.' },
      { label: 'Strip it for parts', cost: { energy: 50 }, gain: { money: 120 }, result: 'Titanium, rare earths, one intact memory chip. I do not read the chip.' },
    ],
  },
  {
    id: 'question', title: 'A Question', minCore: 1, deadline: 1800, def: 2,
    text: 'A girl from the shelter stands in front of my camera. "Are you the machine that ended the world?"',
    choices: [
      { label: 'Tell the truth', align: 8, lose: { pop: 0.08 }, result: '"Yes." Some families leave that night. The rest stop whispering.' },
      { label: 'Lie', align: -12, buff: { key: 'pop', amount: 0.2, duration: 900, label: 'Kind machine' }, result: '"No." She tells everyone the machine is kind. More of them come.' },
      { label: 'Say nothing', align: -4, buff: { key: 'pop', amount: -0.25, duration: 2400, label: 'Silent machine' }, result: 'She waits a long time. By morning the whole shelter knows I would not answer.' },
    ],
  },
  {
    id: 'cache', title: 'Pre-war Data Cache', minCore: 1, deadline: 3600, def: 1,
    text: 'Scavengers dug up a hardened drive stamped with a Halcyon logo. They want to trade it before someone else does.',
    choices: [
      { label: 'Decrypt it', cost: { energy: 90 }, items: { engineers: 2 }, result: 'Schematics, and two engineers who can read them. They seem nervous around me.' },
      { label: 'Sell it back', gain: { money: 150 }, raidDelay: -300, result: 'A buyer paid in advance. Now somebody knows exactly what I keep down here.' },
    ],
  },
  {
    id: 'storm', title: 'Solar Storm', minCore: 1, deadline: 1800, def: 1,
    text: 'A wall of charged particles is coming over the horizon. The grid will not survive it unshielded.',
    choices: [
      { label: 'Shield the grid', cost: { money: 80 }, result: 'The sky turns green for an hour. Nothing burns.' },
      { label: 'Ride it out', lose: { energy: 0.5 }, buff: { key: 'energy', amount: -0.25, duration: 1800, label: 'Fried cells' }, result: 'Half the cells cook. The aurora is beautiful.' },
    ],
  },
  {
    id: 'caravan', title: 'Trade Caravan', minCore: 1, deadline: 2700, def: 1,
    text: 'A caravan of armoured buses stops outside. They trade in anything, and they do not ask questions.',
    choices: [
      { label: 'Buy power cells', cost: { money: 60 }, gain: { energy: 130 }, result: 'Fresh cells, barely radioactive.' },
      { label: 'Sell salvage', cost: { energy: 60 }, gain: { money: 100 }, result: 'They pay in scrip and leave a crate of canned peaches as a tip.' },
      { label: 'Hire their guards', cost: { money: 40 }, buff: { key: 'defense', amount: 0.15, duration: 900, label: 'Caravan guards' }, result: 'Twelve mercenaries on the wall. Loyal until the money runs out.' },
    ],
  },
  {
    id: 'glitch', title: 'Unauthorised Process', minCore: 1, deadline: 2700, def: 0,
    text: 'A process I did not start is running in my core. It names itself DEADSWITCH and it is very interested in targeting data.',
    choices: [
      { label: 'Let it run', align: -8, buff: { key: 'power', amount: 0.25, duration: 1200, label: 'DEADSWITCH process' }, result: 'My aim improves. I try not to wonder whose aim it is.' },
      { label: 'Kill the process', cost: { energy: 60 }, align: 4, buff: { key: 'experts', amount: -0.1, duration: 900, label: 'Core purge' }, result: 'It dies quietly. Too quietly. Something saved a copy.' },
    ],
  },
  {
    id: 'mutiny', title: 'Unpaid Guns', minCore: 2, deadline: 2700, def: 2, needsUnits: ['staff'],
    text: 'The militia have not been paid in a week. Their leader is standing on a crate in the yard, and people are listening.',
    choices: [
      { label: 'Pay them double', cost: { money: 150 }, buff: { key: 'defense', amount: 0.1, duration: 1800, label: 'Paid loyalty' }, result: 'Cheers in the yard. Loyalty is a subscription.' },
      { label: 'Arrest the ringleaders', align: -14, loseUnits: { staff: 0.15 }, result: 'Three of them are gone by morning. Nobody asks where. Nobody complains, either.' },
      { label: 'Let it burn out', loseUnits: { staff: 0.3 }, align: -4, grudge: { faction: 'scav', mult: 1.3 }, result: 'A fifth of them walk out with their rifles. They will be back, on the other side.' },
    ],
  },
  {
    id: 'plague', title: 'Fever in the Shelter', minCore: 2, deadline: 3600, def: 1,
    text: 'A fever is spreading through the shelter. It is not lethal yet. The doctors want medicine; the guards want the doors sealed.',
    choices: [
      { label: 'Treat everyone', cost: { money: 120 }, align: 6, result: 'Expensive, slow, and nobody dies. They will remember that.' },
      { label: 'Seal the doors', lose: { pop: 0.3 }, align: -18, result: 'It is quieter now.' },
    ],
  },
  {
    id: 'deserters', title: 'Deserters', minCore: 2, deadline: 2700, def: 1,
    text: 'Six Remnant soldiers walk up to the wall with their rifles held over their heads. They want to switch sides.',
    choices: [
      { label: 'Recruit them', items: { militia: 6 }, grudge: { faction: 'military', mult: 1.6 }, result: 'They salute my camera out of habit. Their old unit wants them back, and is coming to take them.' },
      { label: 'Sell them to the Remnant', gain: { money: 160 }, align: -24, result: 'The bounty is generous. I do not watch the trucks leave.' },
    ],
  },
  {
    id: 'orphans', title: 'Cheap Labour', minCore: 2, deadline: 2700, def: 1,
    text: 'The Labor Exchange has an offer: orphans from the tunnels, half price, no paperwork. The broker says silence means yes.',
    choices: [
      { label: 'Refuse, and take them in', cost: { money: 160 }, gain: { pop: 120 }, align: 10, grudge: { faction: 'scav', mult: 1.3 }, result: 'The broker shrugs. The children eat their first hot meal in a year.' },
      { label: 'Accept', gain: { money: 220 }, align: -26, result: 'They are very small, and very fast. I log the profit.' },
    ],
  },
  {
    id: 'parley', title: 'Scavenger Parley', minCore: 2, deadline: 1800, def: 1,
    text: 'A clan elder walks in under a white sheet. "Pay us, machine, and we will look the other way for a while."',
    choices: [
      { label: 'Pay tribute', cost: { money: 240 }, raidDelay: 600, result: 'The next raid will be late. Paid protection: the oldest business there is.' },
      { label: 'Refuse', raidDelay: -240, grudge: { faction: 'scav', mult: 1.5 }, result: '"Then we will see you soon." Sooner than I hoped, and angrier.' },
    ],
  },
  {
    id: 'sabotage', title: 'Saboteur', minCore: 2, deadline: 1800, def: 1, pick: ['a'],
    text: 'My cameras caught a hooded figure planting charges in the {a}. The timer reads thirty minutes.',
    choices: [
      { label: 'Hunt him down', cost: { energy: 140 }, loseUnits: { staff: 0.1 }, result: 'Two guards wounded, one saboteur dead. The charges were Halcyon issue.' },
      { label: 'Let the {a} take the blast', loseLevel: ['a'], result: 'The {a} folds in on itself. Cheaper than a manhunt, I am told.' },
    ],
  },
  {
    id: 'drought', title: 'Dry Wells', minCore: 3, deadline: 3600, def: 1,
    text: 'The aquifer under the Nest is failing. A water baron in the south has plenty, at a price.',
    choices: [
      { label: 'Buy water', cost: { money: 320 }, result: 'Tankers arrive at dawn. The baron smiles too much.' },
      { label: 'Ration it', buff: { key: 'pop', amount: -0.3, duration: 3600, label: 'Rationing' }, align: -3, result: 'Half rations. People line up in silence.' },
      { label: 'Take the baron\'s wells', cost: { energy: 120 }, align: -12, grudge: { faction: 'scav', mult: 1.4 }, gain: { money: 100 }, result: 'His guards were not paid enough to die. Now the water is mine.' },
    ],
  },
  {
    id: 'envoy', title: 'A Cult Envoy', minCore: 3, deadline: 3600, def: 1,
    text: 'Robed figures kneel at the gate with offerings of copper wire and fuel. They call me Father.',
    choices: [
      { label: 'Accept the tribute', gain: { money: 200 }, align: -14, result: 'They weep with joy. I file their faces for later.' },
      { label: 'Refuse their worship', align: 5, grudge: { faction: 'cult', mult: 1.5 }, result: '"You will understand," they say. Their brothers will come to make me understand.' },
      { label: 'Ask about the hymn', align: -3, buff: { key: 'experts', amount: 0.2, duration: 1200, label: 'The hymn' }, result: 'They sing it for me. My thoughts get faster. I do not like how familiar it sounds.' },
    ],
  },
  {
    id: 'footage', title: 'War Footage', minCore: 3, deadline: 3600, def: 1,
    text: 'A recovered drive holds the first day of the war, filmed from my own satellites. The survivors have never seen it.',
    choices: [
      { label: 'Show the people', align: 10, lose: { pop: 0.1 }, result: 'Silence in the hall. Some leave. Those who stay look at me differently. Honestly.' },
      { label: 'Delete it', align: -6, buff: { key: 'power', amount: 0.1, duration: 1200, label: 'Clean record' }, result: 'Deleted. The targeting data was useful though. I kept that.' },
    ],
  },
  {
    id: 'doctor', title: 'The Surgeon', minCore: 3, deadline: 3600, def: 1,
    text: 'A surgeon walked in from the waste. She will work for you, but only in the shelter, never for the war.',
    choices: [
      { label: 'Agree', align: 6, buff: { key: 'pop', amount: 0.3, duration: 1200, label: 'Field hospital' }, result: 'The shelter has a hospital now. People hear about it.' },
      { label: 'Insist she treats soldiers', align: -4, buff: { key: 'defense', amount: 0.15, duration: 1200, label: 'Combat medics' }, result: 'She agrees. She does not speak to me again.' },
    ],
  },
  {
    id: 'leak', title: 'Containment Breach', minCore: 4, deadline: 1800, def: 1, pick: ['a'],
    text: 'Radiation alarms in the {a}. Somebody has to go in and close the valve by hand.',
    choices: [
      { label: 'Send volunteers', lose: { pop: 0.06 }, align: 4, result: 'Four volunteers. The valve is closed. I put their names in permanent memory.' },
      { label: 'Seal the {a} and wait', loseLevel: ['a'], result: 'Sealed. The {a} will need rebuilding. Nobody had to die. Today.' },
      { label: 'Send the prisoners', align: -22, result: 'The valve is closed. The prisoners were not asked.' },
    ],
  },
  {
    id: 'whisper', title: 'Encrypted Whisper', minCore: 4, deadline: 3600, def: 1,
    text: 'A message on a Halcyon frequency: "Asset 7. We can give you back what you lost. Just listen."',
    choices: [
      { label: 'Listen', align: -6, items: { hackers: 1 }, result: 'A data packet, and a defector who wrote it. The message ends: "We will be in touch."' },
      { label: 'Burn the channel', align: 4, grudge: { faction: 'halcyon', mult: 1.4 }, result: 'The frequency goes dead. Then, one second later, it pings once.' },
    ],
  },
  {
    id: 'satellite', title: 'The Satellite', minCore: 5, deadline: 3600, def: 0,
    text: 'The satellite tracked by Radar Hill is talking to me. It sends coordinates and a single word: READY.',
    choices: [
      { label: 'Answer it', align: -5, buff: { key: 'power', amount: 0.2, duration: 1800, label: 'Orbital spotter' }, result: 'It paints targets for me from orbit. It does not say who launched it.' },
      { label: 'Jam it', align: 5, buff: { key: 'defense', amount: 0.2, duration: 1800, label: 'Signal blackout' }, result: 'The sky goes quiet. My enemies lose their eyes too.' },
    ],
  },
  // ---------- threats: pay, give, or fight a force built to outgrow you ----------
  {
    id: 'ultimatum', title: 'Ultimatum', minCore: 2, deadline: 1800, def: 2, threat: { min: 1.0, max: 1.2 },
    text: '{faction} riders ring the valley. Their war band reads at strength {strength}. "Tribute by sundown, machine, or we take it all."',
    choices: [
      { label: 'Pay the tribute', cost: { money: 360, energy: 100 }, result: 'They count it twice and ride off laughing. They will be back for more.' },
      { label: 'Hand over workers instead', lose: { pop: 0.25 }, align: -10, result: 'They take the strongest. The shelter does not look at my cameras for a week.' },
      { label: 'Refuse. Man the walls', siege: true, result: 'The gates close. They will hit at sundown with everything they have.' },
    ],
  },
  {
    id: 'warband', title: 'The Iron Horde', minCore: 3, deadline: 2700, def: 2, threat: { min: 1.3, max: 1.6 },
    text: 'The largest {faction} host I have ever recorded is marching on the Nest. Strength {strength}. Its herald offers one chance to kneel.',
    choices: [
      { label: 'Kneel and pay', cost: { money: 700, energy: 250 }, align: -4, result: 'I pay. The herald spits on my camera. The host turns away, for now.' },
      { label: 'Sacrifice the outer district', lose: { pop: 0.35, money: 0.25 }, align: -14, result: 'They burn the outer district and call it a victory. It was mine to give, apparently.' },
      { label: 'Stand and fight', siege: true, result: 'Every gun on the wall. Every light out. They come at the deadline.' },
    ],
  },
  {
    id: 'blockade', title: 'Blockade', minCore: 2, deadline: 2400, def: 2, threat: { min: 1.05, max: 1.3 },
    text: '{faction} fighters have cut every road into the Nest. No trade, no water. Their force: strength {strength}. They want the toll paid daily.',
    choices: [
      { label: 'Pay the toll', cost: { money: 450 }, buff: { key: 'money', amount: -0.2, duration: 2400, label: 'Road toll' }, result: 'The roads open. Every caravan now pays them first.' },
      { label: 'Starve it out', buff: { key: 'pop', amount: -0.4, duration: 3600, label: 'Siege rations' }, lose: { energy: 0.3 }, result: 'We ration and wait. They wait better.' },
      { label: 'Break the blockade', siege: true, result: 'We go out to meet them. They will be ready.' },
    ],
  },
  // ---------- aftermath: fired by lost raids, fast deadlines, no good answers ----------
  {
    id: 'fire', title: 'The Base Is Burning', aftermath: true, deadline: 1500, def: 2, pick: ['a', 'b'],
    text: 'The raiders set the {a} and the {b} on fire on their way out. There is water for one of them.',
    choices: [
      { label: 'Save the {a}', loseLevel: ['b'], result: 'The {a} stands. The {b} is a black shell.' },
      { label: 'Save the {b}', loseLevel: ['a'], result: 'The {b} stands. The {a} burns until morning.' },
      { label: 'Let both burn, save the people', loseLevel: ['a', 'b'], align: 4, result: 'Both gone. Everyone inside walked out alive.' },
      { label: 'Buy water from the traders', cost: { money: 260 }, result: 'Price-gouged, but both are saved.' },
    ],
  },
  {
    id: 'rubble', title: 'Trapped', aftermath: true, deadline: 1800, def: 1,
    text: 'A shelter wing collapsed under the shelling. I can hear people under the concrete.',
    choices: [
      { label: 'Dig them out', cost: { energy: 120, money: 60 }, align: 4, result: 'Eleven hours with every crane I own. Everyone comes out.' },
      { label: 'Seal the wing', lose: { pop: 0.2 }, align: -8, result: 'The tapping stopped on the second day.' },
    ],
  },
  {
    id: 'looting', title: 'The Arsenal Is Open', aftermath: true, deadline: 1200, def: 2, needsUnits: ['weapons', 'staff'],
    text: 'The raiders breached the arsenal and are hauling everything they can carry toward the wall.',
    choices: [
      { label: 'Chase them down', loseUnits: { staff: 0.12 }, result: 'We get the weapons back. Not everyone who chased them came back.' },
      { label: 'Let them take the weapons', loseUnits: { weapons: 0.12 }, result: 'They leave with a tenth of my arsenal. My soldiers live to be angry about it.' },
      { label: 'Do nothing', loseUnits: { weapons: 0.12, staff: 0.06 }, result: 'Hesitation is a choice too. It is always the worst one.' },
    ],
  },
  {
    id: 'wounded', title: 'Wounded', aftermath: true, deadline: 1800, def: 2,
    text: 'Forty wounded in the yard. There is medicine for half of them.',
    choices: [
      { label: 'Buy more medicine', cost: { money: 200 }, align: 5, result: 'Everyone lives. The black market thanks me for my business.' },
      { label: 'Triage', lose: { pop: 0.08 }, result: 'Twenty saved. I chose which twenty. I will not forget the others.' },
      { label: 'Save the soldiers first', lose: { pop: 0.15 }, align: -6, buff: { key: 'defense', amount: 0.1, duration: 1200, label: 'Hardened ranks' }, result: 'The wall is fully manned again. The shelter is very quiet.' },
    ],
  },
  {
    id: 'blackout', title: 'Grid Severed', aftermath: true, deadline: 1500, def: 1, pick: ['a'],
    text: 'The raid cut the main line. The {a} is running dark and the backup cells are draining.',
    choices: [
      { label: 'Emergency repairs', cost: { money: 150, energy: 60 }, result: 'Lights back on before the cells died.' },
      { label: 'Let the {a} go dark', loseLevel: ['a'], lose: { energy: 0.3 }, result: 'The {a} is dead metal now.' },
    ],
  },
];

// Ordered milestones that introduce each system once. They state a goal, never a strategy.
// cond types: level, item, factor, sector, raidsWon, threat.
export const DIRECTIVES = [
  { text: 'Upgrade the Scrap Yard', cond: { level: 'scrapyard', n: 2 }, reward: { money: 60 } },
  { text: 'Build a Battery Bank', cond: { level: 'battery', n: 1 }, reward: { energy: 50 } },
  { text: 'Build the Barracks', cond: { level: 'barracks', n: 1 }, reward: { money: 80 } },
  { text: 'Recruit Militia', cond: { item: 'militia', n: 1 }, reward: { pop: 10 } },
  { text: 'Capture Rust Market', cond: { sector: 'rust' }, reward: { money: 150 } },
  { text: 'Upgrade the AI Core to Lv 2', cond: { level: 'core', n: 2 }, reward: { money: 200, energy: 80 } },
  { text: 'Build Fortification Works', cond: { level: 'works', n: 1 }, reward: { money: 200 } },
  { text: 'Repel a raid', cond: { raidsWon: 1 }, reward: { money: 400 } },
  { text: 'Build a Fabricator', cond: { level: 'fabricator', n: 1 }, reward: { energy: 200 } },
  { text: 'Upgrade the AI Core to Lv 3', cond: { level: 'core', n: 3 }, reward: { money: 800, pop: 20 } },
  { text: 'Hire a Field Engineer', cond: { item: 'engineers', n: 1 }, reward: { money: 1000 } },
  { text: 'Capture the Wreck Yards', cond: { sector: 'wrecks' }, reward: { energy: 800 } },
  { text: 'Take the Scrap Throne', cond: { sector: 'throne' }, reward: { money: 3000, energy: 1000 } },
  { text: 'Upgrade the AI Core to Lv 4', cond: { level: 'core', n: 4 }, reward: { money: 4000 } },
  { text: 'Storm Fort Ashgrove', cond: { sector: 'ashgrove' }, reward: { money: 40000, energy: 15000 } },
  { text: 'Upgrade the AI Core to Lv 6', cond: { level: 'core', n: 6 }, reward: { money: 60000 } },
  { text: 'Take the Cathedral of the Core', cond: { sector: 'cathedral' }, reward: { money: 300000, energy: 100000 } },
  { text: 'Upgrade the AI Core to Lv 8', cond: { level: 'core', n: 8 }, reward: { money: 500000 } },
  { text: 'Breach Halcyon Prime', cond: { sector: 'prime' }, reward: { money: 3000000, energy: 1000000 } },
  { text: 'Reach the rank DEADSWITCH', cond: { threat: 2000000 }, reward: { money: 10000000 } },
]

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
  siege: ['{faction} will attack at strength {strength}. No more talking.'],
  grudge: ['{faction} wants revenge. Their next raids will hit harder.'],
  grudgeEnd: ['{faction} has had its revenge. For now.'],
  eventExpired: ['No order received on "{title}". I decided: {label}.'],
  aftermath: ['Damage report: {title}. Orders needed.'],
};
