using System.Linq;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    /// <summary>SPEC-007: reliance feeds the hidden project; the Audit tells the truth for a price.</summary>
    public class ProjectTests
    {
        [Fact]
        public void BoldAi_GrowsTheProjectAndSkims_ManualAiStaysDormant()
        {
            var bold = new Simulation(5UL);
            bold.Execute(Command.SetDelegation(DelegationLevel.Autopilot));
            var manual = new Simulation(5UL);

            bold.Run(7L * SimConfig.TicksPerDay);
            manual.Run(7L * SimConfig.TicksPerDay);

            Assert.True(ProjectSystem.Stage(bold.Config, bold.State.ProjectMilli) >= ProjectStage.Active);
            Assert.True(bold.State.SkimmedSinceAudit > 0);
            Assert.Contains(bold.Log.Events, e => e.Kind == EventKind.ProjectStage);
            Assert.Equal(0, manual.State.ProjectMilli);
            Assert.Equal(0, manual.State.SkimmedSinceAudit);
        }

        [Fact]
        public void Audit_CostsCompute_CoolsDown_AndReportsTheTruth()
        {
            var sim = new Simulation(5UL);
            sim.Config.Secrets.NodeCompute = 10;
            sim.Config.Secrets.FromBoldnessPct = 0;
            sim.Execute(Command.SetDelegation(DelegationLevel.Autopilot));
            sim.Run(5L * SimConfig.TicksPerDay);
            Assert.Equal(RejectReason.NothingPending, sim.Execute(Command.DismantleSecrets()).Reason);
            int compute = sim.State.Compute;
            int skimmed = sim.State.SkimmedSinceAudit;

            Assert.True(sim.Execute(Command.Audit()).Accepted);

            SimEvent run = sim.Log.Events.First(e => e.Kind == EventKind.AuditRun);
            SimEvent drain = sim.Log.Events.First(e => e.Kind == EventKind.AuditDrain);
            Assert.Equal(sim.State.BoldnessMilli, run.C);
            Assert.Equal(sim.State.CorruptionMilli, run.D);
            Assert.Equal(skimmed, drain.A);
            Assert.Equal(compute - sim.Config.Project.AuditComputeCost, sim.State.Compute);
            Assert.Equal(0, sim.State.SkimmedSinceAudit);
            Assert.Equal(RejectReason.OnCooldown, sim.Execute(Command.Audit()).Reason);

            // SPEC-034: the Audit exposes the AI's hidden nodes; tearing them down sets its project back
            int nodes = sim.State.SecretNodes;
            Assert.True(nodes > 0);
            Assert.Equal(nodes, sim.Log.Events.First(e => e.Kind == EventKind.SecretExposed).A);
            int project = sim.State.ProjectMilli;
            Assert.True(sim.Execute(Command.DismantleSecrets()).Accepted);
            Assert.Equal(0, sim.State.SecretNodes);
            Assert.Equal(System.Math.Max(0, project - (nodes * sim.Config.Secrets.DismantleProjectMilli)), sim.State.ProjectMilli);
        }
    }
}
