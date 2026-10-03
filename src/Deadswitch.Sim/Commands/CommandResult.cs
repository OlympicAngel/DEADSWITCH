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
