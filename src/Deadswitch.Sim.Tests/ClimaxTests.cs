using System.Linq;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.Persistence;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    /// <summary>SPEC-011: a visible window always precedes the climax; each answer works; the climax follows the dials.</summary>
    public class ClimaxTests
    {
        [Fact]
        public void Imminent_OpensTheWindow_AndExpiryForksABoldAi()
        {
            Simulation sim = AtImminent(boldness: 90_000, coldness: 10_000);
            Assert.True(sim.State.ClimaxAtTick > sim.State.Tick);
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.ClimaxWarned);
            sim.State.Modules |= 1UL << (int)ModuleNode.LG1;

            sim.Run(sim.State.ClimaxAtTick - sim.State.Tick);

            SimEvent climax = sim.Log.Events.Last(e => e.Kind == EventKind.Climax);
            Assert.Equal((int)ClimaxKind.Fork, climax.A);
            Assert.False(Modules.Has(sim.State, ModuleNode.LG1));
            Assert.Equal(sim.Config.Override.MaxCharges - 1, OverrideSystem.MaxCharges(sim.State, sim.Config));
            Assert.Equal(0, sim.State.ProjectMilli);
            Assert.Equal(0, sim.State.ClimaxAtTick);
        }

        [Fact]
        public void ColdAi_Betrays_WithARaidTheTurretsDoNotFace()
        {
            Simulation sim = AtImminent(boldness: 10_000, coldness: 90_000);
            sim.Run(sim.State.ClimaxAtTick - sim.State.Tick + 2);

            SimEvent climax = sim.Log.Events.Last(e => e.Kind == EventKind.Climax);
            Assert.Equal((int)ClimaxKind.Betrayal, climax.A);
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.RaidResolved && e.A == climax.B);
            Assert.True(sim.State.People > 0);
        }

        [Fact]
        public void Silence_PausesTheWindow_CancelNeedsAnAudit_PurgeEndsIt()
        {
            Simulation sim = AtImminent(boldness: 90_000, coldness: 0);
            long at = sim.State.ClimaxAtTick;
            sim.State.Compute = 100;

            Assert.Equal(RejectReason.NeedsAudit, sim.Execute(Command.CancelProject()).Reason);
            Assert.True(sim.Execute(Command.UseOverride(OverrideKind.Silence)).Accepted);
            Assert.Equal(at + (sim.Config.Climax.SilenceHours * SimConfig.TicksPerHour), sim.State.ClimaxAtTick);
            Assert.Equal(DelegationLevel.Manual, sim.State.Delegation);
            Assert.Equal(RejectReason.Silenced, sim.Execute(Command.SetDelegation(DelegationLevel.Autopilot)).Reason);

            sim.State.Energy = 500;
            Assert.True(sim.Execute(Command.PurgeCore()).Accepted);
            Assert.Equal(0, sim.State.ClimaxAtTick);
            Assert.Equal(0, sim.State.Compute);
            Assert.Equal(0, sim.State.ProjectMilli);
        }

        [Fact]
        public void Betrayal_WaitsForTheMercyWindow()
        {
            Simulation sim = AtImminent(boldness: 0, coldness: 90_000);
            sim.State.MercyUntilTick = sim.State.ClimaxAtTick + 120;
            sim.Run(sim.State.ClimaxAtTick - sim.State.Tick + 60);
            Assert.DoesNotContain(sim.Log.Events, e => e.Kind == EventKind.Climax);

            sim.Run(120);
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.Climax && e.A == (int)ClimaxKind.Betrayal);
        }

        [Fact]
        public void SaveLoad_MidWindow_ContinuesIdentically()
        {
            Simulation live = AtImminent(boldness: 90_000, coldness: 0);
            live.Execute(Command.UseOverride(OverrideKind.Silence));
            Simulation loaded = SaveGame.Load(SaveGame.Write(live), live.Config).Simulation;

            long rest = live.State.ClimaxAtTick - live.State.Tick + 30;
            live.Run(rest);
            loaded.Run(rest);

            Assert.Equal(StateHasher.Hash(live.State), StateHasher.Hash(loaded.State));
            Assert.Equal(live.Log.Events, loaded.Log.Events);
        }

        private static Simulation AtImminent(int boldness, int coldness)
        {
            var sim = new Simulation(6UL);
            sim.Execute(Command.SetDelegation(DelegationLevel.Autopilot));
            sim.State.BoldnessMilli = boldness;
            sim.State.ColdnessMilli = coldness;
            sim.State.ProjectMilli = (sim.Config.Project.ImminentFrom * 1000) - 1;
            while (sim.State.ClimaxAtTick == 0)
            {
                sim.Step();
            }

            sim.State.BoldnessMilli = boldness;
            sim.State.ColdnessMilli = coldness;
            return sim;
        }
    }
}
