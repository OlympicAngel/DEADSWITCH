# SPEC-002: Economy core (facilities, construction, power priority, crew)

- Status: Done (F-005)
- Pillar: Base & economy
- Touches: people (crew, population cap), AI corruption (unmanned facilities = automation load, consumed in M2), defense (raids loot the energy stock; turrets join the same power/crew rules in F-006), living base view (slots are the diorama layout)
- Source rules: doc 02 s1-8, doc 10 s1.3 and s3, doc 03 s3 (automation load), doc 06 s2 (tier gates), quality bar

## Goal
The handler grows the Hub by building and upgrading facilities in a fixed set of slots. Every level adds output **and** upkeep, so growth always costs more to sustain. When power runs short, facilities shut down in an order the handler chose (a planning decision, not a punishment). People crew the facilities; short-handed facilities are run by the AI at reduced output, which is the economy's link to corruption.

Player value: a clear build/upgrade decision every session ("power first, or compute for the AI?"), readable cause and effect in the energy balance, and a satisfying base that visibly grows.

## Non-goals
Military facilities and turrets (F-006), outposts, fuel income (operations), tier-ups, module discounts, delegated build queue (M2).

## Rules
1. **Rates are per hour** in config and delivered evenly per tick: on tick `t` a rate `R` delivers `floor(R*(k+1)/60) - floor(R*k/60)` with `k = t mod 60` (exactly `R` per game hour, chunking-safe, no remainder state).
2. **Hub slots.** The Hub has `hub.slots` slots *(tune: 6)*. Run start: slot 0 Generator L1, slot 1 Server Rack L1, rest empty. Starting rates equal doc 10 s3: generation 480/h (8/min), core upkeep 240/h, rack upkeep 180/h, rack compute 60/h.
3. **Facilities** (Tier 1): Generator (energy/h), Server Rack (compute/h), Life Support Grid (+population cap), Battery Bank (+energy cap). Each kind has per-level tables *(tune)*: build energy cost, build compute cost, build minutes, upkeep/h, output, crew. Max level = table length.
4. **Construction.** `build.queue_slots` jobs at once *(tune: 1)*. Build (empty slot) or Upgrade (+1 level) pays the full cost up front and finishes after the level's build minutes. An upgrading facility keeps running at its old level. Cancel refunds `build.cancel_refund_pct` *(tune: 50)*. Demolish clears a slot and refunds `build.demolish_refund_pct` *(tune: 25)* of the current level's cost.
5. **Power order.** Each tick: available = stock + generation. The AI core's upkeep is paid first; if it cannot be paid the Hub is in **blackout** (no facility runs, population regrowth pauses). Facilities are then powered in the handler's **priority order**; a facility whose upkeep cannot be paid is **shed** (no output, no upkeep). A shed facility restarts only when available energy covers its upkeep **plus one hour of it** (hysteresis: no flicker). The handler can switch any facility off manually. Leftover energy is stored up to the cap.
6. **Crew.** Facilities need crew per level *(tune)*, assigned in priority order from the population. Short-handed facilities run **unmanned** at `crew.unmanned_output_pct` *(tune: 50)* of output; the count of unmanned facilities is the **automation load** (feeds corruption in M2).
7. **Population cap** = `people.cap` + Life Support Grid bonus while it is powered. Regrowth only fills to the current cap; a lowered cap never kills anyone.
8. **Commands:** Build(slot, kind), Upgrade(slot), CancelJob(slot), Demolish(slot), SetFacilityPower(slot, on), SetPriority(slot, rank). All validated; rejections carry a reason (occupied, no job, queue full, max level, cannot afford...).
9. **Events:** BuildStarted, BuildCompleted, BuildCancelled, FacilityDemolished, FacilityShed, FacilityRestored, FacilityPowerSet, BlackoutStarted, BlackoutEnded, PriorityChanged.

## Acceptance criteria
- [x] Doc 10 starting rates reproduced exactly by the starting layout (guard test)
- [x] Per-hour delivery sums exactly to the rate over every game hour; chunking holds
- [x] Build/upgrade/cancel/demolish costs, refunds, timers and validation
- [x] Priority shedding follows the handler's order; hysteresis prevents flicker; blackout when the core cannot be powered
- [x] Unmanned facilities produce reduced output and count as automation load
- [x] Population cap includes Life Support only while powered; regrowth respects it
- [x] Save/load/replay equality with construction in flight
- [x] A scripted "sensible builder" over 7 days never blacks out and grows (guard)

## Tests
`EconomyTests` (rates, starting layout), `ConstructionTests`, `PowerPriorityTests`, `CrewTests`, plus determinism/chunking/save/replay with economy commands.

## Open questions
- Should Battery Banks leak (self-discharge) to keep stockpiling costly? Default no (upkeep 0) *(tune)*.
- Fuel income and fuel-costed facilities wait for operations (scavenging).
