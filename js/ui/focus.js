// What the player is looking at right now: the screen, a sector, a card, an order, an attack.
// Only the debugger reads it, but every feature should set it (see AGENTS.md).

let focus = { kind: 'screen', id: 'command', data: null };
let seq = 0; // bumped on every change, so the debugger knows when to rebuild

/** Records the thing now in front of the player. `data` carries whatever has no id of its own. */
export function setFocus(kind, id, data = null) {
  if (focus.kind === kind && focus.id === id && focus.data === data) {
    return;
  }
  focus = { kind, id, data };
  seq++;
}

export const getFocus = () => focus;
export const focusSeq = () => seq;
