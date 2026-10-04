# TASK: F-055 Hazard zones and drifting fallout

- Status: In progress
- Started: 2026-10-04   Branch: claude/confident-heisenberg-m3gwju
- Pillar / milestone: Offense & world / M6
- Spec: docs/specs/SPEC-032-hazard-zones.md
- Sources: doc 05 s6-7, doc 07 (four kinds of damage), doc 10 s4 (heat, offline fairness)

## Goal
Three wild zones (radiation, plague, machine graveyard) give rewards the factions do not (survivors, repair parts,
rich salvage) at a zone-specific risk that scouting halves; a fallout front drifts between sites and reshapes which
routes are safe. Done = sim rules + config + saves, advisor lines, map UI wired, docs updated, gate green.

## Steps
- [x] 1. Spec, task, backlog and roadmap (stale boxes ticked)
- [x] 2. Sim: wild zones in the catalog, `[hazards]` config, zone rewards and risks, no faction effects (verification: test)
- [x] 3. Sim: fallout front (state, save v29, drift, fuel/sickness/outpost effects) (verification: test, chunking, CLI run)
- [x] 4. Host: advisor lines for the new events; balance file dump (verification: config check, advisor tests)
- [x] 5. Unity: map shows zones and the fallout front; site sheet states risk (verification: Unity compile check)
- [ ] 6. Docs (doc 10 corrections log, doc 05 open task, HANDOFF), finish feature

## Notes
- Owner direction (2026-10-04): continue gameplay work in the cloud; visual styling waits for the end (F-099).
- Owner queued F-057 (2.5D sector map rework) right after this feature; F-056 follows it.
- New events need no schema bump (only payload changes do); save layout v29.
- F-099 stays open (phone and Editor checks need the owner).

## Blocked / questions
- none
