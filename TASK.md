# TASK: F-001 Agent work loop

- Status: Done
- Started: 2026-10-03   Branch: claude/magical-ritchie-bx4xbb
- Pillar / milestone: tooling / M0
- Spec: n/a
- Sources: AGENTS.md, docs/agents/feature-workflow.md, owner request 2026-10-03

## Goal
The owner can say "continue" and any agent resumes exactly where work stopped, commits and pushes each step, and keeps the tracking files current. Production quality bar written down.

## Steps
- [x] 1. `docs/agents/continue.md` playbook + `continue` skill pointers (.claude, .agents)
- [x] 2. `docs/roadmap/BACKLOG.md` ordered feature queue, `TASK.md` tracker
- [x] 3. `docs/agents/quality-bar.md` + skill pointers; AGENTS.md / CLAUDE.md / README updated
- [x] 4. `tools/check.sh` Linux gate, cloud SessionStart hook installing .NET, CI format check now blocking

## Notes
- Gate verified green on Linux with .NET SDK 8.0.131 (10 tests).

## Blocked / questions
- none
