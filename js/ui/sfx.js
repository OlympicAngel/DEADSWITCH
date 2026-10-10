// Every sound the interface makes. All of it is synthesised through the shared bus (js/ui/audio.js),
// so a tap and the music sit in the same room. Cues are built from a few shapes: a `blip` for
// anything the player did, a `thud` for weight, `air` for the sound of something moving.
import { voice, air, volume, setVolume, isMuted } from './audio.js';

export { volume, setVolume, isMuted };

const SFX = 'sfx';
const blip = (f, dur, o = {}) => voice(SFX, f, dur, { type: 'triangle', vol: 0.05, wet: 0.18, ...o });
const thud = (f, dur, o = {}) => voice(SFX, f, dur, { type: 'sine', vol: 0.09, wet: 0.3, ...o });
const seq = (notes, step, o = {}) => notes.forEach((f, i) => blip(f, o.dur || 0.22, { ...o, at: (o.at || 0) + i * step }));

// A minor pentatonic, so unrelated cues still agree with each other and with the music.
const A = { low: 110, root: 220, m3: 261.6, p4: 293.7, p5: 329.6, m7: 392, oct: 440, high: 523.3 };

export const sfx = {
  /** Anything the player tapped that did not change the world. */
  click: () => blip(A.m7, 0.045, { vol: 0.035, wet: 0.1 }),
  /** Bought something. */
  buy: () => { blip(A.p5, 0.07); blip(A.oct, 0.1, { at: 0.055 }); air(SFX, 0.08, { vol: 0.02, freq: 4200, q: 3, type: 'bandpass' }); },
  /** A building finished: three notes up and a little weight under them. */
  build: () => { seq([A.p4, A.p5, A.high], 0.09, { dur: 0.3 }); thud(A.low, 0.5, { slide: -30 }); },
  /** An operation left the Nest. */
  launch: () => {
    voice(SFX, 90, 0.5, { type: 'sawtooth', vol: 0.06, slide: 320, wet: 0.5 });
    air(SFX, 0.6, { vol: 0.05, freq: 300, slide: 3200, q: 2.5, type: 'bandpass', wet: 0.5 });
  },
  /** Something hostile is on the clock. Two falling pairs, the second a tone lower. */
  alarm: () => {
    [0, 0.26].forEach((at, k) => {
      voice(SFX, 740 - k * 60, 0.16, { type: 'square', vol: 0.045, at, wet: 0.4 });
      voice(SFX, 523 - k * 40, 0.18, { type: 'square', vol: 0.045, at: at + 0.13, wet: 0.4 });
    });
    thud(55, 0.9, { vol: 0.06, slide: -10 });
  },
  /** An attack landed and we held it. */
  win: () => { seq([A.root, A.m3, A.p5, A.oct], 0.1, { dur: 0.35, vol: 0.055, wet: 0.45 }); thud(A.low, 1, { slide: -20 }); },
  /** An attack landed and we did not. */
  lose: () => {
    [A.p5, A.m3, A.root].forEach((f, i) => voice(SFX, f * 0.5, 0.45, { type: 'sawtooth', vol: 0.05, at: i * 0.17, wet: 0.5 }));
    air(SFX, 1.2, { vol: 0.06, freq: 1600, slide: -1400, wet: 0.6 });
  },
  /** A sector of ours fell, or a crisis landed: lower and heavier than a lost raid. */
  fall: () => {
    voice(SFX, 44, 1.8, { type: 'sawtooth', vol: 0.11, slide: -14, wet: 0.5 });
    air(SFX, 0.26, { vol: 0.14, freq: 110, slide: -60, q: 0.6, wet: 0.8 });
    air(SFX, 1.6, { vol: 0.06, freq: 2400, slide: -2200, wet: 0.7 });
  },
  /** A sector of theirs is ours. */
  capture: () => { seq([A.m3, A.p5, A.m7, A.high], 0.085, { dur: 0.4, vol: 0.05, wet: 0.6 }); thud(73.4, 1.2, { vol: 0.08 }); },
  /** An order arrived. */
  event: () => { blip(1046, 0.06, { type: 'sine', vol: 0.045, wet: 0.4 }); blip(1318, 0.06, { type: 'sine', vol: 0.045, at: 0.08, wet: 0.4 }); blip(1046, 0.1, { type: 'sine', vol: 0.04, at: 0.17, wet: 0.5 }); },
  /** A deadline is close. */
  urgent: () => { blip(880, 0.07, { type: 'square', vol: 0.04 }); blip(880, 0.07, { type: 'square', vol: 0.04, at: 0.14 }); },
  /** The AI speaking: used under typed text. */
  type: () => voice(SFX, 1700 + Math.random() * 500, 0.014, { type: 'square', vol: 0.01, wet: 0 }),
  /** A rank, a chapter, a memory: something the archive will remember. */
  story: () => {
    [A.low, A.m3 * 0.5, A.p5 * 0.5].forEach((f, i) => voice(SFX, f, 2.4, { type: 'triangle', vol: 0.05, at: i * 0.12, wet: 0.8, attack: 0.25 }));
    air(SFX, 0.2, { vol: 0.07, freq: 130, q: 0.8, wet: 0.9 });
  },
  /** Something opened up that was not there before. */
  unlock: () => { seq([A.p5, A.m7, A.high, A.high * 1.5], 0.07, { dur: 0.45, vol: 0.045, wet: 0.7 }); },
  /** A choice was taken, for good or ill. */
  choose: (good = true) => {
    if (good) {
      blip(A.p5, 0.14, { vol: 0.05, wet: 0.4 });
      blip(A.oct, 0.3, { vol: 0.05, at: 0.1, wet: 0.5 });
    } else {
      voice(SFX, A.m3, 0.16, { type: 'sawtooth', vol: 0.045, wet: 0.4 });
      voice(SFX, A.low, 0.5, { type: 'sawtooth', vol: 0.05, at: 0.11, wet: 0.5, slide: -20 });
    }
  },
};
