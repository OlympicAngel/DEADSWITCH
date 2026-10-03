@AGENTS.md

# Claude Code notes

- Project skills live in `.claude/skills/` and point at the canonical playbooks in `docs/agents/`. The playbooks are the single source of truth (Codex reads them too).
- Slash commands: `/check`, `/spec <name>`, `/adr <title>`, `/sim-run <seed> <hours>`, `/handoff`.
- Use plan mode for anything touching `GameState`, the save format, or the event log.
- Use the `determinism-reviewer` subagent before merging any change under `src/Deadswitch.Sim/`.
- Keep replies short. Prefer editing existing files over creating new ones.
