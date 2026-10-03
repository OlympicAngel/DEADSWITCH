# ADR-0009: Save format and host services package

- Status: Accepted
- Date: 2026-10-03

## Context
Offline-first play needs saves that survive crashes, OS kills and game updates, and that continue bit-identically (offline catch-up, ADR-0004). The sim may not do I/O, but the same file logic is needed by Unity and the CLI and should be unit-tested.

## Decision
- **One field declaration.** `GameState.Visit(IStateVisitor)` lists every state field once. The state hash and the save serializer are both visitors, so a field cannot be saved but not hashed (or the reverse).
- **Binary format v1** (`SaveGame`, little-endian): magic `DSWS`, `u16` format version, `u16` event schema, `u64` seed, `u64` config hash, `i64` tick, `u64` state hash, state (visitor order), command log `(i64 tick, i32 kind, i32 a, b, c)*`, event log `i64 nextSeq, (i64 seq, i64 tick, i32 kind, i32 a, b, c, d)*`, `u64` FNV-1a checksum of everything before it.
- **Validation on load:** magic, checksum, supported version, collection lengths, no trailing bytes, and the restored state must match the stored state hash. Failures raise `SaveLoadException` with `NotASave`, `Truncated`, `Corrupted` or `UnsupportedVersion`.
- **Balance changes never brick saves:** a config hash mismatch loads with `ConfigChanged = true` and continues with the current config.
- **Host services package** `src/Deadswitch.Host` (netstandard2.1, UPM `com.deadswitch.host`, no UnityEngine): I/O and clocks allowed. `SaveFileStore` writes `.tmp`, flushes, rotates primary to `.bak`, renames tmp to primary; loads fall back to `.bak`. At every instant one complete copy exists.
- **Layout changes:** bump `SaveGame.FormatVersion` and add a migration that reads the old layout. Never reorder existing visitor calls without a version bump.

## Consequences
- Good: crash-safe, tamper-evident, platform-identical bytes; saves double as replay inputs (seed + config hash + command log); host logic is testable in xUnit.
- Bad / costs: full event log is stored (fine for now; compaction with snapshots comes when logs grow); migrations must be written by hand per version.

## Alternatives considered
- JSON saves: readable but large, float-prone tooling, no layout discipline.
- Unity `PlayerPrefs`: size limits, no atomicity, Unity-only.
- SQLite (doc 10 s8 suggestion): heavier dependency for one blob; can wrap these bytes later if multiple slots/cloud need it.
