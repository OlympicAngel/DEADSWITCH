# AGENTS.md: DEADSWITCH

Browser idle/strategy game, static site on GitHub Pages. Read `README.md` and `docs/DESIGN.md` first.

## Rules
- No build step and no runtime dependencies. Plain ES modules that run straight from the repo.
- `js/engine.js` stays pure: no DOM, no `localStorage`, no `Date`/`performance`. The host (`js/main.js`) passes elapsed seconds in. This keeps offline catch-up and tests honest.
- Every number and every piece of content goes in `js/data/` (re-exported by `js/data.js`). Do not hardcode balance in the engine or UI.
- Randomness only through `js/sim/rng.js` (seeded, stored in the save). Never `Math.random` in the engine.
- Player-facing text is the AI's voice: cold, precise, darkly funny. Keep the canon in `docs/DESIGN.md`.
- New save fields: add a default in `newState()` and make sure `migrate()` fills it for old saves. Bump `SAVE_VERSION` only for a breaking change, with a migration.
- UI: rebuild structure only on structural changes; patch numbers per frame through cached refs (see `ui.js`). Check desktop (1366px) and phone (390px) widths.
- Tests: only for rules that would break silently (catch-up, caps, pricing, save migration). Run `npm test` before pushing.
- After balance changes run `npm run balance` and sanity-check the milestone times.
- Conventional Commits (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`).
