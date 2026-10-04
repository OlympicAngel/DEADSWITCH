# TASK: F-057 2.5D sector map rework

- Status: In progress
- Started: 2026-10-04   Branch: claude/confident-heisenberg-m3gwju
- Pillar / milestone: Offense & world (presentation) / M6
- Spec: docs/specs/SPEC-033-sector-map.md
- Sources: owner brief (2026-10-04), doc 05 s6, doc 11 (Master art direction, Environment construction rules), ADR-0007, `docs/agents/environment-art.md`

## Goal
Replace the flat radar plot with a 2.5D map: fixed-tilt ruined terrain with kit landmarks per site and hazard ground
treatments, an illustrated layer (territory, routes, fog of war, fallout haze), terminal UI on top, glitching AI
estimates. Done = renders reviewed at day/dusk/night, Unity wired and compiling, docs updated.

## Steps
- [x] 1. Spec and task
- [ ] 2. Art: `SectorScene` terrain + fixed camera + projection; `art export --map`; preview reads the camera from the scene (verification: renders day/dusk/night)
- [ ] 3. Art: site landmarks per kind, the Hub, hazard ground treatments (verification: renders, close-ups)
- [ ] 4. Art: `SectorOverlay` from the game state (territory, routes, fog of war, fallout haze, marker anchors); preview draws it (verification: render with overlay)
- [ ] 5. Unity: `MapView` (map world on its own layer, map camera, render to texture on demand, fog/shadow overrides) (verification: Unity compile check)
- [ ] 6. Unity: map screen shows the render, draws the overlay (Painter2D) and projected markers, glitching estimates (verification: Unity compile check, UI preview)
- [ ] 7. Docs (doc 11 map note, HANDOFF Editor checklist), finish

## Notes
- Owner direction (2026-10-04): gameplay first in cloud sessions; F-057 was queued by the owner right after F-055.
- F-056 (AI builds in secret) is next after this; F-099 stays last (phone and Editor checks need the owner).

## Blocked / questions
- none
