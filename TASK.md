# TASK: F-005 Economy core

- Status: Done
- Started: 2026-10-03   Branch: claude/magical-ritchie-bx4xbb
- Pillar / milestone: Base & economy / M1
- Spec: docs/specs/SPEC-002-economy-core.md
- Sources: doc 02 s1-8, doc 10 s1.3 + s3, doc 03 s3, ADR-0008, ADR-0009

## Goal
Facilities in Hub slots, a build/upgrade queue with timers, power priority with shedding and blackout, crew and unmanned (AI-run) facilities, population cap from Life Support. Growth always costs more upkeep; shortages are the handler's planned trade-off.

## Steps
- [x] 1. Per-hour rate delivery (`Rates.PerTick`) + config sections (`hub`, `build`, `crew`, facility tables) with IntList keys; reader tests for lists
- [x] 2. State: facility slots, build jobs, power priority (visitor; save format stays v1); starting layout; `EconomyQueries` (flows for UI)
- [x] 3. Construction system + commands Build/Upgrade/CancelJob/Demolish; tests
- [x] 4. Crew + power systems (priority, shedding hysteresis, blackout, manual power, SetPriority); production; population cap; tests
- [x] 5. Determinism/save/replay/chunking with economy; sensible-builder 7-day guard; shipped balance file regenerated
- [x] 6. CLI shows economy; docs (SPEC status, balance numbers), HANDOFF/BACKLOG

## Notes
- Save format stays v1: no saves have shipped yet. From the first public build on, layout changes need a version bump + migration.

## Blocked / questions
- none
