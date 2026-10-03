# Handoff (current state)

A short, living snapshot for the next agent. **Edit in place; do not append session logs** (history is in git and `docs/roadmap/BACKLOG.md`). Only record what a future agent needs and cannot get from the code, `TASK.md` or the backlog. Keep it under ~40 lines.

## State
- Sim (M0-M1) done: config file (ADR-0008), commands/replay, saves (ADR-0009), economy (SPEC-002), pressure loop (SPEC-001). `tools/check.sh` green.
- Unity: code-only boot (`Runtime/Core/Bootstrap.cs`), `GameHost`, UI kit + HUD (UI Toolkit, `Resources/UI`). 3D base (SPEC-003, F-010) in progress: world rebuilt from the modular kit (`Models/Kit*.cs`) per the doc 11 master art direction and construction rules; Unity side not yet run in the Editor.

## Not yet verified in the Unity Editor
- Nothing in `unity/Assets/Game` has run in the Editor yet (cloud sessions have no Unity). Compile check covers runtime code except `Runtime/Rendering/` (URP) and `Editor/`. First owner run: open `unity/`, press Play, check Console for `[DEADSWITCH]` lines and pink materials (shader `Resources/Shaders/DeadswitchLit.shader`).

## Gotchas
- Git LFS uploads fail from cloud sessions: binaries go in the non-LFS overrides at the end of `.gitattributes`.
- New files under `src/Deadswitch.{Sim,Host,Art}` or `unity/Assets` need `python3 tools/gen_meta.py`.
- Base look values live in `unity/Assets/Game/Resources/Base/BaseLook.json` (read by Unity and `tools/basepreview`).
