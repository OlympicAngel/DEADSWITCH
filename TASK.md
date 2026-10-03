# TASK: F-013 Battle report

- Status: In progress
- Started: 2026-10-03   Branch: claude/magical-ritchie-bx4xbb
- Pillar / milestone: Defense & offline / M3
- Spec: docs/specs/SPEC-006-battle-report.md
- Sources: doc 10 s4 + s7, ADR-0003, ADR-0007, doc 07 s7

## Goal
A graphic-novel report after each raid: four rendered panels, the AI's (possibly edited) summary, the true loss ledger, and Verify to catch the AI.

## Steps
- [x] 1. Sim: `[report]` config, report edit lie, `VerifyReport` command + `ReportVerified` event, tests
- [x] 2. Host: `BattleReport` view from the log (summary with edits, ledger, verify findings, captions); advisor verify lines
- [ ] 3. Art: gate camera shots + raider silhouettes (`HubScene.ReportShots`)
- [ ] 4. Preview: report mode in `tools/basepreview` (four stills, ink/halftone grade, composed page); iterate the look
- [ ] 5. Unity: report stills (RenderTexture + graphic-novel shader), Report screen (UXML), HUD chip, OPS link
- [ ] 6. Docs (SPEC-006, BACKLOG, HANDOFF)

## Notes
- F-012 closed 2026-10-03 (OPS screen).

## Blocked / questions
- none
