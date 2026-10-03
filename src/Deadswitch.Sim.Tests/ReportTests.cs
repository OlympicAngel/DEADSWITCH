using System.Linq;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    /// <summary>SPEC-006: Verify exposes the lies a report hides, costs compute, and works once.</summary>
    public class ReportTests
    {
        [Fact]
        public void Verify_ExposesTheFirstLie_CostsCompute_AndOnlyOnce()
        {
            var sim = new Simulation(1UL);
            while (!sim.State.RaidRecords.Any(r => r.RaidId == 1))
            {
                sim.Step();
            }

            int compute = sim.State.Compute;
            Assert.True(sim.Execute(Command.VerifyReport(1)).Accepted);

            SimEvent verified = sim.Log.Events.Last();
            Assert.Equal(EventKind.ReportVerified, verified.Kind);
            Assert.Equal(RaidRecord.GateLie, verified.B & RaidRecord.GateLie);
            Assert.Equal(compute - sim.Config.Report.VerifyComputeCost, sim.State.Compute);
            Assert.Equal(RejectReason.AlreadyVerified, sim.Execute(Command.VerifyReport(1)).Reason);
            Assert.Equal(RejectReason.NoReport, sim.Execute(Command.VerifyReport(999)).Reason);
        }

        [Fact]
        public void CorruptedAi_EditsBreachSummaries_AndRecordsEveryEdit()
        {
            SimConfig config = SimConfig.Tier1();
            config.Corruption.DecayMilliPerHour = 0;
            var sim = new Simulation(4UL, config);
            sim.State.CorruptionMilli = 70_000;
            sim.Run(6L * SimConfig.TicksPerDay);

            var edits = sim.Log.Events.Where(e => e.Kind == EventKind.AdvisorLied && e.A == (int)LieKind.ReportEdit).ToList();
            Assert.NotEmpty(edits);
            foreach (SimEvent e in edits)
            {
                Assert.True(e.D < e.C);
                Assert.Contains(sim.Log.Events, l => l.Kind == EventKind.LossLine && l.A == e.B && l.B == (int)LossResource.Energy && l.C == e.C);
            }
        }
    }
}
