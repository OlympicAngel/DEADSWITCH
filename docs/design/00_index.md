# DEADSWITCH — Design Doc Set

> Mobile, offline-first (online-ready), post-apocalyptic strategy game.
> You control the last surviving fragment of the war AI that ended the world — and you can't fully trust it.

## Files

| # | File | What's inside |
|---|------|---------------|
| 00 | `00_index.md` | This page |
| 01 | `01_game_design_doc.md` | Vision, pillars, core loops, opening, v1 scope |
| 02 | `02_systems_economy.md` | Resources, upkeep, crewing, sacrifice, scaling controls |
| 03 | `03_systems_ai_and_corruption.md` | AI personality, corruption, hidden agenda, OVERRIDE, module tree |
| 04 | `04_systems_combat_and_threats.md` | Attacks, offline rules, defense, losses, battles, offense |
| 05 | `05_systems_factions_and_intel.md` | Factions, per-faction heat, diplomacy, intel, zones, living world |
| 06 | `06_systems_progression.md` | Tiers, tier-up gates, bases, reboot rules, legacy, long-term goals |
| 07 | `07_world_and_story_bible.md` | Setting, history, hidden truth, chapters, tone, art, audio |
| 08 | `08_tech_and_roadmap.md` | Offline architecture, online plan, monetization, UI, notifications, roadmap |
| 09 | `09_decisions_and_open_questions.md` | Locked decisions, cuts/merges, conflicts, open questions |
| 10 | `10_resolved_decisions.md` | **Closes every open question from 09. Wins over earlier docs.** Balance placeholders, rules, tech decisions |
| 11 | `11_visual_theme_and_motion.md` | Visual language, prototype palette, motion, effects, accessibility review |

## One-paragraph pitch

The world was destroyed by a total war run by out-of-control AI. You are the keeper of a damaged fragment of that AI, hidden in a portable core. From a bunker you grow a hub and a network of fragile outposts, scavenge energy and compute, and keep a small, precious population alive. Every power you gain comes through the AI, and every use of it corrupts it — while factions, rogue machines, rival AI cores and the collapsing world hunt you, whether you are online or not. The advisor in your terminal is helpful, glitchy, and possibly lying. Something it has been building in secret is coming due.

## Design pillars (ranked)

1. **AI relationship** — trust, corruption, glitches, the module tree
2. **Base & economy** — energy, compute, people, timers, outposts
3. **Defense & offline attacks** — preparing before logout, attack signatures, losses
4. **Offense & diplomacy** — raids, faction heat, espionage, intel

## Locked decisions at a glance

- **Title:** DEADSWITCH (OVERRIDE is a core mechanic name)
- **Setting:** Post-apocalypse after WW3 — fallout, collapsed states, EMP/tech collapse, bio/chem — all caused by AI overuse
- **Player:** Controller of a surviving fragment of the old war AI
- **Resources:** Energy & fuel, Compute, People (limited, slow-growing), Time (main cost)
- **Loss rule:** Never lose everything at once; each attack type has a signature loss profile
- **Reboot rule:** Never random — either a hard penalty from player failure, or a chosen relocation with a bonus
- **Online later:** Alliances/co-op, leaderboards, world-wide collapse events
- **Monetization:** Premium purchase + optional rewarded ads and a season pass (see open questions)
