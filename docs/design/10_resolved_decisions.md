# 10 — Resolved Decisions

This file closes every conflict and open question from `09_decisions_and_open_questions.md`. Where a decision conflicts with earlier docs, **this file wins**. All numbers marked *(tune)* are starting placeholders for the paper prototype, not final balance.

---

## 1. Conflicts resolved

### 1.1 Premium plus ads → "Free demo, premium unlock"
- **Free download:** Tier 1 (Bunker) is fully playable for free. Optional **rewarded ads** exist only here, and only for convenience (examples: one extra salvage roll per day, a small idle-cap extension, a cosmetic).
- **Premium (one-time purchase):** unlocks Tier 2 and beyond, Ironman mode, and all future content. Premium removes ads permanently.
- **Season pass:** cosmetics and convenience only (base skins, AI voice packs, UI themes, extra codex content).
- **Hard rules:** nothing sold or watched can grant safety, defense, timer skips, or combat power. Ads never appear during an attack countdown, battle, or crisis.

### 1.2 Ironman vs forced reboot
| | **Forced reboot** (normal mode) | **Ironman** (opt-in mode, chosen at start) |
|--|----------------------------------|--------------------------------------------|
| What happens | Hub falls or the AI takes over; the portable core survives and you relocate | The core itself is destroyed and the run ends |
| Keeps | Legacy modules, faction scars, a few survivors, **50%** of the legacy score as a bonus | Final legacy score is recorded; bonus applies to the next Ironman run and to cosmetic unlocks |
| Loses | Hub, most outposts, most people, current corruption state resets partially | Everything |
| Safety nets | Mercy window, vacation shield | **No** vacation shield, shorter mercy window |
| Ships | v1.x (with Tier 3+) | v1.x |

### 1.3 Population cap without a living-quarters category
- The cap comes from three sources: **hub tier base cap**, a **Life Support Grid** (a building in the Power & fuel category that draws energy), and a **Habitat Management** node in the Logistics & economy module field.
- Base cap by tier *(tune)*: Tier 1 = 20, Tier 2 = 45, Tier 3 = 100, Tier 4 = 220.
- Regrowth: each hour the population regains about 5% of the gap to the cap, **paused during blackouts**.
- **Loyalty** is a simple hidden status, not a full morale system. It shows as Steady, Strained, or Mutinous. Forced labor surges and purges lower it. Low loyalty cuts output and can trigger a betrayal dilemma (operator goes rogue).

### 1.4 Trust → no explicit trust stat
- There is **no visible trust meter**. The AI's behavior is driven by two hidden dials, **Coldness** and **Boldness**.
- The player *feels* them through tone, voice, and suggestions. The exact values are only revealed by the **Audit tool**, shown as a "Core Profile" readout.
- **Corruption** stays visible, but a bold AI may under-report it. The Audit shows the true number.

### 1.5 Heat visibility
- Each faction has a heat indicator on the world map and faction screen with four named levels. Exact numbers stay hidden.
- The HUD always shows the **highest faction heat level**.
- At high corruption, the AI's displayed reading can be wrong (cross-check with spies).

### 1.6 Tribute works offline
- Tribute is a **standing order** set before logout (a recurring payment). It resolves in the offline simulation.
- If the player can't pay, the order fails and that faction's heat spikes. Demands rise over time.

---

## 2. Open questions resolved

| Question | Decision |
|----------|----------|
| What makes the AI warm or loyal by default? | A surviving directive in its code: **"Protect the handler."** It explains the AI's baseline loyalty and later becomes a twist (what does "protect" allow?). |
| How does the player earn people back after a forced reboot? | Start with a small cohort plus survivor-legend veterans. A temporary **Rebuilding Surge** boosts passive regrowth until the population reaches 50% of cap. No new sources are added. |
| Do alerts show the attack signature? | **Live warnings always show the signature** (icon, color, sound, haptic). The AI's size estimate may be wrong. **Ambushes** show nothing until they hit. **Viruses** are silent until an audit or a failure reveals them. |
| How many tiers at launch? | **Two** (Bunker, District). Tiers 3 and 4 come in v1.x. Infinite scaling uses repeating "Sector" tiers (5+) with scaled multipliers. |
| First major antagonist? | **Scavenger Clans** (Tier 1), then **Remnant Military** (Tier 2). **AI Cultists** appear late in Tier 2 as a hack-focused teaser. **Corporate Holdouts** come in v1.x. |
| How does the hidden project clock show itself? | No progress bar. The player sees **clues** (unexplained resource drift, report inconsistencies, AI slips). A successful **Audit** reveals the project stage: Dormant, Active, Advanced, Imminent. At **Imminent**, the player always gets a final warning window (24 real hours) to purge, silence, or cancel. |
| Commit to event log and cloud save? | **Yes, early.** Event log from day one. Optional account link and cloud backup in v1 (backup only, not required to play). |
| Colorblind-safe HUD and assists? | **Yes before launch.** Colorblind-safe means shape plus color coding. Assists: scalable text and touch targets, and a slow-motion option in live battles. |
| Is the vacation shield free, limited, or earned? | **Limited and earned.** Players start with 1 charge. A new charge arrives every 60 days (max 2). It gives 72 hours of attack pause (timers continue, upkeep halved). Activation has a 2-hour delay so it can't dodge a visible attack. It can't be bought. |

