# ADR-0005: Repo layout and multi-agent workflow

- Status: Accepted
- Date: 2026-10-03

## Context
Development is done with Claude Code and Codex in the same repo, on Windows, with tooling kept off `C:`.

## Decision
- Monorepo: `src/` (sim, tests, CLI), `unity/`, `docs/`, `tools/`.
- Single instruction file `AGENTS.md` (read by Codex and others); `CLAUDE.md` imports it. Canonical playbooks in `docs/agents/`; `.claude/skills/*` are thin pointers so there is one source of truth.
- Agents work one-per-branch via git worktrees and leave a note in `docs/agents/HANDOFF.md`.
- Gate: `tools/check.ps1` locally, same steps in CI.
- Recommendation: the working folder name has a space (`DEADSWITCH AI`). Rename to `deadswitch` to avoid tooling quirks.

## Consequences
- Good: consistent behavior across agents; docs double as onboarding.
- Bad: playbooks must be kept current or agents drift.
