#!/usr/bin/env node
// Headless UI preview: renders Unity UI Toolkit layouts (UXML + USS) to PNG with Chromium.
// Mirrors Unity's layout defaults (Yoga flex, column, border-box) and the runtime behaviors in
// unity/Assets/Game/Runtime/UI (corner brackets, segmented meters, sparklines, gauges) closely enough
// to review hierarchy, spacing, typography and color without the Editor. See docs/agents/quality-bar.md.
//
// usage: node tools/uipreview/preview.mjs <screen.uxml> [--out file.png] [--bg image.png] [--height 2340] [--scale 0.5]
import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);
let playwright;
try { playwright = require('playwright'); } catch { playwright = require(path.join(process.execPath, '../../lib/node_modules/playwright')); }

const ROOT = path.resolve(path.dirname(new URL(import.meta.url).pathname), '../..');
const UI = path.join(ROOT, 'unity/Assets/Game/Resources/UI');
const FONTS = path.join(ROOT, 'unity/Assets/Game/Resources/Fonts');

const args = process.argv.slice(2);
const opt = (name, dflt) => { const i = args.indexOf('--' + name); return i >= 0 ? args[i + 1] : dflt; };
const uxmlPath = args.find(a => a.endsWith('.uxml'));
if (!uxmlPath) { console.error('usage: preview.mjs <screen.uxml> [--out png]'); process.exit(2); }
const out = opt('out', path.join(ROOT, 'artifacts/uipreview', path.basename(uxmlPath, '.uxml') + '.png'));
const width = 1080, height = parseInt(opt('height', '2340'), 10), scale = parseFloat(opt('scale', '0.5'));
const bg = opt('bg', null);

// ---------- UXML -> HTML ----------
function parseXml(src) {
  src = src.replace(/<\?xml[^>]*\?>/g, '').replace(/<!--[\s\S]*?-->/g, '');
  const root = { tag: '#root', attrs: {}, children: [] };
  const stack = [root];
  const re = /<\s*(\/?)\s*([\w:.-]+)((?:\s+[\w:.-]+\s*=\s*"[^"]*")*)\s*(\/?)\s*>/g;
  let m;
  while ((m = re.exec(src))) {
    const [, closing, tag, attrSrc, selfClose] = m;
    if (closing) { stack.pop(); continue; }
    const attrs = {};
    attrSrc.replace(/([\w:.-]+)\s*=\s*"([^"]*)"/g, (_, k, v) => { attrs[k] = v; });
    const node = { tag, attrs, children: [] };
    stack[stack.length - 1].children.push(node);
    if (!selfClose) stack.push(node);
  }
  return root;
}

const TYPE = { VisualElement: 'ui-ve', Label: 'ui-label unity-label', Button: 'ui-button', ScrollView: 'ui-scroll', Image: 'ui-image', TextElement: 'ui-label' };
const esc = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');
const unesc = s => s.replace(/&amp;/g, '&').replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&#10;/g, '\n');
const styleSheets = [];

function toHtml(node) {
  const local = node.tag.split(':').pop();
  if (local === 'UXML' || node.tag === '#root') return node.children.map(toHtml).join('');
  if (local === 'Style') { styleSheets.push(node.attrs.src); return ''; }
  const cls = [TYPE[local] || 'ui-ve', ...(node.attrs.class || '').split(/\s+/).filter(Boolean)];
  const id = node.attrs.name ? ` id="${node.attrs.name}" data-name="${node.attrs.name}"` : '';
  const style = node.attrs.style ? ` style="${ussToCss(node.attrs.style, true)}"` : '';
  const text = node.attrs.text !== undefined ? esc(unesc(node.attrs.text)) : '';
  return `<div class="${cls.join(' ')}"${id}${style}>${text}${node.children.map(toHtml).join('')}</div>`;
}

