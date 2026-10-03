# TASK: F-009 Terminal HUD + command bar

- Status: Done
- Started: 2026-10-03   Branch: claude/magical-ritchie-bx4xbb
- Pillar / milestone: presentation / M2
- Spec: doc 08 s4 (always-visible essentials), doc 11, doc 10 s4 (alert presentation)
- Sources: docs/agents/quality-bar.md, docs/narrative/ADVISOR_VOICE.md

## Goal
The always-visible essentials (next timers, energy balance, corruption, threat) read at a glance over the 3D base; an advisor line gives the AI a voice; raid warnings are unmistakable (amber, diamond shape, countdown, DEFEND); a thumb-reachable command bar switches Base / Map / Core / Ops.

## Steps
- [x] 1. `Hud.uxml` + `Hud.uss` layout, previewed and polished
- [x] 2. Procedural tab icons (Mesh2D) + preview mirror
- [x] 3. `HudController`: binds sim state (animated counters, meters, sparkline, gauge, clock, next timer, OVERRIDE pips), raid banner, advisor ticker (typewriter + glitch)
- [x] 4. Screen router for the command bar (Base / Map / Core / Ops) with transitions; placeholder-free empty states in the AI's voice
- [x] 5. Docs, HANDOFF/BACKLOG

## Notes

## Blocked / questions
- none
