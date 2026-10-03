using System.Linq;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    public class SimulationTests
    {
        private const long ThreeDays = 3L * SimConfig.TicksPerDay;

        [Fact]
        public void SameSeed_SameResult()
        {
            var a = new Simulation(42UL);
            var b = new Simulation(42UL);
            a.Run(ThreeDays);
            b.Run(ThreeDays);

            Assert.Equal(StateHasher.Hash(a.State), StateHasher.Hash(b.State));
            Assert.Equal(a.Log.Count, b.Log.Count);
        }

        [Fact]
        public void DifferentSeeds_Diverge()
        {
            var a = new Simulation(1UL);
            var b = new Simulation(2UL);
            a.Run(ThreeDays);
            b.Run(ThreeDays);

            Assert.NotEqual(StateHasher.Hash(a.State), StateHasher.Hash(b.State));
        }

        [Fact]
        public void ChunkedRun_EqualsSingleRun()
        {
            // Offline catch-up relies on this: stepping in chunks must not change the outcome.
            var whole = new Simulation(99UL);
            whole.Run(ThreeDays);

            var chunked = new Simulation(99UL);
            chunked.Run(1000);
            chunked.Run(1);
            chunked.Run(ThreeDays - 1001);

            Assert.Equal(StateHasher.Hash(whole.State), StateHasher.Hash(chunked.State));
        }

        [Fact]
        public void ResourcesNeverExceedCapsOrGoNegative()
        {
            var sim = new Simulation(5UL);
            for (long i = 0; i < ThreeDays; i++)
            {
                sim.Step();
                GameState s = sim.State;
                Assert.InRange(s.Energy, 0, sim.Config.Energy.Cap);
                Assert.InRange(s.Compute, 0, sim.Config.Compute.Cap);
                Assert.InRange(s.People, 0, sim.Config.People.Cap);
                Assert.InRange(s.Corruption, 0, sim.Config.Corruption.Cap);
            }
        }

        [Fact]
        public void RaidsPerDay_NeverExceedOfflineCap()
        {
            var sim = new Simulation(123UL);
            sim.Run(30L * SimConfig.TicksPerDay);

            var perDay = sim.Log.Events
                .Where(e => e.Kind == EventKind.RaidStarted)
                .GroupBy(e => e.Tick / SimConfig.TicksPerDay);

            foreach (var day in perDay)
            {
                Assert.True(day.Count() <= sim.Config.Raid.MaxPerDay);
            }
        }

        [Fact]
        public void Tier1Defaults_DoNotBlackOutOverAWeek()
        {
            // Guards the economy numbers: generation must outpace upkeep + rack cost.
            var sim = new Simulation(77UL);
            sim.Run(7L * SimConfig.TicksPerDay);

            Assert.DoesNotContain(sim.Log.Events, e => e.Kind == EventKind.BlackoutStarted);
        }

        [Fact]
        public void RaidLoot_IsCapped()
        {
            var sim = new Simulation(321UL);
            sim.Run(30L * SimConfig.TicksPerDay);

            foreach (SimEvent e in sim.Log.Events.Where(x => x.Kind == EventKind.RaidStarted))
            {
                Assert.InRange(e.A, 0, sim.Config.Raid.LootCap);
            }
        }
    }
}
