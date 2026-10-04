# SPEC-017: Living world (ultimatum, dilemmas, trade, world events)

- Status: Done (numbers are placeholders, tune at F-099)
- Pillar: Offense & diplomacy
- Touches: raids (Warlord wave), world map (heat, ops loot and odds), corruption, people
- Source rules: doc 05 s3-4+s7, doc 10 s4 (forced events, fairness), doc 10 s5 (trade)

## Goal
The world moves without the player: a warlord loses patience with a Hub that stays small, strangers bring offers with
hidden risks, factions trade and fight each other, and named world events change what is worth doing this day.

## Non-goals
Pacts, espionage and framing, blueprints as trade goods, drifting hazards on the map, spy double-agents.

## Rules (all numbers in `[living]`, (tune))
1. **Warlord Ultimatum:** still in Tier 1 on day `ultimatum_day`, Mother Kess (Rustborn) demands `ultimatum_energy` + `ultimatum_fuel` within `ultimatum_hours`. `PayUltimatum` pays it (Rustborn heat -`ultimatum_paid_heat_drop`). A tier-up before the deadline withdraws it. A standing tribute order pays at the deadline if it can. Otherwise a Warlord wave (attack kind Warlord, Rustborn, `ultimatum_strength_pct`% of a raid) spawns, waiting like the purge strike for the inbound attack, mercy, the shield and the daily cap. Once per run.
2. **Dilemmas:** one pending at a time, first at hour `dilemma_first_hour`, then every `dilemma_every_hours` + up to `dilemma_jitter_hours`. None is offered while the player is away. `ResolveDilemma(0|1)`: 0 takes the offer, 1 refuses. Unanswered after `dilemma_expire_hours` = refused with no penalty. Kinds: Trader (energy for fuel, `trader_trap_pct`% a trap), Refugees (+people, `refugee_spy_pct`% a spy: heat on the hottest faction), AI Shortcut (energy + compute now, corruption), Vanguard Deserters (+people and Vanguard heat, or hand back: Vanguard heat down), Church Signal (corruption down, `church_taint_pct`% tainted: up instead). Every risk is shown before the tap.
3. **Trade:** `Trade(faction, good)` buys one lot (fuel or compute for energy, energy cells for fuel). Price = base x `trade_price_pct_by_level`[heat level]; Supply Window takes `supply_window_price_pct`% off; a Marked faction refuses. `trades_per_day` per faction, reset at midnight. Gains clamp to caps.
4. **World events:** from day `world_event_first_day`, one every `world_event_every_hours` for `world_event_hours`: Signal Storm (hack odds -, corruption + per hour), Supply Window (op loot +%, cheaper trade), Dead Week (time between attacks +%). At each start the hottest faction cools by `faction_war_heat` and a rival warms by half (factions fight each other).
5. All decisions are hashes of tick and RNG state; RaidSystem keeps its four draws per tick. Save layout v10, event schema v10.

## Acceptance criteria
- [x] Ultimatum, dilemma, trade and world event visible and answerable in DISPATCH and the MAP site sheet; HUD chip with countdown.
- [x] Advisor lines for each event; logout projection alerts on the ultimatum.

## Tests
`ThreatTests.WarlordUltimatum_FiresOnce_ForAHubStuckInTier1_AndTheWaveResolves`; the chunked-month and core-guarantees tests cover determinism and caps.
