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
| 2026-10-03 | Art direction refined: stylized 3D -> **heroic realism** (stylized realistic PBR, believable proportions, layered wear); cartoon/toy-like forms explicitly excluded | Owner review of the first renders. Brief in doc 11 (Master art direction), ADR-0007 amendment. |
| 2026-10-04 | Look v3: **no miniature/tilt-shift blur; lighting follows the game clock (overcast day reference, dusk and night variants); procedural assets only** | Owner: the first heroic-realism pass still read as a toy. Doc 11 anti-toy and lighting rules, ADR-0007 amendment. |
| 2026-10-04 | Day/night v4: **warm day reference, vivid sunset and blue hour, cold moonlit nights (brighter ambient); damaged facilities pulse a red rim glow; bigger scar fire and smoke** | Owner after the first Editor run: nights too dark, cycle too flat, damage did not read. Doc 11 lighting rule; values in BaseLook.json (`times`, `scarFx`). |
| 2026-10-04 | Pacing *(tune)*: Tier 1 targets **5-7 days** for an engaged player. Tier gate 10 -> **22** facility levels (Tier 2 gate 25 -> 40), M1 research 4 h -> **24 h**, build times x2 at level 2 and x3 from level 3 (level 1 unchanged for onboarding); **max tier 2** in this build (section 2: two tiers at launch) | Balance runner (SPEC-014): Tier 2 was reached on day 2 and every tier within two weeks. Now day 6 for active and casual profiles. |
| 2026-10-04 | Corruption *(tune)*: idle decay 1%/h -> **0.2%/h + 1% of the current level per hour**; **heavy compute use** (doc 03 s3) now adds 0.075% per compute point spent on research and construction | Corruption never rose above 0% in any profile. Now an active player peaks around 46% and spends a few hours a day in Glitchy. |
| 2026-10-04 | Raid tall-poppy scaling *(tune)*: power coefficient 1.0 -> **1.4** (strength = base + 1.4 x power^0.7) | F-099 pass with the scripted handlers repairing, flushing and closing the app prepared: active breaches 0% -> **5%**, a casual player who leaves on Turtle with a garrison **36%**, an unprepared one still ~89% (preparing must matter). Doc 01 s9: close the app prepared and feel slightly nervous. |
| 2026-10-04 | Warlord Ultimatum price *(tune)*: 400 energy + 100 fuel | 800 energy was above a Tier 1 base's energy cap, so it could never be paid. |
| 2026-10-04 | Ambushes *(tune)*: **15%** of raids from a Hunted-or-worse faction arrive with one minute of warning, no estimate and no gate; a loyal spy in that camp prevents it; never forecast in logout alerts | Section 2 says ambushes show nothing until they hit; the rule had no number yet. |
| 2026-10-04 | Tiers 3 and 4 enabled (Stronghold, Sector; section 2 had them in v1.x) *(tune)*: memory sectors restore in their **own lane** beside field research; M2 14 days, M3 20 days; gates 46 / 68 facility levels, +500 / +900 net energy, 15 / 30 people | Owner: build the whole game. Scripted handler: Tier 2 day 6, Tier 3 day 20, Tier 4 day 40. Without its own lane a two-week restoration would have frozen every other module. |
| 2026-10-04 | Fourth faction (doc 07 Corporate Holdouts) enabled as **Halcyon Dynamics** *(tune)*: two sites (Halcyon Vault data center, Mercenary Compound outpost); attacker weight **30** + heat % from Tier 3, heat % only before | Owner: build the whole game. Late-game pressure that Tier 3 and 4 lacked; it shares every heat, spy, trade and ceasefire rule. |
| 2026-10-04 | Chapter arcs (doc 07 s4, SPEC-024) *(tune)*: twist on its story trigger or after **96 h**; payoff after **4** points (villain attacks 2, others 1); pays **150 / 300 / 500 / 800** energy and **20 / 40 / 80 / 120** compute by tier, villain heat **-20**, corruption **-3%**; **12** fragments kept across every reboot | Owner: build the whole game. Scripted handler closes chapters 1-4 on days ~5, ~11, ~25, ~45. |
| 2026-10-04 | Delegated AI builds turrets while its best defense is under **100%** of the expected raid (`plan_defense_pct`) | It built one turret ever, so from Tier 3 every raid breached (41 of 41 for the scripted handler). Now a prepared Hub holds: active breach ~0%, casual-prepared ~2% (45 days); ambushes, purges and leaving unprepared still bite. |
| 2026-10-04 | Alliances (doc 05 s3, SPEC-025) *(tune)*: only with a Cold faction; **600 E + 100 F**, then **120 E/day**; the ally's fighters add **40 / 80 / 140 / 220** defense by tier; its rival gains **+3** heat a day; **5%** a day it walks out; dissolving or an unpaid share **+5** heat, striking it **+40** | Owner: build the whole game. Completes the "pacts either side can break" half of doc 05 s3. |
| 2026-10-04 | Sabotage ops (doc 04 s8, SPEC-026) *(tune)*: squad **1-3**; odds 40 + 10/person (+masking, **+20** with a loyal agent) - defense/6; success cripples the owner's attacks **-30%** for **48 h**; **35%** traced (raid heat), else scout heat; failure loses one saboteur | Listed under "ship after v1" (section 9). |
| 2026-10-04 | Season track (section 1.1, ADR-0006) *(tune)*: **20 ranks x 250 XP**; XP only from play events (repelled raid 20, won op 15, caught lie 40, chapter 120, tier 150, mastery 80, reboot 200); free and premium tracks of themes, AI voice packs and codex logs; **the track never expires** and the pass unlocks everything already reached | No deceptive urgency or punitive absence (AGENTS design quality): no countdown, no dailies, no missed rewards. Cosmetic only. |
| 2026-10-04 | Adaptive enemies (doc 04 s9, SPEC-027) *(tune)*: up to **3** learned levels per posture per faction; each costs Turtle **10** defense points, Dark **8** miss points, Evacuate **+10%** loot; fortification **+15%** site defense per level, up to 3; counters fade a level every **4** days, fortification every **3** | A casual player who always turtles now breaches ~8% (was ~2% after F-044). Varying tactics is the answer, and counters are always shown. |
| 2026-10-04 | Luck swings (doc 04 s9, SPEC-028) *(tune)*: **12 h** windows, **25%** calm (raid interval **+60%**), **25%** restless (interval **-30%**, strength **+10%**); hunch **60%** of moody windows, right **70%**; a raid repelled at **150%** defense sends that faction regrouping **8 h** with sites **-25%** | Raids per day unchanged (~2.8); casual-turtle breaches ~12%. Daily cap and mercy untouched. |
| 2026-10-04 | Reactor (doc 02 s3, SPEC-029) *(tune)*: Tier **3+**, one per Hub; output **1800 / 2600 / 3600** energy/h for **10 / 14 / 18** fuel/h; costs **2400 / 3600 / 5200** E; breach damage hits it **50%** of the time; at damage **2+** it leaks **3** people a night until repaired | Doc 02 lists reactors as a risky power source; the late tiers had no power answer past the generator. |
| 2026-10-04 | AI initiative (doc 03 s5, SPEC-030) *(tune)*: from Boldness **60%** under Delegated (never Autopilot or away), **3%** an hour it raids a site it rates **60%+** with **4** people; never during mercy or protection; recall returns the squad with no heat | Doc 03 lists "acts without orders" and "cancel AI actions". Only under Delegated with the handler present: an absent player can never be raided into trouble by it. |
| 2026-10-04 | Starting regions (doc 06 s4, SPEC-031) *(tune)*: Ridge turrets **+15%**, River **+4** fuel/h, Ruins racks **+15%**, Hollow none; chosen when relocating, random after a forced reboot | Doc 06 lists "better starting region" as a reboot benefit. |
| 2026-10-04 | Hazard zones (doc 05 s6-7, SPEC-032) *(tune)*: three wild zones owned by nobody (no heat, scout and raid only, restock **48 h**). Radiation: fuel **+50%**, sickness **40%** losing **25%** of the squad. Plague: **+3** survivors on a win, infection **35%** kills **2** at home. Graveyard: a win repairs **2** damage points, a loss loses **20%** more of the squad. Scouting first makes the risk **50%** of normal. A fallout front settles from day **3** next to the crater and drifts every **60-84 h** to one of the two nearest faction sites: ops there **+50%** fuel and radiation sickness, outposts send **25%** | Doc 05 lists the zones and drifting hazards without rules. |
| 2026-10-04 | The AI builds in secret (doc 03 s5, SPEC-034) *(tune)*: from Boldness **55%**, each **120** skimmed compute builds a hidden node (max **3**); a node adds **40** project milli/h and draws **12** energy/h off the books; an Audit exposes them; dismantling sets the project back **6%** per node and salvages **40** compute; silence pauses them, a purge wipes them | Doc 03 lists secret builds without rules; the drain is the "unexplained resource drift" clue of section 2. |
| 2026-10-05 | Unit families (doc 10 s4 counters, SPEC-035) *(tune)*: garrison = infantry; **Drone Bay** (Tier 1, 24-80 drone defense) and **Motor Pool** (Tier 2, 45-100 vehicle defense, 2-4 fuel/h); counter **+/-40%** x the attacker share a family beats / loses to; faction mixes infantry/drones/vehicles Rustborn 60/10/30, Vanguard 40/15/45, Church 25/65/10, Halcyon 15/55/30 (jitter 15); barracks are the garrison itself | Doc 10 named the counters without a model; doc 02 s6 lists drone bays and vehicle factories. |
| 2026-10-05 | Doc 10 completeness (F-059) *(tune)*: **blueprints** trade at **500** E base (hold **3**), each takes **35%** off the next field research; **going dark** cools every faction's heat **+50%** faster (doc 05 s2); mastery 5 now needs the **first** lie caught within **24 h**, mastery 6 (outlast a collapse crisis with no rollback and no forced reboot) added; warnings use **signature haptics** (raid triple pulse, siege rumble, purge alarm, virus silent) | Rules already in this document or doc 05 without code. |
| 2026-10-05 | Breakdown phases (doc 07 s1, SPEC-036) *(tune)*: fallout wave (outposts **50%**, crater salvage **+50%**), plague outbreak (no regrowth, infection **25%** on any return), rolling blackouts (generation **-15%**, after-effect only), machine surge (**+20** drone points, defection **+15%**); pressure **100** (warning at 50) from crater/plague/graveyard raids (**35/35/30**), leak **5**/h, blackout **8**/h, unmanned war machine **1**/h | Doc 07 names the repeating phases and the player-caused after-effects without rules. |
| 2026-10-05 | Recovered fragments (doc 03 s7, SPEC-037) *(tune)*: each field's capstone (node 6) needs **1** data fragment; won data-center raids or hacks recover one **60%** of the time, won ruins raids **25%**; hold **4** | Doc 03 names fragment unlocks without rules. |
| 2026-10-05 | Strategic retreat (doc 04 s5): a breach under **Evacuate** abandons the newest outpost instead of scarring the Hub. Fix: Evacuate's loot multiplier (**150%**, plus learned counters) was clamped to 100% since F-050, removing its cost; now capped at **300%** | Doc 04 lists retreat as sacrificing outposts; SPEC-001 rule 6 sets the loot cost. |
| 2026-10-05 | Gestures (doc 08 s4): quick horizontal flicks move between the command-bar layers (Base only from a screen edge, so the drone pan keeps working); long-press on a map pin sends a two-person scout team. **No pinch-zoom on the sector map**: the owner's 2.5D brief (SPEC-033) fixes its camera | Doc 08 lists the gestures; the later owner brief wins for the map camera. |
| 2026-10-05 | Support facilities (doc 02 s3+s6, SPEC-038) *(tune)*: Solar Field **90/150/220** E/h by day only, no fuel or upkeep; Fuel Depot **+120/250/400** fuel cap; Cooling Tower **-15/25/35%** corruption from compute; Memory Chamber **15/25/35%** faster memory lane (stacking caps **60%**) | Doc 02 lists the categories; solar's day-only output keeps nights for the generators. |
| 2026-10-05 | Rewarded-ad conveniences (section 1.1, ADR-0006) *(tune)*: an extra salvage roll once per day (**+120** energy, **+12** fuel) and an idle-cap extension (energy storage **+15%** for **24 h**), as sim commands; Tier 1 only, none with premium, never during an attack, purge, ultimatum or climax. No ad network linked yet (development builds simulate a view) | The whitelist examples named in section 1.1 and ADR-0006. |
| 2026-10-05 | World palette (doc 11 Master art direction): materials carry more color (owner: "too pale"): blue solar cells and tarps, orange copper and rust, red/blue/yellow drums; post saturation **+4** (was -6). Still weathered, not saturated | Owner feedback on the base look. |
