using System.Linq;
using Deadswitch.Host.Notifications;
using Deadswitch.Sim.State;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    /// <summary>SPEC-010: the logout projection never touches the live sim and forecasts the raids a copy would see.</summary>
    public class ProjectionTests
    {
        [Fact]
        public void Projection_LeavesTheLiveSimAlone_AndForecastsRaids()
        {
            var sim = new Simulation(8UL);
            sim.Run(12L * SimConfig.TicksPerHour);
            ulong hash = StateHasher.Hash(sim.State);
            int events = sim.Log.Count;

            var alerts = LogoutProjection.Project(sim, AlertKinds.All);

            Assert.Equal(hash, StateHasher.Hash(sim.State));
            Assert.Equal(events, sim.Log.Count);
            Assert.Contains(alerts, a => a.Kind == AlertKinds.Raids);
            Assert.True(alerts.Count <= sim.Config.Host.NotifyMax);
            Assert.Equal(alerts.OrderBy(a => a.InMinutes).Select(a => a.InMinutes), alerts.Select(a => a.InMinutes));
            Assert.Empty(LogoutProjection.Project(sim, AlertKinds.None));
        }
    }
}
