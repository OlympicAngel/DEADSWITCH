#!/usr/bin/env node
// Headless UI preview: renders Unity UI Toolkit layouts (UXML + USS) to PNG with Chromium.
// Mirrors Unity's layout defaults (Yoga flex, column, border-box) and the runtime behaviors in
// unity/Assets/Game/Runtime/UI (corner brackets, segmented meters, sparklines, gauges) closely enough
// to review hierarchy, spacing, typography and color without the Editor. See docs/agents/quality-bar.md.
//
// usage: node tools/uipreview/preview.mjs <screen.uxml> [--out file.png] [--bg image.png] [--height 2340] [--scale 0.5]
//        [--img element-name=image.png] (a runtime texture, e.g. the sector map render; prints the element's size)
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
// 1920 = 16:9, the shortest common phone (and Unity's default portrait Game view): overlaps show up here first.
const width = 1080, height = parseInt(opt('height', '1920'), 10), scale = parseFloat(opt('scale', '0.5'));
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
for (const el of document.querySelectorAll('#preview-root, #preview-root *')) { const kids = [...el.children].filter(c => !c.classList.contains('ds-corner')); if (kids.length) { kids[0].classList.add('is-first'); kids[kids.length - 1].classList.add('is-last'); } }
// Pager.cs mirror: tab strip from page titles, first page (or data-page) shown; folds collapse to the header
for (const pg of document.querySelectorAll('.ds-pager')) {
  const pages = [...pg.children].filter(c => c.classList.contains('ds-page'));
  const show = Math.max(0, pages.findIndex(p => p.classList.contains('is-preview')));
  const tabs = document.createElement('div'); tabs.className = 'ui-ve ds-pager__tabs';
  pages.forEach((p, i) => {
    const t = document.createElement('div'); t.className = 'ui-ve ds-pager__tab' + (i === show ? ' is-active' : '');
    const g = p.querySelector('.ds-page__glyph'); if (g) { const ic = document.createElement('div'); ic.className = 'ui-ve ds-icon ds-icon--' + g.textContent.trim() + ' ds-pager__icon'; t.appendChild(ic); }
    const l = document.createElement('div'); l.className = 'ui-label unity-label ds-pager__label'; l.textContent = (p.querySelector('.ds-page__title') || {}).textContent || ('PAGE ' + (i + 1)); t.appendChild(l);
    tabs.appendChild(t); if (i !== show) p.style.display = 'none';
  });
  pg.insertBefore(tabs, pg.firstChild);
  requestAnimationFrame(() => {}); const act = tabs.children[show]; if (act) { const r = document.createElement('div'); r.className = 'ui-ve ds-pager__rail'; r.style.left = act.offsetLeft + 'px'; r.style.width = act.offsetWidth + 'px'; tabs.appendChild(r); }
}
for (const c of document.querySelectorAll('.ds-card--fold')) {
  const head = c.querySelector('.ds-card__head'); if (!head) continue;
  head.classList.add('ds-fold__head'); const ch = document.createElement('div'); ch.className = 'ui-ve ds-icon ds-icon--chevron ds-fold__chev'; ch.style.rotate = c.classList.contains('is-folded') ? '0deg' : '90deg'; head.appendChild(ch);
  if (c.classList.contains('is-folded')) for (const k of c.children) if (k !== head && !k.classList.contains('ds-corner')) k.style.display = 'none';
}
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
// Icons.cs mirror: the same glyph file (Resources/UI/Icons.json)
const GLYPH_SRC = ${JSON.stringify(JSON.parse(fs.readFileSync(path.join(UI, 'Icons.json'), 'utf8')).glyphs)};
const arcPts = (r, cx, cy, d0, d1, n) => Array.from({ length: n + 1 }, (_, i) => { const a = (d0 + (d1 - d0) * i / n) * Math.PI / 180; return [cx + Math.cos(a) * r, cy + Math.sin(a) * r]; });
const parseLine = src => { const t = src.trim().split(' ').filter(Boolean); if (t[0] === 'o') return arcPts(+t[2], +t[3], +t[4], 0, 360, +t[1]); if (t[0] === 'a') return arcPts(+t[1], +t[2], +t[3], +t[4], +t[5], +t[6]);
  const pts = []; for (let i = 0; i + 1 < t.length; i += 2) pts.push([+t[i], +t[i + 1]]); return pts; };
