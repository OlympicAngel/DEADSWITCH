# SPEC-014: Balance scenario runner

- Status: Done (F-020); tuning decision pending (see Findings)
- Pillar: all (tuning tool)
- Touches: CLI (`balance` command), Host `ScriptedPlayer` (research, tier-up, extra plots)
- Source rules: doc 10 s3-4 (placeholders, attack caps), `docs/agents/balance-tuning.md`

## Goal
Every number is a placeholder until runs say otherwise. One command plays many seeds under a few player profiles for a month of game time and prints a short report: how fast a handler progresses, how often raids land and hurt, whether anyone gets wiped or stuck in a blackout, and how far the AI's project gets. Guards turn the intended feel into pass/fail lines.

## Non-goals
Auto-tuning, charts, CI gating on soft targets.

## Rules
1. `balance [--seeds N] [--days N] [--profile active|casual|autopilot|idle|all] [--out PATH]`; defaults 100 seeds, 30 days, all.
2. Profiles: **active** (scripted handler every 20 min, present), **casual** (three 45-minute sessions a day at 08:00 / 13:00 / 20:00, Delegated routines, away otherwise), **autopilot** (Autopilot, always away), **idle** (no input, away).
3. Per profile: median and p10-p90 of Tier 2 day (and share reached), raids per day, breach share, people at end and minimum, blackout hours, corruption at end, project stage reached, climax share.
4. Hard guards (exit code 1 on failure): the daily attack cap is never exceeded; no active or casual run drops below `people_choices.min_people`; no active run spends more than 24 hours in one blackout.
5. Seeds run in parallel; results are ordered by seed, so the report is deterministic.

## Acceptance criteria
- [x] `balance` prints the report and guard lines; exit code reflects the guards
- [x] Scripted handler researches, tiers up and fills new plots
- [x] First report reviewed; findings noted in the corrections log or as tuning changes

## Tests
None (dev tool over the tested sim); the guards are the check.

## Findings (first run, 2026-10-03, 100 seeds x 30 days)
- **Tier 1 is far too short.** A competent handler (active) or Delegated routines (casual) clear every Tier 2 gate on day 2; all four tiers are done and every plot is maxed (90 levels) within about two weeks. Doc 10 implies Tier 1 runs for days (the Warlord Ultimatum at ~10 days). Cause: one day of Generator L1 output (~11.5k energy) buys a dozen levels; `levels_to_advance` (10) and M1 research (4 h) are cheap.
- **Autopilot stalls at 18 levels from day 2** (energy sits at cap; it neither researches nor upgrades further) while the project reaches Imminent and the climax fires in every run.
- **Raids:** the cap holds (guards pass). Breaches reach 95%+ once tier scaling kicks in for fast climbers; people are rarely lost (minimum 12 in every profile, from the opening raid).
- **Corruption ends at 0%** in every profile: decay outpaces gain at current values.
- Not retuned here: pacing, difficulty and the Tier 1 length are design decisions (see HANDOFF).
