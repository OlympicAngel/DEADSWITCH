# SPEC-037: Recovered fragments unlock key modules

- Status: Done (numbers are placeholders, tune with play data)
- Pillar: AI relationship (touches Offense & world)
- Touches: module tree (SPEC-008), operations (F-019), data center and ruins sites
- Source rules: doc 03 s7 (recovered fragments: rare data from dead data centers and bunkers unlocks key modules)

## Goal
The strongest module in each field is not bought with time alone: the AI needs a piece of its old self back. That
gives the map's data centers a reason beyond loot and makes a won hack on the Cathedral Array feel like progress.

## Rules (numbers in `[modules]`, (tune))
1. Each field's capstone (LG6, WF6, CY6, ST6) needs one **data fragment** to start; starting uses it, cancelling
   returns it. The Hub holds up to `fragment_max`.
2. A won raid or hack on a dead data center recovers a fragment with chance `fragment_pct_data_center`; a won raid on
   ruins (old bunkers) with `fragment_pct_ruins`. Hash picks, no RNG draws.
3. The map sheet states the chance; MODULES says when a fragment is needed and how many are held.
4. Save layout v34 (`DataFragments`; the story's memory fragments are a separate bitmask).

## Tests
`ModuleTests.KeyModule_NeedsARecoveredFragment_AndUsesItUp`.
