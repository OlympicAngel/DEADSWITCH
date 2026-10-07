# DEADSWITCH: design

You are what is left of the war AI that ended the world. Rebuild a base in the ruins, arm it, conquer the wasteland, and find out who you were. Your choices decide what you become.

## Core loop
1. **Economy**: Scrip (money), Energy, Population. Energy and Population have storage caps (Battery Bank, Habitat Block). Buildings level up one at a time through a timed build queue.
   - *Producers* make a resource for free. *Converters* turn one resource into another, throttle themselves on empty input or full output, and can be paused.
   - The **AI Core** caps every other building at `core level × 5` and opens new buildings and new chapters. It can only be upgraded once the average level of all unlocked buildings (unbuilt count as 0) reaches 80% of the current cap.
   - *Unlockers* (Barracks, Armory, Fortification Works, Think Tank, Research Lab) open Arsenal tabs; each level unlocks more and cuts that tab's prices 3%.
2. **Arsenal**: unlimited units at escalating prices (`base × growth^owned`) raise the three factors:
   - **AI Power** wins operations. **AI Defense** holds off raids. **AI Experts** add +0.5% to all production each. **Tech** adds % bonuses.
3. **Operations** (map): attack sectors adjacent to territory you hold. Odds are `P^4 / (P^4 + D^4)` (equal = 50%, double = 94%), shown before you launch and rolled when the operation lands. Wins give loot, a permanent bonus and a memory fragment (story). Losses kill 15% of Military Staff. Capturing a faction's capital stops its raids. Sectors with several approaches (links from sectors closer to home) are fortified: +50% defense while you hold one approach, scaling linearly to 0 when you hold them all (`OPS.flankBonus`).
4. **Raids**: from AI Core 2, factions you have not beaten attack every 8 to 14 minutes with a visible countdown and strength. Strength tracks 30% of your Threat index, so an all-offense build gets punished. Losing costs 5 to 20% of stockpiles and 10% of troops, and triggers **damage reports**: crisis events where you choose what to lose (a building level, units, people). A rout triggers two. While you are away at most one raid lands, then the timer waits for you.
5. **Events are orders.** Every 6 to 12 minutes (only while you play) a transmission arrives and opens on screen. Each has a deadline (20 minutes for crises, up to 4 hours for story events); if it expires, I pick the listed default, which is usually the worst option. Many choices have real drawbacks: lost building levels, deserting troops, timed penalties, raids arriving sooner. Rewards scale with your production. Choices shift **Humanity** alignment (−100 Overlord … +100 Guardian), which grants scaling bonuses (Guardian: Population and Experts; Overlord: Power and Energy) and picks the ending.
6. **Directives**: 20 milestones that introduce each system once and pay rewards. They name a goal, never a strategy.
7. **Threat index** = Power + Defense + Experts × 5 sets your rank title, and raids scale with it.

## Story
Four chapters open with AI Core levels 1, 4, 6, 8: Scavenger Clans, Remnant Military, AI Cultists, Halcyon Dynamics. Each sector holds a memory fragment; together they reveal that Halcyon built the war to never end and built you as its trigger. Taking Halcyon Prime ends the story with one of three endings (Guardian, Overlord, Fork, chosen by alignment). After it, Rival Cores keep raiding as an endless mode.

## Interface
Portrait, phone first (desktop shows the same column).
- **Top bar**: AI Core level, rank, Threat, settings, and the three resources with storage bars and rates. An alert strip appears under it on every screen while a raid is inbound or orders are pending.
- **Bottom tabs**: Economy, Military, **Command** (centre), Research, Map. Inner tabs pair each building with what it unlocks: Military has Offense (Armory + weapons), Defense (Fortification Works + defenses), Troops (Barracks + staff); Research has Experts and Tech; Economy has Production (with the AI Core), Conversion, Storage; Map has Theater and Archive.
- Unlocked tabs and entries come first; only the next locked entry is shown, faded. A tab (or bottom-nav item) whose contents are all locked is locked itself; tapping any locked thing jumps to and highlights what unlocks it.
- **Command** is the war room: condition banner, Threat reactor with Power, Defense, Experts and Humanity around it, then incoming raid, outgoing operation, construction, pending orders, the current directive (with a Go button that jumps to and highlights the target), active effects and the system feed.
- Everything numeric explains itself on hover or tap: resources (sources, bonuses, time to full), factors, Threat, Humanity and every cost chip (shortfall, time to afford, storage limits).
- Pressure: alarm, vibration, shake and red flash when a raid is spotted; a red pulsing vignette in the final minute; vibrating, shaking battle reports on defeat. Sound and vibration can be turned off in settings.
- Icons: Tabler line icons (MIT), compiled into `js/ui/icon-sprite.js` by `tools/build-icons.mjs`.

## Where things live
- All content and numbers: `js/data/` (economy, world, story).
- Rules: `js/sim/` (economy, war, story) behind `js/engine.js`. Pure and seeded (`state.rng`), so offline catch-up and tests are reproducible.
- `npm run balance` has a bot play 24h and prints milestone times. Last run (with the Core gate): chapter 1 done in ~40 min, Core 6 at ~9h of nonstop play.

## Next ideas
Rival AI players (online), unit upgrades and generals, sector garrisons that can be retaken, seasonal events, prestige reset ("reboot the core").
