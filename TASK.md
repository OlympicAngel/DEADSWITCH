# TASK: F-106 Interface redesign

- Status: In progress
- Branch: feat/f106-ui-redesign
- Spec: `docs/specs/SPEC-046-ui-redesign.md`

## Steps
- [x] 0. Audit current screens in the Editor; research and spec (SPEC-046)
- [x] 1. Type and touch-target floor in tokens (11 dp floor, 44 dp touch); HUD chrome caps at the 115 step; pods, rail, legacy chip, world tags fixed at 100/115/130
- [x] 2. HUD v4 (part): pod ETA moves to the pod sheet; the objective keeps only its title off BASE. Merged objective+comms strip waits on the SPEC-046 open question (owner)
- [x] 3. Opaque surfaces (all screens but BASE, BATTLE, REPORT) and one-line headers (taglines hidden; data status right of the title)
- [x] 4. CORE decision-first: NEXT FROM YOU card (tier up, module ready, audit, restoring) above a smaller orb; empty transcript hidden. Primary actions already carry their cost in the label
- [ ] 5. Per-screen pass (OPS, WORKFORCE, STORY, SEASON, SETTINGS, GUIDE, LEGACY, DISPATCH, REPORT, BATTLE)
- [ ] 6. First-time task check at 720x1280 and 1080x2340

Previous: F-108 done; F-110 on `feat/f110-fog` (awaiting merge).
