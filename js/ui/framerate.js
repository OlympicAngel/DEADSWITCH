// Ambient loops (spinners, pulses, glows) run at AMBIENT_FPS instead of the display rate. Every loop
// steps on one shared clock, so the screen is redrawn AMBIENT_FPS times a second at most instead of
// 60-120: the biggest battery cost left once the page itself is idle. Each loop keeps its own easing;
// only the sampling rate changes. Set to 0 to run loops at full frame rate.
export const AMBIENT_FPS = 30;

const period = AMBIENT_FPS ? 1000 / AMBIENT_FPS : 0;
// The shared clock ticks halfway between two display frames, so every part of the browser agrees on
// which frame a step belongs to. Measured from the display's frame times at start.
let offset = 0;
let frame = 1000 / 60;

const align = (t) => Math.round((t - offset) / period) * period + offset;

function quantize(anim, finite = false) {
  const t = anim.effect.getTiming();
  if ((!finite && t.iterations !== Infinity) || anim.__stepped) return;
  anim.__stepped = true;
  // Whole steps only (a 250 ms glide becomes 8 steps, 267 ms), so every step lands on the clock.
  const steps = Math.max(2, Math.round(Number(t.duration) / period));
  anim.effect.updateTiming({ easing: `steps(${steps})`, duration: steps * period });
  if (anim.startTime != null) anim.startTime = align(anim.startTime);
}

// The first moment at or after t when the shared clock steps. Changes made just after it are drawn in
// the frame that shows the step anyway (steps sit half a frame before a display frame).
export function nextStep(t) {
  return period ? Math.ceil((t - offset) / period) * period + offset : t;
}

export function stepAmbientLoops(root = document) {
  if (!period) return;
  // The display's frame interval is the shortest gap over a few frames (start-up work can skip some).
  let last = null;
  let n = 0;
  const probe = (ts) => {
    if (last != null) frame = Math.min(n > 1 ? frame : 34, Math.max(4, ts - last));
    last = ts;
    if (++n < 12) {
      requestAnimationFrame(probe);
      return;
    }
    offset = (ts + frame / 2) % period;
    for (const x of document.getAnimations()) if (x.__stepped && x.startTime != null) x.startTime = align(x.startTime);
  };
  requestAnimationFrame(probe);
  root.addEventListener('animationstart', (e) => {
    for (const a of e.target.getAnimations({ subtree: true })) {
      if (a.animationName === e.animationName) quantize(a);
    }
  });
  // Progress bars and rings glide to each new value; those glides step on the same clock.
  // Taps, pop-ins and other one-off feedback keep the full frame rate.
  root.addEventListener('transitionrun', (e) => {
    if (!GLIDES.has(e.propertyName)) return;
    for (const a of e.target.getAnimations()) {
      if (a.transitionProperty === e.propertyName) quantize(a, true);
    }
  });
  for (const a of document.getAnimations()) quantize(a);
}

const GLIDES = new Set(['width', 'stroke-dasharray']);
