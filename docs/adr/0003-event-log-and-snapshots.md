# ADR-0003: Append-only event log with snapshots

- Status: Accepted
- Date: 2026-10-03

## Context
Battle reports must list every loss line by line (the loss ledger), the AI may later edit what the player sees, and online play needs sync and verification.

## Decision
The sim writes an **append-only event log** (`EventLog`, `SimEvent`). The player-visible report is a *view* over the true log; AI edits apply to the view, never to the log. Periodic **snapshots** (state + `Pcg32` state/inc + tick) bound replay cost. Event schema changes are versioned.

## Consequences
- Good: replays, audits (Audit tool compares view vs log), cloud backup and server verification share one mechanism; the "AI lies" feature has a true record to contradict.
- Bad: log growth needs compaction after snapshots; schema versioning discipline required.

## Alternatives considered
- State-only saves: smaller, but no reports, no audit, no verification.

## Amendment (2026-10-03, F-003): commands and replay
- All player input is a `Command` (kind + three int args) executed between ticks via `Simulation.Execute`. Validation reads state only; a rejected command changes nothing (no state, RNG draws or events) and is not recorded.
- Accepted commands go to the `CommandLog` as `(tick, command)`, meaning "applied after tick `tick` completed". `Replay.Run(seed, config, commands, toTick)` must reproduce the live state hash and event log exactly; a command that fails on replay raises `ReplayDivergenceException`.
- Events carry `Seq` (unique, increasing), `Tick`, `Kind` and four int payloads documented per kind. `EventKind`, `CommandKind`, `RejectReason` and enum values in state are persisted: never renumber or reuse. `EventLog.SchemaVersion` is bumped when a payload meaning changes.
- Systems live in `src/Deadswitch.Sim/Systems/` and run in the fixed order in `Simulation.Step`; the order is part of the rules.
