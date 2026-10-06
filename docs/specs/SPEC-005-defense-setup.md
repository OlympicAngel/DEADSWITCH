# SPEC-005: Defense setup (OPS screen)

- Status: Done (F-012); checked in the Editor 2026-10-06 (no contact and raid states, posture, garrison, confidence, Set & Go, lockdown gating, delegation)
- Pillar: Defense & offline (feeds AI relationship)
- Touches: raids (SPEC-001 posture, garrison, OVERRIDE lockdown), crew (garrison takes people off duty), AI (estimate, Confidence, delegation ladder of SPEC-004)
- Source rules: doc 10 s4 "Defense setup UI", doc 03 s2, SPEC-001 rules 5-6, SPEC-004 rules 3-4

## Goal
One screen where the handler reads the threat and prepares: pick a posture, post defenders, see the AI's Confidence, or tap **Set & Go** to take the AI's recommendation. It is also where the handler decides how much the AI runs (delegation). Player value: a fast, legible pre-raid decision with a visible trade-off (defenders do not work; Dark costs power; Evacuate gives up loot) and a recommendation that is convenient but only as good as the AI's estimate.

## Non-goals
Per-slot crew roles and dragging specific people (chips are counts in Tier 1), siege/virus/purge signatures (F-018), battle report (F-013).

## Rules
1. **Threat card:** while a raid is incoming: RAID signature (amber, diamond shape + label), countdown, the AI's estimate and reported gate, both tagged AI ESTIMATE. Otherwise "No contact" (the screen still works: posture and garrison persist).
2. **Posture cards:** None, Turtle, Dark, Evacuate with their effect read from the balance (`+turtle_defense_pct% DEF`, `dark_miss_pct% MISS, -dark_upkeep_per_hour/h`, `NO CASUALTIES, LOOT x evacuate_loot_pct%`). Tap = `SetPosture`.
3. **Garrison:** `garrison_slots` sockets; tapping socket *n* posts *n* defenders (tapping the last filled one removes it). Shows defense per defender and how many crew stay on duty. Tap = `SetGarrison`.
4. **Readout:** current DEF (`Defense.Rating`) vs AI ESTIMATE and the AI **Confidence** `= clamp((DEF x 100 / estimate - 40) x 100 / 120, 0, 100)` rounded to 5% *(tune)*: LOW < 35, FAIR < 70, HIGH. It uses the AI's estimate, so it can be wrong when corrupted.
5. **Set & Go:** applies the AI's recommendation (the same rule as autopilot, SPEC-004 rule 4: estimate <= DEF x `autopilot_turtle_pct`% -> no change, <= x `autopilot_evacuate_pct`% -> Turtle + full garrison, else Evacuate). Shown before tapping ("AI RECOMMENDS: TURTLE, 5").
6. **Lockdown:** OVERRIDE "Emergency lockdown" with charges and cooldown, enabled only during a raid; refusals use the existing reasons.
7. **Delegation:** Manual / Routines / Autopilot segmented control with one line each; tap = `SetDelegation`.

## Acceptance criteria
- [x] Every control is a sim command; rejections are explained
- [x] Recommendation and Confidence come from one sim helper shared with autopilot
- [x] UI preview of the OPS screen at phone size: no clipping, 44 px targets, shape + label on the signature

## Tests
Existing defense/save/determinism tests cover the commands; the shared helper is covered by the autopilot path in the chunking and save tests.
