# SPEC-012: Ruthless choices and loyalty

- Status: Done (F-021); Editor play check pending
- Pillar: AI relationship (Coldness), Base & economy (people)
- Touches: economy (output), corruption (neural cleansing), people (deaths, regrowth), AI tone (Coldness), events (rogue operator)
- Source rules: doc 02 s5 (people as a strategic sacrifice), doc 03 s1 + s3 (Coldness, human cost), doc 10 s1.3 (loyalty: Steady / Strained / Mutinous)

## Goal
People are the one resource the handler should feel bad about spending. Three choices trade lives or trust for power: a **forced labor surge** (more output now), **neural cleansing** (people scrub corruption), and a **crackdown** (fear restores order). Each makes the AI colder and the people less loyal; low loyalty cuts output and can turn an operator rogue. Player value: real, tempting shortcuts with a visible human and personal cost; a colder AI is the long-term price.

## Non-goals
Individual named survivors, morale events beyond the rogue operator, faction recruitment.

## Rules (numbers *(tune)* in `[people_choices]`)
1. **Loyalty** `0..100_000`, hidden number, visible status: Steady >= `strained_below`, Strained, Mutinous < `mutinous_below` (percent). Recovers `recover_per_hour` while no surge runs. Strained cuts facility output by `strained_output_pts` points, Mutinous by `mutinous_output_pts`.
2. **Forced labor surge** (`ForcedLabor`): `surge_deaths` people die at once; for `surge_hours` facility output +`surge_output_pts` points; loyalty -`surge_loyalty`; Coldness +`surge_coldness`; cooldown `surge_cooldown_hours`. Refused when it would leave fewer than `min_people`.
3. **Neural cleansing** (`NeuralCleanse(n)`, 1..`cleanse_max`): n people are used up; corruption -n x `cleanse_per_person`; loyalty -n x `cleanse_loyalty`; Coldness +n x `cleanse_coldness`.
4. **Crackdown** (`Crackdown`, only while Strained or Mutinous): `crackdown_people` people are removed; loyalty +`crackdown_loyalty`; Coldness +`crackdown_coldness`.
5. **Rogue operator:** once a day (game noon) while Mutinous, with chance `rogue_chance_pct` (hash, no RNG draw) one person leaves and takes `rogue_energy` energy. Never below `min_people`.
6. Coldness rises only through these choices (doc 03 s1); every choice emits an event and the AI answers in its voice.

## Acceptance criteria
- [x] Each choice applies its costs and effects; refusals explained; never below `min_people`
- [x] Loyalty bands cut output; recovery holds; rogue operator only while Mutinous
- [x] WORKFORCE sheet from the HUD people cell: population, crew, garrison, loyalty status, the three choices with their costs

## Tests
`PeopleChoiceTests`: surge (deaths, output, cooldown, Coldness, loyalty); cleanse (corruption, people); crackdown gated by loyalty; min-people floor.
