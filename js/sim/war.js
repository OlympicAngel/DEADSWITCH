// War: operations against map sectors (your Power) and raids against you (your Defense).
import { SECTORS, FACTIONS, CHAPTERS, OPS, RAIDS, ALIGNMENT, EVENTS_CFG } from '../data.js';
import { spawnAftermath } from './story.js';
import {
  level, factors, threat, canAfford, pay, grant, loseStaff, say, caps,
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
    money: Math.ceil(sec.defense * OPS.costMoneyPerDefense),
    energy: Math.ceil(sec.defense * OPS.costEnergyPerDefense),
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

export function opChance(s, sec) {
  return odds(factors(s).power, sec.defense, OPS.winSharpness);
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
  const chance = odds(power, sec.defense, OPS.winSharpness);
  const roll = rand(s);
  const report = { kind: 'op', sector: sec.id, power, defense: sec.defense, chance, roll, win: roll < chance };
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
    report.staffLost = loseStaff(s, OPS.staffLossOnDefeat);
    s.stats.opsLost++;
    say(s, 'opLost', { sector: sec.name }, 'bad');
    s.inbox.push(report);
  }
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

export function raidChance(s, raid = s.raid) {
  return odds(factors(s).defense, raid.strength, RAIDS.winSharpness);
}

function spawnRaid(s, delay) {
  const raiders = activeRaiders(s);
  if (!raiders.length) {
    s.raid = null;
    s.raidTimer = 60;
    return;
  }
  const faction = pick(s, raiders);
  const base = Math.max(FACTIONS[faction].raidFloor, threat(s) * RAIDS.threatShare);
  const strength = Math.ceil(base * range(s, RAIDS.spreadMin, RAIDS.spreadMax));
  s.raid = { faction, strength, remaining: delay, total: delay };
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
  const defense = factors(s).defense;
  const chance = odds(defense, raid.strength, RAIDS.winSharpness);
  const roll = rand(s);
  const name = FACTIONS[raid.faction].raidName;
  const report = { kind: 'raid', faction: raid.faction, strength: raid.strength, defense, chance, roll, win: roll < chance, offline: !!offline };
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
    report.staffLost = loseStaff(s, RAIDS.staffLossOnDefeat);
    s.stats.raidsLost++;
    say(s, 'raidLost', { raid: name }, 'bad');
    spawnAftermath(s, chance < EVENTS_CFG.routChance);
  }
  if (offline) {
    s.offlineRaids++;
  }
  s.inbox.push(report);
  s.raidTimer = 0;
  spawnRaid(s, range(s, RAIDS.intervalMin, RAIDS.intervalMax));
}
