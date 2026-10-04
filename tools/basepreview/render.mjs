#!/usr/bin/env node
// Renders artifacts/basepreview/scene.json (from `dotnet run --project src/Deadswitch.Cli -- art export`) to PNG.
// usage: node tools/basepreview/render.mjs [--out file.png] [--w 1080] [--h 1920] [--t seconds] [--target x,y,z --dist N --az deg] [--hour 0-24] [--report 1]
import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);
const playwright = require('/opt/node22/lib/node_modules/playwright');
const HERE = path.dirname(new URL(import.meta.url).pathname);
const ROOT = path.resolve(HERE, '../..');
const args = process.argv.slice(2);
const opt = (n, d) => { const i = args.indexOf('--' + n); return i >= 0 ? args[i + 1] : d; };
const out = opt('out', path.join(ROOT, 'artifacts/basepreview/base.png'));
const scenePath = opt('scene', path.join(ROOT, 'artifacts/basepreview/scene.json'));
const w = opt('w', '1080'), h = opt('h', '1920'), t = opt('t', '3.2');
const THREE_DIR = path.join(HERE, 'node_modules/three');
if (!fs.existsSync(THREE_DIR)) { console.error('Run: (cd tools/basepreview && npm install)'); process.exit(2); }

const types = { '.js': 'text/javascript', '.json': 'application/json', '.html': 'text/html', '.ttf': 'font/ttf' };
const server = http.createServer((req, res) => {
  const url = decodeURIComponent(req.url.split('?')[0]);
  let file = url === '/' ? path.join(HERE, 'page.html') : url === '/scene.json' ? scenePath : url === '/look.json' ? path.join(ROOT, 'unity/Assets/Game/Resources/Base/BaseLook.json') : url === '/salvage.txt' ? path.join(ROOT, 'unity/Assets/Game/Resources/Shaders/SalvageCommon.hlsl')
    : url.startsWith('/three/') ? path.join(THREE_DIR, url.slice(7))
    : url.startsWith('/fonts/') ? path.join(ROOT, 'unity/Assets/Game/Resources/Fonts', path.basename(url)) : null;
  if (!file || !fs.existsSync(file)) { res.writeHead(404); res.end(); return; }
  res.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream' });
  fs.createReadStream(file).pipe(res);
}).listen(0);
const port = server.address().port;

const browser = await playwright.chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
const page = await browser.newPage({ viewport: { width: +w, height: +h } });
page.on('console', m => { if (m.type() === 'error') console.error('page:', m.text()); });
page.on('pageerror', e => console.error('page error:', e.message));
const extra = ['target', 'dist', 'az', 'report', 'hour'].filter(k => opt(k, null) !== null).map(k => `&${k}=${opt(k)}`).join('');
await page.goto(`http://127.0.0.1:${port}/?w=${w}&h=${h}&t=${t}${extra}`);
await page.waitForFunction(() => window.__done, null, { timeout: 180000 });
fs.mkdirSync(path.dirname(out), { recursive: true });
await page.screenshot({ path: out, timeout: 300000 });
console.log('wrote ' + path.relative(ROOT, out), await page.evaluate(() => window.__done));
await browser.close();
server.close();
