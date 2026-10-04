# Playbook: Balance tuning

1. Every number is a placeholder `(tune)` until a prototype run says otherwise.
2. Numbers live in `SimConfig` (one place). Name the unit: `PerTick`, `PerHour`, `Percent`, `Cap`.
3. Before changing a number: write the intended feel in one sentence ("a Tier 1 player sees a raid every 4-8h, never more than 3 a day").
4. Change it, then run the headless harness over many seeds:
   `dotnet run --project src/Deadswitch.Cli -- <seed> <hours>`
5. Check the **net rates** by hand. Example of a bug caught this way: +6 gen - 4 upkeep - 3 rack = -1/min.
6. Update the existing guard test only if the change breaks a guarantee it asserts; do not add per-number tests.
7. If the value differs from `docs/design/10_resolved_decisions.md`, add a row to its **Corrections log**.
8. Never tune by weakening a test.

## Intended feel (from design)
- Hard, rough, slightly rushed; never fully wiped in one hit.
- Losses are readable: the loss ledger explains every line.
- Mercy after devastating loss; offline cap 3 attacks / 24h in Tier 1, 4 in Tier 2.
