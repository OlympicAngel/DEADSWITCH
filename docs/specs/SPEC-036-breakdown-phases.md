# SPEC-036: Breakdown phases and after-effects

- Status: Done (numbers are placeholders, tune with play data)
- Pillar: Offense & world (touches Base & economy, Defense)
- Touches: world events (SPEC-017), hazard zones (SPEC-032), reactor (SPEC-029), unit mix (SPEC-035), glitches, people
- Source rules: doc 07 s1 (the breakdown repeats as phases and as after-effects of the player's actions: fallout waves,
  blackouts, plague outbreaks, rogue-machine surges), doc 05 s7 (drifting hazards, rotating world events)

## Goal
The apocalypse is not over, and the handler can make it worse. Raiding the crater, letting the reactor leak or running
war machines without crews comes back as a wave the whole sector feels. The warning at half pressure makes the link
legible and gives a choice: ease off, or prepare and profit.

## Rules (numbers in `[phases]`, (tune))
1. Four breakdown phases: **fallout wave** (outposts send `fallout_outpost_pct`%, crater raids bring +`fallout_salvage_pct`%),
   **plague outbreak** (no regrowth; any returning squad infects with at least `plague_infection_pct`%), **rolling
   blackouts** (generators and the reactor lose `blackout_generation_pct`%), **machine surge** (attack mixes +`surge_drone_pts`
   drones; unmanned war machines turn +`surge_defection_pct`% more often).
2. Pressure per phase from the handler's actions: radiation, plague and graveyard raids; reactor leak hours; grid
   blackout hours; unmanned war machines per hour. Half the threshold warns; at the threshold that phase is the next
   world event and its pressure resets.
3. Fallout waves, plague outbreaks and machine surges also join the random world-event rotation; rolling blackouts come
   only as an after-effect (no blackout at default numbers stays a guard).
4. No RNG draws. Save layout v33.

## Tests
`ThreatTests.HazardZones_PayAndHurt_WithoutHeat_AndFalloutDrifts` (plague pressure brings the outbreak next; regrowth stops).
