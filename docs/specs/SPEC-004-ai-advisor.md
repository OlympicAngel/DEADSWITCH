# SPEC-004: The AI advisor (voice, dials, delegation, the first lie)

- Status: In progress (F-011)
- Pillar: AI relationship
- Touches: defense (raid gates, autopilot posture), economy (delegated build queue), corruption (glitch voice, estimate error), offline play (autopilot while away), audit (F-014 reads the lie record)
- Source rules: doc 03 s1-2 and s5, doc 10 s1.4 and s7, `docs/narrative/ADVISOR_VOICE.md`

## Goal
The AI is a voice the handler lives with: short terminal lines that react to what just happened, colder or bolder depending on how the handler has treated it and how much it is trusted to run things. Handing it the build queue or the night watch is convenient and makes it bolder. Early on it tells a small, checkable lie about where a raid will hit, so the theme "do not fully trust it" is planted by play, not by text.

Player value: the base talks back (feedback for every major event), delegation is a real time-saver with a felt cost, and the first lie is a discovery moment ("it said north").

## Non-goals
Coldness triggers (forced labor, purges, sacrifices arrive with those choices, BACKLOG F-021), the Audit/Core Profile readout (F-014), scout cross-checks (operations), battle-report Verify (F-013), voice audio, the hidden project's influence on lies (F-014).

## Rules (numbers *(tune)*, in the balance file `[ai]`)
1. **Dials** (hidden, doc 10 s1.4): `Coldness` and `Boldness` in milli-units `0..100_000`, saved and hashed. Coldness has no Tier 1 trigger yet (it shapes tone only once it rises).
2. **Boldness from reliance** (doc 03 s2): every game hour, +`boldness_per_hour_delegated` at Delegated, +`boldness_per_hour_autopilot` at Autopilot, -`boldness_decay_per_hour_manual` at Manual. Clamped.
3. **Delegated routines:** at Delegated or Autopilot the AI runs the build queue: every `plan_every_minutes`, if a queue slot is free and no raid is incoming, it starts one build or upgrade with a fixed, readable policy (power first when net energy is under `plan_energy_margin`, then missing storage, defense, people, then the lowest-level facility). It never overrides a handler-cancelled job within the same hour. Each action emits `AiActed`.
4. **Offline autopilot:** at Autopilot, when a raid warning arrives while the handler is away, the AI sets the defense from **its own estimate**: estimate > defense x `autopilot_turtle_pct`% -> Turtle with the full garrison; estimate > defense x `autopilot_evacuate_pct`% -> Evacuate. Because the estimate carries corruption error, it can be wrong. Emits `AiActed`.
5. **Raid gates:** every raid approaches through one of four gates (North ridge, South gate, East, West), chosen at spawn from the tick's otherwise unused draw (no extra RNG draws: the four-draws-per-tick rule holds). The warning reports a gate (`RaidVector`); resolution reveals the true gate (`RaidContact`), which is the cross-checkable trace (lie rule 2).
6. **The first lie** (doc 10 s7.4): the first raid warning of a run reports the opposite gate (true South -> reported North, and so on). Later warnings lie with chance `lie_chance_permille_at_full_boldness` x Boldness / 100%, decided by a deterministic hash of the raid id and tick. A lie is about information only (rule 7.1): gates do not change combat in Tier 1. Every lie emits `AdvisorLied` (for the Audit; never shown directly).
7. **Lines** live in the host package (`Resources/AdvisorLines.txt`): `id | trigger | tone | text`, tone `neutral | warm | cold | bold`, placeholders `{gate}`, `{est}`, `{min}`, `{kind}`, `{lost}`, `{band}`. The advisor picks the tone from the dials (cold at Coldness >= 50%, bold at Boldness >= 50%, otherwise warm/neutral), rotates lines per trigger without repeating the last one, and never invents numbers that the event does not carry. Raid lines interrupt; ambient lines wait for `idle seconds` of quiet. After a lie is exposed by `RaidContact`, the next line is a deflection ("Prediction variance. Noted.") and it never admits intent.
8. **Rules version:** AI actions are derived inside the tick, so a command log replays only under the rules that recorded it; saves carry their state, not a replay (ADR-0009). v1 saves load with dials at 0 and a truthful gate for an incoming raid.
9. **Glitch:** the line text glitches by corruption band x effect intensity (existing ticker); numbers and gate names stay readable.

## Acceptance criteria
- [ ] 50 lines covering boot, raids (warning, repelled, breached, missed, lockdown, mercy), power (low, shed, blackout), construction, corruption bands, OVERRIDE, delegation, autopilot actions, the lie and its deflection, idle
- [ ] Delegated: a 7-day run with no handler input never blacks out and grows (guard test)
- [ ] First raid warning reports the opposite of the true gate; the contact event reveals it
- [ ] Determinism, chunking and save/load hold with the new state

## Tests
`AdvisorTests` (sim): first-lie gate, delegated 7-day guard. Existing determinism/chunking/save tests cover the new fields. `AdvisorLinesTests` (host): the shipped file parses and every trigger has a neutral line.

## Open questions
- Should Boldness also rise when the handler follows the AI's defense recommendation verbatim?
