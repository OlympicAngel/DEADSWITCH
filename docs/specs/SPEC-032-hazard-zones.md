# SPEC-032: Hazard zones and drifting fallout

- Status: In progress (numbers are placeholders, tune with play data)
- Pillar: Offense & world (supports Base & economy)
- Touches: operations (scout/raid), economy (loot, fuel), people, battle scars (repair parts), outposts
- Source rules: doc 05 s6 (zone types: reward and risk), doc 05 s7 (drifting hazards), doc 07 (the four kinds of damage)

## Goal
The wasteland itself is a place to go, not only the factions in it. Each zone pays in something the factions do not
(survivors, repair parts, rich salvage) and hurts in its own way. Scouting first is the preparation that halves the
risk, so the map rewards a careful player. A fallout front drifts between sites and changes which routes are safe.

## Rules (numbers in `[hazards]`, (tune))
1. Three wild zones, owned by nobody: **BLACK CRATER** (radiation), **QUARANTINE BLOCK** (plague), **DRONE BONEYARD**
   (machine graveyard). Scout and raid only (no hack, no sabotage, no claim). No heat, no ceasefire or alliance effect,
   no faction adaptation or luck; the AI's estimate is off until scouted, as at any site. The AI never sends its own
   raids there. A raided zone restocks after `zone_cooldown_hours`.
2. **Radiation:** rich loot. Entering costs +`radiation_fuel_pct`% fuel. A returning squad is sick with chance
   `radiation_sick_pct` and loses `radiation_casualty_pct`% of its survivors (at least 1).
3. **Plague:** a won raid brings back `plague_people` survivors (up to the population cap) and medical compute.
   Any return carries infection with chance `plague_infection_pct`: `plague_infected` people at home die (never under
   the minimum population).
4. **Machine graveyard:** strong drone defense. A won raid brings parts that repair `graveyard_repair_points` damage on
   the most damaged facility for free. A lost raid also loses `graveyard_nest_pct`% more of the squad.
5. **Preparation:** a scouted zone's risk chance (rules 2-3) is `prepared_risk_pct`% of normal.
6. **Fallout front:** from day `fallout_first_day` a fallout front lies over one non-wild site, starting next to the
   crater. Every `fallout_drift_hours` (+ a hash jitter up to `fallout_jitter_hours`) it drifts to one of the two
   sites nearest its current one. While covered: ops to that site cost +`fallout_fuel_pct`% fuel and their squads
   risk radiation sickness as in rule 2; an outpost there sends `fallout_outpost_pct`% of its output. It never covers
   the Hub.
7. Hash picks, no RNG draws. Save layout v29 (fallout site and next drift tick), event schema v28.

## Feedback
Map: zones drawn neutral with their hazard named; the fallout front marked on its site with the hours to the next
drift; the site sheet states the risk and whether it is halved by scouting. Advisor lines for sickness, infection,
survivors found, parts recovered, fallout drift.

## Tests
`WorldTests.HazardZones_PayAndHurt_AndFalloutDrifts` (zones give no heat; plague survivors; fallout drifts and saves).
