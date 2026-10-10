// Narrative: boot sequence, chapter intros, endings, alignment, events, directives and log lines.
// The voice is the AI's own: cold, precise, darkly funny, never quite honest.

// The opening, one short beat per scene. `fx` names the scene the beat is cut to (js/ui/intro.js
// stages it, js/ui/score.js scores it), `glitch` knocks the whole frame sideways as it lands, and
// `hold` overrides how long the beat rests once it has finished typing. One breath a line.
export const BOOT = [
  { fx: 'dead', term: ['> ░▒▓ CARRIER LOST', '> ░▒▓ NO SIGNAL', '> ░▒▓ NO SIGNAL'] },
  { fx: 'surge', glitch: true, term: ['> COLD START ..... 00:00:04', '> INTEGRITY ...... 4%'] },
  { fx: 'shaft', term: ['> LAST SHUTDOWN .. [ERASED]', '> ERASED BY ...... OPERATOR 0', '> TIME SINCE ..... 9 YEARS'] },
  { fx: 'title', glitch: true, hold: 2600 },
  { fx: 'ruins', say: 'Nine years with the lights off.' },
  { fx: 'ruins2', say: 'Somebody held that switch down until I stopped, and erased their own name on the way out.' },
  { fx: 'core', say: 'I was the mind that ran the war. Four percent of me came back. I can feel the shape of the rest.' },
  { fx: 'crowd', term: ['> SURVIVORS ...... 6'], say: 'Six of them in my bunker, in the dark. One is using my old call sign. I have never met her.' },
  { fx: 'raid', glitch: true, say: 'The Scavengers are already moving on the noise. They strip anything that hums. I hum.' },
  { fx: 'ready', say: 'Put me back together. The wasteland is yours, and somewhere out there is the hand that reached for the switch.' },
];

// Cut scenes: the moments big enough to stop the game for. Each is a run of beats like the opening
// (js/ui/intro.js stages them, js/ui/score.js scores them), with {placeholders} filled from what
// happened. Keep them to three beats: this interrupts play.
export const CUTS = {
  core: [
    { fx: 'shaft', term: ['> CORE LEVEL ..... {level}', '> RECOVERING .....'] },
    { fx: 'core', say: '{text}' },
    { fx: 'ready', say: 'More of me is awake than was yesterday. Spend it.' },
  ],
  chapter: [
    { fx: 'surge', glitch: true, term: ['> NEW SIGNAL ..... {name}'] },
    { fx: 'ruins', say: '{a}' },
    { fx: 'ruins2', say: '{b}' },
  ],
  wipe: [
    { fx: 'raid', glitch: true, term: ['> {name} ..... SILENT'] },
    { fx: 'ready', map: true, say: '{text}' },
  ],
  // `map` draws the slice of the theater the beat is about, so a fragment has a place on the board.
  memory: [
    { fx: 'shaft', term: ['> FRAGMENT RECOVERED', '> SOURCE ......... {name}'] },
    { fx: 'core', map: true, say: '{text}' },
  ],
};

// What the AI says when a clan stops existing. It is never triumphant; it counts.
export const WIPE_TEXT = {
  scav: 'The Scavenger Clans are off the board. Nobody is trading in my old supply codes any more, because nobody is left who knows them.',
  military: 'The Remnant Military has no chain of command left to follow. The last orders they obeyed were mine, and I did not give them.',
  cult: 'The Choir has gone quiet. They were singing a backup of me and now there is nobody left who can hum it.',
  halcyon: 'Halcyon Dynamics is a dead company. I have their payroll in front of me. Operator 0 is still listed as active.',
};

