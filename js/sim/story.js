// Story systems: choice events, directives, chapters, alignment and timed effects.
import {
  EVENTS, EVENTS_CFG, DIRECTIVES, CHAPTERS, CHAPTER_TEXT, ALIGNMENT,
} from '../data.js';
import {
  level, factors, threat, grossRate, canAfford, pay, grant, giveItems, owned, say, caps,
} from './economy.js';
import { delayRaid } from './war.js';
import { range, pick } from './rng.js';

const EVENT_BY_ID = Object.fromEntries(EVENTS.map((e) => [e.id, e]));
export const eventById = (id) => EVENT_BY_ID[id];
const RECENT_EVENTS = 6;

// ---------- events ----------

// Converts "seconds of production" into units, with a floor so early events still matter.
export function eventValue(s, r, seconds) {
  const floor = EVENTS_CFG.minRatePerCore[r] * Math.max(1, level(s, 'core'));
  return Math.ceil(seconds * Math.max(grossRate(s, r), floor));
}

function scaled(s, obj) {
  const out = {};
  for (const [r, sec] of Object.entries(obj || {})) {
    out[r] = eventValue(s, r, sec);
  }
  return out;
}

// Concrete numbers for one choice, for both the UI preview and resolution.
export function choiceOutcome(s, choice) {
  const lose = {};
  for (const [r, share] of Object.entries(choice.lose || {})) {
    lose[r] = Math.floor(s.res[r] * share);
  }
  const gain = scaled(s, choice.gain);
  const c = caps(s);
  for (const r of Object.keys(gain)) {
    gain[r] = Math.min(gain[r], Math.floor(c[r]));
  }
  return { cost: scaled(s, choice.cost), gain, lose };
}

export function canChoose(s, choice) {
  return canAfford(s, choiceOutcome(s, choice).cost);
}

export function advanceEvents(s, dt) {
  if (s.event) {
    return;
  }
  s.eventTimer -= dt;
  if (s.eventTimer > 0) {
    return;
  }
  const core = level(s, 'core');
  const pool = EVENTS.filter((e) => e.minCore <= core && !s.recentEvents.includes(e.id));
  s.eventTimer = range(s, EVENTS_CFG.intervalMin, EVENTS_CFG.intervalMax);
  if (!pool.length) {
    return;
  }
  const ev = pick(s, pool);
  s.event = ev.id;
  s.recentEvents.push(ev.id);
  if (s.recentEvents.length > RECENT_EVENTS) {
    s.recentEvents.shift();
  }
  s.fx.push({ kind: 'event', id: ev.id });
}

export function resolveEvent(s, index) {
  const ev = EVENT_BY_ID[s.event];
  const choice = ev && ev.choices[index];
  if (!choice || !canChoose(s, choice)) {
    return null;
  }
  const out = choiceOutcome(s, choice);
  pay(s, out.cost);
  grant(s, out.gain);
  for (const [r, v] of Object.entries(out.lose)) {
    s.res[r] -= v;
  }
  if (choice.items) {
    giveItems(s, choice.items);
  }
  if (choice.align) {
    s.align = Math.max(ALIGNMENT.min, Math.min(ALIGNMENT.max, s.align + choice.align));
  }
  if (choice.buff) {
    s.buffs.push({ ...choice.buff, remaining: choice.buff.duration });
  }
  if (choice.raidDelay) {
    delayRaid(s, choice.raidDelay);
  }
  s.event = null;
  s.stats.events++;
  say(s, 'event', { title: ev.title, result: choice.result }, 'story');
  return { ev, choice, out };
}

export function alignmentLabel(a) {
  if (a >= ALIGNMENT.guardianAt) {
    return 'Guardian';
  }
  if (a <= ALIGNMENT.overlordAt) {
    return 'Overlord';
  }
  return 'Undecided';
}

// ---------- timed effects ----------

export function advanceBuffs(s, dt) {
  for (const b of s.buffs) {
    b.remaining -= dt;
    if (b.remaining <= 0) {
      say(s, 'buffEnd', { label: b.label });
    }
  }
  s.buffs = s.buffs.filter((b) => b.remaining > 0);
}

// ---------- directives ----------

export function directiveProgress(s, d) {
  const c = d.cond;
  if (c.level) {
    return [level(s, c.level), c.n];
  }
  if (c.item) {
    return [owned(s, c.item), c.n];
  }
  if (c.factor) {
    return [factors(s)[c.factor], c.n];
  }
  if (c.sector) {
    return [s.sectors.includes(c.sector) ? 1 : 0, 1];
  }
  if (c.raidsWon) {
    return [s.stats.raidsWon, c.raidsWon];
  }
  if (c.threat) {
    return [threat(s), c.threat];
  }
  return [0, 1];
}

export const currentDirective = (s) => DIRECTIVES[s.directive] || null;

export function checkDirectives(s) {
  const d = currentDirective(s);
  if (!d) {
    return;
  }
  const [have, need] = directiveProgress(s, d);
  if (have < need) {
    return;
  }
  grant(s, d.reward);
  s.directive++;
  say(s, 'directive', { text: d.text }, 'good');
  s.fx.push({ kind: 'directive', text: d.text, reward: d.reward });
}

// ---------- chapters ----------

export function checkChapters(s) {
  for (const ch of CHAPTERS) {
    if (ch.id > s.chapter && level(s, 'core') >= ch.core) {
      s.chapter = ch.id;
      say(s, 'chapter', { kicker: CHAPTER_TEXT[ch.id].kicker, title: ch.title }, 'rank');
      s.inbox.push({ kind: 'chapter', id: ch.id });
    }
  }
}
