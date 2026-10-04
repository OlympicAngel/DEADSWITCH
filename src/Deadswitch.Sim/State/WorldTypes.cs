namespace Deadswitch.Sim.State
{
    /// <summary>The factions (doc 10 s5 working names). Index into <see cref="GameState.Heat"/>; stored in saves and events: never renumber.</summary>
    public enum Faction
    {
        /// <summary>Scavenger clans: The Rustborn (Mother Kess).</summary>
        Rustborn = 0,

        /// <summary>Remnant military: Vanguard Command (Colonel Idris Vale).</summary>
        Vanguard = 1,

        /// <summary>AI cultists: Church of the Last Signal (The Prophet).</summary>
        Church = 2,

        /// <summary>Corporate holdouts: Halcyon Dynamics (a ghost CEO known only by voice). From Tier 3.</summary>
        Holdouts = 3,
    }

    /// <summary>Heat level (doc 10 s4): Cold 0-24, Watched 25-49, Hunted 50-74, Marked 75-100. Stored in events.</summary>
    public enum HeatLevel
    {
        Cold = 0,
        Watched = 1,
        Hunted = 2,
        Marked = 3,
    }

    /// <summary>What a map site is. Stored in events: never renumber.</summary>
    public enum SiteKind
    {
        Convoy = 0,
        Outpost = 1,
        DataCenter = 2,
        Ruins = 3,
    }

    /// <summary>Kind of operation sent from the Hub (doc 04 s8, doc 05 s5). Stored in commands and events.</summary>
    public enum OpKind
    {
        Scout = 0,
        Raid = 1,
        Hack = 2,
    }

    /// <summary>A spy in a faction camp (SPEC-019). Loyalty is hidden from the player. Stored in saves: never renumber.</summary>
    public enum SpyState
    {
        None = 0,
        Loyal = 1,

        /// <summary>Turned: feeds false intel until exposed.</summary>
        Double = 2,
    }

    /// <summary>How a spy was lost. Stored in events.</summary>
    public enum SpyLoss
    {
        Caught = 0,

        /// <summary>Unmasked as a double agent (cross-check or a backfired frame).</summary>
        Exposed = 1,
    }

    /// <summary>What the Hub knows and holds at one map site.</summary>
    public sealed class SiteState
    {
        public bool Scouted;

        /// <summary>Raided recently: nothing to take until this tick.</summary>
        public long CooldownUntilTick;

        /// <summary>Ruins cleared by a raid and open to claim.</summary>
        public bool Cleared;

        /// <summary>The Hub runs an outpost here.</summary>
        public bool Outpost;

        public void Visit(IStateVisitor v)
        {
            v.Bool(ref Scouted);
            v.Long(ref CooldownUntilTick);
            v.Bool(ref Cleared);
            v.Bool(ref Outpost);
        }
    }

    /// <summary>A squad (or a hack) in the field.</summary>
    public sealed class Operation
    {
        public int Id;

        public int Site;

        public OpKind Kind;

        /// <summary>People sent (0 for a hack).</summary>
        public int Squad;

        /// <summary>Compute committed (hacks).</summary>
        public int Compute;

        public long ReturnTick;

        public void Visit(IStateVisitor v)
        {
            v.Int(ref Id);
            v.Int(ref Site);
            int kind = (int)Kind;
            v.Int(ref kind);
            Kind = (OpKind)kind;
            v.Int(ref Squad);
            v.Int(ref Compute);
            v.Long(ref ReturnTick);
        }
    }

    /// <summary>Why an alliance ended (SPEC-025). Stored in events.</summary>
    public enum AllianceEnd
    {
        Dissolved = 0,

        /// <summary>The Hub struck the ally's sites.</summary>
        Betrayed = 1,

        /// <summary>The daily share went unpaid.</summary>
        Unpaid = 2,

        /// <summary>The ally walked out on its own.</summary>
        Walkout = 3,

        /// <summary>The ally's heat with the Hub reached Watched.</summary>
        Distrust = 4,
    }
}