const GLYPHS = Object.fromEntries(GLYPH_SRC.map(g => [g.name, g.lines.map(parseLine)]));
for (const el of document.querySelectorAll('.ds-icon')) {
  const g = [...el.classList].find(c => c.startsWith('ds-icon--'))?.slice(9); if (!GLYPHS[g]) continue;
  const s = Math.min(el.clientWidth, el.clientHeight), col = getComputedStyle(el).getPropertyValue('--icon-color').trim();
  el.innerHTML = '<svg width="'+s+'" height="'+s+'">' + GLYPHS[g].map(l => '<polyline fill="none" stroke="'+col+'" stroke-width="'+Math.min(5,Math.max(2,s*0.07))+'" stroke-linejoin="round" points="'+l.map(p => (p[0]*s)+','+(p[1]*s)).join(' ')+'"/>').join('') + '</svg>';
}
// Sheen.cs mirror: top light on surfaces, edge darkening on scrims
for (const sel of ['.ds-card', '.ds-tile', '.ds-btn', '.pod', '.hud-advisor', '.ds-sheet', '.ds-scrim']) for (const el of document.querySelectorAll(sel)) {
  const st = getComputedStyle(el); const col = st.getPropertyValue('--sheen-color').trim(); if (!col) continue;
  const reach = parseFloat(st.getPropertyValue('--sheen-reach')) || (sel === '.ds-scrim' ? 1 : 0.6);
  const up = el.classList.contains('ds-scrim--up');
  const g = up ? 'linear-gradient(to top, ' + col + ' 0%, transparent ' + (reach * 100) + '%)' : 'linear-gradient(to bottom, ' + col + ' 0%, transparent ' + (reach * 100) + '%)';
  el.style.backgroundImage = g;
}
// AiOrb.cs / AiWave mirror (a still frame)
for (const el of document.querySelectorAll('.ai-orb')) {
  const st = getComputedStyle(el), ring = st.getPropertyValue('--ring-color').trim() || v('--c-cyan'), core = st.getPropertyValue('--core-color').trim() || v('--c-cyan-glow');
  const w = Math.min(el.clientWidth, el.clientHeight), R = w / 2, c = R;
  const arc = (r, a0, a1, sw, col, op = 1) => { const x0 = c + r * Math.cos(a0), y0 = c + r * Math.sin(a0), x1 = c + r * Math.cos(a1), y1 = c + r * Math.sin(a1); return '<path d="M'+x0+','+y0+' A'+r+','+r+' 0 '+((a1-a0)>Math.PI?1:0)+' 1 '+x1+','+y1+'" stroke="'+col+'" stroke-opacity="'+op+'" stroke-width="'+sw+'" fill="none"/>'; };
  let svg = '<defs><radialGradient id="g'+w+'"><stop offset="0" stop-color="'+core+'"/><stop offset="1" stop-color="'+core+'" stop-opacity="0"/></radialGradient></defs>';
  svg += '<circle cx="'+c+'" cy="'+c+'" r="'+(R*0.62)+'" fill="url(#g'+w+')"/><circle cx="'+c+'" cy="'+c+'" r="'+(R*0.2)+'" fill="'+ring+'" opacity="0.9"/>';
  svg += '<circle cx="'+c+'" cy="'+c+'" r="'+(R*0.93)+'" stroke="'+ring+'" stroke-opacity="0.35" stroke-width="1.5" fill="none"/>';
  for (let i = 0; i < 36; i++) { const t = Math.PI * 2 * i / 36, ri = i % 9 === 0 ? 0.8 : 0.86; svg += '<line x1="'+(c+Math.cos(t)*R*ri)+'" y1="'+(c+Math.sin(t)*R*ri)+'" x2="'+(c+Math.cos(t)*R*0.91)+'" y2="'+(c+Math.sin(t)*R*0.91)+'" stroke="'+ring+'" stroke-opacity="0.35" stroke-width="'+(i%9===0?2.5:1.2)+'"/>'; }
  for (let i = 0; i < 3; i++) { const a = 0.4 + i * Math.PI * 2 / 3; svg += arc(R * 0.72, a, a + 1.25, 4, ring); }
  for (let i = 0; i < 12; i++) { const a = i * Math.PI * 2 / 12; svg += arc(R * 0.52, a, a + 0.32, 2, ring, 0.7); }
  el.insertAdjacentHTML('afterbegin', '<svg style="position:absolute;left:0;top:0" width="'+w+'" height="'+w+'">'+svg+'</svg>');
}
for (const el of document.querySelectorAll('.ai-wave')) {
  const col = getComputedStyle(el).getPropertyValue('--wave-color').trim() || v('--c-cyan'), w = el.clientWidth, h = el.clientHeight, n = 18, step = w / n;
  let svg = ''; for (let i = 0; i < n; i++) { const bh = Math.max(2, h * (0.25 + 0.6 * Math.abs(Math.sin(i * 1.7) * Math.sin(i * 0.6 + 1)))); svg += '<rect x="'+(i*step+1)+'" y="'+((h-bh)/2)+'" width="'+(step-3)+'" height="'+bh+'" fill="'+col+'"/>'; }
  el.insertAdjacentHTML('afterbegin', '<svg style="position:absolute;left:0;top:0" width="'+w+'" height="'+h+'">'+svg+'</svg>');
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
// --img name=file: stand-ins for runtime textures (MapScreen's 3D render); the element's size is printed for matching renders
for (let i = 0; i < args.length; i++) {
  if (args[i] !== '--img') continue;
  const [name, file] = args[i + 1].split('=');
  const data = fs.existsSync(file) ? fs.readFileSync(file).toString('base64') : null;
  const size = await page.evaluate(([n, d]) => { const el = document.getElementById(n); if (!el) return null; if (d) el.style.background = `url(data:image/png;base64,${d}) center/100% 100%`; return [el.clientWidth, el.clientHeight]; }, [name, data]);
  console.log(name + ' size ' + JSON.stringify(size));
}
await page.waitForTimeout(150);
// Unity shrinks flex children (flex-shrink: 1) instead of overflowing, so text that needs more height than it
// got overlaps its neighbours in the Editor. Report it; long screens belong in a ScrollView.
const squeezed = await page.evaluate(() => [...document.querySelectorAll('.ui-label')]
  .filter(e => e.offsetParent && e.textContent.trim() && e.scrollHeight > e.clientHeight + 3)
  .map(e => `${e.id || e.className.split(' ').slice(-1)[0]} '${e.textContent.trim().slice(0, 24)}' needs ${e.scrollHeight}px, has ${e.clientHeight}px`));
for (const s of squeezed) console.warn('squeezed: ' + s);
await page.screenshot({ path: out });
await browser.close();
console.log('wrote ' + path.relative(ROOT, out));
