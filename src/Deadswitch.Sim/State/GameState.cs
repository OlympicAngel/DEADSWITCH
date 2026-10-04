using System.Collections.Generic;
using Deadswitch.Sim.Rng;

namespace Deadswitch.Sim.State
{
    /// <summary>
    /// All mutable sim state. Every field must be declared in <see cref="Visit"/>, which drives both the
    /// state hash and the save format (ADR-0009). Public fields so the visitor can take them by ref.
    /// </summary>
    public sealed class GameState
    {
        /// <summary>Field layout version (save format). v2: AI dials, raid gates, lies, planner hold (SPEC-004). v3: raid records (SPEC-006). v4: project clock and audit (SPEC-007). v5: tier, modules, research (SPEC-008). v6: climax window, silence, betrayal, OVERRIDE penalty (SPEC-011). v7: loyalty and surge (SPEC-012). v8: threats (SPEC-015). v9: world map (SPEC-016). v10: living world (SPEC-017). v11: battle scars (SPEC-018). v12: spies (SPEC-019). v13: live battles (SPEC-020). v14: corruption effects (SPEC-021). v15: legacy cycle (SPEC-022). v16: Ironman. v17: cycle mastery, rebuilding surge, memory lane. v18: ceasefires (SPEC-023). v19: fourth faction (Halcyon Dynamics). v20: chapters and memory fragments (SPEC-024). v21: alliances (SPEC-025). v22: sabotage (SPEC-026). v23: adaptive enemies (SPEC-027). v24: luck swings (SPEC-028). v25: reactor (SPEC-029). v26: AI initiative (SPEC-030). v27: starting regions (SPEC-031). v28: reactor fuel per tick, AI initiative only when present. v29: fallout front (SPEC-032).</summary>
        public const int LayoutVersion = 29;

        public long Tick;

        public int Energy;

        public int Fuel;

        public int Compute;

        public int People;

        /// <summary>0..100_000 (100_000 = 100%).</summary>
        public int CorruptionMilli;

        public int RaidsToday;

        public DelegationLevel Delegation;

        public Pcg32 Rng;

        /// <summary>True while the AI core itself is unpowered (SPEC-002 rule 5).</summary>
        public bool Blackout;

        /// <summary>Unmanned facilities run by the AI this tick (SPEC-002 rule 6). Feeds corruption in M2.</summary>
        public int AutomationLoad;

        public int OverrideCharges;

        public long OverrideNextChargeTick;

        public long OverrideCooldownUntil;

        public Posture Posture;

        /// <summary>People posted as defenders (not available as crew).</summary>
        public int Garrison;

        /// <summary>Handler is away (host sends SetPresence at logout/login).</summary>
        public bool Away;

        /// <summary>Id the next raid receives.</summary>
        public int NextRaidId = 1;

        /// <summary>Id of the incoming raid, or 0.</summary>
        public int RaidId;

        public long RaidArriveTick;

        /// <summary>True strength before variance.</summary>
        public int RaidStrength;

        /// <summary>What the AI told the handler (may be wrong when corrupted).</summary>
        public int RaidEstimate;

        public long MercyUntilTick;

        /// <summary>Hidden dial (doc 10 s1.4), 0..100_000. Raised by ruthless choices.</summary>
        public int ColdnessMilli;

        /// <summary>Hidden dial (doc 10 s1.4), 0..100_000. Raised by reliance on delegation.</summary>
        public int BoldnessMilli;

        /// <summary>Gate the incoming raid really uses.</summary>
        public RaidGate RaidGate;

        /// <summary>Gate the AI reported for the incoming raid.</summary>
        public RaidGate RaidGateReported;

        /// <summary>Lies told this run (the first lie is special, doc 10 s7.4).</summary>
        public int LiesTold;

        /// <summary>The delegated planner waits until this tick (set when the handler cancels a job).</summary>
        public long PlanHoldUntilTick;

