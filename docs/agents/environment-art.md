# Playbook: Building environment art (procedural kit)

Use for any 3D world asset (facilities, bunker, props, terrain dressing) in `src/Deadswitch.Art`. Style source: doc 11 **Master art direction** and **Environment construction rules**. This playbook says how to build it.

## Principles (owner rules, 2026-10-03)
- **Assemble, don't decorate.** Every structure is composed from kit modules (container blocks, sheds, lean-tos, platforms, catwalks, stairs, ladders, trusses, I-beams, pipe runs, vents, tanks, antennas, plates). A single box is never a building.
- **Break every silhouette.** No straight rectangular outlines: offset stacks, overhangs, extensions, tilted roofs, patch plates, damage, repairs, additions. Asymmetric by default; use the seeded `ArtRandom` for variation.
- **Verticality.** Elevated platforms, catwalks between structures, stacked containers, retaining walls, staircases, cables strung between poles, rooftop equipment.
- **Construction detail at medium frequency:** rebar, concrete seams, panel joints, vents, utility boxes, tanks, generators, transformers, machinery. Large and medium forms first; nothing that only reads at close range.
- **Density with purpose.** Fill empty space with work areas (crates, pallets, tarps, barrels, sandbags, debris, maintenance gear, abandoned vehicles), grouped where people would really work. No uniform random scatter.
- **Function reads at a glance.** Each facility kind has a distinct silhouette (power: stacks + tank farm + transformer yard; compute: antenna lattice + dishes + cooling; habitat: stacked homes, balconies, laundry, water tower; battery: canopy + racks + capacitor towers; turret: fortified gun tower).
- **Geometry budget:** roughly 3-5x the old cube look. Merge statics per object; keep animated parts separate. Re-check frame cost on device when a facility exceeds ~60k triangles.

## How
- Kit modules live in `src/Deadswitch.Art/Models/Kit*.cs`; facilities in `Facilities.cs` compose them per level (each level adds modules, never just scales).
- Materials: palette slots + the salvage shader (`SalvageCommon.hlsl`). Worn edges come from bevel faces; give large flat areas seams, plates or stains.
- Verify every change with `dotnet run --project src/Deadswitch.Cli -- art export --days N` + `node tools/basepreview/render.mjs`, and review against the doc 11 avoid-list (no cube aesthetic, no empty ground, no plastic/flat look).
