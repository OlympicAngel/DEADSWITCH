// A collapsible view of any plain object. Every number, string and boolean is a field; editable trees
// write the parsed value straight back into the object it came from, so the debugger can reach every
// corner of the save without a form per feature.

const isLeaf = (v) => v === null || typeof v !== 'object';
const short = (v) => (Array.isArray(v) ? `[${v.length}]` : `{${Object.keys(v).length}}`);

/** Reads a dotted path ('res.money', 'nodes.rust.a'). */
export function getPath(obj, path) {
  return path.split('.').reduce((o, k) => (o == null ? o : o[k]), obj);
}

/** Writes a dotted path, parsing the text back to the type the leaf had. */
export function setPath(obj, path, raw) {
  const keys = path.split('.');
  const last = keys.pop();
  const host = keys.reduce((o, k) => (o == null ? o : o[k]), obj);
  if (!host || !(last in host)) {
    return false;
  }
  const was = host[last];
  if (typeof was === 'number') {
    const n = Number(raw);
    if (!Number.isFinite(n)) return false;
    host[last] = n;
  } else if (typeof was === 'boolean') {
    host[last] = raw === 'true' || raw === '1';
  } else if (was === null || typeof was === 'string') {
    host[last] = raw;
  } else {
    return false;
  }
  return true;
}

// One row per key. Expanded branches recurse; everything else shows its size and opens on tap.
export function treeHtml(obj, open, { path = '', depth = 0, editable = true } = {}) {
  if (depth > 12 || isLeaf(obj)) {
    return '';
  }
  return Object.keys(obj).map((k) => {
    const full = path ? `${path}.${k}` : k;
    const v = obj[k];
    const pad = `style="padding-left:${6 + depth * 10}px"`;
    if (isLeaf(v)) {
      const val = v === null ? 'null' : String(v);
      return `<div class="dbg-row" ${pad}><span class="dbg-k">${k}</span>${editable
        ? `<input class="dbg-in" data-path="${full}" value="${val.replace(/"/g, '&quot;')}" spellcheck="false">`
        : `<b class="dbg-v" data-live="${full}">${val}</b>`}</div>`;
    }
    const isOpen = open.has(full);
    const head = `<div class="dbg-row branch ${isOpen ? 'open' : ''}" ${pad} data-open="${full}"><span class="dbg-k">${isOpen ? '▾' : '▸'} ${k}</span><b class="dbg-v dim">${short(v)}</b></div>`;
    return head + (isOpen ? treeHtml(v, open, { path: full, depth: depth + 1, editable }) : '');
  }).join('');
}
