using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.Systems;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    /// <summary>SPEC-012: each ruthless choice costs lives, loyalty and makes the AI colder; floors and gates hold.</summary>
    public class PeopleChoiceTests
    {
        [Fact]
        public void ForcedLabor_KillsBoostsAndCools_ThenWaitsForCooldown()
        {
            var sim = new Simulation(11UL);
            var p = sim.Config.PeopleChoices;
            int people = sim.State.People;
            int coldness = sim.State.ColdnessMilli;

            Assert.True(sim.Execute(Command.ForcedLabor()).Accepted);

            Assert.Equal(people - p.SurgeDeaths, sim.State.People);
            Assert.Equal(coldness + p.SurgeColdness, sim.State.ColdnessMilli);
            Assert.Equal(100_000 - p.SurgeLoyalty, sim.State.LoyaltyMilli);
            Assert.Equal(p.SurgeOutputPts, PeopleChoices.OutputPts(sim.State, sim.Config));
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.ForcedLabor);
            Assert.Equal(RejectReason.OnCooldown, sim.Execute(Command.ForcedLabor()).Reason);
        }

        [Fact]
        public void Cleanse_TradesPeopleForCorruption_CrackdownNeedsUnrest()
        {
            var sim = new Simulation(12UL);
            var p = sim.Config.PeopleChoices;
            sim.State.CorruptionMilli = 30_000;

            Assert.Equal(RejectReason.LoyaltyHolds, sim.Execute(Command.Crackdown()).Reason);
            Assert.True(sim.Execute(Command.NeuralCleanse(2)).Accepted);
            Assert.Equal(30_000 - (2 * p.CleansePerPerson), sim.State.CorruptionMilli);

            sim.State.LoyaltyMilli = (p.MutinousBelow * 1000) - 1;
            Assert.True(sim.Execute(Command.Crackdown()).Accepted);
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.LoyaltyChanged);

            sim.State.People = p.MinPeople + 1;
            Assert.Equal(RejectReason.NotEnoughPeople, sim.Execute(Command.NeuralCleanse(2)).Reason);
        }
    }
}
