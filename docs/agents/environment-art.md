# Playbook: Building environment art (procedural kit)

Use for any 3D world asset (facilities, bunker, props, terrain dressing, lighting) in `src/Deadswitch.Art` and `BaseLook.json`. Style source: doc 11 **Master art direction**, **Environment construction rules**, **Anti-toy rules** and **Lighting and time of day**. Read them first; this playbook says how to build it.

## Principles (owner rules, 2026-10-03 / 2026-10-04)
- **Assemble, don't decorate.** Every structure is composed from kit modules (container blocks, sheds, lean-tos, platforms, catwalks, stairs, ladders, trusses, I-beams, pipe runs, vents, tanks, antennas, plates). A single box is never a building.
- **Break every silhouette.** Offset stacks, overhangs, extensions, pitched roofs, patch plates, damage, repairs, additions. Asymmetric by default; use the seeded `ArtRandom` for variation.
- **Verticality.** Elevated platforms, catwalks between structures, stacked containers, retaining walls, staircases, cables strung between poles, rooftop equipment.
- **Medium-frequency construction detail:** rebar, concrete seams, panel joints, vents, utility boxes, tanks, generators, transformers, machinery. Large and medium forms first; nothing that only reads at close range.
- **Density with purpose.** Work areas (crates, pallets, tarps, barrels, sandbags, debris, maintenance gear, abandoned vehicles) grouped where people would really work. No uniform random scatter.
- **Function reads at a glance.** Each facility kind has a distinct silhouette (power: stacks + tank farm + transformer yard; compute: antenna lattice + dishes + cooling; habitat: stacked homes, balconies, laundry, water tower; battery: canopy + racks + capacitor towers; turret: fortified gun tower).
- **Never a toy** (doc 11 anti-toy rules): real thickness and scale, round things round (16+ segments from ~0.25 m radius), draped tarps, recessed doors and small lit windows, faded small markings, value contrast over hue, no blur.

## How
- Kit modules live in `src/Deadswitch.Art/Models/Kit*.cs`; facilities in `Facilities.cs` compose them per level (each level adds modules, never just scales). Fix a defect in the shared kit part, not per asset.
- Materials: palette slots (`Geometry/Palette.cs`) + the salvage shader (`Resources/Shaders/SalvageCommon.hlsl`, one source for Unity and the preview). Keep base colors desaturated; let wear, AO and wetness carry the variation.
- Lighting: keyframes by game hour in `Resources/Base/BaseLook.json`. Unity (`BaseView.ApplyLight`, `PostFx`) and `tools/basepreview/page.html` blend them the same way.
- Verify every change headlessly and look at the images:
  - Overview: `dotnet run --project src/Deadswitch.Cli -- art export --layout Generator:5,ServerRack:5,LifeSupport:5,BatteryBank:5,Turret:5,None:0 --out artifacts/basepreview/show.json`, then `node tools/basepreview/render.mjs --scene artifacts/basepreview/show.json --hour 12` (also `--hour 19` and `--hour 23`).
  - Close-up of one plot: add `--target x,y,z --dist 22 --w 900 --h 900` (slot positions: `HubScene.SlotPosition`).
  - District / tiers: `art export --tier 2 ...`.
- Review checklist for every render: doc 11 avoid-list + anti-toy rules. Anything cartoon, toy-like, cube-like, blob-like, flat-colored, plastic or glowing-slab is a defect to fix before committing.
- Budget check (fully upgraded base, 2026-10-03): ~200k triangles total; facilities 6-18k each, bunker ~42k, surroundings ~62k. Re-check on device when a facility exceeds ~60k triangles.
