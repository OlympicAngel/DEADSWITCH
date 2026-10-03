using Deadswitch.Sim.Config;

namespace Deadswitch.Sim
{
    /// <summary>
    /// Every tunable number in the sim, grouped into sections. 1 tick = 1 game minute. Integers only.
    /// Code defaults are the doc 10 baseline; the game runs the shipped balance file
    /// (<c>Resources/DeadswitchBalance.toml</c>, read with <see cref="BalanceText"/>), which may diverge while tuning.
    /// Treat instances as immutable after constructing a Simulation.
    /// </summary>
    public sealed class SimConfig
    {
        public const int TicksPerHour = 60;
        public const int TicksPerDay = 1440;

        public EnergyConfig Energy = new EnergyConfig();
        public FuelConfig Fuel = new FuelConfig();
        public ComputeConfig Compute = new ComputeConfig();
        public PeopleConfig People = new PeopleConfig();
        public CorruptionConfig Corruption = new CorruptionConfig();
        public RaidConfig Raid = new RaidConfig();

        /// <summary>The doc 10 baseline values.</summary>
        public static SimConfig Tier1()
        {
            return new SimConfig();
        }

        /// <summary>Deep copy (round-trips through the balance text, which also proves the writer and reader agree).</summary>
        public SimConfig Clone()
        {
            return BalanceText.Parse(BalanceText.Write(this));
        }

        /// <summary>Stable 64-bit identity of every value. See <see cref="ConfigHasher"/>.</summary>
        public ulong ComputeHash()
        {
            return ConfigHasher.Hash(this);
        }

        /// <summary>Visits every section in a fixed order. The order defines the balance file layout and the config hash.</summary>
        public void Visit(IConfigVisitor visitor)
        {
            Energy.Visit(visitor);
            Fuel.Visit(visitor);
            Compute.Visit(visitor);
            People.Visit(visitor);
            Corruption.Visit(visitor);
            Raid.Visit(visitor);
        }
    }
}
