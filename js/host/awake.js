// Keeping the screen on. An idle game is watched as much as it is played: a countdown running down
// to an attack, or a cut scene talking, should not be interrupted by the phone dimming. The lock is
// held only while there is a reason, and the browser drops it on its own when the page is hidden,
// so it is re-taken on the way back.
const SOON = 150; // seconds: an attack this close is worth watching the clock for

let lock = null;
let want = false;

/** True while something on screen is worth keeping the screen on for. */
export function needsAwake(attack, cinema) {
  return !!cinema || !!(attack && attack.remaining < SOON);
}

/** Takes or drops the lock. Safe to call every frame: it only acts on a change. */
export function keepAwake(on) {
  if (on === want) {
    return;
  }
  want = on;
  if (!on) {
    const held = lock;
    lock = null;
    if (held) {
      held.release().catch(() => {});
    }
    return;
  }
  if (!navigator.wakeLock) {
    return;
  }
  navigator.wakeLock.request('screen').then((l) => {
    if (!want) {
      l.release().catch(() => {});
      return;
    }
    lock = l;
    // The browser takes it back when the page is hidden; ask again when the player returns.
    l.addEventListener('release', () => { lock = null; }, { once: true });
  }).catch(() => {
    // Refused (low battery, no permission, not supported): the screen behaves as it always did.
  });
}

/** Called when the page comes back: re-takes a lock the browser dropped while it was away. */
export function regainAwake() {
  if (want && !lock) {
    want = false;
    keepAwake(true);
  }
}

export const awakeHeld = () => !!lock;
