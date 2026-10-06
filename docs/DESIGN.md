# DEADSWITCH: design

You are what is left of the war AI that ended the world. Rebuild a base in the ruins, grow an economy, and turn it into raw threat.

## Core loop
1. **Resources**: Scrip (money), Energy, Population. Energy and Population have storage caps (Battery Bank, Habitat Block); Scrip does not.
2. **Buildings** level up one at a time through a single build queue with a timer. Four kinds:
   - *Producers* make a resource for free.
   - *Converters* turn one resource into another. They throttle themselves when an input runs dry or a capped output is full, and can be paused.
   - *Storage* raises caps. A price above your cap shows amber: expand storage first.
   - *Unlockers* (Barracks, Armory, Fortification Works, Think Tank, Research Lab) open an Arsenal tab. Higher levels unlock more items and cut that tab's prices by 3% per level.
   - The **AI Core** caps every other building at `core level × 5` and unlocks new buildings.
3. **Arsenal**: buy unlimited units; each unit costs more than the last (`base × growth^owned`). Items raise the three factors:
   - **AI Power** (strike), **AI Defense** (survive), **AI Experts** (each one adds +0.5% to all production).
   - **Tech** items add percentage bonuses to production or factors.
4. **Threat index** = Power + Defense + Experts × 5. It sets your rank title, the long-term goal.

Offline time runs the same simulation (up to 7 days) and shows a report on return.

## Not built yet (next)
Enemies and raids that test Power vs Defense, random events, missions, and later online play. Factor values are already computed (`engine.factors`) for those systems to read.

## Where numbers live
All content and balance is in `js/data.js`. Run `npm run balance` to have a greedy bot play 24h and print milestone times after a change.
