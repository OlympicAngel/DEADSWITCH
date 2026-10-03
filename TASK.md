# TASK: F-011 AI advisor

- Status: In progress
- Started: 2026-10-03   Branch: claude/magical-ritchie-bx4xbb
- Pillar / milestone: AI relationship / M2
- Spec: docs/specs/SPEC-004-ai-advisor.md
- Sources: doc 03 s1-2, doc 10 s1.4 + s7, docs/narrative/ADVISOR_VOICE.md

## Goal
The AI talks back: short lines for every major event, tone shaped by hidden Coldness/Boldness dials, a delegation ladder that saves the handler time and makes the AI bolder, and a first, checkable lie about where a raid hits.

## Steps
- [x] 1. Sim: `[ai]` config, dials in GameState (versioned visitor, save v2), Boldness from delegation
- [x] 2. Sim: raid gates (RaidVector / RaidContact), first lie + Boldness-scaled lies (AdvisorLied), tests
- [x] 3. Sim: delegated build planner + offline autopilot defense (AiActed), 7-day delegated guard test
- [x] 4. Host: advisor line engine + `AdvisorLines.txt` (50 lines) + parse test
- [x] 5. Unity: HUD wired to the advisor (events -> lines, tone, glitch), raid row shows the reported gate; resolution lines name the contact gate
- [ ] 6. CLI `advisor` transcript for a scripted run; review the voice; docs (ADVISOR_VOICE, HANDOFF, BACKLOG)

## Notes
- F-010 closed 2026-10-03; its Unity play-mode check is listed in HANDOFF (owner's machine).
- Gates use the spawn tick's unused miss draw: RNG stays at four draws per tick.

## Blocked / questions
- none
