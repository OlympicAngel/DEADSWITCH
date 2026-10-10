// The music under the game. It is not a loop: a mood is read off the state every second (what is
// inbound, what is pending, how hard it would land, how deep into the story we are) and the bed
// moves to it — a different chord, a brighter or darker filter, a faster pulse, a shimmer when the
// stores are full. It runs on the music bus (js/ui/audio.js) and stops whenever the page does.
import * as E from '../engine.js';
import { RESOURCES } from '../data.js';
import { audio, startPad, stopPad, padChord, voice, air, volume, ramp } from './audio.js';

// Chords climb with the chapter, so the world gets grander as it opens up. Each mood picks a shape
// over that root: how the bed sits, how often the pulse lands, and how bright it all is.
const ROOTS = [55, 61.7, 65.4, 73.4]; // A, B, C, D, one per chapter
const SHAPES = {
  calm: { steps: [0, 7, 12], gain: 0.05, cut: 520, beat: 4, drive: 0.35 },
  work: { steps: [0, 7, 12, 19], gain: 0.06, cut: 760, beat: 4, drive: 0.5 },
  push: { steps: [0, 5, 12, 17], gain: 0.07, cut: 1000, beat: 2, drive: 0.6 },
  alert: { steps: [0, 3, 10], gain: 0.07, cut: 900, beat: 2, drive: 0.75 },
  danger: { steps: [0, 1, 6, 12], gain: 0.09, cut: 1500, beat: 1, drive: 1 },
  crisis: { steps: [0, 6, 11], gain: 0.1, cut: 1900, beat: 1, drive: 1 },
};
const STEP = 60 / 74 / 2; // an eighth at 74 BPM, the tempo the opening is scored at
const LOOKAHEAD = 0.35;
const semitone = (root, n) => root * Math.pow(2, n / 12);

let bed = null;
let watch = null;

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

/** Full stores and a running operation both shimmer: the bed answers what the player is doing. */
function sparkle(s) {
  const caps = E.caps(s);
  const full = Object.keys(RESOURCES).filter((r) => Number.isFinite(caps[r]) && s.res[r] >= caps[r] * 0.995).length;
  return Math.min(1, full / 2);
}

function read(s) {
  const id = moodOf(s);
  const shape = SHAPES[id];
  const root = ROOTS[Math.min(ROOTS.length - 1, Math.max(0, s.chapter - 1))];
  return { id, shape, chord: shape.steps.map((n) => semitone(root, n)), shimmer: sparkle(s) };
}

// The pulse: a heartbeat on the mood's beat and a tick between, both scaled by its drive.
function pulse(n) {
  const { shape, chord, shimmer } = bed.now;
  if (n % shape.beat === 0) {
    voice('music', chord[0] * 0.5, 0.45, { type: 'sine', vol: 0.11 * shape.drive, slide: -10, wet: 0.25 });
    air('music', 0.1, { vol: 0.03 * shape.drive, freq: 170, q: 2, wet: 0.3 });
  } else if (shimmer && n % 4 === 2) {
    // Stores at the brim: a high note that only sounds while nothing can be stored.
    voice('music', chord[chord.length - 1] * 4, 0.5, { type: 'sine', vol: 0.02 * shimmer, wet: 0.8 });
  }
}

function pump() {
  const { ctx } = bed;
  while (bed.next < ctx.currentTime + LOOKAHEAD) {
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
  if (!b) {
    return;
  }
  const pad = startPad('music', 4, { wet: 0.55 });
  if (!pad) {
    return;
  }
  bed = { ctx: b.ctx, pad, now: read(s), step: 0, next: b.ctx.currentTime + 0.1, timer: setInterval(pump, 90) };
  apply(true);
}

export function stopAmbient() {
  if (!bed) {
    return;
  }
  clearInterval(bed.timer);
  stopPad(bed.pad);
  bed = null;
}

/** Called every second with the live state: moves the bed when the mood has changed. */
export function ambientTick(s) {
  if (!bed) {
    return;
  }
  if (!volume('music')) {
    stopAmbient();
    return;
  }
  const next = read(s);
  const changed = next.id !== bed.now.id || next.chord[0] !== bed.now.chord[0];
  bed.now = next;
  if (changed) {
    apply(false);
  }
}

function apply(first) {
  const { shape, chord } = bed.now;
  padChord(bed.pad, chord, { gain: shape.gain, cut: shape.cut, secs: first ? 2.5 : 1.6 });
  if (!first) {
    // A short breath across the change, so the mood turns instead of switching.
    air('music', 0.7, { vol: 0.045, freq: 500, slide: shape.drive > 0.7 ? 3600 : -300, q: 2, type: 'bandpass', wet: 0.7 });
  }
}

/** The music ducks while a dialog is telling a story, then comes back. */
export function duckAmbient(on) {
  if (!bed) {
    return;
  }
  ramp(bed.pad.gain.gain, on ? bed.now.shape.gain * 0.15 : bed.now.shape.gain, bed.ctx.currentTime, 0.5);
  bed.ducked = on;
}

/** Whether anything is playing, for the developer panel. */
export const ambientMood = () => (bed ? bed.now.id : 'off');
