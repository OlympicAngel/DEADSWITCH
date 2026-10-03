# Unity project

This is the presentation and platform layer. Game rules live in `src/Deadswitch.Sim` (ADR-0002), included as a local Unity package in `Packages/manifest.json`.
See [`../docs/design/11_visual_theme_and_motion.md`](../docs/design/11_visual_theme_and_motion.md) for the visual and motion guide; its exact palette remains provisional.

## Project setup

- Unity **6000.3.25f1 (Unity 6.3 LTS)** was used to generate this project from the Universal Render Pipeline blank template. The Unity 6.3 template catalog did not offer a dedicated 2D URP starter; URP is the target pipeline for the stylized 3D world (ADR-0007).
- Android Build Support, Android SDK/NDK tools, OpenJDK, and iOS Build Support were installed with the Editor. Android builds can be developed on Windows; producing and signing an iOS build still requires macOS and Xcode.
- On the setup workstation, the Editor and Unity Hub are installed under `D:\dev\unity`. The existing Hub session opened this project and the Editor resolved its Unity Personal entitlement; a separate Unity CLI sign-in is not required. If licensing prompts return, resolve them in Hub/Editor. Keep the Editor, project, and caches on D:; Unity keeps small per-user Hub/licensing metadata in its normal profile location.
- Open this `unity` folder from Unity Hub. Keep the exact editor version in `ProjectSettings/ProjectVersion.txt` committed.
- The simulation package is linked via `"com.deadswitch.sim": "file:../../src/Deadswitch.Sim"`. Keep the path relative so the project works from a checkout at a different location.
- `git lfs install` is already configured on the setup workstation; `.gitattributes` is preconfigured for binary art/audio.
- Commit generated `.meta` files. Never ignore them.

## Layout to use inside Assets/
```
Assets/
  Game/            # runtime code (UI, presentation, platform glue)
    Bootstrap/     # app start, save/load, catch-up on launch
    Presentation/  # view models over sim state; never owns rules
    UI/            # CRT terminal HUD, command bar, base diorama
    Platform/      # IEntitlements, ads, notifications, widgets
  Art/  Audio/  Content/
  Tests/           # PlayMode/EditMode tests
```

## Rules
- UI reads sim state and sends **commands** into the sim. It never mutates `GameState` directly.
- No game rule in a MonoBehaviour.
- Effect-intensity and colorblind-safe (shape plus color) settings from day one.

## Runtime architecture (F-007)
- **No scene wiring.** `Assets/Game/Runtime/Core/Bootstrap.cs` runs after the first scene loads and builds the persistent `DEADSWITCH` root (host, then presentation systems in explicit order).
- **`GameHost`** owns the `Simulation`: loads `DeadswitchBalance.toml` from the sim package's `Resources` (imported by `Editor/TomlImporter.cs`), restores the save from `Application.persistentDataPath` (crash-safe `.tmp`/`.bak`), catches up offline time (`OfflineClock`, capped by `[host] max_catch_up_hours`, backwards clock = time desync), ticks in real time (1 tick = 1 real minute; `GameSettings.DevTimeScale` speeds this up in development builds), autosaves, and sends `SetPresence` on pause/resume. All player input goes through `GameHost.Execute(Command)`.
- **Assemblies:** `Deadswitch.Game` (runtime, `Assets/Game/Runtime`), `Deadswitch.Game.Editor` (editor-only).
- **Headless compile check:** `dotnet build tools/UnityCompileCheck` compiles `Assets/Game/**` (except `Editor/`) against Unity reference assemblies (2021.3 API set). Runs in `tools/check.sh` / `check.ps1`.
- After adding files, run `python3 tools/gen_meta.py` so `.meta` GUIDs are committed once.
