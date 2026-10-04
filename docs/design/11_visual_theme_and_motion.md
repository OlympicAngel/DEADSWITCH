# 11 — Visual Theme, Motion & Tone

This guide makes the art direction in [07_world_and_story_bible.md](./07_world_and_story_bible.md) and mobile UX in [08_tech_and_roadmap.md](./08_tech_and_roadmap.md) actionable for UI, concept art, effects, and audio.

## Status

- **Locked direction:** heroic realism (stylized realistic 3D, PBR) seen through a subtle drone-feed camera ([ADR-0007](../adr/0007-art-direction-stylized-3d.md), master brief below); a restrained military terminal/CRT interface; battle reports as graphic-novel panels of rendered stills; a living base diorama; persistent scars; corruption that affects presentation.
- **Locked alert semantics:** signature colors and sound/haptic cues are defined by [10_resolved_decisions.md](./10_resolved_decisions.md). Pair color with shape and a readable label.
- **Prototype proposals:** palette swatches, typography roles, material detail, and motion guidance below are starting points, not production locks. Validate them on devices and against accessibility needs before locking.

The world should feel worn and physical; the interface should feel precise, purposeful, and faintly untrustworthy. Tension comes from decisions and what the player must protect, not from obscuring essential information.

## Visual language

- **World:** follows the master art direction below. Real-time 3D (Unity URP) with believable proportions and layered PBR material detail under a cold desaturated grade with warm practical lights.
- **Camera:** the AI's recon-drone view. High-angle isometric-style framing, narrow FOV, no depth-of-field blur (it reads as a miniature). Sensor effects (noise, edge chromatic aberration, faint scanline interference) are **barely noticeable** at default intensity, never cover gameplay, and scale with corruption and the effect-intensity setting.
- **Machine:** restrained field-terminal surfaces, phosphor accents, telemetry, grids, and controlled scanline texture. Avoid generic neon cyberpunk.
- **Base:** present it as a living diorama. Show power, population, damage, activity, and tier identity through landmarks; repairs visibly change persistent scars.
- **Screens:** give each screen one primary focal area, a clear action hierarchy, and stable critical status. Keep touch targets clear and put secondary detail behind deliberate inspection.
- **Texture:** keep grime and distress off small text, icons, controls, maps, and important values.

## Master art direction (owner brief, locked 2026-10-03, restated 2026-10-04)

This brief is the style source for every 3D asset, render and preview. Read it as written; the rules after it say how we apply it.

> Create modular game-ready 3D assets for a post-apocalyptic survivor strategy game in a stylized realistic ("heroic realism") art style. Assets should prioritize strong silhouettes, believable proportions, and physically based materials over excessive polygon counts. Every asset must feel handcrafted from scavenged industrial parts after a global war.
>
> Architecture is improvised from cracked reinforced concrete, rusted corrugated steel, weathered shipping containers, sandbags, scaffolding, exposed rebar, patched sheet metal, salvaged machinery, cables, pipes and welded scrap. Nothing looks pristine or factory-new.
>
> Materials should feature realistic PBR detail: chipped paint, oxidation, dirt accumulation, mud splashes, edge wear, scratches, soot, oil stains, rain streaks, dust buildup, faded warning markings and subtle decals. Surfaces tell a story through age and use.
>
> The environment uses a cold desaturated palette of grey, concrete, olive, charcoal and faded military green, with warm tungsten work lights providing contrast. Bright colours are reserved only for gameplay-significant elements such as AI technology, alarms and interactable objects.
>
> The world should feel inhabited. Scatter believable environmental storytelling throughout: stacked supply crates, fuel barrels, cable reels, pallets, discarded tools, tarps, makeshift workstations, generators, vents, antennas, pipes, barricades, damaged vehicles and maintenance equipment.
>
> Lighting is cinematic but grounded. Overcast skies, soft global illumination, volumetric dust, subtle fog, warm practical lights, emissive windows, wet surfaces and realistic reflections. Avoid dramatic fantasy lighting.
>
> Scale everything for an isometric mobile strategy game viewed from high above. Prioritize readability over tiny details. Buildings should have bold shapes with medium-frequency detail that remains visible at gameplay distance.
>
> The overall feeling is tense, militarized and functional. A fragile survivor settlement that has endured years of conflict.
>
> **Design assets specifically for a high-angle isometric mobile strategy camera. Detail should be concentrated in large and medium forms rather than tiny surface details so the environment remains readable at gameplay zoom levels.** (Applies to every asset and every prompt.)

**Keywords (use consistently):** stylized realism, physically based rendering (PBR), Unreal Engine 5 quality, modular environment assets, believable proportions, grounded military aesthetic, environmental storytelling, production-quality game assets, cinematic lighting, realistic material definition, high readability from isometric camera, lived-in environment, layered detail, post-war industrial salvage, atmospheric perspective, volumetric lighting, physically accurate materials.

