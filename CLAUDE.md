@AGENTS.md

# Claude Code notes

- Project skills live in `.claude/skills/` and point at the canonical playbooks in `docs/agents/`. The playbooks are the single source of truth (Codex reads them too).
- Slash commands: `/check`, `/spec <name>`, `/adr <title>`, `/sim-run <seed> <hours>`, `/handoff`.
- Use plan mode only for save-format or event-log changes; otherwise just do the work.
- Use the `determinism-reviewer` subagent only when a change adds RNG draws, `GameState` fields, or offline catch-up logic. The build and tests cover the rest.
- Keep replies short. Prefer editing existing files over creating new ones.
