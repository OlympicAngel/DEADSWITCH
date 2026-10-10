# Graph Report - DEADSWITCH  (2026-10-10)

## Corpus Check
- 61 files · ~83,793 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 4 file(s) not represented in the graph (top: (none) 2, .css 1, .webmanifest 1)

## Summary
- 833 nodes · 2626 edges · 43 communities (38 shown, 5 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 58 edges (avg confidence: 0.84)
- Token cost: 140,167 input · 1,241 output

## Community Hubs (Navigation)
- Host, Clock and Persistence
- Economy Simulation
- UI Shell and Effects
- World and Clan Data
- War Simulation: Sectors and Assaults
- Story, Events and Directives
- Balance Data and Release Rules
- Developer Panel
- Story Content and Cut Scene Canon
- Command Screen and DOM Patching
- Map Screen and Minimap
- Modals and Tag Formatting
- Engine Core: Step, Save, Migrate
- Audio Engine and Cut Scene Score
- Domain Screen and Shop
- Map Layout Generator
- Clan Stances and Traits
- Adaptive Ambient Music
- Cut Scene Staging
- Sound Design Canon
- Events as Orders and Humanity
- Static Hosting and Service Worker
- Seeded RNG Streams
- AI Spoken Voice
- Tooltips
- Icon Sprite Pipeline
- Package Scripts
- Core Loop and Operations Design
- AI Stats and Threat Index
- Number Formatting and Icons
- Raids and Breaches
- Chapter and Ending Modals
- Smoke Test Harness
- Arsenal, Siege and Unlocks
- In-Game Tutor
- Lessons and UI Copy Rules
- AI Core, Converters and Chapters
- Living Map Pressure Design
- Phone-First Build Gate
- Map Layout Helpers
- App Icon Artwork
- Command Screen Screenshot
- Map Screen Screenshot

## God Nodes (most connected - your core abstractions)
1. `createModals()` - 68 edges
2. `createUI()` - 65 edges
3. `icon()` - 64 edges
4. `num()` - 40 edges
5. `esc()` - 37 edges
6. `pct()` - 35 edges
7. `level()` - 33 edges
8. `createDebug()` - 28 edges
9. `say()` - 25 edges
10. `put()` - 24 edges

## Surprising Connections (you probably didn't know these)
- `Watch Daemon (Offline Catch-Up Limit)` --semantically_similar_to--> `Host Owns Clock, Save and Browser`  [INFERRED] [semantically similar]
  docs/DESIGN.md → AGENTS.md
- `The Map In Place Of An Icon` --semantically_similar_to--> `Name Things With Their Icon`  [INFERRED] [semantically similar]
  docs/DESIGN.md → AGENTS.md
- `Aggression (AGGR, Hidden Grudge)` --conceptually_related_to--> `Facts-Only UI Copy`  [INFERRED]
  docs/DESIGN.md → AGENTS.md
- `Pressure Feedback (Alarm, Vibration, Vignette)` --conceptually_related_to--> `Battery Animation Budget`  [INFERRED]
  docs/DESIGN.md → AGENTS.md
- `PWA and Mobile Metadata` --conceptually_related_to--> `Offline Play and PWA Install`  [INFERRED]
  index.html → docs/DESIGN.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Enemy Pressure Model** — docs_design_clan_profiles, docs_design_stances, docs_design_aggression, docs_design_living_sectors, docs_design_assaults, docs_design_raids, docs_design_rallying [EXTRACTED 1.00]
- **Synthesised Audio Stack** — docs_design_audio_engine, docs_design_effects_cues, docs_design_adaptive_music_bed, docs_design_ai_speech_voice, docs_design_cut_scene_score, agents_synthesised_sound [EXTRACTED 1.00]
- **Engine / Host Boundary** — agents_pure_engine_rule, agents_host_owns_clock, docs_design_presence, docs_design_catchup, docs_design_saving, docs_design_seeded_roll_hash, js_engine, js_main [EXTRACTED 1.00]

## Communities (43 total, 5 thin omitted)

### Community 0 - "Host, Clock and Persistence"
Cohesion: 0.05
Nodes (51): css/style.css, catchUp (Whole Absence In One Piece), Platform Features (Persistent Storage, Wake Lock, Web Share), Presence (Leaving and Arriving), Watch Daemon (Offline Catch-Up Limit), App Shell and Module Entry Point, keepAwake(), needsAwake() (+43 more)

### Community 1 - "Economy Simulation"
Cohesion: 0.08
Nodes (51): ITEMS, ALIGNMENT, alignBonus(), bonusBreakdown(), bonusTotal(), buffBonus(), buildingCost(), buildingStatus() (+43 more)

### Community 2 - "UI Shell and Effects"
Cohesion: 0.09
Nodes (49): BUILDINGS, setAttr(), setCls(), setFocus(), burst(), flash(), floatText(), reduced() (+41 more)

### Community 3 - "World and Clan Data"
Cohesion: 0.07
Nodes (34): AGGR, CHAPTERS, CLANS, FACTIONS, MAP, NODES, OPS, RAIDS (+26 more)

### Community 4 - "War Simulation: Sectors and Assaults"
Cohesion: 0.13
Nodes (39): clanProfile(), clanProfiles(), factors(), odds(), rand(), advanceAssaults(), advanceNodes(), advanceOp() (+31 more)

### Community 5 - "Story, Events and Directives"
Cohesion: 0.11
Nodes (30): grant(), grossRate(), loseUnits(), lossPlan(), unitsLost(), pick(), advanceEvents(), applicable() (+22 more)

### Community 6 - "Balance Data and Release Rules"
Cohesion: 0.08
Nodes (21): Conventional Commits, BALANCE, CONVERSION, FACTOR_KEYS, FACTORS, ITEM_BY_ID, RANKS, RESOURCE_KEYS (+13 more)

### Community 7 - "Developer Panel"
Cohesion: 0.13
Nodes (29): Developer Panel (Five Tabs), inspect(), worldRows(), createDebug(), build(), dataBody(), focusBody(), perch() (+21 more)

### Community 8 - "Story Content and Cut Scene Canon"
Cohesion: 0.08
Nodes (19): The AI's Spoken Voice (Speech Synthesis), BOOT Opening Cut Scene, Cut Scenes (CUTS, Inbox-Queued), BOOT, CHAPTER_TEXT, CORE_MEMORIES, CUTS, DIRECTIVES (+11 more)

### Community 9 - "Command Screen and DOM Patching"
Cohesion: 0.25
Nodes (24): pct(), time(), bonusText(), chanceClass(), clock(), setChips(), put(), putHtml() (+16 more)

### Community 10 - "Map Screen and Minimap"
Cohesion: 0.15
Nodes (25): ANCHOR, mapBackdrop(), node(), ring(), WINDOW, applyPan(), beam(), bindPan() (+17 more)

### Community 11 - "Modals and Tag Formatting"
Cohesion: 0.21
Nodes (27): esc(), num(), bonusChips(), factorTag(), resTag(), tags(), vibrate(), labeled() (+19 more)

### Community 12 - "Engine Core: Step, Save, Migrate"
Cohesion: 0.17
Nodes (20): BY_ID, LESSONS, EVENTS_CFG, catchUp(), migrate(), newState(), NUMBERS, SAVE_VERSION (+12 more)

### Community 13 - "Audio Engine and Cut Scene Score"
Cohesion: 0.18
Nodes (20): air(), audio(), DEFAULTS, impulse(), level, noiseBuffer(), out(), padChord() (+12 more)

### Community 14 - "Domain Screen and Shop"
Cohesion: 0.20
Nodes (21): costChips(), named(), reqText(), icon(), buildingCard(), BUY_MODES, effectHtml(), givesHtml() (+13 more)

### Community 15 - "Map Layout Generator"
Cohesion: 0.11
Nodes (20): n(), cap, CAPITAL, CH, cross(), deg, edges, FACTIONS (+12 more)

### Community 16 - "Clan Stances and Traits"
Cohesion: 0.17
Nodes (15): advanceClans(), CAPITAL_OF, CHAPTER_OF, clamp01(), clan(), clanAwake(), clanContext(), inRange() (+7 more)

### Community 17 - "Adaptive Ambient Music"
Cohesion: 0.22
Nodes (16): ambientMood(), duckAmbient(), moodOf(), MOODS, pulse(), pump(), read(), ROOTS (+8 more)

### Community 18 - "Cut Scene Staging"
Cohesion: 0.17
Nodes (15): block(), CITY, FAR_CITY, frameHit(), FROM, introStage(), playScene(), rnd() (+7 more)

### Community 19 - "Sound Design Canon"
Cohesion: 0.19
Nodes (12): Adaptive Music Bed (Moods, Tension, Weight), Audio Engine (Context, Reverb, Two Buses), Cut Scene Score (74 BPM Pulse and Pad), Effects Cues (A Minor Pentatonic), Pressure Feedback (Alarm, Vibration, Vignette), isMuted(), voice(), A (+4 more)

### Community 20 - "Events as Orders and Humanity"
Cohesion: 0.19
Nodes (11): Aggression (AGGR, Hidden Grudge), Border Orders, Damage Reports (Crisis Choices), Three Endings (Guardian, Overlord, Fork), Events Are Orders, EVENTS_CFG Price and Gain Multipliers, Humanity Alignment (Overlord to Guardian), Intercepts (ix_* Enemy Traffic) (+3 more)

### Community 21 - "Static Hosting and Service Worker"
Cohesion: 0.20
Nodes (6): Stage Site File Set, Offline Play and PWA Install, GitHub Pages Static Hosting From main, fireDue(), readDue(), SHELL

### Community 22 - "Seeded RNG Streams"
Cohesion: 0.27
Nodes (8): Alerts, Badge and Background Sync, peek (Read The Future Without Playing It), daySeed(), IDS, mix(), peek(), rollAt(), streamId()

### Community 23 - "AI Spoken Voice"
Cohesion: 0.32
Nodes (11): awakeHeld(), volume(), screen(), HARSH, OTHER_SCRIPT, pick(), say(), score() (+3 more)

### Community 24 - "Tooltips"
Cohesion: 0.35
Nodes (12): createTips(), alignTip(), bonusRows(), content(), factorTip(), hide(), mount(), place() (+4 more)

### Community 25 - "Icon Sprite Pipeline"
Cohesion: 0.20
Nodes (4): SPRITE, ICONS, Tabler Icons Bundled As SVG Sprite, symbols

### Community 26 - "Package Scripts"
Cohesion: 0.20
Nodes (9): engines, node, name, private, scripts, balance, serve, smoke (+1 more)

### Community 27 - "Core Loop and Operations Design"
Cohesion: 0.25
Nodes (9): DEADSWITCH Agent Contract, Keep Replies Short, The Archive, Clan Support (OPS.flankBonus), Core Loop, Memory Fragments, Operation Odds Formula P^4/(P^4+D^4), Operations (Sector Attacks) (+1 more)

### Community 28 - "AI Stats and Threat Index"
Cohesion: 0.28
Nodes (8): AI Defense, AI Experts, AI Power, Balance Simulation (3 Seeded 12h Runs), Command (The War Room), Threat Index, Top Bar and Floating Alerts, Troops (Military Staff)

### Community 29 - "Number Formatting and Icons"
Cohesion: 0.25
Nodes (6): rate(), SUFFIXES, whole(), ICON_LABELS, mountIcons(), updateTop()

### Community 30 - "Raids and Breaches"
Cohesion: 0.33
Nodes (9): range(), advanceRaids(), breaches(), fmtShort(), opTime(), raidTarget(), resolveRaid(), spawnRaid() (+1 more)

### Community 31 - "Chapter and Ending Modals"
Cohesion: 0.42
Nodes (9): bindName(), close(), cut(), finish(), pump(), showBoot(), showChapter(), showEnding() (+1 more)

### Community 32 - "Smoke Test Harness"
Cohesion: 0.22
Nodes (4): errors, require, server, TYPES

### Community 33 - "Arsenal, Siege and Unlocks"
Cohesion: 0.25
Nodes (6): Arsenal, Bottom Tabs and Paired Inner Tabs, Unit Durability, Escalating Price Formula, Threats and the Scheduled Siege, Unlockers (Arsenal Tab Openers)

### Community 34 - "In-Game Tutor"
Cohesion: 0.36
Nodes (7): setStyle(), createTutor(), advance(), centre(), finish(), frame(), scroller()

### Community 36 - "AI Core, Converters and Chapters"
Cohesion: 0.29
Nodes (3): AI Core (Global Level Cap and Chapter Gate), Four Chapters and Factions, Economy (Scrip, Energy, Population)

### Community 37 - "Living Map Pressure Design"
Cohesion: 0.38
Nodes (7): Assaults (Enemy Sector Attacks), Breaches and Overrun, Clan Profiles (CLANS Traits), Living Sectors (NODES Strength Drift), Rallying (RALLY), Stance Matrix (11 Stances), The Map (47-Sector Open World)

### Community 38 - "Phone-First Build Gate"
Cohesion: 0.40
Nodes (3): Smoke Job (Headless Chromium Gate), PWA and Mobile Metadata, npm run smoke

## Knowledge Gaps
- **93 isolated node(s):** `SAVE_VERSION`, `NUMBERS`, `SUFFIXES`, `timers`, `local` (+88 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 143 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **5 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Portrait Phone-First UI (390x844)` connect `Phone-First Build Gate` to `UI Shell and Effects`, `Map Layout Generator`?**
  _High betweenness centrality (0.063) - this node is a cross-community bridge._
- **Why does `createModals()` connect `Modals and Tag Formatting` to `Host, Clock and Persistence`, `UI Shell and Effects`, `Story Content and Cut Scene Canon`, `Command Screen and DOM Patching`, `Map Screen and Minimap`, `Audio Engine and Cut Scene Score`, `Domain Screen and Shop`, `Adaptive Ambient Music`, `Cut Scene Staging`, `AI Spoken Voice`, `Chapter and Ending Modals`?**
  _High betweenness centrality (0.040) - this node is a cross-community bridge._
- **Why does `icon()` connect `Domain Screen and Shop` to `UI Shell and Effects`, `In-Game Tutor`, `Balance Data and Release Rules`, `Developer Panel`, `Story Content and Cut Scene Canon`, `Command Screen and DOM Patching`, `Map Screen and Minimap`, `Modals and Tag Formatting`, `Cut Scene Staging`, `Tooltips`, `Number Formatting and Icons`, `Chapter and Ending Modals`?**
  _High betweenness centrality (0.040) - this node is a cross-community bridge._
- **Are the 7 inferred relationships involving `createModals()` (e.g. with `open()` and `openMenu()`) actually correct?**
  _`createModals()` has 7 INFERRED edges - model-reasoned connections that need verification._
- **What connects `SAVE_VERSION`, `NUMBERS`, `SUFFIXES` to the rest of the system?**
  _93 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Host, Clock and Persistence` be split into smaller, more focused modules?**
  _Cohesion score 0.05136986301369863 - nodes in this community are weakly interconnected._
- **Should `Economy Simulation` be split into smaller, more focused modules?**
  _Cohesion score 0.07985480943738657 - nodes in this community are weakly interconnected._