namespace Deadswitch.Sim.State
{
    /// <summary>Defense posture (doc 04 s5, doc 10 s4). Stored in saves: never renumber.</summary>
    public enum Posture
    {
        None = 0,

        /// <summary>Dig in: extra defense.</summary>
        Turtle = 1,

        /// <summary>Go dark: the raid may miss the Hub; costs energy while active.</summary>
        Dark = 2,

        /// <summary>Pull people out: no casualties, more loot taken.</summary>
        Evacuate = 3,
    }

    /// <summary>Kind of incoming attack (SPEC-015, doc 04 s3). Stored in saves and events: never renumber.</summary>
    public enum AttackKind
    {
        Raid = 0,

        /// <summary>Bombardment: breaks buildings rather than looting.</summary>
        Siege = 1,

        /// <summary>The end of the warning ladder: combined losses.</summary>
        Purge = 2,
    }

    /// <summary>Purge warning ladder (doc 10 s4). Stored in saves and events: never renumber.</summary>
    public enum PurgeStage
    {
        None = 0,
        Rumor = 1,
        Staging = 2,
        Ultimatum = 3,
    }

    /// <summary>How a raid ended. Stored in events: never renumber.</summary>
    public enum RaidOutcome
    {
        None = 0,
        Repelled = 1,
        Breached = 2,
        Missed = 3,
        Lockdown = 4,

        /// <summary>A tribute standing order paid it to leave (SPEC-015 rule 6).</summary>
        Tribute = 5,
    }

    /// <summary>Where a raid hits the Hub (SPEC-004 rule 5). Stored in saves and events: never renumber.</summary>
    public enum RaidGate
    {
        None = 0,

        /// <summary>The ridge above the bunker.</summary>
        North = 1,

        /// <summary>The main gate on the road.</summary>
        South = 2,

        East = 3,
        West = 4,
    }

    /// <summary>Loss ledger line types. Stored in events: never renumber.</summary>
    public enum LossResource
    {
        None = 0,
        Energy = 1,
        Compute = 2,
        People = 3,
    }

    /// <summary>OVERRIDE uses (doc 03 s6). Stored in commands: never renumber.</summary>
    public enum OverrideKind
    {
        None = 0,

        /// <summary>Force-stop the incoming attack.</summary>
        Lockdown = 1,

        /// <summary>Silence the AI: its agenda stops for a time, and so do its advice and predictions (SPEC-011).</summary>
        Silence = 2,
    }

    /// <summary>Corruption bands (doc 10 s3).</summary>
    public enum CorruptionBand
    {
        Stable = 0,
        Glitchy = 1,
        Unstable = 2,
        Critical = 3,
    }
}
