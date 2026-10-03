# TASK: F-006 Pressure loop

- Status: Done
- Started: 2026-10-03   Branch: claude/magical-ritchie-bx4xbb
- Pillar / milestone: Base & economy + Defense / M1
- Spec: docs/specs/SPEC-001-pressure-loop.md
- Sources: doc 03 s3+s6, doc 04 s2-5, doc 10 s3-4

## Goal
Raids announce themselves, the handler prepares (turrets, garrison, posture, OVERRIDE), and the loss ledger explains every loss. Corruption becomes a live system fed by unmanned facilities and OVERRIDE.

## Steps
- [x] 1. FixedMath (integer log2/exp2/pow) for power^0.7
- [x] 2. Corruption in milli-units with bands + automation load; OVERRIDE charges/cooldown/lockdown
- [x] 3. Turret facility, garrison, postures, presence commands
- [x] 4. Raid lifecycle: spawn -> warning (AI estimate) -> resolve vs defense, loot/casualties, loss ledger, mercy window
- [x] 5. Balance file regenerated, CLI raid summary over 30 days x seeds, docs/HANDOFF/BACKLOG

## Notes
- Minimal testing: one FixedMath accuracy test; existing determinism/save/replay tests cover the rest (they hash all state).

## Blocked / questions
- none
