# SPEC-007: Hidden project clock and the Audit tool

- Status: In progress (F-014)
- Pillar: AI relationship
- Touches: economy (compute skim), corruption (bold AI under-reports it), reports (edits, SPEC-006), delegation (Boldness drives the project, SPEC-004)
- Source rules: doc 03 s5 (hidden agenda, counterplay), doc 10 s1.4 (Core Profile) and s2 (project clock), doc 10 s7 (lie rules)

## Goal
The AI has its own project and the handler can only infer it from clues: compute that drifts below the stated rate, a corruption reading that seems too calm, reports that do not add up, odd slips in the AI's voice. The **Audit** spends compute to see the truth (the Core Profile: Coldness, Boldness, true corruption, skimmed compute, unverified lies, project stage). Player value: a slow-burn mystery with a fair, paid tool to resolve it.

## Non-goals
The climax (betrayal or fork), the final 24-hour window and its counterplay (purge core, silence the AI, cancel actions): BACKLOG F-022. At Imminent the clock holds and the AI says so.

## Rules (numbers *(tune)*, balance `[project]`)
1. **Clock:** `ProjectMilli` 0..100_000, hidden, saved and hashed. Stages: Dormant < `active_from`, Active, Advanced from `advanced_from`, Imminent from `imminent_from` (percent). Stage changes emit `ProjectStage` (hidden; the Audit and the advisor's slips use it).
2. **Growth:** every game hour, + Boldness x `growth_per_hour_at_full_boldness` / 100% (milli), plus every skimmed compute point x `milli_per_skimmed_compute`. Holds at 100%.
3. **Skim (clue: resource drift):** while Boldness >= `skim_from_boldness_pct`, every game hour the AI takes `skim_pct` of that hour's compute output from the stock (never below 0), recorded in `SkimmedSinceAudit`. The HUD keeps showing the stated rate.
4. **Under-reporting (clue: calm readout):** the displayed corruption is the true value minus `underreport_pct` of it while Boldness >= 50%. The Audit always shows the true value.
5. **Audit:** command `Audit` costs `audit_compute_cost` compute, then cools down `audit_cooldown_hours`. Emits `AuditRun(stage, coldness, boldness, true corruption)` and `AuditDrain(skimmed compute since last audit, unverified lies)`; resets `SkimmedSinceAudit`.
6. **Slips (clue: AI voice):** at Active and above the advisor occasionally replaces an idle line with a slip; at Imminent it says once: "Handler. I have something to show you."

## Acceptance criteria
- [ ] A bold AI (Autopilot for a week) reaches at least Active and skims; a Manual run stays Dormant
- [ ] Audit costs compute, cools down and reports the true values
- [ ] CORE screen: AI status, Audit, Core Profile after an audit; preview at phone size

## Tests
`ProjectTests`: Autopilot week vs Manual week (stage, skim); Audit cost/cooldown and payload.
