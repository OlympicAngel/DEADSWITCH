// War: operations against map sectors (your Power) and raids against you (your Defense).
import { SECTORS, FACTIONS, CHAPTERS, OPS, RAIDS, NODES, AGGR, CLANS, ALIGNMENT, EVENTS_CFG, MAP, BALANCE, TUTORIAL, RALLY, WARN } from '../data.js';
import { spawnAftermath } from './story.js';
import {
  level, factors, threat, canAfford, pay, grant, loseUnits, lossValue, say, caps, rewardCurve, troopsOut, troopDefense,
} from './economy.js';
import { clanProfile, clanProfiles, stirClan } from './clans.js';
import { rand, range, pick, odds } from './rng.js';

const SECTOR_BY_ID = Object.fromEntries(SECTORS.map((x) => [x.id, x]));
export const sectorById = (id) => SECTOR_BY_ID[id];

export const chapterOf = (id) => CHAPTERS.find((c) => c.id === id);

export function chapterOpen(s, chapterId) {
  return level(s, 'core') >= chapterOf(chapterId).core;
}

/** The AI Core level a sector needs: its own hard gate, or its chapter's (data/world.js). */
export const sectorGate = (sec) => sec.core ?? chapterOf(sec.chapter).core;

// What each clan's ground is worth on average, which is the standard it rebuilds anything it takes to.
const CLAN_AVG = {};
const CLAN_ATK = {};
const CLAN_MAX = {};
for (const f of new Set(SECTORS.map((x) => x.faction).filter(Boolean))) {
  const own = SECTORS.filter((x) => x.faction === f);
  CLAN_AVG[f] = own.reduce((a, x) => a + x.defense, 0) / own.length;
  CLAN_ATK[f] = own.reduce((a, x) => a + x.attack, 0) / own.length;
  CLAN_MAX[f] = Math.max(...own.map((x) => x.attack));
}

/** Who holds a sector now. The map says where a sector started; a clan that takes it off us when it
 *  was never theirs keeps it, with its own colour, stances and raids. */
export function factionOf(s, id) {
  return (s.owner && s.owner[id]) || (SECTOR_BY_ID[id] ? SECTOR_BY_ID[id].faction : null);
}

/** Every sector a clan holds right now, captured ground included. */
export const clanSectors = (s, faction) => SECTORS.filter((x) => factionOf(s, x.id) === faction);

/** What a sector is worth under whoever holds it. Ground taken off another clan is rebuilt to that
 *  clan's own standard (NODES.foreignShare of what they hold at home), never weaker than it was and
 *  never more than NODES.foreignCap x, so a prize is harder without belonging to another chapter. */
export function baseDefense(s, id) {
  const sec = SECTOR_BY_ID[id];
  const owner = s.owner && s.owner[id];
  if (!owner || !CLAN_AVG[owner]) {
    return sec.defense;
  }
  return Math.round(clamp(CLAN_AVG[owner] * NODES.foreignShare, sec.defense, sec.defense * NODES.foreignCap));
}

/** What a sector throws at us, under whoever holds it. Ground taken off another clan is garrisoned
 *  their way, on the same terms as its walls (baseDefense). */
export function baseAttack(s, id) {
  const sec = SECTOR_BY_ID[id];
  const owner = s.owner && s.owner[id];
  if (!owner || !CLAN_ATK[owner]) {
    return sec.attack;
  }
  return Math.round(clamp(CLAN_ATK[owner] * NODES.foreignShare, sec.attack, sec.attack * NODES.foreignCap));
}

// The weakest sectors of a faction that border ground we hold, which is what leaks early.
// Sorted by defense, then id, so the set only moves when the front does.
export function earlyTargets(s, faction) {
  return SECTORS
    .filter((x) => factionOf(s, x.id) === faction && x.core === undefined
      && !s.sectors.includes(x.id) && x.links.some((l) => s.sectors.includes(l)))
    .sort((a, b) => a.defense - b.defense || (a.id < b.id ? -1 : 1))
    .slice(0, MAP.earlyOpen)
    .map((x) => x.id);
}

/** A sector is open when its gate is met, or when its clan's frontier is already leaking it. */
export function sectorOpen(s, sec) {
  const gate = sectorGate(sec);
  const core = level(s, 'core');
  if (core >= gate) {
    return true;
  }
  return core >= gate - MAP.earlyLead && earlyTargets(s, factionOf(s, sec.id)).includes(sec.id);
}

// ---------- operations ----------

export function opCost(sec) {
  return {
    money: Math.ceil(sec.defense * OPS.costMoneyPerDefense * BALANCE.priceMult),
    energy: Math.ceil(sec.defense * OPS.costEnergyPerDefense * BALANCE.priceMult),
  };
}

export function opTime(sec) {
  return OPS.timeBase + Math.sqrt(sec.defense) * OPS.timePerSqrtDefense;
}

/** How long an attack out of `fromId` is visible before it lands (data/world.js). */
export function warning(s, base, fromId) {
  const from = SECTOR_BY_ID[fromId];
  const march = from ? opTime(from) * WARN.travel : 0;
  return Math.max(WARN.floor, base * WARN.share + march);
}

// Capture loot, paid the first time a sector falls to you (retaking a lost sector pays nothing).
export function opLoot(sec) {
  const out = {};
  for (const [k, curve] of Object.entries(OPS.loot)) {
    out[k] = Math.ceil(rewardCurve(curve, sec.defense));
  }
  return out;
}

// ---------- living sectors ----------

const node = (s, id) => s.nodes[id] || (s.nodes[id] = { m: 1, marks: 0, a: 0, seen: 0, p: 1, last: 0 });
const clamp = (v, lo, hi) => Math.min(hi, Math.max(lo, v));

// Strength multiplier of a sector you do not hold (1 = its base defense).
export const nodeStrength = (s, id) => (s.nodes[id] ? s.nodes[id].m : 1);

// Breached assaults a held sector has taken (it falls at NODES.breachesToFall).
export const breaches = (s, id) => (s.nodes[id] ? s.nodes[id].marks : 0);

