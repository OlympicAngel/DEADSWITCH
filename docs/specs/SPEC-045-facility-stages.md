# SPEC-045 appendix: facility stage plan (F-111, rule A.12)

- Status: Draft plan 2026-10-06, for owner review before any stage asset is built
- Parent: `SPEC-045-base-progression-and-scaling.md` (sections A-D); kit in `src/Deadswitch.Art/Models` (`KitModules`, `KitParts`, `Props`, `Military`, `Utilities`)

This is the per-kind visual plan that SPEC-045 A.1, A.12 and B ask for before assets. It sets silhouettes and what changes at each stage boundary. It does **not** set levels per stage, costs or effects: those come with the economy formulas (SPEC-045 rules 1-3, owner sign-off).

## Shared ladder (every kind follows it, in its own order)
| Stages | Build quality | Light and FX | What the player reads |
|--------|---------------|--------------|-----------------------|
| 1-2 | Improvised: open skid or tarp, loose cabling, no plinth | One bare lamp; poor-start fault FX (sputter, spark, smoke puff, flicker) | "It barely works" |
| 3-4 | Stabilized: concrete plinth, first enclosure, cable trays | Fault FX fades at 4; steady status lamp | "Fixed and housed" |
| 5-6 | Capacity: second core unit, support systems (cooling, tanks, transformer) | Practical floodlights, a status strip | "Bigger and organized" |
| 7-8 | Protection and control: armoured walls or sandbag revetment, control cab, service access | Controlled lighting, panel glow | "Military installation" |
| 9-10 | Integrated: clean housings, protected emissive conduits, AI cyan accents on control and status only | Brightest, restrained cyberpunk detail | "Top-tier, earned" |

Rules carried from SPEC-045: each stage must change silhouette or medium/large equipment (not colour, scale, decals or particles alone); the kind stays recognizable at gameplay zoom and when off or damaged; stages 9-10 never become neon-only.

## Per-kind stages
Signature = the shape that identifies the kind at every stage.

