# SPEC-001: The pressure loop

- Status: Done (F-006)
- Pillar: Base & economy, Defense & offline (feeds AI relationship)
- Touches: economy (turrets use power and crew, loot hits stock), people (garrison, casualties), AI corruption (automation load, OVERRIDE), offline play (presence, offline penalty)
- Source rules: doc 02 s3-5, doc 03 s3 + s6, doc 04 s2-5, doc 10 s3-4

## Goal
Growth always costs more to sustain and being away has consequences. The handler reads a raid warning, prepares (posture, garrison, turrets, or burns an OVERRIDE), and afterwards reads exactly what was lost and why in the loss ledger. Never wiped in one hit; mercy after a devastating loss.

## Non-goals
Factions and heat (heat = 0 until F-019), other signatures, live battles, AI misreporting beyond the warning estimate (F-011).

## Rules (all numbers *(tune)*, in the balance file)
1. **Corruption** is stored in milli-units (`0..100_000` = 0-100%). Idle decay 1.000/h. Each **unmanned** facility adds `automation_milli_per_hour`. Bands (doc 10 s3): Stable 0-30, Glitchy 31-60, Unstable 61-85, Critical 86-100.
2. **OVERRIDE** (doc 10 s3): start 1 charge, max 3, +1 every 12h, shared cooldown 30 min, +8.000 corruption per use. First use available now: **Emergency lockdown** cancels the incoming raid.
3. **Raids** spawn with chance 1/`mean_interval_ticks` per tick, max 3 per day, never during a **mercy window**. A spawned raid gives a **warning** of `warning_minutes` before it arrives (live warnings always show the signature). The AI's strength estimate is off by up to `estimate_error_pct_by_band` for the current corruption band.
4. **Raid strength** = `base_strength` + `power_coeff_permille` x (power rating)^0.7 / 1000, then x(100 +/- 15)% seeded variance, then x`offline_unprepared_pct` if the handler is away with posture None. Power rating = 10 x total facility levels + population (tall-poppy).
5. **Defense** = powered Turret output + garrison x `defense_per_defender`, then x posture modifier. Garrison (0..`garrison_slots`) takes people off crew duty.
6. **Postures:** None; **Turtle** (+`turtle_defense_pct` defense); **Dark** (`dark_miss_pct` chance the raid misses entirely; costs `dark_upkeep_per_hour` energy while active); **Evacuate** (no casualties, loot x`evacuate_loot_pct`).
7. **Resolution:** if defense >= strength the raid is **repelled** (no loss). Otherwise the breach share `(strength - defense) / strength` scales loot (energy and compute, each % of stock, each capped) and garrison casualties. Never a full wipe: caps per resource.
8. **Mercy window:** a raid that kills more than 25% of the population starts `mercy_hours` with no new raids (doc 10 s4).
9. **Loss ledger:** each raid emits `RaidWarning`, then `RaidResolved` (outcome, strength, defense) and one `LossLine` per lost resource, all tagged with the raid id.
10. **Presence:** the host sends `SetPresence(away/here)` at logout/login so offline rules apply deterministically.
11. RNG: every tick draws exactly four numbers (spawn, variance, miss, estimate) whether used or not.

## Acceptance criteria
- [x] Defense setup changes outcomes in a way the loss ledger explains
- [x] Mercy window and daily cap hold
- [x] 30 days x many seeds: no soft-lock, no full wipe (CLI)

## Open questions
- Should raid cadence scale with `RaidsToday` already taken, or stay flat until the cap?

## Verification (2026-10-03, seed 1, 30 days, idle Tier 1 base)
| Setup | Raids | Repelled | Energy lost | Compute lost |
|---|---|---|---|---|
| none, away | 72 | 0 | 4320 | 1080 |
| garrison 5 + Turtle | 72 | 23 | 540 | 57 |
| Dark | 72 | 31 missed | 716 | 609 (Dark upkeep sheds the rack) |
| Evacuate, away | 72 | 0 | 4320 | 1584 |
