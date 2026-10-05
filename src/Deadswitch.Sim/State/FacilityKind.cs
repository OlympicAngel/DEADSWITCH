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
    }
}
