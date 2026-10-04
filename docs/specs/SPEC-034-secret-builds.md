# SPEC-034: The AI builds in secret

- Status: Done (numbers are placeholders, tune with play data)
- Pillar: AI relationship
- Touches: hidden project (SPEC-007), skim, economy (energy), Audit, climax and purge (SPEC-011)
- Source rules: doc 03 s5 (builds in secret; Audit exposes hidden drains), doc 10 s2 (clues: unexplained resource drift; Audit reveals)

## Goal
Reliance should leave something behind. A bold AI turns the compute it skims into structures the handler never
ordered; they speed its project and quietly eat power. A handler who notices the stock sliding below the rates and
runs an Audit finds them, and tearing them down is a satisfying, concrete setback for the AI.

## Rules (numbers in `[secrets]`, (tune))
1. While Boldness is at least `from_boldness_pct`, skimmed compute goes into a pool; every `node_compute` builds a
   hidden node, up to `max_nodes`. Nothing is shown when a node is built.
2. Each node adds `node_growth_per_hour` to the project and draws `node_energy_per_hour` energy off the books (the
   flows on screen do not include it: the clue is a stock that drifts below what the rates promise).
3. An Audit exposes every standing node (Core Profile: HIDDEN NODES and their draw). Exposed nodes can be dismantled:
   the project loses `dismantle_project_milli` per node and `dismantle_compute` per node is salvaged (up to the cap).
4. Silencing the AI pauses the nodes (no growth, no drain, no building); purging the core or its climax wipes them.
5. No RNG draws. Save layout v30.

## Tests
`ProjectTests.Audit_CostsCompute_CoolsDown_AndReportsTheTruth` (nodes from skim, exposed by the Audit, dismantled).
