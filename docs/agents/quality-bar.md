# Playbook: Quality bar (production from day one)

The owner's direction (2026-10-03): **no throwaway prototypes.** Everything we build is meant to ship. Numbers can change later; structure, code quality, and presentation should not need a rewrite. Visuals and polish get real effort: the owner is detail-obsessed and loves eye candy.

## Code
- Production structure from the first commit: clear modules, no "temp" hacks, no dead code, no TODO without a BACKLOG item.
- **Every tunable lives in config**, never as a literal in logic. Sim: `SimConfig` sections, mirrored in the balance file. Presentation: design tokens (colors, spacing, durations, easing) in one place. Mark design placeholders *(tune)*.
- Use real math where it improves feel or fairness (fixed-point curves, easing, smoothing, logistic/soft caps, exponent scaling via integer tables). In the sim it must stay integer/fixed-point (see `sim-determinism.md`).
- Minimal testing: tests only where a bug would be silent and costly (determinism, saves, parsing, soft-lock guards). Verify everything else by running it.
- Errors are handled, not swallowed. Saves are versioned. Nothing blocks the main thread on mobile.

## Visual and motion bar
Source: `docs/design/11_visual_theme_and_motion.md` (palette is provisional, structure is not). **3D world: follow the Master art direction in doc 11 (heroic realism, anti-toy rules, day/night lighting). Review every render at day, dusk and night against its avoid-list; anything cartoon, toy-like, miniature, cube-like, blob-like, flat-colored or plastic is a defect. Build 3D assets per `docs/agents/environment-art.md` (modular, layered, broken silhouettes, real thickness).**
- **One token system.** Colors, type scale, spacing (4 px grid), radii, glow strengths, animation durations and easings are named tokens. No raw hex or magic pixel values in feature code.
- **Hierarchy first.** Each screen has one focal area, one primary action, stable critical status. Align to the grid; consistent padding; optical alignment of numbers (tabular/monospace digits).
- **Redesign root causes, not just layout.** When the owner identifies the current UI as confusing or overwhelming, do not count moving, spacing, or recoloring the same panels as a redesign. Revisit information architecture and interaction flow; replace patterns that fail the player's task. Start from player goals and validate comprehension by asking someone unfamiliar with the screen to complete representative tasks without coaching.
- **Alive, not noisy.** Terminal phosphor glow, scanlines, subtle flicker, animated counters, eased transitions, sparklines, radar sweeps. Motion explains state change; every animation has a readable static end state; nothing delays input.
- **Corruption is visible** on signal surfaces and scales with the doc 10 bands, never on touch targets, warnings, or values.
- **Accessibility is part of polish:** shape + color (never color alone), contrast checked on real backgrounds, effect-intensity and reduced-motion settings honored, touch targets at least 44 px, one-handed reach for primary actions.
- **Pixel care:** no clipped text, no jitter in counters (fixed-width digits), no misaligned icons, safe areas respected on notched phones, portrait first.

## Verification (you cannot ship what you have not seen)
- Sim: `tools/check.sh` / `tools/check.ps1` gate (format, build with warnings as errors, tests).
- Unity code: the **Unity compile check** (`tools/UnityCompileCheck`) builds `unity/Assets/Game/**` against Unity reference assemblies so API mistakes fail before the Editor is opened.
- Visuals: the **UI preview** tool (`node tools/uipreview/preview.mjs unity/Assets/Game/Resources/UI/<Screen>.uxml`, output in `artifacts/uipreview/`) renders UXML + USS to PNG headlessly, mirroring the kit behaviors in `Runtime/UI/Kit.cs`. When you add a kit behavior in C#, mirror it in the preview script. Look at every screenshot you produce; fix misalignment, contrast, overflow, and clutter before committing. It renders at 1080x1920 (16:9) by default and warns `squeezed:` where Unity would overlap text: put long screens in a `ScrollView`. USS is not CSS: no `+`/`~`/`[attr]` selectors (one drops the whole sheet), no `:first-child`/`:last-child` (use the `is-first`/`is-last` classes from `Kit.MarkEnds`); `tools/uss_lint.py` (in the gate) enforces it. The base preview (`tools/basepreview`) mirrors `BaseLook.json` including `scarFx`; with a live Editor, see the agent-driving notes in `docs/agents/HANDOFF.md`. Attach key screenshots in the PR / HANDOFF.
- When the Unity Editor is available (owner's machine), open the project and confirm the play-mode result; log anything the headless checks missed back into this playbook.

## Assets and storage
- Git LFS uploads are refused from cloud agent sessions. Keep binary assets small and listed in the non-LFS overrides at the end of `.gitattributes` (fonts, `docs/media` screenshots), or generate them procedurally (preferred for meshes, textures and UI art).
- UI is authored as UXML + USS under `unity/Assets/Game/Resources/UI/` using only `Tokens.uss` variables and `Components.uss` classes; no custom UXML element types (API differs between Unity versions).
