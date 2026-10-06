# SPEC-033: 2.5D sector map

- Status: Done; full-screen pan/zoom map and site sheet 2026-10-06 (F-107); checked in the Editor 2026-10-06
- Pillar: Offense & world (presentation)
- Touches: map screen (F-019), hazard zones and fallout (SPEC-032), intel (scouting, AI estimate), corruption glitches
- Source rules: owner brief 2026-10-04 (blend of the 3D kit and an illustrated map); doc 05 s6 (layered views, fog of war, node regions); doc 11 (heroic realism, never toy-like); ADR-0007 (procedural 3D)

## Goal
The world map should feel like the same place as the base: real ground seen from an oblique recon angle, with the
factions' holdings built from the same kit and an intelligence layer that says what the Hub knows. It is the
full-screen primary map view: players can pan and zoom to inspect the sector, then tap a location to see its known
information and available actions. Reading it answers at a glance: who owns what, where it is safe to go, what we
have not seen yet, and where the fallout is.

## Rules
1. **Ground (3D, `Deadswitch.Art/World/SectorScene`)**: a fixed oblique viewing angle (about 50 degrees) with
   pan and zoom, over low-relief ruined terrain built like the base terrain (salvage shader ground, dirt tracks,
   craters, rubble, dead trees, collapsed blocks). The map plane is the sim's map (-100..100 each way, north = +Z);
   camera movement must preserve orientation and keep the player aware of the map bounds.
2. **Sites**: small landmarks from the base kit, one shape per kind: outpost (walled yard and tower), data center
   (blocks with dishes and a mast), ruins (broken walls), convoy (a road with trucks), the Hub (bunker mound with
   its phosphor beacon). Hazard zones get ground treatments: a glowing crater, a quarantine fence ring, a wreck field.
3. **Illustrated layer (`SectorOverlay`)**, drawn by the UI over the render from one C# description shared with the
   preview: faction territory tints around their sites, route lines from the Hub to each site (solid when an op is
   on it), fog of war over unscouted sites (hatched), the fallout front as a drifting haze over its site.
4. **UI**: the map fills the primary view; compact status/navigation and the terminal sheet, heat panels and op
   controls remain UI Toolkit overlays. Site markers sit on projected landmark positions. Tapping a location opens
   a contextual popover with its known information and available actions. In Unity (F-107) the popover is the map's
   bottom sheet (SITE, OPS, TRADE, FACTIONS): folded it is only its tab row; a pin or a tab opens it, the open tab
   or a tap on open ground folds it, and a tapped site flies in (zoom and center above the open sheet, F-112). Sites
   stand twice as far apart as the first map (1.2 m per map unit); the view never pans past the outermost sites or
   shows the edge of the ground. The map opens close over the Hub; drag pans, pinch or the wheel zooms; pins off the view hide; where tags still collide (zoomed out, large text) the Hub and the selected site keep theirs and the others show the pin only; a tag never runs off the plot edge; a press becomes a drag only
   past an 8 dp slop so a tap still reaches its pin. The AI's defense estimate beside each
   unscouted site glitches with the corruption band (no glitch at reduced motion / zero effect intensity).
5. **Look values** in `BaseLook.json` (`map` block: camera, fog scale, `zoomStart`/`zoomMin`/`zoomMax`/`zoomStep`, `panSlack`), shared by Unity and `tools/basepreview`.
   Same time-of-day lighting as the base.
6. **Cost**: the map renders to a texture only while the map screen is open, and refreshes when the hour, displayed
   state, or camera view changes; the haze animates in the UI layer.

## Verification
Headless render of the map at day, dusk and night (`art export --map`, `render.mjs --scene ... --hour N`) with the
overlay; Unity compile check; verify pan, zoom, location popovers and action access in the Unity Editor at phone
layout. No sim tests (presentation only).
