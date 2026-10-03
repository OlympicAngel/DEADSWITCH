# TASK: F-007 Unity foundation

- Status: Done
- Started: 2026-10-03   Branch: claude/magical-ritchie-bx4xbb
- Pillar / milestone: platform / M2
- Spec: ADR-0004 (offline catch-up), ADR-0009 (saves)
- Sources: unity/README.md, ADR-0002, ADR-0004, doc 08 s1 + s6, quality bar

## Goal
Pressing Play (or launching on a phone) boots the game with no scene setup: the sim loads the shipped balance file, restores the save (or starts a new run), catches up offline time safely, ticks in real time, saves crash-safely on pause/quit, and exposes state + commands to the presentation layer. Unity code is compile-checked headlessly.

## Steps
- [x] 1. `tools/UnityCompileCheck` project (Unity reference assemblies) wired into the gates; `Assets/Game` asmdef layout
- [x] 2. Host: offline clock guard (elapsed, cap, desync) + session timestamp, in `Deadswitch.Host`
- [x] 3. Unity: `.toml` ScriptedImporter, bootstrap (`RuntimeInitializeOnLoadMethod`), `GameHost` (load/new, catch-up, real-time ticking with time scale, autosave, pause/resume presence)
- [x] 4. Settings (effect intensity, reduced motion, haptics, time scale for dev) persisted in PlayerPrefs; `GameEvents` bridge for the UI (sim events -> C# events)
- [x] 5. Docs: unity/README, HANDOFF/BACKLOG

## Notes
- No Unity Editor here: compile check uses UnityEngine.Modules 2021.3 reference assemblies; avoid APIs newer than 2021.3 or removed in Unity 6 in `Assets/Game` runtime code. Editor scripts are not compile-checked: keep them tiny.

## Blocked / questions
- none