export function shiftStrength(s, id, delta) {
  const n = node(s, id);
  n.m = Math.min(NODES.strengthMax, Math.max(NODES.strengthMin, n.m + delta));
}

/** How much harder than usual this sector is pushing: 1 until its attacks start failing. */
export const nodePush = (s, id) => (s.nodes[id] ? s.nodes[id].p || 1 : 1);

/** One more failure against the same wall. Nothing here touches its strength; that falls on its own. */
export function shiftPush(s, id, delta) {
  const n = node(s, id);
  n.p = clamp((n.p || 1) + delta, 1, NODES.pushMax);
}

/** They got what they came for, or something made them think again: back to an ordinary posture. */
export const resetPush = (s, id) => { if (s.nodes[id]) s.nodes[id].p = 1; };

// Enemy sectors in open chapters slowly rebuild on their own, up to the hard ceiling, and every
// sector's anger at you cools off while you leave it alone.
// A cracked sector is sealed once nothing hostile borders it: with no enemy at its walls there is
// nobody to finish the job, and the breaches are repaired.
function sealSafe(s) {
  for (const id of s.sectors) {
    const n = s.nodes[id];
    if (!n || !n.marks) continue;
    if (!SECTOR_BY_ID[id].links.some((l) => factionOf(s, l) && !s.sectors.includes(l))) {
      n.marks = 0;
      say(s, 'breachSealed', { sector: SECTOR_BY_ID[id].name }, 'good');
    }
  }
}

export function advanceNodes(s, dt) {
  sealSafe(s);
  const grow = (NODES.growthPerHour * dt) / 3600;
  const profiles = clanProfiles(s);
  for (const sec of SECTORS) {
    const f = factionOf(s, sec.id);
    if (f && !s.sectors.includes(sec.id) && sectorOpen(s, sec) && nodeStrength(s, sec.id) < NODES.strengthMax) {
      shiftStrength(s, sec.id, grow * profiles[f].growth);
    }
  }
  const calm = (AGGR.calmPerHour * dt) / 3600;
  for (const n of Object.values(s.nodes)) {
    if (n.a) n.a = Math.abs(n.a) <= calm ? 0 : n.a - Math.sign(n.a) * calm;
  }
}

// ---------- aggression: how keen a sector is to attack you (never shown to the player) ----------

/** Stored anger plus what the sector reads off your Power right now. */
export function aggression(s, id) {
  const sec = SECTOR_BY_ID[id];
  if (!sec) return 0;
  const stored = (s.nodes[id] && s.nodes[id].a) || 0;
  return clamp(stored + pressure(s, sec), AGGR.min, AGGR.max);
}

// A sector watches you gain on it: nothing while you are far below its defense, full tension once you match it.
function pressure(s, sec) {
  const ratio = opPower(s) / Math.max(1, sectorDefense(s, sec));
  return AGGR.pressure * clamp((ratio - AGGR.pressureFrom) / (1 - AGGR.pressureFrom), 0, 1);
}

export function shiftAggression(s, id, delta) {
  if (!factionOf(s, id) || s.sectors.includes(id)) return;
  const n = node(s, id);
  n.a = clamp((n.a || 0) + delta, AGGR.min, AGGR.max);
}

/** Spreads a nudge from one sector: itself, every sector of its clan, and the sectors it links to. */
export function stirAggression(s, id, spread) {
  const sec = SECTOR_BY_ID[id];
  if (!sec) return;
  if (spread.node) shiftAggression(s, id, spread.node);
  const f = factionOf(s, id);
  if (spread.clan && f) {
    for (const x of clanSectors(s, f)) {
      if (x.id !== id) shiftAggression(s, x.id, spread.clan);
    }
  }
  if (spread.near) {
    for (const l of sec.links) shiftAggression(s, l, spread.near);
  }
}

// Opening a sector's briefing is counted (the developer panel shows it) and changes nothing: reading
// the map has to be safe, or the careful player is the one punished.
export function inspectSector(s, id) {
  if (!factionOf(s, id) || s.sectors.includes(id)) return;
  const n = node(s, id);
  n.seen = (n.seen || 0) + 1;
  shiftAggression(s, id, AGGR.onInspect / n.seen); // being studied is noticed
}

// Clan support: a sector is fortified by the sectors of its own faction it links to. +flankBonus while
// every one of them is still theirs, falling linearly to 0 as you take them. Links to other factions
// (or to your own sectors through another faction) give nothing; an outpost alone among strangers gets none.
export function flank(s, sec) {
  const own = factionOf(s, sec.id);
  const clan = own ? sec.links.filter((l) => factionOf(s, l) === own) : [];
  const held = clan.filter((l) => s.sectors.includes(l)).length;
  if (!clan.length) {
    return { approaches: 0, held: 0, bonus: 0 };
  }
  const support = OPS.flankBonus * clanProfile(s, own).support;
  return { approaches: clan.length, held, bonus: support * ((clan.length - held) / clan.length) };
}

export function sectorDefense(s, sec) {
  // The first target a directive names sits at a fixed, soft number, drift and clan support aside,
  // until the clan takes it back at AI Core 2 (data/world.js).
  if (sec.id === TUTORIAL.target && s.directive >= TUTORIAL.directive && (s.scripted.retake || 0) < 2) {
    return TUTORIAL.defense;
  }
  return Math.ceil(baseDefense(s, sec.id) * OPS.defenseScale * nodeStrength(s, sec.id) * (1 + flank(s, sec).bonus));
}

/** What we hit a sector with: our Power, as it reads everywhere else. */
export const opPower = (s, f = factors(s)) => f.power;

export function opChance(s, sec) {
  return odds(opPower(s), sectorDefense(s, sec), OPS.winSharpness);
}

// owned | locked (chapter) | hidden (not adjacent) | target (attackable)
export function sectorStatus(s, sec) {
  if (s.sectors.includes(sec.id)) {
    return 'owned';
  }
  const adjacent = sec.links.some((l) => s.sectors.includes(l));
  if (!adjacent) {
    return 'far';
  }
  // Ground I have held once is always mine to take back, whatever chapter its clan belongs to.
  if (!sectorOpen(s, sec) && !s.taken.includes(sec.id)) {
    return 'locked';
  }
  return 'target';
}

