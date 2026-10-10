// Dev server for tools/map-editor.html.
//
// It does three things the editor cannot do from a page:
//   - serves the repo, so the editor can import the real game modules instead of copying their maths
//   - stages edited data files in memory, so the sim runs the game's own functions over the
//     unsaved map without anything touching disk
//   - writes a file back when the editor asks, leaving a .bak the first time each session
//
// A module requested with ?v=N has its relative imports rewritten to carry the same ?v=N, which is
// what lets the editor re-import the whole graph after an edit instead of reloading the page.
//
// Usage: npm run editor   (http://127.0.0.1:8123/tools/map-editor.html)
import http from 'node:http';
import { createReadStream } from 'node:fs';
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const PORT = Number(process.argv[2] || 8123);

// Only these may be staged or written back.
const WRITABLE = new Set(['js/data/world.js', 'js/data/economy.js']);
const staged = new Map(); // repo-relative path -> unsaved source text
const backedUp = new Set();

const TYPES = {
  '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8', '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8', '.svg': 'image/svg+xml',
  '.png': 'image/png', '.jpg': 'image/jpeg', '.ico': 'image/x-icon',
};

const send = (res, code, body, type = 'application/json; charset=utf-8') => {
  res.writeHead(code, { 'content-type': type, 'cache-control': 'no-store' });
  res.end(body);
};

/** Resolve a URL path inside the repo, or null if it escapes it. */
function safe(rel) {
  const full = path.resolve(ROOT, '.' + path.posix.normalize('/' + rel));
  return full === ROOT || full.startsWith(ROOT + path.sep) ? full : null;
}

async function readBody(req) {
  let raw = '';
  for await (const chunk of req) {
    raw += chunk;
    if (raw.length > 8e6) throw new Error('body too large');
  }
  return raw ? JSON.parse(raw) : {};
}

// Carry the cache-busting version through every relative import, so one re-import gets a whole
// fresh module graph reading the staged data rather than the copies already in the browser.
function version(src, v) {
  const tag = (spec) => (spec.includes('?') ? spec : `${spec}?v=${v}`);
  return src
    .replace(/(\bfrom\s*)(['"])(\.[^'"]+)\2/g, (_, a, q, spec) => `${a}${q}${tag(spec)}${q}`)
    .replace(/(\bimport\s*\(\s*)(['"])(\.[^'"]+)\2/g, (_, a, q, spec) => `${a}${q}${tag(spec)}${q}`);
}

const routes = {
  // Hold an edited file in memory. The sim then imports exactly what the editor is showing.
  async stage(body) {
    const rel = String(body.path || '').replace(/^\/+/, '');
    if (!WRITABLE.has(rel)) throw new Error(`not writable: ${rel}`);
    if (typeof body.text !== 'string') throw new Error('bad text');
    staged.set(rel, body.text);
    return { ok: true, staged: [...staged.keys()] };
  },
  // Drop every staged edit; the next import sees what is on disk.
  async reset() {
    staged.clear();
    return { ok: true };
  },
  // Commit to disk. The editor sends whole files it produced by splicing, never by regenerating.
  async write(body) {
    const rel = String(body.path || '').replace(/^\/+/, '');
    if (!WRITABLE.has(rel)) throw new Error(`not writable: ${rel}`);
    const full = safe(rel);
    if (!full || typeof body.text !== 'string') throw new Error('bad request');
    if (!backedUp.has(rel)) {
      await fs.copyFile(full, full + '.bak');
      backedUp.add(rel);
      console.log(`backup  ${rel}.bak`);
    }
    await fs.writeFile(full, body.text, 'utf8');
    staged.delete(rel);
    console.log(`wrote   ${rel} (${body.text.length} bytes)`);
    return { ok: true, bytes: body.text.length };
  },
};

http.createServer(async (req, res) => {
  const url = new URL(req.url, 'http://127.0.0.1');

  if (req.method === 'POST') {
    const name = url.pathname.replace('/api/', '');
    const route = routes[name];
    if (!route) return send(res, 404, '{"error":"no route"}');
    try {
      return send(res, 200, JSON.stringify(await route(await readBody(req))));
    } catch (e) {
      return send(res, 400, JSON.stringify({ error: String(e.message || e) }));
    }
  }
  if (req.method !== 'GET') return send(res, 405, '{"error":"method"}');

  let rel = decodeURIComponent(url.pathname);
  if (rel.endsWith('/')) rel += 'index.html';
  const key = rel.replace(/^\/+/, '');
  const full = safe(rel);
  if (!full) return send(res, 403, 'forbidden', 'text/plain');

  const v = url.searchParams.get('v');
  const isJs = /\.m?js$/.test(full);
  try {
    // Staged text, or a module that needs its imports versioned, is served from memory.
    if (staged.has(key) || (isJs && v)) {
      const src = staged.has(key) ? staged.get(key) : await fs.readFile(full, 'utf8');
      return send(res, 200, v ? version(src, v) : src, TYPES['.js']);
    }
    const stat = await fs.stat(full);
    if (stat.isDirectory()) return send(res, 404, 'not found', 'text/plain');
    res.writeHead(200, {
      'content-type': TYPES[path.extname(full).toLowerCase()] || 'application/octet-stream',
      'cache-control': 'no-store',
    });
    createReadStream(full).pipe(res);
  } catch {
    send(res, 404, 'not found', 'text/plain');
  }
}).listen(PORT, '127.0.0.1', () => {
  console.log(`map editor: http://127.0.0.1:${PORT}/tools/map-editor.html`);
  console.log(`writable:   ${[...WRITABLE].join(', ')}`);
});
