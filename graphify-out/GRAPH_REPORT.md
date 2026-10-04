# Graph Report - DEADSWITCH  (2026-10-04)

## Corpus Check
- Large corpus: 361 files · ~530,797 words. Semantic extraction will be expensive (many Claude tokens). Consider running on a subfolder.

## Summary
- 3674 nodes · 9176 edges · 204 communities (145 shown, 59 thin omitted)
- Extraction: 89% EXTRACTED · 11% INFERRED · 0% AMBIGUOUS · INFERRED: 1024 edges (avg confidence: 0.81)
- Token cost: 171,215 input · 7,024 output

## Community Hubs (Navigation)
- Community 0
- Community 1
- Community 2
- Community 3
- Community 4
- Community 5
- Community 6
- Community 7
- Community 8
- Community 9
- Community 10
- Community 11
- Community 12
- Community 13
- Community 14
- Community 15
- Community 16
- Community 17
- Community 18
- Community 19
- Community 20
- Community 21
- Community 22
- Community 23
- Community 24
- Community 25
- Community 26
- Community 27
- Community 28
- Community 29
- Community 30
- Community 31
- Community 32
- Community 33
- Community 34
- Community 35
- Community 36
- Community 37
- Community 38
- Community 39
- Community 40
- Community 41
- Community 42
- Community 43
- Community 44
- Community 45
- Community 46
- Community 47
- Community 48
- Community 49
- Community 50
- Community 51
- Community 52
- Community 53
- Community 54
- Community 55
- Community 56
- Community 57
- Community 58
- Community 59
- Community 60
- Community 61
- Community 62
- Community 63
- Community 64
- Community 65
- Community 66
- Community 67
- Community 68
- Community 69
- Community 70
- Community 71
- Community 72
- Community 73
- Community 74
- Community 75
- Community 76
- Community 77
- Community 78
- Community 79
- Community 80
- Community 81
- Community 82
- Community 83
- Community 84
- Community 85
- Community 86
- Community 87
- Community 88
- Community 89
- Community 90
- Community 91
- Community 92
- Community 93
- Community 94
- Community 95
- Community 96
- Community 97
- Community 98
- Community 99
- Community 100
- Community 101
- Community 102
- Community 103
- Community 104
- Community 105
- Community 106
- Community 107
- Community 108
- Community 109
- Community 110
- Community 111
- Community 112
- Community 113
- Community 114
- Community 115
- Community 116
- Community 117
- Community 118
- Community 119
- Community 120
- Community 121
- Community 122
- Community 123
- Community 124
- Community 125
- Community 126
- Community 127
- Community 128
- Community 129
- Community 130
- Community 131
- Community 132
- Community 133
- Community 134
- Community 135
- Community 136
- Community 137
- Community 138
- Community 139
- Community 140
- Community 141
- Community 142
- Community 143
- Community 144
- Community 145
- Community 146
- Community 147
- Community 148
- Community 149
- Community 150
- Community 151
- Community 152
- Community 153
- Community 154
- Community 155
- Community 156
- Community 157
- Community 158
- Community 159
- Community 160
- Community 161
- Community 162
- Community 163
- Community 164
- Community 165
- Community 166
- Community 167
- Community 168
- Community 169
- Community 170
- Community 171
- Community 172
- Community 173
- Community 174
- Community 175
- Community 176
- Community 177
- Community 178
- Community 179
- Community 180
- Community 181
- Community 182
- Community 183
- Community 184
- Community 185
- Community 186
- Community 187
- Community 188
- Community 189
- Community 190
- Community 191
- Community 192
- Community 195
- Community 196
- Community 197
- Community 198
- Community 199

## God Nodes (most connected - your core abstractions)
1. `MeshBuilder` - 144 edges
2. `GameState` - 144 edges
3. `Command` - 114 edges
4. `EventKind` - 103 edges
5. `Deadswitch.Sim.State` - 101 edges
6. `Deadswitch.Sim.Config` - 71 edges
7. `Mat` - 69 edges
8. `Deadswitch.Sim.Systems` - 65 edges
9. `Simulation` - 63 edges
10. `GameHost` - 62 edges

## Surprising Connections (you probably didn't know these)
- `PartState` --references--> `AnimPart`  [EXTRACTED]
  unity/Assets/Game/Runtime/Base/BaseView.cs → src/Deadswitch.Art/Models/Model.cs
