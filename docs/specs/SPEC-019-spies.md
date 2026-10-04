# SPEC-019: Spies (double agents, framing, cross-checks)

- Status: Done (numbers are placeholders, tune at F-099)
- Pillar: Offense & diplomacy
- Touches: raids (warning, estimate), world map (site estimates, scouts, hacks), heat, people
- Source rules: doc 05 s3 (espionage & deception) + s5, doc 10 s5 (spy double-agent rules), doc 10 lie rule 2

## Goal
Information you can buy but not fully trust: an agent in a faction camp gives early, exact warnings and true site
reports, unless the agent was turned, and only a cross-check tells the difference.

## Rules (all numbers in `[intel]`, (tune))
1. `PlantSpy(faction)`: one agent per camp; costs `spy_energy` and `spy_people` (away; they hold their place in the population). A hidden loyalty roll at planting: double-agent chance = `double_base_pct` (10%) + heat% x `double_heat_permille`/1000 + Coldness% x `double_cold_permille`/1000.
2. **Loyal:** attacks from that faction come `spy_warning_minutes` earlier with an exact estimate; its sites show true defense.
3. **Double agent:** that faction's attacks are estimated at `double_estimate_pct`%, its sites at `double_site_defense_pct`% (lures into bad raids). The UI never shows loyalty.
4. **Cross-check:** a successful scout or hack at that faction's site exposes a double agent (lost). Loyal agents are unaffected.
5. `FrameFaction(faction)`: a loyal agent moves up to `frame_heat` off its faction (half lands on a rival); `frame_catch_pct`% chance the agent is caught. A double agent's frame backfires (+`frame_heat`) and exposes it.
6. While a faction hunts the Hub (Hunted+), `caught_pct_per_hour_hunted`% per hour the agent is caught (+`caught_heat`). `RecallSpy` brings the agent home.
7. Hash decisions, no RNG draws. Save layout v12, event schema v12.

## Tests
`ThreatTests.Operation_ComesHome_WithHeatOnTheOwner_AndEndsMercy` (double agent lowers estimates; a scout exposes it).
