// The only check that runs before a push: serve the repo, open it in a headless browser and fail on
// anything the page throws. It does not play the game; it catches the import, syntax and first-paint
// breakages that a static site cannot survive. Usage: npm run smoke
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { extname, join, normalize } from 'node:path';
import { createRequire } from 'node:module';

const ROOT = new URL('..', import.meta.url).pathname;
const PORT = 8123;
const TYPES = { '.html': 'text/html', '.js': 'text/javascript', '.mjs': 'text/javascript',
  '.css': 'text/css', '.json': 'application/json', '.svg': 'image/svg+xml', '.webmanifest': 'application/manifest+json' };

const server = createServer(async (req, res) => {
  const asked = join(ROOT, normalize(decodeURIComponent(req.url.split('?')[0])).replace(/^(\.\.[/\\])+/, ''));
  const path = asked.endsWith('/') ? join(asked, 'index.html') : asked;
  try {
    const body = await readFile(path);
    res.writeHead(200, { 'content-type': TYPES[extname(path)] || 'application/octet-stream' });
    res.end(body);
  } catch {
    res.writeHead(404).end();
  }
});
await new Promise((ok) => server.listen(PORT, ok));

// Playwright is not a dependency of the game; it is whatever the machine already has.
const require = createRequire(import.meta.url);
let chromium;
try {
  ({ chromium } = require('playwright'));
} catch {
  console.log('smoke: no playwright on this machine, nothing to run');
  server.close();
  process.exit(0);
}

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
const errors = [];
page.on('pageerror', (e) => errors.push(String(e)));
page.on('console', (m) => m.type() === 'error' && errors.push(m.text()));
await page.goto(`http://127.0.0.1:${PORT}/`, { waitUntil: 'networkidle' });
await page.waitForTimeout(3000);
const title = await page.title();
await browser.close();
server.close();

if (errors.length) {
  console.error('smoke: FAILED\n' + errors.join('\n'));
  process.exit(1);
}
console.log(`smoke: ok (${title})`);
