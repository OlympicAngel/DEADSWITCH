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
