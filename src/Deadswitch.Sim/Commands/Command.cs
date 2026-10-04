using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Commands
{
    /// <summary>
    /// Kinds of player input. Values are stored in saves and replays: never renumber or reuse;
    /// argument meaning per kind is documented on each member.
    /// </summary>
    public enum CommandKind
    {
        None = 0,

        /// <summary>A: target <see cref="DelegationLevel"/>.</summary>
        SetDelegation = 1,

        /// <summary>Build a new facility. A: slot, B: <see cref="FacilityKind"/>.</summary>
        Build = 2,

        /// <summary>Upgrade a facility by one level. A: slot.</summary>
        Upgrade = 3,

        /// <summary>Cancel the construction job on a slot (partial refund). A: slot.</summary>
        CancelJob = 4,

        /// <summary>Demolish a facility (partial refund). A: slot.</summary>
        Demolish = 5,

        /// <summary>Switch a facility on or off. A: slot, B: 1 = on, 0 = off.</summary>
        SetFacilityPower = 6,

        /// <summary>Move a slot in the power/crew priority order. A: slot, B: new rank (0 = highest).</summary>
        SetPriority = 7,

        /// <summary>Use an OVERRIDE charge. A: <see cref="OverrideKind"/>.</summary>
        UseOverride = 8,

        /// <summary>Set the defense posture. A: <see cref="Posture"/>.</summary>
        SetPosture = 9,

        /// <summary>Post defenders. A: number of people (0..garrison slots).</summary>
        SetGarrison = 10,

        /// <summary>Host-reported presence. A: 1 = away (logout), 0 = here.</summary>
        SetPresence = 11,

        /// <summary>A: raid id. Verify that raid's report (SPEC-006).</summary>
        VerifyReport = 12,

        /// <summary>No args. Run the Audit (SPEC-007).</summary>
        Audit = 13,

        /// <summary>A: ModuleNode. Start researching a module (SPEC-008).</summary>
        StartResearch = 14,

        /// <summary>No args. Cancel the running research.</summary>
        CancelResearch = 15,

        /// <summary>No args. Advance the Hub a tier through the three gates.</summary>
        TierUp = 16,

        /// <summary>No args. Purge the core (SPEC-011).</summary>
        PurgeCore = 17,

        /// <summary>No args. Cancel the AI's project after an Audit in the final window (SPEC-011).</summary>
        CancelProject = 18,

        /// <summary>No args. Forced labor surge (SPEC-012).</summary>
        ForcedLabor = 19,

        /// <summary>A: people (1..cleanse_max). Neural cleansing.</summary>
        NeuralCleanse = 20,

        /// <summary>No args. Crackdown (only while loyalty is Strained or worse).</summary>
        Crackdown = 21,

        /// <summary>Raise the vacation shield (SPEC-015 rule 5).</summary>
        ActivateShield = 22,

        /// <summary>Tribute standing order. A: 1 on / 0 off.</summary>
        SetTributeOrder = 23,

        /// <summary>Pay the purge ultimatum's tribute.</summary>
        PayPurgeTribute = 24,

        /// <summary>Send an operation. A: site, B: OpKind, C: squad (people) or compute (hack).</summary>
        LaunchOp = 25,

        /// <summary>Set up an outpost on cleared ruins. A: site.</summary>
        ClaimOutpost = 26,

        /// <summary>No args. Pay the Warlord Ultimatum.</summary>
        PayUltimatum = 27,

        /// <summary>Answer the pending dilemma. A: 0 take the offer, 1 refuse.</summary>
        ResolveDilemma = 28,

        /// <summary>Buy one lot from a faction. A: Faction, B: TradeGood.</summary>
        Trade = 29,

        /// <summary>Repair a damaged facility. A: slot.</summary>
        Repair = 30,

        /// <summary>No args. Clear the wrecks from the yard.</summary>
        ClearWreckage = 31,

        /// <summary>Plant a spy in a faction camp. A: Faction.</summary>
        PlantSpy = 32,

        /// <summary>Bring a spy home. A: Faction.</summary>
        RecallSpy = 33,

        /// <summary>Have a spy plant false intel against its faction. A: Faction.</summary>
        FrameFaction = 34,

        /// <summary>Command the incoming attack live (SPEC-020). A: 1 take command, 0 let it auto-resolve.</summary>
        TakeCommand = 35,

        /// <summary>Spend a live-battle ability. A: BattleAbility.</summary>
        UseBattleAbility = 36,

        /// <summary>No args. Flush the core: corruption down, AI offline for a while (SPEC-021).</summary>
        FlushCore = 37,

        /// <summary>No args. Relocate by choice: end the cycle at a peak for the full legacy bonus (SPEC-022).</summary>
        Relocate = 38,

        /// <summary>Buy the next level of a legacy perk. A: Perk.</summary>
        BuyPerk = 39,

        /// <summary>Choose Ironman for this run (only at its start). A: 1 on, 0 off.</summary>
        SetIronman = 40,

        /// <summary>Buy a ceasefire with a faction (SPEC-023). A: Faction.</summary>
        ProposeCeasefire = 41,

        /// <summary>Ally with a Cold faction (SPEC-025). A: Faction.</summary>
        ProposeAlliance = 42,

        /// <summary>No args. Dissolve the current alliance.</summary>
        EndAlliance = 43,

        /// <summary>Recall an op the AI launched on its own (SPEC-030). A: op id.</summary>
        RecallOp = 44,
    }

    /// <summary>
    /// One player input as plain data (kind + three int arguments) so it serializes compactly and replays exactly.
    /// Build instances with the static factories, which document the arguments.
    /// </summary>
    public readonly struct Command
    {
        public Command(CommandKind kind, int a = 0, int b = 0, int c = 0)
        {
            Kind = kind;
            A = a;
            B = b;
            C = c;
        }

        public CommandKind Kind { get; }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public static Command SetDelegation(DelegationLevel level)
        {
            return new Command(CommandKind.SetDelegation, (int)level);
        }

        public static Command Build(int slot, FacilityKind kind)
        {
            return new Command(CommandKind.Build, slot, (int)kind);
        }

        public static Command Upgrade(int slot)
        {
            return new Command(CommandKind.Upgrade, slot);
        }

        public static Command CancelJob(int slot)
        {
            return new Command(CommandKind.CancelJob, slot);
        }

        public static Command Demolish(int slot)
        {
            return new Command(CommandKind.Demolish, slot);
        }

        public static Command SetFacilityPower(int slot, bool on)
        {
            return new Command(CommandKind.SetFacilityPower, slot, on ? 1 : 0);
        }

        public static Command SetPriority(int slot, int rank)
        {
            return new Command(CommandKind.SetPriority, slot, rank);
        }

        public static Command UseOverride(OverrideKind kind)
        {
            return new Command(CommandKind.UseOverride, (int)kind);
        }

        public static Command SetPosture(Posture posture)
        {
            return new Command(CommandKind.SetPosture, (int)posture);
        }

        public static Command SetGarrison(int defenders)
        {
            return new Command(CommandKind.SetGarrison, defenders);
        }

        public static Command VerifyReport(int raidId)
        {
            return new Command(CommandKind.VerifyReport, raidId);
        }

        public static Command Audit()
        {
            return new Command(CommandKind.Audit);
        }

        public static Command StartResearch(ModuleNode node)
        {
            return new Command(CommandKind.StartResearch, (int)node);
        }

        /// <summary>Cancels the memory-sector restoration (its own lane).</summary>
        public static Command CancelMemory()
        {
            return new Command(CommandKind.CancelResearch, 1);
        }

        public static Command CancelResearch()
        {
            return new Command(CommandKind.CancelResearch);
        }

        public static Command TierUp()
        {
            return new Command(CommandKind.TierUp);
        }

        public static Command PurgeCore()
        {
            return new Command(CommandKind.PurgeCore);
        }

        public static Command CancelProject()
        {
            return new Command(CommandKind.CancelProject);
        }

        public static Command ForcedLabor()
        {
            return new Command(CommandKind.ForcedLabor);
        }

        public static Command NeuralCleanse(int people)
        {
            return new Command(CommandKind.NeuralCleanse, people);
        }

        public static Command Crackdown()
        {
            return new Command(CommandKind.Crackdown);
        }

        public static Command ActivateShield()
        {
            return new Command(CommandKind.ActivateShield);
        }

        public static Command SetTributeOrder(bool on)
        {
            return new Command(CommandKind.SetTributeOrder, on ? 1 : 0);
        }

        public static Command PayPurgeTribute()
        {
            return new Command(CommandKind.PayPurgeTribute);
        }

        public static Command LaunchOp(int site, OpKind kind, int squadOrCompute)
        {
            return new Command(CommandKind.LaunchOp, site, (int)kind, squadOrCompute);
        }

        public static Command ClaimOutpost(int site)
        {
            return new Command(CommandKind.ClaimOutpost, site);
        }

        public static Command ProposeCeasefire(Faction faction)
        {
            return new Command(CommandKind.ProposeCeasefire, (int)faction);
        }

        public static Command ProposeAlliance(Faction faction)
        {
            return new Command(CommandKind.ProposeAlliance, (int)faction);
        }

        public static Command RecallOp(int opId)
        {
            return new Command(CommandKind.RecallOp, opId);
        }

        public static Command EndAlliance()
        {
            return new Command(CommandKind.EndAlliance);
        }

        public static Command SetIronman(bool on)
        {
            return new Command(CommandKind.SetIronman, on ? 1 : 0);
        }

        public static Command Relocate()
        {
            return new Command(CommandKind.Relocate);
        }

        public static Command BuyPerk(Perk perk)
        {
            return new Command(CommandKind.BuyPerk, (int)perk);
        }

        public static Command FlushCore()
        {
            return new Command(CommandKind.FlushCore);
        }

        public static Command TakeCommand(bool on)
        {
            return new Command(CommandKind.TakeCommand, on ? 1 : 0);
        }

        public static Command UseBattleAbility(Systems.BattleAbility ability)
        {
            return new Command(CommandKind.UseBattleAbility, (int)ability);
        }

        public static Command PlantSpy(Faction faction)
        {
            return new Command(CommandKind.PlantSpy, (int)faction);
        }

        public static Command RecallSpy(Faction faction)
        {
            return new Command(CommandKind.RecallSpy, (int)faction);
        }

        public static Command FrameFaction(Faction faction)
        {
            return new Command(CommandKind.FrameFaction, (int)faction);
        }

        public static Command Repair(int slot)
        {
            return new Command(CommandKind.Repair, slot);
        }

        public static Command ClearWreckage()
        {
            return new Command(CommandKind.ClearWreckage);
        }

        public static Command PayUltimatum()
        {
            return new Command(CommandKind.PayUltimatum);
        }

        public static Command ResolveDilemma(int choice)
        {
            return new Command(CommandKind.ResolveDilemma, choice);
        }

        public static Command Trade(Faction faction, TradeGood good)
        {
            return new Command(CommandKind.Trade, (int)faction, (int)good);
        }

        public static Command SetPresence(bool away)
        {
            return new Command(CommandKind.SetPresence, away ? 1 : 0);
        }

        public override string ToString()
        {
            return Kind + "(" + A + ", " + B + ", " + C + ")";
        }
    }

    /// <summary>A command as accepted at a tick boundary: it was applied after tick <see cref="Tick"/> completed.</summary>
    public readonly struct RecordedCommand
    {
        public RecordedCommand(long tick, Command command)
        {
            Tick = tick;
            Command = command;
        }

        public long Tick { get; }

        public Command Command { get; }
    }
}
