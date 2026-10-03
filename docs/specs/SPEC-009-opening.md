# SPEC-009: Opening (prologue, boot sequence, first raids, guided first steps)

- Status: Done (F-016); Editor play check pending
- Pillar: AI relationship (first impression of the AI), Defense & offline
- Touches: raids (scripted opening raid, protection window, first-lie raid), UI (boot reveal, guide), advisor (boot lines)
- Source rules: doc 01 s7 (opening, first 30 minutes), doc 10 s7.4 (first lie in Tier 1), quality bar (no punitive pressure)

## Goal
The first minutes teach the game by playing it: a short prologue, the AI booting system by system, a raid already on its way that the handler beats by setting a defense, a calm protection window to build, then a real hit that hurts and carries the AI's first lie. A short objective guide keeps the next meaningful choice visible. Player value: momentum and understanding without walls of text.

## Non-goals
Voice-over, animated cutscenes beyond the drone feed, monetization, cloud accounts.

## Rules (numbers *(tune)* in `[opening]`)
1. **Opening raid:** in a new run, raid 1 spawns at minute `raid_at_minute` with fixed strength `raid_strength` (beatable with a Turtle garrison, survivable without), using the normal warning, gate report and resolution (no extra RNG draws).
2. **Protection:** no other raid spawns before hour `protection_hours`.
3. **First lie:** moves to raid `ai.first_lie_raid` (default 2: the first real hit). 0 disables it.
4. **Prologue:** five short typed cards over the drone feed, skippable at any time, shown once per new run.
5. **Boot sequence:** the HUD comes online in steps (power bus -> resource bar -> sensors/base -> advisor -> command bar), each with a boot log line; reduced motion shows the final state at once.
6. **Guide:** one objective at a time, derived from state (never stored): set a defense for the opening raid; build a Battery Bank; upgrade the Generator; start restoring a module; verify a battle report. Each shows a short reason and the control to use; completed objectives get an advisor line. The guide can be dismissed.

## Acceptance criteria
- [x] Opening raid at the scripted minute and strength; no other raid in protection; first lie on raid 2
- [x] Prologue and boot reveal on a new run only, skippable, reduced-motion safe
- [x] Guide objectives advance from real state

## Tests
`OpeningTests`: opening raid timing/strength and the protection window; first lie on the configured raid.
