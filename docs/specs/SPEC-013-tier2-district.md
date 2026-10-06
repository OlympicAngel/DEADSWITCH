# SPEC-013: Tier 2 District

- Status: Done (F-023); checked in the Editor 2026-10-06 (tier-up banner, four district plots, camera pans to the district wall, plot tap opens the build sheet)
- Pillar: Base & economy (growth), Defense & offline (threat scaling)
- Touches: tier-up (plots), raids (strength, daily cap), hub scene (district, terrain), base view and camera, art export
- Source rules: doc 06 s2-3 (tiers, visual evolution), doc 10 s1.3 (pop caps), doc 10 s4 (4 attacks per 24h in Tier 2)

## Goal
Tiering up should be seen and felt. At Tier 2 the compound breaks out of its fence: new plots on the apron outside the gate, an outer wall with its own gate and towers, and raids that hit harder. Player value: the expansion they paid people for is visible ground to build on, and the next defensive problem is legible (more to defend, stronger raiders). Before tier-up, the cleared lots outside the gate are already staked out: the next goal is on screen.

## Non-goals
Tier 3+ layout (v1.x), Remnant Military faction behavior (F-019), sieges and purges (F-018), battle scars.

## Rules (numbers *(tune)* in `[tier]`)
1. Tier-up appends `slots_added[tier-1]` empty plots (lowest power priority). Hub slots plus every addition stay within 64.
2. Raid strength is scaled by `raid_strength_pct[tier-1]`; the daily attack cap gets `extra_raids_per_day[tier-1]` (doc 10: 3 then 4). Tier 1 values are 100% and +0, so Tier 1 balance is unchanged.
3. District plots 6..9 sit on flattened pads outside the front fence, on both sides of the road, facing it.
4. Tier 1: the pads show as staked-out lots (survey stakes, tape, rubble). Tier 2+: an outer wall (jersey barriers, scrap palisade, sandbags), a second gate with a sign gantry, corner watchtowers and floodlight masts enclose them.
5. The base view rebuilds the surroundings and adds plot objects when the slot count changes; the drone camera can pan south over the district from Tier 2.

## Acceptance criteria
- [x] Tier-up adds plots; raids scale by tier; Tier 1 unchanged (existing tests green)
- [x] District visible in the headless preview at Tier 1 (staked lots) and Tier 2 (walled, plots built)
- [x] Unity: new plots appear and are selectable after tier-up without a restart

## Tests
`ModuleTests`: tier-up appends plots to slots and priority; raid strength and cap scale with tier.
