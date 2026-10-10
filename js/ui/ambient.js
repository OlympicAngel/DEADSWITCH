// The music under the game. It is not a loop. A mood is read off the state every second (what is
// inbound and how close, what is pending, what is being built, how full the stores are) and the bed
// answers it, but within a mood nothing repeats for long either: the harmony walks a progression a
// bar at a time, the rhythm steps through several patterns, and a mood that has held for a while
// quietly thins out, because an alarm that never changes stops being an alarm. Runs on the music
// bus (js/ui/audio.js), stops whenever the page does.
import * as E from '../engine.js';
import { RESOURCES } from '../data.js';
import { audio, startPad, stopPad, padChord, voice, air, volume, ramp } from './audio.js';

const BAR = 16; // sixteen eighths: two bars of four, which is how long a pattern runs
const STEP = 60 / 74 / 2; // an eighth at 74 BPM, the tempo the opening is scored at
const LOOKAHEAD = 0.35;
const FADE_AFTER = 45; // seconds in one mood before it starts stepping back
const FADE_FLOOR = 0.45; // how far down it steps: a long siege sits at this much of its weight

// Chords climb with the chapter, so the world grows as it opens up.
const ROOTS = [55, 61.7, 65.4, 73.4]; // A, B, C, D
const semitone = (root, n) => root * Math.pow(2, n / 12);

// Each mood: a progression walked one chord a bar, patterns stepped through a bar at a time, and
// how loud and bright it sits. In a pattern `K` is the low hit, `t` a tick, `o` an off-beat accent.
const MOODS = {
  calm: {
    chords: [[0, 7, 12], [0, 5, 12], [-2, 7, 10], [0, 7, 14]],
    bars: ['K.......t.......', 'K...........t...', 'K.......t...t...'],
    gain: 0.035, cut: 480, hit: 0.055,
  },
  work: {
    chords: [[0, 7, 12, 19], [0, 5, 12, 17], [3, 10, 15, 22], [0, 7, 12, 19]],
    bars: ['K...t...K...t...', 'K...t...K..tt...', 'K..t.t..K...t..o'],
    gain: 0.042, cut: 700, hit: 0.06,
  },
  push: {
    chords: [[0, 5, 12, 17], [-2, 5, 10, 17], [0, 7, 12, 19], [-4, 3, 8, 15]],
    bars: ['K..t.K..t...K.t.', 'K.t.K...t.K.t..o', 'K...K..t.t..K..t'],
    gain: 0.05, cut: 900, hit: 0.062,
  },
  alert: {
    chords: [[0, 3, 10], [0, 3, 8], [-1, 3, 10], [0, 2, 9]],
    bars: ['K..o.t..K..o.t..', 'K.t..o..K.t..o.t', 'K..t..o.K.o..t..'],
    gain: 0.05, cut: 1000, hit: 0.07,
  },
  danger: {
    // Two notes a semitone apart under a tritone: it does not resolve, and it does not have to.
    chords: [[0, 1, 6, 12], [0, 1, 6, 13], [-1, 1, 6, 12], [0, 1, 5, 12]],
    bars: ['K.oK..t.K.oK.t.o', 'K.K..o.tK..oK.t.', 'Ko.K.t.oK.K..o.t'],
    gain: 0.058, cut: 1300, hit: 0.075,
  },
  crisis: {
    chords: [[0, 6, 11], [-1, 6, 11], [0, 6, 13], [1, 6, 11]],
    bars: ['KoKo.t.oKo.tK.o.', 'K.KoKo.tK.oKo.t.', 'KoK.o.tKK.oK.ot.'],
    gain: 0.062, cut: 1500, hit: 0.08,
  },
};

let bed = null;

/** What the state sounds like right now. The first match wins, worst first. Pure: the developer
 * panel reads it whether or not the music is playing. */
export function moodOf(s) {
  const atk = E.nextAttack(s);
  if (s.events.some((x) => E.eventById(x.id).aftermath) || (atk && E.overrunRisk(s, atk))) {
    return 'crisis';
  }
  if (atk) {
    return E.raidChance(s, atk) < 0.5 ? 'danger' : 'alert';
  }
  if (s.events.length) {
    return 'push';
  }
  return s.op || s.build ? 'work' : 'calm';
}

/** 0 to 1: how close the nearest attack is, which tightens the rhythm as it comes in. */
function tension(s) {
  const atk = E.nextAttack(s);
  if (!atk || !atk.total) {
    return 0;
  }
  return Math.max(0, Math.min(1, 1 - atk.remaining / Math.min(atk.total, 600)));
}

/** Stores at the brim shimmer: the bed answers what the player has let pile up. */
function sparkle(s) {
  const caps = E.caps(s);
  const full = Object.keys(RESOURCES).filter((r) => Number.isFinite(caps[r]) && s.res[r] >= caps[r] * 0.995).length;
  return Math.min(1, full / 2);
}

