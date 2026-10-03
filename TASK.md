# TASK: F-008 Visual system

- Status: In progress
- Started: 2026-10-03   Branch: claude/magical-ritchie-bx4xbb
- Pillar / milestone: presentation / M2
- Spec: docs/design/11_visual_theme_and_motion.md, ADR-0007 (HUD stays a crisp 2D terminal over the 3D world)
- Sources: doc 11, doc 08 s4 + s6, docs/agents/quality-bar.md

## Goal
One token system and component kit that makes every screen look like the AI's field terminal: phosphor and amber on graphite, corner-bracket panels, mono readouts, subtle CRT life that scales with corruption and the effect-intensity setting. A headless preview renders UXML/USS to PNG so every screen is inspected before commit.

## Steps
- [x] 1. Tokens (`Tokens.uss`), component styles (`Components.uss`), theme, fonts via `resource()`
- [x] 2. `tools/uipreview` (UXML + USS -> HTML -> PNG via Playwright) and a kit fixture; review screenshot
- [ ] 3. Runtime: `UiRoot` (UIDocument + PanelSettings at runtime, safe area), element behaviors (corner brackets, segmented meters, sparkline, arc gauge) drawn with `generateVisualContent`
- [ ] 4. Motion: tween/easing helpers, count-up numbers, typewriter + glitch text (corruption + effects scaled), reduced-motion aware
- [ ] 5. `CrtOverlay` (scanlines, vignette, roll bar, corruption glitch slices), docs

## Notes
- No custom UXML element classes (API differs between 2021.3 refs and Unity 6): plain elements + classes, behaviors attached in C#.
- Git LFS uploads are blocked from cloud sessions; fonts are stored as plain binaries. Prefer procedural art.

## Blocked / questions
- none
