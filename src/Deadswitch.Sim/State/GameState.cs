using Deadswitch.Sim.Rng;

namespace Deadswitch.Sim.State
{
    /// <summary>All mutable sim state. Every field here must be included in StateHasher.</summary>
    public sealed class GameState
    {
        public GameState(ulong seed, SimConfig config)
        {
            Rng = Pcg32.Create(seed);
            Energy = config.EnergyStart;
            Fuel = config.FuelStart;
            Compute = config.ComputeStart;
            People = config.PeopleStart;
        }

        public long Tick { get; set; }

        public int Energy { get; set; }

        public int Fuel { get; set; }

        public int Compute { get; set; }

        public int People { get; set; }

        public int Corruption { get; set; }

        public int RaidsToday { get; set; }

        public Pcg32 Rng { get; set; }
    }
}