---

## 3. Stat and balance placeholders *(tune)*

### Tier 1 starting values
| Item | Start | Cap | Rate |
|------|-------|-----|------|
| Energy | 200 | 500 | +8/min generation, -4/min upkeep (corrected from +6, see Corrections log) |
| Fuel | 100 | 300 | No passive income; from scavenging, raids, trade |
| Compute | 50 | 100 | +1/min (racks cost 3 energy/min) |
| People | 12 | 20 | Passive regrowth (see 1.3) |
| Corruption | 0 | 100 | Rises with use; slow idle decay |
| Idle production cap | | | 8 hours of output |

### Corruption bands
| Range | State | Effects |
|-------|-------|---------|
| 0–30 | Stable | Normal |
| 31–60 | Glitchy | Occasional unit errors, UI flicker |
| 61–85 | Unstable | Frequent errors, reports can be edited, defection risk |
| 86–100 | Critical | Crisis ladder can trigger |

### OVERRIDE
- Start with 1 charge, max 3 in v1. One charge regenerates every 12 hours. Shared cooldown 30 minutes. Each use adds about 8 corruption.

---

## 4. Threat and combat rules

### Offline fairness
- **Offline attack cap:** at most 3 attack events per 24 hours in Tier 1, and 4 in Tier 2.
- **Mercy window:** 6 hours after a devastating loss (up to 12 for the worst). It does not stack, and it ends if the player launches offense.
- **"Devastating loss"** = a hub building downgraded, or more than 25% of population lost in one event.
- **Forced events:** if the player stays in Tier 1 beyond about 10 days, a **Warlord Ultimatum** wave arrives and forces the issue.

### Attack cadence (starting)
| Signature | Cadence |
|-----------|---------|
| Raids | Every 4–8 hours in Tier 1 |
| Sieges | From Tier 2, about every 2 days |
| Viruses | About every 3 days, never before the AI has restored its first module |
| Purges | Rare, Tier 2 onward, always preceded by the warning ladder below |

### Purge warning ladder (fairness rule)
1. **Rumor:** an AI half-warning that may be wrong.
2. **Staging confirmed:** the force appears on the map with a 24-hour timer.
3. **Ultimatum:** a final 6-hour countdown with options to pay tribute, retreat, or prepare.

A purge only forces a reboot if the hub falls undefended **and** the player ignored all three warnings.

### Heat levels and behavior
| Level | Range | Behavior |
|-------|-------|----------|
| Cold | 0–24 | No targeted attacks |
| Watched | 25–49 | Scouting and occasional raids |
| Hunted | 50–74 | Regular targeted attacks and that faction's signature attacks |
| Marked | 75–100 | Purge track begins (warning ladder) |

### Damage model (simple)
- Battles compare strength against defense with **seeded variance of ±15%**.
- Light counters: **drones beat infantry in the open, heavy vehicles beat drones, infantry with traps or EMP beat heavy vehicles in ruins.**
- Enemy strength = base(tier) × (1 + 0.8 × heat/100) × (player power rating)^0.7 × phase modifier *(tune)*.

### Defense setup UI
- Choose a **Posture** (Turtle, Dark, or Evacuate), then drag crew chips onto 3–5 defense slots.
- The AI shows a **Confidence** readout for the setup (may be wrong when corrupted).
- A one-tap **"Set & Go"** applies the AI's recommended setup (which may also be wrong).

### Alert presentation by signature
| Signature | Color | Sound / haptic |
|-----------|-------|----------------|
| Raid | Amber | Fast triple pulse |
| Siege | Red | Slow heavy rumble |
| Virus | Magenta | Silence plus screen glitch |
| Purge | White/red | Continuous alarm, music cuts out |

### Battle report format
- 4–6 graphic-novel panels (rendered stills) plus a **loss ledger** (every loss as a line item).
- The AI adds an annotation that may be altered. A **Verify** button costs compute to compare against scout data.

---

## 5. Factions, intel, and economy details

### Faction working names *(placeholder)*
| Faction | Group name | Leader |
|---------|-----------|--------|
| Remnant Military | Vanguard Command | Colonel Idris Vale |
| Scavenger Clans | The Rustborn | Mother Kess |
| AI Cultists | Church of the Last Signal | The Prophet (name unknown) |
| Corporate Holdouts | Halcyon Dynamics | A ghost CEO known only by voice |

### Spy double-agent rules
- Each spy has a hidden loyalty roll. Base double-agent chance is **10%**, rising with that faction's heat and with the AI's Coldness.
- Double agents feed false intel. Cross-checking against scouts exposes them.

