// Deterministic rolls. Nothing here draws from a moving stream: every roll is a pure hash of the
// save's own seed, the day of play it is on, which system is asking, and how many times that system
// has asked. Three things follow. The same state always has the same future, so it can be read ahead
// (`peek`) without playing it out. One system rolling never shifts another's results. And the day
// seed turns over every day, so a run never repeats itself.

const DAY = 86400; // seconds of play per seed

/** 32-bit avalanche mix of a list of integers. */
export function mix(...nums) {
  let h = 0x9e3779b9;
  for (const n of nums) {
    h = Math.imul(h ^ (n >>> 0), 0x85ebca6b);
    h = (h ^ (h >>> 13)) >>> 0;
  }
  h = Math.imul(h ^ (h >>> 16), 0xc2b2ae35);
  return (h ^ (h >>> 16)) >>> 0;
}

const IDS = {};
function streamId(name) {
  if (IDS[name] === undefined) {
    let h = 2166136261;
    for (let i = 0; i < name.length; i++) {
      h = Math.imul(h ^ name.charCodeAt(i), 16777619);
    }
    IDS[name] = h >>> 0;
  }
  return IDS[name];
}

/** The seed in force on the day of play the state has reached. */
export const daySeed = (s) => mix(s.rng, Math.floor(s.playTime / DAY));

/** The nth roll of a stream, in [0, 1). Pure: it takes nothing. */
export const rollAt = (s, stream, n) => mix(daySeed(s), streamId(stream), n) / 4294967296;

/** Takes a stream's next roll. */
export function rand(s, stream = 'main') {
  const n = ((s.rolls && s.rolls[stream]) || 0) + 1;
  if (!s.rolls) s.rolls = {};
  s.rolls[stream] = n;
  return rollAt(s, stream, n);
}

/** What a stream will roll `ahead` draws from now, without taking it. */
export const peek = (s, stream, ahead = 1) => rollAt(s, stream, ((s.rolls && s.rolls[stream]) || 0) + ahead);

export const range = (s, min, max, stream) => min + (max - min) * rand(s, stream);

export const pick = (s, list, stream) => list[Math.floor(rand(s, stream) * list.length)];

// Contest odds: a^k / (a^k + b^k). Equal strength is a coin flip; double strength is ~94% at k=4.
export function odds(a, b, k) {
  if (b <= 0) {
    return 1;
  }
  if (a <= 0) {
    return 0;
  }
  const r = Math.pow(b / a, k);
  return 1 / (1 + r);
}
