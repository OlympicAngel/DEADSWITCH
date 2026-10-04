using System.Linq;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
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

        [Fact]
        public void Tier1Defaults_HoldCoreGuarantees()
        {
            // Resource caps, raid cadence cap, loot cap, and no blackout at default economy numbers.
            var sim = new Simulation(123UL);
            for (long i = 0; i < Month; i++)
            {
                sim.Step();
                GameState s = sim.State;
                Assert.InRange(s.Energy, 0, sim.Config.EnergyCap);
                Assert.InRange(s.Compute, 0, sim.Config.ComputeCap);
                Assert.InRange(s.People, 0, sim.Config.PeopleCap);
                Assert.InRange(s.Corruption, 0, sim.Config.CorruptionCap);
            }

            var raids = sim.Log.Events.Where(e => e.Kind == EventKind.RaidStarted).ToList();
            Assert.All(raids, e => Assert.InRange(e.A, 0, sim.Config.RaidLootCap));
            Assert.All(
                raids.GroupBy(e => e.Tick / SimConfig.TicksPerDay),
                day => Assert.True(day.Count() <= sim.Config.MaxRaidsPerDay));
            Assert.DoesNotContain(sim.Log.Events, e => e.Kind == EventKind.BlackoutStarted);
        }
    }
}
