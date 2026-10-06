// Visual juice: floating numbers, flashes, screen shake, particle bursts.
const reduced = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches;

export function floatText(anchor, text, cls = '') {
  if (!anchor || reduced()) {
    return;
  }
  const r = anchor.getBoundingClientRect();
  const el = document.createElement('div');
  el.className = 'float-text ' + cls;
  el.textContent = text;
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

export function shake() {
  if (reduced()) {
    return;
  }
  flash(document.body, 'shake');
}

export function burst(anchor, color = 'var(--accent)', count = 14) {
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
