// Surgical source editing for tools/map-editor.html.
//
// The editor never regenerates a data file. It finds the exact span of the one value it is changing
// and replaces those characters, so every comment, blank line and hand-written bit of formatting in
// the file survives untouched. Only brand new entries are generated, and only in the house style.
//
// Pure ES module, no DOM and no fs: the browser imports it, node can test it.

const QUOTES = `'"\``;

/** Walk `src` from `i`, skipping one string, template or comment; returns the index after it. */
function skip(src, i) {
  const ch = src[i];
  if (ch === '/' && src[i + 1] === '/') {
    const nl = src.indexOf('\n', i);
    return nl === -1 ? src.length : nl;
  }
  if (ch === '/' && src[i + 1] === '*') {
    const end = src.indexOf('*/', i + 2);
    return end === -1 ? src.length : end + 2;
  }
  if (!QUOTES.includes(ch)) return i;
  for (let j = i + 1; j < src.length; j++) {
    if (src[j] === '\\') {
      j++;
      continue;
    }
    if (src[j] === ch) return j + 1;
  }
  return src.length;
}

/**
 * Span of the bracketed literal that starts at `open` (which must index a `{` or `[`).
 * @returns {{start: number, end: number}} end is just past the closing bracket.
 */
export function literalSpan(src, open) {
  const close = { '{': '}', '[': ']', '(': ')' }[src[open]];
  if (!close) throw new Error(`not a bracket at ${open}: ${src[open]}`);
  let depth = 0;
  for (let i = open; i < src.length; i++) {
    const j = skip(src, i);
    if (j !== i) {
      i = j - 1;
      continue;
    }
    const ch = src[i];
    if (ch === '{' || ch === '[' || ch === '(') depth++;
    else if (ch === '}' || ch === ']' || ch === ')') {
      depth--;
      if (depth === 0) return { start: open, end: i + 1 };
    }
  }
  throw new Error(`unclosed literal at ${open}`);
}

/** Span of the literal assigned by `export const NAME = ...`. */
export function exportSpan(src, name) {
  const re = new RegExp(`export\\s+const\\s+${name}\\s*=\\s*`, 'g');
  for (let m = re.exec(src); m; m = re.exec(src)) {
    const open = m.index + m[0].length;
    if ('{['.includes(src[open])) return literalSpan(src, open);
  }
  throw new Error(`export const ${name} not found`);
}

/** Top-level `{...}` members of the array literal spanning [start, end). */
export function arrayItems(src, { start, end }) {
  const out = [];
  let depth = 0;
  for (let i = start; i < end; i++) {
    const j = skip(src, i);
    if (j !== i) {
      i = j - 1;
      continue;
    }
    const ch = src[i];
    if (ch === '[' || ch === '(') depth++;
    else if (ch === ']' || ch === ')') depth--;
    else if (ch === '{') {
      if (depth === 1) {
        const span = literalSpan(src, i);
        out.push(span);
        i = span.end - 1;
      } else depth++;
    } else if (ch === '}') depth--;
  }
  return out;
}

/**
 * Span of `key`'s value inside the object literal [start, end), plus the span of the whole
 * `key: value` entry (used when removing it). Null when the key is absent.
 */