- `SlotObject` --references--> `SlotView`  [EXTRACTED]
  unity/Assets/Game/Runtime/Base/BaseView.cs → src/Deadswitch.Art/World/HubScene.cs
- `AdvisorVoice` --references--> `Advisor`  [EXTRACTED]
  unity/Assets/Game/Runtime/UI/Hud/AdvisorVoice.cs → src/Deadswitch.Host/Narrative/Advisor.cs
- `OpeningFlow` --references--> `GuideStep`  [EXTRACTED]
  unity/Assets/Game/Runtime/UI/Hud/OpeningFlow.cs → src/Deadswitch.Host/Narrative/OpeningGuide.cs
- `GameHost` --references--> `SaveFileStore`  [EXTRACTED]
  unity/Assets/Game/Runtime/Core/GameHost.cs → src/Deadswitch.Host/Persistence/SaveFileStore.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Agent Workflow System** — agents_md, task_md, docs_agents_handoff [EXTRACTED 1.00]
- **Agent Workflow System** — agents_md, task_md, docs_agents_handoff [EXTRACTED 1.00]
- **AI Relationship Pillar** — docs_narrative_advisor_voice, docs_specs_spec_004_ai_advisor, docs_specs_spec_007_project_and_audit, docs_specs_spec_011_project_climax, docs_specs_spec_012_ruthless_choices [EXTRACTED 1.00]
- **Base & Economy Pillar** — docs_specs_spec_002_economy_core, docs_specs_spec_003_base_diorama, docs_specs_spec_008_modules_and_tier, docs_specs_spec_012_ruthless_choices [EXTRACTED 1.00]
- **Defense & Offline Pillar** — docs_specs_spec_001_pressure_loop, docs_specs_spec_005_defense_setup, docs_specs_spec_006_battle_report, docs_specs_spec_010_notifications, docs_specs_spec_015_threat_signatures [EXTRACTED 1.00]
- **Simulation Determinism Flow** — src_deadswitch_sim, docs_adr_0002_deterministic_sim_core, docs_adr_0008_balance_config_file, docs_adr_0009_save_format [EXTRACTED 1.00]
- **Simulation Determinism Flow** — src_deadswitch_sim, docs_adr_0002_deterministic_sim_core, docs_adr_0008_balance_config_file, docs_adr_0009_save_format [EXTRACTED 1.00]

## Communities (204 total, 59 thin omitted)

### Community 0 - "Community 0"
Cohesion: 0.02
Nodes (100): EventKind, AdvisorLied, AiActed, AiHunch, AiSilenced, AllianceEnded, AllianceFormed, AllianceUpkeep (+92 more)

### Community 1 - "Community 1"
Cohesion: 0.05
Nodes (13): CrtOverlay, ArcGauge, Sparkline, Max, Min, Mesh2D, UiRoot, Crt (+5 more)

### Community 2 - "Community 2"
Cohesion: 0.11
Nodes (23): Facilities, AnimPart, Kind, Mesh, Pivot, Range, Speed, LightRole (+15 more)

### Community 3 - "Community 3"
Cohesion: 0.06
Nodes (12): Command, A, B, C, Kind, Command, Faction, Church (+4 more)

### Community 4 - "Community 4"
Cohesion: 0.08
Nodes (16): SimConfig, WorldEventKind, DeadWeek, None, SignalStorm, SupplyWindow, Operation, OpKind (+8 more)

### Community 5 - "Community 5"
Cohesion: 0.08
Nodes (15): ScriptedPlayer, LogoutProjection, Simulation, Commands, Config, Context, Log, Seed (+7 more)

### Community 6 - "Community 6"
Cohesion: 0.04
Nodes (52): RejectReason, AbilityUsed, AiTakeover, AllianceActive, AlreadyRestored, AlreadyVerified, Excluded, FactionHostile (+44 more)

### Community 7 - "Community 7"
Cohesion: 0.12
Nodes (7): Deadswitch.Sim.Systems, Deadswitch.Host.Online, Deadswitch.Sim.State, Deadswitch.Sim.Commands, Deadswitch.Sim.Config, Deadswitch.Sim.Events, Rates

### Community 8 - "Community 8"
Cohesion: 0.14
Nodes (7): CommandProcessor, CommandResult, Accepted, Ok, Reason, DefenseCommands, EconomyCommands

