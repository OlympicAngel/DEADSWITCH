# SPEC-031: Starting regions

- Status: Done (numbers are placeholders, tune with play data)
- Pillar: Base & economy
- Touches: legacy cycle (relocation), economy (output bonuses), fuel
- Source rules: doc 06 s4 (reboot benefit: a better starting region, richer zones or a stronger strategic position)

## Goal
Relocating at a peak should feel like a choice of where to stand next, not only a reset.

## Rules (numbers in `[legacy]`, (tune))
1. `Relocate(region)` picks the next site:
   - Hollow: no bonus. The first site, and old command logs (A = 0).
   - Ridge: turret output +`ridge_turret_pct`%.
   - River: +`river_fuel_per_hour` fuel each hour, capped by storage.
   - Ruins: server rack output +`ruins_compute_pct`%.
2. A forced reboot (the Hub lost, collapse, takeover) settles wherever the core can flee: a hash pick of the four, never the handler's choice.
3. The region lasts the whole cycle. The Legacy screen shows it and offers the choice next to RELOCATE, and the advisor names it on arrival.
4. No RNG draws. Save layout v27, event schema v27.

## Tests
`ThreatTests.Relocation_CarriesTheLegacy_AndTheNewSiteSavesExactly` (relocates to River; save and replay hold).