export function keySpan(src, { start, end }, key) {
  let depth = 0;
  for (let i = start; i < end; i++) {
    const j = skip(src, i);
    if (j !== i) {
      i = j - 1;
      continue;
    }
    const ch = src[i];
    if ('{[('.includes(ch)) {
      depth++;
      continue;
    }
    if ('}])'.includes(ch)) {
      depth--;
      continue;
    }
    if (depth !== 1) continue;
    // A key sits at depth 1 and is followed by a colon.
    const m = /^(['"]?)([A-Za-z_$][\w$]*)\1\s*:/.exec(src.slice(i, Math.min(end, i + 64)));
    if (!m || m[2] !== key) continue;
    // Make sure we are at a token boundary, not mid-identifier.
    if (/[\w$.]/.test(src[i - 1] || '')) continue;
    const vStart = i + m[0].length + (/\s/.test(src[i + m[0].length]) ? 1 : 0);
    let k = i + m[0].length;
    while (k < end && /\s/.test(src[k])) k++;
    let vEnd = k;
    let d = 0;
    for (; vEnd < end; vEnd++) {
      const n = skip(src, vEnd);
      if (n !== vEnd) {
        vEnd = n - 1;
        continue;
      }
      const c = src[vEnd];
      if ('{[('.includes(c)) d++;
      else if ('}])'.includes(c)) {
        if (d === 0) break;
        d--;
      } else if (c === ',' && d === 0) break;
    }
    // Trim trailing whitespace and any line comment that belongs to this entry's line.
    let valEnd = vEnd;
    while (valEnd > k && /\s/.test(src[valEnd - 1])) valEnd--;
    let entryEnd = vEnd;
    if (src[entryEnd] === ',') entryEnd++;
    return { start: k, end: valEnd, entryStart: i, entryEnd, vStart };
  }
  return null;
}

/** Replace `key`'s value inside the object at `obj`; appends the key when it is missing. */
export function setKey(src, obj, key, valueText) {
  const found = keySpan(src, obj, key);
  if (found) {
    return { text: src.slice(0, found.start) + valueText + src.slice(found.end), shift: valueText.length - (found.end - found.start) };
  }
  // Append before the closing brace, matching the indent of the last entry.
  let i = obj.end - 1;
  while (i > obj.start && /\s/.test(src[i - 1])) i--;
  const needsComma = src[i - 1] !== ',' && src[i - 1] !== '{';
  const indent = /\n([ \t]*)[^\s]/.exec(src.slice(obj.start, obj.end))?.[1] ?? '  ';
  const ins = `${needsComma ? ',' : ''}\n${indent}${key}: ${valueText},`;
  return { text: src.slice(0, i) + ins + src.slice(i), shift: ins.length };
}

/** Remove `key: value` (and its comma) from the object at `obj`. */
export function removeKey(src, obj, key) {
  const found = keySpan(src, obj, key);
  if (!found) return { text: src, shift: 0 };
  // Take the blank space on exactly one side of the entry, never both: in `a: 1, b: 2, c: 3`,
  // eating the space before AND after `b` welds the survivors into `a: 1,c: 3`.
  let pre = found.entryStart;
  while (pre > obj.start && (src[pre - 1] === ' ' || src[pre - 1] === '\t')) pre--;
  let post = found.entryEnd;
  while (src[post] === ' ' || src[post] === '\t') post++;

  let a = found.entryStart;
  let b = found.entryEnd;
  if (src[pre - 1] === '\n' || src[pre - 1] === '{') {
    // First on its line or in its object: keep the indent in front and take the gap behind instead.
    if (src[post] === '\n') { a = pre; b = post + 1; } // it had the line to itself
    else b = post;
  } else {
    a = pre; // mid-line: the gap in front of it goes with it
  }
  return { text: src.slice(0, a) + src.slice(b), shift: -(b - a) };
}

// ---------- value formatting, in the file's own house style ----------

export const quote = (v) => `'${String(v).replace(/\\/g, '\\\\').replace(/'/g, "\\'")}'`;

/** A number as short as it can be written without losing the value the editor holds. */
export function num(v) {
  if (!Number.isFinite(v)) throw new Error(`not a number: ${v}`);
  if (Number.isInteger(v)) return String(v);
  for (let p = 1; p <= 6; p++) {
    const r = Number(v.toFixed(p));
    if (r === Number(v.toPrecision(12))) return String(r);
  }
  return String(v);
}

/** One value, inline, the way world.js writes them. */
export function value(v) {
  if (v === null) return 'null';
  if (v === undefined) return 'undefined';
  if (typeof v === 'boolean') return String(v);
  if (typeof v === 'number') return num(v);
  if (typeof v === 'string') return quote(v);
  if (Array.isArray(v)) return `[${v.map(value).join(', ')}]`;
  const body = Object.entries(v).map(([k, x]) => `${k}: ${value(x)}`).join(', ');
  return body ? `{ ${body} }` : '{}';
}

/**
 * A new SECTORS entry, laid out like the ones already in the file:
 * identity on one line, the war numbers on the next, lore on its own.
 */
export function sectorText(sec, indent = '  ') {
  const i2 = indent + '  ';
  const head = ['id', 'name', 'faction', 'chapter', 'x', 'y']
    .map((k) => `${k}: ${value(sec[k] ?? null)}`).join(', ');
  const warParts = [`links: ${value(sec.links || [])}`, `defense: ${num(sec.defense || 0)}`];
  if (sec.core !== undefined) warParts.push(`core: ${num(sec.core)}`);
  if (sec.boss) warParts.push('boss: true');
  if (sec.bonus && Object.keys(sec.bonus).length) warParts.push(`bonus: ${value(sec.bonus)}`);
  const lines = [`${indent}{`, `${i2}${head},`, `${i2}${warParts.join(', ')},`];
  if (sec.lore) lines.push(`${i2}lore: ${quote(sec.lore)},`);
  lines.push(`${indent}},`);
  return lines.join('\n');
}

/** Append `itemText` as the last member of the array literal at `arr`. */
export function insertArrayItem(src, arr, itemText) {
  let i = arr.end - 1; // the closing bracket
  while (i > arr.start && /[ \t]/.test(src[i - 1])) i--;
  const hasItems = /[^\s[]/.test(src.slice(arr.start + 1, i));
  const needsNewline = src[i - 1] !== '\n';
  return src.slice(0, i) + (needsNewline && hasItems ? '\n' : '') + itemText + '\n' + src.slice(i);
}

/** Remove the member spanning `item` from an array literal, with its comma and its own line. */
export function removeArrayItem(src, item) {
  let a = item.start;
  let b = item.end;
  if (src[b] === ',') b++;
  while (a > 0 && /[ \t]/.test(src[a - 1])) a--;
  if (src[a - 1] === '\n' && src[b] === '\n') b++; // take the whole line, not a blank one
  return src.slice(0, a) + src.slice(b);
}

/**
 * Apply a list of field edits to one source string, back to front so earlier spans stay valid.
 * Each edit is {start, end, text}. Overlapping edits are a bug and throw.
 */
export function applyEdits(src, edits) {
  const sorted = [...edits].sort((a, b) => b.start - a.start);
  let last = Infinity;
  let out = src;
  for (const e of sorted) {
    if (e.end > last) throw new Error(`overlapping edits at ${e.start}..${e.end}`);
    last = e.start;
    out = out.slice(0, e.start) + e.text + out.slice(e.end);
  }
  return out;
}