export function canLaunch(s, sec) {
  return !s.op && factors(s).power > 0 && sectorStatus(s, sec) === 'target' && canAfford(s, opCost(sec)) && !Object.keys(opCost(sec)).some((k) => opCost(sec)[k] > caps(s)[k]);
}

export function launchOp(s, id) {
  const sec = SECTOR_BY_ID[id];
  if (!sec || !canLaunch(s, sec)) {
    return false;
  }
  pay(s, opCost(sec));
  stirAggression(s, id, AGGR.onOp);
  const total = opTime(sec);
  s.op = { sector: id, remaining: total, total };
  say(s, 'opLaunched', { sector: sec.name });
  return true;
}

export function advanceOp(s, dt) {
  if (!s.op) {
    return;
  }
  s.op.remaining -= dt;
  if (s.op.remaining > 1e-9) {
    return;
  }
  const sec = SECTOR_BY_ID[s.op.sector];
  s.op = null;
  const power = opPower(s);
  const defense = sectorDefense(s, sec);
  const chance = odds(power, defense, OPS.winSharpness);
  const roll = rand(s, 'op');
  const report = { kind: 'op', sector: sec.id, power, defense, chance, roll, win: roll < chance };
  // Taking ground costs people too; the garrison that held it is gone either way.
  takeLosses(s, report, report.win ? OPS.winLoss : OPS.unitLoss, power, defense);
  if (report.win) {
    report.theirLoss = defense;
    const held = factionOf(s, sec.id); // whoever took it off us last, not whoever drew the map
    // A capital is its own clan's: taking it back off whoever seized it is that clan's fall, not theirs.
    const capitalOf = sec.boss ? sec.faction : null;
    stirClan(s, held, CLANS.stir[capitalOf && held === capitalOf ? 'capitalTaken' : 'sectorTaken']);
    s.sectors.push(sec.id);
    delete s.nodes[sec.id];
    delete s.owner[sec.id];
    // Loot and memories only the first time; a retaken sector just comes back.
    const first = !s.taken.includes(sec.id);
    const loot = first ? opLoot(sec) : {};
    if (first) s.taken.push(sec.id);
    grant(s, loot);
    report.loot = loot;
    report.retaken = !first;
    s.stats.opsWon++;
    say(s, first ? 'opWon' : 'opRetaken', { sector: sec.name }, 'good');
    if (first) {
      say(s, 'lore', { text: sec.lore }, 'story');
      // A capital, or every sixth fragment, is the archive moving on: those get a cut scene.
      if (sec.boss || s.taken.length % MAP.fragmentCut === 0) {
        s.inbox.push({ kind: 'memory', sector: sec.id });
      }
    }
    // Every stone a clan held is ours: that is the end of them, said once.
    if (held && !clanSectors(s, held).some((x) => !s.sectors.includes(x.id)) && !s.scripted['wipe:' + held]) {
      s.scripted['wipe:' + held] = 1;
      say(s, 'clanWiped', { faction: FACTIONS[held].name }, 'rank');
      s.inbox.push({ kind: 'wipe', faction: held });
    }
    if (held) {
      report.rally = rallyClan(s, held, sec.id);
    }
    if (capitalOf) {
      say(s, 'bossDown', { faction: FACTIONS[capitalOf].name }, 'rank');
      if (s.raid && s.raid.faction === capitalOf) {
        s.raid = null;
        s.raidTimer = RAIDS.minWarning;
      }
    }
    s.inbox.push(report);
    if (sec.id === 'prime' && !s.ending) {
      s.ending = s.align >= ALIGNMENT.guardianAt ? 'guardian' : s.align <= ALIGNMENT.overlordAt ? 'overlord' : 'fork';
      s.inbox.push({ kind: 'ending', key: s.ending });
    }
  } else {
    // A repelled operation emboldens the defenders, minus what repelling me cost them.
    report.theirLoss = Math.round(defense * NODES.opDefenderCut);
    stirClan(s, factionOf(s, sec.id), CLANS.stir.opHeld);
    shiftStrength(s, sec.id, NODES.opLossGain - NODES.opDefenderCut);
    report.strength = nodeStrength(s, sec.id);
    s.stats.opsLost++;
    say(s, 'opLost', { sector: sec.name }, 'bad');
    s.inbox.push(report);
  }
}

// Share of a unit tab lost in a defeat: base x (their strength / ours), capped. Even fights cost the base.
export function lossShare(rule, ours, theirs) {
  return Math.min(rule.cap, rule.base * (ours > 0 ? theirs / ours : Infinity));
}

function casualties(s, rules, ours, theirs, floor) {
  const out = {};
  for (const [tab, rule] of Object.entries(rules)) {
    Object.assign(out, loseUnits(s, tab, lossShare(rule, ours, theirs), floor));
  }
  return out;
}

// Writes my casualties onto a report, both as units and as the Power / Defense they were worth.
// A win only costs people when it was close: a comfortable one rounds down to nobody.
function takeLosses(s, report, rules, ours, theirs) {
  report.units = casualties(s, rules, ours, theirs, !report.win);
  report.cost = lossValue(s, report.units);
}

// ---------- raids ----------

// Factions whose chapter is open and whose capital still stands. Rival Cores join after the ending.
export function activeRaiders(s) {
  const list = [];
  for (const c of CHAPTERS) {
    const capital = SECTORS.find((x) => x.faction === c.faction && x.boss);
    if (chapterOpen(s, c.id) && !s.sectors.includes(capital.id)) {
      list.push(c.faction);
    }
  }
  // Rival Cores come after the ending, until their own capital falls like any other.
  const mirror = SECTORS.find((x) => x.faction === 'rogue' && x.boss);
  if (s.ending && !(mirror && s.sectors.includes(mirror.id))) {
    list.push('rogue');
  }
  return list;
}