### Community 9 - "Community 9"
Cohesion: 0.09
Nodes (9): Deadswitch.Art.Models, Deadswitch.Host.Timing, Deadswitch.Host.Persistence, Deadswitch.Art.World, Deadswitch.Cli, Deadswitch.Host.Dev, Deadswitch.Art.Geometry, TestConfigs (+1 more)

### Community 10 - "Community 10"
Cohesion: 0.04
Nodes (48): com.deadswitch.art, com.deadswitch.host, com.unity.ai.navigation, com.unity.collab-proxy, com.unity.ide.rider, com.unity.ide.visualstudio, com.unity.inputsystem, com.unity.modules.accessibility (+40 more)

### Community 11 - "Community 11"
Cohesion: 0.04
Nodes (49): dependencies, com.deadswitch.art, com.deadswitch.host, com.deadswitch.sim, com.unity.ai.navigation, com.unity.collab-proxy, com.unity.ide.rider, com.unity.ide.visualstudio (+41 more)

### Community 12 - "Community 12"
Cohesion: 0.06
Nodes (34): Mat, Char, Concrete, ConcreteDark, Copper, DarkSteel, Ember, Foliage (+26 more)

### Community 13 - "Community 13"
Cohesion: 0.05
Nodes (38): args, extra, h, HERE, opt(), out, playwright, require (+30 more)

### Community 14 - "Community 14"
Cohesion: 0.11
Nodes (10): ByteReader, Position, Remaining, SaveGame, StateReader, IsReading, Version, SaveLoadException (+2 more)

### Community 15 - "Community 15"
Cohesion: 0.11
Nodes (4): LuckSystem, RaidSystem, SimMath, ThreatSystem

### Community 16 - "Community 16"
Cohesion: 0.04
Nodes (46): CommandKind, ActivateShield, Audit, Build, BuyPerk, CancelJob, CancelProject, CancelResearch (+38 more)

### Community 17 - "Community 17"
Cohesion: 0.13
Nodes (11): Deadswitch.Host.Seasons, Deadswitch.Game.Presentation, Deadswitch.Sim, Deadswitch.Game.UI.Screens, Deadswitch.Game.Core, Deadswitch.Host.Narrative, Deadswitch.Game.UI.Hud, Deadswitch.Game.Store (+3 more)

### Community 18 - "Community 18"
Cohesion: 0.09
Nodes (6): BattleFx, LiveBattle, Instance, SlowMotion, Raider, Tracer

### Community 19 - "Community 19"
Cohesion: 0.10
Nodes (7): Deadswitch.Game.Base, Deadswitch.Game.UI, Deadswitch.Game.Reports, Deadswitch.Game.Rendering, Deadswitch.Host.Notifications, Deadswitch.Game.Notifications, Deadswitch.Game.Audio

### Community 20 - "Community 20"
Cohesion: 0.09
Nodes (13): AdvisorTranscript, RecordedCommand, Tick, CommandLog, Commands, Count, Replay, ReplayDivergenceException (+5 more)

### Community 21 - "Community 21"
Cohesion: 0.05
Nodes (37): AllianceEnd, Betrayed, Dissolved, Distrust, Unpaid, Walkout, HeatLevel, Cold (+29 more)

### Community 22 - "Community 22"
Cohesion: 0.05
Nodes (37): ModuleNode, CY1, CY2A, CY2B, CY3, CY4, CY5A, CY5B (+29 more)

### Community 24 - "Community 24"
Cohesion: 0.09
Nodes (10): BalanceRunner, RunResult, Collector, Entries, ConfigEntries, ConfigEntry, Description, Key (+2 more)

### Community 25 - "Community 25"
Cohesion: 0.14
Nodes (3): HubScene, Noise, ArtExport

### Community 26 - "Community 26"
Cohesion: 0.08
Nodes (13): GameSettings, DevTimeScale, EffectIntensityPct, Effects, Haptics, Music, ReducedMotion, SoundPct (+5 more)

### Community 27 - "Community 27"
Cohesion: 0.06
Nodes (33): com.unity.burst, com.unity.collections, com.unity.ext.nunit, com.unity.mathematics, com.unity.modules.hierarchycore, com.unity.modules.subsystems, com.unity.nuget.mono-cecil, com.unity.render-pipelines.core (+25 more)

