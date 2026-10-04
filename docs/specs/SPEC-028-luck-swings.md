# SPEC-028: Luck swings

- Status: Done (numbers are placeholders, tune with play data)
- Pillar: Defense & offline
- Touches: raids (frequency and strength), the advisor (hunches), world ops (weaker sites), map (regrouping)
- Source rules: doc 04 s9 (hidden streaks, AI half-warnings, opportunity windows)

## Goal
The wastes should feel like weather: quiet stretches you can use and restless ones you brace for. The player feels
the luck but never sees the numbers, and reading the signs pays off.

## Rules (all numbers in `[luck]`, (tune))
1. Hidden streaks. Time runs in `streak_hours` windows. Each window is rolled calm (`calm_pct`), restless (`restless_pct`) or normal.
   - Calm windows stretch the time between raids by `calm_interval_pct`.
   - Restless windows shorten it by `restless_interval_pct` and add `restless_strength_pct` to raid strength.
   - The daily attack cap and the mercy window still hold.
2. AI half-warnings. At the start of a calm or restless window, the AI voices a hunch `hunch_pct`% of the time. The hunch names the real mood `hunch_accuracy_pct`% of the time, and the opposite otherwise. Moods are never shown as numbers.
3. Opportunity windows. A raid repelled with defense at least `regroup_margin_pct`% of its strength sends that faction regrouping for `regroup_hours`. During that time its sites are `regroup_defense_pct`% weaker. The map shows REGROUPING with a countdown, and the advisor points at it.
4. No RNG draws (hash rolls on the RNG state; the raid roll's draw count is unchanged). Save layout v24, event schema v24.

## Tests
The chunking, save and core-guarantee tests (daily cap) cover the streaks. Balance runner, 20 seeds x 30 days: raids per day unchanged (~2.8); casual-turtle breaches ~12%.
