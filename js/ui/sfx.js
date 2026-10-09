// Tiny WebAudio synth for UI feedback. No audio files. Muting is remembered per browser.
const KEY = 'deadswitch.muted';
let ctx = null;
let muted = false;
try {
  muted = localStorage.getItem(KEY) === '1';
} catch {
  // Storage blocked: default to sound on.
}

export const isMuted = () => muted;

export function setMuted(m) {
  muted = m;
  try {
    localStorage.setItem(KEY, m ? '1' : '0');
  } catch {
    // ignore
  }
}

function audio() {
  if (muted) {
    return null;
  }
  if (!ctx) {
    const AC = window.AudioContext || window.webkitAudioContext;
    if (!AC) {
      return null;
    }
    ctx = new AC();
  }
  if (ctx.state === 'suspended') {
    ctx.resume();
  }
  return ctx;
}

function tone(freq, dur, { type = 'square', vol = 0.05, at = 0, slide = 0 } = {}) {
  const a = audio();
  if (!a) {
    return;
  }
  const t = a.currentTime + at;
  const o = a.createOscillator();
  const g = a.createGain();
  o.type = type;
  o.frequency.setValueAtTime(freq, t);
  if (slide) {
    o.frequency.exponentialRampToValueAtTime(Math.max(30, freq + slide), t + dur);
  }
  g.gain.setValueAtTime(0, t);
  g.gain.linearRampToValueAtTime(vol, t + 0.01);
  g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  o.connect(g).connect(a.destination);
  o.start(t);
  o.stop(t + dur + 0.02);
}

// White noise, for hiss and blasts. Short buffers, looped for as long as the sound lasts.
function noiseBuffer(a, secs) {
  const buf = a.createBuffer(1, Math.ceil(a.sampleRate * secs), a.sampleRate);
  const d = buf.getChannelData(0);
  for (let i = 0; i < d.length; i++) {
    d[i] = Math.random() * 2 - 1;
  }
  return buf;
}

function hiss(dur, { vol = 0.04, at = 0, freq = 900, q = 1, slide = 0, type = 'lowpass' } = {}) {
  const a = audio();
  if (!a) {
    return;
  }
  const t = a.currentTime + at;
  const src = a.createBufferSource();
  src.buffer = noiseBuffer(a, Math.min(2, dur + 0.1));
  src.loop = true;
  const f = a.createBiquadFilter();
  f.type = type;
  f.Q.value = q;
  f.frequency.setValueAtTime(freq, t);
  if (slide) {
    f.frequency.exponentialRampToValueAtTime(Math.max(40, freq + slide), t + dur);
  }
  const g = a.createGain();
  g.gain.setValueAtTime(0, t);
  g.gain.linearRampToValueAtTime(vol, t + Math.min(0.08, dur / 3));
  g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  src.connect(f).connect(g).connect(a.destination);
  src.start(t);
  src.stop(t + dur + 0.05);
}

// ---------- the opening ----------

// The bed under the boot sequence: two low voices and a fifth through a lowpass, breathing on a slow
// LFO. Each beat moves its weight, its brightness and how fast it breathes (BED), so the music is the
// sequence rather than a loop behind it. It exists only while the intro is on screen.
const BED = {
  gate: { gain: 0.03, cut: 260, rate: 0.1 },
  noise: { gain: 0.04, cut: 380, rate: 0.16 },
  power: { gain: 0.07, cut: 760, rate: 0.34 },
  scan: { gain: 0.05, cut: 560, rate: 0.22 },
  title: { gain: 0.09, cut: 1200, rate: 0.45 },
  voice: { gain: 0.04, cut: 420, rate: 0.13 },
  core: { gain: 0.06, cut: 780, rate: 0.2 },
  crowd: { gain: 0.05, cut: 620, rate: 0.26 },
  blast: { gain: 0.1, cut: 1600, rate: 0.75 },
};
let bed = null;

export function ambient(on, fx = 'gate') {
  const a = audio();
  if (!a) {
    return;
  }
  if (!on) {
    stopBed(a);
    return;
  }
  if (!bed) {
    bed = startBed(a);
  }
  const m = BED[fx] || BED.gate;
  const t = a.currentTime;
  // Anchor each parameter at what it is now, or the ramp starts from whatever was scheduled before.
  for (const [param, to] of [[bed.gain.gain, m.gain], [bed.filter.frequency, m.cut], [bed.lfo.frequency, m.rate]]) {
    param.cancelScheduledValues(t);
    param.setValueAtTime(param.value, t);
    param.linearRampToValueAtTime(to, t + 1.1);
  }
}