### Community 28 - "Community 28"
Cohesion: 0.09
Nodes (22): RaiderSpec, Position, Seed, Yaw, ReportScene, ShotSpec, Fov, Position (+14 more)

### Community 29 - "Community 29"
Cohesion: 0.17
Nodes (4): GameState, Economy, EnergySystem, ProductionSystem

### Community 30 - "Community 30"
Cohesion: 0.07
Nodes (23): BattleReport, ContactGate, Damage, DefenseRating, Estimate, Faction, Findings, Kind (+15 more)

### Community 31 - "Community 31"
Cohesion: 0.07
Nodes (18): AlertKinds, All, Construction, None, Power, Raids, Research, ProjectedAlert (+10 more)

### Community 32 - "Community 32"
Cohesion: 0.15
Nodes (3): AudioDirector, Instance, Synth

### Community 33 - "Community 33"
Cohesion: 0.10
Nodes (16): MeshBuilder, AoFloor, AoHeight, Cones, FaceJitter, GroundOffset, Mesh, Random (+8 more)

### Community 34 - "Community 34"
Cohesion: 0.23
Nodes (3): ArtRandom, Core, KitModules

### Community 36 - "Community 36"
Cohesion: 0.13
Nodes (5): FacilitySlot, IsEmpty, GlitchSystem, PeopleSystem, ScarSystem

### Community 37 - "Community 37"
Cohesion: 0.16
Nodes (5): Fmt, Texts, SlotSheet, IsOpen, Slot

### Community 38 - "Community 38"
Cohesion: 0.06
Nodes (28): CoreProfile, BoldnessMilli, ColdnessMilli, Skimmed, Stage, Tick, TrueCorruptionMilli, UnverifiedLies (+20 more)

### Community 39 - "Community 39"
Cohesion: 0.14
Nodes (8): Advisor, LastId, Pending, Age, FixedId, Priority, Trigger, Vars

### Community 40 - "Community 40"
Cohesion: 0.07
Nodes (30): dependencies, depth, source, version, dependencies, depth, source, version (+22 more)

### Community 41 - "Community 41"
Cohesion: 0.17
Nodes (5): TradeGood, Compute, EnergyCells, Fuel, LivingSystem

### Community 42 - "Community 42"
Cohesion: 0.14
Nodes (11): OpeningGuide, BuildJob, FacilityKind, BatteryBank, Generator, LifeSupport, None, Reactor (+3 more)

### Community 43 - "Community 43"
Cohesion: 0.08
Nodes (20): RunSubmission, Cycle, Fragments, HighestTier, Info, Ironman, LegacyScore, LegacyTotal (+12 more)

### Community 44 - "Community 44"
Cohesion: 0.10
Nodes (13): SeasonReward, Id, Kind, Name, Premium, Rank, SeasonTrack, SeasonPass (+5 more)

### Community 46 - "Community 46"
Cohesion: 0.11
Nodes (13): SessionStampFile, Path, GameHost, BootIssues, Config, Instance, IsNewRun, IsReady (+5 more)

### Community 47 - "Community 47"
Cohesion: 0.11
Nodes (5): BaseLabels, Tag, BaseScreen, Id, Root

### Community 48 - "Community 48"
Cohesion: 0.08
Nodes (26): dependencies, depth, source, version, dependencies, depth, source, version (+18 more)

### Community 49 - "Community 49"
Cohesion: 0.14
Nodes (10): BalanceReadMode, Lenient, Strict, BalanceText, Entry, Consumed, Key, Line (+2 more)

### Community 50 - "Community 50"
Cohesion: 0.17
Nodes (6): SaveFileStore, BackupPath, Exists, PrimaryPath, TempPath, SaveFileStoreTests

### Community 51 - "Community 51"
Cohesion: 0.08
Nodes (24): Mastery, CatchLie, CleanBattle, CleanCore, ManualTier, NoOutpostLost, PurgeNoAi, RelocatePeak (+16 more)

### Community 52 - "Community 52"
Cohesion: 0.09
Nodes (25): dependencies, depth, source, version, dependencies, depth, source, version (+17 more)

### Community 53 - "Community 53"
Cohesion: 0.08
Nodes (25): dependencies, depth, source, version, dependencies, depth, source, version (+17 more)

### Community 54 - "Community 54"
Cohesion: 0.09
Nodes (25): depth, source, version, dependencies, depth, source, version, dependencies (+17 more)

