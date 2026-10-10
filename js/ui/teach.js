// The tutor: me walking the commander through something for the first time. While a lesson runs,
// four panels cover the screen with a hole left over the one control the step is about, so the only
// thing that can be tapped is the thing being explained. The hole is cut with panels rather than a
// z-index because the target can live inside any scrolling box or stacking context on the screen.
import * as E from '../engine.js';
import { icon } from './icons.js';
import { esc } from '../format.js';
import { setStyle, put } from './dom.js';

const PAD = 8; // px of room around the target
const CARD_GAP = 14;

export function createTutor(root, game, onChange) {
  const el = document.createElement('div');
  el.className = 'teach';
  el.hidden = true;
  el.innerHTML = `
    <i class="teach-mask" data-m="t"></i><i class="teach-mask" data-m="b"></i>
    <i class="teach-mask" data-m="l"></i><i class="teach-mask" data-m="r"></i>
    <i class="teach-ring" data-ring hidden></i>
    <div class="teach-card" data-card role="dialog" aria-live="polite" aria-label="Tutorial">
      <span class="kicker">${icon('core')}<span data-step></span></span>
      <p data-say></p>
      <div class="teach-foot">
        <button class="btn link" data-teach="skip">Skip</button>
        <button class="btn primary small" data-teach="next" hidden>Continue</button>
      </div>
    </div>`;
  root.appendChild(el);
  const masks = Object.fromEntries([...el.querySelectorAll('[data-m]')].map((x) => [x.dataset.m, x]));
  const ring = el.querySelector('[data-ring]');
  const card = el.querySelector('[data-card]');
  const sayEl = el.querySelector('[data-say]');
  const stepEl = el.querySelector('[data-step]');
  const nextBtn = el.querySelector('[data-teach="next"]');

  let lesson = null;
  let at = 0;
  let tapped = false;
  let shown = ''; // what the card is currently saying, so it is only written when it changes

  const step = () => (lesson ? lesson.steps[at] : null);

  function start(l) {
    lesson = l;
    at = 0;
    tapped = false;
    shown = '';
  }

  function finish() {
    if (lesson) {
      E.markTaught(game.state, lesson.id);
      game.act.save();
    }
    lesson = null;
    el.hidden = true;
    onChange();
  }

  function advance() {
    if (!lesson) return;
    tapped = false;
    at++;
    if (at >= lesson.steps.length) {
      finish();
    }
  }

  // A tap on the hole is the answer to a `tap` step. Nothing else can be tapped, so nothing else counts.
  root.addEventListener('click', (e) => {
    const s = step();
    if (s && s.at && e.target.closest(s.at)) {
      tapped = true;
    }
  }, true);

  el.addEventListener('click', (e) => {
    const b = e.target.closest('[data-teach]');
    if (!b || !lesson) return;
    if (b.dataset.teach === 'skip') finish();
    else advance();
    onChange();
  });

  /** The nearest ancestor that actually scrolls, which is what has to move. */
  function scroller(from) {
    for (let p = from.parentElement; p; p = p.parentElement) {
      if (p.scrollHeight > p.clientHeight + 2 && /auto|scroll/.test(getComputedStyle(p).overflowY)) {
        return p;
      }
    }
    return null;
  }

  /** Puts the target in the band the tutor leaves free: inside the scroller, above the bottom nav,
   *  with room under it for the card. Run every frame, because the screen is still laying out
   *  around it and a target scrolled once drifts; once it fits, this does nothing. */
  function centre(target) {
    const box = scroller(target);
    const r = target.getBoundingClientRect();
    if (!box || (!r.width && !r.height)) {
      return;
    }
    const br = box.getBoundingClientRect();
    const nav = root.querySelector('.bottom-nav');
    const top = Math.max(br.top, 0) + PAD;
    const bottom = Math.min(br.bottom, innerHeight - (nav ? nav.offsetHeight : 0)) - cardRoom();
    const room = bottom - top;
    // Centred in the band, or pinned to the top of it when it is taller than the band.
    const want = room > r.height ? top + (room - r.height) / 2 : top;
    const delta = r.top - want;
    if (Math.abs(delta) > 4) {
      box.scrollTop += delta;
    }
  }

  /** How much height under the target the card needs. */
  const cardRoom = () => (card.offsetHeight || 150) + CARD_GAP * 2;

  /** Puts the four panels and the ring around the target, and the card in whatever room is left. */
  function frame(target) {
    const vw = innerWidth;
    const vh = innerHeight;
    const r = target ? target.getBoundingClientRect() : null;
    const box = r && r.width && r.height
      ? { x: Math.max(0, r.left - PAD), y: Math.max(0, r.top - PAD), w: r.width + PAD * 2, h: r.height + PAD * 2 }
      : { x: vw / 2, y: vh / 2, w: 0, h: 0 };
    setStyle(masks.t, 'cssText', `left:0;top:0;width:100%;height:${Math.max(0, box.y)}px`);
    setStyle(masks.b, 'cssText', `left:0;top:${box.y + box.h}px;width:100%;height:${Math.max(0, vh - box.y - box.h)}px`);
    setStyle(masks.l, 'cssText', `left:0;top:${box.y}px;width:${Math.max(0, box.x)}px;height:${box.h}px`);
    setStyle(masks.r, 'cssText', `left:${box.x + box.w}px;top:${box.y}px;width:${Math.max(0, vw - box.x - box.w)}px;height:${box.h}px`);
    ring.hidden = !box.w;
    if (box.w) {
      setStyle(ring, 'cssText', `left:${box.x}px;top:${box.y}px;width:${box.w}px;height:${box.h}px`);
    }
    // Below the target when there is room for the card, otherwise above it, and never off-screen.
    const need = cardRoom();
    const below = vh - (box.y + box.h) > need || box.y < need;
    setStyle(card, 'cssText', below
      ? `top:${Math.max(8, Math.min(vh - card.offsetHeight - 8, box.y + box.h + CARD_GAP))}px`
      : `bottom:${Math.max(12, vh - box.y + CARD_GAP)}px`);
  }

  return {
    active: () => !!lesson,
    /** Runs every frame: starts what is owed, points at the step, and moves on when it is answered. */
    update(covered) {
      const s = game.state;
      if (!lesson) {
        const due = s.taught ? E.lessonDue(s) : null;
        if (!due) {
          el.hidden = true;
          return;
        }
        start(due);
      }
      // A dialog is its own lesson; stand back until it is closed.
      if (covered) {
        el.hidden = true;
        return;
      }
      const cur = step();
      if (cur.done === 'tap' ? tapped : cur.done !== 'read' && E.reached(s, cur.done)) {
        advance();
        if (!lesson) return;
      }
      el.hidden = false;
      const now = step();
      if (shown !== lesson.id + at) {
        shown = lesson.id + at;
        put(sayEl, now.say);
        put(stepEl, `Step ${at + 1} of ${lesson.steps.length}`);
        nextBtn.hidden = now.done !== 'read';
      }
      // What the step points at, or, while that is on another screen, the way to get there.
      const wanted = now.at ? root.querySelector(now.at) : null;
      if (wanted) {
        centre(wanted);
      }
      frame(wanted || (now.at ? root.querySelector('.bottom-nav') : null));
    },
  };
}
