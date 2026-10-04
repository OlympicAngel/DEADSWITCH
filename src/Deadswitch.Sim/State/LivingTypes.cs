namespace Deadswitch.Sim.State
{
    /// <summary>Warlord Ultimatum (doc 10 s4, forced event). Stored in saves: never renumber.</summary>
    public enum UltimatumStage
    {
        None = 0,

        /// <summary>Mother Kess has named her price; the deadline runs.</summary>
        Issued = 1,

        /// <summary>Paid, cancelled by a tier-up, or the wave came. Fires once per run.</summary>
        Done = 2,
    }

    /// <summary>How an ultimatum ended. Stored in events.</summary>
    public enum UltimatumOutcome
    {
        Paid = 0,
        Outgrown = 1,
        Wave = 2,
    }

    /// <summary>Dilemma events (doc 05 s4). Choice 0 takes the offer, 1 refuses (the default when it expires). Stored in saves and events.</summary>
    public enum DilemmaKind
    {
        None = 0,

        /// <summary>Risky gamble: fuel for energy now; may be a trap.</summary>
        Trader = 1,

        /// <summary>Take refugees in: more people, maybe a spy.</summary>
        Refugees = 2,

        /// <summary>AI temptation: resources now, corruption later.</summary>
        Shortcut = 3,

        /// <summary>Vanguard deserters: arm them (Vanguard heat) or hand them back (goodwill).</summary>
        Deserters = 4,

        /// <summary>A Church broadcast: clean data, or something hidden in it.</summary>
        ChurchSignal = 5,
    }

    /// <summary>Rotating world events (doc 05 s7, doc 04 named phases). Stored in saves and events.</summary>
    public enum WorldEventKind
    {
        None = 0,

        /// <summary>Hacks harder, corruption creeps up.</summary>
        SignalStorm = 1,

        /// <summary>Operations bring back more; traders sell cheaper.</summary>
        SupplyWindow = 2,

        /// <summary>The wastes go quiet: attacks come less often.</summary>
        DeadWeek = 3,
    }

    /// <summary>What the Hub can buy from a faction (doc 10 s5; people are never tradeable). Stored in commands and events.</summary>
    public enum TradeGood
    {
        /// <summary>Fuel, paid in energy.</summary>
        Fuel = 0,

        /// <summary>Energy cells, paid in fuel.</summary>
        EnergyCells = 1,

        /// <summary>Compute fragments, paid in energy.</summary>
        Compute = 2,
    }
}