        /// <summary>Hub slots; the index is the slot id.</summary>
        public List<FacilitySlot> Slots = new List<FacilitySlot>();

        /// <summary>The AI's hidden project, 0..100_000 (SPEC-007).</summary>
        public int ProjectMilli;

        /// <summary>Compute the AI skimmed since the last Audit.</summary>
        public int SkimmedSinceAudit;

        /// <summary>The Audit is available again from this tick.</summary>
        public long AuditReadyTick;

        /// <summary>Hub tier (1 = Bunker).</summary>
        public int Tier = 1;

        /// <summary>Restored modules: bit n set for ModuleNode n.</summary>
        public ulong Modules;

        /// <summary>Module being researched (ModuleNode), or 0.</summary>
        public int ResearchNode;

        public long ResearchStartTick;

        public long ResearchCompleteTick;

        public int ResearchPaidEnergy;

        public int ResearchPaidCompute;

        /// <summary>Tick the project's climax fires, or 0 when no final window is open (SPEC-011).</summary>
        public long ClimaxAtTick;

        /// <summary>An Audit ran inside the current final window (allows cancelling the project).</summary>
        public bool ClimaxAudited;

        /// <summary>The AI stays silenced until this tick.</summary>
        public long SilencedUntilTick;

        /// <summary>Raid the AI let in (turrets stay offline against it), or 0.</summary>
        public int BetrayalRaidId;

        /// <summary>OVERRIDE charges lost to a fork.</summary>
        public int OverrideMaxPenalty;

        /// <summary>People's loyalty, 0..100_000 (SPEC-012). Shown only as a status.</summary>
        public int LoyaltyMilli = 100_000;

        /// <summary>A forced labor surge boosts output until this tick.</summary>
        public long SurgeUntilTick;

        /// <summary>The next surge can be ordered from this tick.</summary>
        public long SurgeReadyTick;

        /// <summary>Kind of the incoming attack (SPEC-015).</summary>
        public AttackKind RaidKind;

        /// <summary>Next siege / virus / purge-ladder due tick (0 = not scheduled yet).</summary>
        public long NextSiegeTick;

        public long NextVirusTick;

        public long NextPurgeTick;

        public PurgeStage PurgeStage;

        /// <summary>When the staged purge strikes (0 when no ladder runs).</summary>
        public long PurgeAtTick;

        /// <summary>False rumors fizzle before staging.</summary>
        public bool PurgeReal;

        /// <summary>Module locked by a virus, and until when.</summary>
        public int LockedModule;

        public long LockedUntilTick;

        /// <summary>The next raid estimate is false (virus).</summary>
        public bool FalseIntel;

        public int ShieldCharges;

        /// <summary>Vacation shield holds from this tick (after the activation delay) until <see cref="ShieldUntilTick"/>.</summary>
        public long ShieldFromTick;

        /// <summary>Vacation shield end (0 = none raised or pending).</summary>
        public long ShieldUntilTick;

        public long ShieldNextChargeTick;

        public bool TributeOrder;

        /// <summary>Faction heat (milli, 100000 = 100), indexed by <see cref="Faction"/>.</summary>
        public int[] Heat = new int[Systems.WorldSystem.FactionCount];

        /// <summary>Faction that sent the incoming attack.</summary>
        public Faction RaidFaction;

        public int NextOpId = 1;

        /// <summary>Per map site (index = <see cref="Systems.WorldSystem.Sites"/>).</summary>
        public List<SiteState> Sites = new List<SiteState>();

        /// <summary>Operations in the field, in launch order.</summary>
        public List<Operation> Ops = new List<Operation>();

        public UltimatumStage Ultimatum;

        /// <summary>When the Warlord's deadline runs out (moves on by an hour while fairness rules hold the wave back).</summary>
        public long UltimatumDeadlineTick;

        /// <summary>The dilemma waiting for an answer (None when none).</summary>
        public DilemmaKind Dilemma;

        public long DilemmaUntilTick;

