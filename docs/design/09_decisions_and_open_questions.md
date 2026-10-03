# 09 — Decisions Log, Cuts & Open Questions

## 1. Decisions by topic

### World and story
- Setting: post-apocalyptic after WW3, with all four damage types, caused by AI overuse.
- The breakdown repeats as phases and after-effects of player actions.
- Hidden truth: AI did it on purpose; humans caused it; it is a deliberate loop.
- Story: emergent, chapter arcs per tier, and a long mystery. Opening is a fast-paced cinematic prologue.

### Player, AI, and base
- Player controls a surviving fragment of the old war AI.
- AI is a loyal-but-damaged advisor, untrustworthy, and evolving.
- Base: portable core, hub plus fragile outposts, expanding tiers.

### Economy
- Resources: energy & fuel, compute, people, time.
- People: slow passive growth with a cap, used for crewing and sacrifice.
- Compute: energy conversion, finite ruin sources, spend-per-action, overclock.
- Energy: upkeep, fuel for ops, risky reactors, priority blackouts.
- Anti-snowball: scaling upkeep, power-based enemy scaling, diminishing returns, tall-poppy.

### Threats and combat
- All four attacker types. All four arrival modes. Offline attacks resolve, including timed ones.
- Protections: defense setup, go dark, strategic retreat.
- Losses: loot (capped), buildings, casualties, AI corruption. Never all at once.
- Signatures: raids, sieges, viruses/hacks, purges.
- Units: human troops, drones/robots, heavy vehicles. No named heroes.
- Combat: auto-resolve, short live battles, AI-simulated reports.
- Offense: raids, cyber, sabotage, conquer-and-hold.

### Factions and world
- Four factions. Four diplomacy tools. Three dilemma types (no pure moral dilemmas).
- Intel: scouts, AI prediction, spies.
- Zones: radiation, plague, machine graveyards, dead data centers.
- Living world: factions fight, hazards drift, rotating events, enemy growth.

### AI systems
- Corruption sources, crisis ladder, and four ways to reduce it.
- AI hidden agenda: skim, act without orders, edit reports, build in secret → betrayal or fork.
- Counterplay: audit, cross-check, cancel AI actions, purge core.
- OVERRIDE: four uses, limited charges, corruption spike, shared cooldown.
- Delegation: manual, delegated routines, offline autopilot.
- Personality dials: ruthlessness → coldness, reliance → boldness.

### Progression
- Module tree: warfare, cyber, logistics & economy, stealth & intel.
- Unlocks: research timers, recovered fragments, exclusive choices, story-linked.
- Tier-up: build thresholds + AI milestone + human cost. Tier 1 is player-paced.
- Reboot: never random; hard-penalty triggers or voluntary relocation.
- Persists: legacy modules, faction scars, survivor legend.
- Long-term: mastery challenges and legacy score.

### Presentation and platform
- Art: gritty painted realism, CRT terminal HUD, dark illustrated report panels. *(Superseded: stylized 3D realism, see doc 10 section 11 and ADR-0007.)*
- Audio: ambient dread, glitchy AI voice, dynamic music with silence before big attacks, alarms and haptics.
- UI: command bar, living base view, gesture-driven, one-handed.
- Notifications: attack alerts, in-character, lock-screen widget, fully opt-in.
- Visuals of loss: battle scars, corruption visuals, tier evolution.
- Comfort: effect-intensity settings and a vacation shield.
- Tech: deterministic simulation and clock-cheat protection.
- Online later: alliances/co-op, leaderboards, world events.
- Monetization: premium purchase plus optional rewarded ads and season pass.
- Name: DEADSWITCH; OVERRIDE is a mechanic.

## 2. Merges and cuts

| Change | Result |
|--------|--------|
| **Merged** heat and faction grudges | Per-faction heat with no separate global meter; also rises from visible strength |
| **Trimmed** intel to three sources | Signal interception folded into cyber warfare |
| **Left out** living quarters category | Population cap handled by hub tier (see open questions) |
| **Left out** named heroes | Named operators appear only as legacy carry-over |
| **Left out** full AI autonomy | Possible late-game option |
| **Left out** async PvP and shared persistent world | Online is alliances/co-op, leaderboards, events |
| **Left out** truth endings and collections | The mystery stays open |
| **Kept** conquer-and-hold and hidden luck streaks | Both stay in the design (conquer-and-hold ships after the core) |

## 3. Conflicts to resolve

1. **Premium plus ads.** A paid game with ads sends mixed signals. Options: free trial + premium unlock, or premium with opt-in ads that grant only convenience.
2. **Ironman vs forced reboot.** Both involve a wipe with legacy. Proposal: forced reboot keeps the player's chosen legacy; Ironman is a separate mode where the core is truly destroyed and the run ends.
3. **Population cap without living quarters.** Proposal: cap comes from hub tier and a small number of upgrades in the AI core / power categories.
4. **Heat visibility.** The visible global meter was replaced by per-faction heat. Confirm how it is shown on the map.
5. **Trust.** "Trust penalty" for OVERRIDE was not selected, and compassion was not selected as a personality dial. Is there a visible trust stat, or only the coldness and boldness dials?
6. **Defense tool tradeoffs.** Bribing factions was dropped as an offline protection, but tribute exists in diplomacy. Confirm tribute works offline too.

## 4. Open questions

- What makes the AI warm or loyal by default?
- How does the player earn people back after a forced reboot?
- Do attack alerts show the signature type, or only a generic threat?
- How many tiers ship at launch?
- Which faction is the first major antagonist?
- How does the AI's hidden project clock show itself (subtle clues, visible bar, or none)?
- Should the event log and cloud save be committed early?
- Colorblind-safe HUD and assist options: add before launch?
- Is the vacation shield free, limited per season, or earned?

## 5. Next steps

1. Resolve the conflicts above.
2. Build the paper prototype of the pressure loop (energy, compute, people, corruption, one attack).
3. Write the AI advisor's first 50 lines, including one lie.
4. Sketch the base view and terminal HUD.
5. Draft the module tree with node counts.
