# SPEC-035: Unit families and counters

- Status: In progress (numbers are placeholders, tune with play data)
- Pillar: Defense & offline (touches Base & economy, AI relationship)
- Touches: facilities and crewing, defense rating, raids, intel, glitches (unmanned machines), fuel, OPS screen
- Source rules: doc 10 s4 damage model (drones beat infantry in the open, heavy vehicles beat drones, infantry with traps
  or EMP beat heavy vehicles in ruins); doc 04 s6 (troops, drones, heavy vehicles; machines need operators or run
  glitchy under the AI); doc 02 s5-6 (crewing; military facilities: barracks, drone bays, vehicle factories, turrets)

## Goal
A raid is not just a number. Each faction brings its own kind of force, the warning says what is coming, and the
handler answers with the right mix: garrison against tanks, drones against foot soldiers, vehicles against drones.
Reading the forces and shifting the defense is a small, satisfying decision before logging off.

## Rules (numbers in `[units]`, `[facility_drone_bay]`, `[facility_motor_pool]`, (tune))
1. Three defender families: **infantry** = the garrison (people on the wall), **drones** = Drone Bays (from Tier 1),
   **vehicles** = Motor Pools (from Tier `motor_pool_min_tier`). Turrets and allied fighters are neutral.
   Barracks are the garrison itself (no separate building).
2. Drone Bays and Motor Pools are facilities: energy upkeep, crew per level; unmanned they run under the AI (automation
   load) and can misfire like unmanned turrets. Motor Pools also burn `motor_pool_fuel_per_hour` fuel; without fuel
   their vehicles stand still (no defense).
3. Every attack has a force mix (infantry / drones / vehicles %, sums to 100) from its faction's profile
   (`mix_<faction>`) plus a hash jitter of up to `mix_jitter_pct`. The warning names the mix unless the AI predicts
   nothing (ambush, silence, flush).
4. Counters (doc 10): a defender family gains `counter_pct` x the share of attackers it beats and loses the same x the
   share that beats it. Drones beat infantry, vehicles beat drones, infantry beat vehicles. With no attack under way
   the rating is shown without counters.
5. No RNG draws (hash jitter). Save layout v31.

## Feedback
OPS shows the forces and the defense by family with its counter effect; the advisor names the main threat and the
answer; the build sheet explains each unit family's counter.

## Tests
`ThreatTests.UnitFamilies_CounterTheRaidMix` (drones help against an infantry-heavy raid and hurt against vehicles;
the mix sums to 100 and saves).
