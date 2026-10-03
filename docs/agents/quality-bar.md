# Playbook: Quality bar (production from day one)

The owner's direction (2026-10-03): **no throwaway prototypes.** Everything we build is meant to ship. Numbers can change later; structure, code quality, and presentation should not need a rewrite. Visuals and polish get real effort: the owner is detail-obsessed and loves eye candy.

## Code
- Production structure from the first commit: clear modules, no "temp" hacks, no dead code, no TODO without a BACKLOG item.
- **Every tunable lives in config**, never as a literal in logic. Sim: `SimConfig` sections, mirrored in the balance file. Presentation: design tokens (colors, spacing, durations, easing) in one place. Mark design placeholders *(tune)*.
- Use real math where it improves feel or fairness (fixed-point curves, easing, smoothing, logistic/soft caps, exponent scaling via integer tables). In the sim it must stay integer/fixed-point (see `sim-determinism.md`).
- Tests for every rule; guard tests for intended feel (caps, cadence, no soft-lock).
- Errors are handled, not swallowed. Saves are versioned. Nothing blocks the main thread on mobile.

## Visual and motion bar
Source: `docs/design/11_visual_theme_and_motion.md` (palette is provisional, structure is not).
- **One token system.** Colors, type scale, spacing (4 px grid), radii, glow strengths, animation durations and easings are named tokens. No raw hex or magic pixel values in feature code.
- **Hierarchy first.** Each screen has one focal area, one primary action, stable critical status. Align to the grid; consistent padding; optical alignment of numbers (tabular/monospace digits).
- **Alive, not noisy.** Terminal phosphor glow, scanlines, subtle flicker, animated counters, eased transitions, sparklines, radar sweeps. Motion explains state change; every animation has a readable static end state; nothing delays input.
- **Corruption is visible** on signal surfaces and scales with the doc 10 bands, never on touch targets, warnings, or values.
- **Accessibility is part of polish:** shape + color (never color alone), contrast checked on real backgrounds, effect-intensity and reduced-motion settings honored, touch targets at least 44 px, one-handed reach for primary actions.
- **Pixel care:** no clipped text, no jitter in counters (fixed-width digits), no misaligned icons, safe areas respected on notched phones, portrait first.

## Verification (you cannot ship what you have not seen)
- Sim: `tools/check.sh` / `tools/check.ps1` gate (format, build with warnings as errors, tests).
- Unity code: the **Unity compile check** (`tools/UnityCompileCheck`) builds `unity/Assets/Game/**` against Unity reference assemblies so API mistakes fail before the Editor is opened.
- Visuals: the **UI preview** tool renders UI layouts and procedural visuals to PNG so they can be inspected headlessly. Look at every screenshot you produce; fix misalignment, contrast, overflow, and clutter before committing. Attach key screenshots in the PR / HANDOFF.
- When the Unity Editor is available (owner's machine), open the project and confirm the play-mode result; log anything the headless checks missed back into this playbook.
