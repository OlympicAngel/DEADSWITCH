# TASK: F-004 Save/snapshot format

- Status: Done
- Started: 2026-10-03   Branch: claude/magical-ritchie-bx4xbb
- Pillar / milestone: foundations / M0
- Spec: docs/adr/0008-save-format.md
- Sources: ADR-0002, ADR-0003 (+ amendment), ADR-0004, ADR-0007, docs/agents/sim-determinism.md

## Goal
A run can be saved to bytes and restored so that continuing the restored run is bit-identical to never having stopped. Saves are versioned, checksummed and reject corruption with a clear error; balance changes between game versions never brick a save. One declaration of state fields drives the hasher and the serializer, so "every field must be hashed" can no longer be forgotten.

## Steps
- [x] 1. `IStateVisitor` + `GameState.Visit` (fields declared once); `StateHasher` rebuilt on it (same coverage, new hash values)
- [x] 2. Binary save format v1: header (magic, version, seed, config hash, schema versions), state, command log, event log, FNV-1a checksum; `SaveGame.Write/Read`; `Simulation` restore path
- [x] 3. Tests: save/load/continue equals continuous run (many seeds, mid-day, with commands), corrupted/truncated/foreign bytes rejected, newer version rejected, config change loads with a flag
- [x] 4. Atomic file store for hosts (temp + rename + .bak fallback) in a host-side helper; CLI `run --save/--load`
- [x] 5. ADR-0008, determinism playbook (state visitor rule), HANDOFF/BACKLOG

## Notes
- `GameState` moves from properties to public fields so the visitor can take `ref`.

- Host services live in the new `src/Deadswitch.Host` package (Unity manifest updated).

## Blocked / questions
- none
