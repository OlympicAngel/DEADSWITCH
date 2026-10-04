# SPEC-025: Alliances

- Status: Done (numbers are placeholders, tune with play data)
- Pillar: Offense & diplomacy
- Touches: defense rating (ally fighters), raids (attacker pick), trade prices, heat (the rival's grudge), ops (striking an ally)
- Source rules: doc 05 s3 (temporary pacts and alliances that either side can break), SPEC-023 (ceasefires)

## Goal
Choose a side. An alliance puts another camp's fighters on your wall for a daily price. It also feeds its rival's
grudge, and it can end on the ally's terms. Who you stand with should be a choice you feel.

## Rules (all numbers in `[diplomacy]`, (tune))
1. `ProposeAlliance(faction)`: one at a time; only with a Cold faction; not while its attack is inbound. Costs `alliance_energy` + `alliance_fuel`.
2. While allied, that faction sends no attacks. It trades `trade_discount_pct`% cheaper, and its fighters add `alliance_defense_by_tier` to the defense rating (none against its own attack).
3. Every day the ally takes `alliance_upkeep_energy`, and its rival gains `alliance_rival_heat` (Rustborn and Vanguard are rivals, as are Church and Halcyon).
4. It ends:
   - when the Hub dissolves it (`EndAlliance`, two taps; heat +`alliance_end_heat`);
   - when the daily share goes unpaid (same heat);
   - when the ally's heat reaches Watched;
   - when the ally walks out (`alliance_walkout_pct` per day, a hash roll with no RNG draw);
   - when the Hub strikes its sites (heat +`alliance_betray_heat`).
5. No RNG draws. Save layout v21, event schema v21.

## Tests
`ThreatTests.Ceasefire_KeepsAFactionAway_UntilTheHubStrikesIt` (one alliance at a time, the wall gains the ally's fighters, an unpaid share ends it).