// ---------- USS -> CSS ----------
const EASE = { 'ease-out-cubic': 'cubic-bezier(0.33,1,0.68,1)', 'ease-in-out-sine': 'cubic-bezier(0.37,0,0.63,1)', 'ease-out-back': 'cubic-bezier(0.34,1.56,0.64,1)' };
const ALIGN = { 'upper-left': ['left', 'flex-start'], 'upper-center': ['center', 'flex-start'], 'upper-right': ['right', 'flex-start'],
  'middle-left': ['left', 'center'], 'middle-center': ['center', 'center'], 'middle-right': ['right', 'center'],
  'lower-left': ['left', 'flex-end'], 'lower-center': ['center', 'flex-end'], 'lower-right': ['right', 'flex-end'] };

function declToCss(prop, val) {
  prop = prop.trim(); val = val.trim();
  if (prop === '-unity-font-definition' || prop === '-unity-font') {
    const m = /resource\("Fonts\/([^"]+)"\)/.exec(val);
    return m ? `font-family:'${m[1]}'` : '';
  }
  if (prop === '-unity-text-align') { const a = ALIGN[val] || ['left', 'center']; return `text-align:${a[0]};justify-content:${a[1]}`; }
  if (prop === '-unity-font-style') return val.includes('bold') ? 'font-weight:700' : '';
  if (prop.startsWith('-unity-')) return '';
  if (prop === 'transition-timing-function') return `transition-timing-function:${val.split(',').map(v => EASE[v.trim()] || v).join(',')}`;
  return `${prop}:${val}`;
}

