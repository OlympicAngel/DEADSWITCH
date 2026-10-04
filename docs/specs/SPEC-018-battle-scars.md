# SPEC-018: Battle scars (damage that stays until repaired)

- Status: Done (numbers are placeholders, tune at F-099)
- Pillar: Defense & offline
- Touches: raids and threats (breach), economy (output), people (regrowth), delegated AI, 3D base
- Source rules: doc 04 s4 ("battle scars stay visible until repaired"), doc 06 s3

## Goal
A defeat is seen and felt: burning facilities, soot and wrecks in the yard, and a real but recoverable cost that
the handler (or the delegated AI) clears with energy and time.

## Rules (all numbers in `[scars]`, (tune))
1. **Facility damage** 0..`max_damage` on Generator, Server Rack and Turret only (storage and beds never lose capacity, so caps never drop under what is held). Each point cuts output by `output_pct_per_damage`%.
2. **Breach scars** (hash picks, no RNG draws): a raid breach of at least `heavy_breach_permille` deals `raid_damage`; siege and Warlord wave `siege_damage`; purge `purge_damage`. A facility under repair is never hit. Wrecks: breached raid `breach_wreckage`, siege/Warlord `siege_wreckage`, purge `purge_wreckage`, repelled attack `repel_wreckage` (enemy hulks), up to `max_wreckage`.
3. **Wreckage** cuts regrowth by `regrowth_pct_per_wreck`% each (one rounding). `ClearWreckage` costs `clear_energy_per_wreck` x wrecks; not during an attack.
4. **Repair(slot)** costs `repair_energy_per_point` x level x damage and finishes after `repair_minutes_per_point` x damage; the damage holds until then. Not during an attack; demolish waits for it.
5. **Delegated AI** repairs the worst producer and clears the yard when half its stock covers it (no punitive absence).
6. **Visuals:** soot, scorch, rubble and torn panels by damage; fire (flames, embers, flickering light) from damage 2; smoke from a grey wisp (1) to a black plume (3); yard hulks and craters smoulder, the newest burn for `burn_hours`. Procedural only; effect intensity and reduced motion apply.
7. Save layout v11, event schema v11.

## Tests
`ThreatTests.Siege_BreaksABuilding_AndStartsMercy` (scars, output loss, repair); core-guarantee caps on damage and wreckage; chunked month.
