// The game's audio engine. One context, two buses the player controls separately (effects and
// music), one compressor and one procedural reverb that everything shares, so a cue fired by the
// interface sits in the same room as the music. Nothing here is a file: every sound is synthesised.
// Built lazily, because a browser only starts a context from a gesture.

const KEY = 'deadswitch.audio';
const LEGACY_MUTE = 'deadswitch.muted';
// `voice` is not a bus: speech synthesis has no node to route, so it only scales the utterance.
const DEFAULTS = { sfx: 0.8, music: 0.55, voice: 0.9 };

let level = { ...DEFAULTS };
try {
  const raw = JSON.parse(localStorage.getItem(KEY) || 'null');
  if (raw && Number.isFinite(raw.sfx) && Number.isFinite(raw.music)) {
    level = { ...DEFAULTS, sfx: clamp01(raw.sfx), music: clamp01(raw.music) };
    if (Number.isFinite(raw.voice)) {
      level.voice = clamp01(raw.voice);
    }
  } else if (localStorage.getItem(LEGACY_MUTE) === '1') {
    level = { sfx: 0, music: 0, voice: 0 }; // a save from when sound was one switch
  }
} catch {
  // Storage blocked: defaults are fine.
}

function clamp01(n) {
  return Math.max(0, Math.min(1, n));
}

let bus = null;

/** Volume of a bus, 0 to 1. */
export const volume = (id) => level[id];
export const isMuted = () => !level.sfx && !level.music && !level.voice;

export function setVolume(id, v) {
  level[id] = clamp01(v);
  if (bus && bus[id]) {
    bus[id].gain.setTargetAtTime(level[id], bus.ctx.currentTime, 0.02);
  }
  try {
    localStorage.setItem(KEY, JSON.stringify(level));
  } catch {
    // ignore
  }
}

/** The engine, built on first use. Null when the browser has no audio at all. */
export function audio() {
  if (bus) {
    if (bus.ctx.state === 'suspended') {
      bus.ctx.resume();
    }
    return bus;
  }
  const AC = window.AudioContext || window.webkitAudioContext;
  if (!AC) {
    return null;
  }
  const ctx = new AC();
  const comp = ctx.createDynamicsCompressor();
  comp.threshold.value = -16;
  comp.ratio.value = 7;
  comp.attack.value = 0.004;
  comp.release.value = 0.22;
  comp.connect(ctx.destination);
  const verb = ctx.createConvolver();
  verb.buffer = impulse(ctx, 2.6, 2.4);
  const wet = ctx.createGain();
  wet.gain.value = 0.85;
  verb.connect(wet).connect(comp);
  const make = (id) => {
    const g = ctx.createGain();
    g.gain.value = level[id];
    g.connect(comp);
    const send = ctx.createGain();
    send.gain.value = 1;
    send.connect(verb);
    return { gain: g, send };
  };
  const sfx = make('sfx');
  const music = make('music');
  bus = { ctx, comp, sfx: sfx.gain, music: music.gain, sfxSend: sfx.send, musicSend: music.send };
  return bus;
}

/** Where a sound plugs in: its bus and the reverb send beside it. */
function out(id) {
  const b = audio();
  if (!b || !level[id]) {
    return null;
  }
  return { ctx: b.ctx, dry: b[id], send: id === 'music' ? b.musicSend : b.sfxSend };
}

/** A decaying noise tail, which is all a convolution reverb needs to put a synth in a room. */
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

export function noiseBuffer(ctx, secs) {
  const buf = ctx.createBuffer(1, Math.ceil(ctx.sampleRate * secs), ctx.sampleRate);
  const d = buf.getChannelData(0);
  for (let i = 0; i < d.length; i++) {
    d[i] = Math.random() * 2 - 1;
  }
  return buf;
}

