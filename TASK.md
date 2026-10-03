# TASK: F-010 3D base diorama

- Status: In progress
- Started: 2026-10-03   Branch: claude/magical-ritchie-bx4xbb
- Pillar / milestone: presentation / M2
- Spec: docs/specs/SPEC-003-base-diorama.md
- Sources: ADR-0007, doc 11, doc 06 s3

## Goal
A procedural stylized-3D Hub seen through a drone camera; facilities show kind, level and state; tapping a pad opens the build/upgrade sheet.

## Steps
- [x] 1. `src/Deadswitch.Art`: MeshBuilder (bevelled boxes, cylinders, frustums), palette, weathering vertex colors
- [x] 2. Models: AI core bunker, 5 facility kinds x 5 levels, scaffold, pad, props, terrain, perimeter; `HubScene` composition from GameState
- [x] 3. CLI `art export` + `tools/basepreview` (three.js, PBR, shadows, tilt-shift, grain) -> PNG; iterate on looks
- [ ] 4. Unity: mesh conversion, vertex-color URP shader (+ Lit fallback), lights, `BaseView` sync with state, animated parts, beacons
- [ ] 5. Drone camera (drift, drag/pinch bounds) + URP post (DOF tilt-shift, grain, CA, vignette, bloom)
- [ ] 6. Slot picking + slot sheet UI (build/upgrade/power/demolish) via commands; preview
- [ ] 7. Docs, HANDOFF/BACKLOG

## Notes
- Unity C# for URP (Volume, post overrides) is outside the compile check (no URP reference assemblies): keep it isolated in `Runtime/Rendering/` and simple.

- Preview: `dotnet run --project src/Deadswitch.Cli -- art export --days 7` then `node tools/basepreview/render.mjs` (first time: `cd tools/basepreview && npm install`). Look values in `tools/basepreview/look.json` must match Unity's `BaseLook`.

## Blocked / questions
- none
