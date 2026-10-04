# SPEC-026: Sabotage ops

- Status: Done (numbers are placeholders, tune with play data)
- Pillar: Offense & diplomacy
- Touches: world ops (a fourth op kind), raids (attack strength), heat (traced or clean), spies (odds)
- Source rules: doc 04 s8 (sabotage and stealth ops with small elite teams), doc 05 s2 (sabotage that gets traced raises heat), doc 10 (ship after v1)

## Goal
Hit a faction where it hurts before it hits you. A quiet team burns their depots so their next strikes land soft.
Get caught and they know exactly who did it.

## Rules (all numbers in `[world]`, (tune))
1. `LaunchOp(site, Sabotage, squad)`: squad 1..`sabotage_max_squad`; travel fuel and time like a raid; the site's cooldown applies; it breaks a ceasefire or alliance with the owner like any strike.
2. Odds: 40 + 10 per saboteur, plus ST1 masking points, plus `sabotage_spy_pts` with a loyal agent inside the owner's camp, minus defense / 6. Clamped to 5..90.
3. Success: the owner's attacks launched within `sabotage_hours` lose `sabotage_strength_pct`% strength, and the site goes on cooldown. `sabotage_trace_pct`% of successes are traced (raid heat); the rest are clean (scout heat).
4. Failure: one saboteur is lost, and the owner takes raid heat.
5. No RNG draws (hash rolls). Save layout v22, event schema v22.

## Tests
`ThreatTests.Operation_ComesHome_WithHeatOnTheOwner_AndEndsMercy` (squad cap; a landed sabotage cripples the owner).