/** One oscillator with an envelope. `wet` is how much of it goes to the reverb. */
export function voice(id, f, dur, { type = 'sine', vol = 0.05, at = 0, slide = 0, wet = 0.2, attack = 0.008 } = {}) {
  const o = out(id);
  if (!o) {
    return;
  }
  const t = o.ctx.currentTime + at;
  const osc = o.ctx.createOscillator();
  osc.type = type;
  osc.frequency.setValueAtTime(f, t);
  if (slide) {
    osc.frequency.exponentialRampToValueAtTime(Math.max(20, f + slide), t + dur);
  }
  const g = o.ctx.createGain();
  g.gain.setValueAtTime(0, t);
  g.gain.linearRampToValueAtTime(vol, t + attack);
  g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  osc.connect(g);
  g.connect(o.dry);
  if (wet) {
    const w = o.ctx.createGain();
    w.gain.value = wet;
    g.connect(w).connect(o.send);
  }
  osc.start(t);
  osc.stop(t + dur + 0.05);
}

/** Filtered noise: hiss, wind, the air a blast moves. */
export function air(id, dur, { vol = 0.05, at = 0, freq = 900, q = 1, slide = 0, type = 'lowpass', wet = 0.35 } = {}) {
  const o = out(id);
  if (!o) {
    return;
  }
  const t = o.ctx.currentTime + at;
  const src = o.ctx.createBufferSource();
  src.buffer = noiseBuffer(o.ctx, Math.min(3, dur + 0.2));
  src.loop = true;
  const f = o.ctx.createBiquadFilter();
  f.type = type;
  f.Q.value = q;
  f.frequency.setValueAtTime(freq, t);
  if (slide) {
    f.frequency.exponentialRampToValueAtTime(Math.max(40, freq + slide), t + dur);
  }
  const g = o.ctx.createGain();
  g.gain.setValueAtTime(0, t);
  g.gain.linearRampToValueAtTime(vol, t + Math.min(0.1, dur / 3));
  g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  src.connect(f).connect(g);
  g.connect(o.dry);
  if (wet) {
    const w = o.ctx.createGain();
    w.gain.value = wet;
    g.connect(w).connect(o.send);
  }
  src.start(t);
  src.stop(t + dur + 0.1);
}

/** A sustained pad: `slots` notes of three detuned saws each, retuned while it plays. */
export function startPad(id, slots = 4, { wet = 0.5, q = 2 } = {}) {
  const b = audio();
  if (!b) {
    return null;
  }
  const ctx = b.ctx;
  const gain = ctx.createGain();
  gain.gain.value = 0;
  const filter = ctx.createBiquadFilter();
  filter.type = 'lowpass';
  filter.frequency.value = 500;
  filter.Q.value = q;
  filter.connect(gain);
  gain.connect(b[id]);
  const w = ctx.createGain();
  w.gain.value = wet;
  gain.connect(w).connect(id === 'music' ? b.musicSend : b.sfxSend);
  const notes = [...Array(slots)].map(() => {
    const g = ctx.createGain();
    g.gain.value = 0;
    const oscs = [-7, 0, 7].map((cents) => {
      const osc = ctx.createOscillator();
      osc.type = 'sawtooth';
      osc.detune.value = cents;
      osc.frequency.value = 55;
      osc.connect(g);
      osc.start();
      return osc;
    });
    g.connect(filter);
    return { g, oscs };
  });
  return { ctx, gain, filter, slots: notes };
}

export function stopPad(pad, secs = 1.2) {
  if (!pad) {
    return;
  }
  const t = pad.ctx.currentTime;
  ramp(pad.gain.gain, 0, t, secs);
  for (const n of pad.slots) {
    for (const o of n.oscs) {
      try {
        o.stop(t + secs + 0.1);
      } catch {
        // already stopped
      }
    }
  }
}

/** Moves a pad to a chord (Hz), silencing the slots the chord does not use. */
export function padChord(pad, chord, { gain = 0.07, cut = 700, secs = 1.1 } = {}) {
  if (!pad) {
    return;
  }
  const t = pad.ctx.currentTime;
  ramp(pad.gain.gain, gain, t, secs);
  ramp(pad.filter.frequency, cut, t, secs);
  pad.slots.forEach((slot, i) => {
    const f = chord[i];
    if (f) {
      slot.oscs.forEach((o) => ramp(o.frequency, f, t, secs * 0.7));
    }
    ramp(slot.g.gain, f ? 0.26 : 0, t, secs);
  });
}

/** Anchors a parameter at where it is now, then ramps: without this it jumps. */
export function ramp(param, to, t, secs) {
  param.cancelScheduledValues(t);
  param.setValueAtTime(param.value, t);
  param.linearRampToValueAtTime(to, t + secs);
}
