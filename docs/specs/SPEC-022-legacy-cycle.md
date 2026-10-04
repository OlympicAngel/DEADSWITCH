# SPEC-022: The cycle (relocation, forced reboot, legacy)

- Status: Done (numbers are placeholders, tune with play data)
- Pillar: AI relationship / progression
- Touches: whole game state (fresh site), raids (hub falls), corruption (takeover), people (collapse), modules, heat
- Source rules: doc 06 s4, doc 10 s1.2 (forced reboot), s6 (legacy score, perks, mastery), doc 01 s9 success criteria

## Goal
The long loop: choose to relocate at a peak for a full legacy bonus, or lose the Hub to failure for half of it. Either
way the portable core carries modules, grudges, a few veterans and perks into a fresh site.

## Rules (all numbers in `[legacy]`, (tune))
1. **Legacy score** = highest tier x `score_per_tier` + peak power / `power_divisor` + veterans x `score_per_veteran` + mastery x `score_per_mastery`. Veterans = `base_veterans` + Old Guard perk + people / `veteran_per_people`.
2. **Relocate** (`Relocate`, two taps): needs `relocate_min_tier` reached and no attack inbound; earns `voluntary_bonus_pct`% of the score as legacy points.
3. **Forced reboot** (never random, doc 06 s4): the Hub falls to a purge met with no posture and no garrison; `takeover_critical_hours` at Critical corruption; `collapse_hours` at the people floor. Earns `forced_bonus_pct`%. Waits while an attack is inbound.
4. **What carries:** cycle count, legacy points and record, perks, mastery, `kept_modules` restored field modules, `heat_kept_pct`% of faction heat, the AI's dials and delegation, veterans (added to the starting people), corruption (0 after a relocation, `forced_corruption_kept_pct`% after a reboot). Everything else is a fresh site; a mercy window covers its first hours. Raid and operation ids keep counting.
5. **Perks** (`BuyPerk`, `perk_cost` x next level, max `perk_max_level`): Starting cache, Rebuilding surge (regrowth), Spare OVERRIDE (+1 charge), Cold trail (heat decay), Old guard (+1 veteran).
6. **Mastery** (doc 10 s6, kept across cycles): purge survived on Manual, Tier 2 with no outpost lost, a live battle held, a tier on Manual, a lie caught with Verify, corruption under 40% for a tier, relocation at peak power.
7. No RNG draws. Save layout v15, event schema v15. The Warlord Ultimatum counts days from the cycle start.

## Tests
`ThreatTests.Relocation_CarriesTheLegacy_AndTheNewSiteSavesExactly`.