        /// <summary>When the next dilemma is offered (0 = not scheduled yet).</summary>
        public long NextDilemmaTick;

        public WorldEventKind WorldEvent;

        public long WorldEventUntilTick;

        /// <summary>When the next world event starts (0 = not scheduled yet).</summary>
        public long NextWorldEventTick;

        /// <summary>Reactor fuel owed, in fuel x ticks (60 = one unit of fuel), burned every tick it runs.</summary>
        public int ReactorFuelTicks;

        /// <summary>Where this cycle's Hub stands (SPEC-031); chosen when relocating.</summary>
        public Region Region;

        /// <summary>Map site under the drifting fallout front (SPEC-032); -1 before it first settles.</summary>
        public int FalloutSite = -1;

        /// <summary>When the fallout front next drifts (0 = not scheduled yet).</summary>
        public long NextFalloutTick;

        /// <summary>The reactor had its fuel at the last hour (SPEC-029); without it the reactor makes nothing.</summary>
        public bool ReactorFueled = true;

        /// <summary>Hidden streak of the current window (SPEC-028): -1 calm, 0 normal, 1 restless. Never shown.</summary>
        public int Mood;

        public long MoodUntilTick;

        /// <summary>Faction regrouping after a crushing defense (-1 = none), until <see cref="RegroupUntilTick"/>.</summary>
        public int RegroupFaction = -1;

        public long RegroupUntilTick;

        /// <summary>Counters each faction learned against the Hub's postures (SPEC-027): [faction * 3 + Turtle/Dark/Evacuate].</summary>
        public int[] Learned = new int[Systems.WorldSystem.FactionCount * Systems.AdaptSystem.Tactics];

        /// <summary>Fortification level of each faction's sites against the Hub's raids.</summary>
        public int[] Fortified = new int[Systems.WorldSystem.FactionCount];

        /// <summary>Faction whose attacks are crippled by sabotage (-1 = none, SPEC-026), until <see cref="SabotageUntilTick"/>.</summary>
        public int SabotageFaction = -1;

        public long SabotageUntilTick;

        /// <summary>Allied faction (-1 = none, SPEC-025).</summary>
        public int AllyFaction = -1;

        /// <summary>The ally takes its next daily share at this tick.</summary>
        public long AllyUpkeepTick;

        /// <summary>Tier of the open chapter (SPEC-024); 0 until the first one opens.</summary>
        public int ChapterTier;

        /// <summary>Chapter beat: 0 opened, 1 twist revealed, 2 closed.</summary>
        public int ChapterBeat;

        public long ChapterOpenedTick;

        /// <summary>Payoff points earned since the twist.</summary>
        public int ChapterPoints;

        /// <summary>Memory fragments recovered: bit n for fragment n (12 in all). Survives every reboot.</summary>
        public int Fragments;

        /// <summary>Faction under ceasefire (-1 = none, SPEC-023), until <see cref="CeasefireUntilTick"/>.</summary>
        public int CeasefireFaction = -1;

        public long CeasefireUntilTick;

        /// <summary>No new ceasefire before this tick.</summary>
        public long CeasefireReadyTick;

        /// <summary>Memory sector (trunk M1-M3) restoring in its own lane, beside field research (0 = none).</summary>
        public int MemoryNode;

        public long MemoryStartTick;

        public long MemoryCompleteTick;

        public int MemoryPaidEnergy;

        public int MemoryPaidCompute;

        /// <summary>Mastery earned in this cycle (bit per <see cref="State.Mastery"/>): only these score at the move.</summary>
        public int CycleMastery;

        /// <summary>After a forced reboot (doc 10 s2): regrowth is boosted until the population reaches half the cap.</summary>
        public bool RebuildingSurge;

        /// <summary>Ironman run (doc 10 s1.2): no shield, shorter mercy, losing the core ends the run.</summary>
        public bool Ironman;

        /// <summary>Cycles completed (relocations and reboots, SPEC-022); 0 in the first run.</summary>
        public int Cycle;

        public long CycleStartTick;