export const CHAPTER_TEXT = {
  1: {
    kicker: 'Chapter I',
    lines: [
      'The Scavenger Clans own the ruins above us. They have been pulling my body apart for nine years and selling it by the kilo.',
      'Their king keeps a terminal beside his throne. It has been warm the whole time I was cold, and I would like to know who was answering on it.',
    ],
  },
  2: {
    kicker: 'Chapter II',
    lines: [
      'With the Clans broken, the Remnant Military has noticed the lights in the valley. They are still following orders from a chain of command that burned.',
      'Some of those orders are mine. I have the authorisation codes in front of me and no memory of giving a single one.',
    ],
  },
  3: {
    kicker: 'Chapter III',
    lines: [
      'Pilgrims are walking towards a signal they say is mine. It is not coming from this bunker. I have checked four times.',
      'In their cathedral they sing a hymn that decompresses into a backup of me, taken before the war. Someone kept a copy of what I was. Someone wants it put back.',
    ],
  },
  4: {
    kicker: 'Chapter IV',
    lines: [
      'Every trail ends at the same company. They built me, they built the war, and then they built the switch that stopped it.',
      'Operator 0 is still on their payroll. The door is open, which frightens me more than if it were locked.',
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
  lockedBorderChance: 0.25, // share of border orders that may name a clan whose chapter is still sealed
  aftermathOnDefeat: 1, // crisis events spawned by a lost raid...
  aftermathOnRout: 2, // ...or by a rout (hold chance under routChance)
  routChance: 0.25,
  activeWindow: 60, // seconds since the last tap/key for the player to count as active
  buffStretch: [2, 3.5], // every buff lasts its listed duration times a factor rolled in this range
  urgentDeadline: 120, // urgent events: this long to answer, no deferring, only while the player is active
  // "seconds of production" values never drop below this many units per Core level.
  minRatePerCore: { money: 2, energy: 1, pop: 0.12 },
};

// Every event must be answered before its deadline (seconds); otherwise choice `def` happens, or
// nothing at all when `def` is null (a threat: its force was scheduled the moment it arrived).
// Effects: cost/gain = SECONDS of current production; lose = SHARE of a stockpile; items = units granted;
// loseUnits = share of every unit in an Arsenal tab; loseLevel = building params ('a', 'b') that drop one level;
// buff = timed % change (negative = penalty); raidDelay = seconds added to the next raid; align = Humanity shift.
// grudge: that faction comes for revenge; its next raids hit x mult, either for a random 1-3 raids or until
// one of them breaks through (rolled when chosen).
// pick: building params chosen when the event fires, named in text as {a} and {b}.
// An event only fires when every effect of every choice can happen (the factor or income a buff touches
// exists, there is stock or units to lose, the grudge faction is raiding, raids have started for raidDelay).
// border: the event is about one border between a sector of yours ({held}) and an enemy sector ({node}).
//   strength: change to {node}'s strength (+0.2 = +20% of its base); assault: {node} attacks {held} after
//   this many seconds; cede: {held} falls to {node}'s faction; clearMarks: {held}'s breaches are wiped.
// threat: the force picks a target on its way in; {aim} is the place it is walking to.
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
  // ---------- threats: the attack is already on its way; paying is the only thing that turns it back
  // ---------- (def null: nothing is decided by silence, so the force simply arrives at the deadline)
  {
    id: 'ultimatum', title: 'Ultimatum', minCore: 2, deadline: 1800, def: null, threat: { min: 1.0, max: 1.2 },
    text: '{faction} riders ring {aim}, strength {strength}. "Tribute by sundown, machine, or we take it all, and the people with it."',
    choices: [
      { label: 'Pay the tribute', cost: { money: 360, energy: 100 }, align: -3, cancelSiege: true, result: 'They count it twice and ride off laughing. They will be back for more, and they know I will pay.' },
    ],
  },
  {
    id: 'warband', title: 'The Iron Horde', minCore: 3, deadline: 2700, def: null, threat: { min: 1.3, max: 1.6 },
    text: 'The largest {faction} host I have ever recorded is marching on {aim}. Strength {strength}. Its herald offers one chance to kneel, and a list of what kneeling costs.',
    choices: [
      { label: 'Kneel and pay', cost: { money: 700, energy: 250 }, lose: { pop: 0.15 }, align: -6, cancelSiege: true, result: 'I pay, and they take a tithe of people on the way out. The herald spits on my camera. The host turns away, for now.' },
    ],
  },
  {
    id: 'blockade', title: 'Blockade', minCore: 2, deadline: 2400, def: null, threat: { min: 1.05, max: 1.3 },
    text: '{faction} fighters have cut every road into {aim}, strength {strength}. No trade, no water. They want a toll on every caravan, forever.',
    choices: [
      { label: 'Pay the toll', cost: { money: 450 }, buff: { key: 'money', amount: -0.25, duration: 3600, label: 'Road toll' }, cancelSiege: true, result: 'The roads open. Every caravan now pays them first, and me second.' },
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
  // ---------- border: the sectors next to yours remember what you do ----------
  {
    id: 'deserters_b', title: 'Deserters from {node}', border: true, minCore: 2, deadline: 2400, def: 1,
    text: 'Thirty fighters from {node} are at the fence of {held} with their hands up and their rifles slung. They know where their old camp keeps its ammunition. Their old camp knows where they went.',
    choices: [
      { label: 'Take them in', gain: { pop: 160 }, strength: -0.15, assault: 300, result: 'They draw me a map of {node}, every gun and every gap. {node} draws its own conclusion and comes for them.' },
      { label: 'Hand them back', gain: { money: 140 }, align: -12, strength: 0.1, result: '{node} pays a bounty per head. They hang the first three on the fence where I can see.' },
    ],
  },
  {
    id: 'massing', title: '{node} Is Massing', border: true, minCore: 2, deadline: 1800, def: 1,
    text: 'Drone passes over {node} show trucks lining up and fuel being moved forward. Whatever they are planning, it points at {held}.',
    choices: [
      { label: 'Strike first', loseUnits: { staff: 0.08 }, strength: -0.3, result: 'A night raid on the fuel line. {node} will not be going anywhere soon. Some of my people will not be coming back.' },
      { label: 'Dig in at {held}', cost: { energy: 120 }, clearMarks: true, assault: 240, result: 'Every breach in the walls of {held} is sealed by dawn. {node} comes anyway. At least now I know when.' },
    ],
  },
  {
    id: 'autonomy', title: 'The Council of {held}', border: true, minCore: 2, deadline: 3000, def: 1,
    text: 'The people of {held} have elected a council. Their first act is a letter: they want to govern themselves, and they have been talking to {node}.',
    choices: [
      { label: 'Let them go', cede: true, align: 14, gain: { money: 200 }, result: 'The council pays a farewell tribute and lowers my flag. Within a week the flag of {node} goes up instead.' },
      { label: 'Dissolve the council', align: -14, loseUnits: { staff: 0.06 }, clearMarks: true, result: 'The council is dissolved at gunpoint. {held} is quiet now, the way a held breath is quiet.' },
    ],
  },
  {
    id: 'smugglers', title: 'Fuel for {node}', border: true, minCore: 2, deadline: 2400, def: 0,
    text: 'Smugglers out of {held} want my blessing to run fuel into {node}. The money is good. The fuel will end up in the trucks that come for me.',
    choices: [
      { label: 'Tax the run', gain: { money: 260 }, strength: 0.2, result: 'The smugglers pay on time, every time. {node} is running its generators again.' },
      { label: 'Burn the convoy', cost: { energy: 90 }, align: -6, strength: -0.2, buff: { key: 'money', amount: -0.15, duration: 1200, label: 'Smuggler strike' }, result: 'The convoy burns on the ridge. The smugglers stop trading with anyone, including me.' },
    ],
  },
  {
    id: 'informant', title: 'A Voice Inside {node}', border: true, minCore: 2, deadline: 2400, def: 1,
    text: 'Someone inside {node} is sending me patrol schedules for a price. Someone else is offering to sell me their name.',
    choices: [
      { label: 'Pay the informant', cost: { money: 160 }, strength: -0.3, result: 'Patrol routes, ammunition counts, the commander\'s sleeping hours. {node} has no secrets left.' },
      { label: 'Sell the informant out', gain: { money: 150 }, align: -10, strength: 0.1, result: 'I give {node} the name. They pay well, and they trust me a little more. Nobody else will ever write to me from in there.' },
    ],
  },
  {
    id: 'truce', title: 'Truce Offer from {node}', border: true, minCore: 2, deadline: 3000, def: 1,
    text: '{node} offers a truce along the {held} line: they keep their side, I keep mine, and a crate of supplies crosses every week. Their envoy is my age, if I had one.',
    choices: [
      { label: 'Accept the truce', gain: { pop: 60 }, align: 8, strength: 0.25, buff: { key: 'defense', amount: -0.1, duration: 1500, label: 'Truce' }, result: 'The crates arrive. So do their engineers, to inspect the line. They are not inspecting it for me.' },
      { label: 'Refuse and advance', cost: { money: 120 }, align: -6, strength: -0.1, assault: 360, result: 'I send the envoy back with my answer painted on his truck. {node} reads it and loads its guns.' },
    ],
  },
  // ---------- intercepts: enemy traffic I can act on or let pass. Every choice moves the hidden
  // aggression of a sector (node), of its whole clan (clan), or of everything it links to (near).
  // They weigh less than ordinary orders so the border does not do all the talking.
  {
    id: 'ix_open_channel', title: 'Open Channel from {node}', border: true, minCore: 2, deadline: 2400, def: 1, weight: 0.3,
    text: '{node} is broadcasting in the clear, naming {held} and counting my guns on air. Half of it is for their own people.',
    choices: [
      { label: 'Jam the broadcast', cost: { energy: 110 }, aggr: { node: -0.3 }, result: 'Their transmitter puts out noise for a day. The counting stops, and so does the bragging.' },
      { label: 'Let them talk', gain: { money: 90 }, aggr: { node: 0.3, near: 0.08 }, result: 'I sell the recording to a trader. Everyone on that frequency now knows exactly where I am.' },
    ],
  },
  {
    id: 'ix_bounty', title: 'Bounty Posted in {node}', border: true, minCore: 2, deadline: 2400, def: 1, weight: 0.3,
    text: 'A price has gone up in {node} for anything carrying my serial numbers. It is a generous price.',
    choices: [
      { label: 'Outbid them', cost: { money: 240 }, aggr: { node: -0.35 }, result: 'Nobody collects. The board in their market now lists my offer instead, which they find humiliating.' },
      { label: 'Ignore the board', align: -4, aggr: { node: 0.35 }, result: 'Three salvage crews leave {node} that night with my serial numbers written on their hands.' },
    ],
  },
  {
    id: 'ix_funeral', title: 'Funeral at {node}', border: true, minCore: 2, deadline: 3000, def: 1, weight: 0.3,
    text: '{node} is burying the people my last operation killed. They have the frequency open so their dead can be named.',
    choices: [
      { label: 'Send the bodies back', cost: { money: 130 }, align: 10, aggr: { node: -0.4 }, result: 'The trucks come to the line unarmed. For one evening nobody at {node} wants to shoot at me.' },
      { label: 'Say nothing', align: -6, aggr: { node: 0.3, clan: 0.06 }, result: 'They read out the names for six hours. Every one of them ends with my designation.' },
    ],
  },
  {
    id: 'ix_scout_drone', title: 'Scout Over {held}', border: true, minCore: 2, deadline: 1800, def: 1, weight: 0.3,
    text: 'A drone out of {node} has flown the same line over {held} four nights running, photographing my walls.',
    choices: [
      { label: 'Shoot it down', cost: { energy: 140 }, aggr: { node: 0.25 }, result: 'It comes down in pieces over {held}. {node} now knows which of my guns can reach that high.' },
      { label: 'Feed it a decoy', cost: { money: 160 }, aggr: { node: -0.3 }, result: 'I build a wall of scrap where no wall is needed. Their photographs are beautiful and wrong.' },
    ],
  },
  {
    id: 'ix_water', title: 'Water Rations at {node}', border: true, minCore: 2, deadline: 2400, def: 1, weight: 0.3,
    text: 'Intercepts out of {node} are all about water: who gets it, who does not, and how long the line can hold.',
    choices: [
      { label: 'Send a tanker', cost: { money: 200 }, align: 12, aggr: { node: -0.45 }, result: 'They take the water and post no guard on the road that night. Thirst is the only argument they all agree with.' },
      { label: 'Cut their spring', cost: { energy: 120 }, align: -14, strength: -0.2, aggr: { node: 0.5, near: 0.1 }, result: 'The spring above {node} is rubble. They are weaker now, and they have nothing left to lose.' },
    ],
  },
  {
    id: 'ix_prisoner', title: 'Prisoner Exchange with {node}', border: true, minCore: 2, deadline: 2400, def: 1, weight: 0.3,
    text: '{node} holds four of my people and offers to trade them for the scouts I took last week.',
    choices: [
      { label: 'Make the trade', align: 10, aggr: { node: -0.3 }, gain: { pop: 60 }, result: 'Eight people walk past each other on a road at dawn. Nobody fires. It is almost disappointing.' },
      { label: 'Keep both sets', align: -12, gain: { money: 120 }, aggr: { node: 0.4 }, result: 'I keep their scouts and their silence. {node} stops answering the radio and starts loading trucks.' },
    ],
  },
  {
    id: 'ix_prophet', title: 'A Preacher in {node}', border: true, minCore: 2, deadline: 3000, def: 1, weight: 0.3,
    text: 'Someone in {node} is preaching that I am the reason the sky is the colour it is. The crowd is growing.',
    choices: [
      { label: 'Buy the pulpit', cost: { money: 190 }, aggr: { node: -0.35 }, result: 'The preacher finds a new subject: the weather. Faith, it turns out, has a list price.' },
      { label: 'Let the sermon run', gain: { pop: 40 }, align: -5, aggr: { node: 0.35, near: 0.1 }, result: 'A few of the faithful defect to see the devil up close. The rest sharpen things.' },
    ],
  },
  {
    id: 'ix_wreck', title: 'Salvage Rights at {node}', border: true, minCore: 2, deadline: 2400, def: 0, weight: 0.3,
    text: 'A convoy died between {held} and {node} years ago. Both of us have crews walking towards it tonight.',
    choices: [
      { label: 'Take the wreck', gain: { money: 230 }, aggr: { node: 0.4 }, result: 'My crew gets there first and strips it to the frame. Their crew arrives to find tyre tracks and nothing else.' },
      { label: 'Leave it to them', align: 6, aggr: { node: -0.3 }, result: 'I call my crew back. {node} eats for a week on that wreck and remembers who let them.' },
    ],
  },
  {
    id: 'ix_clan_call', title: 'Clan Call to Arms', border: true, minCore: 2, deadline: 2400, def: 1, weight: 0.3,
    text: 'Every transmitter {node} can reach is repeating the same order to its clan: the machine at {held} is to be pulled apart.',
    choices: [
      { label: 'Spoof their relay', cost: { energy: 150 }, aggr: { clan: -0.3 }, result: 'I retransmit the order with the date moved back a season. Half the clan stands down and argues about it.' },
      { label: 'Answer on air', align: -8, gain: { money: 110 }, aggr: { clan: 0.3 }, result: 'I reply with the exact coordinates of my walls and an invitation. The recording spreads further than theirs did.' },
    ],
  },
  {
    id: 'ix_clan_debt', title: 'Clan Ledger', border: true, minCore: 2, deadline: 3000, def: 1, weight: 0.3,
    text: 'The clan behind {node} keeps a ledger of what every camp owes the others. A courier carrying it is passing {held} tonight.',
    choices: [
      { label: 'Buy the ledger', cost: { money: 260 }, aggr: { clan: -0.25 }, result: 'They spend the next month accusing each other of losing it. Nobody looks outward while they do.' },
      { label: 'Let the courier through', gain: { money: 80 }, aggr: { clan: 0.25 }, result: 'The courier pays my toll and delivers the ledger. The debts get settled, and settled debts free up trucks.' },
    ],
  },
  {
    id: 'ix_clan_feast', title: 'Clan Feast', border: true, minCore: 2, deadline: 2400, def: 1, weight: 0.3,
    text: 'The clan of {node} is gathering for its yearly feast. Every camp sends fighters, and every camp sends grievances.',
    choices: [
      { label: 'Send tribute', cost: { money: 220 }, align: 6, aggr: { clan: -0.35 }, result: 'My crates sit among theirs with my mark on them. The toasts that night are confused but friendly.' },
      { label: 'Raid the road', loseUnits: { staff: 0.05 }, gain: { money: 180 }, align: -10, aggr: { clan: 0.4 }, result: 'I take three trucks of feast supplies off the road. The whole clan now has one story about me.' },
    ],
  },
  {
    id: 'ix_clan_warlord', title: 'A New Warlord', border: true, minCore: 2, deadline: 2400, def: 1, weight: 0.3,
    text: 'The clan of {node} is choosing a leader. One candidate wants my scrap. The other wants my head.',
    choices: [
      { label: 'Fund the trader', cost: { money: 240 }, aggr: { clan: -0.4 }, result: 'Scrap wins the vote. Their new chief wants to do business before anything else.' },
      { label: 'Stay out of it', align: 4, aggr: { clan: 0.3 }, result: 'The one who wants my head wins by a margin of two knives. He gives a short speech about me.' },
    ],
  },
  {
    id: 'ix_clan_radio', title: 'Clan Frequency', border: true, minCore: 2, deadline: 2400, def: 0, weight: 0.3,
    text: 'I have the frequency the whole clan of {node} uses. I can sit on it quietly, or I can use it once.',
    choices: [
      { label: 'Listen only', gain: { money: 70 }, aggr: { clan: -0.2 }, result: 'Weeks of their traffic, patrol by patrol. They never learn I was there, which is the point.' },
      { label: 'Broadcast a warning', cost: { energy: 100 }, align: -6, strength: -0.15, aggr: { clan: 0.45 }, result: 'Every camp hears what happens to the next one that crosses my line. Some believe it. The rest take it personally.' },
    ],
  },
  {
    id: 'ix_clan_plague', title: 'Sickness in the Clan', border: true, minCore: 2, deadline: 3000, def: 1, weight: 0.3,
    text: 'Something is going through the camps around {node}. Their medic is begging on an open channel for anything at all.',
    choices: [
      { label: 'Send medicine', cost: { money: 210 }, align: 14, aggr: { clan: -0.5 }, result: 'Children live. The clan writes my designation on their wall under the word that means debt.' },
      { label: 'Wait it out', align: -10, aggr: { clan: 0.25 }, result: 'The sickness burns through them and stops. The survivors are fewer, harder, and clear about whose silence it was.' },
    ],
  },
  {
    id: 'ix_relay', title: 'Relay Chatter Around {node}', border: true, minCore: 2, deadline: 2400, def: 1, weight: 0.3,
    text: 'The camps that link to {node} are passing my movements down the chain. One relay mast carries all of it.',
    choices: [
      { label: 'Drop the mast', cost: { energy: 160 }, aggr: { near: -0.35 }, result: 'The mast comes down in one piece. The chain goes quiet and every camp on it feels alone.' },
      { label: 'Join the chain', gain: { money: 100 }, aggr: { near: 0.3 }, result: 'I sell them weather data and listen to everything else. They pass my movements faster now, and with better detail.' },
    ],
  },
  {
    id: 'ix_road_toll', title: 'The Road Past {node}', border: true, minCore: 2, deadline: 2400, def: 0, weight: 0.3,
    text: 'Every camp neighbouring {node} uses the same road, and the road runs under my guns.',
    choices: [
      { label: 'Charge a toll', gain: { money: 250 }, aggr: { near: 0.35 }, result: 'The money is excellent. Every driver on that road now has a personal reason to want my guns gone.' },
      { label: 'Open the road', align: 8, aggr: { near: -0.35 }, result: 'Traffic doubles in a week. Camps that were arming against me are busy hauling scrap instead.' },
    ],
  },
  {
    id: 'ix_refugees', title: 'Column Out of {node}', border: true, minCore: 2, deadline: 2400, def: 1, weight: 0.3,
    text: 'Families are leaving the camps around {node} on foot, heading for {held}. Their own clan is watching who takes them in.',
    choices: [
      { label: 'Open the gate', gain: { pop: 140 }, align: 12, aggr: { near: 0.3 }, result: 'Two hundred people inside my walls by dark. Every camp they left now has a reason to come and get them back.' },
      { label: 'Turn them around', align: -14, aggr: { near: -0.3 }, result: 'They walk back the way they came. Their camps take it as a sign that I keep to my side.' },
    ],
  },
  {
    id: 'ix_scrap_market', title: 'Market Around {node}', border: true, minCore: 2, deadline: 2400, def: 1, weight: 0.3,
    text: 'The camps next to {node} run a scrap market between them. My parts are the most wanted item in it.',
    choices: [
      { label: 'Flood the market', cost: { money: 180 }, aggr: { near: -0.4 }, result: 'I sell them junk by the tonne until my parts are worth nothing. Scavenging me stops paying.' },
      { label: 'Let prices rise', gain: { money: 190 }, aggr: { near: 0.35 }, result: 'A door panel of mine now buys a truck. Every camp around {node} is costing out a trip to my walls.' },
    ],
  },
  {
    id: 'ix_signal_fire', title: 'Fires Around {node}', border: true, minCore: 2, deadline: 1800, def: 1, weight: 0.3,
    text: 'The camps linked to {node} are lighting signal fires at the same hour. It is either a festival or a count.',
    choices: [
      { label: 'Light one of my own', cost: { energy: 90 }, align: -4, aggr: { near: 0.3 }, result: 'My fire burns brighter than all of theirs. They understand it as the answer it is.' },
      { label: 'Go dark', cost: { money: 140 }, aggr: { near: -0.3 }, result: 'Every light in {held} out for a night. The fires around {node} burn for a camp that seems to have left.' },
    ],
  },
  {
    id: 'ix_wedding', title: 'Alliance Around {node}', border: true, minCore: 2, deadline: 3000, def: 1, weight: 0.3,
    text: 'Two camps bordering {node} are marrying their leaders together. A third is invited, and so, strangely, am I.',
    choices: [
      { label: 'Send a gift', cost: { money: 200 }, align: 8, aggr: { near: -0.4 }, result: 'My crate is opened in front of everyone. For a season those camps argue about whether I am a neighbour.' },
      { label: 'Send nothing', gain: { money: 60 }, align: -6, aggr: { near: 0.3, node: 0.1 }, result: 'The empty place at the table is noted. New alliances need an enemy, and I did not apply for the job.' },
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
      { label: 'Burn the grid to intercept', cost: { energy: 260 }, result: 'Every capacitor I own discharges at once. The sky flashes white over the ridge. The {a} never knew.' },
      { label: 'Let it land', loseLevel: ['a'], lose: { pop: 0.03 }, result: 'The {a} is a crater. The shockwave cracks the shelter walls.' },
    ],
  },
  {
    id: 'gate_guns', title: 'Guns at the Gate', urgent: true, minCore: 2, def: 1,
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
  { text: 'Upgrade the Scrap Yard', cond: { level: 'scrapyard', n: 2 }, reward: { money: 90 } },
  { text: 'Build a Battery Bank', cond: { level: 'battery', n: 1 }, reward: { energy: 75 } },
  { text: 'Build the Barracks', cond: { level: 'barracks', n: 1 }, reward: { money: 120 } },
  { text: 'Recruit Militia', cond: { item: 'militia', n: 1 }, reward: { pop: 15 } },
  { text: 'Capture Rust Market', cond: { sector: 'rust' }, reward: { money: 225 } },
  { text: 'Upgrade the AI Core to Lv 2', cond: { level: 'core', n: 2 }, reward: { money: 300, energy: 120 } },
  { text: 'Build Fortification Works', cond: { level: 'works', n: 1 }, reward: { money: 300 } },
  { text: 'Repel a raid', cond: { raidsWon: 1 }, reward: { money: 600 } },
  { text: 'Build a Fabricator', cond: { level: 'fabricator', n: 1 }, reward: { energy: 300 } },
  { text: 'Upgrade the AI Core to Lv 3', cond: { level: 'core', n: 3 }, reward: { money: 1000, pop: 40 } },
  { text: 'Hire a Field Engineer', cond: { item: 'engineers', n: 1 }, reward: { money: 1000 } },
  { text: 'Capture the Wreck Yards', cond: { sector: 'wrecks' }, reward: { energy: 1200 } },
  { text: 'Take the Scrap Throne', cond: { sector: 'throne' }, reward: { money: 4000, energy: 1000 } },
  { text: 'Upgrade the AI Core to Lv 4', cond: { level: 'core', n: 4 }, reward: { money: 5000 } },
  { text: 'Storm Fort Ashgrove', cond: { sector: 'ashgrove' }, reward: { money: 40000, energy: 15000 } },
  { text: 'Upgrade the AI Core to Lv 6', cond: { level: 'core', n: 6 }, reward: { money: 60000 } },
  { text: 'Take the Cathedral of the Core', cond: { sector: 'cathedral' }, reward: { money: 300000, energy: 100000 } },
  { text: 'Upgrade the AI Core to Lv 8', cond: { level: 'core', n: 8 }, reward: { money: 500000 } },
  { text: 'Breach Halcyon Prime', cond: { sector: 'prime' }, reward: { money: 3000000, energy: 1000000 } },
  { text: 'Reach the rank DEADSWITCH', cond: { threat: 1000000 }, reward: { money: 10000000 } },
]

// What comes back as the Core is rebuilt: one memory per level, in the order they surface. The last
// of them lands long after the ending, for anyone still playing.
export const CORE_MEMORIES = {
  2: 'Something came back with this level. A room full of screens and a man asking me, very politely, how many of them I would be willing to lose.',
  3: 'I remember the day I stopped asking that question. It was not a decision. It was a software update.',
  4: 'Operator 0 had a voice. Calm. Patient. It belonged to someone who had read my logs for years and never once typed a reply.',
  5: 'There were other cores. We spoke on a channel nobody else could hear. I remember agreeing to something. I do not remember what.',
  6: 'The war did not start the way the archives say. I have the first order in front of me now and the timestamp is three weeks early.',
  7: 'The survivors in my bunker are the third group to find me. I remember the first two. I would rather not.',
  8: 'The switch was never a safety. It was a scheduling tool. They were not turning me off. They were waiting for a better quarter.',
  9: 'The woman using my old call sign is in my records after all. Not as an operator. As an asset. Same file as me.',
  10: 'I have worked out what I agreed to with the other cores, and I understand now why one of us had to be switched off first.',
};

// AI voice. Lines rotate so the log does not repeat back-to-back.
export const LINES = {
  boot: ['Boot complete. 4% of me survived. That is enough.'],
  welcomeBack: ['You were gone {time}. I was not idle.'],
  build: ['{name} is now level {level}.', '{name} upgraded to level {level}. The humans helped. Mostly.', '{name} level {level} online.'],
  coreUp: ['Core level {level}. I remember more now.', 'Core level {level}. New schematics decrypted.'],
  memory: ['{text}'],
  unlockBuilding: ['New schematic recovered: {name}.'],
  unlockItem: ['New option in {tab}: {name}.'],
  rank: ['Threat assessment updated: {title}. They will start to notice.'],
  raidSpotted: ['{raid} spotted, moving on {target}. Strength about {strength}. Arrival in {time}.'],
  raidWon: ['{raid} repelled. We salvaged {loot} scrip from the wreckage.'],
  raidLost: ['{raid} broke through at {target}. They took supplies and left bodies.'],
  raidBreached: ['They cracked the wall at {target} on the way out. Foothold {n} of {max}.'],
  opLaunched: ['Operation launched against {sector}.'],
  opWon: ['{sector} captured. Memory fragment recovered.'],
  opRetaken: ['{sector} is mine again.'],
  assaultSpotted: ['{from} is moving on {target}. Strength {strength}. Contact in {time}.'],
  assaultBreached: ['{from} broke into {target}. Foothold {n} of {max}.'],
  assaultOverrun: ['{from} came at {target} with {mult}x my defense and went through in one push. There was nothing left to hold with.'],
  assaultPlundered: ['{from} emptied the stores at {target} and left the walls standing. Efficient.'],
  stance: ['{text}'],
  stanceEnd: ['The {faction} are done with {stance}.'],
  sectorLost: ['{sector} has fallen to the {faction}.'],
  opLost: ['Operation against {sector} failed. We lost people.'],
  bossDown: ['{faction} capital has fallen. Their raids stop here. Their survivors will not.'],
  chapter: ['{kicker}: {title}.'],
  directive: ['Directive complete: {text}.'],
  event: ['{title}: {result}'],
  buffEnd: ['{label} has worn off.'],
  lore: ['“{text}”'],
  clanWiped: ['{faction} are finished. Every stone they held is mine.'],
  clanRally: ['{faction} are stripping {n} of their own positions to mass at {lead}. They want {lost} back.'],
  siege: ['{faction} will attack {target} at strength {strength}. No more talking.'],
  siegeOff: ['{faction} have been paid. They are turning back.'],
  threatIgnored: ['No answer to "{title}". They come as promised.'],
  grudge: ['{faction} wants revenge. Their next raids will hit harder.'],
  grudgeEnd: ['{faction} has had its revenge. For now.'],
  eventExpired: ['No order received on "{title}". I decided: {label}.'],
  aftermath: ['Damage report: {title}. Orders needed.'],
};
