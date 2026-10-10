// App shell: top bar, alert strip, screen router, bottom navigation and the frame loop.
// Screens rebuild only on structural change; numbers and timers are patched in place every frame.
import {
  RESOURCES, RESOURCE_KEYS, FACTIONS, ITEMS, ITEM_BY_ID, RANKS, BUILDINGS, SECTORS,
} from '../data.js';
import * as E from '../engine.js';
import { num, whole, rate, pct, esc } from '../format.js';
import { icon, mountIcons } from './icons.js';
import { clock, chanceClass, factorTag } from './common.js';
import { NAV, locate, domainReq, domainReady, sortedTabs, DOMAINS } from './layout.js';
import { renderDomain, bindDomain, updateDomain, buyCount } from './screens/domain.js';
import { renderCommand, bindCommand, updateCommand } from './screens/command.js';
import { renderMapScreen, bindMapScreen, updateMapScreen, defaultSector, liveSector, centerOn } from './screens/map.js';
import { createModals } from './modals.js';
import { createTips } from './tooltip.js';
import { sfx } from './sfx.js';
import {
  floatText, flash, burst, shake, vibrate, screenFlash,
} from './fx.js';
import { put, setAttr, setCls, setData, setW } from './dom.js';
import { watchVisible, measureVisible } from './onscreen.js';
import { stepAmbientLoops } from './framerate.js';
import { setFocus } from './focus.js';
import { createDebug } from './debug/panel.js';
import { createTutor } from './teach.js';

