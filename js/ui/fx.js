// Visual and haptic juice: floating numbers, flashes, shakes, particle bursts, vibration.
const HAPTICS_KEY = 'deadswitch.haptics';
let haptics = true;
try {
  haptics = localStorage.getItem(HAPTICS_KEY) !== '0';
} catch {
  // Storage blocked: keep haptics on.
}

const reduced = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches;

export const hapticsOn = () => haptics;

export function setHaptics(on) {
  haptics = on;
  try {
    localStorage.setItem(HAPTICS_KEY, on ? '1' : '0');
  } catch {
    // ignore
  }
}

export function vibrate(pattern) {
  if (haptics && navigator.vibrate) {
    try {
      navigator.vibrate(pattern);
    } catch {
      // Some browsers throw before the first user gesture.
    }
  }
}

export function floatText(anchor, html, cls = '') {
  if (!anchor || reduced()) {
    return;
  }
  const r = anchor.getBoundingClientRect();
  const el = document.createElement('div');
  el.className = 'float-text ' + cls;
  el.innerHTML = html;
  el.style.left = r.left + r.width / 2 + 'px';
  el.style.top = r.top + 'px';
  document.body.appendChild(el);
  el.addEventListener('animationend', () => el.remove());
}

export function flash(el, cls = 'flash') {
  if (!el) {
    return;
  }
  el.classList.remove(cls);
  void el.offsetWidth;
  el.classList.add(cls);
  el.addEventListener('animationend', () => el.classList.remove(cls), { once: true });
}

export function shake(strong = false) {
  if (reduced()) {
    return;
  }
  flash(document.body, strong ? 'shake-hard' : 'shake');
}

// Full-screen colour flash (red for hits, cyan for wins).
export function screenFlash(kind = 'alert') {
  const el = document.createElement('div');
  el.className = 'screen-flash sf-' + kind;
  document.body.appendChild(el);
  el.addEventListener('animationend', () => el.remove());
}

export function burst(anchor, color = 'var(--hud)', count = 14) {
  if (!anchor || reduced()) {
    return;
  }
  const r = anchor.getBoundingClientRect();
  const cx = r.left + r.width / 2;
  const cy = r.top + r.height / 2;
  for (let i = 0; i < count; i++) {
    const p = document.createElement('i');
    p.className = 'spark';
    const a = (Math.PI * 2 * i) / count + Math.random() * 0.4;
    const d = 40 + Math.random() * 50;
    p.style.left = cx + 'px';
    p.style.top = cy + 'px';
    p.style.background = color;
    p.style.setProperty('--dx', Math.cos(a) * d + 'px');
    p.style.setProperty('--dy', Math.sin(a) * d + 'px');
    document.body.appendChild(p);
    p.addEventListener('animationend', () => p.remove());
  }
}
