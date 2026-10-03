using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim
{
    /// <summary>
    /// Deterministic tick loop (1 tick = 1 game minute). This is the paper-prototype "pressure loop":
    /// energy, compute, people, corruption and one raid type. Grow it per docs/specs/SPEC-001-pressure-loop.md.
    /// Invariant: Run(a) then Run(b) must equal Run(a + b) (offline catch-up relies on it).
    /// </summary>
    public sealed class Simulation
    {
        public Simulation(ulong seed, SimConfig? config = null)
        {
            Config = config ?? SimConfig.Tier1();
            State = new GameState(seed, Config);
            Log = new EventLog();
        }

        public SimConfig Config { get; }

        public GameState State { get; }

        public EventLog Log { get; }

        public void Run(long ticks)
        {
            for (long i = 0; i < ticks; i++)
            {
                Step();
            }
        }

        public void Step()
        {
            GameState s = State;
            SimConfig c = Config;
            s.Tick++;

            if (s.Tick % SimConfig.TicksPerDay == 0)
            {
                s.RaidsToday = 0;
            }

            bool hadEnergy = s.Energy > 0;
            s.Energy = Clamp(s.Energy + c.EnergyGenPerTick - c.EnergyUpkeepPerTick, 0, c.EnergyCap);

            if (s.Energy >= c.RackEnergyCostPerTick)
            {
                s.Energy -= c.RackEnergyCostPerTick;
                s.Compute = Clamp(s.Compute + c.ComputePerTick, 0, c.ComputeCap);
            }

            if (hadEnergy && s.Energy == 0)
            {
                Log.Append(new SimEvent(s.Tick, EventKind.BlackoutStarted, 0, 0));
            }

            if (s.Tick % SimConfig.TicksPerHour == 0)
            {
                HourlyUpdate();
            }

            TryRaid();
        }

        private void HourlyUpdate()
        {
            GameState s = State;
            SimConfig c = Config;

            s.Corruption = Clamp(s.Corruption - c.CorruptionDecayPerHour, 0, c.CorruptionCap);

            // Regrowth pauses during blackouts (Energy == 0).
            if (s.Energy > 0 && s.People < c.PeopleCap)
            {
                int gap = c.PeopleCap - s.People;
                int gain = ((gap * c.PeopleRegrowthPercentOfGapPerHour) + 99) / 100;
                s.People = Clamp(s.People + gain, 0, c.PeopleCap);
            }
        }

        private void TryRaid()
        {
            GameState s = State;
            SimConfig c = Config;

            // Always consume exactly one RNG draw per tick so cap checks never desync the stream.
            bool roll = s.Rng.NextBelow(c.RaidMeanIntervalTicks) == 0;
            if (!roll || s.RaidsToday >= c.MaxRaidsPerDay)
            {
                return;
            }

            int loot = (s.Energy * c.RaidLootPercentOfEnergy) / 100;
            if (loot > c.RaidLootCap)
            {
                loot = c.RaidLootCap;
            }

            s.Energy -= loot;
            s.RaidsToday++;
            Log.Append(new SimEvent(s.Tick, EventKind.RaidStarted, loot, 0));
        }

        private static int Clamp(int v, int lo, int hi)
        {
            return v < lo ? lo : (v > hi ? hi : v);
        }
    }
}
