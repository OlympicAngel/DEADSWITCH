# Feature backlog

Ordered queue. The top `Ready` item is next (see `docs/agents/continue.md`). Status: `Ready`, `In progress`, `Done`, `Later`, `Blocked`.
Order follows pillar rank (AI relationship > Base & economy > Defense & offline > Offense) **after** the foundations every pillar needs.
Note (2026-10-03): with F-001..F-017 done, F-022 (climax) is pulled forward because the project clock otherwise stalls at Imminent.

| ID | Feature | Milestone | Status | Notes |
|----|---------|-----------|--------|-------|
| F-001 | Agent work loop: `continue` skill, `TASK.md`, backlog, quality bar, Linux gate, cloud session hook | M0 | Done | 2026-10-03 |
| F-002 | Balance config: sectioned `SimConfig`, human-editable balance file, validation, config hash, CLI dump/check/diff | M0 | Done | 2026-10-03, ADR-0008 |
| F-003 | Commands and typed event log, replay | M0 | Done | 2026-10-03, ADR-0003 amendment |
| F-004 | Save/snapshot format + crash-safe save store (`Deadswitch.Host`) | M0 | Done | 2026-10-03, ADR-0009 |
| F-005 | Economy core: facilities, build/upgrade queue, power priority + shedding, crew, population cap | M1 | Done | 2026-10-03, SPEC-002 |
| F-006 | Pressure loop: corruption (milli-units, bands, automation load), OVERRIDE charges/cooldown, turrets + defense posture + garrison, raid strength vs defense (±15%), offline penalty, mercy window, loss ledger | M1 | Done | 2026-10-03, SPEC-001 |
| F-007 | Unity foundation: compile-check project, code-only bootstrap, SimHost (real-time ticking), balance file import, save/load, offline catch-up, clock guard, settings | M2 | Done | 2026-10-03 |
| F-008 | Visual system: design tokens (USS), fonts, CRT terminal overlay, motion helpers, headless UI preview tool | M2 | Done | 2026-10-03 |
| F-009 | Terminal HUD + command bar: always-visible essentials, animated readouts, energy sparkline, Base / Map / AI / Ops navigation | M2 | Done | 2026-10-03 |
| F-010 | 3D base diorama: heroic-realism compound (procedural geometry + procedural PBR salvage shader), drone-feed camera, slot selection, build/upgrade sheet, power/crew states visible | M2 | Done 2026-10-03 | ADR-0007, doc 11 |
| F-011 | AI advisor: line system with triggers, Coldness/Boldness dials, delegation effects, glitch text by corruption, 50 lines, the first lie | M2 | Done 2026-10-03 | ADVISOR_VOICE |
| F-012 | Defense setup screen (OPS): posture, crew chips, AI Confidence readout, delegation selector (Manual / Delegated / Autopilot), Set & Go | M3 | Done 2026-10-03 | doc 10 s4 |
| F-013 | Battle report: graphic-novel panels of rendered stills, loss ledger, AI annotation, Verify | M3 | Done 2026-10-03 | ADR-0003, ADR-0007 |
| F-014 | Hidden project clock + Audit tool (Core Profile readout) | M2 | Done 2026-10-03 | doc 10 s2 |
| F-015 | Module tree: trunk M1-M3 + first field (8 nodes), research timers, Tier 2 gate | M2 | Done 2026-10-03 | doc 10 s6 |
| F-016 | Boot sequence + opening flow (early protection, first hit) | M4 | Done 2026-10-03 | doc 01 s7 |
| F-017 | Logout projection + local notifications (opt-in, AI voice) | M3 | Done 2026-10-03 | ADR-0004 |
| F-018 | Remaining signatures: siege, virus, purge + warning ladder, vacation shield, tribute orders | M3 | Done 2026-10-04 | doc 04 s3+s5, doc 10 s4 |
| F-019 | World map: factions + heat, scout / raid / hack operations, outposts (absorbs F-031, F-032 basics, F-033) | M5 | Done 2026-10-04 | doc 05 s1-2+s6, doc 10 |
| F-021 | Ruthless choices: forced labor surge, neural cleansing, crackdown; loyalty and rogue operators (SPEC-012) | M3 | Done 2026-10-03 | doc 03 s1, doc 10 s1.3 |
| F-022 | Project climax: final 24h warning at Imminent, counterplay (purge core, silence the AI via OVERRIDE, cancel AI actions), betrayal / fork events | M3 | Done 2026-10-03 | doc 03 s5-6, doc 10 s2 |
| F-023 | Tier 2 district: new plots outside the walls, visual evolution of the compound per tier, Tier 2 threats scaling | M3 | Done 2026-10-03 | doc 06 s2-3 |
| F-024 | Settings and accessibility screen: effect intensity, reduced motion, haptics, text scale, alerts; reachable from CORE | M4 | Done 2026-10-03 | doc 08 s6, doc 10 (assists), quality bar |
| F-025 | Art direction v3: realism pass against the toy/miniature look, day/night lighting cycle, rebuilt kit primitives (owner, 2026-10-04) | M2 | Done 2026-10-04 | doc 11, ADR-0007 |
| F-027 | One-click Android dev build (Editor menu) | M5 | Done 2026-10-04 | unity/README |
| F-028 | Corruption effects: unmanned-unit glitches, defection, crisis ladder at Critical, core flush (SPEC-021) | M2 | Done 2026-10-04 | doc 03 s3-4 |
| F-029 | Battle scars: raid damage persists on facilities and the yard until repaired; repair action (SPEC-018) | M4 | Done 2026-10-04 | doc 06 s3, doc 10 |
| F-030 | Full module tree: four fields x 8 nodes (32), exclusive pairs, Tier 2 nodes | M5 | Done 2026-10-04 | doc 03 s7, doc 10 |
| F-031 | Outposts: small sites on the map that produce and can be raided | M5 | Done 2026-10-04 (in F-019) | doc 02 s7 |
| F-032 | Intel: spies in faction camps (double-agent risk, cross-check with scouts); scouts and AI prediction already in F-019 (SPEC-019) | M5 | Done 2026-10-04 | doc 05 s5, doc 10 |
| F-033 | Player offense: raids and cyber warfare against factions | M5 | Done 2026-10-04 (in F-019) | doc 04 s8 |
| F-034 | Living world: Warlord Ultimatum, dilemma events, trade, world events, faction wars (SPEC-017) | M5 | Done 2026-10-04 | doc 05 s3-4+s7, doc 10 s4 |
| F-035 | Audio pass: ambient dread, glitchy AI voice, silence before purge (procedural synth, AudioDirector) | M4 | Done 2026-10-04 | ROADMAP M4 |
| F-036 | Free demo (Tier 1) + premium unlock, rewarded ads (convenience only) (entitlements, tier gate, cosmetic themes) | M5 | Done 2026-10-04 | ADR-0006, doc 10 |
| F-037 | Short live battles: take command at contact, four abilities incl. OVERRIDE takeover and machine seizure, 3D fight at the gate (SPEC-020) | M5 | Done 2026-10-04 | doc 04 s7, doc 01 v1 scope |
| F-038 | The cycle: voluntary relocation, forced reboot, legacy score, perks, mastery challenges (SPEC-022) | M6 | Done 2026-10-04 | doc 06 s4, doc 10 s1.2 + s6 |
| F-039 | Ironman mode: chosen at a run start, no shield, shorter mercy, losing the core ends the run (SPEC-022 rule 7) | M6 | Done 2026-10-04 | doc 10 s1.2 |
| F-040 | Tiers 3 and 4: Stronghold yard and outer wall, Sector flank terraces, staked lots for the next tier, memory lane, gates and pacing | M6 | Done 2026-10-04 | doc 06 s2-3, doc 10 s1.3 + s2 |
| F-041 | Diplomacy: paid ceasefires, one at a time, broken by striking the faction (SPEC-023) | M6 | Done 2026-10-04 | doc 05 s3 |
| F-042 | Conquer and hold: a faction outpost beaten in a raid can be seized (energy, heat, breaks a ceasefire); it pays more than claimed ruins and its owner wants it back from Watched on (`[world]` seize/held keys) | M6 | Done 2026-10-04 | doc 04 s8, doc 01 v1.x |
| F-043 | Fourth faction: Halcyon Dynamics (corporate holdouts) with the Halcyon Vault and the Mercenary Compound; it raids from Tier 3 and has its own heat, spies, trades and ceasefire (`attacker_weight_holdouts_tier3`) | M6 | Done 2026-10-04 | doc 07, doc 01 v1.x |
| F-044 | Chapter arcs and the long mystery: one chapter per tier (villain, twist, payoff), twelve memory fragments that survive reboots, STORY screen (SPEC-024); the delegated AI and the scripted handler now build turrets when outgunned (`[chapters]`, `plan_defense_pct`) | M6 | Done 2026-10-04 | doc 07 s4 |
| F-045 | Alliances: a Cold faction mans the wall for a daily share, feeds its rival's grudge, and can walk out; dissolving or striking it costs heat (SPEC-025) | M6 | Done 2026-10-04 | doc 05 s3 |
| F-046 | Faction look: attackers in live battles and report panels wear their faction (Rustborn scavengers, Vanguard soldiers, Church robes with magenta signal lamps, Halcyon white armour with a cold visor); new emissive LampMagenta and LampCold | M6 | Done 2026-10-04 | doc 05 s8, doc 07 s5 |
| F-047 | Sabotage ops: a team of 1-3 cripples the owner's attacks for 48 h; traced or clean decides the heat; a loyal agent inside helps (SPEC-026) | M6 | Done 2026-10-04 | doc 04 s8, doc 05 s2 |
| F-048 | Season track (cosmetic): 20 ranks of XP from play, free and premium rewards (HUD themes Verdigris and Ash, AI voice packs Low Carrier and Static Choir, seven codex logs); never expires; SEASON screen from the premium screen | M6 | Done 2026-10-04 | doc 10 s1.1, ADR-0006 |
| F-049 | Leaderboard-ready runs: deterministic run verification (a save replays from its seed and commands; edits are caught), `RunSubmission` with the ranking fields, CLI `verify --save`, personal bests on the Legacy screen. Online boards and co-op still need a backend | M6 | Done 2026-10-04 | doc 08 s3 + phase 6 |
| F-050 | Adaptive enemies: factions learn the postures the Hub meets them with and counter them, and fortify sites the Hub keeps raiding; both fade; stated on OPS and the map (SPEC-027) | M6 | Done 2026-10-04 | doc 04 s9, doc 05 s2 |
| F-051 | Luck swings: hidden calm and restless streaks, AI hunches that are right only some of the time, and regrouping windows after a crushing defense that leave a faction's sites thin (SPEC-028) | M6 | Done 2026-10-04 | doc 04 s9 |
| F-052 | Reactor: Tier 3 power plant with huge output for fuel; scrams when dry, draws raiders, leaks radiation when cracked; new 3D model (SPEC-029) | M6 | Done 2026-10-04 | doc 02 s3 + s6 |
| F-053 | The AI acts without orders: a bold delegated AI launches its own raids at targets its estimate likes; the handler can recall them from the map (SPEC-030) | M6 | Done 2026-10-04 | doc 03 s5 |
| F-054 | Starting regions: relocation picks Hollow, Ridge (turrets), River (fuel) or Ruins (compute) for the next cycle; a forced reboot lands wherever the core can flee (SPEC-031) | M6 | Done 2026-10-04 | doc 06 s4 |
| F-055 | Hazard zones: radiation, plague and machine-graveyard zones with their own rewards and risks (scouting halves the risk); a fallout front drifts between sites (SPEC-032) | M6 | Done 2026-10-04 | doc 05 s6-7 |
| F-057 | 2.5D sector map rework (owner, 2026-10-04): fixed-tilt view over low-relief painted ruined terrain with the salvage shader; sites as small kit landmarks (outposts, data centers, ruins); hazard zones with their own ground (glowing crater, quarantine fence, wreck field); illustrated layer with faction territory tints, route lines, fog of war over unscouted ground, fallout drawn as a drifting haze; terminal UI stays an overlay for the site sheet and op controls; the AI's estimate labels glitch with corruption | M6 | Done 2026-10-04 (Editor check pending) | doc 05 s6, doc 11, ADR-0007 |
| F-056 | The AI builds in secret: a bold AI diverts skimmed resources into hidden structures that feed the project clock; Audit and cross-checks expose them, the handler can dismantle them (SPEC-034) | M6 | Done 2026-10-04 | doc 03 s5, doc 10 s2 |
| F-058 | Unit families and counters: Drone Bays and Motor Pools beside the garrison and turrets; every raid brings a faction-flavored mix of infantry, drones and vehicles; doc 10 counters decide how well each family holds (SPEC-035) | M6 | In progress | doc 10 s4, doc 04 s6, doc 02 s5-6 |
| F-099 | LAST: polish and balance pass (all tuning, visual detail, phone build checks), only after every feature is in (owner, 2026-10-04) | M5 | In progress: balance pass, ambushes, corruption visuals, HUD heat done; Editor and phone checks need the owner (no Unity in cloud sessions) | SPEC-014 |
| F-026 | Pacing retune: Tier 1 5-7 days, corruption from heavy compute use and proportional decay, launch max tier 2 (owner targets) | M1 | Done 2026-10-04 | SPEC-014, doc 10 corrections |
| F-020 | Balance scenario runner: scripted profiles, 100 seeds x 30 days report | M1 | Done 2026-10-03 | fold into CLI |
