# TASK: F-002 Balance config

- Status: Done
- Started: 2026-10-03   Branch: claude/magical-ritchie-bx4xbb
- Pillar / milestone: foundations / M0
- Spec: docs/adr/0007-balance-config-file.md
- Sources: doc 10 s3 (numbers), docs/agents/balance-tuning.md, docs/agents/sim-determinism.md, docs/agents/quality-bar.md

## Goal
Every tunable number lives in one human-editable balance file that the sim, CLI, tests and Unity all read. Designers tweak the file, run the CLI or tests, and see the effect; bad edits fail loudly with line numbers. Saves/replays can tell which config produced them (config hash).

## Steps
- [x] 1. Config visitor infrastructure (`IConfigVisitor`) + `SimConfig` split into sections with ranges and descriptions; existing code migrated; tests
- [x] 2. Balance file reader/writer (TOML subset) with strict validation: unknown, duplicate, missing, out-of-range, malformed values reported with line numbers; round-trip tests
- [x] 3. Config hash + clone; tests
- [x] 4. Shipped balance file `src/Deadswitch.Sim/Resources/DeadswitchBalance.toml`; tests load it strictly; feel guard tests run on the shipped file
- [x] 5. CLI: `run [--config path] [--seed n] [--hours n]`, `config dump`, `config check <path>`; defaults to the shipped file
- [x] 6. ADR-0007, balance-tuning playbook, README/AGENTS command updates, BACKLOG/HANDOFF

## Notes
- Code defaults = doc 10 baseline. The shipped file is what the game runs; it may diverge while tuning (log in doc 10 corrections when it changes a doc 10 number).
- Fixed-point naming: `Pct` (0-100), `Permille` (0-1000), `Bp` (basis points, 0-10000). Units in names: `PerTick`, `PerHour`, `Ticks`, `Cap`.

- `tools/gen_meta.py` creates Unity .meta files for new files in the sim package and `unity/Assets` (gate checks it).
- Bool and IntList readers have no shipped keys yet; add reader tests with the first such key (F-005).

## Blocked / questions
- none
