# SPEC-029: Reactor (risky power)

- Status: Done (numbers are placeholders, tune with play data)
- Pillar: Base & economy
- Touches: energy grid (a second power source), fuel economy, battle scars (targeting, leak), people, base art
- Source rules: doc 02 s3 (risky power sources: reactors give huge output but become targets and radiation hazards if hit), doc 02 s6 (power category)

## Goal
A late-game power spike with strings attached. The reactor out-produces every generator, but it eats fuel, raiders
aim for it, and a cracked one poisons the Hub until it is repaired.

## Rules (numbers in `[facility_reactor]` and `[reactor]`, (tune))
1. From Tier `min_tier`, at most `max_count` per Hub. It has 3 levels and is a power source like the generator (no grid upkeep).
2. It burns `fuel_per_hour`[level] spread over every minute it runs. Out of fuel it scrams at once and makes nothing; any fuel restarts it.
3. Each point of breach damage goes to the reactor `target_pct`% of the time while the reactor can take more.
4. At `leak_damage` or more, it leaks: each midnight `leak_people_per_day` people are lost (never below the people floor, never garrison) until it is repaired, and only while the handler is present (no punitive absence).
5. Legibility: the slot sheet shows a FUEL chip, SCRAMMED, or RADIATION LEAK, and the advisor speaks on scram, refuel and leak. The model is a containment drum and dome, a cooling tower, a coolant loop and a control room, growing with level.
6. No RNG draws. Save layout v25, event schema v25.

## Tests
`EconomyTests.Reactor_IsTierLocked_OnePerHub_AndScramsWithoutFuel`.
