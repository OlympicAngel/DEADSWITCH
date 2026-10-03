namespace Deadswitch.Sim.State
{
    /// <summary>AI module tree nodes (SPEC-008). Values are bit indices in GameState.Modules and are stored: never renumber.</summary>
    public enum ModuleNode
    {
        None = 0,

        /// <summary>Memory sector 1: trunk, gates Tier 2.</summary>
        M1 = 1,

        /// <summary>Memory sector 2: trunk, gates Tier 3.</summary>
        M2 = 2,

        /// <summary>Memory sector 3: trunk, gates Tier 4.</summary>
        M3 = 3,

        /// <summary>Load Balancing: facility upkeep down.</summary>
        LG1 = 10,

        /// <summary>Overclocked Racks: Server Rack output up (pair with LG2B).</summary>
        LG2A = 11,

        /// <summary>Deep Cells: Battery Bank capacity up (pair with LG2A).</summary>
        LG2B = 12,

        /// <summary>Habitat Management: population cap up.</summary>
        LG3 = 13,

        /// <summary>Prefab Assembly: build time down.</summary>
        LG4 = 14,

        /// <summary>Salvage Doctrine: cancel and demolish refunds up (pair with LG5B).</summary>
        LG5A = 15,

        /// <summary>Fuel Cells: Generator output up (pair with LG5A).</summary>
        LG5B = 16,

        /// <summary>Automation Protocols: unmanned facilities produce more.</summary>
        LG6 = 17,
    }

    /// <summary>Module fields (doc 03 s7). Only the trunk and Logistics exist in v1 so far.</summary>
    public enum ModuleField
    {
        Trunk = 0,
        Logistics = 1,
    }
}
