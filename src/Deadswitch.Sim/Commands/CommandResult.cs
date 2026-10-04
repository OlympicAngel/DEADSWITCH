namespace Deadswitch.Sim.Commands
{
    /// <summary>Why a command was refused. The UI turns these into advisor lines; never renumber.</summary>
    public enum RejectReason
    {
        None = 0,

        /// <summary>The command kind is unknown to this build.</summary>
        UnknownCommand = 1,

        /// <summary>An argument is outside its valid set.</summary>
        InvalidArgument = 2,

        /// <summary>The command would change nothing (for example, setting the current level again).</summary>
        NoChange = 3,

        /// <summary>The slot id does not exist.</summary>
        InvalidSlot = 4,

        /// <summary>The slot already holds a facility.</summary>
        SlotOccupied = 5,

        /// <summary>The slot is empty.</summary>
        SlotEmpty = 6,

        /// <summary>A construction job is already working on this slot.</summary>
        JobInProgress = 7,

        /// <summary>No construction job on this slot.</summary>
        NoJob = 8,

        /// <summary>Every construction queue slot is busy.</summary>
        QueueFull = 9,

        /// <summary>The facility is at its maximum level.</summary>
        MaxLevel = 10,

        /// <summary>Not enough energy for the cost.</summary>
        NotEnoughEnergy = 11,

        /// <summary>Not enough compute for the cost.</summary>
        NotEnoughCompute = 12,

        /// <summary>No OVERRIDE charge left.</summary>
        NoCharges = 13,

        /// <summary>The shared OVERRIDE cooldown is running.</summary>
        OnCooldown = 14,

        /// <summary>Nothing to act on (for example a lockdown with no incoming attack).</summary>
        NoTarget = 15,

        /// <summary>Not enough free people (crew or garrison).</summary>
        NotEnoughPeople = 16,

        /// <summary>No report kept for that raid.</summary>
        NoReport = 17,

        /// <summary>That report was already verified.</summary>
        AlreadyVerified = 18,

        /// <summary>The module needs a higher tier or its prerequisite first.</summary>
        Locked = 19,

        /// <summary>The other half of this exclusive choice was restored.</summary>
        Excluded = 20,

        /// <summary>The module is already restored.</summary>
        AlreadyRestored = 21,

        /// <summary>Another module is being researched.</summary>
        ResearchBusy = 22,

        /// <summary>Tier-up: build threshold not met (facility levels or net energy).</summary>
        GateBuild = 23,

        /// <summary>Tier-up: the trunk memory module is not restored.</summary>
        GateModule = 24,

        /// <summary>Tier-up: not enough free people to pay the human cost.</summary>
        GatePeople = 25,

        /// <summary>No higher tier in this build.</summary>
        MaxTier = 26,

        /// <summary>The AI is silenced: it will not run anything for you.</summary>
        Silenced = 27,

        /// <summary>Cancel the project needs an Audit inside the final window first.</summary>
        NeedsAudit = 28,

        /// <summary>Loyalty is Steady: a crackdown has no reason.</summary>
        LoyaltyHolds = 29,

        /// <summary>No vacation shield charge left.</summary>
        NoShield = 30,

        /// <summary>The shield cannot rise with an attack incoming or a purge staged.</summary>
        ThreatActive = 31,

        /// <summary>Every operation slot is in the field.</summary>
        OpsBusy = 32,

        /// <summary>The site was hit recently, or the Hub holds it.</summary>
        SiteCooldown = 33,

        /// <summary>Only cleared ruins can become an outpost.</summary>
        NotClaimable = 34,

        NotEnoughFuel = 35,

        /// <summary>No ultimatum or dilemma is waiting.</summary>
        NothingPending = 36,

        /// <summary>A Marked faction will not trade.</summary>
        FactionHostile = 37,

        /// <summary>That faction has traded enough today.</summary>
        TradeCap = 38,

        /// <summary>No room left to store what is on offer.</summary>
        StorageFull = 39,

        /// <summary>Nothing there is damaged (or it is already being repaired).</summary>
        NotDamaged = 40,

        /// <summary>A spy already works that camp.</summary>
        SpyActive = 41,

        /// <summary>No spy in that camp.</summary>
        NoSpy = 42,

        /// <summary>No live battle (or it already started).</summary>
        NoBattle = 43,

        /// <summary>That ability was already spent this battle.</summary>
        AbilityUsed = 44,

        /// <summary>The AI has taken over and ignores that order (flush the core to end it).</summary>
        AiTakeover = 45,
    }

    public readonly struct CommandResult
    {
        private CommandResult(RejectReason reason)
        {
            Reason = reason;
        }

        public static CommandResult Ok => new CommandResult(RejectReason.None);

        public bool Accepted => Reason == RejectReason.None;

        public RejectReason Reason { get; }

        public static CommandResult Reject(RejectReason reason)
        {
            return new CommandResult(reason);
        }

        public override string ToString()
        {
            return Accepted ? "accepted" : "rejected: " + Reason;
        }
    }
}
