using System.Linq;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    public class SimulationTests
    {
        private const long Month = 30L * SimConfig.TicksPerDay;

        [Fact]
        public void SameSeed_ChunkedOrWhole_GivesSameState()
        {
            // Offline catch-up relies on this: stepping in chunks must not change the outcome.
            var whole = new Simulation(99UL);
            whole.Run(Month);

            var chunked = new Simulation(99UL);
            chunked.Run(1000);
            chunked.Run(1);
            chunked.Run(Month - 1001);

            Assert.Equal(StateHasher.Hash(whole.State), StateHasher.Hash(chunked.State));
        }

        [Theory]
        [InlineData(TestConfigs.Defaults)]
        [InlineData(TestConfigs.Shipped)]
        public void Tier1Defaults_HoldCoreGuarantees(string config)
        {
            // Resource caps, raid cadence cap, loot cap, and no blackout at default economy numbers.
            var sim = new Simulation(123UL, TestConfigs.Get(config));
            SimConfig c = sim.Config;
            for (long i = 0; i < Month; i++)
            {
                sim.Step();
                GameState s = sim.State;
                Assert.InRange(s.Energy, 0, Economy.EnergyCap(s, c));
                Assert.InRange(s.Compute, 0, c.Compute.Cap);
                Assert.InRange(s.People, 0, Economy.PopulationCap(s, c));
                Assert.InRange(s.CorruptionMilli, 0, CorruptionSystem.MaxMilli);
            }

            Assert.All(
                sim.Log.Events.Where(e => e.Kind == EventKind.LossLine && e.B == (int)LossResource.Energy),
                e => Assert.InRange(e.C, 1, c.Raid.LootCap));
            Assert.All(
                sim.Log.Events.Where(e => e.Kind == EventKind.RaidWarning).GroupBy(e => e.Tick / SimConfig.TicksPerDay),
                day => Assert.True(day.Count() <= RaidSystem.MaxPerDay(sim.State, c)));
            Assert.DoesNotContain(sim.Log.Events, e => e.Kind == EventKind.BlackoutStarted);
        }
    }
}