function ussToCss(src, inline = false) {
  if (inline) return src.split(';').filter(d => d.includes(':')).map(d => { const i = d.indexOf(':'); return declToCss(d.slice(0, i), d.slice(i + 1)); }).filter(Boolean).join(';');
  src = src.replace(/\/\*[\s\S]*?\*\//g, '');
  return src.replace(/([^{}]+)\{([^{}]*)\}/g, (_, sel, body) => {
    sel = sel.replace(/(^|[\s>,+~])(VisualElement|Label|Button|ScrollView|Image)\b/g, (m, pre, t) => pre + '.' + TYPE[t].split(' ')[0]);
    const decls = body.split(';').filter(d => d.includes(':')).map(d => { const i = d.indexOf(':'); return declToCss(d.slice(0, i), d.slice(i + 1)); }).filter(Boolean);
    return `${sel.trim()}{${decls.join(';')}}\n`;
  });
}

// ---------- page ----------
const tree = parseXml(fs.readFileSync(uxmlPath, 'utf8'));
const body = toHtml(tree);
const sheets = ['Tokens.uss', 'Components.uss', ...styleSheets.map(s => path.basename(s))];
const css = [...new Set(sheets)].map(f => { const p = path.join(UI, f); return fs.existsSync(p) ? ussToCss(fs.readFileSync(p, 'utf8')) : ''; }).join('\n');
const fonts = fs.readdirSync(FONTS).filter(f => f.endsWith('.ttf')).map(f =>
  `@font-face{font-family:'${f.replace('.ttf', '')}';src:url(data:font/ttf;base64,${fs.readFileSync(path.join(FONTS, f)).toString('base64')})}`).join('\n');
const bgCss = bg ? `background:url(data:image/png;base64,${fs.readFileSync(bg).toString('base64')}) center/cover;` :
  'background: radial-gradient(ellipse at 50% 42%, #3a3f36 0%, #232823 38%, #121513 75%);';

const html = `<!doctype html><html><head><meta charset="utf-8"><style>
${fonts}
html,body{margin:0;width:${width}px;height:${height}px;overflow:hidden;${bgCss}}
.ui-ve,.ui-label,.ui-button,.ui-scroll,.ui-image{display:flex;flex-direction:column;box-sizing:border-box;position:relative;flex-shrink:1;min-width:0;min-height:0;border:0 solid transparent}
.ui-label,.ui-button{white-space:nowrap;font-family:'IBMPlexMono-Regular';line-height:1.25}
.ui-scroll{overflow:hidden}
#preview-root{position:absolute;inset:0}
${css}
</style></head><body><div id="preview-root" class="ui-ve ds-root">${body}</div>
<div id="crt" style="position:absolute;inset:0;pointer-events:none;
  background: repeating-linear-gradient(to bottom, rgba(0,0,0,0.10) 0 2px, transparent 2px 6px),
  radial-gradient(ellipse at center, transparent 45%, rgba(4,6,5,0.55) 100%);"></div>
<script>
// Mirrors of the C# element behaviors (Runtime/UI/Behaviors.cs).
for (const p of document.querySelectorAll('.ds-panel')) for (const c of ['tl','tr','bl','br']) { const d = document.createElement('div'); d.className = 'ui-ve ds-corner ds-corner--' + c; p.appendChild(d); }
for (const m of document.querySelectorAll('.ds-meter')) {
  const n = 20, on = Math.round(n * parseFloat(m.dataset.fill || (m.id && m.id.includes('corruption') ? '0.23' : '0.62')));
  for (let i = 0; i < n; i++) { const s = document.createElement('div'); s.className = 'ui-ve ds-meter__seg' + (i < on ? ' is-on' : ''); m.appendChild(s); }
}
const cs = getComputedStyle(document.querySelector('.ds-root') || document.body);
const v = n => cs.getPropertyValue(n).trim();
for (const el of document.querySelectorAll('.ds-sparkline')) {
  const w = el.clientWidth, h = el.clientHeight, pts = []; let y = 0.55;
  for (let i = 0; i <= 48; i++) { y = Math.min(0.92, Math.max(0.12, y + Math.sin(i * 0.7) * 0.06 + (i % 7 === 0 ? -0.18 : 0.02))); pts.push([i / 48 * w, (1 - y) * h]); }
  const poly = pts.map(p => p.join(',')).join(' ');
  el.innerHTML = '<svg width="'+w+'" height="'+h+'"><polygon points="0,'+h+' '+poly+' '+w+','+h+'" fill="'+v('--c-phosphor-glow')+'" opacity="0.35"/><polyline points="'+poly+'" fill="none" stroke="'+v('--c-phosphor')+'" stroke-width="3"/></svg>';
}
// Icons.cs mirror
const poly = (n, r, cx = 0.5, cy = 0.5) => Array.from({ length: n + 1 }, (_, i) => [cx + Math.cos(Math.PI * 2 * i / n) * r, cy + Math.sin(Math.PI * 2 * i / n) * r]);
const P = (...a) => a.reduce((acc, v, i) => (i % 2 ? acc[acc.length - 1].push(v) : acc.push([v]), acc), []);
const GLYPHS = {
  base: [P(0.1,0.9,0.1,0.45,0.5,0.15,0.9,0.45,0.9,0.9,0.1,0.9), P(0.4,0.9,0.4,0.62,0.6,0.62,0.6,0.9)],
  map: [poly(6, 0.42), P(0.44,0.44,0.56,0.44,0.56,0.56,0.44,0.56,0.44,0.44)],
  core: [poly(32, 0.4), P(0.5,0.3,0.7,0.5,0.5,0.7,0.3,0.5,0.5,0.3)],
  bolt: [P(0.58,0.06,0.24,0.56,0.5,0.56,0.4,0.94,0.76,0.42,0.5,0.42,0.58,0.06)],
  chip: [P(0.25,0.25,0.75,0.25,0.75,0.75,0.25,0.75,0.25,0.25), P(0.4,0.4,0.6,0.4,0.6,0.6,0.4,0.6,0.4,0.4), P(0.38,0.08,0.38,0.25), P(0.62,0.08,0.62,0.25), P(0.38,0.75,0.38,0.92), P(0.62,0.75,0.62,0.92), P(0.08,0.38,0.25,0.38), P(0.08,0.62,0.25,0.62), P(0.75,0.38,0.92,0.38), P(0.75,0.62,0.92,0.62)],
  people: [poly(16, 0.15, 0.5, 0.3), P(0.18,0.92,0.22,0.66,0.36,0.54,0.64,0.54,0.78,0.66,0.82,0.92)],
  cross: [P(0.38,0.1,0.62,0.1,0.62,0.38,0.9,0.38,0.9,0.62,0.62,0.62,0.62,0.9,0.38,0.9,0.38,0.62,0.1,0.62,0.1,0.38,0.38,0.38,0.38,0.1)],
  battery: [P(0.12,0.3,0.82,0.3,0.82,0.7,0.12,0.7,0.12,0.3), P(0.82,0.42,0.92,0.42,0.92,0.58,0.82,0.58), P(0.24,0.42,0.24,0.58), P(0.4,0.42,0.4,0.58), P(0.56,0.42,0.56,0.58)],
  ops: [poly(28, 0.3), P(0.5,0.02,0.5,0.25), P(0.5,0.75,0.5,0.98), P(0.02,0.5,0.25,0.5), P(0.75,0.5,0.98,0.5)],
  shield: [P(0.5,0.08,0.86,0.2,0.82,0.55,0.5,0.92,0.18,0.55,0.14,0.2,0.5,0.08), P(0.5,0.24,0.5,0.74)],
  dark: [P(0.08,0.5,0.3,0.3,0.5,0.24,0.7,0.3,0.92,0.5,0.7,0.7,0.5,0.76,0.3,0.7,0.08,0.5), P(0.16,0.86,0.84,0.14)],
  evacuate: [P(0.55,0.15,0.15,0.15,0.15,0.85,0.55,0.85), P(0.4,0.5,0.92,0.5), P(0.75,0.32,0.92,0.5,0.75,0.68)],
  hold: [poly(24, 0.36), P(0.3,0.5,0.7,0.5)],
};
for (const el of document.querySelectorAll('.ds-icon')) {
  const g = [...el.classList].find(c => c.startsWith('ds-icon--'))?.slice(9); if (!GLYPHS[g]) continue;
  const s = Math.min(el.clientWidth, el.clientHeight), col = getComputedStyle(el).getPropertyValue('--icon-color').trim();
  el.innerHTML = '<svg width="'+s+'" height="'+s+'">' + GLYPHS[g].map(l => '<polyline fill="none" stroke="'+col+'" stroke-width="3" stroke-linejoin="round" points="'+l.map(p => (p[0]*s)+','+(p[1]*s)).join(' ')+'"/>').join('') + '</svg>';
}
for (const el of document.querySelectorAll('.ds-gauge')) {
  const w = el.clientWidth, r = w / 2 - 8, c = w / 2, f = 0.23, a0 = Math.PI * 0.75, a1 = a0 + Math.PI * 1.5 * f, aEnd = a0 + Math.PI * 1.5;
  const arc = (from, to) => { const x0 = c + r * Math.cos(from), y0 = c + r * Math.sin(from), x1 = c + r * Math.cos(to), y1 = c + r * Math.sin(to); return 'M'+x0+','+y0+' A'+r+','+r+' 0 '+((to-from)>Math.PI?1:0)+' 1 '+x1+','+y1; };
  el.insertAdjacentHTML('afterbegin', '<svg style="position:absolute;left:0;top:0" width="'+w+'" height="'+w+'"><path d="'+arc(a0,aEnd)+'" stroke="'+v('--c-line-mid')+'" stroke-width="8" fill="none"/><path d="'+arc(a0,a1)+'" stroke="'+v('--c-phosphor')+'" stroke-width="8" fill="none"/></svg>');
}
</script></body></html>`;

fs.mkdirSync(path.dirname(out), { recursive: true });
fs.writeFileSync(out.replace(/\.png$/, '.html'), html);
const browser = await playwright.chromium.launch({ executablePath: fs.existsSync('/opt/pw-browsers/chromium') ? undefined : undefined });
const page = await browser.newPage({ viewport: { width, height }, deviceScaleFactor: scale });
await page.setContent(html, { waitUntil: 'load' });
await page.waitForTimeout(150);
await page.screenshot({ path: out });
await browser.close();
console.log('wrote ' + path.relative(ROOT, out));
