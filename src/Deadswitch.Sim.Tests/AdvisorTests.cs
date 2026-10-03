using System.Linq;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    /// <summary>SPEC-004: the first lie is told and is checkable; delegation runs the base without blackouts.</summary>
    public class AdvisorTests
    {
        [Theory]
        [InlineData(1UL)]
        [InlineData(7UL)]
        [InlineData(42UL)]
        public void FirstLieRaid_ReportsTheOppositeGate_AndContactRevealsTheTruth(ulong seed)
        {
            var sim = new Simulation(seed);
            sim.Run(5L * SimConfig.TicksPerDay);

            int raid = sim.Config.Ai.FirstLieRaid;
            SimEvent vector = sim.Log.Events.First(e => e.Kind == EventKind.RaidVector && e.A == raid);
            SimEvent contact = sim.Log.Events.First(e => e.Kind == EventKind.RaidContact && e.A == raid);
            SimEvent lie = sim.Log.Events.First(e => e.Kind == EventKind.AdvisorLied);

            Assert.NotEqual(contact.B, vector.B);
            Assert.Equal((int)LieKind.RaidGate, lie.A);
            Assert.Equal(raid, lie.B);
            Assert.Equal(contact.B, lie.C);
            Assert.Equal(vector.B, lie.D);
        }

        [Theory]
        [InlineData(TestConfigs.Defaults)]
        [InlineData(TestConfigs.Shipped)]
        public void DelegatedRoutines_GrowTheBaseForAWeek_WithoutBlackouts(string config)
        {
            var sim = new Simulation(3UL, TestConfigs.Get(config));
            Assert.True(sim.Execute(Command.SetDelegation(DelegationLevel.Delegated)).Accepted);
            int levelsBefore = sim.State.Slots.Sum(s => s.Level);

            sim.Run(7L * SimConfig.TicksPerDay);

            Assert.DoesNotContain(sim.Log.Events, e => e.Kind == EventKind.BlackoutStarted);
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.AiActed && e.A == (int)AiActionKind.Build);
            Assert.True(sim.State.Slots.Sum(s => s.Level) >= levelsBefore + 6);
            Assert.True(sim.State.BoldnessMilli > 0);
        }
    }
}
