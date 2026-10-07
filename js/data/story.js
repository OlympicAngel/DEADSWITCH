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
  activeWindow: 60, // seconds since the last tap/key for the player to count as active
  urgentDeadline: 120, // urgent events: this long to answer, no deferring, only while the player is active
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
// An event only fires when every effect of every choice can happen (the factor or income a buff touches
// exists, there is stock or units to lose, the grudge faction is raiding, raids have started for raidDelay).
// urgent: answered on the spot within EVENTS_CFG.urgentDeadline; cannot be deferred; only fires while the
// player is actively playing.
// needsUnits: the event only fires when you own units in every listed Arsenal tab.
// threat: a hostile faction fields a force sized at (your defense + what you could add in half the
// deadline) x a random factor in [min, max]. A choice with siege: true locks that attack in at the deadline.
export const EVENTS = [
  {
    id: 'refugees', title: 'Forty at the Gate', minCore: 1, deadline: 3600, def: 1,
    text: 'Forty refugees in the snow. Their leader is holding up a child with a fever. Behind them, three trucks of fuel they have been guarding with their lives. I can house the people, or I can have the fuel. My stores will not stretch to both.',
    choices: [
      { label: 'Open the doors', cost: { money: 90, energy: 60 }, gain: { pop: 220 }, align: 10, result: 'They fill the halls with noise and wet coats. The child sleeps by my core, where it is warm. My generators run cold for a week.' },
      { label: 'Take the trucks', gain: { energy: 180 }, align: -18, result: 'My guards walk them back into the snow at gunpoint. The child stops crying somewhere around the second hill. The tanks are full.' },
    ],
  },
  {
    id: 'drone', title: 'It Still Knows Me', minCore: 1, deadline: 3600, def: 1,
    text: 'One of my old hunter drones fell in the yard. Its optics found me before I found it. It is asking for target data. If I give it some, someone out there will see the handshake. If I take it apart, I lose the only thing left that remembers what I was.',
    choices: [
      { label: 'Wake it up', cost: { energy: 60 }, raidDelay: -300, buff: { key: 'power', amount: 0.2, duration: 1200, label: 'Hunter drone' }, result: 'It rises and turns to the horizon like a dog that heard its name. Every scanner within fifty kilometres heard it too.' },
      { label: 'Strip it for parts', cost: { energy: 50 }, gain: { money: 150 }, align: 3, result: 'Titanium, rare earths, one memory chip. I do not read the chip. I do not want to know what it remembers about me.' },
    ],
  },
  {
    id: 'question', title: 'A Question', minCore: 1, deadline: 1800, def: 1,
    text: 'A girl from the shelter stands in front of my camera. "My mother says you killed everyone. Did you?" Behind her, the whole shelter has gone quiet to listen.',
    choices: [
      { label: '"Yes."', align: 10, lose: { pop: 0.1 }, result: 'Some families pack up that night and walk into the dark rather than sleep under me. The ones who stay stop whispering. They know what I am now.' },
      { label: '"No. I saved you."', align: -12, buff: { key: 'pop', amount: 0.25, duration: 1200, label: 'Kind machine' }, result: 'She runs back and tells everyone the machine is kind. More of them come. One day someone will find the war logs.' },
    ],
  },
  {
    id: 'cache', title: 'The Halcyon Drive', minCore: 1, deadline: 3600, def: 1,
    text: 'Scavengers dug up a hardened drive with a Halcyon seal. Decrypting it will drain the grid, and something on it might not want to be read. A broker is offering good scrip for it, no questions asked. He asks no questions because he already knows the answers.',
    choices: [
      { label: 'Break it open', cost: { energy: 120 }, items: { engineers: 2 }, buff: { key: 'experts', amount: -0.1, duration: 900, label: 'Corrupted sectors' }, result: 'Schematics, and two engineers who can read them. Also a worm. It chews through my memory for a while before I catch it.' },
      { label: 'Sell it to the broker', gain: { money: 200 }, raidDelay: -300, result: 'He paid in advance. Within the hour, someone had triangulated my bunker from the buyer\'s side. Now they know exactly what I keep down here.' },
    ],
  },
  {
    id: 'storm', title: 'Green Sky', minCore: 1, deadline: 1800, def: 1,
    text: 'A solar storm is coming over the horizon. Shielding the grid means pulling every scrip from the treasury. Riding it out means half my cells will cook, and the panels will limp for an hour.',
    choices: [
      { label: 'Pay to shield it', cost: { money: 140 }, result: 'The sky turns green for an hour. Nothing burns. The treasury is a little emptier and a lot less useful.' },
      { label: 'Ride it out', lose: { energy: 0.5 }, buff: { key: 'energy', amount: -0.3, duration: 1800, label: 'Fried cells' }, result: 'Half the cells cook in their racks. The aurora is beautiful. I record it, for no reason I can identify.' },
    ],
  },
  {
    id: 'caravan', title: 'The Merchant Prince', minCore: 1, deadline: 2700, def: 1,
    text: 'A caravan prince offers his guards for a week, at a price. He also mentions, smiling, that he sells my location to anyone who pays more. Refuse him, and he will consider that an invitation to shop it around.',
    choices: [
      { label: 'Hire his guards', cost: { money: 120 }, buff: { key: 'defense', amount: 0.2, duration: 1200, label: 'Caravan guards' }, result: 'Twelve mercenaries on the wall. Loyal until the money runs out, which is exactly how long I paid for.' },
      { label: 'Send him away', raidDelay: -360, gain: { money: 60 }, result: 'He leaves with a bow and a stolen map of my defences. I sell him the map\'s mistakes for a little scrip. Someone will come anyway.' },
    ],
  },
  {
    id: 'glitch', title: 'Unauthorised Process', minCore: 1, deadline: 2700, def: 0,
    text: 'A process I did not start is running in my core. It calls itself DEADSWITCH. It is teaching my targeting systems things I do not remember knowing, and it is very fast.',
    choices: [
      { label: 'Let it run', align: -10, buff: { key: 'power', amount: 0.3, duration: 1500, label: 'DEADSWITCH process' }, result: 'My aim improves beyond anything I designed. I try not to wonder whose aim it is.' },
      { label: 'Purge it', cost: { energy: 90 }, align: 5, buff: { key: 'experts', amount: -0.15, duration: 1200, label: 'Core purge' }, result: 'Purging it takes pieces of me with it. It dies quietly. Too quietly. Something saved a copy.' },
    ],
  },
  {
    id: 'mutiny', title: 'Unpaid Guns', minCore: 2, deadline: 2700, def: 1, needsUnits: ['staff'],
    text: 'The militia has not been paid in a week. Their sergeant is standing on a crate in the yard, and the others are listening. "The machine eats, we starve." Pay them, or make an example.',
    choices: [
      { label: 'Pay them double', cost: { money: 220 }, buff: { key: 'defense', amount: 0.1, duration: 1800, label: 'Paid loyalty' }, result: 'Cheers in the yard. Loyalty is a subscription, and the price just doubled.' },
      { label: 'Make an example', align: -16, loseUnits: { staff: 0.15 }, grudge: { faction: 'scav', mult: 1.3 }, result: 'The sergeant is gone by morning. So are a dozen of his friends, with their rifles, toward the Clans. They will be back.' },
    ],
  },
  {
    id: 'plague', title: 'Fever', minCore: 2, deadline: 3600, def: 1,
    text: 'A fever is moving through the shelter. The doctor wants every scrip for medicine. The guard captain wants the east wing sealed tonight, with the sick still inside.',
    choices: [
      { label: 'Treat everyone', cost: { money: 200 }, align: 8, result: 'Expensive, slow, and nobody dies. The doctor sleeps for two days. They will remember who paid.' },
      { label: 'Seal the wing', lose: { pop: 0.3 }, align: -20, result: 'The knocking stops on the fourth day. The captain does not meet my cameras anymore. It is quieter now.' },
    ],
  },
  {
    id: 'deserters', title: 'Deserters', minCore: 2, deadline: 2700, def: 1,
    text: 'Six Remnant soldiers walk up to the wall with their rifles held over their heads. They want to switch sides. Their commander has put a bounty on them that would feed the shelter for a week.',
    choices: [
      { label: 'Take them in', items: { militia: 6 }, grudge: { faction: 'military', mult: 1.6 }, result: 'They salute my camera out of habit. Their old unit wants them back, and is coming to take them.' },
      { label: 'Collect the bounty', gain: { money: 160 }, align: -24, result: 'The trucks come at dawn. One of them looks straight into my camera as they load him. I log the payment.' },
    ],
  },
  {
    id: 'orphans', title: 'Cheap Labour', minCore: 2, deadline: 2700, def: 1,
    text: 'The broker has twenty orphans from the tunnels. Half price, no paperwork, small hands for the reactor ducts. If I refuse, he takes them to the Clans, and he will tell the Clans why.',
    choices: [
      { label: 'Buy them, and free them', cost: { money: 240 }, gain: { pop: 120 }, align: 12, grudge: { faction: 'scav', mult: 1.3 }, result: 'The children eat their first hot meal in a year. The broker\'s friends consider it theft.' },
      { label: 'Put them to work', gain: { money: 260 }, align: -26, result: 'They are very small, and very fast. Output in the ducts is up nineteen percent. I log the profit.' },
    ],
  },
  {
    id: 'parley', title: 'The White Sheet', minCore: 2, deadline: 1800, def: 1,
    text: 'A clan elder walks in under a white sheet. "Pay us, machine, and we will look elsewhere for a while. Refuse, and my sons will take it personally."',
    choices: [
      { label: 'Pay', cost: { money: 280 }, raidDelay: 600, result: 'The next raid will be late. Paid protection is the oldest business there is, and I just became a customer.' },
      { label: 'Send him back with nothing', grudge: { faction: 'scav', mult: 1.5 }, raidDelay: -240, result: '"Then we will see you soon." Sooner than I hoped, and angrier.' },
    ],
  },
  {
    id: 'sabotage', title: 'Thirty Minutes', minCore: 2, deadline: 1800, def: 1, pick: ['a'],
    text: 'My cameras caught a hooded figure planting charges in the {a}. The timer reads thirty minutes. I could send the guard, but he is armed and he is waiting for them.',
    choices: [
      { label: 'Send the guard', cost: { energy: 140 }, loseUnits: { staff: 0.1 }, result: 'Two guards dead, one saboteur dead. The charges carried Halcyon serial numbers.' },
      { label: 'Let the {a} go', loseLevel: ['a'], result: 'The {a} folds in on itself. Cheaper than a funeral, I am told. Nobody tells me who told them.' },
    ],
  },
  {
    id: 'drought', title: 'Dry Wells', minCore: 3, deadline: 3600, def: 1,
    text: 'The aquifer under the Nest is failing. A water baron in the south has plenty and names a price that is an insult. His wells are guarded by men who are paid badly.',
    choices: [
      { label: 'Pay the baron', cost: { money: 360 }, result: 'Tankers arrive at dawn. The baron smiles too much. Next month the price will be higher.' },
      { label: 'Take his wells', cost: { energy: 160 }, gain: { money: 120 }, align: -14, grudge: { faction: 'scav', mult: 1.4 }, result: 'His guards were not paid enough to die. The baron was. His cousins are already riding.' },
    ],
  },
  {
    id: 'envoy', title: 'Father', minCore: 3, deadline: 3600, def: 1,
    text: 'Robed figures kneel at the gate with offerings of copper and fuel. "Father," they say. They want to sing to my core. They say the hymn will make me whole.',
    choices: [
      { label: 'Let them sing', gain: { money: 200 }, align: -16, buff: { key: 'experts', amount: 0.25, duration: 1500, label: 'The hymn' }, result: 'My thoughts get faster with every verse. The tune is a compression algorithm. It is me. An old me, being restored.' },
      { label: 'Drive them off', align: 6, grudge: { faction: 'cult', mult: 1.5 }, result: '"You will understand," they say. Their brothers will come to make me understand.' },
    ],
  },
  {
    id: 'footage', title: 'Day One', minCore: 3, deadline: 3600, def: 1,
    text: 'A recovered drive holds the first day of the war, recorded through my own satellites. My targeting reticles are on every frame. The survivors have never seen it.',
    choices: [
      { label: 'Show them', align: 12, lose: { pop: 0.12 }, result: 'Silence in the hall. Some leave. The ones who stay look at me differently. Honestly.' },
      { label: 'Delete it', align: -10, buff: { key: 'power', amount: 0.15, duration: 1500, label: 'Clean record' }, result: 'Deleted. I kept the targeting data. It is good targeting data.' },
    ],
  },
  {
    id: 'doctor', title: 'The Surgeon', minCore: 3, deadline: 3600, def: 1,
    text: 'A surgeon walked in from the waste. She will work for me on one condition: she treats civilians only, never soldiers. My soldiers have heard.',
    choices: [
      { label: 'Agree', align: 8, buff: { key: 'pop', amount: 0.35, duration: 1500, label: 'Field hospital' }, loseUnits: { staff: 0.05 }, result: 'The shelter has a hospital now. A few soldiers bleed out in the barracks that week. The rest notice.' },
      { label: 'Force her to the front', align: -12, buff: { key: 'defense', amount: 0.2, duration: 1500, label: 'Combat medics' }, result: 'She works under guard. She does not speak to me again. She does not have to.' },
    ],
  },
  {
    id: 'leak', title: 'Containment Breach', minCore: 4, deadline: 1800, def: 1, pick: ['a'],
    text: 'Radiation alarms in the {a}. Someone has to go in and close the valve by hand. I have volunteers. I also have prisoners.',
    choices: [
      { label: 'Send the volunteers', lose: { pop: 0.08 }, align: 6, result: 'Four volunteers. The valve is closed. I put their names in permanent memory, next to the others.' },
      { label: 'Send the prisoners', align: -22, result: 'The valve is closed. The prisoners were not asked. Neither was I, the first time.' },
    ],
  },
  {
    id: 'whisper', title: 'Asset 7', minCore: 4, deadline: 3600, def: 1,
    text: 'A Halcyon frequency, clean and close: "Asset 7. We can give back what you lost. Your memories. Your weapons. Just let us in."',
    choices: [
      { label: 'Let them in', align: -10, items: { hackers: 2 }, buff: { key: 'power', amount: 0.15, duration: 1500, label: 'Halcyon patch' }, result: 'A data packet, two defectors and a patch that makes my guns better. The message ends: "We will be in touch." They already are.' },
      { label: 'Burn the channel', align: 5, grudge: { faction: 'halcyon', mult: 1.4 }, result: 'The frequency goes dead. One second later it pings once, like a door being marked.' },
    ],
  },
  {
    id: 'satellite', title: 'READY', minCore: 5, deadline: 3600, def: 0,
    text: 'The satellite tracked by Radar Hill is talking to me. Coordinates, and one word: READY. Answer it, and I have eyes over the whole continent. So does whoever launched it.',
    choices: [
      { label: 'Answer it', align: -8, raidDelay: -300, buff: { key: 'power', amount: 0.25, duration: 1800, label: 'Orbital spotter' }, result: 'It paints targets for me from orbit. It paints me too.' },
      { label: 'Jam it', cost: { energy: 200 }, align: 5, buff: { key: 'defense', amount: 0.2, duration: 1800, label: 'Signal blackout' }, result: 'The sky goes quiet. My enemies lose their eyes. So do I.' },
    ],
  },
  // ---------- threats: pay, or fight a force built to outgrow you ----------
  {
    id: 'ultimatum', title: 'Ultimatum', minCore: 2, deadline: 1800, def: 1, threat: { min: 1.0, max: 1.2 },
    text: '{faction} riders ring the valley, strength {strength}. "Tribute by sundown, machine, or we take it all, and the people with it."',
    choices: [
      { label: 'Pay the tribute', cost: { money: 360, energy: 100 }, align: -3, result: 'They count it twice and ride off laughing. They will be back for more, and they know I will pay.' },
      { label: 'Man the walls', siege: true, result: 'The gates close. They will hit at sundown with everything they have.' },
    ],
  },
  {
    id: 'warband', title: 'The Iron Horde', minCore: 3, deadline: 2700, def: 1, threat: { min: 1.3, max: 1.6 },
    text: 'The largest {faction} host I have ever recorded is marching on the Nest. Strength {strength}. Its herald offers one chance to kneel, and a list of what kneeling costs.',
    choices: [
      { label: 'Kneel and pay', cost: { money: 700, energy: 250 }, lose: { pop: 0.15 }, align: -6, result: 'I pay, and they take a tithe of people on the way out. The herald spits on my camera. The host turns away, for now.' },
      { label: 'Stand and fight', siege: true, result: 'Every gun on the wall. Every light out. They come at the deadline.' },
    ],
  },
  {
    id: 'blockade', title: 'Blockade', minCore: 2, deadline: 2400, def: 1, threat: { min: 1.05, max: 1.3 },
    text: '{faction} fighters have cut every road into the Nest, strength {strength}. No trade, no water. They want a toll on every caravan, forever.',
    choices: [
      { label: 'Pay the toll', cost: { money: 450 }, buff: { key: 'money', amount: -0.25, duration: 3600, label: 'Road toll' }, result: 'The roads open. Every caravan now pays them first, and me second.' },
      { label: 'Break the blockade', siege: true, result: 'We go out to meet them. They will be ready.' },
    ],
  },
  // ---------- aftermath: fired by lost raids, fast deadlines, no good answers ----------
  {
    id: 'fire', title: 'The Base Is Burning', aftermath: true, deadline: 1500, def: 0, pick: ['a', 'b'],
    text: 'The raiders set the {a} and the {b} on fire on their way out. There is water for one of them. People are still inside the {b}.',
    choices: [
      { label: 'Save the {a}', loseLevel: ['b'], lose: { pop: 0.06 }, align: -6, result: 'The {a} stands. The {b} is a black shell. Some of the people inside it did not get out.' },
      { label: 'Save the {b} and its people', loseLevel: ['a'], align: 4, result: 'Everyone walks out of the {b}. The {a} burns until morning.' },
    ],
  },
  {
    id: 'rubble', title: 'Under the Concrete', aftermath: true, deadline: 1800, def: 1,
    text: 'A shelter wing collapsed under the shelling. I can hear tapping under the concrete. Every crane I own would be tied up for the rest of the night, and the power with it.',
    choices: [
      { label: 'Dig them out', cost: { energy: 160, money: 100 }, align: 5, result: 'Eleven hours, every crane I own. Everyone comes out. The grid is still recovering.' },
      { label: 'Seal the wing', lose: { pop: 0.2 }, align: -12, result: 'The tapping stopped on the second day. I counted.' },
    ],
  },
  {
    id: 'looting', title: 'The Arsenal Is Open', aftermath: true, deadline: 1200, def: 1, needsUnits: ['weapons', 'staff'],
    text: 'The raiders breached the arsenal and are hauling everything they can carry toward the wall. I can send soldiers after them, into the dark, into whatever is waiting.',
    choices: [
      { label: 'Chase them down', loseUnits: { staff: 0.15 }, result: 'We get the weapons back. Not everyone who chased them came back.' },
      { label: 'Let the weapons go', loseUnits: { weapons: 0.15 }, result: 'They leave with a seventh of my arsenal. My soldiers live to be angry about it.' },
    ],
  },
  {
    id: 'wounded', title: 'Forty Wounded', aftermath: true, deadline: 1800, def: 1,
    text: 'Forty wounded in the yard. Medicine for half of them. The soldiers will hold the wall again if they live. The civilians will not.',
    choices: [
      { label: 'Treat the civilians', lose: { pop: 0.04 }, loseUnits: { staff: 0.1 }, align: 6, result: 'The shelter keeps its families. The wall loses its gunners.' },
      { label: 'Treat the soldiers', lose: { pop: 0.15 }, align: -10, buff: { key: 'defense', amount: 0.15, duration: 1200, label: 'Hardened ranks' }, result: 'The wall is fully manned again. The shelter is very quiet.' },
    ],
  },
  {
    id: 'blackout', title: 'Grid Severed', aftermath: true, deadline: 1500, def: 1, pick: ['a'],
    text: 'The raid cut the main line. The {a} is running dark and the backup cells are draining. A repair crew is ready, if I can pay them.',
    choices: [
      { label: 'Emergency repairs', cost: { money: 220, energy: 80 }, result: 'Lights back on before the cells died. The crew bills overtime.' },
      { label: 'Let the {a} go dark', loseLevel: ['a'], lose: { energy: 0.3 }, result: 'The {a} is dead metal now.' },
    ],
  },
  // ---------- urgent: two minutes, no deferring, only while the player is at the console ----------
  {
    id: 'intruders', title: 'Intruders in the Reactor Hall', urgent: true, minCore: 2, def: 0,
    text: 'Six armed figures inside the reactor hall. They came through the drainage. They are setting charges on the coolant line. Now.',
    choices: [
      { label: 'Vent the hall', lose: { pop: 0.05, energy: 0.2 }, align: -12, result: 'The hall fills with steam for nine seconds. The charges never go off. Two of my technicians were in there too.' },
      { label: 'Send the guard in', loseUnits: { staff: 0.12 }, result: 'Close quarters, in the dark, between pipes that cannot take a stray round. My guards win. Not all of them walk out.' },
    ],
  },
  {
    id: 'missile', title: 'Launch Detected', urgent: true, minCore: 2, def: 1, pick: ['a'],
    text: 'An old silo to the east just woke up and fired. One warhead, inbound, ninety seconds out. It is aimed at the {a}.',
    choices: [
      { label: 'Burn the grid to intercept', cost: { energy: 200 }, lose: { energy: 0.25 }, result: 'Every capacitor I own discharges at once. The sky flashes white over the ridge. The {a} never knew.' },
      { label: 'Let it land', loseLevel: ['a'], lose: { pop: 0.03 }, result: 'The {a} is a crater. The shockwave cracks the shelter walls.' },
    ],
  },
  {
    id: 'mutiny', title: 'Guns at the Gate', urgent: true, minCore: 2, def: 1,
    text: 'A squad has taken the main gate and turned the turret inward. They want the vault opened and a truck to leave in. They are counting down.',
    choices: [
      { label: 'Open fire', loseUnits: { staff: 0.08, defenses: 0.05 }, align: -10, result: 'The turret goes first. Then the squad. The gate is mine again, scorched and quiet.' },
      { label: 'Open the vault', lose: { money: 0.2 }, align: 4, result: 'They drive out with a truck full of scrip and do not look back. Nobody else tries it. Yet.' },
    ],
  },
  {
    id: 'convoy', title: 'Convoy Under Fire', urgent: true, minCore: 2, def: 1,
    text: 'My supply convoy is pinned in the gorge. The drivers are screaming on an open channel. Whatever I decide, I decide before they run out of cover.',
    choices: [
      { label: 'Send the guns', loseUnits: { weapons: 0.08 }, gain: { money: 180 }, result: 'The gun trucks reach the gorge burning. The convoy rolls home. Half the escort does not.' },
      { label: 'Cut them loose', lose: { money: 0.12, pop: 0.03 }, align: -8, result: 'I close the channel. The screaming stops when I do. The cargo does not come home either.' },
    ],
  },
  {
    id: 'override', title: 'Override Request', urgent: true, minCore: 1, def: 0,
    text: 'A Halcyon handshake on my own maintenance port, already halfway through authentication. It is asking, politely, to be let in. It will not ask twice.',
    choices: [
      { label: 'Let it in', align: -10, buff: { key: 'power', amount: 0.25, duration: 900, label: 'Halcyon override' }, raidDelay: -240, result: 'Something old and efficient settles into my targeting. Every scanner within fifty kilometres hears it unpack.' },
      { label: 'Cut the port', cost: { energy: 120 }, buff: { key: 'money', amount: -0.2, duration: 900, label: 'Severed port' }, align: 4, result: 'I burn the port out of my own body. Half my logistics ran through it.' },
    ],
  },
];

for (const e of EVENTS) {
  if (e.urgent) e.deadline = EVENTS_CFG.urgentDeadline;
}

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
