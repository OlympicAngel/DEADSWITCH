# SPEC-024: Chapter arcs and the long mystery

- Status: Done (numbers are placeholders, tune with play data)
- Pillar: AI relationship
- Touches: tiers (a chapter per tier), raids (payoff), report verification and spies (twists), project, legacy cycle (fragments carry)
- Source rules: doc 07 s4 (chapter arcs per tier with a villain, a twist and a payoff; the long mystery in fragments), doc 03 (story-linked restoration)

## Goal
Each tier reads as a short story: a named villain pressing the Hub, a twist that makes the player look at the AI
differently, and a payoff earned by beating that villain. Each payoff recovers one memory fragment of the hidden
truth. Fragments survive reboots, so later cycles reveal what the first could not, and the mystery never fully closes.

## Rules (all numbers in `[chapters]`, (tune))
1. A chapter opens when the Hub reaches a tier it has no chapter for: 1 First Boot (Rustborn), 2 Foothold (Vanguard), 3 The Cult (Church), 4 Fork (Halcyon). Each reboot starts again at First Boot.
2. Twist, the first of these:
   - Chapter 1: a lie is caught by verifying a report.
   - Chapter 2: Vanguard heat reaches Watched, or a spy is inside Vanguard.
   - Chapter 3: Church heat reaches Watched.
   - Chapter 4: the project reaches `fork_project_milli`, or its final window opens.
   - Any chapter: `twist_fallback_hours` after it opened.
3. After the twist, each repelled or missed attack adds 1 point, or `villain_points` if the villain sent it. At `payoff_points` the chapter closes and pays `payoff_energy`[tier] and `payoff_compute`[tier] (capped by storage). It also lowers the villain's heat by `payoff_heat_drop` and corruption by `payoff_corruption_drop`.
4. A closed chapter recovers the first missing of its three fragments, numbered (tier-1)*3 + k. Twelve fragments in all. They are kept across every reboot, Ironman included, because the record outlives the core.
5. No RNG draws. Save layout v20, event schema v20. A save from before v20 opens the current tier's chapter on the next hour and starts with no fragments (earlier chapters are not granted retroactively).

## Tests
`ThreatTests.Relocation_CarriesTheLegacy_AndTheNewSiteSavesExactly` (chapter 1 pays a fragment; fragments survive the move; First Boot reopens).
