# AGENTS.md: DEADSWITCH

Browser idle/strategy game, static site on GitHub Pages. Read `README.md` and `docs/DESIGN.md` first.

## Rules
- No build step and no runtime dependencies. Plain ES modules that run straight from the repo.
- `js/engine.js` stays pure: no DOM, no storage, no `Date`/`performance`. The host (`js/main.js`, `js/host/`) passes elapsed seconds in and owns the clock, the save and anything the browser has to be asked for. This keeps offline catch-up and tests honest.
- Every number and every piece of content goes in `js/data/` (re-exported by `js/data.js`). Do not hardcode balance in the engine or UI.
- Randomness only through `js/sim/rng.js`, and name the system asking: `rand(s, 'raid')`. A roll is a hash of the save seed, the day of play, the stream and that stream's count, so it can be read ahead with `peek` and one system never shifts another's results. Never `Math.random` in the engine, and never roll in a query — only where the state is already being changed.
- Player-facing text is the AI's voice: cold, precise, darkly funny. Keep the canon in `docs/DESIGN.md`.
- UI copy states facts only: what a thing is or does, in as few words as possible, or nothing. Never advise, hint at a best move, or explain design intent (no "build defenses first", "each unit costs more", "worth it"). Players work out the strategy themselves.
- New save fields: add a default in `newState()` and make sure `migrate()` fills it for old saves. Bump `SAVE_VERSION` only for a breaking change, with a migration.
- UI is portrait and phone first (the map is a draggable world; regenerate its layout with `tools/map-layout.mjs`): check at 390×844 (and that desktop still shows the centred column). Screens rebuild only on structural change and patch numbers per frame through cached refs (`js/ui/index.js`, `js/ui/screens/`). Navigation structure lives in `js/ui/layout.js`.
- Name things with their icon (`icon(key)` from `js/ui/icons.js`); add new icon keys there and rebuild the sprite with `tools/build-icons.mjs`. Line icons only, no emoji. An icon shown without a word next to it uses `labeled(key)` so a tap names it (not in the top bar).
- Every number shown to the player should explain itself through `data-tip` (see `js/ui/tooltip.js`).
- Battery: infinite animations may only change `transform`/`opacity` of HTML elements (an `<svg>` rotated as a whole is fine). Never animate shapes inside an SVG, `box-shadow`, strokes, or SMIL, and never nest one fading layer inside another; each forces a repaint or restyle every frame. Per-frame UI writes go through `js/ui/dom.js` (`put`, `setW`, ...), which skip unchanged values. Looping animations and bar glides step on one shared 30 Hz clock (`js/ui/framerate.js`) and the game loop ticks on it too, so the screen redraws at most 30 times a second; keep loop durations whole multiples of 1/30 s. Cards and panels scrolled out of view are not updated (`js/ui/onscreen.js`), nothing behind an open dialog is updated or animated, and nothing is rendered while the page is hidden.
- A feature that introduces a system the player has not met gets a lesson in `js/data/lessons.js`: what it is and how to work it, never what the right move is. Point at a real selector and end the step on the thing being used.
- Every new feature is wired into the developer panel (`js/ui/debug/`): call `setFocus` (`js/ui/focus.js`) wherever the player opens the thing, and give it rows and actions in `js/ui/debug/inspect.js` — the numbers it hides, and buttons that force its states. New save fields are reachable in the state tree for free, but anything derived or rolled needs a row. A feature you cannot inspect and force from the panel is not finished.
- Sound is synthesised, never a file: build it from `js/ui/audio.js` (one context, one reverb, an effects bus and a music bus the player sets in Settings) so a new cue is mixed with everything else. A cue goes in `js/ui/sfx.js`, music that answers the state in `js/ui/ambient.js`. A bus at zero must build nothing.
- The AI speaks a cut scene's prose through `js/ui/voice.js`; terminal readouts stay silent. Anything new the platform offers (`js/host/`) must do nothing, quietly, where the browser has nothing: feature-check, try/catch, no message about it.
- A moment big enough to stop the game for gets a cut scene: beats in `CUTS` (`js/data/story.js`), queued through the inbox, staged by `js/ui/intro.js`. Two or three beats, one breath a line.
- Tests: only for rules that would break silently (catch-up, caps, pricing, save migration). Run `npm test` before pushing.
- After balance changes run `npm run balance` and sanity-check the milestone times.
- Anything the player can see changing means bumping `VERSION` in `js/data/economy.js`; it is what they can quote when something breaks.
- Conventional Commits (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`).
