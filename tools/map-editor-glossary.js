// Plain-language explanations for every knob the map editor exposes.
//
// The game's own UI states facts only and never explains design intent (see AGENTS.md). This file is
// the opposite on purpose: it is a tool for the person setting the numbers, so each entry says what
// the knob actually does, in what units, and what moving it feels like.
//
// Keys are the splice path: `BLOCK.key`, `BLOCK.key.sub`, and `BLOCK.stances.*` for anything inside
// a stance (the stance list is addressed by index at runtime, so its keys are matched by name).

/** What each top-level block of js/data/world.js is for. */
export const BLOCKS = {
  FACTIONS: 'The four clans (plus the Rival Cores). Identity, colour, and the mood their traits drift back to when nothing is happening.',
  CHAPTERS: 'Which clan belongs to which chapter, and the AI Core level that opens it.',
  SECTORS: 'The map itself. Edited on the map and in the sector card, not here.',
  MAP: 'Where the player starts, and how a chapter\'s front line starts leaking before the chapter formally opens.',
  OPS: 'Your attacks on them: odds, price, duration, loot and what a loss costs you.',
  WARN: 'How much notice you get before an attack lands.',
  RAIDS: 'Attacks on your stockpiles that come from a clan\'s territory rather than from one bordering sector.',
  TUTORIAL: 'The scripted opening: one soft first target, then one scripted counter-attack that teaches breaches.',
  NODES: 'Enemy sectors as living things: how they gain and lose strength, and how they assault your border.',
  RALLY: 'What a clan does after you take a sector from it: strip its other ground to make one push at taking it back.',
  AGGR: 'Aggression — hidden per-sector anger that decides which border the next assault comes from. The player never sees it.',
  CLANS: 'Clan psychology: four traits per faction, and the stance matrix those traits are read through.',
};

