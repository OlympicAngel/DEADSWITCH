# AGENTS.md — DEADSWITCH

Instructions for any AI coding agent (Codex, Claude Code, others) and for humans. Read this first.

## What this is
DEADSWITCH: a mobile, offline-first (online-ready) post-apocalyptic strategy game. The player controls a damaged fragment of the war AI that ended the world, and cannot fully trust it. Stack: **Unity (C#)** front end + an **engine-agnostic deterministic C# simulation core**.

## Source of truth (in priority order)
1. `docs/design/10_resolved_decisions.md` — wins over everything below it.
2. `docs/adr/` — accepted architecture decisions.
3. `docs/design/00..11` — the full design.
4. `docs/specs/SPEC-*.md` — the feature you are working on.
If two sources conflict, stop and flag it in your reply. Do not silently pick one.

## Repo map
| Path | Purpose |
|------|---------|
| `src/Deadswitch.Sim/` | Pure C# sim core (netstandard2.1). No Unity references. |
| `src/Deadswitch.Host/` | Engine-agnostic host services (save files, catch-up, clock guard). I/O allowed, no UnityEngine (ADR-0009) |
| `src/Deadswitch.Sim.Tests/` | xUnit tests (sim + host), incl. determinism tests |
| `src/Deadswitch.Cli/` | Headless sim runner for the paper prototype and balancing |
| `unity/` | Unity project (UI, art, audio, platform glue). Consumes the sim as a local package |
| `docs/` | Design, ADRs, specs, roadmap, narrative, agent playbooks |
| `tools/` | PowerShell scripts: env setup, checks, repo restructure |

## Commands
```
& "$env:DOTNET_ROOT\dotnet.exe" build DEADSWITCH.sln -warnaserror
& "$env:DOTNET_ROOT\dotnet.exe" test DEADSWITCH.sln
& "$env:DOTNET_ROOT\dotnet.exe" run --project src/Deadswitch.Cli -- run --seed 42 --hours 24  # headless sim
& "$env:DOTNET_ROOT\dotnet.exe" run --project src/Deadswitch.Cli -- config check            # validate the balance file
powershell -ExecutionPolicy Bypass -File tools\check.ps1  # full gate: format + build + test (Windows)
tools/check.sh                                            # same gate on Linux/macOS/cloud sessions
```

## Hard rules (the sim core)
Read `docs/agents/sim-determinism.md` before touching `src/Deadswitch.Sim`. Summary:
- **No floats or doubles** in sim state or sim math. Integers / fixed-point only.
- **Only `Pcg32`** for randomness. No `System.Random`, no `Guid.NewGuid`, no `Mathf`.
- **No wall-clock** in the sim (`DateTime`, `Stopwatch`). Time is `State.Tick` (1 tick = 1 game minute).
- **No UnityEngine** references, no I/O, no static mutable state in `Deadswitch.Sim`.
- **No iteration over unordered collections** (`Dictionary`/`HashSet`) where order affects state. Use lists or sorted keys.
- Every field of `GameState` is declared in `GameState.Visit` (drives the hash and the save format, ADR-0009). Layout change = bump `SaveGame.FormatVersion` + migration.
- Invariant: `Run(a); Run(b)` equals `Run(a+b)`. Keep the chunking test green.

## Work loop ("continue")
The owner drives work by saying **"continue"**. Follow [`docs/agents/continue.md`](docs/agents/continue.md):
- `TASK.md` (root) = the one active feature and its step checklist. `docs/roadmap/BACKLOG.md` = ordered feature queue.
- Resume the first unchecked step, or start the top `Ready` backlog item. **Commit and push after every finished step.**
- Keep going without asking unless truly blocked; record blockers in `TASK.md`.

## Quality bar
Production-ready from the first commit, not prototypes. Every tunable goes in config (`SimConfig` + balance file; design tokens for visuals). Visual polish is a first-class requirement. See [`docs/agents/quality-bar.md`](docs/agents/quality-bar.md). The 3D world follows the **Master art direction** and **Environment construction rules** in `docs/design/11_visual_theme_and_motion.md` (heroic realism; modular layered construction; never cartoon or cube-like); how-to in `docs/agents/environment-art.md`.

## Workflow
1. **Spec first** only for a new mechanic or system (`docs/specs/TEMPLATE.md`, under one page). Tweaks, fixes, and refactors need none.
2. **Minimal tests only.** Add a test only for a rule that would break silently (determinism/chunking, a hard cap, a locked decision). No tests for getters, trivial math, or restating the implementation. Prefer extending an existing test over adding one. Run only the affected test project while iterating; the full gate once before finishing.
3. **Small PRs.** One concern per branch. Conventional Commits (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`).
4. **ADRs only for hard-to-reverse decisions.** Anything that is expensive to reverse (engine, save format, protocol) gets an ADR in `docs/adr/`.
5. **Update docs in the same change** only where behavior or canon changed (spec, `docs/design/10_*.md` corrections log). Edit in place, keep them lean (no logs, no duplicated summaries). Add a short `HANDOFF.md` entry only if work is left half-done or the next agent needs context.

## Design Quality
- For game ideas and player-facing changes, identify the player value, the satisfying action/result, the feedback that makes it legible, and the next meaningful choice. Aim for earned "one more turn" momentum, not pressure to keep checking in.
- Keep challenge understandable, fair, and recoverable. Preserve accessibility and resolved decisions; do not use deceptive urgency, shame, punitive absence, coercive notifications, or pay-to-win pressure.
- Improve clearly beneficial, low-risk details within scope. Ask before material or subjective changes to scope, balance, difficulty, rewards, loss, timers, monetization, notifications, accessibility, or intended player emotion.
- Use [the engagement playbook](docs/agents/engagement.md) for substantial design and feature work. Keep reusable guidance in the canonical `docs/agents/` playbooks and their thin agent-skill pointers; update the closest playbook rather than creating duplicates.

## Definition of done
- `pwsh tools/check.ps1` passes (or the equivalent `dotnet` commands).
- Rules that could silently break are covered by a test; no existing test was weakened to make it pass.
- No new warnings (warnings are errors).
- Affected docs updated; `TASK.md` and `BACKLOG.md` are current.
- Committed and pushed.

## Working with multiple agents
Claude and Codex both work in this repo. To avoid collisions:
- One agent per branch. Use git worktrees for parallel work (`git worktree add ../ds-feat-x feat/x`).
- Do not edit the same file as another active branch without saying so in `docs/agents/HANDOFF.md`.
- Keep `TASK.md` accurate at the end of a session; touch `docs/agents/HANDOFF.md` only when the current state or a gotcha changed.

## Do not
- Do not rename or move `docs/design/*` numbered files (they are cross-referenced).
- Do not commit secrets, keystores, or `Library/` / `Temp/` from Unity.
- Do not add dependencies to `Deadswitch.Sim` (it must stay dependency-free).
- Do not install tooling on `C:`. All dev tooling lives under `D:\dev` (see `tools/setup-env.ps1`).
- Do not invent balance numbers silently. Declare them in a `SimConfig` section and the shipped balance file (`src/Deadswitch.Sim/Resources/DeadswitchBalance.toml`, ADR-0008) and log them in the corrections log if they change doc 10.

## Style
- C# 9 (Unity-compatible), block-scoped namespaces, braces always, `_camelCase` private fields.
- Names from the glossary in `docs/design/07_world_and_story_bible.md`: Core, Hub, Outpost, Corruption, Heat, Override, Signature, Purge, Relocation.
- Player-facing text: AI advisor voice rules in `docs/narrative/ADVISOR_VOICE.md`.