// Where an attack comes from: a sector of that faction, the frontier first so it is somewhere the
// player can see, and the keenest of those. A faction with no ground left attacks from nowhere.
export function attackSource(s, faction) {
  const own = clanSectors(s, faction).filter((x) => !s.sectors.includes(x.id));
  const front = own.filter((x) => x.links.some((l) => s.sectors.includes(l)));
  const pool = front.length ? front : own;
  if (!pool.length) return null;
  const w = (x) => 1 + aggression(s, x.id) - AGGR.min;
  let r = rand(s, 'source') * pool.reduce((a, x) => a + w(x), 0);
  return (pool.find((x) => (r -= w(x)) <= 0) || pool[pool.length - 1]).id;
}

/** Where a raid out of `from` actually lands: the nearest ground we hold on the way to the Nest,
 *  and of those, the one they have already cracked. A clan marching on the Nest does not walk past
 *  a half-breached outpost of ours to get there. */
export function raidTarget(s, from) {
  if (!from || !SECTOR_BY_ID[from]) {
    return MAP.home;
  }
  const seen = new Set([from]);
  let edge = [from];
  while (edge.length) {
    const next = [];
    const held = [];
    for (const id of edge) {
      for (const l of SECTOR_BY_ID[id].links) {
        if (seen.has(l)) {
          continue;
        }
        seen.add(l);
        (s.sectors.includes(l) ? held : next).push(l);
      }
    }
    if (held.length) {
      // Cracked first, then angriest, and the Nest last: they would rather take an outpost.
      return held.sort((a, b) => breaches(s, b) - breaches(s, a)
        || (a === MAP.home ? 1 : 0) - (b === MAP.home ? 1 : 0)
        || aggression(s, b) - aggression(s, a) || (a < b ? -1 : 1))[0];
    }
    edge = next;
  }
  return MAP.home;
}

/** The two sectors an attack runs between, or null while it has no place on the map any more. */
export function attackPlace(s, atk) {
  if (!atk || !SECTOR_BY_ID[atk.from] || !SECTOR_BY_ID[atk.target]) return null;
  if (s.sectors.includes(atk.from) || !s.sectors.includes(atk.target)) return null;
  return [atk.from, atk.target];
}

// Sieges are threats the player chose (or failed) to answer with force; they land at a fixed time.
export function attacks(s) {
  return [s.raid, s.assault, ...(s.sieges || [])].filter(Boolean).sort((a, b) => a.remaining - b.remaining);
}

export const nextAttack = (s) => attacks(s)[0] || null;

export function attackName(atk) {
  if (atk.assault) return `Assault from ${SECTOR_BY_ID[atk.from].name}`;
  if (atk.siege) return `${FACTIONS[atk.faction].short} siege`;
  if (atk.grudge) return `${FACTIONS[atk.faction].short} vengeance`;
  return FACTIONS[atk.faction].raidName;
}

// Revenge: the faction's next raids are multiplied, for a set count or until one breaks through.
export function addGrudge(s, g) {
  const untilLoss = rand(s, 'grudge') < EVENTS_CFG.grudgeUntilLossChance;
  const [lo, hi] = EVENTS_CFG.grudgeRaids;
  const left = untilLoss ? EVENTS_CFG.grudgeUntilLossMax : lo + Math.floor(rand(s, 'grudge') * (hi - lo + 1));
  const old = s.grudges.find((x) => x.faction === g.faction);
  if (old) {
    old.mult = Math.max(old.mult, g.mult);
    old.left += left;
    old.untilLoss = old.untilLoss || untilLoss;
  } else {
    s.grudges.push({ faction: g.faction, mult: g.mult, left, untilLoss });
  }
  if (s.raid && !s.raid.grudge && s.raid.faction === g.faction) {
    s.raid.strength = Math.ceil(s.raid.strength * g.mult);
    // A hold already under way rebuilds its strength from these, so they carry the grudge too.
    if (s.raid.rushBase !== undefined) s.raid.rushBase = Math.ceil(s.raid.rushBase * g.mult);
    if (s.raid.rushPay !== undefined) s.raid.rushPay *= g.mult;
    s.raid.grudge = true;
  }
  return { untilLoss, left };
}

function settleGrudge(s, raid, won) {
  const g = s.grudges.find((x) => x.faction === raid.faction);
  if (!g) return;
  g.left--;
  if ((g.untilLoss && !won) || g.left <= 0) {
    s.grudges = s.grudges.filter((x) => x !== g);
    say(s, 'grudgeEnd', { faction: FACTIONS[g.faction].name });
  }
}

// `uid` ties the siege to the order that announced it, so paying that order calls this attack off.
export function startSiege(s, faction, strength, delay, uid = 0) {
  const from = attackSource(s, faction);
  const t = Math.max(0, delay);
  const sg = { faction, strength, remaining: t, total: Math.max(t, 1), siege: true, uid, from, target: raidTarget(s, from) };
  s.sieges.push(sg);
  return sg;
}

export function advanceSieges(s, dt, offline) {
  for (const sg of s.sieges.slice()) {
    // A siege is an attack like any other, so an absence that has had its share of them holds it at
    // the gate until someone is back.
    if (offline && s.offlineRaids >= s.offlineRaidCap) {
      continue;
    }
    if (s.sectors.includes(sg.from)) {
      // The ground it marched out of is ours now: it comes from wherever they still hold.
      sg.from = attackSource(s, sg.faction);
      sg.target = raidTarget(s, sg.from);
    } else if (!s.sectors.includes(sg.target)) {
      sg.target = raidTarget(s, sg.from);
    }
    sg.remaining -= dt;
    if (sg.remaining <= 1e-9) {
      s.sieges = s.sieges.filter((x) => x !== sg);
      resolveAttack(s, sg, offline);
      if (offline) s.offlineRaids++;
    }
  }
}

/** The share of the premium paid by this far into a hold: steep at the start, flat by the end. */
export const rushEase = (f) => 1 - Math.pow(1 - Math.max(0, Math.min(1, f)), NODES.rushCurve);

