namespace Deadswitch.Sim.State
{
    /// <summary>Legacy perks (doc 10 s6). Index into <see cref="GameState.Perks"/>; stored in saves: never renumber.</summary>
    public enum Perk
    {
        StartResources = 0,
        Regrowth = 1,
        Override = 2,
        HeatDecay = 3,
        Veterans = 4,
    }

    /// <summary>Mastery challenges (doc 10 s6 starter list). Bit index into <see cref="GameState.Mastery"/>: never renumber.</summary>
    public enum Mastery
    {
        /// <summary>Survive a purge without AI help (Manual delegation, no OVERRIDE during it).</summary>
        PurgeNoAi = 0,

        /// <summary>Reach Tier 2 without losing an outpost.</summary>
        NoOutpostLost = 1,

        /// <summary>Win a live battle with no casualties.</summary>
        CleanBattle = 2,

        /// <summary>Complete a tier with delegation on Manual.</summary>
        ManualTier = 3,

        /// <summary>Catch one of the AI's lies with Verify.</summary>
        CatchLie = 4,

        /// <summary>Keep corruption under 40% for a full tier.</summary>
        CleanCore = 5,

        /// <summary>Relocate at peak power.</summary>
        RelocatePeak = 6,
    }

    /// <summary>Why a cycle ended. Stored in events.</summary>
    public enum RebootReason
    {
        Relocation = 0,
        HubDestroyed = 1,
        AiTakeover = 2,
        PopulationCollapse = 3,
    }

    /// <summary>Where the core settles after a reboot (SPEC-031, doc 06 s4 better starting region). Stored in saves and commands.</summary>
    public enum Region
    {
        /// <summary>A sheltered hollow: no bonus (the first site, and old saves).</summary>
        Hollow = 0,

        /// <summary>High ground: turrets see farther.</summary>
        Ridge = 1,

        /// <summary>A river crossing: barges bring fuel every hour.</summary>
        River = 2,

        /// <summary>A dead data center's ruins: salvaged racks think faster.</summary>
        Ruins = 3,
    }
}
