# ADR-0007: Balance config file

- Status: Accepted
- Date: 2026-10-03

## Context
The owner wants every number tweakable without touching code. The sim, the CLI balancing harness, the tests and Unity must all agree on the same values, and replays/saves must know which values produced them. The sim may not do I/O or take dependencies.

## Decision
- `SimConfig` is split into sections (`Deadswitch.Sim.Config.*Config`). Each section declares every value **once** through `IConfigVisitor.Int/Bool/IntList(key, ref field, range, description)`.
- That one declaration drives: the **balance file writer** and **strict reader** (`BalanceText`, a TOML subset: `[section]`, `key = int|bool|[ints]`, `#` comments), the **config hash** (`ConfigHasher`, FNV-1a 64 in visit order), `ConfigEntries` (flat list for diffs and debug UI), and `Clone()`.
- The game runs the **shipped file** `src/Deadswitch.Sim/Resources/DeadswitchBalance.toml` (inside the sim UPM package so Unity can load it from `Resources`). Code defaults are the doc 10 baseline and the lenient fallback for missing keys.
- Reading is pure string processing; callers (CLI, tests, Unity) load the text.
- Errors carry line numbers. Strict mode (tests, CLI, CI) rejects unknown, duplicate, missing, malformed and out-of-range values. Lenient mode (game) keeps defaults for missing keys and reports warnings.
- Saves and replays store the config hash (ADR-0008 save format).

## Consequences
- Good: one place to tune; typos fail loudly; feel guard tests run on the shipped file; no reflection (IL2CPP-safe); deterministic order.
- Bad / costs: a new tunable needs a `Visit` line; the shipped file must be regenerated (`config dump --out`) when keys are added, enforced by `ShippedFile_IsNormalized`; free-form designer comments in the file are not preserved (put notes in commits or doc 10).

## Alternatives considered
- JSON + `JsonUtility` / `System.Text.Json`: no comments, two different parsers, dependency in the sim.
- ScriptableObject: Unity-only; the CLI and tests could not read it.
- Reflection over fields: fragile under IL2CPP stripping, no ranges or descriptions, unstable order.
