# Feature backlog

Ordered queue. The top `Ready` item is next (see `docs/agents/continue.md`). Status: `Ready`, `In progress`, `Done`, `Later`, `Blocked`.
Order follows pillar rank (AI relationship > Base & economy > Defense & offline > Offense) **after** the foundations every pillar needs.

| ID | Feature | Milestone | Status | Notes |
|----|---------|-----------|--------|-------|
| F-001 | Agent work loop: `continue` skill, `TASK.md`, backlog, quality bar, Linux gate, cloud session hook | M0 | Done | 2026-10-03 |
| F-002 | Balance config: sectioned `SimConfig`, human-editable balance file, validation, config hash, CLI dump/check/diff | M0 | Done | 2026-10-03, ADR-0008 |
| F-003 | Commands and typed event log, replay | M0 | Done | 2026-10-03, ADR-0003 amendment |
| F-004 | Save/snapshot format + crash-safe save store (`Deadswitch.Host`) | M0 | Done | 2026-10-03, ADR-0009 |
| F-005 | Economy core: facilities, build/upgrade queue, power priority + shedding, crew, population cap | M1 | Done | 2026-10-03, SPEC-002 |
| F-006 | Pressure loop: corruption (milli-units, bands, automation load), OVERRIDE charges/cooldown, turrets + defense posture + garrison, raid strength vs defense (±15%), offline penalty, mercy window, loss ledger | M1 | Ready | SPEC-001 |
| F-007 | Unity foundation: compile-check project, code-only bootstrap, SimHost (real-time ticking), balance file import, save/load, offline catch-up, clock guard, settings | M2 | Ready | ADR-0004 |
| F-008 | Visual system: design tokens (USS), fonts, CRT terminal overlay, motion helpers, headless UI preview tool | M2 | Ready | doc 11 |
| F-009 | Terminal HUD + command bar: always-visible essentials, animated readouts, energy sparkline, Base / Map / AI / Ops navigation | M2 | Ready | doc 08 s4 |
| F-010 | 3D base diorama: procedural stylized-3D facilities (chunky bevelled forms, URP PBR materials), drone-feed camera (tilt-shift, subtle sensor fx), slot selection, build/upgrade sheet, power/crew states visible | M2 | Ready | ADR-0007, doc 11 |
| F-011 | AI advisor: line system with triggers, Coldness/Boldness dials, delegation effects, glitch text by corruption, 50 lines, the first lie | M2 | Ready | ADVISOR_VOICE |
| F-012 | Defense setup screen: posture, crew chips, AI Confidence readout, Set & Go | M3 | Ready | doc 10 s4 |
| F-013 | Battle report: graphic-novel panels of rendered stills, loss ledger, AI annotation, Verify | M3 | Ready | ADR-0003, ADR-0007 |
| F-014 | Hidden project clock + Audit tool (Core Profile readout) | M2 | Ready | doc 10 s2 |
| F-015 | Module tree: trunk M1-M3 + first field (8 nodes), research timers, Tier 2 gate | M2 | Ready | doc 10 s6 |
| F-016 | Boot sequence + opening flow (early protection, first hit) | M4 | Ready | doc 01 s7 |
| F-017 | Logout projection + local notifications (opt-in, AI voice) | M3 | Ready | ADR-0004 |
| F-018 | Remaining signatures: siege, virus, purge + warning ladder, vacation shield, tribute orders | M3 | Later | |
| F-019 | Factions + per-faction heat, world map | M5 | Later | |
| F-020 | Balance scenario runner: scripted profiles, 100 seeds x 30 days report | M1 | Later | fold into CLI |