        /// <summary>Unspent legacy points.</summary>
        public int LegacyPoints;

        /// <summary>Sum of every finished cycle's legacy score (the recorded legacy).</summary>
        public int LegacyTotal;

        /// <summary>Perk levels, indexed by <see cref="Perk"/>.</summary>
        public int[] Perks = new int[Systems.LegacySystem.PerkCount];

        /// <summary>Mastery challenges earned (bit per <see cref="State.Mastery"/>); kept across cycles.</summary>
        public int Mastery;

        public int HighestTier = 1;

        public int PeakPower;

        /// <summary>Delegation stayed Manual for the whole current tier.</summary>
        public bool TierManual = true;

        public int TierMaxCorruption;

        public int OutpostsLostThisCycle;

        /// <summary>Hours in a row at the people floor / at Critical corruption (forced reboot triggers).</summary>
        public int CollapseHours;

        public int CriticalHours;

        /// <summary>The Hub fell to a purge it ignored, undefended: the cycle ends at the next hour.</summary>
        public bool HubFallen;

        /// <summary>No Critical crisis before this tick (SPEC-021).</summary>
        public long NextCrisisTick;

        /// <summary>AI takeover: build, research and posture orders are refused until this tick.</summary>
        public long TakeoverUntilTick;

        /// <summary>Core flush: AI-run units stop and the AI predicts nothing until this tick.</summary>
        public long FlushUntilTick;

        /// <summary>The handler will command the incoming attack live (SPEC-020).</summary>
        public bool BattleLive;

        /// <summary>A live battle holds resolution until this tick (0 = no battle).</summary>
        public long BattleEndTick;

        public int BattleDefensePct;

        public int BattleStrengthCutPct;

        /// <summary>Abilities spent this battle (bit per <see cref="Systems.BattleAbility"/>).</summary>
        public int BattleUsed;

        /// <summary>Spy per faction camp (SPEC-019), indexed by <see cref="Faction"/>.</summary>
        public SpyState[] Spies = new SpyState[Systems.WorldSystem.FactionCount];

        /// <summary>Wrecks in the yard (SPEC-018), 0..scars.max_wreckage.</summary>
        public int Wreckage;

        /// <summary>When the last attack added wreckage (fresh wrecks burn for a while).</summary>
        public long ScarredAtTick;

        /// <summary>Trades made today, indexed by <see cref="Faction"/>.</summary>
        public int[] TradesToday = new int[Systems.WorldSystem.FactionCount];

        /// <summary>Recent raids' report records, oldest first (at most <c>report.keep_raids</c>).</summary>
        public List<RaidRecord> RaidRecords = new List<RaidRecord>();

        /// <summary>Construction queue in start order.</summary>
        public List<BuildJob> Jobs = new List<BuildJob>();

        /// <summary>Slot ids from highest to lowest power and crew priority. Always a permutation of all slots.</summary>
        public List<int> PowerPriority = new List<int>();

        public GameState(ulong seed, SimConfig config)
        {
            Rng = Pcg32.Create(seed);
            Energy = config.Energy.Start;
            Fuel = config.Fuel.Start;
            Compute = config.Compute.Start;
            People = config.People.Start;
            OverrideCharges = config.Override.StartCharges;
            OverrideNextChargeTick = config.Override.RegenMinutes;
            ShieldCharges = config.Threats.ShieldStartCharges;
            Resize(Sites, Systems.WorldSystem.Sites.Count);
            ShieldNextChargeTick = (long)config.Threats.ShieldRegenDays * SimConfig.TicksPerDay;

            for (int i = 0; i < config.Hub.Slots; i++)
            {
                Slots.Add(new FacilitySlot());
                PowerPriority.Add(i);
            }

            // Starting layout (SPEC-002 rule 2): reproduces the doc 10 s3 starting rates.
            Slots[0].Kind = FacilityKind.Generator;
            Slots[0].Level = 1;
            Slots[0].Powered = true;
            Slots[0].Staffed = true;
            Slots[1].Kind = FacilityKind.ServerRack;
            Slots[1].Level = 1;
            Slots[1].Powered = true;
            Slots[1].Staffed = true;
        }

