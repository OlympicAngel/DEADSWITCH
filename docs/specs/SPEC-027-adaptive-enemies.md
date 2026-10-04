# SPEC-027: Adaptive enemies

- Status: Done (numbers are placeholders, tune with play data)
- Pillar: Defense & offline
- Touches: defense postures (counters), raids (learning), world ops (fortified sites), map and OPS (legibility)
- Source rules: doc 04 s9 and doc 05 s2 (adaptive enemies: rivals learn your tactics and counter them)

## Goal
Make the same safe answer stop being safe. Factions remember how the Hub met them and come prepared, so varying
postures and targets matters. The counters are always stated plainly; they are never hidden.

## Rules (all numbers in `[adapt]`, (tune))
1. When an attack resolves (missed, repelled or breached), its faction learns the posture it met (Turtle, Dark or Evacuate), up to `learn_cap` levels per posture.
2. Against that faction's attacks, each learned level costs the Hub that posture's edge:
   - Turtle: `turtle_counter_pts` defense points ("they bring charges").
   - Dark: `dark_counter_pts` miss chance ("they sweep").
   - Evacuate: `evacuate_counter_pts` more loot taken ("they hunt caches").
   The edge never goes below zero.
3. A raid the Hub wins on a faction's site raises that faction's fortification (up to `fortify_cap`). Each level adds `fortify_pct`% to all its sites' defense.
4. At midnight, every `learn_decay_days` days each learned counter drops a level, and every `fortify_decay_days` days each fortification does.
5. Legibility: OPS posture cards show the reduced value and the counter in amber, the map shows "KNOWS TURTLE 2 // FORT 1" per faction, and the advisor speaks when a faction learns or fortifies.
6. No RNG draws. Save layout v23, event schema v23. A reboot starts the new site with no counters.

## Tests
`ThreatTests.Operation_ComesHome_WithHeatOnTheOwner_AndEndsMercy` (a won raid fortifies the owner). The chunking test, save test and balance runner cover learning (casual Turtle breaches 2% -> 8%).