### Community 55 - "Community 55"
Cohesion: 0.19
Nodes (3): Deadswitch.Sim.Persistence, Deadswitch.Sim.Rng, Deadswitch.Sim.Tests

### Community 56 - "Community 56"
Cohesion: 0.12
Nodes (17): coverlet.collector (6.0.2), Microsoft.NET.Test.Sdk (17.11.1), UnityEngine.Modules (2021.3.33), xunit (2.9.2), xunit.runner.visualstudio (2.8.2), netstandard2.1, Microsoft.NET.Sdk, net8.0 (+9 more)

### Community 57 - "Community 57"
Cohesion: 0.10
Nodes (21): MaterialDef, BaseColor, EmissionColor, EmissionIntensity, IsLamp, Metallic, Name, Smoothness (+13 more)

### Community 58 - "Community 58"
Cohesion: 0.13
Nodes (6): Feedback, Enabled, HudController, Advisor, Instance, Router

### Community 59 - "Community 59"
Cohesion: 0.11
Nodes (14): AdvisorLine, Id, Text, Tone, Trigger, AdvisorLines, All, Issues (+6 more)

### Community 60 - "Community 60"
Cohesion: 0.13
Nodes (11): DilemmaKind, ChurchSignal, Deserters, None, Refugees, Shortcut, Trader, LivingTexts (+3 more)

### Community 62 - "Community 62"
Cohesion: 0.15
Nodes (4): ClimaxSystem, TrunkMask, OverrideSystem, ProjectSystem

### Community 63 - "Community 63"
Cohesion: 0.13
Nodes (3): BaseLook, Target, LightKey

### Community 64 - "Community 64"
Cohesion: 0.09
Nodes (23): dependencies, dependencies, depth, source, url, version, dependencies, depth (+15 more)

### Community 65 - "Community 65"
Cohesion: 0.12
Nodes (8): ReadmeEditor, BodyStyle, ButtonStyle, HeadingStyle, LinkStyle, TitleStyle, Readme, Section

### Community 66 - "Community 66"
Cohesion: 0.30
Nodes (3): BalanceFile, Program, UsageException

### Community 67 - "Community 67"
Cohesion: 0.10
Nodes (21): dependencies, depth, source, version, dependencies, depth, source, version (+13 more)

### Community 68 - "Community 68"
Cohesion: 0.10
Nodes (21): dependencies, depth, source, version, dependencies, depth, source, version (+13 more)

### Community 69 - "Community 69"
Cohesion: 0.23
Nodes (3): FireLight, PartState, SlotObject

### Community 70 - "Community 70"
Cohesion: 0.14
Nodes (6): DroneCamera, Camera, Instance, PanLimitX, SouthLimit, Tier

### Community 71 - "Community 71"
Cohesion: 0.10
Nodes (17): Names, AttackKind, Purge, Raid, Siege, Warlord, OverrideKind, Lockdown (+9 more)

### Community 72 - "Community 72"
Cohesion: 0.20
Nodes (3): OpsScreen, Id, Root

### Community 73 - "Community 73"
Cohesion: 0.14
Nodes (6): Posture, Dark, Evacuate, None, Turtle, AdaptSystem

### Community 74 - "Community 74"
Cohesion: 0.15
Nodes (6): BaseView, CoreAnchor, Instance, Lighting, SlotCount, Walker

### Community 75 - "Community 75"
Cohesion: 0.11
Nodes (6): LockedScreen, Id, Root, MapScreen, Id, Root

### Community 76 - "Community 76"
Cohesion: 0.15
Nodes (5): ByteWriter, Length, StateWriter, IsReading, Version

### Community 77 - "Community 77"
Cohesion: 0.12
Nodes (6): GlitchText, AdvisorTicker, Current, AdvisorVoice, History, Paused

### Community 78 - "Community 78"
Cohesion: 0.20
Nodes (7): Scars, SpotCount, ScarSet, Fires, Model, SmokeLevel, Smokes

### Community 79 - "Community 79"
Cohesion: 0.13
Nodes (12): EventLog, Count, Events, NextSeq, SimEvent, A, B, C (+4 more)

### Community 80 - "Community 80"
Cohesion: 0.14
Nodes (4): ReportStills, ReportScreen, Id, Root

