# DEADSWITCH: design

You are what is left of the war AI that ended the world. Rebuild a base in the ruins, arm it, conquer the wasteland, and find out who you were. Your choices decide what you become.

## Core loop
1. **Economy**: Scrip (money), Energy, Population. Energy and Population have storage caps (Battery Bank, Habitat Block). Buildings level up one at a time through a timed build queue.
   - *Producers* make a resource for free. *Converters* turn one resource into another, throttle themselves on empty input or full output, and can be paused.
   - The **AI Core** caps every other building at `core level × 5` and opens new buildings and new chapters.
   - *Unlockers* (Barracks, Armory, Fortification Works, Think Tank, Research Lab) open Arsenal tabs; each level unlocks more and cuts that tab's prices 3%.
2. **Arsenal**: unlimited units at escalating prices (`base × growth^owned`) raise the three factors:
   - **AI Power** wins operations. **AI Defense** holds off raids. **AI Experts** add +0.5% to all production each. **Tech** adds % bonuses.
3. **Operations** (map): attack sectors adjacent to territory you hold. Odds are `P^4 / (P^4 + D^4)` (equal = 50%, double = 94%), shown before you launch and rolled when the operation lands. Wins give loot, a permanent bonus and a memory fragment (story). Losses kill 15% of Military Staff. Capturing a faction's capital stops its raids.
4. **Raids**: from AI Core 2, factions you have not beaten attack every 8 to 14 minutes with a visible countdown and strength. Strength tracks 30% of your Threat index, so an all-offense build gets punished. Losing costs 5 to 20% of stockpiles and 10% of staff, never buildings. While you are away at most one raid lands, then the timer waits for you.
5. **Events**: every 4 to 8 minutes a transmission arrives with a dilemma. No timer; decide when you like. Rewards scale with your production. Choices shift **Humanity** alignment (−100 Overlord … +100 Guardian), which grants scaling bonuses (Guardian: Population and Experts; Overlord: Power and Energy) and picks the ending.
6. **Directives**: 22 ordered goals that teach the game and pay rewards.
7. **Threat index** = Power + Defense + Experts × 5 sets your rank title, and raids scale with it.

## Story
Four chapters open with AI Core levels 1, 4, 6, 8: Scavenger Clans, Remnant Military, AI Cultists, Halcyon Dynamics. Each sector holds a memory fragment; together they reveal that Halcyon built the war to never end and built you as its trigger. Taking Halcyon Prime ends the story with one of three endings (Guardian, Overlord, Fork, chosen by alignment). After it, Rival Cores keep raiding as an endless mode.

## Feel
Dark military terminal look, typed story screens, animated battle reports (odds bar, roll needle, stamp), floating numbers, particle bursts, screen shake on defeat, WebAudio sound effects (mutable in settings).

## Where things live
- All content and numbers: `js/data/` (economy, world, story).
- Rules: `js/sim/` (economy, war, story) behind `js/engine.js`. Pure and seeded (`state.rng`), so offline catch-up and tests are reproducible.
- `npm run balance` has a bot play 24h and prints milestone times. Last run: chapter 1 done in ~45 min, Halcyon Prime around 24h of nonstop play.

## Next ideas
Rival AI players (online), unit upgrades and generals, sector garrisons that can be retaken, seasonal events, prestige reset ("reboot the core").
