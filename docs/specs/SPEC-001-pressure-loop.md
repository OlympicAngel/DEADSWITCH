# SPEC-001: The pressure loop (paper prototype)

- Status: In progress (skeleton exists in `Simulation`)
- Pillar: Base & economy (feeds AI relationship)
- Touches: AI corruption, defense and offline attacks, people
- Source rules: doc 02 sections 3-5, doc 03 section 3, doc 04 sections 2-5, doc 10 sections 3-4

## Goal
Prove that growth always costs more to sustain and that being away has consequences, using only energy, compute, people, corruption and one raid type, headless, with no art.

## Non-goals
Factions, heat, modules, UI, other attack signatures.

## Rules (current skeleton)
1. 1 tick = 1 game minute. Integers only.
2. Energy: `+gen - upkeep` per tick, then racks cost energy to produce compute. Cap 500. Net must stay positive at Tier 1 defaults (**gen = 8**, see correction below).
3. Compute: +1/tick if a rack can be powered; cap 100.
4. People regrow hourly by 5% of the gap to cap (rounded up), paused while Energy = 0.
5. Corruption decays 1/hour; cap 100. (Raisers come with M2.)
6. Raid: each tick has a 1/360 chance (one RNG draw per tick, always). At most 3 per day. Loot = 20% of energy, capped at 60. Never a full wipe.

## To add for M1
7. Defense posture (Turtle / Dark / Evacuate) and 3-5 garrison slots reduce loot or casualties.
8. Offline: if no defense was set at logout, raid strength x1.5 (activity penalty, tune).
9. Mercy window: 6h after a devastating loss, no new attacks; ends if the player launches offense.
10. Loss ledger: each raid emits itemized events.

## Correction found while building
Doc 10 listed energy +6/min, upkeep -4/min, racks 3/min = **-1/min**. The base blacked out after about 3 hours. Generation raised to 8. Logged in doc 10 section 11 and guarded by `Tier1Defaults_DoNotBlackOutOverAWeek`.

## Acceptance criteria
- [x] Same seed gives same hash; different seeds diverge
- [x] Chunked run equals single run
- [x] Resources within caps; raid loot capped; max 3 raids per day
- [ ] Defense setup changes outcomes in a way the loss ledger can explain
- [ ] Mercy window implemented and tested
- [ ] 100 seeds x 30 days: no soft-lock, no full wipe

## Open questions
- Should raid cadence scale with `RaidsToday` already taken, or stay flat until the cap?
- How should unmanned-unit load feed corruption (M2)?