/** What the whole march still costs from here, held in full. Pure, so the panel can show the bill. */
export function rushPrice(s, atk) {
  if (!atk || !atk.total || atk.remaining <= NODES.rushFloor) {
    return null;
  }
  const seconds = atk.remaining - NODES.rushFloor;
  const held = atk.rush || 0;
  const pay = atk.rushPay ?? atk.strength * NODES.rushCost * (seconds / atk.total);
  return { strength: Math.ceil((atk.rushBase ?? atk.strength) + pay), skipped: seconds / atk.total, seconds, held };
}

/** One step of holding an attack in: the clock comes down evenly, the strength it gains does not. */
export function rushStep(s, atk = nextAttack(s), step = 1 / (NODES.rushHold * NODES.rushTicks)) {
  if (!atk || !atk.total || atk.remaining <= NODES.rushFloor || (atk.rush || 0) >= 1) {
    return null;
  }
  const was = atk.rush || 0;
  if (!was) {
    // The whole bill is fixed when the hold starts, so a long march cannot be shaved a tap at a time.
    atk.rushBase = atk.strength;
    atk.rushPay = atk.strength * NODES.rushCost * ((atk.remaining - NODES.rushFloor) / atk.total);
    s.stats.rushed = (s.stats.rushed || 0) + 1;
    say(s, 'rushed', { raid: attackName(atk) }, 'bad');
  }
  const now = Math.min(1, was + step);
  atk.strength = Math.ceil(atk.rushBase + atk.rushPay * rushEase(now));
  atk.remaining = Math.max(NODES.rushFloor, atk.remaining - (atk.remaining - NODES.rushFloor) * ((now - was) / (1 - was)));
  atk.rush = now;
  return atk;
}

/** Holds an attack all the way in, in one go. */
export function rushAttack(s, atk = nextAttack(s)) {
  return rushStep(s, atk, 1);
}

export function raidChance(s, raid = s.raid) {
  return odds(factors(s).defense, raid.strength, RAIDS.winSharpness);
}

function spawnRaid(s, delay) {
  const raiders = activeRaiders(s);
  const grudge = (s.grudges || [])[0];
  if (!raiders.length && !grudge) {
    s.raid = null;
    s.raidTimer = RAIDS.minWarning;
    return;
  }
  // A faction with a grudge takes the next raid, active chapter or not, and hits harder.
  const faction = grudge ? grudge.faction : pick(s, raiders, 'raid');
  // A grudge from a clan whose chapter is not open yet brings no more than the clans that are.
  const own = raiders.includes(faction) ? FACTIONS[faction].raidBase : Math.min(...raiders.map((f) => FACTIONS[f].raidBase), FACTIONS[faction].raidBase);
  const strength = Math.ceil((own + raidThreat(s) * RAIDS.threatShare) * range(s, RAIDS.spreadMin, RAIDS.spreadMax, 'raid')
    * (grudge ? grudge.mult : 1) * clanProfile(s, faction).raid);
  // Raids march out of whatever that faction still holds, at the first of ours on the way in.
  const from = attackSource(s, faction);
  const t = warning(s, delay, from);
  s.raid = { faction, strength, remaining: t, total: t, grudge: !!grudge, from, target: raidTarget(s, from) };
  say(s, 'raidSpotted', { raid: FACTIONS[faction].raidName, target: SECTOR_BY_ID[s.raid.target].name, strength, time: fmtShort(t) }, 'bad');
}

function fmtShort(sec) {
  const m = Math.round(sec / 60);
  return m >= 1 ? m + ' min' : Math.round(sec) + 's';
}

/** A faction's raid before its clan's state and the spread: its own base plus its share of our Threat. */
export function raidSize(s, faction) {
  return FACTIONS[faction].raidBase + raidThreat(s) * RAIDS.threatShare;
}

/** What a raid is sized on: Threat, with Defense counted at RAIDS.defenseWeight. */
export function raidThreat(s, f = factors(s)) {
  return threat(s, { ...f, defense: f.defense * RAIDS.defenseWeight });
}

// Positive pushes the next raid back; negative brings it closer (never under a minute of warning).
export function delayRaid(s, seconds) {
  if (s.raid) {
    const before = s.raid.remaining;
    s.raid.remaining = Math.max(Math.min(before, RAIDS.minWarning), before + seconds);
    s.raid.total += s.raid.remaining - before;
  } else {
    s.raidTimer = Math.max(0, s.raidTimer + seconds);
  }
}

// Raids only ever arrive once per offline stretch, so being away is never punished repeatedly.
export function advanceRaids(s, dt, offline) {
  if (level(s, 'core') < RAIDS.startAtCore) {
    return;
  }
  if (offline && s.offlineRaids >= s.offlineRaidCap) {
    return;
  }
  if (!s.raid) {
    if (!s.raidsStarted) {
      s.raidsStarted = true;
      spawnRaid(s, RAIDS.firstDelay);
      return;
    }
    s.raidTimer -= dt;
    if (s.raidTimer <= 0) {
      spawnRaid(s, range(s, RAIDS.intervalMin, RAIDS.intervalMax, 'raid'));
    }
    return;
  }
  // Ground they were marching on can fall out from under them; they keep coming, further in. And if
  // we took the ground they marched out of, they come from what they still hold.
  if (s.sectors.includes(s.raid.from)) {
    s.raid.from = attackSource(s, s.raid.faction);
    s.raid.target = raidTarget(s, s.raid.from);
  } else if (!s.sectors.includes(s.raid.target)) {
    s.raid.target = raidTarget(s, s.raid.from);
  }
  s.raid.remaining -= dt;
  if (s.raid.remaining > 1e-9) {
    return;
  }
  resolveRaid(s, offline);
}

function resolveRaid(s, offline) {
  const raid = s.raid;
  s.raid = null;
  resolveAttack(s, raid, offline);
  if (offline) {
    s.offlineRaids++;
  }
  s.raidTimer = 0;
  spawnRaid(s, range(s, RAIDS.intervalMin, RAIDS.intervalMax, 'raid'));
}

// ---------- assaults: bordering enemy sectors try to take yours ----------

