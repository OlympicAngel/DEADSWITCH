# Contributing

Applies to humans and AI agents alike. Rules for agents are in [`AGENTS.md`](AGENTS.md).

- Branches: `feat/<name>`, `fix/<name>`, `docs/<name>`, `chore/<name>`. One concern per branch.
- Commits: Conventional Commits (`feat: add raid mercy window`).
- PRs: use the template. Must pass `tools/check.ps1` and CI.
- Sim changes: keep the existing determinism/chunking test green; add a test only for a rule that could silently break (see `docs/agents/sim-determinism.md`).
- Decisions that are expensive to reverse: write an ADR first.
- Parallel work: use git worktrees, one agent per branch, and log in `docs/agents/HANDOFF.md`.
- Tooling lives on `D:\dev`. Nothing is installed on `C:`.
