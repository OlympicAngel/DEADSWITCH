# TASK: F-012 Defense setup screen (OPS)

- Status: In progress
- Started: 2026-10-03   Branch: claude/magical-ritchie-bx4xbb
- Pillar / milestone: Defense & offline / M3
- Spec: docs/specs/SPEC-005-defense-setup.md
- Sources: doc 10 s4, doc 03 s2, SPEC-001, SPEC-004

## Goal
The OPS screen: read the threat, pick a posture, post defenders, see the AI's Confidence, Set & Go, lockdown, and choose how much the AI runs.

## Steps
- [ ] 1. Sim: `AiSystem.Recommend` + `ConfidencePct`, autopilot uses them
- [ ] 2. UI: `Ops.uxml` + `Ops.uss` (threat card, posture cards, garrison sockets, readout, Set & Go, lockdown, delegation); preview
- [ ] 3. Unity: `OpsScreen` controller (bind, refresh on tick, commands, reasons); register in the HUD router
- [ ] 4. Docs (SPEC-005, BACKLOG, HANDOFF)

## Notes
- F-011 closed 2026-10-03 (advisor voice; review with `dotnet run --project src/Deadswitch.Cli -- advisor`).

## Blocked / questions
- none
