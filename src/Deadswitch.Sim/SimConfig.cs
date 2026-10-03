namespace Deadswitch.Sim
{
    /// <summary>
    /// Tuning values. 1 tick = 1 game minute. Integers only.
    /// Source of truth for numbers: docs/design/10_resolved_decisions.md (section 3). All are (tune).
    /// Treat instances as immutable after constructing a Simulation.
    /// </summary>
    public sealed class SimConfig
    {
        public const int TicksPerHour = 60;
        public const int TicksPerDay = 1440;

        public int EnergyStart = 200;
        public int EnergyCap = 500;
        public int EnergyGenPerTick = 8; // doc 10 said 6, but 6 - 4 upkeep - 3 rack = -1/min (blackout in ~3h). Corrected, see SPEC-001.
        public int EnergyUpkeepPerTick = 4;

        public int FuelStart = 100;
        public int FuelCap = 300;

        public int ComputeStart = 50;
        public int ComputeCap = 100;
        public int ComputePerTick = 1;
        public int RackEnergyCostPerTick = 3;

        public int PeopleStart = 12;
        public int PeopleCap = 20;
        public int PeopleRegrowthPercentOfGapPerHour = 5;

        public int CorruptionCap = 100;
        public int CorruptionDecayPerHour = 1;

        /// <summary>Raid chance each tick is 1 / RaidMeanIntervalTicks (about every 6h in tier 1).</summary>
        public uint RaidMeanIntervalTicks = 360;
        public int RaidLootPercentOfEnergy = 20;
        public int RaidLootCap = 60;
        public int MaxRaidsPerDay = 3;

        public static SimConfig Tier1()
        {
            return new SimConfig();
        }
    }
}
