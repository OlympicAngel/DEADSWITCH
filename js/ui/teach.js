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
  let centred = ''; // the step the target was last scrolled into view for

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

/** Brings the target into the middle of the screen, unless it is already somewhere it can be seen.
 *  Without this a step can point at a control below the fold and cut its hole over whatever is. */
  function centre(el) {
    const r = el.getBoundingClientRect();
    if (!r.width && !r.height) {
      return;
    }
    const h = innerHeight;
    if (r.top > h * 0.16 && r.bottom < h * 0.6) {
      return;
    }
    el.scrollIntoView({ block: 'center', inline: 'nearest' });
  }

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
    // Below the target when there is room for the card, otherwise above it.
    const below = vh - (box.y + box.h) > 190 || box.y < 190;
    setStyle(card, 'cssText', below
      ? `top:${Math.min(vh - 20, box.y + box.h + CARD_GAP)}px`
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
      if (wanted && centred !== shown) {
        centred = shown;
        centre(wanted);
      }
      frame(wanted || (now.at ? root.querySelector('.bottom-nav') : null));
    },
  };
}
