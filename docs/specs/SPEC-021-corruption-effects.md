# SPEC-021: Corruption effects (glitches, defection, crisis ladder, core flush)

- Status: Done (numbers are placeholders, tune at F-099)
- Pillar: AI relationship
- Touches: economy (output), scars, raids (defection, swarm), modules (rollback), commands (takeover), AI planning
- Source rules: doc 03 s3-4, doc 10 corruption bands

## Goal
Corruption is felt, not just shown: the machines the AI runs start to misbehave, a broken core turns guns on the
Hub, and a Critical core can seize control, until the handler pays to flush it.

## Rules (all numbers in `[glitch]`, (tune))
1. **Glitches:** each hour, every AI-run (unstaffed) Generator, Server Rack or Turret glitches with `glitch_pct_by_band`[band]% (a crewed one at a third of that): Misfire (a battle scar on itself, friendly fire), Stall (stops `stall_hours`), Drain (burns `drain_energy`).
2. **Defection:** at Unstable or worse, `defection_pct`% of fights an AI-run turret is hijacked at resolution: its guns leave our defense and join the attack.
3. **Crisis ladder:** at Critical, hourly `crisis_pct_per_hour`% (then `crisis_cooldown_hours` quiet): Collapse (lose `collapse_energy_pct`% energy, producers stop `collapse_stall_hours`, and a breakdown phase starts: fallout wave, plague outbreak or rolling blackouts, SPEC-036), AI takeover (build, upgrade, research, posture, garrison and delegation orders refused for `takeover_hours`; the AI plans on its own), Forced rollback (a restored module locked `rollback_hours`), Rival swarm (a raid at `swarm_strength_pct`%; if fairness rules forbid an attack it becomes a takeover instead).
4. **Core flush** (`FlushCore`): `flush_energy` energy, corruption -`flush_milli`, ends a takeover; for `flush_hours` AI-run units stop and the AI predicts nothing. Not during an attack.
5. Hash decisions, no RNG draws. Save layout v14, event schema v14.

## Tests
`ThreatTests.CriticalCorruption_TriggersACrisis_AndAFlushPullsItBack`; core-guarantee and chunked-month tests.
