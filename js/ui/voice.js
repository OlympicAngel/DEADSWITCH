// The AI speaking out loud during a cut scene, through the browser's own speech synthesis.
// There is no control over timbre in that API, so the robot is made three ways: pick the most
// synthetic voice the device has (an offline engine, or a voice for another language, which mangles
// English phonemes exactly the way a machine reading from a dictionary would), drive it low and
// fast, and wrap it in a carrier click and a band of static from our own synth (js/ui/audio.js).
import { voice as tone, air, volume } from './audio.js';

// Voices named like this are the old formant synths: eSpeak on Android and Linux, the macOS
// novelty set, the "compact" iOS voices. They are what we want.
const ROBOT = /espeak|e-speak|compact|robot|zarvox|trinoids|albert|bahh|boing|bubbles|cellos|deranged|hysterical|whisper|android|pico/i;
// And like this are the ones that sound like a person, which is the opposite of the point.
const HUMAN = /natural|neural|enhanced|premium|siri|wavenet|studio|journey|news|polyglot/i;
// Languages whose phoneme set reads English like a machine: hard consonants, flat vowels, but
// still written in the Latin alphabet, so the engine has a rule for every letter it is handed.
const HARSH = ['de', 'pl', 'cs', 'sk', 'nl', 'hu', 'tr', 'fi', 'ro', 'hr', 'sl', 'lt', 'lv', 'et'];
// A voice for a language in another script has no idea what to do with English letters: it either
// spells them out or mangles them into something that is not robotic, just wrong.
const OTHER_SCRIPT = ['bg', 'ru', 'uk', 'sr', 'mk', 'be', 'el', 'he', 'ar', 'fa', 'hi', 'bn', 'ta',
  'th', 'ka', 'hy', 'am', 'ur', 'zh', 'ja', 'ko', 'yue', 'cmn'];

const RATE = 1.22; // semi-fast: it is reading, not talking
const PITCH = 0.35; // as low as the API goes without most engines clamping

let picked = null;
let ready = false;
let speaking = null;

const synth = () => (typeof speechSynthesis === 'undefined' ? null : speechSynthesis);

/** How robotic a voice is, highest wins. */
function score(v) {
  const name = `${v.name} ${v.voiceURI}`;
  let n = 0;
  if (ROBOT.test(name)) n += 100;
  if (HUMAN.test(name)) n -= 120;
  if (v.localService) n += 25; // offline engines are the older, flatter ones
  const lang = (v.lang || '').toLowerCase().split(/[-_]/)[0];
  if (OTHER_SCRIPT.includes(lang)) return -200; // never: it cannot read the alphabet we hand it
  if (HARSH.includes(lang)) n += 40;
  else if (lang !== 'en') n += 18; // any other Latin-script language still breaks the vowels up
  return n;
}

/** Chooses once per session, and again if the list arrives late (Chrome fills it asynchronously). */
function pick() {
  const s = synth();
  if (!s) {
    return null;
  }
  const all = s.getVoices();
  if (!all.length) {
    return null;
  }
  if (!ready) {
    ready = true;
    picked = all.slice().sort((a, b) => score(b) - score(a))[0];
  }
  return picked;
}

if (synth()) {
  // The list is empty on first call in most browsers; this fires when it is not.
  synth().addEventListener('voiceschanged', () => { ready = false; pick(); });
  pick();
}

/** The voice the game would use, for the developer panel and the settings line. */
export function voiceName() {
  const v = pick();
  return v ? `${v.name} (${v.lang})` : 'none';
}

/** Roughly how long a line will take to speak, for pacing a scene that has not started yet. */
export const sayLength = (text) => (text ? 0.5 + String(text).length / 14 : 0);

/** Speaks a line as the AI. Terminal readouts are not spoken: they are machine output, not speech.
 *  `done` is called when the line finishes, but never when it is cut off. Returns whether it
 *  started, so a cut scene knows whether to wait for it. */
export function say(text, done) {
  const s = synth();
  const vol = volume('voice');
  if (!s || !vol || !text) {
    return false;
  }
  stopVoice();
  const u = new SpeechSynthesisUtterance(String(text));
  const v = pick();
  try {
    if (v) {
      u.voice = v;
      u.lang = v.lang;
    }
  } catch {
    // The engine would not take that voice; its default still speaks.
  }
  u.rate = RATE;
  u.pitch = PITCH;
  u.volume = vol;
  // The transmission around it: a carrier click, a band of static while it talks, a click to close.
  tone('sfx', 1400, 0.04, { type: 'square', vol: 0.03 * vol, wet: 0.3 });
  const hiss = Math.min(9, 0.4 + text.length * 0.055); // roughly how long the line will take
  air('sfx', hiss, { vol: 0.012 * vol, freq: 1800, q: 0.7, type: 'bandpass', wet: 0.2 });
  const finish = () => {
    if (speaking !== u) {
      return; // it was cut off, and whoever cut it has already moved on
    }
    speaking = null;
    tone('sfx', 900, 0.03, { type: 'square', vol: 0.02 * vol, wet: 0.3 });
    if (done) {
      done();
    }
  };
  u.onend = finish;
  u.onerror = finish; // an engine that gives up must not leave the scene waiting on it
  speaking = u;
  try {
    s.speak(u);
  } catch {
    speaking = null; // some engines refuse a line; the scene carries on without it
    return false;
  }
  return true;
}

/** Cuts the line off: a beat changed, the scene was skipped, or the dialog closed. */
export function stopVoice() {
  const s = synth();
  if (s && (s.speaking || s.pending)) {
    speaking = null;
    s.cancel();
  }
}
