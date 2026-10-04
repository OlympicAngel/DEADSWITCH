# SPEC-023: Temporary pacts (ceasefires)

- Status: Done (numbers are placeholders, tune with play data)
- Pillar: Offense & diplomacy
- Touches: raids (attacker pick), world map (ops break pacts), trade prices, heat
- Source rules: doc 05 s3 (temporary pacts that either side can break; tribute grows), doc 10 s5 (trade)

## Goal
Buy breathing room from one faction while you deal with another, at a price that rises with how much they hate you,
and feel the cost of breaking your word.

## Rules (all numbers in `[diplomacy]`, (tune))
1. `ProposeCeasefire(faction)`: one at a time; price `ceasefire_energy` + `ceasefire_fuel` x `price_pct_by_level`[heat level]; a Marked faction will not talk; not while that faction's attack is inbound; `cooldown_days` after the last one ends.
2. For `ceasefire_days` that faction sends no attacks (other camps still do) and trades `trade_discount_pct`% cheaper.
3. Launching a raid or hack at its sites breaks it: heat +`break_heat`, the pact ends, the cooldown starts. Scouting does not break it.
4. No RNG draws. Save layout v18, event schema v18.

## Tests
`ThreatTests.Ceasefire_KeepsAFactionAway_UntilTheHubStrikesIt`.
