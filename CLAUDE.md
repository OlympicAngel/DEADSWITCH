@AGENTS.md

# Claude Code notes

- Project skills live in `.claude/skills/` and point at the canonical playbooks in `docs/agents/`. The playbooks are the single source of truth (Codex reads them too).
- "continue" (or `/continue`) runs the work loop in `docs/agents/continue.md`: resume `TASK.md`, commit and push after each step, keep going.
- Slash commands: `/continue`, `/check`, `/spec <name>`, `/adr <title>`, `/sim-run <seed> <hours>`, `/handoff`.
- On Linux/cloud sessions run the gate with `tools/check.sh`. A SessionStart hook installs the .NET SDK in cloud sessions.
- Visuals matter as much as rules: load the `quality-bar` skill for any UI, visual, or config work and look at every preview screenshot you generate.
- Use plan mode only for save-format or event-log changes; otherwise just do the work.
- Use the `determinism-reviewer` subagent only when a change adds RNG draws, `GameState` fields, or offline catch-up logic. The build and tests cover the rest.
- Keep replies short. Prefer editing existing files over creating new ones (`TASK.md` per feature is expected).
