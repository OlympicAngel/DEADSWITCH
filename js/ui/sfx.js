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
