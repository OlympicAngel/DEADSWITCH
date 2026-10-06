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

**Generator** (signature: engine block + exhaust stacks) — reference build in `Facilities.GeneratorStage`, preview with `art export --layout Generator:s1,...,Generator:s10`
1 diesel genset under a blue tarp on timber poles, one thin stack, bare bulb, trodden ground (no slab); 2 second mismatched genset in the open, cable drum, crates; 3 concrete plinth, block back wall, lean-to roof over both sets, cable tray; 4 container engine hall with the genset bay and two stacks; 5 roof radiator with fan, fuel tank fed over the roof; 6 transformer yard behind a fence, day tank; 7 wind turbine mast; 8 control cab on the hall, ladder and railing; 9 heavy third stack with a warning lamp, sandbag revetment, armour plates over the genset bay, power pole with spotlight; 10 shielded conduit trench to the plot edge with a cyan line, cyan status lines on the control cab and transformer only.

**Server Rack** (signature: rack cabinets + antenna mast) — reference build in `Facilities.ComputeStage`
1 two racks on pallets under a blue tarp, box fan, barrel, cable reel; 2 third rack, car batteries on a bench, a short antenna pole, crates; 3 concrete plinth, block back wall, lean-to with a truss over four racks, two-fan chiller piped to the row; 4 server container with two racks under a lean-to, roof condensers, ground chiller, antenna mast; 5 four racks; 6 roof dish; 7 uplink container stacked on the hall, ladder and railing; 8 taller mast with six panel antennas and a second dish; 9 armour plates in front of the rack row, sandbag flank, camera pole; 10 cyan status rails on the rack row and uplink, shielded conduit to the plot edge.

**Life Support Grid** (signature: habitat block + laundry and water) — reference build in `Facilities.HabitatStage`
1 canvas tent, blue water barrel with a hand pump, laundry line, crates; 2 second tent, a tarp catching rain into barrels, workbench in the open; 3 med bay container on a plinth with a decked porch under a lean-to and the clinic sign; 4 roof services (header tank, solar water heater, AC); 5 bunk container stacked on top with a balcony and side stairs; 6 greenhouse tunnel in the yard corner; 7 elevated water tower; 8 rooftop shack, aerial, lamp string on the balcony; 9 sandbag wall across the yard front, armour shutters on the balcony, extra AC; 10 cyan status line under the balcony, shielded conduit, cyan porch lamp.

**Battery Bank** (signature: cell cabinets in a shed + capacitor towers) — reference build in `Facilities.BatteryStage`
1 car batteries on a bench under a blue tarp, junction box; 2 second bench, a salvaged panel propped on a crate, barrels; 3 slab and open cell shed with four cabinets, CELLS sign; 4 three roof panels, six cabinets, inverter and the first capacitor tower behind a fence; 5 four panels, eight cabinets; 6 five panels, ten cabinets, bus-bar gantry; 7 six panels, twelve cabinets, second tower; 8 fourteen cabinets, third tower, red gantry lamp; 9 blast wall between the cabinet rows, sandbags on the open sides; 10 cyan charge meters on every cabinet and rings on the towers, shielded conduit.

**Turret** (signature: gun on a ring) — reference build in `Facilities.TurretStage` (shares `TurretCore` with the level models)
1 machine gun on a tripod behind a short sandbag wall; 2 full sandbag ring, ammo crates, a tank trap; 3 octagonal concrete emplacement with the shielded pintle gun; 4 twin guns, spotlight pole; 5 steel gun tower with armour skirts and a seat box; 6 rotating search radar on a mast; 7 four guns, a third sandbag layer, a deck spotlight; 8 concrete ammo bunker with a blast door; 9 second ring of armour plates around the gun deck, extra tank traps; 10 AI fire control: a rangefinder housing with a cyan aiming line along the guns.

**Reactor** (signature: containment drum + cooling tower) — reference build in `Facilities.ReactorStage` (shares `ReactorCore` with the level models)
1 two finned RTG casks on a pallet behind a hazard fence; 2 four casks behind a lead-sheet shield; 3 a first small containment drum on a plinth, control hut, short exhaust; 4 full drum and dome, cooling tower, coolant loop, control room; 5 second smaller tower and a relief stack; 6 gantry crane over the dome; 7 coolant pump skid; 8 radiation placards and a sandbagged control entrance; 9 blast berm walls behind and beside the drum; 10 cyan containment rings on the drum, shielded conduit.

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
