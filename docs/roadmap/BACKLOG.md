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
| F-018 | Remaining signatures: siege, virus, purge + warning ladder, vacation shield, tribute orders | M3 | Later | |
| F-019 | Factions + per-faction heat, world map | M5 | Later | |
| F-021 | Ruthless choices: forced labor surge, purge, sacrifice (raise Coldness, lower loyalty) | M3 | Later | doc 03 s1, doc 10 s1.3 |
| F-022 | Project climax: final 24h warning at Imminent, counterplay (purge core, silence the AI via OVERRIDE, cancel AI actions), betrayal / fork events | M3 | In progress | doc 03 s5-6, doc 10 s2 |
| F-023 | Tier 2 district: new plots outside the walls, visual evolution of the compound per tier, Tier 2 threats scaling | M3 | Later | doc 06 s2-3 |
| F-024 | Settings and accessibility screen: effect intensity, reduced motion, haptics, text scale, alerts; reachable from CORE | M4 | Ready | doc 08 s6, doc 10 (assists), quality bar |
| F-020 | Balance scenario runner: scripted profiles, 100 seeds x 30 days report | M1 | Later | fold into CLI |
