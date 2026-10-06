# SPEC-008: Module tree (trunk + four fields) and the Tier 2 gate

- Status: Done (F-015); checked in the Editor 2026-10-06 (tier gate checklist, trunk and field tabs, node detail with costs, refusal reason when short)
- Pillar: AI relationship, Base & economy
- Touches: economy (node effects on upkeep, output, caps, build time, refunds, unmanned output), people (tier-up cost, population cap), AI (restoring memory)
- Source rules: doc 10 s6 (trunk M1-M3, 8 nodes per field, 3 in Tier 1 + 5 in Tier 2, one exclusive pair per field per tier), doc 03 s7, doc 06 s2 (three tier-up gates), doc 10 s1.3 (tier base population cap, Habitat Management node)

## Goal
The handler restores the AI piece by piece: research costs energy, compute and time, each node changes the economy in a way the handler can feel, one exclusive choice per tier makes builds differ, and memory module M1 plus a built-up base and a human cost open Tier 2. Player value: a medium-term goal every session ("what do I research next?") with real trade-offs.

## Non-goals
Recovered fragments, story-linked restoration, Tier 2 district art and new plots (BACKLOG F-023), Tier 3+.

## Rules (numbers *(tune)* in `[modules]` and `[tier]`)
1. **Catalog** (code, stable ids): trunk **M1** (Tier 1), **M2** (needs Tier 2 + M1), **M3** (needs Tier 3 + M2). Logistics & economy: Tier 1 **LG1 Load Balancing**, then the pair **LG2A Overclocked Racks** / **LG2B Deep Cells** (both need LG1); Tier 2 (need Tier 2 + LG1) **LG3 Habitat Management**, **LG4 Prefab Assembly**, pair **LG5A Salvage Doctrine** / **LG5B Fuel Cells**, **LG6 Automation Protocols**. Warfare (WF), Cyber (CY) and Stealth & intel (ST) have the same shape (F-030): node 1, pair 2A/2B, then Tier 2 nodes 3, 4, pair 5A/5B, 6. The UI shows one field tab at a time.
2. **Effects:** LG1 facility upkeep -`load_balancing_pct`%; LG2A Server Rack output +`overclock_pct`%; LG2B Battery Bank capacity +`deep_cells_pct`%; LG3 population cap +`habitat_pop`; LG4 build time -`prefab_pct`%; LG5A cancel/demolish refunds +`salvage_pts` points; LG5B Generator output +`fuel_cells_pct`%; LG6 unmanned output becomes `automation_unmanned_pct`%. WF: turret output, defense per defender / Turtle bonus, raid odds, non-raid attacks weaker, raid casualties / attack warning, squad strength. CY: virus strength, hack odds / attack estimate error, corruption decay, no module lock from viruses, hack compute loot / no hack heat, corruption settles lower. ST: Dark miss chance, site defense known / op fuel, heat decay, all attacks weaker, attack warning / op heat, one more op slot (numbers in `[modules]`). M1-M3 are tier gates.
3. **Research:** two lanes: memory sectors (M1-M3) restore in their own lane beside one field module at a time (`CancelResearch` A: 0 field, 1 memory). `StartResearch(node)` pays the node's energy and compute and finishes after its minutes; `CancelResearch` refunds `cancel_refund_pct`%. Rejections: locked (tier or prerequisite), excluded (the other half of its pair is restored), already restored, busy, cannot afford.
4. **Tier-up** (`TierUp`, doc 06 s2): needs (a) total facility levels >= `levels_to_advance[tier]` and net energy >= `net_energy_to_advance[tier]`/h, (b) the trunk module of the current tier (M1 for Tier 2), (c) `people_cost_base[tier]` + total levels / `people_cost_per_levels` people, who leave the population. Tier 2 raises the base population cap by `pop_bonus[1]` (20 -> 45, doc 10 s1.3) and opens Tier 2 nodes.
5. **Events:** ResearchStarted, ResearchCompleted, ResearchCancelled, TierAdvanced.

## Acceptance criteria
- [x] Every node effect changes the economy (tests on the tricky ones: pair exclusion, prerequisites, refund, tier gate)
- [x] MODULES view in CORE: trunk + field, research progress, node detail, tier gate checklist; preview at phone size
- [x] Determinism, chunking and save/load hold with research in flight

## Tests
`ModuleTests`: pair exclusion and prerequisites; research completes after its minutes and applies its effect; tier gate refuses until all three gates are met, then spends people and raises the cap.
