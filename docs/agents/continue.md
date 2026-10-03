# Playbook: Continue (resume the work loop)

Use when the user says **"continue"** (or "go on", "next", "keep going"), or at the start of any session that should pick up where the last one stopped. The goal: any agent can resume with zero chat history.

## The three tracking files
| File | Role | Who edits |
|------|------|-----------|
| `TASK.md` (repo root) | The **one active feature**: goal, sources, step checklist, notes, blockers. Live state. | Updated after every step |
| `docs/roadmap/BACKLOG.md` | Ordered **feature queue** (F-NNN) with status. What comes next. | When a feature starts/finishes, or scope is discovered |
| `docs/agents/HANDOFF.md` | Append-only **session log**. | End of each feature and each session |

`docs/roadmap/ROADMAP.md` stays the milestone view; tick its boxes when a feature completes one.

## Loop
1. **Sync.** `git status`, then `git pull origin <current branch>` (retry with backoff on network errors). Never discard uncommitted work you did not create; ask.
2. **Tooling.** Make sure the gate runs: `tools/check.sh` (Linux/macOS) or `tools/check.ps1` (Windows). If `dotnet` is missing in a cloud session, the SessionStart hook installs it; otherwise see `README.md`.
3. **Read `TASK.md`.**
   - `Status: In progress` → resume the **first unchecked step**. Re-read the sources it lists.
   - `Status: Done` or no active feature → take the **top `Ready` item** in `BACKLOG.md`, set it `In progress`, and rewrite `TASK.md` from the template at the bottom of this file.
4. **Plan the feature** (only when starting one): follow `docs/agents/feature-workflow.md` and `docs/agents/quality-bar.md`. Break it into 4-12 steps, each one commit-sized and testable. Write them into `TASK.md` before coding.
5. **Do one step at a time.** For each step:
   1. Implement with tests (sim) or a verification path (Unity/visuals: compile check + preview screenshot, see quality bar).
   2. Run the gate. Fix everything. Never weaken a test.
   3. Tick the step in `TASK.md`, add any notes/decisions under **Notes**.
   4. Commit (Conventional Commits) and **push**. One step = one commit is the default.
6. **Finish the feature** when every step is ticked:
   - Definition of done in `AGENTS.md` holds (gate green, tests, docs).
   - `BACKLOG.md`: mark `Done` with the date. Tick any ROADMAP boxes it completes. Update the spec status.
   - Append a `HANDOFF.md` entry (done / half-done / next / decisions).
   - Set `TASK.md` to `Status: Done` with a one-line summary, then immediately start the next feature (step 3). Commit and push.
7. **Keep going** without asking, unless blocked. A real blocker is a decision the source-of-truth order cannot settle (canon conflict, material scope/balance/monetization/accessibility change, something only the user can do such as signing in to Unity). Write it under **Blocked / questions** in `TASK.md`, push, tell the user in one short paragraph, and continue with the next unblocked step or feature.
8. **Before the session ends** (or context runs low): `TASK.md` must describe exactly where you stopped, everything committed and pushed.

## Rules
- One active feature at a time. Discovered work goes into `BACKLOG.md` (as `Ready` or `Later`), not into the current feature, unless it blocks it.
- Keep `TASK.md` short and current. Delete stale notes; history lives in git and `HANDOFF.md`.
- Do not reorder `BACKLOG.md` priorities silently. Add a note with the reason if you do.
- Push after every commit to the branch named by the session/user. Never force-push shared branches.

## TASK.md template
```markdown
# TASK: F-NNN <feature title>

- Status: In progress | Done
- Started: YYYY-MM-DD   Branch: <branch>
- Pillar / milestone: <pillar> / <M#>
- Spec: docs/specs/SPEC-NNN-<name>.md (or n/a)
- Sources: <doc 10 sections, design docs, ADRs>

## Goal
<2-4 sentences: player value + what "done" looks like>

## Steps
- [ ] 1. <commit-sized step> (verification: <test / screenshot / command>)
- [ ] 2. ...

## Notes
- <decisions, gotchas, numbers added to SimConfig>

## Blocked / questions
- none
```
