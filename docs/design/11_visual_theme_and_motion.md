# 11 — Visual Theme, Motion & Tone

This guide makes the art direction in [07_world_and_story_bible.md](./07_world_and_story_bible.md) and mobile UX in [08_tech_and_roadmap.md](./08_tech_and_roadmap.md) actionable for UI, concept art, effects, and audio.

## Status

- **Locked direction:** stylized 3D realism seen through a subtle drone-feed camera ([ADR-0007](../adr/0007-art-direction-stylized-3d.md)); a restrained military terminal/CRT interface; battle reports as graphic-novel panels of rendered stills; a living base diorama; persistent scars; corruption that affects presentation.
- **Locked alert semantics:** signature colors and sound/haptic cues are defined by [10_resolved_decisions.md](./10_resolved_decisions.md). Pair color with shape and a readable label.
- **Prototype proposals:** palette swatches, typography roles, material detail, and motion guidance below are starting points, not production locks. Validate them on devices and against accessibility needs before locking.

The world should feel worn and physical; the interface should feel precise, purposeful, and faintly untrustworthy. Tension comes from decisions and what the player must protect, not from obscuring essential information.

## Visual language

- **World:** real-time 3D (Unity URP) with physically based materials: dust, smoke, faded paint, patched metal, wet mud, and localized warm light under a cold, desaturated grade. Forms are slightly chunky and simplified (semi-cartoonish proportions, bevelled edges, readable shapes) so silhouettes and interactable objects stay legible at phone scale. Not photoreal, not cel-shaded.
- **Camera:** the AI's recon-drone view. High three-quarter angle, light tilt-shift so the base reads as a real miniature. Sensor effects (noise, edge chromatic aberration, faint scanline interference) are **barely noticeable** at default intensity, never cover gameplay, and scale with corruption and the effect-intensity setting.
- **Machine:** restrained field-terminal surfaces, phosphor accents, telemetry, grids, and controlled scanline texture. Avoid generic neon cyberpunk.
- **Base:** present it as a living diorama. Show power, population, damage, activity, and tier identity through landmarks; repairs visibly change persistent scars.
- **Screens:** give each screen one primary focal area, a clear action hierarchy, and stable critical status. Keep touch targets clear and put secondary detail behind deliberate inspection.
- **Texture:** keep grime and distress off small text, icons, controls, maps, and important values.

## Prototype palette

All hex values are **proposed (tune)**. The alert meanings are locked in doc 10; these exact swatches are not.

| Token | Proposed swatch | Use |
|---|---|---|
| Terminal phosphor | `#A8D58A` | Focus, confirmed actions, healthy readouts |
| Terminal amber | `#E8B45C` | Caution, timers, raid signature |
| Siege red | `#D85B52` | Siege signature, immediate danger |
| Signal magenta | `#C176B4` | Virus signature, compromised data |
| Purge white | `#F0EBDD` | Purge highlight, paired with red |
| World charcoal | `#171B19` | Deep background and terminal surround |
| Panel graphite | `#252B27` | Raised surfaces, map plates, report frames |
| Field paper | `#D8D1BC` | Main text and battle-report paper |
| Dust grey | `#9DA197` | Secondary labels and inactive telemetry |
| Signal blue | `#82AEB6` | Neutral navigation or intel, not an alert type without a decision |

Keep the world low-chroma and reserve bright accents for meaningful states. Check contrast on actual backgrounds, in light and dark conditions, and with color-vision simulations. Never communicate attack type, success, or failure by color alone; use distinct shapes, labels, and patterns as needed.

## Type, state, and motion

- Use a highly legible mono or technical face for readouts, timers, and coordinates; a clear sans-serif for navigation/actions; and comfortable text for dialogue and reports. Exact fonts and sizes remain open for device testing.
- Pair icons with short labels on first use and for high-consequence actions. Distinguish verified facts from uncertain AI estimates without making critical controls appear corrupted.
- Motion must explain state and consequence; every animation needs a readable static state. It must never delay input or determine a gameplay outcome.
- Keep ambient motion sparse. Use brief, direct interface feedback; orienting strategic transitions; and clear anticipation, impact, and aftermath for major threats.
- Localize corruption effects to signal surfaces. Scale them with the corruption bands in doc 10, but do not distort touch targets or obscure warnings, controls, timers, values, or outcomes.
- Avoid rapid full-screen flashes and repeated camera shake. Respect effect-intensity and reduced-motion settings; essential information remains available with effects, sound, or haptics disabled.

## Surface priorities

| Surface | Priority |
|---|---|
| Base | Readable diorama, persistent scars, visible activity, essential status |
| World map | Legible routes/zones, faction heat, scout-confirmed versus predicted intel |
| AI terminal | Diagnostics, dialogue, and clear audit/verification affordances |
| Operations | Cost, duration, destination, risk, and confirmation before launch |
| Battle report | Graphic-novel panels of rendered stills as evidence, itemized losses, and a visible verification action |
| Notifications | Short, factual, opt-in alerts that do not hide the event or timer |

## Prototype review

- At phone size, can a player identify the screen, primary action, current state, and next threat?
- Can every attack signature be distinguished without color or sound alone?
- Do damage, repair, construction, and tier changes communicate what changed?
- Can players reduce motion/effects without losing information or input responsiveness?
- Are prototype swatches, text, contrast, and touch targets checked on representative devices before production lock?
