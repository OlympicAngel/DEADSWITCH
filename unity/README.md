# Unity project

The Unity project is created here (not yet generated). It is the presentation and platform layer only. Game rules live in `src/Deadswitch.Sim` (ADR-0002).

## Create it
1. Install Unity Hub with its install location on D: (`tools/setup-env.ps1` prints how).
2. Install **Unity 6 LTS (6000.x)** with Android and iOS modules.
3. In Unity Hub: New project > 2D (URP) > location `<repo>\unity` > name `Deadswitch`. (If Hub insists on a subfolder, move the contents up so `unity\Assets` exists.)
4. Record the exact editor version in `unity/ProjectSettings/ProjectVersion.txt` (commit it) and note it in an ADR if it differs from 6000.x.
5. Add the sim as a local package. Edit `unity/Packages/manifest.json`:
   ```json
   "com.deadswitch.sim": "file:../../src/Deadswitch.Sim"
   ```
6. Commit generated `.meta` files. Never ignore them.
7. `git lfs install` once so binary art/audio goes to LFS (`.gitattributes` is preconfigured).

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
