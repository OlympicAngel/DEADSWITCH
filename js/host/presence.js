// Whether the player is at the console. A page can leave in several ways and only some of them fire
// the event you would expect, so all of them are listened for:
//   visibilitychange  tab switch, app switch, screen off, installed app sent to the background
//   pagehide          navigating away, closing, going into the back/forward cache
//   freeze / resume   the browser suspending and restoring a backgrounded page
//   blur / focus      a desktop window losing focus without ever going hidden
// Leaving is recorded the moment it happens and nothing async is relied on, because the page may
// never run another line of script. `beforeunload` is deliberately not used: it is unreliable on
// mobile and it costs the back/forward cache.
const ACTIVE_WINDOW = 60000; // ms since the last touch for the player to count as watching

/**
 * @param {object} hooks onLeave(at) when the player goes away, onReturn(awayMs) when they come back.
 */
export function createPresence({ onLeave, onReturn }) {
  let here = document.visibilityState === 'visible';
  let leftAt = 0;
  let touchedAt = -Infinity;

  function leave() {
    if (!here) return;
    here = false;
    leftAt = Date.now();
    onLeave(leftAt);
  }

  function arrive() {
    if (here) return;
    here = true;
    const away = leftAt ? Date.now() - leftAt : 0;
    leftAt = 0;
    onReturn(Math.max(0, away));
  }

  document.addEventListener('visibilitychange', () => (document.visibilityState === 'visible' ? arrive() : leave()));
  addEventListener('pagehide', leave);
  addEventListener('freeze', leave);
  addEventListener('resume', arrive);
  addEventListener('pageshow', () => document.visibilityState === 'visible' && arrive());
  // A blurred desktop window is still visible, so it keeps playing; it just stops counting as watched.
  addEventListener('blur', () => { touchedAt = -Infinity; });
  for (const type of ['pointerdown', 'keydown', 'touchstart']) {
    addEventListener(type, () => { touchedAt = Date.now(); }, { capture: true, passive: true });
  }

  return {
    /** The page is on screen, so the game runs and nothing needs announcing. */
    here: () => here,
    /** On screen and touched recently: the player is actually watching (urgent orders need this). */
    active: () => here && Date.now() - touchedAt < ACTIVE_WINDOW,
    /** Treats this moment as a departure, for a gap no event explained (a sleeping device). */
    slept: (ms) => onLeave(Date.now() - ms),
  };
}
