namespace Deadswitch.Sim.Events
{
    /// <summary>
    /// What happened, as recorded truth. Numeric values are stable forever (saves and replays store them):
    /// never renumber or reuse a value; add new kinds at the end and bump <see cref="EventLog.SchemaVersion"/>
    /// if a payload meaning changes. Payload meaning per kind is documented on each member (A, B, C, D).
    /// </summary>
    public enum EventKind
    {
        None = 0,

        /// <summary>An attack is incoming. A: attack id, B: minutes until arrival, C: the AI's strength estimate (may be wrong), D: AttackKind (schema 8+).</summary>
        RaidWarning = 1,

        /// <summary>The AI core lost power: no facility runs, regrowth pauses. No payload.</summary>
        BlackoutStarted = 2,

        /// <summary>Delegation level changed by command. A: new level, B: previous level (see DelegationLevel).</summary>
        DelegationChanged = 3,

        /// <summary>The AI core is powered again. No payload.</summary>
        BlackoutEnded = 4,

        /// <summary>Construction started. A: slot, B: FacilityKind, C: target level, D: build minutes.</summary>
        BuildStarted = 5,

        /// <summary>Construction finished. A: slot, B: FacilityKind, C: new level.</summary>
        BuildCompleted = 6,

        /// <summary>Construction cancelled. A: slot, B: energy refunded, C: compute refunded.</summary>
        BuildCancelled = 7,

        /// <summary>Facility demolished. A: slot, B: FacilityKind, C: energy refunded, D: compute refunded.</summary>
        FacilityDemolished = 8,

        /// <summary>Facility lost power to priority shedding. A: slot, B: FacilityKind.</summary>
        FacilityShed = 9,

        /// <summary>Facility powered again. A: slot, B: FacilityKind.</summary>
        FacilityRestored = 10,

        /// <summary>Handler switched a facility. A: slot, B: 1 = on, 0 = off.</summary>
        FacilityPowerSet = 11,

        /// <summary>Power priority changed. A: slot, B: new rank (0 = highest).</summary>
        PriorityChanged = 12,

        /// <summary>A raid ended. A: raid id, B: RaidOutcome, C: true strength, D: defense rating.</summary>
        RaidResolved = 13,

        /// <summary>Loss ledger line. A: raid id, B: LossResource, C: amount lost.</summary>
        LossLine = 14,

        /// <summary>Mercy window began after a devastating loss. A: raid id, B: minutes of protection.</summary>
        MercyStarted = 15,

        /// <summary>OVERRIDE used. A: OverrideKind, B: charges left, C: corruption added (milli).</summary>
        OverrideUsed = 16,

        /// <summary>Corruption crossed a band. A: new CorruptionBand, B: previous band, C: corruption (milli).</summary>
        CorruptionBandChanged = 17,

        /// <summary>Posture changed. A: new Posture, B: previous.</summary>
        PostureSet = 18,

        /// <summary>Garrison changed. A: new defenders, B: previous.</summary>
        GarrisonSet = 19,

        /// <summary>Handler presence changed. A: 1 = away, 0 = here.</summary>
        PresenceSet = 20,

        /// <summary>The AI's report of the raid's approach (may be a lie). A: raid id, B: reported RaidGate.</summary>
        RaidVector = 21,

        /// <summary>Contact: where the raid really hit (the cross-check for lies). A: raid id, B: true RaidGate.</summary>
        RaidContact = 22,

        /// <summary>The AI acted on its own under delegation. A: AiActionKind, B-D: see AiActionKind.</summary>
        AiActed = 23,

        /// <summary>Hidden record of a lie, for the Audit (F-014); never shown directly. A: LieKind, B-D: see LieKind.</summary>
        AdvisorLied = 24,

        /// <summary>The handler verified a report against the sensor log. A: raid id, B: findings (RaidRecord bits), C: compute spent.</summary>
        ReportVerified = 25,

        /// <summary>Hidden: the AI's project changed stage. A: new ProjectStage, B: previous, C: progress (milli).</summary>
        ProjectStage = 26,

