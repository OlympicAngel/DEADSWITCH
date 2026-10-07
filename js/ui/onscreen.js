// Which parts of the screen are in view, so per-frame updates can skip what nobody can see.
// Visibility only changes when the screen is rebuilt, scrolled or resized, so it is measured then,
// not every frame. Elements not measured yet count as visible.
const MARGIN = 120; // px beyond the visible area that still counts, so scrolling never reveals stale values
const state = new WeakMap();
let watched = [];

export function watchVisible(els, root) {
  watched = els;
  // After the first paint of the new screen, when its layout is settled anyway.
  requestAnimationFrame(() => measureVisible(root));
}

// Returns true when something hidden came into view.
export function measureVisible(root) {
  let revealed = false;
  if (!watched.length) return revealed;
  const r = root.getBoundingClientRect();
  for (const el of watched) {
    const b = el.getBoundingClientRect();
    const on = b.bottom > r.top - MARGIN && b.top < r.bottom + MARGIN && b.height > 0;
    if (on && state.get(el) === false) revealed = true;
    state.set(el, on);
  }
  return revealed;
}

export const onScreen = (el) => state.get(el) !== false;
