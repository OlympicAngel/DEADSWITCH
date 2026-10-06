# SPEC-003: Living base diorama (3D)

- Status: Done (F-010); checked in the Editor 2026-10-06 (base at day and night, plot sheet build picker with refusal reasons, facility focus, roofs)
- Pillar: Base & economy (presentation), AI relationship (the drone camera is the AI's eye)
- Touches: economy (facility kinds, levels, power, crew, construction), defense (turrets, raid damage later), corruption (sensor noise)
- Source rules: ADR-0007, doc 11 (world, camera, base), doc 06 s3 (visible progression), quality bar

## Goal
The Hub is a small, tactile 3D world seen through the AI's recon drone. Every facility's kind, level and state is readable at phone scale: higher levels visibly grow, powered facilities glow and move, shed ones go dark, unmanned ones blink amber, construction shows scaffolding. Tapping a slot opens its build/upgrade sheet.

## Rules
1. **Art direction:** doc 11 Master art direction + Environment construction rules (heroic realism, modular layered construction, broken silhouettes, verticality, dense purposeful dressing); `docs/agents/environment-art.md`. Procedural, engine-agnostic geometry (`src/Deadswitch.Art`) with real-world proportions, thin bevels, smooth curved surfaces, medium-frequency detail. Deterministic per seed.
2. **One procedural PBR salvage material model** (base, paint chips, rust, dirt, soot, rain streaks, wetness, edge wear, bump; vertex colors carry AO / edge / variation masks) shared by Unity (custom URP shader, URP Lit fallback) and the headless three.js preview.
3. **Layout:** the AI core bunker at the center, `hub.slots` pads around it, perimeter barriers, ruined terrain, props.
4. **Facility models per kind, growing per level** (new parts added at each level). Animated parts (fans, dishes, rotors) spin only while powered.
5. **States:** powered (lights on, parts move), shed/off (lights off, still), unmanned (amber beacon blinks), under construction (scaffold + partial build), empty pad (markings).
6. **Camera:** high three-quarter view, narrow FOV, slow drift; pinch/drag to look around within bounds; no depth-of-field blur (doc 11 anti-toy rules); subtle grain and chromatic aberration scaled by effects + corruption. Lighting follows the game clock (doc 11).
7. **Interaction:** tap a pad -> select (highlight ring) -> slot sheet: build options (cost, time, effect) or upgrade/power/demolish with clear costs and reasons when unavailable.

## Acceptance criteria
- [x] Headless preview of a day-7 base passes the doc 11 avoid-list review (no cartoon, flat or plastic look) and reads at phone scale
- [x] All facility kinds L1-L5 distinguishable at phone scale
- [x] Slot sheet builds/upgrades through commands with rejection reasons shown
