# TASK: F-003 Commands and typed event log

- Status: Done
- Started: 2026-10-03   Branch: claude/magical-ritchie-bx4xbb
- Pillar / milestone: foundations / M0
- Spec: ADR-0003 (amended: command log + replay)
- Sources: ADR-0002, ADR-0003, docs/agents/sim-determinism.md, doc 03 s2 (delegation levels, first real command)

## Goal
All player input enters the sim as tick-stamped commands that are validated, applied deterministically between ticks, and recorded. Replaying seed + config + command log reproduces the exact state hash, which later powers saves, battle-report replays and server verification. Events carry typed payloads with a schema version.

## Steps
- [x] 1. Split `Simulation.Step` into systems (`Systems/*`) over a shared `SimContext`; behavior unchanged (hashes identical)
- [x] 2. Command pipeline: `Command` (kind + int args), `CommandResult` with reject reasons, `Simulation.Execute`, `CommandLog`; first real command `SetDelegation` (Manual / Delegated / Autopilot)
- [x] 3. Typed events: `SimEvent` with four payload ints and per-kind docs, `EventLog.SchemaVersion`, sequence numbers; `DelegationChanged` event
- [x] 4. `Replay` (seed + config + commands -> state); tests: replay equals live run, chunking with commands, rejected commands not recorded
- [x] 5. Docs: ADR-0003 amendment, determinism playbook (commands), HANDOFF/BACKLOG

## Notes
- Commands apply between ticks at `State.Tick` (last completed tick). Rejected commands change nothing and are not recorded.

## Blocked / questions
- none
