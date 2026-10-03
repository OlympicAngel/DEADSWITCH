# SPEC-003: Living base diorama (3D)

- Status: In progress (F-010)
- Pillar: Base & economy (presentation), AI relationship (the drone camera is the AI's eye)
- Touches: economy (facility kinds, levels, power, crew, construction), defense (turrets, raid damage later), corruption (sensor noise)
- Source rules: ADR-0007, doc 11 (world, camera, base), doc 06 s3 (visible progression), quality bar

## Goal
The Hub is a small, tactile 3D world seen through the AI's recon drone. Every facility's kind, level and state is readable at phone scale: higher levels visibly grow, powered facilities glow and move, shed ones go dark, unmanned ones blink amber, construction shows scaffolding. Tapping a slot opens its build/upgrade sheet.

## Rules
1. **Procedural, engine-agnostic geometry** (`src/Deadswitch.Art`): chunky bevelled forms, flat-shaded facets, vertex-color weathering (ground contact darkening, per-face variation, edge wear). No imported meshes or textures. Deterministic per seed.
2. **One material palette** (PBR base color, metallic, smoothness, emission) shared by Unity (custom URP vertex-color lit shader, URP Lit fallback) and the headless three.js preview.
3. **Layout:** the AI core bunker at the center, `hub.slots` pads around it, perimeter barriers, ruined terrain, props.
4. **Facility models per kind, growing per level** (new parts added at each level). Animated parts (fans, dishes, rotors) spin only while powered.
5. **States:** powered (lights on, parts move), shed/off (lights off, still), unmanned (amber beacon blinks), under construction (scaffold + partial build), empty pad (markings).
6. **Camera:** high three-quarter view, narrow FOV (miniature feel), slow drift; pinch/drag to look around within bounds; tilt-shift depth of field, subtle grain and chromatic aberration scaled by effects + corruption.
7. **Interaction:** tap a pad -> select (highlight ring) -> slot sheet: build options (cost, time, effect) or upgrade/power/demolish with clear costs and reasons when unavailable.

## Acceptance criteria
- [ ] Headless preview of a day-7 base looks like a grounded, readable miniature
- [ ] All facility kinds L1-L5 distinguishable at phone scale
- [ ] Slot sheet builds/upgrades through commands with rejection reasons shown
