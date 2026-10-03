# 02 — Systems: Economy

## 1. Resource overview

| Resource | Role | Nature |
|----------|------|--------|
| **Energy** | Powers buildings, units, the AI core | Continuous flow with upkeep |
| **Fuel** | Consumable for vehicles, raids, outposts, operations | Consumed per use |
| **Compute** | The AI's processing power; spent on AI actions | Produced, spent, and limited |
| **People** | Operators, crew, workforce, settlers | Scarce, slow-growing, can be lost |
| **Time** | Main cost of everything (build, train, travel, research) | Timers |

> Energy and fuel are treated as one resource **family** (steady power and consumable fuel) to keep the economy simple.

## 2. Time (primary cost)

- **Short actions** (seconds–minutes): quick builds, scouting, fast raids.
- **Long timers** (hours–days): big builds, tier upgrades, module restoration.
- **Idle progress**: resources and training continue while away, up to a cap.
- **Compressed crisis time**: emergency events and attacks run on short, tense timers.

## 3. Energy and fuel

- **Continuous upkeep**: buildings and units drain energy over time. Growth demands more generation.
- **Fuel for operations**: vehicles, raids, and outposts consume fuel. Expansion is costly.
- **Risky power sources**: reactors give huge output but become targets and radiation hazards if hit.
- **Priority blackouts**: when energy runs short, systems shut down in an order the player sets. This is a planning decision, not a punishment.

Suggested power sources (draft): scavenged generators (fuel), solar fields (slow, safe, fragile), reactors (huge output, high risk), batteries (buffer).

## 4. Compute

Compute is the AI's lifeblood and the main driver of corruption. It works four ways:

1. **Energy → compute**: server racks burn energy to produce compute. Power limits your AI.
2. **Finite from ruins**: dead data centers hold rare compute. This pushes expansion and raids.
3. **Spend-per-action**: AI tasks cost compute like stamina, and it regenerates slowly.
4. **Overclock burst**: spend extra for a temporary power spike that adds corruption.

## 5. People

- **Slow passive growth**: the hub supports a limited population that regrows over time.
- **Hard cap**: population cap rises with hub tier and facilities (see open questions: no dedicated "living quarters" category was selected).
- **Not the main cost** — people are a strategic resource, not the everyday currency.

### People as a strategic sacrifice

People can be spent for larger gains. They can also be lost in attacks. Examples:

- **Forced labor surges**: temporary production boost that costs lives and loyalty.
- **Human cost of expansion**: founding outposts and tiering up costs people.
- **Neural cleansing**: spend people to scrub corruption (see doc 03).

Consequence rule: sacrifices and ruthless choices push the AI toward a colder personality (see doc 03).

### Crewing (people power machines)

People are required to operate military assets. Example: with **20 drones and 10 operators**, only 10 are fully player-controlled.

- Unmanned units can be run by the AI, but the AI is glitchy and can hurt you.
- The same rule applies to tanks and heavy vehicles.
- Founding an outpost requires people plus power and fuel.

## 6. Facilities (categories)

| Category | Contents |
|----------|----------|
| **Power & fuel** | Generators, reactors, fuel depots, solar fields |
| **AI core** | Server racks, cooling, compute farms, memory restoration chambers |
| **Military** | Barracks, drone bays, vehicle factories, turrets |

> A "living quarters" category was not selected. See open questions for how population cap and morale are handled.

## 7. Bases and outposts

- **Main hub** — your most valuable site. Losing it triggers severe consequences.
- **Outposts** — fragile satellites that extend reach, produce resources, and can be lost.
- **Portable core** — the AI core survives a lost base. You can relocate and rebuild with partial salvage.
- **Expanding tiers** — bunker → district → city → region. Larger tiers give more power, abilities, effects, and gains.

## 8. Keeping scaling hard (anti-snowball rules)

1. **Scaling upkeep**: a bigger base always drains more.
2. **Power-based enemy scaling**: threats scale with your tier and total strength, not only with time.
3. **Diminishing returns**: soft caps so stacking one strategy stops paying off.
4. **Tall-poppy effect**: visible strength raises faction heat and attracts attention.

## 9. Loss rules (economy side)

- Looting is capped. The player is never fully wiped of energy or compute in one hit.
- Buildings can be damaged, downgraded, or destroyed. Outposts can be lost.
- Casualties are permanent.
- Each attack type has a signature loss profile (see doc 04).

## 10. Open balancing tasks

- Starting values, rates, and caps for each resource per tier.
- Upkeep curve formula and enemy-scaling formula.
- Population cap source and growth rate.
- Fuel supply sources (zones, raids, trade).
