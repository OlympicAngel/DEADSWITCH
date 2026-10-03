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

    /// <summary>How a raid ended. Stored in events: never renumber.</summary>
    public enum RaidOutcome
    {
        None = 0,
        Repelled = 1,
        Breached = 2,
        Missed = 3,
        Lockdown = 4,
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
