const SUFFIXES = ['', 'K', 'M', 'B', 'T', 'Qa', 'Qi', 'Sx', 'Sp', 'Oc', 'No', 'Dc'];

export function num(n) {
  if (!Number.isFinite(n)) {
    return '∞';
  }
  const sign = n < 0 ? '-' : '';
  const a = Math.abs(n);
  if (a < 1000) {
    if (a === 0 || a >= 100 || Number.isInteger(a)) {
      return sign + Math.floor(a);
    }
    return sign + (a < 10 ? a.toFixed(2) : a.toFixed(1)).replace(/\.?0+$/, '');
  }
  const tier = Math.floor(Math.log10(a) / 3);
  if (tier >= SUFFIXES.length) {
    return sign + a.toExponential(2).replace('+', '');
  }
  const scaled = a / Math.pow(1000, tier);
  const digits = scaled >= 100 ? 0 : scaled >= 10 ? 1 : 2;
  // Floor so a displayed amount never looks affordable when it is not.
  const p = Math.pow(10, digits);
  return sign + (Math.floor(scaled * p) / p).toFixed(digits) + SUFFIXES[tier];
}

// Whole units with thousands separators (header stockpiles); compact suffixes only past 99,999.
export function whole(n) {
  const a = Math.floor(n);
  return Math.abs(a) < 1e5 ? a.toLocaleString('en-US') : num(a);
}

export function rate(n) {
  if (Math.abs(n) < 0.005) {
    return '±0/s';
  }
  const r = num(Math.abs(n));
  return (n < 0 ? '−' : '+') + r + '/s';
}

export function time(seconds) {
  const s = Math.max(0, Math.ceil(seconds));
  if (s < 60) {
    return s + 's';
  }
  const m = Math.floor(s / 60);
  // Zero tails are dropped: 6h, not 6h 00m.
  const tail = (n, unit) => (n ? ' ' + String(n).padStart(2, '0') + unit : '');
  if (m < 60) {
    return m + 'm' + tail(s % 60, 's');
  }
  const h = Math.floor(m / 60);
  if (h < 48) {
    return h + 'h' + tail(m % 60, 'm');
  }
  return Math.floor(h / 24) + 'd' + (h % 24 ? ' ' + (h % 24) + 'h' : '');
}

export function pct(x) {
  return Math.round(x * 100) + '%';
}

export function esc(str) {
  return String(str).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
}