function startBed(a) {
  const mix = a.createGain();
  const filter = a.createBiquadFilter();
  filter.type = 'lowpass';
  filter.Q.value = 0.8;
  filter.frequency.setValueAtTime(BED.gate.cut, a.currentTime);
  const trem = a.createGain();
  const gain = a.createGain();
  gain.gain.setValueAtTime(0, a.currentTime);
  const voices = [[55, 'sine', 1], [55.4, 'sine', 0.9], [82.5, 'triangle', 0.35]].map(([f, type, v]) => {
    const o = a.createOscillator();
    o.type = type;
    o.frequency.value = f;
    const g = a.createGain();
    g.gain.value = v;
    o.connect(g).connect(mix);
    o.start();
    return o;
  });
  // A slow swell rides on top of a steady floor, so the bed never drops out completely.
  trem.gain.value = 0.7;
  const lfo = a.createOscillator();
  lfo.frequency.value = BED.gate.rate;
  const depth = a.createGain();
  depth.gain.value = 0.3;
  lfo.connect(depth).connect(trem.gain);
  lfo.start();
  const wash = a.createBufferSource();
  wash.buffer = noiseBuffer(a, 2);
  wash.loop = true;
  const washGain = a.createGain();
  washGain.gain.value = 0.05;
  wash.connect(washGain).connect(mix);
  wash.start();
  mix.connect(filter).connect(trem).connect(gain).connect(a.destination);
  return { gain, filter, lfo, nodes: [...voices, lfo, wash] };
}

function stopBed(a) {
  if (!bed) {
    return;
  }
  const dying = bed;
  bed = null;
  const t = a.currentTime;
  dying.gain.gain.cancelScheduledValues(t);
  dying.gain.gain.setValueAtTime(dying.gain.gain.value, t);
  dying.gain.gain.linearRampToValueAtTime(0, t + 1.2);
  for (const n of dying.nodes) {
    try {
      n.stop(t + 1.3);
    } catch {
      // already stopped
    }
  }
}

/** One cue per boot beat, keyed by its fx (data/story.js). */
export const bootSfx = {
  gate: () => { tone(220, 0.5, { type: 'sine', vol: 0.05, slide: -80 }); },
  noise: () => { hiss(1.8, { vol: 0.06, freq: 2800, slide: -2300 }); tone(70, 1.2, { type: 'square', vol: 0.02 }); },
  power: () => {
    tone(55, 2.2, { type: 'sawtooth', vol: 0.05, slide: 260 });
    hiss(1.4, { vol: 0.035, freq: 300, slide: 2800 });
    [660, 880].forEach((f, i) => tone(f, 0.06, { type: 'square', vol: 0.03, at: 1.5 + i * 0.12 }));
  },
  scan: () => { for (let i = 0; i < 5; i++) tone(1500 - i * 140, 0.07, { type: 'sine', vol: 0.03, at: i * 0.32 }); },
  title: () => {
    tone(42, 1.8, { type: 'sine', vol: 0.13, slide: -16 });
    hiss(1, { vol: 0.08, freq: 3400, slide: -3200 });
    [392, 523, 784].forEach((f, i) => tone(f, 0.55, { type: 'triangle', vol: 0.05, at: 0.06 + i * 0.07 }));
  },
  voice: () => tone(140, 1, { type: 'sine', vol: 0.035, slide: -40 }),
  core: () => [110, 165, 220].forEach((f, i) => tone(f, 1.5, { type: 'triangle', vol: 0.04, at: i * 0.14 })),
  crowd: () => { for (let i = 0; i < 6; i++) tone(540 + i * 80, 0.09, { type: 'sine', vol: 0.035, at: 0.12 + i * 0.2 }); },
  blast: () => {
    tone(48, 1.6, { type: 'sawtooth', vol: 0.12, slide: -32 });
    hiss(1.3, { vol: 0.1, freq: 2000, slide: -1700 });
    tone(92, 0.3, { type: 'square', vol: 0.05, at: 0.02 });
  },
};

export const sfx = {
  click: () => tone(660, 0.05, { type: 'triangle', vol: 0.04 }),
  buy: () => { tone(520, 0.06, { type: 'triangle' }); tone(780, 0.08, { type: 'triangle', at: 0.05 }); },
  build: () => { tone(392, 0.1, { type: 'triangle' }); tone(523, 0.1, { type: 'triangle', at: 0.08 }); tone(784, 0.18, { type: 'triangle', at: 0.16 }); },
  launch: () => tone(180, 0.35, { type: 'sawtooth', vol: 0.04, slide: 300 }),
  alarm: () => { for (let i = 0; i < 3; i++) { tone(880, 0.12, { type: 'square', vol: 0.035, at: i * 0.22 }); tone(660, 0.1, { type: 'square', vol: 0.035, at: i * 0.22 + 0.11 }); } },
  win: () => [523, 659, 784, 1046].forEach((f, i) => tone(f, 0.22, { type: 'triangle', vol: 0.06, at: i * 0.1 })),
  lose: () => [330, 262, 196].forEach((f, i) => tone(f, 0.3, { type: 'sawtooth', vol: 0.04, at: i * 0.16 })),
  event: () => { tone(1200, 0.05, { type: 'sine', vol: 0.05 }); tone(1500, 0.05, { type: 'sine', vol: 0.05, at: 0.09 }); tone(1200, 0.08, { type: 'sine', vol: 0.05, at: 0.18 }); },
  type: () => tone(1800 + Math.random() * 400, 0.015, { type: 'square', vol: 0.012 }),
  story: () => tone(110, 1.2, { type: 'sine', vol: 0.07, slide: -40 }),
};
