# Handoff (current state)

A short, living snapshot for the next agent. **Edit in place; do not append session logs** (history is in git and `docs/roadmap/BACKLOG.md`). Only record what a future agent needs and cannot get from the code, `TASK.md` or the backlog. Keep it under ~40 lines.

## State
- Sim (M0-M2 core) done: config file (ADR-0008), commands/replay, saves v2 (ADR-0009, versioned visitor), economy (SPEC-002), pressure loop (SPEC-001), AI dials/delegation/lies (SPEC-004). `tools/check.sh` green.
- Unity: code-only boot (`Runtime/Core/Bootstrap.cs`), `GameHost`, UI kit + HUD (UI Toolkit, `Resources/UI`). 3D base (SPEC-003) built from the modular kit (`Models/Kit*.cs`); look v3 (F-025): time-of-day lighting keyframes in `Resources/Base/BaseLook.json` (overcast day reference, `render.mjs --hour`), anti-toy kit parts, no tilt-shift. Advisor voice: `Host/Narrative/Advisor` + `Resources/AdvisorLines.txt`, bridged by `UI/Hud/AdvisorVoice`. OPS screen (SPEC-005): `UI/Screens/OpsScreen` + `Resources/UI/Ops.uxml`. Battle report (SPEC-006): `Host/Reports/BattleReport` (view over the log), `Reports/ReportStills` + `Shaders/DeadswitchNovel.shader`, `UI/Screens/ReportScreen`. CORE terminal (SPEC-007): `UI/Screens/CoreScreen`; the HUD shows the self-reported corruption, glitch effects follow the true band. Modules (SPEC-008): `Systems/Modules` (catalog, research, tier gates), MODULES view inside CORE (`UI/Screens/ModulesView`). Settings (F-024): `UI/Screens/SettingsScreen` from the CORE header; text size via `ds-scale-115/130` token overrides on the root. Climax (SPEC-011): `Systems/ClimaxSystem`, final-window card in CORE. Opening (SPEC-009): `UI/Hud/OpeningFlow` (prologue, boot reveal, guide; guide dismissal in PlayerPrefs `ds.guide.off`). Ruthless choices (SPEC-012): `Systems/PeopleChoices`, WORKFORCE sheet (`UI/Screens/WorkforceScreen`) from the HUD people cell; lethal choices need a second tap. District (SPEC-013): tier-up appends plots; `HubScene.District` draws staked lots (T1) or the walled district (T2+); `BaseView.Layout` rebuilds on tier/slot change.

## Editor run (2026-10-04, via `unity` CLI + `com.unity.pipeline`)
- Verified in Play mode: every screen at 16:9 (scroll where long), prologue and boot reveal, raid warning, TAKE COMMAND live battle, scar fire/smoke, report stills, day/night sweep. 0 console errors or warnings.
- Still open: puddles render near-black (no sky reflection strength); map ALLY/SABOTAGE/RECALL, reactor model, voice packs, HUD themes not exercised; Android phone run; `com.unity.mobile.notifications` 2.4.0 added to the manifest (2026-10-05) without an Editor resolve: first Editor open must resolve it and compile `Notifications/Mobile` (not covered by the compile check).
- Agent driving: `unity command eval_file` runs C# in the live Editor; `capture_game_view --save_path` writes under `Assets/` (move the PNG out, delete the folder and .meta). Set `Application.runInBackground = true` at runtime or Play mode freezes when Unity is unfocused. Editing C#/USS/UXML during Play breaks the UI: stop and press Play again.

## v1.x systems (F-043 to F-054, all in the backlog and doc 10 corrections log)
- Fourth faction Halcyon, chapters and fragments (STORY screen), alliances, sabotage, faction looks, cosmetic season track (SEASON screen from FULL GAME), verifiable runs (`Host/Online/RunVerifier`, CLI `verify --save`), adaptive enemies, luck swings, reactor, AI initiative with recall, starting regions, hazard zones and the fallout front (`Systems/HazardSystem`, SPEC-032), secret nodes (`Systems/SecretSystem`, SPEC-034, Core Profile + DISMANTLE in CORE). unit families (`Systems/UnitSystem`, Drone Bay and Motor Pool in `Models/Military.cs`, OPS forces line, SPEC-035). Save layout v31.
- Sector map (F-057, SPEC-033): `Base/MapView` (map world on layer 30 at y -2000, render to texture), `Rendering/MapRender` (URP request; not covered by the compile check), overlay drawn as raw UI meshes in `MapScreen.DrawOverlay`. Editor checks: the render appears and follows the hour, pins sit on the landmarks and are tappable, overlay triangles are not culled, no frame hitch while the fallout haze drifts.
- Play-mode checks for these: STORY/SEASON screens, map ALLY/SABOTAGE/RECALL controls, reactor model (`Facilities.Reactor`) and voice packs (`AudioDirector.BuildVoice`).

## Balance (latest, 2026-10-04)
- Turret planning (F-044) plus adaptation and luck: active breach ~0-2%, casual-prepared ~12%, autopilot ~0%; Tier 2/3/4 on days ~6/20/40. All guards pass. Retune only from real play data.

## Gotchas
- Saves that reached Tier 2 before F-023 have no district plots (no migration; pre-release).
- Git LFS uploads fail from cloud sessions: binaries go in the non-LFS overrides at the end of `.gitattributes`.
- New files under `src/Deadswitch.{Sim,Host,Art}` or `unity/Assets` need `python3 tools/gen_meta.py`.
- AI actions (delegated builds, autopilot) are derived inside the tick, not recorded: a command log replays only under the rules version that recorded it.
- Base look values live in `unity/Assets/Game/Resources/Base/BaseLook.json` (read by Unity and `tools/basepreview`).
