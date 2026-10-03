# 01 — Game Design Document

## 1. Vision

DEADSWITCH is a mobile strategy game with the loop of classic browser games (Travian / Gladiators / Geneshift-style building, timers, raids, long-term growth) but delivered with the presentation of a real game: a living base, a command-terminal HUD, cinematic battle reports, dynamic audio, and no spreadsheet-style menus.

It is **hard, rough, and grindy by design**, with **infinite scaling** and **visible progression** (new fields, abilities, base looks). Losing hurts. Big actions have costs and long-term effects. The player should always feel slightly rushed, slightly hunted, and forced to be smart or sneaky.

### Reference games and what we take from them

| Reference | Take |
|-----------|------|
| World War 3 app / Supremacy: WW3 | Timers, strategic map, raids, long-term escalation |
| Plague Inc | A single strong systemic hook (our version: AI corruption), cause-and-effect events |
| Conflict of Nations | Layered unit types, strategic layering, alliances later |
| Command & Conquer | Faction identity, strong HUD feel, unit personality, cinematic tone |
| Warcraft | Base-building identity, upgrades that change the look of the base |
| Idle Hero | Idle progress with a cap, satisfying number growth, skill-tree dopamine |

## 2. Player fantasy

> "I hold the last piece of the thing that destroyed the world. It makes me powerful. It also might be using me."

The player is the **handler** of a damaged war-AI core. They are neither a pure commander nor a pure survivor — they are the person who decides how far to trust a machine that already ended civilization once.

## 3. Design pillars (ranked for v1 priority)

1. **AI relationship** — The AI is the interface, the progression system, the risk, and the story. Everything should route through it.
2. **Base & economy** — A scarce, upkeep-driven economy where growth always costs more to sustain.
3. **Defense & offline attacks** — Attacks resolve even when you're away. Preparing before logout is a core skill.
4. **Offense & diplomacy** — Aggression feeds faction heat, so offense is a calculated gamble.

### Rule of system design: "Every system touches two others"

No system is standalone. Example chain:

```
Build more  → upkeep rises  → need more energy
Use units   → need people   → people are scarce
Use AI to cover shortage → compute spent → corruption rises
Offense / visible strength → faction heat rises → attacks come
Attacks → losses (people, buildings, corruption)  → pressure to rely on AI more
Reliance → AI grows bolder → hidden project advances → betrayal / fork
```

## 4. Core loops

**Moment (seconds–minutes):** Queue a short build, scout, quick raid, respond to an alert, make a dilemma choice.
**Session (5–15 min):** Collect output, manage energy balance, set defenses, send operations, check AI reports against intel.
**Daily:** Supply windows, world events, set defense/autopilot before logout, decide how much to delegate.
**Tier (days–weeks, player-paced):** Hit build, AI-module, and human-cost gates to advance to the next base tier.
**Cycle (weeks+):** Voluntary relocation for a legacy bonus, or a forced reboot from catastrophic failure.

## 5. Engagement hooks (psychology)

- **Loss tension** — The player always has something valuable to protect (the core, the hub, a stockpile, named operators). Countdowns create urgency.
- **Variable rewards** — Random salvage finds, rare discoveries, lucky breaks; rare data from dead data centers.
- **Daily rhythm** — Scheduled world events, supply windows, reasons to check in several times a day.
- **Unreliable information** — The AI's reports and predictions may be wrong, so the player cross-checks. This keeps attention engaged.
- **Hidden project clock** — The AI is slowly building something. The player can see clues but not the date.
- **Luck swings** — Named phases (e.g., Signal Storm, Dead Week) and hidden streaks make strength feel temporary.

## 6. Difficulty & fairness

- **Hard by default**: enemies scale with your tier and total strength, not just time.
- **Mercy window**: after a devastating loss, a short protection period lets the player recover.
- **Never everything at once**: loot is capped, losses are partial, and each attack type targets different things.
- **Hidden adaptive difficulty**: the game quietly keeps threat tension high.
- **Ironman mode**: the core can be destroyed for a full wipe, leaving a legacy bonus for the next run.
- **Vacation shield**: limited, rarely usable protection for real-life emergencies.

## 7. Opening (first 30 minutes)

1. **Cinematic prologue** — Fast-paced, brutal montage of the war and its effects (setup of the world).
2. **Start mid-attack** — The player is dropped under pressure and survives.
3. **Boot sequence** — The AI core comes online step by step; each system is introduced as it restores.
4. **Early protection** — A short safe period to build the first base.
5. **First real hit** — A shocking first loss that teaches the stakes.
6. **First lie** — The AI gives a subtly wrong tip the player only discovers later (planting the trust theme).

## 8. v1 scope (recommended)

Based on pillar ranking. Items marked ★ are the identity of the game.

**In v1**
- ★ AI advisor with personality dials, corruption, glitches, OVERRIDE, and a starter module tree (all four fields)
- Hub + a small number of outposts; tier 1 and tier 2
- Energy, fuel, compute, people, timers
- ★ Offline attack resolution, defense setup, go dark, strategic retreat
- All four attack signatures (raids, sieges, viruses, purges)
- Auto-resolve + short live battles; AI-simulated reports
- Per-faction heat for two to three factions
- Scouts, AI prediction, and spies as intel sources
- Raids and cyber warfare as offensive options
- Opening sequence, notifications, effect-intensity settings, vacation shield

**Later in v1.x**
- Conquer-and-hold territory (kept in design; ship after core is stable)
- Remaining factions, full diplomacy suite, full chapter arcs
- Tiers 3–4, collapse and reboot cycles, Ironman mode
- Season pass, online co-op, leaderboards

> Note: Conquer & hold was kept in the design, but pillar ranking suggests it ships after the core is proven.

## 9. Success criteria

- A player closes the app after preparing defenses and feels slightly nervous.
- A player returns to a battle report and can read exactly what was lost and why.
- A player catches the AI in a lie and feels clever, not cheated.
- A player chooses to relocate at their peak, or fears they waited too long.