**Generator** (signature: engine block + exhaust stacks)
1 diesel genset on a skid under a tarp, one stack; 2 second genset, fuel drums, hand cable reel; 3 plinth + lean-to roof, cable tray; 4 container engine hall (today's L1 look); 5 radiator roof with fan, fuel tank over the roof; 6 transformer yard behind fence; 7 wind turbine mast; 8 control cab on the hall, ladder and railing; 9 third heavy stack, armoured hall cladding, sandbag revetment; 10 twin halls bridged by a gantry, shielded conduits glowing cyan to the core.

**Server Rack** (signature: rack cabinets + roof heat exchangers)
1 two cabinets under a tarp on pallets, fan box; 2 third cabinet, a car battery bank, cable mess; 3 container shell with door; 4 roof fans (today's L1); 5 second container, cable bridge; 6 split heat exchangers; 7 raised floor + cable trays + fire bottles; 8 security cage and camera mast; 9 armoured shell, louvered heat stacks; 10 sealed vault module with cyan status rails and a dish for uplink.

**Life Support Grid** (signature: water tank + habitat block)
1 tent with a water barrel and hand pump; 2 second tent, rain catcher sheet; 3 container bunkhouse; 4 elevated water tank (today's L1); 5 filtration skid with pipes; 6 second bunk container stacked; 7 greenhouse frame; 8 med bay container with red cross, ramp; 9 insulated cladding, HVAC units, walkway; 10 sealed habitat ring with airlock door and warm interior windows.

**Battery Bank** (signature: rows of cells + busbar)
1 car batteries on a bench under a tarp; 2 two benches, jumper cables; 3 cell racks on a plinth; 4 container cell rack (today's L1); 5 second row, inverter cabinet; 6 busbar frame to the hall; 7 thermal fans; 8 blast walls between rows; 9 armoured cell vaults; 10 capacitor towers with cyan charge meters.

**Turret** (signature: gun on a ring)
1 machine gun on a tripod behind sandbags; 2 sandbag ring, ammo crates; 3 timber platform; 4 steel ring mount (today's L1); 5 gun shield; 6 elevated tower base; 7 twin barrels, spotlight; 8 concrete pillbox base, ladder; 9 armoured cupola, rangefinder; 10 radar-directed mount with cyan target laser (off when unpowered).

**Reactor** (signature: dome + cooling stack)
1 sealed RTG casks on a pallet behind a fence; 2 cask rack with lead sheet shield; 3 small containment drum on a plinth; 4 dome (today's L1); 5 cooling tower; 6 second cooling tower; 7 coolant loop pipes and pumps; 8 control bunker with radiation signs; 9 gantry crane over the dome; 10 double dome with blast berm and cyan containment rings.

**Drone Bay** (signature: launch pad + hangar door)
1 crate workbench and a quadcopter; 2 painted pad, charging cable; 3 tent hangar; 4 container hangar; 5 launch rail; 6 second pad, parts racks; 7 antenna mast; 8 armoured hangar door; 9 roof launch deck; 10 automated launcher with cyan pad lights.

**Motor Pool** (signature: vehicle + ramp)
1 a buggy under a tarp, jerrycans; 2 tool rack, engine hoist; 3 inspection pit; 4 container workshop; 5 vehicle ramp; 6 second bay; 7 gantry crane; 8 armour plate racks; 9 armoured garage with roller door; 10 assembly line rails and a finished APC with cyan running lights.

**Solar Field** (signature: tilted panel rows)
1 three loose panels propped on bricks; 2 a row on a timber frame; 3 steel frame row; 4 two rows; 5 inverter cabinet; 6 three rows; 7 tracking mounts; 8 fenced field, cleaning walkway; 9 armoured inverter house; 10 raised array with cyan output meters.

**Fuel Depot** (signature: horizontal tanks + bund wall)
1 drum stack on pallets; 2 hand pump and jerrycans; 3 one horizontal tank on cradles; 4 bund wall; 5 second tank; 6 pump house; 7 vertical tank; 8 loading arm; 9 blast walls and fire suppression; 10 buried tanks with armoured manifold and cyan level gauges.

**Cooling Tower** (signature: tower with fan)
1 fan box and water barrel; 2 cooling coil on a frame; 3 timber slat tower; 4 steel tower with fan; 5 second fan; 6 basin and pumps; 7 tall hyperbolic shell; 8 pipe bridge to the server racks; 9 louvered armour; 10 twin towers with cyan plume lights.

**Memory Restoration Chamber** (signature: chamber + cable crown)
1 a terminal on a crate wired to a drive stack; 2 shielded drive cabinet; 3 small vault; 4 chamber pod; 5 cable crown; 6 cooling jacket; 7 second pod; 8 Faraday cage; 9 armoured vault doors; 10 restoration core with cyan data rings (violet for memory, per doc 11).

## State treatment (every stage)
- **Powered:** lamps, status strips, moving parts (fans, rotors, dishes) on.
- **Off or shed:** same geometry and damage; lights, emissives and motion off (existing `ArtBridge.Swap` and `StatusLights` path).
- **Damaged:** the existing scar set (`Scars.Facility`) on the stage's own bounds; damage glow rim. Never a shared ruin mesh.
- **Upgrading:** the current stage plus scaffold (`Facilities.Scaffold`); the next stage appears only when the job completes.
- Poor-start fault FX (stages 1-3) stop at the stage where the plan says the fault is fixed, not when damaged.

## Milestone label (SPEC-045 D)
At stage 10, the floating tag gains a framed backing, a DEADSWITCH chevron emblem and a rank line under the name. Name and real level stay visible ("GENERATOR // LVL 23"). Levels after the stage-10 threshold add a pip per milestone step (the step is set with the economy). No CSGO names, shapes or colours.

## Open for the owner
1. Levels per stage (2-4, per SPEC-045 A.4) wait on the level curve from the economy formulas.
2. Today's level-1 models sit at stage 4 of this plan. If stages 1-3 apply to new builds, existing saves would see their buildings "regress". Proposal: saves at format N keep a floor of stage 4 for facilities built before the change.
