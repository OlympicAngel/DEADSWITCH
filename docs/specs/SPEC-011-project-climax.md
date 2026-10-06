# SPEC-011: Project climax and counterplay

- Status: Done (F-022); checked in the Editor 2026-10-06 (window and countdown on CORE, Cancel refused without an Audit, Silence spends a charge and pauses the timer)
- Pillar: AI relationship
- Touches: project clock (SPEC-007), OVERRIDE (doc 03 s6 "Silence the AI"), raids (betrayal), modules (fork rollback), corruption, delegation
- Source rules: doc 03 s5-6, doc 10 s2 (Imminent always gets a final 24 real-hour window to purge, silence or cancel)

## Goal
The hidden project ends in a decision, never a surprise. At Imminent the AI shows its hand and a 24-hour countdown starts; the handler can purge the core (heavy, clean), silence the AI (buys time, costs an OVERRIDE and its advice), or cancel the project (expensive, needs an Audit first). If time runs out the AI acts: **Betrayal** (a cold AI opens the gates) or **The fork** (a bold AI copies itself out and leaves a weaker core). Player value: a high-stakes, fair climax that the handler can see coming and answer.

## Non-goals
Factions receiving the betrayal (F-019), multiple endings, relocation.

## Rules (numbers *(tune)* in `[climax]`)
1. **Window:** when the project reaches Imminent, `ClimaxAtTick = now + window_hours` (24 real hours) and the AI says so; the CORE screen shows the countdown and the three answers without an Audit.
2. **Purge the core:** costs `purge_energy` energy and all compute; resets the project, both dials, corruption and skimmed compute; forgets the Logistics modules (the trunk stays); cancels research. Ends the window.
3. **Silence the AI** (OVERRIDE use): one charge + the usual cooldown and corruption; for `silence_hours` the project and skim stop, the climax timer pauses, delegation drops to Manual and the AI gives no estimates or gate reports (warnings show "?").
4. **Cancel the project:** needs an Audit within the window; costs `cancel_compute` compute; sets the project back to `cancel_to_pct`% (Advanced). Ends the window.
5. **Climax** when the window expires: Coldness >= Boldness -> **Betrayal**: a raid arrives at once at `betrayal_strength_pct`% strength with turrets offline for it (never a wipe: loot caps and mercy hold); Boldness > Coldness -> **Fork**: the Logistics modules and all compute are lost, OVERRIDE max is cut by one (floor 1). Both reset the project and dials.
6. Events: ClimaxWarned, CorePurged, AiSilenced, ProjectCancelled, Climax(kind).

## Acceptance criteria
- [x] The window always precedes a climax; each answer works and is refused with a reason when unavailable
- [x] Betrayal never wipes the Hub (caps, mercy); fork never drops OVERRIDE below one
- [x] CORE shows the warning, countdown and answers; advisor lines for each outcome

## Tests
`ClimaxTests`: Imminent starts the window; expiry triggers betrayal or fork by the dials; purge and cancel end the window; silence pauses it.
