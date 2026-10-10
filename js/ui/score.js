// The opening's score. Everything is synthesised: a master bus with compression and a procedural
// reverb, a pulse locked to a tempo, and one musical cue per scene. It exists only while the intro
// is on screen and takes itself apart on the way out (js/ui/intro.js drives it).
import { isMuted } from './sfx.js';

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

let bus = null; // the whole score while it is running

/** Builds the master bus: everything dry goes through a compressor, with a reverb send beside it. */
function build() {
  const AC = window.AudioContext || window.webkitAudioContext;
  if (!AC) {
    return null;
  }
  const ctx = new AC();
  const master = ctx.createGain();
  const comp = ctx.createDynamicsCompressor();
  comp.threshold.value = -18;
  comp.ratio.value = 8;
  comp.attack.value = 0.004;
  comp.release.value = 0.2;
  const tone = ctx.createBiquadFilter(); // the scene opens and closes the whole bus
  tone.type = 'lowpass';
  tone.frequency.value = 500;
  tone.Q.value = 0.7;
  const verb = ctx.createConvolver();
  verb.buffer = impulse(ctx, 2.6, 2.4);
  const wet = ctx.createGain();
  wet.gain.value = 0.9;
  const send = ctx.createGain();
  send.gain.value = 1;
  send.connect(verb).connect(wet).connect(comp);
  tone.connect(comp);
  master.connect(tone);
  comp.connect(ctx.destination);
  return { ctx, master, send, tone, pad: null, timer: null, next: 0, step: 0, scene: SCENES.gate };
}

/** A decaying noise tail, which is all a convolution reverb needs to put the synth in a room. */
function impulse(ctx, secs, decay) {
  const len = Math.ceil(ctx.sampleRate * secs);
  const buf = ctx.createBuffer(2, len, ctx.sampleRate);
  for (let c = 0; c < 2; c++) {
    const d = buf.getChannelData(c);
    for (let i = 0; i < len; i++) {
      d[i] = (Math.random() * 2 - 1) * Math.pow(1 - i / len, decay);
    }
  }
  return buf;
}

function noiseBuffer(ctx, secs) {
  const buf = ctx.createBuffer(1, Math.ceil(ctx.sampleRate * secs), ctx.sampleRate);
  const d = buf.getChannelData(0);
  for (let i = 0; i < d.length; i++) {
    d[i] = Math.random() * 2 - 1;
  }
  return buf;
}

// ---------- voices ----------

/** One oscillator with an envelope. `wet` is how much of it goes to the reverb. */
function voice(f, dur, { type = 'sine', vol = 0.05, at = 0, slide = 0, wet = 0.25, attack = 0.008 } = {}) {
  const { ctx, master, send } = bus;
  const t = ctx.currentTime + at;
  const o = ctx.createOscillator();
  o.type = type;
  o.frequency.setValueAtTime(f, t);
  if (slide) {
    o.frequency.exponentialRampToValueAtTime(Math.max(20, f + slide), t + dur);
  }
  const g = ctx.createGain();
  g.gain.setValueAtTime(0, t);
  g.gain.linearRampToValueAtTime(vol, t + attack);
  g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  o.connect(g);
  g.connect(master);
  if (wet) {
    const w = ctx.createGain();
    w.gain.value = wet;
    g.connect(w).connect(send);
  }
  o.start(t);
  o.stop(t + dur + 0.05);
}

/** Filtered noise: wind, hiss, the air a blast moves. */
function air(dur, { vol = 0.05, at = 0, freq = 900, q = 1, slide = 0, type = 'lowpass', wet = 0.4 } = {}) {
  const { ctx, master, send } = bus;
  const t = ctx.currentTime + at;
  const src = ctx.createBufferSource();
  src.buffer = noiseBuffer(ctx, Math.min(3, dur + 0.2));
  src.loop = true;
  const f = ctx.createBiquadFilter();
  f.type = type;
  f.Q.value = q;
  f.frequency.setValueAtTime(freq, t);
  if (slide) {
    f.frequency.exponentialRampToValueAtTime(Math.max(40, freq + slide), t + dur);
  }
  const g = ctx.createGain();
  g.gain.setValueAtTime(0, t);
  g.gain.linearRampToValueAtTime(vol, t + Math.min(0.1, dur / 3));
  g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  src.connect(f).connect(g);
  g.connect(master);
  if (wet) {
    const w = ctx.createGain();
    w.gain.value = wet;
    g.connect(w).connect(send);
  }
  src.start(t);
  src.stop(t + dur + 0.1);
}

/** The sustained pad: three detuned saws per note, which is what makes it sound like strings. */
function startPad() {
  const { ctx, master, send } = bus;
  const gain = ctx.createGain();
  gain.gain.value = 0;
  const filter = ctx.createBiquadFilter();
  filter.type = 'lowpass';
  filter.frequency.value = 500;
  filter.Q.value = 2;
  const w = ctx.createGain();
  w.gain.value = 0.5;
  filter.connect(gain);
  gain.connect(master);
  gain.connect(w).connect(send);
  // Four slots, retuned per scene; a slot with no note in the chord is simply silenced.
  const slots = [0, 1, 2, 3].map(() => {
    const g = ctx.createGain();
    g.gain.value = 0;
    const oscs = [-7, 0, 7].map((cents) => {
      const o = ctx.createOscillator();
      o.type = 'sawtooth';
      o.detune.value = cents;
      o.frequency.value = 55;
      o.connect(g);
      o.start();
      return o;
    });
    g.connect(filter);
    return { g, oscs };
  });
  return { gain, filter, slots };
}

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
export function startScore() {
  if (bus || isMuted()) {
    return;
  }
  bus = build();
  if (!bus) {
    return;
  }
  bus.ctx.resume();
  bus.pad = startPad();
  bus.next = bus.ctx.currentTime + 0.1;
  bus.timer = setInterval(pump, 80);
  bus.master.gain.setValueAtTime(0, bus.ctx.currentTime);
  bus.master.gain.linearRampToValueAtTime(1, bus.ctx.currentTime + 0.8);
  scoreScene('gate');
}

/** Moves the score to a scene: retunes the pad, opens the bus and fires that scene's cue. */
export function scoreScene(id) {
  if (!bus) {
    return;
  }
  const sc = SCENES[id] || SCENES.gate;
  bus.scene = sc;
  const { ctx, pad, tone } = bus;
  const t = ctx.currentTime;
  ramp(tone.frequency, sc.cut, t, 0.9);
  ramp(pad.filter.frequency, Math.max(300, sc.cut * 0.5), t, 1.2);
  ramp(pad.gain.gain, sc.pad, t, 1.2);
  pad.slots.forEach((slot, i) => {
    const f = sc.chord[i];
    if (f) {
      slot.oscs.forEach((o) => ramp(o.frequency, f, t, 0.7));
    }
    ramp(slot.g.gain, f ? 0.26 : 0, t, 0.9);
  });
  CUES[id]?.();
}

export function stopScore() {
  if (!bus) {
    return;
  }
  const dying = bus;
  bus = null;
  clearInterval(dying.timer);
  const t = dying.ctx.currentTime;
  ramp(dying.master.gain, 0, t, 1.4);
  setTimeout(() => dying.ctx.close().catch(() => {}), 1800);
}

function ramp(param, to, t, secs) {
  param.cancelScheduledValues(t);
  param.setValueAtTime(param.value, t);
  param.linearRampToValueAtTime(to, t + secs);
}