export function createUI(root, game) {
  stepAmbientLoops();
  mountIcons();
  const ui = {
    screen: 'command', inner: {}, buyMode: 1, sector: null, sheet: false,
    key: '', refs: null, logSeq: -1, raidKey: null, imminent: false, unseen: new Set(), coach: null,
  };

  root.innerHTML = `
    <div class="app">
      <header class="topbar">
        <div class="tb-row">
          <button class="core-badge" data-act="core" aria-label="AI Core">
            <svg viewBox="0 0 48 48" aria-hidden="true"><polygon class="cb-hex" points="24,2 43,13 43,35 24,46 5,35 5,13"/></svg>
            <svg class="cb-spin" viewBox="0 0 48 48" aria-hidden="true"><polygon class="cb-in" points="24,8 38,16 38,32 24,40 10,32 10,16"/></svg>
            <b data-corelvl></b>
          </button>
          <div class="ident"><b data-name></b><span data-rank></span></div>
          <button class="threat-chip" data-tip="threat">${icon('threat')}<b data-threat></b></button>
          <button class="icon-btn" data-act="menu" aria-label="Settings">${icon('settings')}</button>
        </div>
        <div class="tb-res">
          ${RESOURCE_KEYS.map((r) => `
            <div class="pill res-${r}" data-tip="res:${r}" data-pill="${r}">
              <span class="pill-ico">${icon(r)}</span>
              <div class="pill-body"><b data-v></b><div class="pill-bar"><i data-fill></i></div><small data-rate></small></div>
            </div>`).join('')}
        </div>
      </header>
      <div class="floats" data-floats></div>
      <main class="view" id="view"></main>
      <nav class="bottom-nav">
        ${NAV.map((n) => `<button class="nav-btn ${n.main ? 'main' : ''}" data-nav="${n.id}">
          <span class="nav-ico">${icon(n.icon)}<span class="nav-lock">${icon('lock')}</span></span><span class="nav-lbl">${n.name}</span><i class="nav-badge" hidden></i></button>`).join('')}
      </nav>
      <div class="vignette"></div>
      <div class="toasts" aria-live="polite"></div>
      <dialog id="modal"></dialog>
    </div>
    <div class="rotate-note">${icon('vibrate')}<p>DEADSWITCH is played in portrait.<br>Rotate your device.</p></div>`;

  const $ = (q) => root.querySelector(q);
  const view = $('#view');
  const app = $('.app');
  const modals = createModals($('#modal'), game, () => render());
  // The developer panel reads the focus and may change anything; a run of it rebuilds the screen.
  const debug = createDebug(game, () => { ui.key = ''; render(); });
  game.dev.panel = debug;
  const tips = createTips(game);
  const tutor = createTutor(root, game, () => render());
  const pills = Object.fromEntries(RESOURCE_KEYS.map((r) => {
    const el = root.querySelector(`[data-pill="${r}"]`);
    return [r, { el, v: el.querySelector('[data-v]'), fill: el.querySelector('[data-fill]'), rate: el.querySelector('[data-rate]') }];
  }));
  // Event dialogs open below the top bar; keep its height in a CSS variable.
  const topbar = root.querySelector('.topbar');
  const syncTop = () => document.documentElement.style.setProperty('--topbar', topbar.offsetHeight + 'px');
  new ResizeObserver(syncTop).observe(topbar);
  syncTop();
  const navBtns = Object.fromEntries([...root.querySelectorAll('[data-nav]')].map((b) => [b.dataset.nav, b]));

  // ---------- navigation ----------

  function go(screen, inner, focus) {
    ui.screen = screen;
    if (inner) {
      ui.inner[screen] = inner;
    }
    // Opening the map while something is under way opens on it.
    if (screen === 'map' && !focus) {
      ui.sector = liveSector(game.state) || ui.sector;
    }
    setFocus('screen', inner ? `${screen}:${inner}` : screen);
    ui.sheet = false;
    ui.coach = focus || null;
    tips.hide();
    render();
    if (!focus) {
      view.scrollTo(0, 0);
    }
  }

  function directiveTarget(s) {
    const d = E.currentDirective(s);
    if (!d) return null;
    const c = d.cond;
    if (c.level) {
      const l = locate('building', c.level);
      return [l.domain, l.tab, `[data-card="${c.level}"]`];
    }
    if (c.item) {
      const item = ITEM_BY_ID[c.item];
      const l = locate('shop', item.tab);
      return [l.domain, l.tab, `[data-item="${c.item}"], .card[data-card]`];
    }
    if (c.factor) {
      const tab = { power: ['military', 'offense'], defense: ['military', 'defense'], experts: ['research', 'experts'] }[c.factor];
      return [tab[0], tab[1], '.card.item.affordable, .card.item, .card[data-card]'];
    }
    if (c.sector) {
      ui.sector = c.sector;
      return ['map', 'theater', null, true];
    }
    if (c.raidsWon) return ['military', 'defense', '.card.item, .card[data-card]'];
    return ['command', null, '.reactor'];
  }

  root.addEventListener('click', (e) => {
    // Whatever was tapped is now the thing the debugger inspects, button or not.
    const card = e.target.closest('[data-card], [data-item], [data-clan]');
    if (card && !card.closest('dialog')) {
      if (card.dataset.card) setFocus('building', card.dataset.card);
      else if (card.dataset.item) setFocus('item', card.dataset.item);
      else setFocus('clan', card.dataset.clan);
    }
    const t = e.target.closest('[data-act], [data-nav], [data-go], [data-inner], [data-mode], [data-sector], [data-req]');
    if (!t || t.disabled || t.closest('dialog')) {
      return;
    }
    const s = game.state;
    // A locked bottom-nav item does nothing.
    if (t.dataset.nav && t.classList.contains('locked')) {
      return;
    }
    // Locked things lead to what unlocks them.
    if (t.dataset.req) {
      const l = locate('building', t.dataset.req);
      sfx.click();
      go(l.domain, l.tab, `[data-card="${t.dataset.req}"]`);
      return;
    }
    if (t.dataset.nav) {
      sfx.click();
      go(t.dataset.nav);
      return;
    }
    if (t.dataset.go) {
      const [screen, inner] = t.dataset.go.split(':');
      sfx.click();
      go(screen, inner);
      return;
    }
    if (t.dataset.inner) {
      ui.inner[ui.screen] = t.dataset.inner;
      ui.sheet = false;
      setFocus('screen', `${ui.screen}:${t.dataset.inner}`);
      sfx.click();
    } else if (t.dataset.mode) {
      ui.buyMode = t.dataset.mode === 'max' ? 'max' : Number(t.dataset.mode);
    } else if (t.dataset.sector) {
      ui.sector = t.dataset.sector;
      ui.sheet = true;
      setFocus('sector', t.dataset.sector);
      game.act.inspect(t.dataset.sector);
      sfx.click();
    } else {
      act(t, s);
    }
    render();
  });
  // Horizontal swipe on the screen body moves between inner tabs. Strict filters avoid false
  // triggers: one finger, mostly horizontal, long enough, quick, not from the screen edge (system
  // back gesture), and not inside anything that scrolls sideways or is interactive by drag.
  const SWIPE = { minDx: 70, ratio: 2.2, maxMs: 650, edge: 24 };
  let touch = null;
  view.addEventListener('touchstart', (e) => {
    const t = e.touches[0];
    const blocked = e.touches.length !== 1 || !DOMAINS[ui.screen] || ui.sheet
      || t.clientX < SWIPE.edge || t.clientX > window.innerWidth - SWIPE.edge
      || e.target.closest('.segs, .map-card, input, textarea, .sheet');
    touch = blocked ? null : { x: t.clientX, y: t.clientY, at: performance.now(), scroll: view.scrollTop };
  }, { passive: true });
  view.addEventListener('touchmove', (e) => {
    if (touch && e.touches.length !== 1) touch = null;
  }, { passive: true });
  view.addEventListener('touchend', (e) => {
    const start = touch;
    touch = null;
    if (!start) return;
    const t = e.changedTouches[0];
    const dx = t.clientX - start.x;
    const dy = t.clientY - start.y;
    const quick = performance.now() - start.at < SWIPE.maxMs;
    const still = Math.abs(view.scrollTop - start.scroll) < 8;
    if (!quick || !still || Math.abs(dx) < SWIPE.minDx || Math.abs(dx) < Math.abs(dy) * SWIPE.ratio) return;
    const tabs = sortedTabs(game.state, ui.screen).filter((x) => x.st !== 'locked').map((x) => x.t.id);
    const i = tabs.indexOf(ui.inner[ui.screen]);
    const next = tabs[i + (dx < 0 ? 1 : -1)];
    if (i < 0 || !next) {
      const el = view.firstElementChild;
      if (el) {
        el.classList.remove('bump-l', 'bump-r');
        void el.offsetWidth;
        el.classList.add(dx < 0 ? 'bump-l' : 'bump-r');
      }
      return;
    }
    ui.inner[ui.screen] = next;
    ui.swipeDir = dx < 0 ? 'from-r' : 'from-l';
    sfx.click();
    render();
  }, { passive: true });

  root.addEventListener('keydown', (e) => {
    if ((e.key === 'Enter' || e.key === ' ') && e.target.dataset && (e.target.dataset.sector || e.target.dataset.req)) {
      e.preventDefault();
      e.target.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    }
  });

  function act(t, s) {
    const id = t.dataset.id;
    switch (t.dataset.act) {
      case 'build':
        if (game.act.build(id)) {
          sfx.click();
          vibrate(15);
          flash(t, 'pop');
        }
        break;
      case 'fl-open':
        if (ui.floats[t.dataset.key]) ui.floats[t.dataset.key] = { since: performance.now(), collapsed: false };
        if (t.dataset.key.startsWith('atk:')) setFocus('attack', 'next');
        break;
      case 'fl-close':
        if (ui.floats[t.dataset.key]) ui.floats[t.dataset.key].collapsed = true;
        break;
      case 'pause':
        game.act.toggle(id);
        sfx.click();
        vibrate(12);
        break;
      case 'buy': {
        const item = ITEMS.find((i) => i.id === id);
        const n = buyCount(s, ui, item);
        const before = E.factors(s);
        if (game.act.buy(id, n)) {
          sfx.buy();
          vibrate(10);
          const after = E.factors(s);
          const gained = Object.keys(after).filter((k) => after[k] > before[k]).map((k) => factorTag(k, after[k] - before[k]));
          floatText(t, gained.join(' ') || `+${n}`);
          flash(t, 'pop');
          burst(t, 'var(--hud-2)', 12);
        }
        break;
      }
      case 'launch':
        if (game.act.launch(id)) {
          sfx.launch();
          vibrate([30, 40, 60]);
          ui.sheet = false;
        }
        break;
      case 'close-sheet':
        ui.sheet = false;
        break;
      case 'event':
        modals.showEvent(Number(t.dataset.uid) || null);
        break;
      case 'directive': {
        const tg = directiveTarget(s);
        if (tg) {
          sfx.click();
          go(tg[0], tg[1], tg[2]);
          if (tg[3]) {
            ui.sheet = true;
          }
        }
        break;
      }
      case 'storage':
        go('economy', 'storage', `[data-card="${t.dataset.target}"]`);
        break;
      case 'core':
        go('economy', 'production', '[data-card="core"]');
        break;
      case 'archive':
        ui.logDirty = true; // the archive carries its own copy of the log
        modals.showArchive();
        break;
      case 'menu':
        modals.openMenu();
        break;
      default:
    }
  }

  // ---------- structure ----------

  function structureKey(s) {
    const base = [ui.screen, ui.inner[ui.screen]];
    if (ui.screen === 'command') return JSON.stringify(base);
    if (ui.screen === 'map') {
      const nodes = Object.entries(s.nodes).map(([id, n]) => `${id}${n.m.toFixed(2)}${n.marks}`).join();
      const postures = Object.values(s.clans).map((c) => c.stances.join('')).join('|');
      const beams = E.attacks(s).map((a) => (E.attackPlace(s, a) || []).join()).join('|');
      return JSON.stringify([...base, ui.sector, ui.sheet, s.sectors, s.op && s.op.sector, beams, nodes, postures, s.chapter, s.ending, E.level(s, 'core')]);
    }
    return JSON.stringify([...base, ui.buyMode, s.levels, s.items, s.paused, s.build && s.build.id, Math.round(s.align)]);
  }

  function render() {
    const s = game.state;
    if (!ui.sector || !E.sectorById(ui.sector)) {
      ui.sector = defaultSector(s);
    }
    for (const [id, b] of Object.entries(navBtns)) {
      setAttr(b, 'aria-current', id === ui.screen);
    }
    setData(app, 'screen', ui.screen);
    const key = structureKey(s);
    if (key !== ui.key) {
      ui.key = key;
      const scroll = view.scrollTop;
      const oldSegs = view.querySelector('.segs');
      const segsScroll = oldSegs && ui.lastScreen === ui.screen ? oldSegs.scrollLeft : 0;
      if (ui.screen === 'command') {
        view.innerHTML = renderCommand();
        ui.refs = bindCommand(view);
        watchVisible(Object.values(ui.refs.panels), view);
      } else if (ui.screen === 'map') {
        view.innerHTML = renderMapScreen(s, ui);
        ui.refs = bindMapScreen(view);
        watchVisible([], view);
      } else {
        view.innerHTML = renderDomain(s, ui, ui.screen);
        ui.refs = bindDomain(view);
        watchVisible([...ui.refs.cards, ...ui.refs.rows].map((x) => x.el), view);
      }
      const entered = ui.lastScreen !== ui.screen || ui.lastInner !== ui.inner[ui.screen];
      const screenEl = view.firstElementChild;
      if (entered && screenEl) {
        screenEl.classList.add('enter');
        if (ui.swipeDir) {
          screenEl.classList.add(ui.swipeDir);
        }
      }
      ui.lastInner = ui.inner[ui.screen];
      ui.swipeDir = null;
      view.scrollTop = scroll;
      centerTab(segsScroll, entered);
      ui.logDirty = true;
      if (ui.screen === 'map' && (ui.lastScreen !== 'map' || ui.sheet)) {
        focusSector();
      }
      ui.lastScreen = ui.screen;
      if (ui.coach) {
        coach(ui.coach);
        ui.coach = null;
      }
    }
    update();
  }

  // Keeps the selected sector in view: centred on arrival, above the briefing sheet when it opens.
  function focusSector() {
    if (view.querySelector('[data-mapview]')) {
      centerOn(view, ui.sector);
      return;
    }
    const node = view.querySelector('.node.sel');
    if (!node) return;
    const v = view.getBoundingClientRect();
    const n = node.getBoundingClientRect();
    const sheet = view.querySelector('.sheet.open');
    const room = sheet ? v.height - sheet.getBoundingClientRect().height + 40 : v.height;
    view.style.scrollBehavior = 'auto';
    view.scrollTop += n.top + n.height / 2 - v.top - room / 2;
    view.style.scrollBehavior = '';
  }

  // Keeps the selected inner tab centred in its scrollable bar, gliding from where the bar was.
  function centerTab(from, animate) {
    const bar = view.querySelector('.segs');
    const tab = bar && bar.querySelector('[aria-selected="true"]');
    if (!tab || bar.scrollWidth <= bar.clientWidth) {
      return;
    }
    bar.scrollLeft = from;
    const left = tab.offsetLeft + tab.offsetWidth / 2 - bar.clientWidth / 2;
    requestAnimationFrame(() => bar.scrollTo({ left, behavior: animate ? 'smooth' : 'auto' }));
  }

  // Points at the element a directive needs: scroll to it and pulse it.
  function coach(selector) {
    const el = view.querySelector(selector);
    if (!el) return;
    el.scrollIntoView({ block: 'center', behavior: 'smooth' });
    el.classList.add('coach');
    setTimeout(() => el.classList.remove('coach'), 3600);
  }

  // ---------- per frame ----------

  // Scrolling between frames refreshes anything it brings into view right away.
  let scrollQueued = false;
  view.addEventListener('scroll', () => {
    if (scrollQueued) return;
    scrollQueued = true;
    requestAnimationFrame(() => {
      scrollQueued = false;
      if (measureVisible(view)) update();
    });
  }, { passive: true });

  addEventListener('resize', () => measureVisible(view), { passive: true });

  function update() {
    const s = game.state;
    updateTop(s);
    updateAlerts(s);
    updateNav(s);
    // Behind an open dialog the screen is covered, so it waits and its loops pause; the top bar stays
    // live (and keeps animating only when an order leaves it uncovered).
    const covered = modals.isOpen();
    setData(app, 'covered', covered ? (modals.coversTop() ? 'all' : 'view') : '');
    if (!covered) {
      if (ui.screen === 'command') {
        updateCommand(s, ui, ui.refs);
      } else if (ui.screen === 'map') {
        updateMapScreen(s, ui, ui.refs);
      } else {
        updateDomain(s, ui, ui.refs, game.flows);
      }
    }
    updateLog(s);
    drainFx(s);
    tutor.update(covered);
    debug.update();
    if (!modals.pump() && !modals.isOpen() && !s.inbox.length && !tutor.active()) {
      const next = s.events.find((x) => ui.unseen.has(x.uid));
      if (next) {
        ui.unseen.delete(next.uid);
        modals.showEvent(next.uid);
      }
    }
  }

  function updateTop(s) {
    const c = E.caps(s);
    const flows = game.flows;
    for (const r of RESOURCE_KEYS) {
      const p = pills[r];
      put(p.v, whole(s.res[r]));
      const capped = Number.isFinite(c[r]);
      const full = capped && s.res[r] >= c[r] * 0.999;
      const net = flows ? flows.prod[r] - flows.cons[r] : 0;
      setW(p.fill, capped ? pct(s.res[r] / c[r]) : '100%');
      put(p.rate, full ? 'FULL' : rate(net));
      p.el.classList.toggle('full', full);
      p.el.classList.toggle('neg', net < -0.005);
    }
    const t = E.threat(s);
    put(root.querySelector('[data-threat]'), num(t));
    put(root.querySelector('[data-rank]'), RANKS[E.rankIndex(t)].title);
    put(root.querySelector('[data-corelvl]'), E.level(s, 'core'));
    put(root.querySelector('[data-name]'), s.name || 'Commander');
  }

  // Floating alerts: open full width when new, fold into a blinking badge after FLOAT_OPEN_MS or
  // when dismissed; tapping the badge opens it again. Attacks we are likely to hold are not shown.
  const FLOAT_OPEN_MS = 30000;
  const SAFE_HOLD = 0.8;
  ui.floats = {};

  function floatState(key) {
    if (!ui.floats[key]) {
      ui.floats[key] = { since: performance.now(), collapsed: false };
    }
    const f = ui.floats[key];
    if (!f.collapsed && performance.now() - f.since > FLOAT_OPEN_MS) {
      f.collapsed = true;
    }
    return f;
  }

  function updateAlerts(s) {
    const box = root.querySelector('[data-floats]');
    const items = [];
    // Command already shows both of these in full, so the floats would only cover the reactor.
    const quiet = ui.screen === 'command';
    const threats = quiet ? [] : E.attacks(s).filter((a) => E.raidChance(s, a) < SAFE_HOLD);
    const atk = threats[0] || null;
    if (atk) {
      const p = E.raidChance(s, atk);
      const more = threats.length - 1;
      items.push({
        key: 'atk:' + threats.map((a) => a.faction + a.strength).join(','), level: p < 0.5 ? 'red' : 'amber', ico: 'alert',
        body: `<button class="fl-body" data-go="command"><span>${E.attackName(atk)}${more ? ` +${more}` : ''}</span><b data-t></b><em data-h></em></button>`,
        time: clock(atk.remaining), hold: `hold ${pct(p)}`, holdCls: 'chance-' + chanceClass(p),
      });
    }
    if (s.events.length && !quiet) {
      const first = s.events.reduce((a, x) => (x.left < a.left ? x : a));
      const crisis = s.events.some((x) => E.eventById(x.id).aftermath);
      items.push({
        key: 'ev:' + s.events.map((x) => x.uid).join(','), level: crisis ? 'red' : 'amber', ico: crisis ? 'fire' : 'message',
        body: `<button class="fl-body" data-act="event" data-uid="${first.uid}"><span>${s.events.length} order${s.events.length > 1 ? 's' : ''}</span><b data-t></b></button>`,
        time: clock(first.left),
      });
    }
    for (const k of Object.keys(ui.floats)) {
      if (!items.some((x) => x.key === k)) delete ui.floats[k];
    }
    // Rebuild only when the set of alerts changes; everything else is patched in place so taps land.
    const structure = items.map((x) => x.key + x.level).join('|');
    if (box.dataset.k !== structure) {
      box.dataset.k = structure;
      box.innerHTML = items.map((x) => `<div class="fl fl-${x.level}" data-fl="${x.key}">
        <button class="fl-ico" data-act="fl-open" data-key="${x.key}" aria-label="Show alert">${icon(x.ico)}</button>
        ${x.body}<button class="fl-x" data-act="fl-close" data-key="${x.key}" aria-label="Minimise">${icon('close')}</button></div>`).join('');
    }
    for (const x of items) {
      const el = box.querySelector(`[data-fl="${x.key}"]`);
      el.classList.toggle('collapsed', floatState(x.key).collapsed);
      put(el.querySelector('[data-t]'), x.time);
      const h = el.querySelector('[data-h]');
      if (h) {
        put(h, x.hold);
        setCls(h, x.holdCls);
      }
    }
    const imminent = !!atk && atk.remaining < 60;
    app.classList.toggle('imminent', imminent);
    if (imminent && !ui.imminent && atk.remaining < 30) {
      ui.imminent = true;
      sfx.alarm();
      vibrate([200, 100, 200]);
    }
    if (!atk) {
      ui.imminent = false;
    }
    const key = E.attacks(s).map((a) => a.faction + a.strength).join(',');
    const newest = E.attacks(s).find((a) => !(ui.raidKey || '').split(',').includes(a.faction + a.strength));
    if (newest && ui.raidKey !== null) {
      sfx.alarm();
      vibrate([150, 80, 150, 80, 300]);
      shake();
      screenFlash('alert');
      toast(`${icon('alert')}<b>${E.attackName(newest)} inbound</b> Arrives in ${clock(newest.remaining)}`, 'bad');
    }
    ui.raidKey = key;
  }

  function badge(id, text, cls) {
    const b = navBtns[id].querySelector('.nav-badge');
    b.hidden = !text;
    put(b, text === true ? '' : text || '');
    setCls(b, 'nav-badge ' + (cls || ''));
  }

  function updateNav(s) {
    for (const [id, b] of Object.entries(navBtns)) {
      const req = domainReq(s, id);
      b.classList.toggle('locked', !!req);
      setAttr(b, 'aria-disabled', !!req);
      if (req) {
        b.dataset.req = req;
      } else {
        delete b.dataset.req;
      }
    }
    const atk = E.nextAttack(s);
    const danger = atk && E.raidChance(s, atk) < 0.8;
    badge('command', s.events.length || (danger ? '!' : ''), 'red');
    badge('military', danger ? '!' : '', 'red');
    // Dot = something here can be built or upgraded now; the same dot marks the inner tab and the card.
    badge('economy', domainReady(s, 'economy') ? true : '', 'dot');
    if (!danger) {
      badge('military', domainReady(s, 'military') ? true : '', 'dot');
    }
    const target = !s.op && SECTORS.some((x) => E.sectorStatus(s, x) === 'target' && E.opChance(s, x) >= 0.8 && E.canLaunch(s, x));
    badge('map', target ? true : '', 'dot');
    badge('research', domainReady(s, 'research') ? true : '', 'dot');
  }

  function updateLog(s) {
    if (ui.logSeq === s.lineSeq && !ui.logDirty) {
      return;
    }
    ui.logDirty = false;
    const n = s.lineSeq - ui.logSeq;
    const fresh = ui.logSeq < 0 || n <= 0 ? [] : s.log.slice(-n);
    ui.logSeq = s.lineSeq;
    const logs = root.querySelectorAll('.log');
    if (logs.length) {
      const html = s.log.slice().reverse().map((l) => `<li class="tone-${l.tone}"><span>${esc(l.text)}</span></li>`).join('');
      logs.forEach((el) => { el.innerHTML = html; });
    }
    for (const l of fresh) {
      if (['unlock', 'item', 'rank', 'core'].includes(l.tone)) {
        toast(`${icon(l.tone === 'rank' ? 'threat' : l.tone === 'core' ? 'core' : 'up')}${esc(l.text)}`, l.tone);
      }
    }
  }

  function drainFx(s) {
    for (const fx of s.fx.splice(0)) {
      if (fx.kind === 'built') {
        sfx.build();
        vibrate(25);
        const card = view.querySelector(`[data-card="${fx.id}"]`);
        flash(card);
        burst(card, 'var(--hud)');
      } else if (fx.kind === 'damaged') {
        sfx.fall();
        shake();
        toast(`${icon('fire')}<b>${esc(BUILDINGS.find((b) => b.id === fx.id).name)}</b> lost a level`, 'bad');
      } else if (fx.kind === 'event') {
        ui.unseen.add(fx.uid);
      } else if (fx.kind === 'unlocked') {
        sfx.unlock();
      } else if (fx.kind === 'directive') {
        sfx.win();
        vibrate(30);
        toast(`${icon('check')}<b>Directive complete</b> ${esc(fx.text)}`, 'good');
      }
    }
  }

  function toast(html, tone = 'info') {
    const box = root.querySelector('.toasts');
    const el = document.createElement('div');
    setCls(el, 'toast tone-' + tone);
    el.innerHTML = html;
    box.appendChild(el);
    while (box.children.length > 3) {
      box.firstChild.remove();
    }
    setTimeout(() => el.classList.add('out'), 3600);
    setTimeout(() => el.remove(), 4200);
  }

  return {
    render,
    go: (screen) => go(screen),
    crash: (detail) => modals.showCrash(detail),
    showOffline: (report) => modals.showOffline(report),
    queuePendingEvents: () => game.state.events.forEach((x) => ui.unseen.add(x.uid)),
    /** True while a cut scene or a report is holding the screen, so the host can keep it awake. */
    storyOpen: () => modals.coversTop(),
    reset: () => { ui.key = ''; ui.sector = null; ui.sheet = false; ui.logSeq = -1; ui.raidKey = null; ui.unseen.clear(); },
  };
}
