# SPEC-030: The AI acts without orders

- Status: Done (numbers are placeholders, tune with play data)
- Pillar: AI relationship
- Touches: delegation and boldness (SPEC-004), world ops, heat, map (recall), advisor
- Source rules: doc 03 s5 (acts without orders: launches raids; the handler can cancel some of the AI's own actions)

## Goal
Delegation is a trade. A bold AI starts making calls the handler did not make, and the handler should notice it,
judge it and sometimes overrule it.

## Rules (all numbers in `[ai]`, (tune))
1. Under Delegated (never Autopilot, never while the handler is away), with Boldness at least `initiative_boldness`, the AI has an `initiative_pct_per_hour` chance each hour to send `initiative_squad` people on a raid. This never happens during an attack, a blackout, mercy or the opening protection, or while another of its own ops is out.
2. It picks the site with the lowest estimated defense whose odds, by its own estimate, are at least `initiative_min_odds`. It skips sites under ceasefire or alliance and sites on cooldown. Its estimate can be wrong (corruption, double agents).
3. Every such op is announced (advisor, urgent) and marked AI on the map with a RECALL button.
4. `RecallOp(id)`, only for AI-launched ops: the squad returns at once, the fuel is spent, and no heat is added. The mercy rule still applies to the launch, so the AI never launches while the handler is protected.
5. No RNG draws. Save layout v26 (Operation.ByAi), event schema v26.

## Tests
`ThreatTests.BoldAi_RaidsWithoutOrders_AndTheHandlerCanRecallIt`. Initiative runs only under Delegated while the handler is present (never Autopilot or offline catch-up), and it keeps 12 hours of reactor fuel in reserve; a recalled target goes on cooldown.
