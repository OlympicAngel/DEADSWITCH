// War: operations against map sectors (your Power) and raids against you (your Defense).
import { SECTORS, FACTIONS, CHAPTERS, OPS, RAIDS, NODES, AGGR, CLANS, ALIGNMENT, EVENTS_CFG, MAP, BALANCE } from '../data.js';
import { spawnAftermath } from './story.js';
import {
  level, factors, threat, canAfford, pay, grant, loseUnits, lossValue, say, caps, rewardCurve,
} from './economy.js';
import { clanProfile, clanProfiles, stirClan } from './clans.js';
import { rand, range, pick, odds } from './rng.js';

const SECTOR_BY_ID = Object.fromEntries(SECTORS.map((x) => [x.id, x]));
export const sectorById = (id) => SECTOR_BY_ID[id];

export const chapterOf = (id) => CHAPTERS.find((c) => c.id === id);

export function chapterOpen(s, chapterId) {
  return level(s, 'core') >= chapterOf(chapterId).core;
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

// Capture loot, paid the first time a sector falls to you (retaking a lost sector pays nothing).
export function opLoot(sec) {
  const out = {};
  for (const [k, curve] of Object.entries(OPS.loot)) {
    out[k] = Math.ceil(rewardCurve(curve, sec.defense));
  }
  return out;
}

// ---------- living sectors ----------

const node = (s, id) => s.nodes[id] || (s.nodes[id] = { m: 1, marks: 0, a: 0, seen: 0 });
const clamp = (v, lo, hi) => Math.min(hi, Math.max(lo, v));

// Strength multiplier of a sector you do not hold (1 = its base defense).
export const nodeStrength = (s, id) => (s.nodes[id] ? s.nodes[id].m : 1);

// Breached assaults a held sector has taken (it falls at NODES.breachesToFall).
export const breaches = (s, id) => (s.nodes[id] ? s.nodes[id].marks : 0);

export function shiftStrength(s, id, delta) {
  const n = node(s, id);
  n.m = Math.min(NODES.strengthMax, Math.max(NODES.strengthMin, n.m + delta));
}

// Enemy sectors in open chapters slowly rebuild on their own, up to the hard ceiling, and every
// sector's anger at you cools off while you leave it alone.
export function advanceNodes(s, dt) {
  const grow = (NODES.growthPerHour * dt) / 3600;
  const profiles = clanProfiles(s);
  for (const sec of SECTORS) {
    if (sec.faction && !s.sectors.includes(sec.id) && chapterOpen(s, sec.chapter) && nodeStrength(s, sec.id) < NODES.strengthMax) {
      shiftStrength(s, sec.id, grow * profiles[sec.faction].growth);
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
  const ratio = factors(s).power / Math.max(1, sectorDefense(s, sec));
  return AGGR.pressure * clamp((ratio - AGGR.pressureFrom) / (1 - AGGR.pressureFrom), 0, 1);
}

export function shiftAggression(s, id, delta) {
  if (!SECTOR_BY_ID[id] || !SECTOR_BY_ID[id].faction || s.sectors.includes(id)) return;
  const n = node(s, id);
  n.a = clamp((n.a || 0) + delta, AGGR.min, AGGR.max);
}

/** Spreads a nudge from one sector: itself, every sector of its clan, and the sectors it links to. */
export function stirAggression(s, id, spread) {
  const sec = SECTOR_BY_ID[id];
  if (!sec) return;
  if (spread.node) shiftAggression(s, id, spread.node);
  if (spread.clan && sec.faction) {
    for (const x of SECTORS) {
      if (x.faction === sec.faction && x.id !== id) shiftAggression(s, x.id, spread.clan);
    }
  }
  if (spread.near) {
    for (const l of sec.links) shiftAggression(s, l, spread.near);
  }
}

// Opening a sector's briefing tells it someone is looking. The first look counts most.
export function inspectSector(s, id) {
  if (!SECTOR_BY_ID[id] || !SECTOR_BY_ID[id].faction || s.sectors.includes(id)) return;
  const n = node(s, id);
  n.seen = (n.seen || 0) + 1;
  shiftAggression(s, id, AGGR.onInspect / n.seen);
}

// Clan support: a sector is fortified by the sectors of its own faction it links to. +flankBonus while
// every one of them is still theirs, falling linearly to 0 as you take them. Links to other factions
// (or to your own sectors through another faction) give nothing; an outpost alone among strangers gets none.
export function flank(s, sec) {
  const clan = sec.faction ? sec.links.filter((l) => SECTOR_BY_ID[l].faction === sec.faction) : [];
  const held = clan.filter((l) => s.sectors.includes(l)).length;
  if (!clan.length) {
    return { approaches: 0, held: 0, bonus: 0 };
  }
  const support = OPS.flankBonus * clanProfile(s, sec.faction).support;
  return { approaches: clan.length, held, bonus: support * ((clan.length - held) / clan.length) };
}

export function sectorDefense(s, sec) {
  return Math.ceil(sec.defense * nodeStrength(s, sec.id) * (1 + flank(s, sec).bonus));
}

export function opChance(s, sec) {
  return odds(factors(s).power, sectorDefense(s, sec), OPS.winSharpness);
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
  if (!chapterOpen(s, sec.chapter) && !s.taken.includes(sec.id)) {
    return 'locked';
  }
  return 'target';
}

export function canLaunch(s, sec) {
  return !s.op && sectorStatus(s, sec) === 'target' && canAfford(s, opCost(sec)) && !Object.keys(opCost(sec)).some((k) => opCost(sec)[k] > caps(s)[k]);
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
  const power = factors(s).power;
  const defense = sectorDefense(s, sec);
  const chance = odds(power, defense, OPS.winSharpness);
  const roll = rand(s, 'op');
  const report = { kind: 'op', sector: sec.id, power, defense, chance, roll, win: roll < chance };
  // Taking ground costs people too; the garrison that held it is gone either way.
  takeLosses(s, report, report.win ? OPS.winLoss : OPS.unitLoss, power, defense);
  if (report.win) {
    report.theirLoss = defense;
    stirClan(s, sec.faction, CLANS.stir[sec.boss ? 'capitalTaken' : 'sectorTaken']);
    s.sectors.push(sec.id);
    delete s.nodes[sec.id];
    // Loot and memories only the first time; a retaken sector just comes back.
    const first = !s.taken.includes(sec.id);
    const loot = first ? opLoot(sec) : {};
    if (first) s.taken.push(sec.id);
    grant(s, loot);
    report.loot = loot;
    report.retaken = !first;
    s.stats.opsWon++;
    say(s, first ? 'opWon' : 'opRetaken', { sector: sec.name }, 'good');
    if (first) say(s, 'lore', { text: sec.lore }, 'story');
    if (sec.boss) {
      say(s, 'bossDown', { faction: FACTIONS[sec.faction].name }, 'rank');
      if (s.raid && s.raid.faction === sec.faction) {
        s.raid = null;
        s.raidTimer = 60;
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
    stirClan(s, sec.faction, CLANS.stir.opHeld);
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
  if (s.ending) {
    list.push('rogue');
  }
  return list;
}

// Where an attack comes from: a sector of that faction, the frontier first so it is somewhere the
// player can see, and the keenest of those. A faction with no ground left attacks from nowhere.
export function attackSource(s, faction) {
  const own = SECTORS.filter((x) => x.faction === faction && !s.sectors.includes(x.id));
  const front = own.filter((x) => x.links.some((l) => s.sectors.includes(l)));
  const pool = front.length ? front : own;
  if (!pool.length) return null;
  const w = (x) => 1 + aggression(s, x.id) - AGGR.min;
  let r = rand(s, 'source') * pool.reduce((a, x) => a + w(x), 0);
  return (pool.find((x) => (r -= w(x)) <= 0) || pool[pool.length - 1]).id;
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
  const t = Math.max(0, delay);
  s.sieges.push({ faction, strength, remaining: t, total: Math.max(t, 1), siege: true, uid, from: attackSource(s, faction), target: MAP.home });
}

export function advanceSieges(s, dt, offline) {
  for (const sg of s.sieges.slice()) {
    sg.remaining -= dt;
    if (sg.remaining <= 1e-9) {
      s.sieges = s.sieges.filter((x) => x !== sg);
      resolveAttack(s, sg, offline);
    }
  }
}

export function raidChance(s, raid = s.raid) {
  return odds(factors(s).defense, raid.strength, RAIDS.winSharpness);
}

function spawnRaid(s, delay) {
  const raiders = activeRaiders(s);
  const grudge = (s.grudges || [])[0];
  if (!raiders.length && !grudge) {
    s.raid = null;
    s.raidTimer = 60;
    return;
  }
  // A faction with a grudge takes the next raid, active chapter or not, and hits harder.
  const faction = grudge ? grudge.faction : pick(s, raiders, 'raid');
  const floor = raiders.includes(faction) ? FACTIONS[faction].raidFloor : Math.min(...raiders.map((f) => FACTIONS[f].raidFloor), FACTIONS[faction].raidFloor);
  const base = Math.max(floor, threat(s) * RAIDS.threatShare);
  const strength = Math.ceil(base * range(s, RAIDS.spreadMin, RAIDS.spreadMax, 'raid') * (grudge ? grudge.mult : 1) * clanProfile(s, faction).raid);
  // Raids march on the Nest itself, from wherever that faction still holds ground.
  s.raid = { faction, strength, remaining: delay, total: delay, grudge: !!grudge, from: attackSource(s, faction), target: MAP.home };
  say(s, 'raidSpotted', { raid: FACTIONS[faction].raidName, strength, time: fmtShort(delay) }, 'bad');
}

function fmtShort(sec) {
  const m = Math.round(sec / 60);
  return m >= 1 ? m + ' min' : Math.round(sec) + 's';
}

// Positive pushes the next raid back; negative brings it closer (never under a minute of warning).
export function delayRaid(s, seconds) {
  if (s.raid) {
    const before = s.raid.remaining;
    s.raid.remaining = Math.max(Math.min(before, 60), before + seconds);
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
  if (offline && s.offlineRaids >= 1) {
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
      if (!from.faction || s.sectors.includes(l)) continue;
      const locked = !chapterOpen(s, from.chapter);
      if (!locked || withLocked) out.push({ from: l, target: id, locked });
    }
  }
  return out;
}

export function assaultStrength(s, fromId) {
  const sec = SECTOR_BY_ID[fromId];
  const own = sec.defense * nodeStrength(s, fromId) * NODES.assaultShare * clanProfile(s, sec.faction).strength;
  return Math.ceil(chapterOpen(s, sec.chapter) ? own : Math.min(own, Math.max(FACTIONS.scav.raidFloor, threat(s) * RAIDS.threatShare) * NODES.lockedCap));
}

/** True when an inbound assault is strong enough to take its target on the first breach. */
export function overrunRisk(s, atk) {
  return !!(atk && atk.assault && atk.strength >= factors(s).defense * NODES.overrunRatio);
}

// How fast assaults come: the keenest clan on my borders sets the pace for all of them.
function assaultTempo(s) {
  const profiles = clanProfiles(s);
  return borders(s, true).reduce((m, b) => Math.max(m, profiles[SECTOR_BY_ID[b.from].faction].tempo), 1);
}

// Launches an assault along one border (rolled by weight unless given); returns it or null.
// Locked borders weigh little, so a front that only touches locked factions is rarely attacked.
export function spawnAssault(s, pair = null, delay = range(s, NODES.warningMin, NODES.warningMax, 'assault')) {
  let p = pair;
  if (!p) {
    const all = borders(s, true);
    const profiles = clanProfiles(s);
    // A locked clan comes rarely, but stirring one up (an order, an operation) counts for much more.
    const w = (b) => {
      const a = aggression(s, b.from);
      const from = SECTOR_BY_ID[b.from];
      const prof = profiles[from.faction];
      const base = b.locked ? NODES.lockedWeight * (1 + a * NODES.lockedAggrGain) : 1 + a;
      // Ground of theirs that I hold pulls them back to it.
      const back = SECTOR_BY_ID[b.target].faction === from.faction ? prof.retake : 1;
      return Math.max(0, base) * prof.weight * back;
    };
    const total = all.reduce((a, b) => a + w(b), 0);
    if (!total || rand(s, 'assault') >= Math.min(1, total)) return null;
    let r = rand(s, 'assault') * total;
    p = all.find((b) => (r -= w(b)) <= 0) || all[all.length - 1];
  }
  const from = SECTOR_BY_ID[p.from];
  const locked = !chapterOpen(s, from.chapter);
  const strength = Math.ceil(assaultStrength(s, p.from) * range(s, NODES.spreadMin, NODES.spreadMax, 'assault'));
  s.assault = { faction: from.faction, strength, remaining: delay, total: delay, assault: true, from: p.from, target: p.target, locked };
  say(s, 'assaultSpotted', { from: from.name, target: SECTOR_BY_ID[p.target].name, strength, time: fmtShort(delay) }, 'bad');
  return s.assault;
}

// Like raids: at most one lands per offline stretch (shared with raids).
export function advanceAssaults(s, dt, offline) {
  if (level(s, 'core') < NODES.startAtCore) return;
  const a = s.assault;
  if (a && (!s.sectors.includes(a.target) || s.sectors.includes(a.from))) {
    s.assault = null; // the border it used no longer exists
  }
  if (!s.assault) {
    if (offline && s.offlineRaids >= 1) return;
    if (s.assaultTimer === null) s.assaultTimer = NODES.firstDelay;
    s.assaultTimer -= dt;
    if (s.assaultTimer <= 0) {
      s.assaultTimer = range(s, NODES.intervalMin, NODES.intervalMax, 'assault') / assaultTempo(s);
      spawnAssault(s);
    }
    return;
  }
  if (offline && s.offlineRaids >= 1) return;
  s.assault.remaining -= dt;
  if (s.assault.remaining > 1e-9) return;
  const atk = s.assault;
  s.assault = null;
  resolveAttack(s, atk, offline);
  if (offline) s.offlineRaids++;
}

// A sector of yours falls to the attacker; its yields stop and it can be retaken.
// They hold it as hard as they took it, and the whole front around it is emboldened.
export function loseSector(s, id, faction, strength = 0) {
  s.sectors = s.sectors.filter((x) => x !== id);
  const held = strength ? clamp(strength / Math.max(1, SECTOR_BY_ID[id].defense), NODES.strengthMin, NODES.takenMax) : 1;
  s.nodes[id] = { m: held, marks: 0, a: 0, seen: 0 };
  stirClan(s, faction, CLANS.stir.sectorSeized);
  stirAggression(s, id, AGGR.onFall);
  if (s.op && s.op.sector === id) s.op = null;
  say(s, 'sectorLost', { sector: SECTOR_BY_ID[id].name, faction: FACTIONS[faction].name }, 'bad');
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
  // Both sides bleed. Holding the line still costs people, and their dead stay dead.
  takeLosses(s, report, report.win ? RAIDS.winLoss : RAIDS.unitLoss, defense, raid.strength);
  report.theirLoss = Math.round(raid.strength * (report.win ? RAIDS.enemyLoss.win : RAIDS.enemyLoss.loss) * prof.attrition);
  if (raid.assault) {
    shiftStrength(s, raid.from, -(report.win ? NODES.defendWinCut : NODES.breachCut) * prof.attrition);
    report.strengthAfter = nodeStrength(s, raid.from);
  }
  const outcome = raid.assault ? (report.win ? 'assaultHeld' : 'assaultBreach') : (report.win ? 'raidHeld' : 'raidBroke');
  stirClan(s, raid.faction, CLANS.stir[outcome]);
  if (report.win) {
    const loot = { money: Math.ceil(rewardCurve(RAIDS.loot.money, raid.strength)) };
    grant(s, loot);
    report.loot = loot;
    s.stats.raidsWon++;
    say(s, 'raidWon', { raid: name, loot: loot.money }, 'good');
  } else if (raid.assault) {
    s.stats.raidsLost++;
    if (prof.plunder) {
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
    say(s, 'raidLost', { raid: name }, 'bad');
    spawnAftermath(s, chance < EVENTS_CFG.routChance);
  }
  if (raid.grudge) {
    settleGrudge(s, raid, report.win);
  }
  s.inbox.push(report);
}
