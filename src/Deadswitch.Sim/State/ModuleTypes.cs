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

        /// <summary>Fire Control: turret output up.</summary>
        WF1 = 20,

        /// <summary>Drilled Militia: more defense per defender (pair with WF2B).</summary>
        WF2A = 21,

        /// <summary>Kill Zones: Turtle bonus up (pair with WF2A).</summary>
        WF2B = 22,

        /// <summary>Combat Simulations: raid odds up.</summary>
        WF3 = 23,

        /// <summary>Hardened Walls: sieges and purges weaker.</summary>
        WF4 = 24,

        /// <summary>Assault Doctrine: fewer raid casualties (pair with WF5B).</summary>
        WF5A = 25,

        /// <summary>Rapid Response: longer attack warnings (pair with WF5A).</summary>
        WF5B = 26,

        /// <summary>Veteran Cadre: squad strength up.</summary>
        WF6 = 27,

        /// <summary>Firewall: viruses weaker.</summary>
        CY1 = 30,

        /// <summary>Counter-intrusion: hack odds up (pair with CY2B).</summary>
        CY2A = 31,

        /// <summary>Signal Interception: better attack estimates (pair with CY2A).</summary>
        CY2B = 32,

        /// <summary>Clean Room: faster corruption decay.</summary>
        CY3 = 33,

        /// <summary>Black ICE: infections lock no module.</summary>
        CY4 = 34,

        /// <summary>Worm Factory: hacks take more compute (pair with CY5B).</summary>
        CY5A = 35,

        /// <summary>Ghost Protocol: hacks raise no heat (pair with CY5A).</summary>
        CY5B = 36,

        /// <summary>Core Partition: corruption settles lower.</summary>
        CY6 = 37,

        /// <summary>Signal Masking: Dark posture misses more.</summary>
        ST1 = 40,

        /// <summary>Long-range Eyes: site defense known without scouting (pair with ST2B).</summary>
        ST2A = 41,

        /// <summary>Quiet Routes: operations use less fuel (pair with ST2A).</summary>
        ST2B = 42,

        /// <summary>Heat Sink: faction heat fades faster.</summary>
        ST3 = 43,

        /// <summary>Decoys: every attack weaker.</summary>
        ST4 = 44,

        /// <summary>Prediction Engine: longer attack warnings (pair with ST5B).</summary>
        ST5A = 45,

        /// <summary>False Trails: operations raise less heat (pair with ST5A).</summary>
        ST5B = 46,

        /// <summary>Ghost Network: one more operation slot.</summary>
        ST6 = 47,
    }

    /// <summary>Module fields (doc 03 s7): the trunk plus four specialisation fields.</summary>
    public enum ModuleField
    {
        Trunk = 0,
        Logistics = 1,
        Warfare = 2,
        Cyber = 3,
        Stealth = 4,
    }
}