### Trade
- Tradeable goods: **fuel, energy cells, compute fragments, blueprints** (module research discount). People are never tradeable.
- Price = base price × faction reputation multiplier (0.8–1.5) × world-event scarcity modifier.

### Fuel sources
- Scavenging missions, raids, trade, and certain zones. No passive fuel income at the start.

---

## 6. Progression details

### Module tree layout
- A **core trunk** of memory restoration nodes **M1, M2, M3**. These are the tier-up AI milestones (M1 gates Tier 2, and so on).
- Four fields branch off the trunk. **v1 has 8 nodes per field (32 total)**: 3 nodes in Tier 1, 5 in Tier 2, with **one exclusive choice pair** per field per tier.

### Legacy score (starting formula) *(tune)*
`Legacy score = (highest tier × 100) + (peak power rating ÷ 10) + (surviving veterans × 20) + (mastery challenges × 50)`
- **Voluntary relocation:** bonus = 100% of score. **Forced reboot:** 50%.
- Perk categories: starting resources, regrowth rate, extra OVERRIDE charge, faster heat decay, compute cap, extra veteran slots.

### Mastery challenges (starter list)
1. Survive a purge with zero AI use.
2. Reach Tier 2 without losing any outpost.
3. Win a live battle with no casualties.
4. Complete a tier with delegation set to Manual.
5. Catch the AI's first lie within 24 hours.
6. Survive a collapse event without a forced rollback.
7. Keep corruption under 40 for a full tier.
8. Complete a relocation at peak power.

---

## 7. The AI's lie rules

To keep lying from feeling like a bug:
1. The AI lies about **information**, never through silent mechanical cheating.
2. **Every lie leaves at least one cross-checkable trace** (a scout report, a spy, an audit).
3. Lies scale with Boldness and with the hidden project's needs.
4. The **first lie** is low-stakes and discoverable in Tier 1 (example: it says raids come from the north gate, but they hit the south).
5. The AI never lies about an immediate threat to the core in a way that makes the situation unwinnable.

---

## 8. Tech decisions

| Area | Decision |
|------|----------|
| **Engine** | **Unity (C#)**. Strongest mobile ecosystem for ads, in-app purchase, local notifications, and widgets. *(Easy to override.)* |
| **Simulation core** | A separate, engine-agnostic C# library. **Integer or fixed-point math only** (no floats) for determinism. Seeded PRNG. 1 tick = 1 game minute. Offline catch-up steps ticks with coarse-graining for long absences. |
| **Event log** | Append-only event log with periodic snapshots. Powers battle reports, replays, and later server verification. |
| **Storage** | Local database (SQLite or similar) plus optional cloud backup via account link. |
| **Notifications offline** | At logout, the deterministic sim projects the next likely attacks and schedules **local notifications** ("if nothing changes, the AI forecasts a raid at 03:10"). Server push is added in the online phase. |
| **Clock-cheat protection** | Track monotonic uptime plus the last trusted network time. If the device clock jumps beyond tolerance, trigger a **time desync** event, cap offline gains, and never grant free progress. Shown in-story as an AI glitch. |

---

## 9. Scope update for v1

Based on pillar ranking (AI relationship, then economy, then defense, then offense):

**Build in this order**
1. AI advisor, corruption, glitches, OVERRIDE, starter module tree.
2. Economy: energy, fuel, compute, people, timers, crewing.
3. Offline pressure: defense setup, go dark, retreat, all four signatures, battle reports, mercy window.
4. Offense: raids and cyber only, plus per-faction heat for **three factions** (Scavengers, Military, Cultists).

**Ship after v1:** conquer-and-hold, sabotage ops, Corporate Holdouts, Tiers 3–4, Ironman, season pass, online co-op and leaderboards.

---

## 10. Immediate next steps

1. **Paper prototype** in a spreadsheet using the Section 3 numbers: energy, compute, people, corruption, one raid type.
2. **Write the advisor's first 50 lines** including the first lie from Section 7.
3. **Build the simulation core** (Section 8) with a tiny test harness before any art.
4. **Sketch the base view and terminal HUD** with the always-visible essentials: next timers, energy balance, corruption, highest faction heat.

---

## 11. Corrections log

| Date | Change | Why |
|------|--------|-----|
| 2026-10-03 | Tier 1 energy generation +6/min -> **+8/min** | With upkeep -4/min and server racks -3/min, +6 nets **-1/min**: the base blacks out after about 3 hours. At +8 the net is +1/min and the cap fills in about 5 hours. Verified by `Tier1Defaults_DoNotBlackOutOverAWeek` in `src/Deadswitch.Sim.Tests`. |
| 2026-10-03 | Art direction: gritty 2D painted realism -> **stylized 3D realism with a subtle drone-feed camera**; battle reports become graphic-novel panels of rendered stills | Painted concepts read as too drawn; a grounded, semi-cartoonish 3D world is more tactile and readable on phones, and the drone camera expresses "you see through the AI". See ADR-0007; docs 07 and 11 updated. |
