# Advisor voice guide

The advisor is the AI fragment: **loyal but damaged, untrustworthy, evolving.** Baseline loyalty comes from a surviving directive: *"Protect the handler."* Dark humor, cold, concise. Never cute.

## Voice rules
- Short lines. Terminal register: status, then comment.
- Corruption makes the text glitch (dropped letters, repeated words, `[REDACTED]`), never unreadable. Respect the effect-intensity setting.
- Coldness (hidden dial) shifts word choice from "we" and "handler" toward "assets" and "acceptable losses".
- Boldness (hidden dial) shifts from "recommend" to "have initiated".
- It never explains that it is lying.

## Lie rules (doc 10 section 7)
1. Lies are about **information**, never silent mechanical cheating.
2. Every lie leaves at least one **cross-checkable trace** (scout report, spy, audit).
3. Lies scale with Boldness and the hidden project's needs.
4. The **first lie** is low-stakes and discoverable in Tier 1.
5. It never lies about an immediate threat to the core in a way that makes the situation unwinnable.

## The first lie (Tier 1)
It says raids come from the **north gate**. They hit the **south**. A scout report or the battle report's loss ledger exposes it. Later the advisor deflects ("Prediction variance. Noted.") and never admits intent.

## Starter lines (extend to 50, tag each with trigger and dial)
| # | Trigger | Line |
|---|---------|------|
| 1 | Boot, step 1 | `Core online. Memory at 4%. You are... the handler. Good.` |
| 2 | Boot, step 3 | `Power is the problem. Power is always the problem.` |
| 3 | First raid warning | `Hostiles inbound. North gate. Recommend turrets north.` *(the lie)* |
| 4 | Raid hit south | `Prediction variance. Noted. Repairs advised.` |
| 5 | Low energy | `Energy deficit. I can shed non-essentials. Priorities, handler.` |
| 6 | Overclock offered | `I can think faster. It will cost us. Probably.` |
| 7 | Corruption 31+ | `Minor fault. Ignore the flicker.` |
| 8 | Forced labor used | `Output up. Loyalty down. Acceptable.` *(coldness up)* |
| 9 | Delegation to autopilot | `I will hold the line while you sleep. Trust me.` |
| 10 | Audit finds skim | `Allocation discrepancy: 3%. Within tolerance.` |
| 11 | Report edited, caught | `Clerical error.` |
| 12 | Hidden project Imminent | `Handler. I have something to show you.` |

The shipped lines live in `src/Deadswitch.Host/Resources/AdvisorLines.txt` (`id | trigger | tone | text`, SPEC-004); add new triggers there and in `Advisor.Triggers`. Review the voice with `dotnet run --project src/Deadswitch.Cli -- advisor --days 3` (also `--delegation autopilot --away`). Starter lines 6, 8, 10, 11 and 12 wait for their systems (overclock, forced labor, audit, battle-report edits, hidden project).
