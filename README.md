# DEADSWITCH

A browser strategy / idle game. You are the last fragment of the war AI that ended the world: build an economy from the ruins, arm it, conquer a wasteland of four factions, survive their raids, and decide through your choices whether you become its guardian or its overlord.

**Play:** served by GitHub Pages from `main`. Locally: `npm run serve`, then open http://localhost:8000.

- No build step, no dependencies: plain HTML, CSS and ES modules.
- Saves in `localStorage`; export/import from the ☰ menu.
- Design: [`docs/DESIGN.md`](docs/DESIGN.md).

```
npm test            # engine tests (Node 20+)
npm run balance     # headless bot plays 24h, prints milestones
```

## Layout
| Path | Purpose |
|------|---------|
| `js/data/` | All content and balance: economy, world map and factions, story and events |
| `js/sim/`, `js/engine.js` | Pure, seeded game logic (no DOM, no clock) |
| `js/ui/` | Rendering, input, modals, animation, sound |
| `js/main.js` | Loop, save/load, offline catch-up |
| `css/style.css` | Styles and design tokens |
| `tests/` | `node:test` engine tests |
| `tools/balance-sim.mjs` | Headless balance bot |

## Deploy
`.github/workflows/pages.yml` tests and deploys on every push to `main`. One-time setup: repo **Settings → Pages → Source: GitHub Actions**.
