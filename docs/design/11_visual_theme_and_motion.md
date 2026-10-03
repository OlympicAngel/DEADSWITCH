# 11 — Visual Theme, Motion & Tone

This guide makes the art direction in [07_world_and_story_bible.md](./07_world_and_story_bible.md) and mobile UX in [08_tech_and_roadmap.md](./08_tech_and_roadmap.md) actionable for UI, concept art, effects, and audio.

## Status

- **Locked direction:** heroic realism (stylized realistic 3D, PBR) seen through a subtle drone-feed camera ([ADR-0007](../adr/0007-art-direction-stylized-3d.md), master brief below); a restrained military terminal/CRT interface; battle reports as graphic-novel panels of rendered stills; a living base diorama; persistent scars; corruption that affects presentation.
- **Locked alert semantics:** signature colors and sound/haptic cues are defined by [10_resolved_decisions.md](./10_resolved_decisions.md). Pair color with shape and a readable label.
- **Prototype proposals:** palette swatches, typography roles, material detail, and motion guidance below are starting points, not production locks. Validate them on devices and against accessibility needs before locking.

The world should feel worn and physical; the interface should feel precise, purposeful, and faintly untrustworthy. Tension comes from decisions and what the player must protect, not from obscuring essential information.

## Visual language

- **World:** follows the master art direction below. Real-time 3D (Unity URP) with believable proportions and layered PBR material detail under a cold desaturated grade with warm practical lights.
- **Camera:** the AI's recon-drone view. High-angle isometric-style framing, narrow FOV, very light depth of field. Sensor effects (noise, edge chromatic aberration, faint scanline interference) are **barely noticeable** at default intensity, never cover gameplay, and scale with corruption and the effect-intensity setting.
- **Machine:** restrained field-terminal surfaces, phosphor accents, telemetry, grids, and controlled scanline texture. Avoid generic neon cyberpunk.
- **Base:** present it as a living diorama. Show power, population, damage, activity, and tier identity through landmarks; repairs visibly change persistent scars.
- **Screens:** give each screen one primary focal area, a clear action hierarchy, and stable critical status. Keep touch targets clear and put secondary detail behind deliberate inspection.
- **Texture:** keep grime and distress off small text, icons, controls, maps, and important values.

## Master art direction (owner brief, locked 2026-10-03)

**Style:** stylized realism ("heroic realism"), physically based rendering, Unreal Engine 5 quality bar, production-quality modular game assets. Strong silhouettes, believable proportions and physically accurate materials over high polygon counts. Every asset looks handcrafted from scavenged industrial parts after a global war.

**Architecture:** improvised from cracked reinforced concrete, rusted corrugated steel, weathered shipping containers, sandbags, scaffolding, exposed rebar, patched sheet metal, salvaged machinery, cables, pipes and welded scrap. Nothing pristine or factory-new.

**Materials (PBR):** chipped paint, oxidation, dirt accumulation, mud splashes, edge wear, scratches, soot, oil stains, rain streaks, dust buildup, faded warning markings, subtle decals. Surfaces tell a story through age and use.

**Palette:** cold, desaturated grey, concrete, olive, charcoal and faded military green; warm tungsten work lights for contrast. Bright color only for gameplay-significant elements (AI technology, alarms, interactables).

**Inhabited world:** believable environmental storytelling: stacked supply crates, fuel barrels, cable reels, pallets, tools, tarps, makeshift workstations, generators, vents, antennas, pipes, barricades, damaged vehicles, maintenance gear. Purposeful, never random clutter.

**Lighting:** cinematic but grounded: overcast sky, soft global illumination, volumetric dust, subtle fog, warm practical lights, emissive windows, wet surfaces, realistic reflections. No dramatic fantasy lighting.

**Camera readability (every asset):** design for a high-angle isometric mobile strategy camera. Concentrate detail in large and medium forms rather than tiny surface details so the environment stays readable at gameplay zoom. Bold building shapes with medium-frequency detail.

**Feeling:** tense, militarized, functional: a fragile survivor settlement that has endured years of conflict.

**Keywords:** stylized realism, PBR, Unreal Engine 5 quality, modular environment assets, believable proportions, grounded military aesthetic, environmental storytelling, production-quality game assets, cinematic lighting, realistic material definition, high readability from isometric camera, lived-in environment, layered detail, post-war industrial salvage, atmospheric perspective, volumetric lighting, physically accurate materials.

**Avoid:** cartoon, mobile casual, toy-like, simplistic, clean surfaces, flat colors, plastic materials, exaggerated proportions, cel shading, fantasy architecture, sci-fi neon, cyberpunk, saturated colors, glossy metal, pristine assets, empty environment, low-detail textures, random clutter, noisy composition.

**Environment construction rules (owner, 2026-10-03):**
- Rebuild, don't decorate: no simple boxes. Buildings are layered structures assembled from modules, overhangs, extensions, welded plates, support beams, pipes, vents, antennas, scaffolding, balconies, ladders and machinery (roughly 3-5x the geometric complexity of the first pass).
- Break every silhouette: no straight rectangular outlines; asymmetry, repairs, damage, additions and a believable construction history.
- Verticality: catwalks, elevated platforms, stacked containers, retaining walls, staircases, cables strung between poles, rooftop equipment.
- Flat surfaces get construction detail: exposed rebar, concrete seams, panel joints, ventilation, utility boxes, fuel tanks, generators, transformers, industrial machinery.
- The settlement is crowded and organically expanded over decades, not designed. Dense, purposeful dressing: crates, pallets, tarps, barrels, fences, sandbags, debris piles, maintenance gear, work areas, abandoned vehicles.
- Each building has a distinct silhouette and readable function from the isometric camera; large and medium forms over tiny details.

**How we build it (no imported assets yet):** procedural geometry (`src/Deadswitch.Art`) with real-world proportions and bevelled medium forms; a procedural PBR "salvage" material (world-space noise layers for rust, chipped paint, dirt, soot, rain streaks, wetness, edge wear and bump) shared by Unity and the headless preview; SSAO, fog and warm practical lights. Hand-made or kitbashed assets may replace procedural ones later if they follow this brief.

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