**Avoid (negative prompt, each one is a defect):** cartoon, mobile casual, toy-like, simplistic, clean surfaces, flat colors, plastic materials, exaggerated proportions, cel shading, fantasy architecture, sci-fi neon, cyberpunk, saturated colors, glossy metal, unused pristine assets, empty environment, low-detail textures, random clutter, noisy composition.

### Environment construction rules (owner, 2026-10-03)
- **Rebuild, don't decorate.** No simple boxes. Buildings are layered structures assembled from modules, overhangs, extensions, welded plates, support beams, pipes, vents, antennas, scaffolding, balconies, ladders and machinery (3-5x the geometric complexity of the first pass).
- **Break every silhouette.** No straight rectangular outlines: asymmetry, repairs, damage, additions and a believable construction history.
- **Verticality:** catwalks, elevated platforms, stacked containers, retaining walls, staircases, cables suspended between poles, rooftop equipment.
- **Flat surfaces get construction detail:** exposed rebar, concrete seams, metal panel joints, ventilation, utility boxes, fuel tanks, generators, transformers, industrial machinery.
- **Crowded, organically expanded over decades,** not intentionally designed. Dense, purposeful dressing: supply crates, pallets, tarps, barrels, fences, sandbags, debris piles, maintenance equipment, work areas, abandoned vehicles.
- **Distinct silhouettes with recognizable functions,** readable from the isometric camera. Large and medium forms over tiny texture details.

### Anti-toy rules (owner, 2026-10-04: "toy / miniature feel" is the main defect)
- **No miniature cues:** no tilt-shift or depth-of-field blur, no saturated "painted model" colors, no strong vignette. The camera is a drone over a real place: atmospheric perspective (haze with distance) gives scale.
- **Real-world thickness and scale:** sheet metal, railings, pipes, cables and tarps are thin; doors 2.1 m, people 1.75 m, barrels 0.9 m. Thick slabs and oversized bevels read as plastic toys.
- **Round things are round:** cylinders of about 0.25 m radius or more get at least 16 segments; barrels have rims and ribs; tanks have caps, bands and fittings.
- **Soft goods drape:** tarps and covers are folded sheets with sag, tie-downs and ropes, never smooth blobs or ellipsoids.
- **Lit openings are recessed:** doors are steel or wood leaves; light comes from small windows, door gaps and practical lamps. No flat glowing slabs.
- **Markings are faded and small:** stencils, hazard stripes and signs are worn, partial and low-contrast, never bold pixel letters.
- **Value contrast over color:** separate forms by light, AO and material (wet dark mud, light dry gravel, dark rust, mid concrete), not by hue.

### Lighting and time of day (owner, 2026-10-04)
- The base follows the game clock: **a warm, hazy day is the reference look** (warm sun, soft shadows, practicals barely on); a pink-gold dawn, a golden hour, an orange sunset and a blue hour lead into cold blue nights where tungsten lamps, emissive windows and light cones take over. Nights are moonlit with a cool white moon, never black (shadows may go pitch black, most of the base must stay readable): each night's moon level comes from `moonPhases` (cycled per night). Lamps mix warm tungsten with some sodium, cold LED and small cyan neon accents; painted parts carry faded repaint colours for life and readability.
- Lighting keyframes by game hour live in `unity/Assets/Game/Resources/Base/BaseLook.json` (shared by Unity and the preview). Review every visual change at day, dusk and night.

**How we build it (owner, 2026-10-04: procedural only, no imported assets):** procedural geometry (`src/Deadswitch.Art`) with real-world proportions and bevelled medium forms; a procedural PBR "salvage" material (world-space noise layers for macro variation, rust, chipped paint, dirt rising from the ground, soot, rain and rust run-off, wetness, edge wear and bump; gravel / soil / wet-mud ground) shared by Unity and the headless preview; soft shadows, AO, fog and warm practical lights. How-to: `docs/agents/environment-art.md`.

### Sector map (owner brief 2026-10-04, SPEC-033)
- A 2.5D map, not a radar plot: one fixed recon angle (about 47 degrees, no free camera) over low-relief ruined ground in the same salvage shader (arid ground mode), with the base's time-of-day light.
- Sites are kit landmarks at map scale (`SectorLandmarks`), one silhouette per kind, faction beacons in faction colors; hazard zones are ground treatments at full size (glowing crater, quarantine camp, wreck field).
- What the Hub knows is drawn, not built: faction territory tints, dashed routes (solid while a team is out), hatched fog of war over unscouted ground, fallout as drifting haze. The AI's estimates under the pins may flicker with corruption; effects intensity and reduced motion turn it off.

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
