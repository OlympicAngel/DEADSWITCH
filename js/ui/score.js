// The opening's score, and the music for every cut scene. It rides the shared music bus
// (js/ui/audio.js), so it is mixed, compressed and reverbed with everything else the game plays.
import { audio, startPad, stopPad, padChord, voice as tone, air as hiss, volume } from './audio.js';

const BPM = 74;
const STEP = 30 / BPM; // an eighth note
const LOOKAHEAD = 0.3; // seconds of pulse scheduled in advance

// Each scene names its chord (Hz), how hard the pulse hits, and how bright the bus is.
// The chords walk A minor down to a tritone for the raid and resolve on the last scene.
const SCENES = {
  gate: { chord: [55], drive: 0, cut: 420, pad: 0.05 },
  dead: { chord: [55], drive: 0, cut: 300, pad: 0.04 },
  surge: { chord: [55, 82.5], drive: 0.5, cut: 1400, pad: 0.07 },
  shaft: { chord: [55, 82.5, 110], drive: 0.8, cut: 2200, pad: 0.06 },
  title: { chord: [55, 110, 164.8, 220], drive: 1, cut: 3400, pad: 0.11 },
  ruins: { chord: [49, 73.4, 98], drive: 0.35, cut: 760, pad: 0.08 },
  ruins2: { chord: [43.7, 65.4, 87.3, 130.8], drive: 0.45, cut: 900, pad: 0.09 },
  core: { chord: [55, 82.5, 130.8], drive: 0.5, cut: 1100, pad: 0.09 },
  crowd: { chord: [65.4, 98, 123.5], drive: 0.4, cut: 900, pad: 0.07 },
  raid: { chord: [46.25, 65.4, 92.5], drive: 1, cut: 2600, pad: 0.12 },
  ready: { chord: [55, 82.5, 110, 164.8], drive: 0.6, cut: 1300, pad: 0.1 },
};
let bus = null; // the score's own state while it is running

const M = 'music';
const voice = (f, dur, o = {}) => tone(M, f, dur, { wet: 0.25, ...o });
const air = (dur, o = {}) => hiss(M, dur, { wet: 0.4, ...o });
// ---------- the pulse ----------

// A heartbeat on every beat and a tick on the off-beat, both scaled by the scene's drive, so the
// score keeps time under whatever is on screen.
function pulseStep(n, t) {
  const d = bus.scene.drive;
  if (!d) {
    return;
  }
  const beat = n % 4 === 0;
  if (beat) {
    voice(bus.scene.chord[0] * 0.5, 0.5, { type: 'sine', vol: 0.16 * d, at: t, slide: -12, wet: 0.2 });
    air(0.12, { vol: 0.05 * d, at: t, freq: 160, q: 2, wet: 0.3 });
  } else if (n % 2 === 0) {
    air(0.05, { vol: 0.03 * d, at: t, freq: 5200, q: 3, type: 'bandpass', wet: 0.5 });
  }
}

function pump() {
  const { ctx } = bus;
  while (bus.next < ctx.currentTime + LOOKAHEAD) {
    pulseStep(bus.step, Math.max(0, bus.next - ctx.currentTime));
    bus.step++;
    bus.next += STEP;
  }
}

// ---------- scene cues ----------

