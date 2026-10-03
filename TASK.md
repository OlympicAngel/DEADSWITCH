# TASK: F-017 Logout projection + local notifications

- Status: In progress
- Started: 2026-10-03   Branch: claude/magical-ritchie-bx4xbb
- Pillar / milestone: Defense & offline / M3
- Spec: docs/specs/SPEC-010-notifications.md
- Sources: ADR-0004, doc 11 surface priorities

## Goal
Opt-in local notifications in the AI's voice, forecast by running a copy of the sim forward at logout.

## Steps
- [ ] 1. Host: `LogoutProjection` + alert texts, test
- [ ] 2. Unity: notifications facade + optional mobile backend, schedule on pause/quit, cancel on resume, OPS opt-in toggle
- [ ] 3. Docs (SPEC-010, BACKLOG, HANDOFF)

## Notes
- F-016 closed 2026-10-03 (opening flow).

## Blocked / questions
- none