### Community 81 - "Community 81"
Cohesion: 0.12
Nodes (18): dependencies, depth, source, url, version, depth, source, url (+10 more)

### Community 83 - "Community 83"
Cohesion: 0.21
Nodes (3): CoreScreen, Id, Root

### Community 84 - "Community 84"
Cohesion: 0.24
Nodes (3): WorkforceScreen, Id, Root

### Community 86 - "Community 86"
Cohesion: 0.16
Nodes (5): BalanceReadResult, Config, HasErrors, Issues, Writer

### Community 87 - "Community 87"
Cohesion: 0.12
Nodes (5): ChapterConfig, DiplomacyConfig, ModuleConfig, RaidConfig, TierConfig

### Community 88 - "Community 88"
Cohesion: 0.18
Nodes (7): Records, BestCycle, BestScore, BestTier, LegacyScreen, Id, Root

### Community 89 - "Community 89"
Cohesion: 0.12
Nodes (16): dependencies, depth, source, version, dependencies, depth, source, version (+8 more)

### Community 90 - "Community 90"
Cohesion: 0.17
Nodes (9): SlotView, BuildingKind, BuildingLevel, Damage, Kind, Level, Powered, UnderConstruction (+1 more)

### Community 91 - "Community 91"
Cohesion: 0.16
Nodes (10): BalanceConfigException, Issues, ConfigIssue, Key, Line, Message, Severity, ConfigIssueSeverity (+2 more)

### Community 92 - "Community 92"
Cohesion: 0.30
Nodes (3): IStateVisitor, IsReading, Version

### Community 93 - "Community 93"
Cohesion: 0.17
Nodes (3): PremiumScreen, Id, Root

### Community 94 - "Community 94"
Cohesion: 0.18
Nodes (5): AnimatedNumber, Target, Motion, Reduced, Tween

### Community 95 - "Community 95"
Cohesion: 0.19
Nodes (6): Entitlements, HasPremium, Instance, StoreAvailable, IEntitlements, HasPremium

### Community 96 - "Community 96"
Cohesion: 0.22
Nodes (7): ConvenienceGrant, ThemeBone, ThemeCold, RewardedAds, Theme, Count, Current

### Community 98 - "Community 98"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, url, version, depth, source, version (+7 more)

### Community 99 - "Community 99"
Cohesion: 0.13
Nodes (15): dependencies, depth, source, version, dependencies, depth, source, version (+7 more)

### Community 101 - "Community 101"
Cohesion: 0.17
Nodes (10): CatchUpPlan, Capped, Desync, ElapsedMinutes, Minutes, OfflineClock, CatchUpReport, FirstEventIndex (+2 more)

### Community 104 - "Community 104"
Cohesion: 0.22
Nodes (3): BattleScreen, Id, Root

### Community 106 - "Community 106"
Cohesion: 0.23
Nodes (5): IGameScreen, Id, Root, ScreenRouter, Current

### Community 108 - "Community 108"
Cohesion: 0.29
Nodes (3): ArtBridge, Off, On

### Community 109 - "Community 109"
Cohesion: 0.17
Nodes (12): GuideObjective, Reason, Step, Target, Title, GuideStep, Battery, Defend (+4 more)

### Community 110 - "Community 110"
Cohesion: 0.17
Nodes (4): AiConfig, HostConfig, IConfigSection, ThreatConfig

### Community 111 - "Community 111"
Cohesion: 0.17
Nodes (3): ClimaxConfig, ComputeConfig, HubConfig

### Community 112 - "Community 112"
Cohesion: 0.30
Nodes (3): ConfigHasher, Visitor, Value

### Community 113 - "Community 113"
Cohesion: 0.24
Nodes (5): StateHasher, Visitor, IsReading, Value, Version

### Community 115 - "Community 115"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, version, dependencies, depth, source, url (+4 more)

### Community 118 - "Community 118"
Cohesion: 0.18
Nodes (7): ModuleField, Cyber, Logistics, Stealth, Trunk, Warfare, ModuleTexts

### Community 119 - "Community 119"
Cohesion: 0.18
Nodes (11): EconomyFlows, AutomationLoad, ComputePerHour, CoreUpkeepPerHour, CrewAssigned, CrewNeeded, EnergyCap, FacilityUpkeepPerHour (+3 more)

