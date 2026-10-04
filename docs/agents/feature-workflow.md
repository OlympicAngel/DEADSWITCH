# Playbook: Feature workflow

1. **Locate the source of truth**: doc 10, then the owning design/system doc (02-11) and accepted ADRs. Read the applicable spec and quote the rule being implemented.
2. **Define the player value**: identify the player goal and ranked pillar served, the meaningful choice, the satisfying result, and how feedback makes the result understandable. Use [the engagement playbook](./engagement.md).
3. **Check systemic impact and fairness**: name relevant system interactions; trace costs, counterplay, failure, recovery, and offline behavior where applicable. Do not force irrelevant coupling.
4. **Protect decision status and scope**: keep locked decisions, proposals, open questions, and *(tune)* values distinct. Ask before material changes to canon, scope, balance, rewards, loss, timers, monetization, notifications, accessibility, or intended player emotion.
5. **Write a spec** (`docs/specs/TEMPLATE.md`) only for a new mechanic or system: goal, non-goals, rules, acceptance criteria, and tests. Keep it under one page.
6. **Minimal tests** in `Deadswitch.Sim.Tests`: only rules that could silently break; extend existing tests first. Verify the rest with a quick CLI run or preview screenshot.
7. **Implement rules in the sim**, then wire Unity UI on top. The UI never owns game rules.
8. **Run the gate**: `pwsh tools/check.ps1` (or the equivalent commands documented in `README.md`).
9. **Update docs** in place only where behavior or canon changed (owning spec, doc 10 corrections log). HANDOFF only if work is half-done.
10. **Open the PR** using the template. One concern per PR.
