using System.Linq;
using Deadswitch.Sim.Events;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    /// <summary>SPEC-009: the opening raid comes on cue at its fixed strength, then nothing until protection ends.</summary>
    public class OpeningTests
    {
        [Theory]
        [InlineData(1UL)]
        [InlineData(9UL)]
        public void OpeningRaid_OnCue_ThenProtection(ulong seed)
        {
            var sim = new Simulation(seed);
            long protection = (long)sim.Config.Opening.ProtectionHours * SimConfig.TicksPerHour;
            sim.Run(protection + (2L * SimConfig.TicksPerDay));

            var warnings = sim.Log.Events.Where(e => e.Kind == EventKind.RaidWarning).ToList();
            Assert.Equal(sim.Config.Opening.RaidAtMinute, warnings[0].Tick);
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.RaidResolved && e.A == 1 && e.C <= sim.Config.Opening.RaidStrength * 115 / 100);
            Assert.True(warnings[1].Tick >= protection);
        }
    }
}