/** `what` is the mechanic; `feel` is what moving the number does to the game. */
export const GLOSSARY = {
  // ---------- MAP ----------
  'MAP.home': { what: 'The sector id the player starts holding. It can never be taken.' },
  'MAP.earlyLead': {
    what: 'How many AI Core levels BEFORE a chapter opens that its front line starts to leak.',
    feel: 'At 1, a chapter gated at Core 4 starts offering a few targets at Core 3. At 0 nothing leaks and chapters arrive one at a time.',
  },
  'MAP.earlyOpen': {
    what: 'How many of that clan\'s sectors leak at once — the weakest ones that touch ground you already hold.',
    feel: 'Higher means more than one clan to fight at a time, and more choice about where to push.',
  },
  'MAP.fragmentCut': {
    what: 'A memory fragment is worth a cut scene every this many captures.',
    feel: 'Lower means the story interrupts more often.',
  },

  // ---------- OPS ----------
  'OPS.winSharpness': {
    what: 'How decisively the stronger side wins. Odds are Power^k / (Power^k + Defense^k).',
    feel: 'k=1 and the odds track the raw ratio. k=4 and matching their defense is a coin flip, 1.5x their defense wins about 83%, and half their defense wins about 6%. Raising it makes being under-powered hopeless and being over-powered boring.',
  },
  'OPS.flankBonus': {
    what: 'Clan support. A sector gets up to this much extra defense while every linked sector of its own faction still stands, falling to zero as you take them.',
    feel: '0.75 means +75% defense at full support. It is what makes chewing into a heartland from the edge cheaper than diving at its middle.',
  },
  'OPS.costMoneyPerDefense': { what: 'Scrip an operation costs per point of the target\'s base defense, before BALANCE.priceMult.' },
  'OPS.costEnergyPerDefense': { what: 'Energy an operation costs per point of base defense, before BALANCE.priceMult.' },
  'OPS.timeBase': { what: 'Seconds every operation takes before defense is counted at all.' },
  'OPS.timePerSqrtDefense': {
    what: 'Extra seconds per square root of the target\'s defense.',
    feel: 'The square root is what stops a 300,000-defense sector taking a week. It also sets march times: an assault\'s warning includes WARN.travel x this sector\'s operation time.',
  },
  'OPS.loot': { what: 'Capture loot, paid the first time a sector falls to you. Each entry is [a, b, c] meaning a x D^b + c x D, where D is the sector\'s base defense.' },
  'OPS.loot.money': { what: '[a, b, c] -> a x D^b + c x D scrip, D = base defense.' },
  'OPS.loot.energy': { what: '[a, b, c] -> a x D^b + c x D energy, D = base defense.' },
  'OPS.loot.pop': { what: '[a, b, c] -> a x D^b + c x D people, D = base defense.' },
  'OPS.unitLoss': { what: 'What a failed operation kills. Share of each tab = base x (their defense / your power), never above cap. Each unit\'s own durability cuts its share again.' },
  'OPS.unitLoss.staff.base': { what: 'Troops killed by a failed operation, at even odds.' },
  'OPS.unitLoss.staff.cap': { what: 'Most troops a single failed operation can ever kill. Not shown to the player.' },
  'OPS.unitLoss.weapons.base': { what: 'Weapons lost by a failed operation, at even odds.' },
  'OPS.unitLoss.weapons.cap': { what: 'Most weapons a single failed operation can ever cost.' },
  'OPS.winLoss': { what: 'The same maths for a win, so a close victory still costs people.' },

  // ---------- WARN ----------
  'WARN.share': {
    what: 'The share of the rolled warning time that is kept.',
    feel: 'At 0.5 an attack carries half the notice it used to, and gets the rest back from the march below.',
  },
  'WARN.travel': {
    what: 'The march. Warning also includes this many times the time one of YOUR operations out of that sector would take.',
    feel: 'A strike out of the next sector barely gives you time to turn around; one from deep in their territory is seen coming for minutes.',
  },
  'WARN.floor': { what: 'Seconds of warning an attack can never drop below, however close it starts.' },

  // ---------- RAIDS ----------
  'RAIDS.startAtCore': { what: 'The AI Core level at which raids begin at all.' },
  'RAIDS.firstDelay': { what: 'Seconds of warning before the very first raid of the game.' },
  'RAIDS.intervalMin': { what: 'Shortest gap between raids, in seconds.' },
  'RAIDS.intervalMax': { what: 'Longest gap between raids, in seconds.' },
  'RAIDS.breachRatio': {
    what: 'A raid this many times over your Defense does not just rob you — it leaves a breach on the sector, like an assault would.',
    feel: 'Two breaches still lose the sector, so a run of heavy raids can cost you ground, not just stock.',
  },
  'RAIDS.threatShare': {
    what: 'Raid strength tracks this share of your Threat index (Power + Defense + experts).',
    feel: 'This is the main self-balancing dial: the bigger you get, the bigger the raids. Raising it makes growth punish you faster.',
  },
  'RAIDS.spreadMin': { what: 'Low end of the random spread on raid strength.' },
  'RAIDS.spreadMax': { what: 'High end of the random spread on raid strength.' },
  'RAIDS.winSharpness': { what: 'As OPS.winSharpness, for holding a raid: your Defense against its strength.' },
  'RAIDS.lossMin': { what: 'Share of each stockpile taken by a raid you barely lost.' },
  'RAIDS.lossMax': { what: 'Share of each stockpile taken by a raid you lost badly.' },
  'RAIDS.unitLoss': { what: 'Troops and defenses killed by a raid that got through. Same shape as OPS.unitLoss.' },
  'RAIDS.winLoss': { what: 'Troops and defenses lost holding a raid. Holding a close one still costs people.' },
  'RAIDS.enemyLoss': { what: 'Share of the attacker\'s own strength that dies on your wall. Assaults pay it out of their sector\'s strength; raids have no sector, so for them it is only reported.' },
  'RAIDS.enemyLoss.win': { what: 'What the attacker loses when you hold.' },
  'RAIDS.enemyLoss.loss': { what: 'What the attacker loses when it breaks through.' },
  'RAIDS.loot': { what: 'Fixed scrip for holding an attack, from its strength S: a x S^b + c x S.' },

  // ---------- TUTORIAL ----------
  'TUTORIAL.target': { what: 'The sector id the opening directive names. It sits at a fixed soft defense while that directive stands, so the first operation is a lesson and not a coin flip.' },
  'TUTORIAL.directive': { what: 'Index of "Capture ..." in DIRECTIVES (js/data/story.js). The soft defense holds from this directive onward.' },
  'TUTORIAL.defense': { what: 'What that sector is worth while the opening directive stands. Its real defense comes back afterwards.' },
  'TUTORIAL.retakeCore': { what: 'The AI Core level at which the clan comes back for it, hard. This is where breaches, overruns and retaking are taught.' },
  'TUTORIAL.retakeDelay': { what: 'Seconds of visible warning before that scripted assault lands.' },
  'TUTORIAL.retakeMargin': { what: 'Multiplier on the overrun threshold for that one assault, so the first breach definitely takes the sector.' },

  // ---------- NODES ----------
  'NODES.strengthMin': { what: 'Floor on a sector\'s strength multiplier. 1.0 means "worth exactly its base defense".' },
  'NODES.strengthMax': { what: 'Hard ceiling on strength, passive growth included.' },
  'NODES.growthPerHour': { what: 'How much strength an enemy sector gains per hour on its own, multiplied by its clan\'s growth stance.' },
  'NODES.opLossGain': { what: 'Strength added to a sector when your operation against it fails. Morale.' },
  'NODES.opDefenderCut': { what: 'Strength removed at the same time: what repelling you cost the defenders.' },
  'NODES.defendWinCut': { what: 'Strength removed from the attacking sector when its assault on you is held.' },
  'NODES.breachCut': { what: 'Strength removed from the attacking sector even when its assault gets through.' },
  'NODES.plunderShare': { what: 'A clan in a Plunder stance takes this much of what a lost raid would have taken, instead of taking ground.' },
  'NODES.takenMax': { what: 'A sector they take off you is held as hard as they took it, up to this many times its base defense.' },
  'NODES.foreignShare': {
    what: 'Ground a clan takes that was never theirs is rebuilt to their own standard: this share of what that clan is worth at home on average.',
    feel: 'It is why a sector changing hands can become a much harder prize than it was.',
  },
  'NODES.foreignCap': { what: 'Ceiling on that rebuild: never more than this many times what the sector was already worth.' },
  'NODES.breachesToFall': { what: 'Breached assaults a sector of yours can take before it is lost.' },
  'NODES.overrunRatio': {
    what: 'An assault this many times your Defense takes the sector on the FIRST breach, with no second visit.',
    feel: 'Shown to the player as an overrun warning while it is inbound.',
  },
  'NODES.startAtCore': { what: 'AI Core level at which assaults on your border begin.' },
  'NODES.firstDelay': { what: 'Seconds before the first assault timer runs out.' },
  'NODES.intervalMin': { what: 'Shortest gap between assaults, in seconds, before the tempo stance divides it.' },
  'NODES.intervalMax': { what: 'Longest gap between assaults, in seconds.' },
  'NODES.warningMin': { what: 'Low end of the rolled spotting time for an assault, before WARN reshapes it.' },
  'NODES.warningMax': { what: 'High end of the rolled spotting time.' },
  'NODES.assaultShare': {
    what: 'Assault strength = the sector\'s defense x this x a random spread x its clan\'s strength stance.',
    feel: 'The single dial for how dangerous the border is. 0.38 means a sector throws about a third of what it is worth at you.',
  },
  'NODES.lockedWeight': {
    what: 'How much a SEALED border (a clan whose chapter has not opened) weighs against 1 for an open one when the next assault is rolled.',
    feel: 'Low, so you mostly fight the clan whose chapter you are in.',
  },
  'NODES.lockedAggrGain': { what: 'A stirred sealed border counts this much more per point of aggression. Poking a clan you cannot reach yet does get noticed.' },
  'NODES.lockedFalloff': { what: 'A sealed border weighs less the further its chapter still is from opening. Higher means distant chapters go quieter.' },
  'NODES.lockedEvery': {
    what: 'Every this many assaults, the roll is reserved for a sealed clan, if one borders you at all.',
    feel: 'Open borders out-weigh sealed ones so heavily that without this a sealed clan could watch for hours and never come.',
  },
  'NODES.lockedCap': { what: 'A sealed clan\'s assault is capped at this many times the raid strength you would be facing anyway.' },
  'NODES.spreadMin': { what: 'Low end of the random spread on assault strength.' },
  'NODES.spreadMax': { what: 'High end of the random spread on assault strength.' },

  // ---------- RALLY ----------
  'RALLY.base': { what: 'Chance a clan rallies after losing a sector, before its own profile is read.' },
  'RALLY.capital': { what: 'Multiplier on that chance when it was their capital that fell.' },
  'RALLY.donors': { what: 'How many of their other sectors can send strength to the one that will lead the counter-attack.' },
  'RALLY.take': { what: 'Share of a donor sector\'s strength that marches.' },
  'RALLY.keep': { what: 'How much of what marched is still there when it arrives. 1 = all of it.' },
  'RALLY.enough': {
    what: 'A neighbour whose assault already reaches this share of your Defense needs no help, so no rally happens.',
    feel: 'It stops a clan stripping itself when one sector could already take the ground back alone.',
  },

  // ---------- AGGR ----------
  'AGGR.min': { what: 'Floor on a sector\'s stored aggression. Negative means calmer than default.' },
  'AGGR.max': { what: 'Ceiling on stored aggression.' },
  'AGGR.calmPerHour': { what: 'How fast aggression drifts back toward calm per hour while nothing happens.' },
  'AGGR.pressure': { what: 'Aggression a sector reads off your Power alone, at full tension. Added on top of stored anger.' },
  'AGGR.pressureFrom': { what: 'The share of its defense your Power must reach before that pressure starts. Full once you match it.' },
  'AGGR.onOp': { what: 'Aggression added when you launch an operation: to the target, to every sector of its clan, and to the sectors it links to.' },
  'AGGR.onOp.node': { what: 'Added to the sector you attacked.' },
  'AGGR.onOp.clan': { what: 'Added to every other sector that clan holds.' },
  'AGGR.onOp.near': { what: 'Added to the sectors the target links to.' },
  'AGGR.onInspect': { what: 'Added when the player opens that sector\'s briefing. Each look after the first counts for less (divided by how many times it has been seen).' },
  'AGGR.onFall': { what: 'Aggression added when a sector of YOURS falls: the front around it is emboldened.' },
  'AGGR.onFall.node': { what: 'Added to the sector that took it.' },
  'AGGR.onFall.clan': { what: 'Added across that clan.' },
  'AGGR.onFall.near': { what: 'Added to the sectors around it.' },

  // ---------- CLANS ----------
  'CLANS.startAtCore': { what: 'The AI Core level at which stances start working and get announced.' },
  'CLANS.driftPerHour': { what: 'How fast each trait slides back toward its faction\'s baseline mood per hour while nothing happens.' },
  'CLANS.interceptShare': { what: 'When you give a border order, this share of the clan-wide aggression it causes also lands as fury.' },
  'CLANS.stir': { what: 'What the war does to a clan\'s four traits. Applied to whoever owns the sector involved.' },
  'CLANS.stir.sectorTaken': { what: 'You took one of theirs.' },
  'CLANS.stir.capitalTaken': { what: 'You took their capital.' },
  'CLANS.stir.assaultHeld': { what: 'Their assault broke on your wall.' },
  'CLANS.stir.assaultBreach': { what: 'Their assault got through.' },
  'CLANS.stir.sectorSeized': { what: 'They took one of yours.' },
  'CLANS.stir.opHeld': { what: 'Your operation against them failed.' },
  'CLANS.stir.raidHeld': { what: 'Their raid was held.' },
  'CLANS.stir.raidBroke': { what: 'Their raid got through.' },
  'CLANS.stances': { what: 'The matrix. A stance holds whenever every range in its `when` is satisfied. Several can hold at once and their effects multiply, so behaviour is always the combination.' },

  // ---------- traits ----------
  fury: { what: 'How badly they want to hurt you. Driven up by losing ground, down by time.' },
  fear: { what: 'What your Power has taught them.' },
  order: { what: 'Command and cohesion. Low order means every camp is its own war.' },
  greed: { what: 'What they want off you instead of your death.' },

  // ---------- stance fields (matched by key name anywhere under CLANS.stances) ----------
  'stance.when': { what: 'The cell of the matrix. Each entry is [min] or [min, max] for a trait, for `held` (share of their sectors you hold) or for `capital` (1 when you hold their capital).' },
  'stance.weight': { what: 'How often an assault is rolled from this clan, against 1 for a clan with no stance.' },
  'stance.retake': { what: 'Extra weight on sectors they lost to you, so they come for their own ground first.' },
  'stance.strength': { what: 'Multiplier on assault strength.' },
  'stance.tempo': { what: 'Divides the gap between assaults across the whole map. Above 1 means attacks come faster.' },
  'stance.growth': { what: 'Multiplier on how fast their sectors gain strength per hour.' },
  'stance.support': { what: 'Multiplier on clan support (OPS.flankBonus) between their sectors. 0 means their sectors no longer fortify each other.' },
  'stance.raid': { what: 'Multiplier on raid strength.' },
  'stance.attrition': { what: 'Multiplier on what an assault costs them in their own sector strength.' },
  'stance.plunder': { what: 'When true, a breach takes stockpiles instead of ground.' },
  'stance.buff': { what: 'A standing effect on your economy while this stance holds. Never ticks down; it ends when the stance ends.' },
  'stance.line': { what: 'What the AI says when this stance begins. {faction} is replaced with the clan\'s short name.' },
  'stance.desc': { what: 'The one-line effect shown to the player.' },
  'stance.icon': { what: 'Icon key from js/ui/icons.js, pinned to the clan\'s hexes on the map.' },

  // ---------- FACTIONS ----------
  'faction.raidFloor': { what: 'The smallest raid this clan ever sends, whatever your Threat is. It is what makes a late chapter dangerous the moment it opens.' },
  'faction.mood': { what: 'Where this clan\'s four traits sit when nothing has happened. Traits always drift back here.' },
  'faction.raidName': { what: 'What its raids are called in the log.' },
  'faction.color': { what: 'Its colour on the map, in the legend and on its attack beams.' },
  'faction.icon': { what: 'Icon key from js/ui/icons.js.' },
  'faction.short': { what: 'Short name, used in log lines and {faction} substitutions.' },

  // ---------- CHAPTERS ----------
  'chapter.core': { what: 'The AI Core level that opens this chapter. MAP.earlyLead lets its weakest frontier sectors leak one level early.' },
  'chapter.faction': { what: 'Which clan this chapter is about. Its capital falling is what ends the chapter.' },

  // ---------- sector fields ----------
  'sector.defense': { what: 'What this sector is worth before anything modifies it. Operation price, duration, loot and assault strength all derive from it.' },
  'sector.core': { what: 'A hard AI Core gate of its own, instead of its chapter\'s. A sector with one never leaks early.' },
  'sector.boss': { what: 'Capital. Taking it ends that clan\'s raids and closes the chapter. Exactly one per faction.' },
  'sector.chapter': { what: 'Which chapter gates this sector, unless it has a `core` of its own.' },
  'sector.bonus': { what: 'Permanent, additive percentage while you hold the sector. 0.05 = +5%.' },
  'sector.links': { what: 'Which sectors it borders. Links are two-way: both ends must name the other.' },
  'sector.faction': { what: 'Who starts holding it. A clan that later takes it off you keeps it, with its own colour and stances.' },
  'sector.lore': { what: 'What the AI says about it in the briefing. Its voice: cold, precise, darkly funny.' },
  'sector.x': { what: 'Map units. Position is cosmetic — no game rule reads it. Regenerate the whole layout with tools/map-layout.mjs.' },
  'sector.y': { what: 'Map units. Position is cosmetic — no game rule reads it.' },

  // ---------- simulation-only ----------
  'sim.core': { what: 'The AI Core level to measure at. It decides which chapters are open and whether assaults and stances are running.' },
  'sim.strength': { what: 'This sector\'s live strength multiplier. 1 = exactly its base defense. In play it grows on its own and moves with every fight.' },
  'sim.aggression': { what: 'This sector\'s stored anger. Never shown to the player; it only weights which border the next assault comes from.' },
  'sim.owner': { what: 'Pretend another clan has taken this sector off you. Their rebuild standard (NODES.foreignShare) then decides what it is worth.' },
  'sim.held': { what: 'Pretend the player holds this sector. Ctrl-click a hex on the map.' },
  'sim.units': { what: 'Unit counts. Power, Defense and Experts are whatever these add up to through sim/economy.js factors() — the same function the game uses.' },
  'sim.troopsOut': { what: 'An operation is away, so staff units stop counting toward Defense (sim/economy.js, rawFactors).' },
  'sim.tutorial': { what: 'The opening directive still stands, so TUTORIAL.target sits at its soft defense.' },
};

/** Look up an explanation by splice path, falling back to the by-name entries. */
export function explain(path) {
  const key = path.join('.');
  if (GLOSSARY[key]) return GLOSSARY[key];
  const last = path[path.length - 1];
  // Numeric segments mean "inside an array" — stances, factions and chapters are keyed by name.
  if (path[0] === 'CLANS' && path.includes('stances') && GLOSSARY[`stance.${last}`]) return GLOSSARY[`stance.${last}`];
  if (path[0] === 'CLANS' && path[1] === 'stir' && path.length === 3) return GLOSSARY[`CLANS.stir.${last}`];
  if (path[0] === 'FACTIONS' && GLOSSARY[`faction.${last}`]) return GLOSSARY[`faction.${last}`];
  if (path[0] === 'CHAPTERS' && GLOSSARY[`chapter.${last}`]) return GLOSSARY[`chapter.${last}`];
  if (GLOSSARY[last] && typeof GLOSSARY[last] === 'object') return GLOSSARY[last];
  return null;
}
