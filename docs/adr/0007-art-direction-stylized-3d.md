# ADR-0007: Stylized 3D realism with a drone-feed presentation

- Status: Accepted (amended 2026-10-03: heroic realism; 2026-10-04: realism pass, day/night, procedural only)
- Date: 2026-10-03

## Context
The original direction (docs 07 and 11, ADR-0001 context) was 2D gritty painted realism. Concept passes read as "drawn" rather than grounded. The game needs a world that feels physical and worth protecting, while staying readable and appealing at phone scale. Art style is expensive to reverse once assets are in production.

## Decision
Render the world (base, map terrain, battle-report stills) as **semi-stylized 3D realism**: physically based materials with slightly simplified, chunky forms and readable silhouettes, shown through a **subtle drone-feed camera** that represents the AI's sensors. The military terminal/CRT HUD stays a crisp 2D overlay. Unity URP (already in `unity/`) is the target pipeline.

## Consequences
- Good: grounded, tactile world; the camera itself expresses the "you see through the AI" theme; 3D diorama makes tier changes, scars and repairs easy to show; reuses one asset set for base, map and report stills.
- Bad / costs: 3D asset pipeline (modeling, texturing, LODs) is heavier than 2D painting; mobile GPU budget needs early profiling; camera effects must stay subtle and fully covered by effect-intensity and reduced-motion settings.

## Alternatives considered
- 2D gritty painted realism (previous direction): read as too illustrated; harder to show persistent damage across many base states.
- Full photorealism: costly on mobile, poor readability at phone scale, and uncanny for small units.

## Amendment (2026-10-03): heroic realism, not cartoon
The owner rejected chunky/semi-cartoon forms. The world is **stylized realism ("heroic realism")**: believable proportions, layered PBR material wear, overcast grounded lighting, readable medium forms for the isometric camera. The full brief and avoid-list live in doc 11 (Master art direction). Implementation: procedural geometry plus a procedural PBR salvage shader shared by Unity and the headless preview.

## Amendment (2026-10-04): realism pass, day/night, procedural only
The owner rejected the toy / miniature feel of the first heroic-realism pass. Decisions: no tilt-shift or depth-of-field blur; lighting follows the game clock with **overcast day as the reference look** (dusk and night are variants), keyed in `BaseLook.json` for Unity and the preview; assets stay **procedural** (no imported textures or models). The brief, the anti-toy rules and the lighting rules live in doc 11.
