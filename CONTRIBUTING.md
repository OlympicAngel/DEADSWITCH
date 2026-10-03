# Contributing

Applies to humans and AI agents alike. Rules for agents are in [`AGENTS.md`](AGENTS.md).

- Branches: `feat/<name>`, `fix/<name>`, `docs/<name>`, `chore/<name>`. One concern per branch.
- Commits: Conventional Commits (`feat: add raid mercy window`).
- PRs: use the template. Must pass `tools/check.ps1` and CI.
- Sim changes: determinism, chunking and cap tests are mandatory (see `docs/agents/sim-determinism.md`).
- Decisions that are expensive to reverse: write an ADR first.
- Parallel work: use git worktrees, one agent per branch, and log in `docs/agents/HANDOFF.md`.
- Tooling lives on `D:\dev`. Nothing is installed on `C:`.
