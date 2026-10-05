# SPEC-033: 2.5D sector map

- Status: Done (Editor check pending: see docs/agents/HANDOFF.md)
- Pillar: Offense & world (presentation)
- Touches: map screen (F-019), hazard zones and fallout (SPEC-032), intel (scouting, AI estimate), corruption glitches
- Source rules: owner brief 2026-10-04 (blend of the 3D kit and an illustrated map); doc 05 s6 (layered views, fog of war, node regions); doc 11 (heroic realism, never toy-like); ADR-0007 (procedural 3D)

## Goal
The world map should feel like the same place as the base: real ground seen from a fixed recon angle, with the
factions' holdings built from the same kit, and a hand-drawn intelligence layer on top that says what the Hub knows.
Reading it answers at a glance: who owns what, where it is safe to go, what we have not seen yet, where the fallout is.

## Rules
1. **Ground (3D, `Deadswitch.Art/World/SectorScene`)**: one fixed camera (no free camera, no pan or zoom), angled
   about 50 degrees, over low-relief ruined terrain built like the base terrain (salvage shader ground, dirt tracks,
   craters, rubble, dead trees, collapsed blocks). The map plane is the sim's map (-100..100 each way, north = +Z).
2. **Sites**: small landmarks from the base kit, one shape per kind: outpost (walled yard and tower), data center
   (blocks with dishes and a mast), ruins (broken walls), convoy (a road with trucks), the Hub (bunker mound with
   its phosphor beacon). Hazard zones get ground treatments: a glowing crater, a quarantine fence ring, a wreck field.
3. **Illustrated layer (`SectorOverlay`)**, drawn by the UI over the render from one C# description shared with the
   preview: faction territory tints around their sites, route lines from the Hub to each site (solid when an op is
   on it), fog of war over unscouted sites (hatched), the fallout front as a drifting haze over its site.
4. **UI**: the terminal sheet, heat panels and op controls stay as UI Toolkit overlays. Site markers sit on the
   projected landmark positions; the AI's defense estimate label beside each unscouted site glitches with the
   corruption band (no glitch at reduced motion / zero effect intensity).
5. **Look values** in `BaseLook.json` (`map` block: camera, fog scale), shared by Unity and `tools/basepreview`.
   Same time-of-day lighting as the base.
6. **Cost**: the map renders to a texture only while the map screen is open, and only when the hour or the state it
   shows changes; the haze animates in the UI layer.

## Verification
Headless render of the map at day, dusk and night (`art export --map`, `render.mjs --scene ... --hour N`) with the
overlay; Unity compile check. No sim tests (presentation only).
