# SPEC-015: Threat signatures and protection tools

- Status: Done (F-018); Editor play check pending
- Pillar: Defense & offline
- Touches: raids (attack kind), facilities (damage), modules (locks), corruption, presence, OPS / HUD / report / advisor / notifications, state v8, events
- Source rules: doc 04 s3-5, doc 10 s4 (cadence, purge warning ladder, alert presentation), ROADMAP M3

## Goal
Raids are only one of four threats. Each signature has its own loss profile and alert so the player learns to fear specific threats: **sieges** break buildings, **viruses** quietly corrupt the core, **purges** take everything but always announce themselves. Two tools make absence fair: a rare **vacation shield** and **tribute standing orders**. Player value: readable, distinct dangers with distinct answers (turrets and posture for sieges, compute reserve for viruses, the ladder's three choices for purges).

## Non-goals
Faction heat as the purge trigger (F-019; purges run on a schedule until then), forced reboot and relocation (v1.x: a purge on an undefended Hub that ignored all warnings is a devastating loss with mercy instead), live battles, battle scars visuals (F-029).

## Rules (numbers *(tune)* in `[threats]`)
1. **One incoming attack at a time**, with a kind: Raid, Siege or Purge. All share the daily attack cap, the mercy window, the AI's estimate and gate report, posture, garrison and OVERRIDE lockdown.
2. **Siege** (Tier 2+): due every `siege_interval_hours` (± jitter by hash); the next spawn roll becomes a siege. Longer warning (`siege_warning_minutes`), strength x `siege_strength_pct`, loot x `siege_loot_pct`. On a breach the strongest running facility loses one level per `siege_damage_per_breach_permille` of breach (at least one); a downgrade is a devastating loss (mercy window).
3. **Virus** (after M1 is restored): due every `virus_interval_hours`; strikes silently (no warning). Virus strength `virus_strength` + tier term. The core's firewall is its compute reserve: if compute covers it, the virus is burned off (compute spent, event logged). Otherwise: corruption +`virus_corruption`, a restored field module (picked by hash) is locked for `virus_lock_hours`, and the next raid estimate is false (x `false_intel_pct`).
4. **Purge** (Tier 2+, warning ladder, doc 10): due every `purge_interval_hours`. (1) **Rumor**: an AI half-warning, false with `rumor_false_pct` chance (a false rumor fizzles). (2) **Staging confirmed** `purge_staging_hours` before the strike. (3) **Ultimatum** `purge_ultimatum_hours` before: pay tribute (`purge_tribute_energy` + `purge_tribute_compute`, cancels the purge), retreat (Evacuate posture) or prepare. At the strike (waiting for any inbound attack, the mercy window, the protection window and the daily cap) the purge spawns with a short warning, strength x `purge_strength_pct`, and combines losses: full loot, garrison casualties plus `purge_population_pct` of the population, and two facility downgrades on a breach.
5. **Vacation shield** (doc 10): start with `shield_start_charges`, one more every `shield_regen_days` (max `shield_max_charges`). Activate with no attack incoming and no purge ladder running; it rises after `shield_delay_minutes` and pauses attacks for `shield_max_hours` (timers keep running, upkeep x `shield_upkeep_pct`). No attack or virus spawns and no purge ladder starts while it holds.
6. **Tribute standing order** (toggle): when a raid (not a siege or purge) arrives while away, pay `tribute_pct` of stored energy (at least `tribute_min_energy`) and it leaves without a fight. Logged as a loss line.
7. Alerts read by signature (doc 10): raid amber diamond, siege red square, virus magenta (silent: shown afterwards), purge white/red. The AI voice announces each; the virus may go unmentioned at high Boldness.

## Acceptance criteria
- [x] Sim: kinds, siege damage, virus outcomes, purge ladder with tribute, shield, tribute orders; state v8 round-trips; 4 RNG draws per tick kept
- [x] OPS threat card and HUD banner per signature; purge ladder card with the three answers; shield and tribute controls
- [x] Advisor lines, battle report titles, logout projection include the new threats

## Tests
`ThreatTests`: siege downgrades on a breach; virus burned by compute vs. infecting (lock + corruption); purge ladder stages and tribute cancels; shield blocks spawns and ends on return.
