namespace Deadswitch.Sim.State
{
    /// <summary>Facility types (SPEC-002). Stored in saves and commands: never renumber or reuse.</summary>
    public enum FacilityKind
    {
        None = 0,
        Generator = 1,
        ServerRack = 2,
        LifeSupport = 3,
        BatteryBank = 4,
        Turret = 5,

        /// <summary>Compact fission plant (doc 02 s3 risky power): huge output for fuel; raiders go for it (SPEC-029).</summary>
        Reactor = 6,

        /// <summary>Drone Bay (doc 02 s6 Military, SPEC-035): reprogrammed machines; drones beat infantry.</summary>
        DroneBay = 7,

        /// <summary>Motor Pool (vehicle factory): armour that burns fuel; vehicles beat drones.</summary>
        MotorPool = 8,

        /// <summary>Solar Field (doc 02 s3, SPEC-038): slow, safe power by day only, no fuel.</summary>
        SolarField = 9,

        /// <summary>Fuel Depot (doc 02 s6): more fuel storage.</summary>
        FuelDepot = 10,

        /// <summary>Cooling Tower (doc 02 s6 AI core): heavy compute use corrupts the core less.</summary>
        CoolingTower = 11,

        /// <summary>Memory Restoration Chamber (doc 02 s6 AI core): the memory lane restores faster.</summary>
        MemoryChamber = 12,
    }
}
