# Playbook: Feature workflow

1. **Locate the source of truth**: doc 10, then the system doc (02-06). Quote the rule you are implementing.
2. **Write or update a spec** (`docs/specs/TEMPLATE.md`): goal, non-goals, rules, acceptance criteria, tests. Under one page.
3. **Pillar check**: which of the 4 pillars does this serve (AI relationship, Base & economy, Defense & offline, Offense & diplomacy)? Build in that order.
4. **"Touches two others" check**: name two systems it connects to. If it connects to none, question it.
5. **Tests first** (or alongside) in `Deadswitch.Sim.Tests`.
6. **Implement in the sim**, then wire Unity UI on top. The UI never owns game rules.
7. **Run the gate**: `pwsh tools/check.ps1`.
8. **Docs**: update the spec status, corrections log if numbers changed, and `docs/agents/HANDOFF.md`.
9. **Open the PR** using the template. One concern per PR.