### Community 120 - "Community 120"
Cohesion: 0.18
Nodes (11): TierGates, All, Build, Levels, LevelsNeeded, Module, ModuleRestored, NetEnergy (+3 more)

### Community 121 - "Community 121"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, url, version, dependencies, depth, source (+3 more)

### Community 122 - "Community 122"
Cohesion: 0.22
Nodes (7): Chapter, Payoff, Premise, Title, Twist, Villain, Story

### Community 123 - "Community 123"
Cohesion: 0.20
Nodes (9): SaveLoadReport, AllCopiesUnreadable, Game, Problems, Source, SaveSlotSource, Backup, None (+1 more)

### Community 125 - "Community 125"
Cohesion: 0.20
Nodes (3): CrewConfig, EnergyConfig, PeopleChoiceConfig

### Community 126 - "Community 126"
Cohesion: 0.20
Nodes (3): DefenseConfig, PeopleConfig, ReportConfig

### Community 127 - "Community 127"
Cohesion: 0.20
Nodes (3): IntelConfig, ScarConfig, WorldConfig

### Community 128 - "Community 128"
Cohesion: 0.24
Nodes (4): Pcg32, Inc, State, Pcg32Tests

### Community 129 - "Community 129"
Cohesion: 0.24
Nodes (5): CorruptionBand, Critical, Glitchy, Stable, Unstable

### Community 130 - "Community 130"
Cohesion: 0.27
Nodes (3): StoryScreen, Id, Root

### Community 131 - "Community 131"
Cohesion: 0.27
Nodes (3): KeyCollector, Descriptions, Keys

### Community 133 - "Community 133"
Cohesion: 0.20
Nodes (10): dependencies, depth, source, version, dependencies, depth, source, version (+2 more)

### Community 134 - "Community 134"
Cohesion: 0.22
Nodes (8): dependencies, com.deadswitch.sim, description, displayName, com.deadswitch.sim, name, unity, version

### Community 135 - "Community 135"
Cohesion: 0.22
Nodes (9): Options, Away, ConfigPath, Garrison, Hours, LoadPath, Posture, SavePath (+1 more)

### Community 136 - "Community 136"
Cohesion: 0.22
Nodes (8): dependencies, com.deadswitch.sim, description, displayName, com.deadswitch.sim, name, unity, version

### Community 137 - "Community 137"
Cohesion: 0.22
Nodes (9): LedgerLine, Amount, Resource, LossResource, Compute, Energy, Fuel, None (+1 more)

### Community 138 - "Community 138"
Cohesion: 0.25
Nodes (3): AdaptConfig, IConfigVisitor, OpeningConfig

### Community 139 - "Community 139"
Cohesion: 0.22
Nodes (8): UltimatumOutcome, Outgrown, Paid, Wave, UltimatumStage, Done, Issued, None

### Community 141 - "Community 141"
Cohesion: 0.25
Nodes (7): three, dependencies, three, description, name, private, type

### Community 142 - "Community 142"
Cohesion: 0.25
Nodes (8): ReportPanel, Caption, Shot, ReportShot, Aftermath, Approach, Contact, Outcome

### Community 143 - "Community 143"
Cohesion: 0.29
Nodes (7): Feature Backlog, Project Roadmap, SPEC-001: Pressure Loop, SPEC-002: Economy Core, SPEC-014: Balance Runner, SPEC-015: Threat Signatures, Unity Project Guide

### Community 146 - "Community 146"
Cohesion: 0.29
Nodes (7): ModuleDef, Field, Index, Node, Pair, Prereq, Tier

### Community 148 - "Community 148"
Cohesion: 0.40
Nodes (6): Advisor Voice Guide, SPEC-004: AI Advisor, SPEC-006: Battle Report, SPEC-007: Hidden Project and Audit, SPEC-011: Project Climax, Advisor Lines Data

### Community 149 - "Community 149"
Cohesion: 0.33
Nodes (3): FacilityConfig, MaxLevel, Section

### Community 150 - "Community 150"
Cohesion: 0.33
Nodes (5): description, displayName, name, unity, version

### Community 151 - "Community 151"
Cohesion: 0.33
Nodes (5): SaveLoadError, Corrupted, NotASave, Truncated, UnsupportedVersion

### Community 154 - "Community 154"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.collab-proxy

### Community 155 - "Community 155"
Cohesion: 0.40
Nodes (3): ADR-0002: Deterministic Sim Core, ADR-0008: Balance Config File, AI Modules UI