        /// <summary>Audit results (Core Profile). A: ProjectStage, B: Coldness (milli), C: Boldness (milli), D: true corruption (milli).</summary>
        AuditRun = 27,

        /// <summary>Audit findings. A: compute skimmed since the last audit, B: unverified lies on record, C: compute spent.</summary>
        AuditDrain = 28,

        /// <summary>Module research started. A: ModuleNode, B: minutes.</summary>
        ResearchStarted = 29,

        /// <summary>Module restored. A: ModuleNode.</summary>
        ResearchCompleted = 30,

        /// <summary>Research cancelled. A: ModuleNode, B: energy refunded, C: compute refunded.</summary>
        ResearchCancelled = 31,

        /// <summary>The Hub advanced a tier. A: new tier, B: people who left to expand.</summary>
        TierAdvanced = 32,

        /// <summary>The project reached Imminent: final warning. A: game minutes until the climax.</summary>
        ClimaxWarned = 33,

        /// <summary>The handler purged the core. A: energy spent, B: compute lost.</summary>
        CorePurged = 34,

        /// <summary>The AI was silenced. A: game minutes of silence.</summary>
        AiSilenced = 35,

        /// <summary>The handler cancelled the project. A: progress left (milli).</summary>
        ProjectCancelled = 36,

        /// <summary>The window ran out and the AI acted. A: ClimaxKind, B: raid id (betrayal) or modules lost (fork).</summary>
        Climax = 37,

        /// <summary>Forced labor surge ordered. A: people who died, B: hours of boosted output.</summary>
        ForcedLabor = 38,

        /// <summary>Neural cleansing. A: people used up, B: corruption removed (milli).</summary>
        NeuralCleanse = 39,

        /// <summary>Crackdown. A: people removed, B: loyalty after (milli).</summary>
        Crackdown = 40,

        /// <summary>An operator went rogue. A: energy taken.</summary>
        RogueOperator = 41,

        /// <summary>Loyalty status changed. A: new LoyaltyStatus, B: previous.</summary>
        LoyaltyChanged = 42,

        /// <summary>A siege or purge downgraded a facility (SPEC-015). A: attack id, B: slot, C: FacilityKind, D: new level.</summary>
        FacilityDamaged = 43,

        /// <summary>A virus hit the core (SPEC-015 rule 3). A: 0 burned off / 1 infected, B: compute burned, C: locked ModuleNode (0 none), D: corruption added (milli).</summary>
        VirusStruck = 44,

        /// <summary>Purge ladder moved (doc 10 s4). A: PurgeStage (0 = ended), B: minutes to the strike, C: end reason (1 rumor false, 2 tribute paid, 3 struck).</summary>
        PurgeLadder = 45,

        /// <summary>Tribute paid. A: attack id (0 = purge ultimatum), B: energy, C: compute.</summary>
        TributePaid = 46,

        /// <summary>Vacation shield. A: 2 activated (pending) / 1 holding / 0 down, B: minutes until it rises (A=2) or holds (A=1).</summary>
        ShieldChanged = 47,

        /// <summary>Tribute standing order toggled. A: 1 on / 0 off.</summary>
        TributeOrderSet = 48,

        /// <summary>Operation sent (doc 04 s8). A: op id, B: site, C: OpKind, D: squad (or compute for a hack).</summary>
        OpLaunched = 49,

        /// <summary>Operation back. A: op id, B: site, C: 1 success / 0 failure, D: casualties.</summary>
        OpReturned = 50,

        /// <summary>Operation loot. A: op id, B: LossResource, C: amount.</summary>
        OpLoot = 51,

        /// <summary>A faction's heat level changed (doc 10 s4). A: Faction, B: new HeatLevel, C: old.</summary>
        HeatLevelChanged = 52,

        /// <summary>Outpost set up. A: site.</summary>
        OutpostClaimed = 53,

        /// <summary>Outpost taken back. A: site, B: Faction.</summary>
        OutpostLost = 54,

        /// <summary>Who sent the incoming attack. A: attack id, B: Faction.</summary>
        AttackerIdentified = 55,

