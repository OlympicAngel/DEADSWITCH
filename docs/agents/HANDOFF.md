# Handoff (current state)

A short, living snapshot for the next agent. **Edit in place; do not append session logs** (history is in git and `docs/roadmap/BACKLOG.md`). Only record what a future agent needs and cannot get from the code, `TASK.md` or the backlog. Keep it under ~40 lines.

## State
- Sim (M0-M2 core) done: config file (ADR-0008), commands/replay, saves v2 (ADR-0009, versioned visitor), economy (SPEC-002), pressure loop (SPEC-001), AI dials/delegation/lies (SPEC-004). `tools/check.sh` green.
- Unity: code-only boot (`Runtime/Core/Bootstrap.cs`), `GameHost`, UI kit + HUD (UI Toolkit, `Resources/UI`). 3D base (SPEC-003) built from the modular kit (`Models/Kit*.cs`); look v3 (F-025): time-of-day lighting keyframes in `Resources/Base/BaseLook.json` (overcast day reference, `render.mjs --hour`), anti-toy kit parts, no tilt-shift. Advisor voice: `Host/Narrative/Advisor` + `Resources/AdvisorLines.txt`, bridged by `UI/Hud/AdvisorVoice`. OPS screen (SPEC-005): `UI/Screens/OpsScreen` + `Resources/UI/Ops.uxml`. Battle report (SPEC-006): `Host/Reports/BattleReport` (view over the log), `Reports/ReportStills` + `Shaders/DeadswitchNovel.shader`, `UI/Screens/ReportScreen`. CORE terminal (SPEC-007): `UI/Screens/CoreScreen`; the HUD shows the self-reported corruption, glitch effects follow the true band. Modules (SPEC-008): `Systems/Modules` (catalog, research, tier gates), MODULES view inside CORE (`UI/Screens/ModulesView`). Settings (F-024): `UI/Screens/SettingsScreen` from the CORE header; text size via `ds-scale-115/130` token overrides on the root. Climax (SPEC-011): `Systems/ClimaxSystem`, final-window card in CORE. Opening (SPEC-009): `UI/Hud/OpeningFlow` (prologue, boot reveal, guide; guide dismissal in PlayerPrefs `ds.guide.off`). Ruthless choices (SPEC-012): `Systems/PeopleChoices`, WORKFORCE sheet (`UI/Screens/WorkforceScreen`) from the HUD people cell; lethal choices need a second tap. District (SPEC-013): tier-up appends plots; `HubScene.District` draws staked lots (T1) or the walled district (T2+); `BaseView.Layout` rebuilds on tier/slot change.

## Not yet verified in the Unity Editor
- Report stills use `RenderPipeline.SubmitRenderRequest` (`Rendering/ReportRender.cs`, Unity 2023.2+ API) with a `Camera.Render` fallback; check the four panels render and are graded.
- Local notifications (SPEC-010): install `com.unity.mobile.notifications` (Package Manager) to activate `Runtime/Notifications/Mobile` (versionDefines); without it alerts only log. Check Android 13 permission prompt and iOS authorization on device.
- Time-of-day lighting (F-025): check `BaseView.ApplyLight` / `PostFx` light levels against the preview (`unitySunScale`, `unityPointScale` in BaseLook.json) and the `_DsEmissionScale` / `_DsConeScale` globals.
- Nothing in `unity/Assets/Game` has run in the Editor yet (cloud sessions have no Unity). Compile check covers runtime code except `Runtime/Rendering/` (URP) and `Editor/`. First owner run: open `unity/`, press Play, check Console for `[DEADSWITCH]` lines and pink materials (shader `Resources/Shaders/DeadswitchLit.shader`).

## Balance (owner, 2026-10-04)
- Feature work first, balance at the end. Parked: away players (casual profile) lose ~85% of raids because unprepared defense is turrets only (raid strength ~2-3x defense by day 6); no fix chosen yet. Use `balance` (SPEC-014) when tuning resumes.

## Gotchas
- Saves that reached Tier 2 before F-023 have no district plots (no migration; pre-release).
- Git LFS uploads fail from cloud sessions: binaries go in the non-LFS overrides at the end of `.gitattributes`.
- New files under `src/Deadswitch.{Sim,Host,Art}` or `unity/Assets` need `python3 tools/gen_meta.py`.
- AI actions (delegated builds, autopilot) are derived inside the tick, not recorded: a command log replays only under the rules version that recorded it.
- Base look values live in `unity/Assets/Game/Resources/Base/BaseLook.json` (read by Unity and `tools/basepreview`).