// Fired once as a scene opens, on top of the pad and the pulse.
const CUES = {
  dead: () => {
    air(2.4, { vol: 0.07, freq: 2600, slide: -2200, wet: 0.2 });
    voice(1800, 0.9, { type: 'square', vol: 0.012, wet: 0.1 }); // the dead carrier tone
    [0.4, 1.1, 1.9].forEach((at) => air(0.07, { vol: 0.05, at, freq: 3400, q: 1.5, type: 'bandpass' }));
  },
  surge: () => {
    voice(28, 2.4, { type: 'sawtooth', vol: 0.1, slide: 180, wet: 0.3 });
    air(1.6, { vol: 0.05, freq: 220, slide: 4000, q: 3, type: 'bandpass' });
    [0, 0.06, 0.13].forEach((at) => voice(1200 + at * 6000, 0.05, { type: 'square', vol: 0.04, at }));
    voice(110, 1.6, { type: 'triangle', vol: 0.05, at: 1.5, slide: -40, wet: 0.5 });
  },
  shaft: () => {
    // An arpeggio that climbs and speeds up: the camera is moving and so is the music.
    const notes = [220, 261.6, 329.6, 440, 523.3, 659.3, 880];
    notes.forEach((f, i) => voice(f, 0.5, { type: 'triangle', vol: 0.045, at: i * (0.26 - i * 0.02), wet: 0.6 }));
    air(2.2, { vol: 0.045, freq: 300, slide: 5000, q: 4, type: 'bandpass', wet: 0.4 });
  },
  title: () => {
    air(0.25, { vol: 0.22, freq: 60, slide: 30, q: 0.6, wet: 0.9 }); // the hit
    voice(38, 3.2, { type: 'sine', vol: 0.2, slide: -14, wet: 0.5 });
    air(2.6, { vol: 0.09, freq: 5000, slide: -4600, wet: 0.9 });
    [110, 164.8, 220, 329.6].forEach((f, i) => voice(f, 2.6, { type: 'triangle', vol: 0.05, at: 0.03 + i * 0.04, wet: 0.8, attack: 0.03 }));
  },
  ruins2: () => {
    air(4, { vol: 0.04, freq: 500, q: 0.6, slide: -260, wet: 0.6 });
    voice(87.3, 3.6, { type: 'sine', vol: 0.045, at: 0.4, wet: 0.7, attack: 0.9 });
  },
  ruins: () => {
    air(4.5, { vol: 0.045, freq: 420, q: 0.6, slide: -200, wet: 0.6 }); // wind
    voice(98, 3.4, { type: 'sine', vol: 0.04, at: 0.6, wet: 0.7, attack: 0.8 });
  },
  core: () => {
    [110, 164.8, 220].forEach((f, i) => voice(f, 2.6, { type: 'triangle', vol: 0.05, at: i * 0.16, wet: 0.7, attack: 0.2 }));
    voice(55, 3, { type: 'sine', vol: 0.09, wet: 0.3 });
  },
  crowd: () => {
    for (let i = 0; i < 6; i++) {
      voice(523.3 + i * 58, 0.22, { type: 'sine', vol: 0.045, at: 0.25 + i * 0.26, wet: 0.7 });
    }
    air(3, { vol: 0.03, freq: 700, q: 0.8, wet: 0.5 });
  },
  raid: () => {
    air(0.3, { vol: 0.24, freq: 90, slide: -50, q: 0.5, wet: 1 });
    voice(34, 2.6, { type: 'sawtooth', vol: 0.16, slide: -16, wet: 0.4 });
    air(2.2, { vol: 0.12, freq: 3200, slide: -2900, wet: 0.8 });
    // Two alarm blasts a bar apart, a tritone against the chord.
    [0.5, 1.4].forEach((at) => {
      voice(740, 0.3, { type: 'square', vol: 0.05, at, wet: 0.6 });
      voice(523, 0.3, { type: 'square', vol: 0.05, at: at + 0.16, wet: 0.6 });
    });
  },
  ready: () => {
    [55, 82.5, 110, 164.8, 220].forEach((f, i) => voice(f, 4, { type: 'triangle', vol: 0.05, at: i * 0.1, wet: 0.8, attack: 0.3 }));
    air(0.2, { vol: 0.1, freq: 120, q: 0.7, wet: 0.9 });
  },
};

// ---------- what the intro calls ----------

/** A whoosh across a cut, so scenes land instead of just appearing. Also used on a tap. */
export function swell(up = true) {
  if (!bus) {
    return;
  }
  air(0.55, { vol: 0.07, freq: up ? 400 : 3000, slide: up ? 4200 : -2600, q: 2.5, type: 'bandpass', wet: 0.7 });
}

/** Starts the score. Must be called from a gesture, or the browser keeps the context suspended. */
export function startScore(first = 'gate') {
  if (bus || !volume('music')) {
    return;
  }
  const b = audio();
  const pad = b && startPad(M, 4, { wet: 0.5 });
  if (!pad) {
    return;
  }
  bus = { ctx: b.ctx, pad, scene: SCENES.gate, step: 0, next: b.ctx.currentTime + 0.1, timer: null };
  bus.timer = setInterval(pump, 80);
  scoreScene(first);
}

/** Moves the score to a scene: retunes the pad and fires that scene's cue. */
export function scoreScene(id) {
  if (!bus) {
    return;
  }
  const sc = SCENES[id] || SCENES.gate;
  bus.scene = sc;
  padChord(bus.pad, sc.chord, { gain: sc.pad, cut: Math.max(300, sc.cut * 0.55), secs: 1.1 });
  CUES[id]?.();
}

export function stopScore() {
  if (!bus) {
    return;
  }
  clearInterval(bus.timer);
  stopPad(bus.pad, 1.4);
  bus = null;
}