        /// <summary>Warlord Ultimatum (F-034). A: minutes to the deadline, B: energy asked, C: fuel asked.</summary>
        UltimatumIssued = 56,

        /// <summary>A: UltimatumOutcome, B: attack id when the wave came.</summary>
        UltimatumResolved = 57,

        /// <summary>A: DilemmaKind, B: minutes to answer.</summary>
        DilemmaOffered = 58,

        /// <summary>A: DilemmaKind, B: choice (0 take, 1 refuse), C: 1 if it expired, D: 1 if the hidden risk struck.</summary>
        DilemmaResolved = 59,

        /// <summary>A: Faction, B: TradeGood, C: amount got, D: price paid.</summary>
        Traded = 60,

        /// <summary>A: WorldEventKind, B: hours, C: faction that cooled (factions fight each other).</summary>
        WorldEventStarted = 61,

        /// <summary>A: WorldEventKind.</summary>
        WorldEventEnded = 62,

        /// <summary>Battle scar (SPEC-018). A: attack id, B: slot, C: FacilityKind, D: damage now.</summary>
        FacilityScarred = 63,

        /// <summary>A: slot, B: energy paid, C: minutes.</summary>
        RepairStarted = 64,

        /// <summary>A: slot, B: FacilityKind.</summary>
        RepairDone = 65,

        /// <summary>A: wrecks in the yard now.</summary>
        WreckageAdded = 66,

        /// <summary>A: wrecks cleared, B: energy paid.</summary>
        WreckageCleared = 67,

        /// <summary>Spy planted (SPEC-019). A: Faction, B: energy paid. Loyalty stays hidden.</summary>
        SpyPlanted = 68,

        /// <summary>A: Faction.</summary>
        SpyRecalled = 69,

        /// <summary>A: Faction, B: SpyLoss.</summary>
        SpyLost = 70,

        /// <summary>False intel planted. A: Faction, B: 1 if it backfired (double agent).</summary>
        SpyFramed = 71,

        /// <summary>Live battle at the wall (SPEC-020). A: attack id, B: minutes, C: AttackKind.</summary>
        BattleStarted = 72,

        /// <summary>A: attack id, B: BattleAbility.</summary>
        BattleAbilityUsed = 73,

        /// <summary>An AI-run unit erred (SPEC-021). A: slot, B: FacilityKind, C: GlitchKind.</summary>
        UnitGlitched = 74,

        /// <summary>An AI-run turret was hijacked mid-fight. A: attack id, B: slot, C: guns turned.</summary>
        UnitDefected = 75,

        /// <summary>Critical crisis. A: CrisisKind, B: hours it lasts (0 = instant).</summary>
        CrisisStruck = 76,

        /// <summary>Core flushed. A: energy paid, B: corruption removed (milli), C: hours offline.</summary>
        CoreFlushed = 77,

        /// <summary>Mastery challenge earned (SPEC-022). A: Mastery.</summary>
        MasteryEarned = 78,

        /// <summary>A: Perk, B: new level, C: legacy points paid.</summary>
        PerkBought = 79,

        /// <summary>The cycle ended and the core moved on. A: RebootReason, B: legacy score, C: points earned, D: veterans carried.</summary>
        CycleEnded = 80,

        /// <summary>Ironman switched. A: 1 on, 0 off.</summary>
        IronmanSet = 81,
    }

    /// <summary>Immutable log entry. <see cref="Seq"/> is unique and increasing across the whole run.</summary>
    public readonly struct SimEvent
    {
        public SimEvent(long seq, long tick, EventKind kind, int a, int b, int c, int d)
        {
            Seq = seq;
            Tick = tick;
            Kind = kind;
            A = a;
            B = b;
            C = c;
            D = d;
        }

        public long Seq { get; }

        public long Tick { get; }

        public EventKind Kind { get; }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public override string ToString()
        {
            return "#" + Seq + " t" + Tick + " " + Kind + " (" + A + ", " + B + ", " + C + ", " + D + ")";
        }
    }
}
