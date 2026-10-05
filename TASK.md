# TASK: F-101 Calm density and motion (living interface II)

- Status: In progress
- Branch: ccr-3e0227c3-c2kae3
- Spec: `docs/specs/SPEC-040-calm-density-and-motion.md`

## Steps
- [x] 1. Motion kit: stagger, directional slides, ripples, meter sweep, shimmer, springy sheets
- [ ] 2. Kit: page tabs (`ds-pager`) and foldable cards, mirrored in the preview
- [ ] 3. HUD: status rail with expandable pills, compact comms panel, sliding tab indicator, pod punches
- [ ] 4. Pages for OPS, CORE, LEGACY and MAP; folds on secondary cards
- [ ] 5. Base motion: construction rise with dust, build punch, hit flash
- [ ] 6. Docs, screenshots

Previous: F-100 living interface (SPEC-039), code complete; Editor tuning pending.

# (F-100 record)

- Status: Code complete; Editor tuning pass pending (owner)
- Branch: ccr-3e0227c3-c2kae3
- Spec: `docs/specs/SPEC-039-living-interface.md` (the 60 ideas and their status)
- Sources: owner direction 2026-10-05, doc 11 (amended: restrained cyberpunk accent on UI and AI tech), `docs/agents/quality-bar.md`
- Previous task F-099 (polish pass) waits on the owner's Editor and phone run; its open items are in the backlog row and `docs/agents/HANDOFF.md`.

## Steps
- [x] 1. Foundation: accent + depth tokens, shared icon data file (Unity + preview), new kit components (section header, card, progress, delta pill, toast)
- [x] 2. Resource pods: capacity bars, rate pills, FULL/LOW/SHORT states, time to full/empty
- [x] 3. Resource breakdown sheet with producers/drains and "how to get more" shortcuts
- [x] 4. Camera: fly-to focus (pan+zoom+orbit), inertia, double-tap zoom, idle orbit, settings toggles
- [x] 5. Focus mode: dimmed labels, quick-action ring, plot markers, build-complete moment
- [x] 6. JARVIS layer: core orb + waveform, comms panel with suggestion chips, toasts
- [x] 7. Attack cinematic: letterbox, title card, shot sequence, shake, impacts, siren, aftermath stamp
- [x] 8. Screen rework: OPS regrouped into titled cards, CORE with the orb as hero, command bar badges
- [x] 9. 3D accents: data links to the core, raid edge vignette
- [x] 10. Remaining screens to the new kit (Workforce, Map sheet, Dispatch, Settings), docs, spec statuses

## Notes
- Presentation only: no `GameState` fields, no sim changes.
- Verify with `tools/check.sh` (includes the Unity compile check) and `node tools/uipreview/preview.mjs`.

## Blocked / questions
- Camera, cinematic and 3D accents compile and follow the existing patterns, but need the owner's Editor run to tune timings and framing.