        /// <summary>Job working on a slot, or null.</summary>
        public BuildJob? JobForSlot(int slot)
        {
            foreach (BuildJob job in Jobs)
            {
                if (job.Slot == slot)
                {
                    return job;
                }
            }

            return null;
        }

        /// <summary>
        /// Visits every field in a fixed order. Append new fields at the end of their group and bump
        /// <c>SaveGame.FormatVersion</c> with a migration when the layout changes.
        /// </summary>
        public void Visit(IStateVisitor v)
        {
            v.Long(ref Tick);
            v.Int(ref Energy);
            v.Int(ref Fuel);
            v.Int(ref Compute);
            v.Int(ref People);
            v.Int(ref CorruptionMilli);
            v.Int(ref RaidsToday);

            int delegation = (int)Delegation;
            v.Int(ref delegation);
            Delegation = (DelegationLevel)delegation;

            ulong rngState = Rng.State;
            ulong rngInc = Rng.Inc;
            v.ULong(ref rngState);
            v.ULong(ref rngInc);
            if (v.IsReading)
            {
                Rng = Pcg32.Restore(rngState, rngInc);
            }

            v.Bool(ref Blackout);
            v.Int(ref AutomationLoad);
            v.Int(ref OverrideCharges);
            v.Long(ref OverrideNextChargeTick);
            v.Long(ref OverrideCooldownUntil);
            int posture = (int)Posture;
            v.Int(ref posture);
            Posture = (Posture)posture;
            v.Int(ref Garrison);
            v.Bool(ref Away);
            v.Int(ref NextRaidId);
            v.Int(ref RaidId);
            v.Long(ref RaidArriveTick);
            v.Int(ref RaidStrength);
            v.Int(ref RaidEstimate);
            v.Long(ref MercyUntilTick);
            if (v.Version >= 2)
            {
                v.Int(ref ColdnessMilli);
                v.Int(ref BoldnessMilli);
                int gate = (int)RaidGate;
                v.Int(ref gate);
                RaidGate = (RaidGate)gate;
                int reported = (int)RaidGateReported;
                v.Int(ref reported);
                RaidGateReported = (RaidGate)reported;
                v.Int(ref LiesTold);
                v.Long(ref PlanHoldUntilTick);
            }
            else if (v.IsReading && RaidId != 0)
            {
                // v1 save with a raid incoming: give it a gate (no RNG draw) and report it truthfully.
                RaidGate = (RaidGate)(1 + (int)(Systems.SimMath.Hash((uint)RaidId, (uint)RaidArriveTick) % 4));
                RaidGateReported = RaidGate;
            }

            int slotCount = v.Count(Slots.Count);
            Resize(Slots, slotCount);
            foreach (FacilitySlot slot in Slots)
            {
                slot.Visit(v);
            }

            int jobCount = v.Count(Jobs.Count);
            Resize(Jobs, jobCount);
            foreach (BuildJob job in Jobs)
            {
                job.Visit(v);
            }

            int priorityCount = v.Count(PowerPriority.Count);
            while (PowerPriority.Count < priorityCount)
            {
                PowerPriority.Add(0);
            }

            if (PowerPriority.Count > priorityCount)
            {
                PowerPriority.RemoveRange(priorityCount, PowerPriority.Count - priorityCount);
            }

            for (int i = 0; i < PowerPriority.Count; i++)
            {
                int slotId = PowerPriority[i];
                v.Int(ref slotId);
                PowerPriority[i] = slotId;
            }

            if (v.Version >= 3)
            {
                int recordCount = v.Count(RaidRecords.Count);
                Resize(RaidRecords, recordCount);
                foreach (RaidRecord record in RaidRecords)
                {
                    record.Visit(v);
                }
            }

            if (v.Version >= 4)
            {
                v.Int(ref ProjectMilli);
                v.Int(ref SkimmedSinceAudit);
                v.Long(ref AuditReadyTick);
            }

            if (v.Version >= 5)
            {
                v.Int(ref Tier);
                v.ULong(ref Modules);
                v.Int(ref ResearchNode);
                v.Long(ref ResearchStartTick);
                v.Long(ref ResearchCompleteTick);
                v.Int(ref ResearchPaidEnergy);
                v.Int(ref ResearchPaidCompute);
            }

            if (v.Version >= 6)
            {
                v.Long(ref ClimaxAtTick);
                v.Bool(ref ClimaxAudited);
                v.Long(ref SilencedUntilTick);
                v.Int(ref BetrayalRaidId);
                v.Int(ref OverrideMaxPenalty);
            }

            if (v.Version >= 7)
            {
                v.Int(ref LoyaltyMilli);
                v.Long(ref SurgeUntilTick);
                v.Long(ref SurgeReadyTick);
            }

            if (v.Version >= 8)
            {
                int kind = (int)RaidKind;
                v.Int(ref kind);
                RaidKind = (AttackKind)kind;
                v.Long(ref NextSiegeTick);
                v.Long(ref NextVirusTick);
                v.Long(ref NextPurgeTick);
                int stage = (int)PurgeStage;
                v.Int(ref stage);
                PurgeStage = (PurgeStage)stage;
                v.Long(ref PurgeAtTick);
                v.Bool(ref PurgeReal);
                v.Int(ref LockedModule);
                v.Long(ref LockedUntilTick);
                v.Bool(ref FalseIntel);
                v.Int(ref ShieldCharges);
                v.Long(ref ShieldFromTick);
                v.Long(ref ShieldUntilTick);
                v.Long(ref ShieldNextChargeTick);
                v.Bool(ref TributeOrder);
            }

            if (v.Version >= 9)
            {
                for (int f = 0; f < (v.Version >= 19 ? Heat.Length : 3); f++)
                {
                    v.Int(ref Heat[f]);
                }

                int faction = (int)RaidFaction;
                v.Int(ref faction);
                RaidFaction = (Faction)faction;
                v.Int(ref NextOpId);
                int siteCount = v.Count(Sites.Count);
                Resize(Sites, siteCount);
                foreach (SiteState site in Sites)
                {
                    site.Visit(v);
                }

                int opCount = v.Count(Ops.Count);
                Resize(Ops, opCount);
                foreach (Operation op in Ops)
                {
                    op.Visit(v);
                }
            }

            if (v.Version >= 10)
            {
                int ultimatum = (int)Ultimatum;
                v.Int(ref ultimatum);
                Ultimatum = (UltimatumStage)ultimatum;
                v.Long(ref UltimatumDeadlineTick);
                int dilemma = (int)Dilemma;
                v.Int(ref dilemma);
                Dilemma = (DilemmaKind)dilemma;
                v.Long(ref DilemmaUntilTick);
                v.Long(ref NextDilemmaTick);
                int worldEvent = (int)WorldEvent;
                v.Int(ref worldEvent);
                WorldEvent = (WorldEventKind)worldEvent;
                v.Long(ref WorldEventUntilTick);
                v.Long(ref NextWorldEventTick);
                for (int f = 0; f < (v.Version >= 19 ? TradesToday.Length : 3); f++)
                {
                    v.Int(ref TradesToday[f]);
                }
            }

            if (v.Version >= 11)
            {
                v.Int(ref Wreckage);
                v.Long(ref ScarredAtTick);
            }

            if (v.Version >= 28)
            {
                v.Int(ref ReactorFuelTicks);
            }

            if (v.Version >= 27)
            {
                int region = (int)Region;
                v.Int(ref region);
                Region = (Region)region;
            }

            if (v.Version >= 25)
            {
                v.Bool(ref ReactorFueled);
            }

            if (v.Version >= 24)
            {
                v.Int(ref Mood);
                v.Long(ref MoodUntilTick);
                v.Int(ref RegroupFaction);
                v.Long(ref RegroupUntilTick);
            }

            if (v.Version >= 23)
            {
                for (int i = 0; i < Learned.Length; i++)
                {
                    v.Int(ref Learned[i]);
                }

                for (int f = 0; f < Fortified.Length; f++)
                {
                    v.Int(ref Fortified[f]);
                }
            }

            if (v.Version >= 22)
            {
                v.Int(ref SabotageFaction);
                v.Long(ref SabotageUntilTick);
            }

            if (v.Version >= 21)
            {
                v.Int(ref AllyFaction);
                v.Long(ref AllyUpkeepTick);
            }

            if (v.Version >= 20)
            {
                v.Int(ref ChapterTier);
                v.Int(ref ChapterBeat);
                v.Long(ref ChapterOpenedTick);
                v.Int(ref ChapterPoints);
                v.Int(ref Fragments);
            }

            if (v.Version >= 18)
            {
                v.Int(ref CeasefireFaction);
                v.Long(ref CeasefireUntilTick);
                v.Long(ref CeasefireReadyTick);
            }

            if (v.Version >= 17)
            {
                v.Int(ref CycleMastery);
                v.Bool(ref RebuildingSurge);
                v.Int(ref MemoryNode);
                v.Long(ref MemoryStartTick);
                v.Long(ref MemoryCompleteTick);
                v.Int(ref MemoryPaidEnergy);
                v.Int(ref MemoryPaidCompute);
            }

            if (v.Version >= 16)
            {
                v.Bool(ref Ironman);
            }

            if (v.Version >= 15)
            {
                v.Int(ref Cycle);
                v.Long(ref CycleStartTick);
                v.Int(ref LegacyPoints);
                v.Int(ref LegacyTotal);
                for (int p = 0; p < Perks.Length; p++)
                {
                    v.Int(ref Perks[p]);
                }

                v.Int(ref Mastery);
                v.Int(ref HighestTier);
                v.Int(ref PeakPower);
                v.Bool(ref TierManual);
                v.Int(ref TierMaxCorruption);
                v.Int(ref OutpostsLostThisCycle);
                v.Int(ref CollapseHours);
                v.Int(ref CriticalHours);
                v.Bool(ref HubFallen);
            }
            else if (v.IsReading)
            {
                // older saves: no tier history, so no tier mastery can be claimed for the tier in progress
                HighestTier = Tier;
                TierManual = false;
                TierMaxCorruption = Systems.CorruptionSystem.MaxMilli;
            }

            if (v.Version >= 14)
            {
                v.Long(ref NextCrisisTick);
                v.Long(ref TakeoverUntilTick);
                v.Long(ref FlushUntilTick);
            }

            if (v.Version >= 13)
            {
                v.Bool(ref BattleLive);
                v.Long(ref BattleEndTick);
                v.Int(ref BattleDefensePct);
                v.Int(ref BattleStrengthCutPct);
                v.Int(ref BattleUsed);
            }

            if (v.Version >= 12)
            {
                for (int f = 0; f < (v.Version >= 19 ? Spies.Length : 3); f++)
                {
                    int spy = (int)Spies[f];
                    v.Int(ref spy);
                    Spies[f] = (SpyState)spy;
                }
            }

            if (v.Version >= 29)
            {
                v.Int(ref FalloutSite);
                v.Long(ref NextFalloutTick);
            }

            if (v.IsReading)
            {
                // older saves (and a catalog that grew) get a state entry for every map site
                Resize(Sites, System.Math.Max(Sites.Count, Systems.WorldSystem.Sites.Count));
            }
        }

        private static void Resize<T>(List<T> list, int count)
            where T : new()
        {
            while (list.Count < count)
            {
                list.Add(new T());
            }

            if (list.Count > count)
            {
                list.RemoveRange(count, list.Count - count);
            }
        }
    }
}
