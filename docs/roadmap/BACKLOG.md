# Feature backlog

Ordered queue. The top `Ready` item is next (see `docs/agents/continue.md`). Status: `Ready`, `In progress`, `Done`, `Later`, `Blocked`.
Order follows pillar rank (AI relationship > Base & economy > Defense & offline > Offense) **after** the foundations every pillar needs.

| ID | Feature | Milestone | Status | Notes |
|----|---------|-----------|--------|-------|
| F-001 | Agent work loop: `continue` skill, `TASK.md`, backlog, quality bar, Linux gate, cloud session hook | M0 | Done | 2026-10-03 |
| F-002 | Balance config: every tunable in `SimConfig` sections, human-editable balance file, validation, config hash, CLI dump/override | M0 | Done | 2026-10-03, ADR-0007 |
| F-003 | Commands and typed event log: player input as tick-stamped commands, replay = seed + config + commands, event schema version | M0 | Ready | ADR-0003 |
| F-004 | Save/snapshot format: versioned binary snapshot, restore equals continuous run, corruption-safe writes | M0 | Ready | Needs ADR-0008 |
| F-005 | Economy core: facilities (generator, server rack, life support grid, battery, fuel depot), build/upgrade queue with timers, scaling upkeep, priority blackouts, fuel | M1 | Ready | SPEC-002 |
| F-006 | Pressure loop: corruption bands + effect hooks, OVERRIDE charges/cooldown, defense posture + garrison slots, raid damage model (±15% variance), offline penalty, mercy window, loss ledger, attack cap | M1 | Ready | SPEC-001 |
| F-007 | Balance scenario runner: scripted player profiles, 100 seeds x 30 days, loss ledger summary, soft-lock detection | M1 | Ready | M1 exit criterion |
| F-008 | Unity foundation: compile-check project, code-only bootstrap, SimHost (real-time ticking), save/load, offline catch-up, clock-cheat guard, settings (effects, motion, colorblind) | M2 | Ready | ADR-0004 |
| F-009 | Visual system: design tokens (USS), fonts, CRT layer (scanlines, glow, vignette, corruption flicker), motion helpers, headless UI preview tool | M2 | Ready | doc 11 |
| F-010 | Terminal HUD + command bar: always-visible essentials, animated readouts, energy sparkline, Base / Map / AI / Ops navigation | M2 | Ready | doc 08 s4 |
| F-011 | Living base schematic: procedural vector diorama of the Hub, facility states, activity, scars, build interaction | M2 | Ready | doc 11 |
| F-012 | AI advisor: line system with triggers, Coldness/Boldness dials, delegation levels, glitch text by corruption, 50 lines, the first lie | M2 | Ready | ADVISOR_VOICE |
| F-013 | Defense setup screen: posture, crew chips, AI Confidence readout, Set & Go | M3 | Ready | doc 10 s4 |
| F-014 | Battle report: panels, loss ledger, AI annotation, Verify (compute cost) | M3 | Ready | ADR-0003 |
| F-015 | Hidden project clock + Audit tool (Core Profile readout) | M2 | Ready | doc 10 s2 |
| F-016 | Module tree: trunk M1-M3 + first field (8 nodes), research timers | M2 | Ready | doc 10 s6 |
| F-017 | Boot sequence + opening flow (early protection, first hit) | M4 | Ready | doc 01 s7 |
| F-018 | Logout projection + local notifications (opt-in, AI voice) | M3 | Ready | ADR-0004 |
| F-019 | Remaining signatures: siege, virus, purge + warning ladder, vacation shield, tribute orders | M3 | Later | |
| F-020 | Factions + per-faction heat, world map | M5 | Later | |