function read(s) {
  const id = moodOf(s);
  return {
    id,
    mood: MOODS[id],
    root: ROOTS[Math.min(ROOTS.length - 1, Math.max(0, s.chapter - 1))],
    tension: tension(s),
    shimmer: sparkle(s),
  };
}

/** How much weight the bed has: it steps back the longer one mood holds, and leans in as an
 *  attack closes. Without the first half, a long siege is just a loud loop. */
function weight() {
  const held = (bed.ctx.currentTime - bed.since) - FADE_AFTER;
  const tired = held <= 0 ? 1 : Math.max(FADE_FLOOR, 1 - (held / 90) * (1 - FADE_FLOOR));
  return tired * (0.8 + bed.now.tension * 0.35);
}

// One eighth note. The pattern says what lands; the mood says how hard.
function pulse(n) {
  const { mood, root, shimmer } = bed.now;
  const w = weight();
  const slot = mood.bars[bed.bar % mood.bars.length][n % BAR];
  const chord = bed.chord;
  if (slot === 'K') {
    voice('music', chord[0] * 0.5, 0.4, { type: 'sine', vol: mood.hit * w, slide: -8, wet: 0.22 });
    air('music', 0.09, { vol: 0.02 * w, freq: 170, q: 2, wet: 0.3 });
  } else if (slot === 't') {
    air('music', 0.045, { vol: 0.016 * w, freq: 5600, q: 3.5, type: 'bandpass', wet: 0.45 });
  } else if (slot === 'o') {
    // The off-beat accent carries the harmony, so the rhythm says something as well as keeping time.
    voice('music', chord[1 + (bed.bar % (chord.length - 1))], 0.3, { type: 'triangle', vol: 0.028 * w, wet: 0.6 });
  }
  if (shimmer && n % BAR === 10) {
    voice('music', chord[chord.length - 1] * 4, 0.5, { type: 'sine', vol: 0.014 * shimmer, wet: 0.8 });
  }
}

/** Walks the progression on: a new chord and the next rhythm, every two bars. */
function turn() {
  const { mood, root } = bed.now;
  bed.bar++;
  bed.chord = mood.chords[bed.bar % mood.chords.length].map((n) => semitone(root, n));
  padChord(bed.pad, bed.chord, {
    gain: mood.gain * weight(),
    cut: mood.cut * (0.75 + bed.now.tension * 0.5),
    secs: STEP * BAR * 0.8,
  });
}

function pump() {
  const { ctx } = bed;
  while (bed.next < ctx.currentTime + LOOKAHEAD) {
    if (bed.step % BAR === 0) {
      turn();
    }
    pulse(bed.step);
    bed.step++;
    bed.next += STEP;
  }
}

/** Starts the bed if it is wanted and not already running. Needs a gesture to have happened. */
export function startAmbient(s) {
  if (bed || !volume('music')) {
    return;
  }
  const b = audio();
  const pad = b && startPad('music', 4, { wet: 0.55 });
  if (!pad) {
    return;
  }
  const now = read(s);
  bed = {
    ctx: b.ctx, pad, now, bar: -1, chord: now.mood.chords[0].map((n) => semitone(now.root, n)),
    step: 0, next: b.ctx.currentTime + 0.1, since: b.ctx.currentTime, timer: setInterval(pump, 90),
  };
}

export function stopAmbient() {
  if (!bed) {
    return;
  }
  clearInterval(bed.timer);
  stopPad(bed.pad);
  bed = null;
}

/** Called every second with the live state: turns the bed when the mood has changed. */
export function ambientTick(s) {
  if (!bed) {
    return;
  }
  if (!volume('music')) {
    stopAmbient();
    return;
  }
  const next = read(s);
  const turned = next.id !== bed.now.id || next.root !== bed.now.root;
  bed.now = next;
  if (turned) {
    bed.since = bed.ctx.currentTime; // a new mood is fresh again, however long the last one ran
    bed.step = 0; // and starts its pattern from the top, on the beat
    bed.bar = -1;
    // A breath across the change, so the mood turns instead of switching.
    air('music', 0.8, { vol: 0.035, freq: 500, slide: next.mood.hit > 0.07 ? 3400 : -300, q: 2, type: 'bandpass', wet: 0.7 });
  }
}

/** The music ducks while a dialog is telling a story, then comes back. */
export function duckAmbient(on) {
  if (!bed) {
    return;
  }
  ramp(bed.pad.gain.gain, on ? bed.now.mood.gain * 0.12 : bed.now.mood.gain * weight(), bed.ctx.currentTime, 0.5);
}

/** What the bed is doing, for the developer panel. */
export const ambientMood = () => (bed ? `${bed.now.id} bar ${bed.bar % bed.now.mood.bars.length} w${weight().toFixed(2)} t${bed.now.tension.toFixed(2)}` : 'off');
