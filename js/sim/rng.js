// Seeded RNG stored in the save (mulberry32), so offline catch-up and tests are reproducible.
export function rand(s) {
  s.rng = (s.rng + 0x6d2b79f5) >>> 0;
  let t = s.rng;
  t = Math.imul(t ^ (t >>> 15), t | 1);
  t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
  return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
}

export function range(s, min, max) {
  return min + (max - min) * rand(s);
}

export function pick(s, list) {
  return list[Math.floor(rand(s) * list.length)];
}

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
