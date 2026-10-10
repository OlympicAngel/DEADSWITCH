// Clan profiles: four traits per faction, moved by everything that happens between us, read through
// the CLANS.stances matrix into the stances that hold right now. Their effects multiply, so a clan's
// behaviour is always the combination (furious and broken reads differently from furious and whole).
import { CLANS, FACTIONS, SECTORS, CHAPTERS } from '../data.js';
import { level, say } from './economy.js';

const TRAITS = ['fury', 'fear', 'order', 'greed'];
const MULTS = ['weight', 'retake', 'strength', 'tempo', 'growth', 'support', 'raid', 'attrition'];
const STANCE_BY_ID = Object.fromEntries(CLANS.stances.map((x) => [x.id, x]));
const CHAPTER_OF = Object.fromEntries(CHAPTERS.map((c) => [c.faction, c]));
const SECTORS_OF = {};
const CAPITAL_OF = {};
for (const sec of SECTORS) {
  if (!sec.faction) continue;
  (SECTORS_OF[sec.faction] = SECTORS_OF[sec.faction] || []).push(sec.id);
  if (sec.boss) CAPITAL_OF[sec.faction] = sec.id;
}

const clamp01 = (v) => Math.min(1, Math.max(0, v));
export const stanceById = (id) => STANCE_BY_ID[id];

/** A clan's live traits, started from its faction's own baseline mood the first time it is asked for. */
export function clan(s, faction) {
  if (!s.clans[faction]) {
    s.clans[faction] = { ...FACTIONS[faction].mood, stances: [] };
  }
  return s.clans[faction];
}

/** Clans the player can see and be raided by: their chapter is open (Rival Cores wake with the ending). */
export const clanAwake = (s, f) => (CHAPTER_OF[f] ? level(s, 'core') >= CHAPTER_OF[f].core : !!s.ending);

export function stirClan(s, faction, deltas, scale = 1) {
  if (!FACTIONS[faction] || !deltas) return;
  const c = clan(s, faction);
  for (const k of TRAITS) {
    if (deltas[k]) c[k] = clamp01(c[k] + deltas[k] * scale);
  }
}

/** Traits, plus the two facts about the map that the matrix reads like traits. */
export function clanContext(s, faction) {
  const c = clan(s, faction);
  // What they hold now: their own ground, less anything another clan has taken off us since, plus
  // whatever they have taken that was never on their map.
  const owner = s.owner || {};
  const own = (SECTORS_OF[faction] || []).filter((id) => !owner[id])
    .concat(Object.keys(owner).filter((id) => owner[id] === faction));
  const cap = CAPITAL_OF[faction];
  return {
    fury: c.fury, fear: c.fear, order: c.order, greed: c.greed,
    held: own.length ? own.filter((id) => s.sectors.includes(id)).length / own.length : 0,
    capital: cap && s.sectors.includes(cap) ? 1 : 0,
  };
}

const inRange = (v, [lo, hi = 1]) => v >= lo && v <= hi;

/** Every stance whose cell of the matrix the clan currently sits in. */
export function stanceList(s, faction) {
  const ctx = clanContext(s, faction);
  return CLANS.stances.filter((st) => Object.entries(st.when).every(([k, range]) => inRange(ctx[k], range)));
}

/** The combined profile: every active stance's numbers multiplied together. */
export function clanProfile(s, faction) {
  const out = { weight: 1, retake: 1, strength: 1, tempo: 1, growth: 1, support: 1, raid: 1, attrition: 1, plunder: false, stances: [] };
  for (const st of stanceList(s, faction)) {
    out.stances.push(st);
    for (const k of MULTS) {
      if (st[k]) out[k] *= st[k];
    }
    if (st.plunder) out.plunder = true;
  }
  return out;
}

/** Profiles for every faction at once, so a loop over sectors costs one pass. */
export function clanProfiles(s) {
  return Object.fromEntries(Object.keys(FACTIONS).map((f) => [f, clanProfile(s, f)]));
}

// A stance that changes my economy holds a buff for as long as it holds (buffs with `hold` never tick down).
function syncBuffs(s, faction, ids) {
  s.buffs = s.buffs.filter((b) => b.clan !== faction || ids.includes(b.stance));
  for (const id of ids) {
    const st = STANCE_BY_ID[id];
    if (st.buff && !s.buffs.some((b) => b.clan === faction && b.stance === id)) {
      s.buffs.push({ ...st.buff, label: `${FACTIONS[faction].short} ${st.name.toLowerCase()}`, remaining: 0, hold: true, clan: faction, stance: id });
    }
  }
}

export function advanceClans(s, dt) {
  const drift = (CLANS.driftPerHour * dt) / 3600;
  for (const [f, fac] of Object.entries(FACTIONS)) {
    const c = clan(s, f);
    for (const k of TRAITS) {
      const d = fac.mood[k] - c[k];
      c[k] = Math.abs(d) <= drift ? fac.mood[k] : c[k] + Math.sign(d) * drift;
    }
    const now = stanceList(s, f).map((x) => x.id);
    const announce = clanAwake(s, f) && level(s, 'core') >= CLANS.startAtCore;
    for (const id of now) {
      if (!c.stances.includes(id) && announce) say(s, 'stance', { text: STANCE_BY_ID[id].line.replace('{faction}', fac.short) }, 'bad');
    }
    for (const id of c.stances) {
      if (!now.includes(id) && announce) say(s, 'stanceEnd', { faction: fac.short, stance: STANCE_BY_ID[id].name.toLowerCase() });
    }
    c.stances = now;
    syncBuffs(s, f, now);
  }
}
