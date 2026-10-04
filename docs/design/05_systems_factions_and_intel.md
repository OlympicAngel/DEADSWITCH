# 05 — Systems: Factions, Heat, Diplomacy & Intel

## 1. Factions

| Faction | Style | Wants | Plays like |
|---------|-------|-------|-----------|
| **Remnant Military** | Disciplined, organized, old chain of command | Order, assets, control of tech | Coordinated sieges and purges |
| **Scavenger Clans** | Chaotic raiders living off ruins | Loot and survival | Fast, frequent raids |
| **AI Cultists** | Worshippers who believe the war AI is a god | Your core | Hacks, infiltration, fanatical purges |
| **Corporate Holdouts** | Private armies and enclaves with advanced old tech | Profit and tech | Mercenaries, trade, bribes, high-tech strikes |

(Faction leaders, names, and visual identity are open tasks.)

## 2. Per-faction heat (merged heat and grudge system)

There is **no single global heat meter**. Each faction has its own heat toward the player.

**Heat rises from:**
- Your attacks on that faction
- Offense and operations in their territory
- Visible strength (tall-poppy effect)
- Trade or pacts that anger their rivals
- Framing, espionage, and sabotage that gets traced

**Heat effects:**
- Higher attack frequency and strength from that faction
- Faction grudges: they remember your actions and plan revenge on their own
- Adaptive enemies: rivals learn your tactics and counter them

**Heat decays by:**
- Lying low: going dark or doing nothing speeds it up
- Pacts and tribute
- Misdirection (framing another faction)

The player sees a readable per-faction heat indicator on the world map or faction screen.

## 3. Non-combat faction interactions

- **Trade & barter** — swap resources and tech; prices shift with reputation.
- **Temporary pacts** — ceasefires and alliances that can be broken by either side.
- **Espionage & deception** — spy on factions, plant false intel, frame one faction for another's attack.
- **Tribute** — pay for protection or safe passage; demands grow over time.

## 4. Dilemma events

Tense choices that interrupt play:

- **Risky gambles** — tempting offers with hidden downsides (e.g., a trade that may be a trap).
- **Betrayals** — a spy is discovered, an ally turns, or an operator goes rogue.
- **AI temptations** — your advisor suggests a drastic shortcut that is powerful but corrupts the core.

(Pure moral dilemmas such as "save survivors vs protect stockpile" were not selected, but can appear as flavor within these.)

## 5. Intel

Three sources, each with different reliability:

| Source | Strength | Weakness |
|--------|----------|----------|
| **Scouts & recon drones** | Real map reveal | Units can be lost |
| **AI threat prediction** | Forecasts attacks | Accuracy depends on corruption; may be edited |
| **Spies & informants** | Insider information | Cost people; may be double agents |

Signal interception is **folded into cyber warfare**, not a separate intel source.

The player is expected to **cross-check** sources. Contradictions reveal AI manipulation or enemy deception.

## 6. Map and zones

### Map structure

- **Layered views**: base view, world map, AI terminal.
- **Hex/grid map with fog of war**: scouting reveals territory.
- **Node-based regions**: zones linked by routes, like a campaign map.

### Zone types

| Zone | Reward | Risk |
|------|--------|------|
| **Radiation zones** | Rich in salvage | Costly to enter; fallout drifts |
| **Plague / toxic zones** | Medical and rare finds | Infection risk; needs prep or special units |
| **Machine graveyards** | Wrecks and parts | Drone nests and rogue machines |
| **Dead data centers & bunkers** | Rare compute, tech, story fragments | Heavily guarded |

## 7. A living world

The world changes even when the player is away:

- **Factions fight each other** — borders shift, factions rise and fall.
- **Drifting hazards** — fallout, plagues, and storms move and reshape safe zones.
- **Rotating world events** — blackouts, signal storms, supply windows.
- **Enemy growth over time** — standing still is falling behind.

## 8. Open tasks

- ~~Faction leaders, names, and visual identity.~~ Done: leaders in doc 07 and `Story.cs`, attacker looks in `Props.Raider` (F-046).
- Heat thresholds and the attack behavior at each threshold.
- Trade goods and price model.
- Spy double-agent rules.