### Community 157 - "Community 157"
Cohesion: 0.40
Nodes (5): AnimKind, None, SpinY, SpinZ, SweepY

### Community 158 - "Community 158"
Cohesion: 0.40
Nodes (5): VerifyResult, Diverged, OtherVersion, Unreadable, Verified

### Community 159 - "Community 159"
Cohesion: 0.40
Nodes (5): BattleAbility, Barrage, FocusFire, Seize, Takeover

### Community 160 - "Community 160"
Cohesion: 0.40
Nodes (5): CrisisKind, Collapse, Rollback, Swarm, Takeover

### Community 161 - "Community 161"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.accessibility

### Community 162 - "Community 162"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.androidjni

### Community 163 - "Community 163"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.wind

### Community 165 - "Community 165"
Cohesion: 0.50
Nodes (4): Priority, Ambient, Normal, Urgent

### Community 166 - "Community 166"
Cohesion: 0.50
Nodes (4): RewardKind, Codex, Theme, Voice

### Community 168 - "Community 168"
Cohesion: 0.50
Nodes (4): SimContext, Config, Log, State

### Community 169 - "Community 169"
Cohesion: 0.50
Nodes (4): GlitchKind, Drain, Misfire, Stall

### Community 170 - "Community 170"
Cohesion: 0.50
Nodes (4): LoyaltyStatus, Mutinous, Steady, Strained

## Knowledge Gaps
- **1287 isolated node(s):** `session-start.sh script`, `netstandard2.1`, `Microsoft.NET.Sdk`, `Concrete`, `ConcreteDark` (+1282 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1569 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **59 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `GameHost` connect `Community 46` to `Community 1`, `Community 130`, `Community 4`, `Community 5`, `Community 9`, `Community 18`, `Community 26`, `Community 31`, `Community 32`, `Community 37`, `Community 44`, `Community 50`, `Community 58`, `Community 60`, `Community 72`, `Community 74`, `Community 75`, `Community 77`, `Community 80`, `Community 83`, `Community 84`, `Community 88`, `Community 97`, `Community 101`, `Community 104`, `Community 107`, `Community 116`?**
  _High betweenness centrality (0.094) - this node is a cross-community bridge._
- **Why does `GameState` connect `Community 29` to `Community 128`, `Community 130`, `Community 3`, `Community 4`, `Community 5`, `Community 8`, `Community 139`, `Community 140`, `Community 14`, `Community 15`, `Community 18`, `Community 147`, `Community 20`, `Community 21`, `Community 28`, `Community 36`, `Community 39`, `Community 168`, `Community 41`, `Community 42`, `Community 43`, `Community 51`, `Community 55`, `Community 60`, `Community 61`, `Community 62`, `Community 192`, `Community 71`, `Community 72`, `Community 73`, `Community 76`, `Community 82`, `Community 84`, `Community 90`, `Community 92`, `Community 102`, `Community 114`?**
  _High betweenness centrality (0.087) - this node is a cross-community bridge._
- **Why does `Deadswitch.Sim.State` connect `Community 7` to `Community 192`, `Community 36`, `Community 38`, `Community 71`, `Community 9`, `Community 42`, `Community 139`, `Community 17`, `Community 113`, `Community 19`, `Community 20`, `Community 51`, `Community 118`, `Community 55`, `Community 21`, `Community 92`?**
  _High betweenness centrality (0.075) - this node is a cross-community bridge._
- **Are the 12 inferred relationships involving `MeshBuilder` (e.g. with `.Build()` and `.Build()`) actually correct?**
  _`MeshBuilder` has 12 INFERRED edges - model-reasoned connections that need verification._
- **Are the 2 inferred relationships involving `GameState` (e.g. with `.Load()` and `.Reboot()`) actually correct?**
  _`GameState` has 2 INFERRED edges - model-reasoned connections that need verification._
- **Are the 5 inferred relationships involving `Command` (e.g. with `.Load()` and `.RejectedCommands_DoNotShiftTheRngStream()`) actually correct?**
  _`Command` has 5 INFERRED edges - model-reasoned connections that need verification._
- **What connects `session-start.sh script`, `netstandard2.1`, `Microsoft.NET.Sdk` to the rest of the system?**
  _1287 weakly-connected nodes found - possible documentation gaps or missing edges._