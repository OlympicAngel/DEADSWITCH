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
    }
}