// Every (enemy sector -> held sector) border; the Nest is never a target. Borders with factions whose
// chapter is not open are marked locked (and left out unless asked for).
export function borders(s, withLocked = false) {
  const out = [];
  for (const id of s.sectors) {
    if (id === MAP.home) continue;
    for (const l of SECTOR_BY_ID[id].links) {
      const from = SECTOR_BY_ID[l];
      if (!factionOf(s, l) || s.sectors.includes(l)) continue;
      const locked = !sectorOpen(s, from);
      if (!locked || withLocked) out.push({ from: l, target: id, locked });
    }
  }
  return out;
}

export function assaultStrength(s, fromId) {
  const sec = SECTOR_BY_ID[fromId];
  const stance = clanProfile(s, factionOf(s, fromId)).strength;
  // Its walls barely carry into its troops (attackFromStrength), and every failure against us adds
  // to the push, so a sector that keeps losing becomes a harder attack and a softer target at once.
  const grit = 1 + (nodeStrength(s, fromId) - 1) * NODES.attackFromStrength;
  const own = baseAttack(s, fromId) * NODES.assaultMult * grit * nodePush(s, fromId);
  if (!sectorOpen(s, sec)) {
    // A clan whose chapter has not opened yet cannot field what its ground is really worth, so it is
    // held near the raid strength the player is already facing. The push rides on top of that cap:
    // hold a sealed neighbour off often enough and the next one is a fight, not a formality.
    const held = raidSize(s, 'scav') * NODES.lockedCap * nodePush(s, fromId);
    return Math.ceil(Math.min(own * stance, held));
  }
  // An open clan musters against what the player has become, not only what its own ground is worth.
  // Where it musters from still counts: a backwater raises half of what the clan's best ground does.
  const faction = factionOf(s, fromId);
  const share = CLAN_MAX[faction] ? baseAttack(s, fromId) / CLAN_MAX[faction] : 1;
  const reach = NODES.assaultFloorMin + (1 - NODES.assaultFloorMin) * Math.min(1, share);
  // Its own ground's attack, plus a share of what we have become, and the clan's mood over both.
  return Math.ceil((own + threat(s) * NODES.assaultThreatShare * reach) * stance);
}

/** True when an inbound assault is strong enough to take its target on the first breach. */
export function overrunRisk(s, atk) {
  return !!(atk && atk.assault && atk.strength >= factors(s).defense * NODES.overrunRatio);
}

// How fast assaults come: the keenest clan on my borders sets the pace for all of them.
function assaultTempo(s) {
  const profiles = clanProfiles(s);
  return borders(s, true).reduce((m, b) => Math.max(m, profiles[factionOf(s, b.from)].tempo), 1);
}

// Launches an assault along one border (rolled by weight unless given); returns it or null.
// Locked borders weigh little, so a front that only touches locked factions is rarely attacked.
// `delay` null means roll the warning and let the march across the map stretch it; a number is
// taken exactly, which is how the scripted beats and the tests set their own clock.
export function spawnAssault(s, pair = null, delay = null, force = 0) {
  let p = pair;
  if (!p) {
    const all = borders(s, true);
    const profiles = clanProfiles(s);
    // An open border out-weighs a sealed one many times over, so on its own a sealed clan can watch
    // for hours and never come. Every lockedEvery-th assault is reserved for one that borders us.
    s.assaultSeq = (s.assaultSeq || 0) + 1;
    // Whoever attacked last stands down for a while: one angry neighbour must not hold the whole
    // front while every other border waits its turn.
    const rested = all.filter((b) => s.playTime - ((s.nodes[b.from] && s.nodes[b.from].last) || -Infinity) >= NODES.reattackGap);
    const ready = rested.length ? rested : all;
    const sealed = ready.filter((b) => b.locked);
    const theirTurn = sealed.length > 0 && s.assaultSeq % NODES.lockedEvery === 0;
    const pool = theirTurn ? sealed : ready;
    // A locked clan comes rarely, rarer the further its chapter still is from opening, but stirring
    // one up (an order, an operation) counts for much more.
    const w = (b) => {
      const a = aggression(s, b.from);
      const from = SECTOR_BY_ID[b.from];
      const prof = profiles[factionOf(s, b.from)];
      const away = Math.max(0, chapterOf(from.chapter).core - level(s, 'core') - 1);
      const base = b.locked
        ? (NODES.lockedWeight / (1 + away * NODES.lockedFalloff)) * (1 + a * NODES.lockedAggrGain)
        : 1 + a;
      // Ground of theirs that I hold pulls them back to it.
      const back = SECTOR_BY_ID[b.target].faction === factionOf(s, b.from) ? prof.retake : 1;
      return Math.max(0, base) * prof.weight * back;
    };
    const total = pool.reduce((a, b) => a + w(b), 0);
    if (!total) return null;
    // On their turn they come whatever the odds would have been; otherwise a thin front often rolls
    // nothing at all, which is what keeps the map quiet while the player is still small.
    if (!theirTurn && rand(s, 'assault') >= Math.min(1, total)) return null;
    let r = rand(s, 'assault') * total;
    p = pool.find((b) => (r -= w(b)) <= 0) || pool[pool.length - 1];
  }
  const from = SECTOR_BY_ID[p.from];
  const locked = !sectorOpen(s, from);
  const strength = force || Math.ceil(assaultStrength(s, p.from) * range(s, NODES.spreadMin, NODES.spreadMax, 'assault'));
  const t = delay === null ? warning(s, range(s, NODES.warningMin, NODES.warningMax, 'assault'), p.from) : delay;
  node(s, p.from).last = s.playTime; // they have had their turn
  s.stats.assaults = (s.stats.assaults || 0) + 1;
  if (locked) s.stats.sealedAssaults = (s.stats.sealedAssaults || 0) + 1;
  s.assault = { faction: factionOf(s, p.from), strength, remaining: t, total: t, assault: true, from: p.from, target: p.target, locked };
  say(s, 'assaultSpotted', { from: from.name, target: SECTOR_BY_ID[p.target].name, strength, time: fmtShort(t) }, 'bad');
  return s.assault;
}

