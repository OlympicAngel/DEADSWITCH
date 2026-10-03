# Playbook: Balance tuning

1. Every number is a placeholder `(tune)` until a prototype run says otherwise.
2. Numbers live in **one place**: declared in a `SimConfig` section (`src/Deadswitch.Sim/Config/*Config.cs`) with a range and description, valued in the shipped balance file `src/Deadswitch.Sim/Resources/DeadswitchBalance.toml` (ADR-0008). Name the unit: `PerTick`, `PerHour`, `Ticks`, `Pct` (0-100), `Permille`, `Bp`, `Cap`.
   - **Tweak a value:** edit the shipped file, then `dotnet run --project src/Deadswitch.Cli -- config check` and the tests.
   - **Add a tunable:** add the field + one `v.Int(...)` line in its section's `Visit`, use it in the sim, then regenerate the file with `config dump --out src/Deadswitch.Sim/Resources/DeadswitchBalance.toml` (keeps your edited values) and run `python3 tools/gen_meta.py` if you added files.
   - **See what diverges from doc 10:** `config diff`.
3. Before changing a number: write the intended feel in one sentence ("a Tier 1 player sees a raid every 4-8h, never more than 3 a day").
4. Change it, then run the headless harness over many seeds:
   `dotnet run --project src/Deadswitch.Cli -- run --seed <seed> --hours <hours> [--config <file>]`
   Many seeds and player profiles at once (SPEC-014): `dotnet run --project src/Deadswitch.Cli -- balance [--seeds 100] [--days 30] [--profile active|casual|autopilot|idle|all]`; exit 1 means a hard guard failed.
5. Check the **net rates** by hand. Example of a bug caught this way: +6 gen - 4 upkeep - 3 rack = -1/min.
6. Add or update a guard test for the intended feel (blackout, cap, cadence). Guards run on both the code defaults and the shipped file (`TestConfigs`).
7. If the value differs from `docs/design/10_resolved_decisions.md`, add a row to its **Corrections log**.
8. Never tune by weakening a test.

## Intended feel (from design)
- Hard, rough, slightly rushed; never fully wiped in one hit.
- Losses are readable: the loss ledger explains every line.
- Mercy after devastating loss; offline cap 3 attacks / 24h in Tier 1, 4 in Tier 2.
