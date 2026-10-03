# SPEC-006: Battle report

- Status: In progress (F-013)
- Pillar: Defense & offline, AI relationship
- Touches: raids (SPEC-001 events and ledger), AI lies (SPEC-004 gate lie, report edits), compute economy (Verify cost), 3D base (panels are rendered stills of the compound, SPEC-003)
- Source rules: doc 10 s4 "Battle report format" + s7, ADR-0003 (the report is a view over the true log), ADR-0007 (rendered stills), doc 07 s7

## Goal
After every raid the handler gets a short graphic-novel report: four panels rendered from the base itself (approach, contact, outcome, aftermath), the AI's one-line summary, and the loss ledger that lists every loss. The summary is the AI's voice and can be wrong; **Verify** spends compute to compare it with the raw sensor log. Player value: "what happened and why" in ten seconds, and a real, optional tool to catch the AI.

## Non-goals
Animated replays, siege/virus/purge reports (F-018), scout data from operations (the raw sensor log stands in for Tier 1), report history beyond the last 10 raids.

## Rules
1. **Source:** a report is a view built from the event log for one raid id: warning (estimate, minutes), reported gate, contact gate, resolution (outcome, true strength, defense), loss lines, mercy. The log is never edited (ADR-0003).
2. **Ledger:** every `LossLine` is listed (energy, compute, people) with its amount; a repelled raid lists "NO LOSSES". The ledger is store counts, never edited.
3. **Summary (the AI's line):** names the gate, the outcome and a loss total. It repeats the gate the AI reported (so a gate lie stays a lie in the summary) and, when the AI **edits** the report, understates the energy loss.
4. **Report edits** (doc 10 s7): at resolution of a breach, while corruption is Unstable or worse, the AI edits the summary with chance `edit_chance_pct` *(tune)*, decided by a hash (no RNG draw). The shown energy loss is the true loss x `edit_shown_pct`% *(tune)*. Recorded as `AdvisorLied(ReportEdit, raid, true, shown)`; the trace is the ledger and Verify.
5. **Verify:** command `VerifyReport(raid)` costs `verify_compute_cost` compute *(tune)*, once per raid (only the last 10 raids). It emits `ReportVerified(raid, flags)`: bit 0 gate lie found, bit 1 summary edit found. The report then shows the sensor log (true gate, true strength vs the AI's estimate) and marks each discrepancy. The advisor answers ("Clerical error." / "Prediction variance. Noted." / "Confirmed. The record stands.").
6. **Panels:** four stills of the compound from the true gate's side (approach from outside the wall, contact at the gate, outcome at the defenders or the breach, aftermath over the yard), rendered in-engine with a graphic-novel grade (ink edges, posterized light, halftone shadows), each with a short caption. Raiders are dark silhouettes; nothing gory.
7. **Access:** a REPORT chip appears on the HUD after a raid resolves; the last report is reachable from OPS.

## Acceptance criteria
- [ ] Report built from the log for every outcome (repelled, breached, missed, lockdown)
- [ ] Edit lie recorded and caught by Verify; Verify cost and once-only enforced
- [ ] Panels preview (headless) reads as graphic-novel stills of our base, phone size

## Tests
`ReportTests`: Verify costs compute once and flags a planted gate lie; report built for a breach lists every loss line.