// The one scripted assault in the game: at AI Core 2 the clan comes back for the sector the opening
// directive handed the player, strong enough to overrun it on the first breach. It is how breaches,
// overruns and retaking are met, and it ends the soft defense on that sector (data/world.js).
export function scriptedRetake(s) {
  const sec = SECTOR_BY_ID[TUTORIAL.target];
  if (s.scripted.retake || level(s, 'core') < TUTORIAL.retakeCore) {
    return false;
  }
  // It needs to be in the player's hands, with ground of the clan's own next door to come from.
  const from = sec.links.find((l) => SECTOR_BY_ID[l].faction === sec.faction && !s.sectors.includes(l));
  if (!s.sectors.includes(sec.id) || !from) {
    s.scripted.retake = 2; // the window has passed: the sector is worth its real defense again
    return false;
  }
  if (s.assault) {
    return false; // one assault at a time; this one goes out at the next opening
  }
  s.scripted.retake = 1;
  const strength = Math.ceil(factors(s).defense * NODES.overrunRatio * TUTORIAL.retakeMargin);
  spawnAssault(s, { from, target: sec.id }, TUTORIAL.retakeDelay, strength);
  return true;
}

// Like raids: at most one lands per offline stretch (shared with raids).
export function advanceAssaults(s, dt, offline) {
  if (level(s, 'core') < NODES.startAtCore) return;
  // The scripted assault jumps the queue, and only while the player is there to watch it happen.
  if (!offline && scriptedRetake(s)) return;
  const a = s.assault;
  if (a && (!s.sectors.includes(a.target) || s.sectors.includes(a.from))) {
    s.assault = null; // the border it used no longer exists
  }
  if (!s.assault) {
    if (offline && s.offlineRaids >= s.offlineRaidCap) return;
    if (s.assaultTimer === null) s.assaultTimer = NODES.firstDelay;
    s.assaultTimer -= dt;
    if (s.assaultTimer <= 0) {
      s.assaultTimer = range(s, NODES.intervalMin, NODES.intervalMax, 'assault') / assaultTempo(s);
      spawnAssault(s);
    }
    return;
  }
  if (offline && s.offlineRaids >= s.offlineRaidCap) return;
  s.assault.remaining -= dt;
  if (s.assault.remaining > 1e-9) return;
  const atk = s.assault;
  s.assault = null;
  resolveAttack(s, atk, offline);
  if (offline) s.offlineRaids++;
}

/** Taking a sector can cost the clan more than the sector: if nobody next door could take it back
 *  alone, they may strip up to RALLY.donors of their own to make one sector strong enough to try,
 *  and that sector then wants it back more than anything. Their profile decides whether they do it
 *  at all, and losing a capital is what really moves them (data/world.js). */
/** The chance a clan that just lost ground pulls its other sectors together to take it back. */
export function rallyChance(s, faction, capital = false) {
  return Math.min(RALLY.maxChance, RALLY.base * clanProfile(s, faction).retake * (capital ? RALLY.capital : 1));
}

export function rallyClan(s, faction, lostId) {
  const lost = SECTOR_BY_ID[lostId];
  const mine = clanSectors(s, faction).filter((x) => !s.sectors.includes(x.id));
  if (!lost || mine.length < 2) {
    return null; // nothing left to move, or nothing to move it to
  }
  // Someone next door who could already assault it off us has no reason to ask anyone for help.
  const near = mine.filter((x) => x.links.includes(lostId));
  const need = factors(s).defense * RALLY.enough;
  if (near.some((x) => assaultStrength(s, x.id) >= need)) {
    return null;
  }
  if (rand(s, 'rally') >= rallyChance(s, faction, lost.boss)) {
    return null;
  }
  // Whoever is closest to it leads, and the strongest of the rest feed them.
  const lead = (near.length ? near : mine).slice().sort((a, b) => nodeStrength(s, b.id) - nodeStrength(s, a.id))[0];
  const donors = mine.filter((x) => x.id !== lead.id && nodeStrength(s, x.id) > NODES.strengthMin)
    .sort((a, b) => nodeStrength(s, b.id) - nodeStrength(s, a.id)).slice(0, RALLY.donors);
  if (!donors.length) {
    return null;
  }
  let moved = 0;
  for (const d of donors) {
    // What actually left: a donor is never stripped below the floor, and what it kept did not march.
    const had = nodeStrength(s, d.id);
    shiftStrength(s, d.id, -had * RALLY.take);
    moved += had - nodeStrength(s, d.id);
  }
  shiftStrength(s, lead.id, moved * RALLY.keep);
  node(s, lead.id).a = AGGR.max;
  say(s, 'clanRally', { faction: FACTIONS[faction].short, n: donors.length, lead: lead.name, lost: lost.name }, 'bad');
  return { lead: lead.id, donors: donors.map((x) => x.id), moved: Math.round(moved * RALLY.keep * 100) / 100 };
}

// A sector of yours falls to the attacker; its yields stop and it can be retaken.
// They hold it as hard as they took it, and the whole front around it is emboldened.
export function loseSector(s, id, faction, strength = 0) {
  s.sectors = s.sectors.filter((x) => x !== id);
  // Whoever walked in owns it now. If the map says it was someone else's, it is theirs anyway, and
  // they rebuild it to their own standard (baseDefense), so a weak sector in strong hands is no
  // longer a weak sector.
  if (FACTIONS[faction] && SECTOR_BY_ID[id].faction !== faction) {
    s.owner[id] = faction;
  }
  const held = strength ? clamp(strength / Math.max(1, baseDefense(s, id)), NODES.strengthMin, NODES.takenMax) : 1;
  s.nodes[id] = { m: held, marks: 0, a: 0, seen: 0 };
  if (id === TUTORIAL.target && s.scripted.retake === 1) {
    s.scripted.retake = 2; // the scripted loss landed: this sector is worth its real defense now
  }
  stirClan(s, faction, CLANS.stir.sectorSeized);
  stirAggression(s, id, AGGR.onFall);
  say(s, 'sectorLost', { sector: SECTOR_BY_ID[id].name, faction: FACTIONS[faction].name }, 'bad');
  // An operation marches out of ground we hold. With that ground gone it has nowhere to come from:
  // it turns back and what it cost comes back with it, rather than landing a sector cut off from us.
  const op = s.op && SECTOR_BY_ID[s.op.sector];
  if (op && !op.links.some((l) => s.sectors.includes(l))) {
    s.op = null;
    grant(s, opCost(op), false);
    say(s, 'opCut', { sector: op.name }, 'bad');
  }
}

