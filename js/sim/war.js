// War: operations against map sectors (your Power) and raids against you (your Defense).
import { SECTORS, FACTIONS, CHAPTERS, OPS, RAIDS, ALIGNMENT, EVENTS_CFG, MAP, BALANCE } from '../data.js';
import { spawnAftermath } from './story.js';
import {
  level, factors, threat, canAfford, pay, grant, loseUnits, say, caps,
} from './economy.js';
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

export function opLoot(sec) {
  return {
    money: Math.ceil(sec.defense * OPS.lootMoneyPerDefense),
    pop: Math.ceil(sec.defense * OPS.lootPopPerDefense),
  };
}

// Steps from home over the whole map; approaches of a sector are its neighbours closer to home
// (links leading further out are not approaches).
let DEPTH = null;
function depth() {
  if (!DEPTH) {
    DEPTH = { [MAP.home]: 0 };
    const queue = [MAP.home];
    while (queue.length) {
      const id = queue.shift();
      for (const l of SECTOR_BY_ID[id].links) {
        if (DEPTH[l] === undefined) {
          DEPTH[l] = DEPTH[id] + 1;
          queue.push(l);
        }
      }
    }
  }
  return DEPTH;
}

export function approaches(sec) {
  const d = depth();
  return sec.links.filter((l) => d[l] < d[sec.id]);
}

// Multi-route sectors are fortified: +50% defense while you hold only one approach, scaling down
// linearly to 0 when you hold every approach. Single-approach sectors never get it.
export function flank(s, sec) {
  const ins = approaches(sec);
  const held = ins.filter((l) => s.sectors.includes(l)).length;
  if (ins.length < 2) {
    return { approaches: ins.length, held, bonus: 0 };
  }
  const missing = Math.min(ins.length - 1, ins.length - held);
  return { approaches: ins.length, held, bonus: OPS.flankBonus * (missing / (ins.length - 1)) };
}

export function sectorDefense(s, sec) {
  return Math.ceil(sec.defense * (1 + flank(s, sec).bonus));
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
  if (!chapterOpen(s, sec.chapter)) {
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
  const roll = rand(s);
  const report = { kind: 'op', sector: sec.id, power, defense, chance, roll, win: roll < chance };
  if (report.win) {
    s.sectors.push(sec.id);
    const loot = opLoot(sec);
    grant(s, loot);
    report.loot = loot;
    s.stats.opsWon++;
    say(s, 'opWon', { sector: sec.name }, 'good');
    say(s, 'lore', { text: sec.lore }, 'story');
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
    report.units = casualties(s, OPS.unitLoss, power, defense);
    s.stats.opsLost++;
    say(s, 'opLost', { sector: sec.name }, 'bad');
    s.inbox.push(report);
  }
}

// Share of a unit tab lost in a defeat: base x (their strength / ours), capped. Even fights cost the base.
export function lossShare(rule, ours, theirs) {
  return Math.min(rule.cap, rule.base * (ours > 0 ? theirs / ours : Infinity));
}

function casualties(s, rules, ours, theirs) {
  const out = {};
  for (const [tab, rule] of Object.entries(rules)) {
    Object.assign(out, loseUnits(s, tab, lossShare(rule, ours, theirs)));
  }
  return out;
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

// Sieges are threats the player chose (or failed) to answer with force; they land at a fixed time.
export function attacks(s) {
  return [s.raid, ...(s.sieges || [])].filter(Boolean).sort((a, b) => a.remaining - b.remaining);
}

export const nextAttack = (s) => attacks(s)[0] || null;

export function attackName(atk) {
  if (atk.siege) return `${FACTIONS[atk.faction].short} siege`;
  if (atk.grudge) return `${FACTIONS[atk.faction].short} vengeance`;
  return FACTIONS[atk.faction].raidName;
}

// Revenge: the faction's next raids are multiplied, for a set count or until one breaks through.
export function addGrudge(s, g) {
  const untilLoss = rand(s) < EVENTS_CFG.grudgeUntilLossChance;
  const [lo, hi] = EVENTS_CFG.grudgeRaids;
  const left = untilLoss ? EVENTS_CFG.grudgeUntilLossMax : lo + Math.floor(rand(s) * (hi - lo + 1));
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

export function startSiege(s, faction, strength, delay) {
  const t = Math.max(0, delay);
  s.sieges.push({ faction, strength, remaining: t, total: Math.max(t, 1), siege: true });
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
  const faction = grudge ? grudge.faction : pick(s, raiders);
  const floor = raiders.includes(faction) ? FACTIONS[faction].raidFloor : Math.min(...raiders.map((f) => FACTIONS[f].raidFloor), FACTIONS[faction].raidFloor);
  const base = Math.max(floor, threat(s) * RAIDS.threatShare);
  const strength = Math.ceil(base * range(s, RAIDS.spreadMin, RAIDS.spreadMax) * (grudge ? grudge.mult : 1));
  s.raid = { faction, strength, remaining: delay, total: delay, grudge: !!grudge };
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
      spawnRaid(s, range(s, RAIDS.intervalMin, RAIDS.intervalMax));
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
  spawnRaid(s, range(s, RAIDS.intervalMin, RAIDS.intervalMax));
}

function resolveAttack(s, raid, offline) {
  const defense = factors(s).defense;
  const chance = odds(defense, raid.strength, RAIDS.winSharpness);
  const roll = rand(s);
  const name = attackName(raid);
  const report = { kind: 'raid', name, faction: raid.faction, strength: raid.strength, defense, chance, roll, win: roll < chance, offline: !!offline };
  if (report.win) {
    const loot = { money: Math.ceil(raid.strength * RAIDS.lootMoneyPerStrength) };
    grant(s, loot);
    report.loot = loot;
    s.stats.raidsWon++;
    say(s, 'raidWon', { raid: name, loot: loot.money }, 'good');
  } else {
    const share = RAIDS.lossMin + (RAIDS.lossMax - RAIDS.lossMin) * (1 - chance);
    const lost = {};
    for (const k of Object.keys(s.res)) {
      lost[k] = Math.floor(s.res[k] * share);
      s.res[k] -= lost[k];
    }
    report.lost = lost;
    report.units = casualties(s, RAIDS.unitLoss, defense, raid.strength);
    s.stats.raidsLost++;
    say(s, 'raidLost', { raid: name }, 'bad');
    spawnAftermath(s, chance < EVENTS_CFG.routChance);
  }
  if (raid.grudge) {
    settleGrudge(s, raid, report.win);
  }
  s.inbox.push(report);
}