// Stockpiles taken when something breaks through: a bigger share the worse the odds were.
function takeStock(s, chance, scale = 1) {
  const share = (RAIDS.lossMin + (RAIDS.lossMax - RAIDS.lossMin) * (1 - chance)) * scale;
  const lost = {};
  for (const k of Object.keys(s.res)) {
    lost[k] = Math.floor(s.res[k] * share);
    s.res[k] -= lost[k];
  }
  return lost;
}

function resolveAttack(s, raid, offline) {
  const defense = factors(s).defense;
  const chance = odds(defense, raid.strength, RAIDS.winSharpness);
  const roll = rand(s, 'battle');
  const name = attackName(raid);
  const prof = clanProfile(s, raid.faction);
  const report = { kind: 'raid', name, faction: raid.faction, strength: raid.strength, defense, chance, roll, win: roll < chance, offline: !!offline };
  Object.assign(report, { assault: !!raid.assault, from: raid.from, target: raid.target });
  // What went into the numbers, so a report can say why as well as what: facts, not advice.
  report.why = {
    troops: troopsOut(s) ? troopDefense(s) : 0, // Defense that was out on an operation
    stance: Math.round(prof.strength * 100) / 100, // the clan's mood on their strength (assaults)
    grudge: !!raid.grudge,
    rushed: !!raid.rush,
  };
  // Both sides bleed. Holding the line still costs people, and their dead stay dead.
  takeLosses(s, report, report.win ? RAIDS.winLoss : RAIDS.unitLoss, defense, raid.strength);
  report.theirLoss = Math.min(raid.strength, Math.round(raid.strength * (report.win ? RAIDS.enemyLoss.win : RAIDS.enemyLoss.loss) * prof.attrition));
  if (raid.assault) {
    shiftStrength(s, raid.from, -(report.win ? NODES.defendWinCut : NODES.breachCut) * prof.attrition);
    // Failing against the same wall makes them throw more at it next time, not less.
    if (report.win) {
      shiftPush(s, raid.from, NODES.pushPerLoss);
    } else {
      resetPush(s, raid.from);
    }
    report.strengthAfter = nodeStrength(s, raid.from);
    report.pushAfter = nodePush(s, raid.from);
  }
  const outcome = raid.assault ? (report.win ? 'assaultHeld' : 'assaultBreach') : (report.win ? 'raidHeld' : 'raidBroke');
  stirClan(s, raid.faction, CLANS.stir[outcome]);
  if (report.win) {
    const loot = { money: Math.ceil(rewardCurve(RAIDS.loot.money, raid.strength)) };
    grant(s, loot);
    report.loot = loot;
    s.stats.raidsWon++;
    if (raid.assault) s.stats.assaultsHeld = (s.stats.assaultsHeld || 0) + 1;
    say(s, 'raidWon', { raid: name, loot: loot.money }, 'good');
  } else if (raid.assault) {
    s.stats.raidsLost++;
    // The scripted retake has to take the ground, whatever mood the clan is in, or the lesson after it never comes.
    if (prof.plunder && !(s.scripted.retake === 1 && raid.target === TUTORIAL.target)) {
      // Plunderers want the stores, not the ground: they empty what they can carry and leave the walls.
      report.lost = takeStock(s, chance, NODES.plunderShare);
      report.plundered = true;
      say(s, 'assaultPlundered', { from: SECTOR_BY_ID[raid.from].name, target: SECTOR_BY_ID[raid.target].name }, 'bad');
    } else {
      const n = node(s, raid.target);
      n.marks = Math.min(n.marks + 1, NODES.breachesToFall);
      report.breaches = n.marks;
      // An assault this far over my defense does not need a second visit.
      report.overrun = raid.strength >= defense * NODES.overrunRatio;
      say(s, report.overrun ? 'assaultOverrun' : 'assaultBreached',
        { from: SECTOR_BY_ID[raid.from].name, target: SECTOR_BY_ID[raid.target].name, n: n.marks, max: NODES.breachesToFall,
          mult: (raid.strength / Math.max(1, defense)).toFixed(1) }, 'bad');
      if (report.overrun || n.marks >= NODES.breachesToFall) {
        loseSector(s, raid.target, raid.faction, raid.strength);
        report.fell = true;
        report.held = nodeStrength(s, raid.target);
      }
    }
  } else {
    report.lost = takeStock(s, chance);
    s.stats.raidsLost++;
    say(s, 'raidLost', { raid: name, target: SECTOR_BY_ID[raid.target].name }, 'bad');
    // A raid that far over our Defense does not just rob the place, it cracks it. The Nest never falls.
    if (raid.target !== MAP.home && s.sectors.includes(raid.target) && raid.strength >= defense * RAIDS.breachRatio) {
      const n = node(s, raid.target);
      n.marks = Math.min(n.marks + 1, NODES.breachesToFall);
      report.breaches = n.marks;
      say(s, 'raidBreached', { target: SECTOR_BY_ID[raid.target].name, n: n.marks, max: NODES.breachesToFall }, 'bad');
      if (n.marks >= NODES.breachesToFall) {
        loseSector(s, raid.target, raid.faction, raid.strength);
        report.fell = true;
        report.held = nodeStrength(s, raid.target);
      }
    }
    spawnAftermath(s, chance < EVENTS_CFG.routChance, offline);
  }
  if (raid.grudge) {
    settleGrudge(s, raid, report.win);
  }
  s.inbox.push(report);
}
