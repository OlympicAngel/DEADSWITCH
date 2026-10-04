using System.Linq;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    /// <summary>SPEC-015: each signature has its own losses; the purge always climbs the ladder; the shield holds.</summary>
    public class ThreatTests
    {
        [Fact]
        public void Siege_BreaksABuilding_AndStartsMercy()
        {
            var sim = new Simulation(21UL);
            sim.State.Tier = 2;
            sim.State.Slots[0].Level = 3;
            sim.State.NextSiegeTick = 1;
            for (int h = 0; h < 96 && !sim.Log.Events.Any(e => e.Kind == EventKind.FacilityDamaged); h++)
            {
                sim.Run(SimConfig.TicksPerHour);
            }

            SimEvent warn = sim.Log.Events.First(e => e.Kind == EventKind.RaidWarning && e.D == (int)AttackKind.Siege);
            SimEvent hit = sim.Log.Events.First(e => e.Kind == EventKind.FacilityDamaged);
            Assert.Equal(warn.A, hit.A);
            Assert.True(sim.State.Slots[0].Level < 3);
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.MercyStarted && e.A == warn.A);
        }

        [Fact]
        public void Virus_IsBurnedByComputeReserve_OrLocksAModuleAndPoisonsIntel()
        {
            var sim = new Simulation(22UL);
            sim.State.Modules |= (1UL << (int)ModuleNode.M1) | (1UL << (int)ModuleNode.LG1);
            sim.Run(1);
            sim.State.Compute = 100;
            sim.State.NextVirusTick = sim.State.Tick + 1;
            sim.Run(1);
            Assert.Equal(0, sim.Log.Events.Last(e => e.Kind == EventKind.VirusStruck).A);
            Assert.Equal(0, sim.State.LockedModule);

            sim.State.Compute = 0;
            int corruption = sim.State.CorruptionMilli;
            sim.State.NextVirusTick = sim.State.Tick + 1;
            sim.Run(1);
            Assert.Equal(1, sim.Log.Events.Last(e => e.Kind == EventKind.VirusStruck).A);
            Assert.Equal((int)ModuleNode.LG1, sim.State.LockedModule);
            Assert.False(Modules.Has(sim.State, ModuleNode.LG1));
            Assert.True(Modules.IsRestored(sim.State, ModuleNode.LG1));
            Assert.True(sim.State.FalseIntel);
            Assert.True(sim.State.CorruptionMilli > corruption);
        }

        [Fact]
        public void Purge_ClimbsTheLadder_AndTributeCallsItOff()
        {
            var sim = new Simulation(23UL);
            var t = sim.Config.Threats;
            sim.State.Tier = 2;
            sim.State.Heat[(int)Faction.Vanguard] = 80_000;
            sim.Run(1);
            sim.State.NextPurgeTick = sim.State.Tick + 1;
            sim.Run(1);
            Assert.Equal(PurgeStage.Rumor, sim.State.PurgeStage);
            sim.State.PurgeReal = true;
            Assert.Equal(RejectReason.NoTarget, sim.Execute(Command.PayPurgeTribute()).Reason);

            sim.Run(t.PurgeRumorHours * SimConfig.TicksPerHour);
            Assert.Equal(PurgeStage.Staging, sim.State.PurgeStage);
            sim.Run((t.PurgeStagingHours - t.PurgeUltimatumHours) * SimConfig.TicksPerHour);
            Assert.Equal(PurgeStage.Ultimatum, sim.State.PurgeStage);

            sim.State.Energy = t.PurgeTributeEnergy;
            sim.State.Compute = t.PurgeTributeCompute;
            Assert.True(sim.Execute(Command.PayPurgeTribute()).Accepted);
            Assert.Equal(PurgeStage.None, sim.State.PurgeStage);
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.PurgeLadder && e.A == 0 && e.C == 2);
        }

        [Fact]
        public void Shield_RisesAfterItsDelay_PausesAttacks_AndHalvesUpkeep()
        {
            // doc 10: 2 h activation delay, 72 h pause, upkeep halved, limited charges
            var sim = new Simulation(24UL);
            var t = sim.Config.Threats;
            sim.Run(sim.Config.Opening.ProtectionHours * SimConfig.TicksPerHour);
            while (sim.State.RaidId != 0)
            {
                sim.Run(1);
            }

            int upkeep = Economy.UpkeepPerHour(sim.State, sim.Config, sim.State.Slots[1]);
            Assert.True(sim.Execute(Command.ActivateShield()).Accepted);
            Assert.Equal(RejectReason.NoChange, sim.Execute(Command.ActivateShield()).Reason);
            sim.Run(t.ShieldDelayMinutes);
            while (sim.State.RaidId != 0)
            {
                sim.Run(1);
            }

            Assert.True(ThreatSystem.Shielded(sim.State));
            Assert.True(Economy.UpkeepPerHour(sim.State, sim.Config, sim.State.Slots[1]) < upkeep);
            int warnings = sim.Log.Events.Count(e => e.Kind == EventKind.RaidWarning);
            sim.Run(48 * SimConfig.TicksPerHour);
            Assert.Equal(warnings, sim.Log.Events.Count(e => e.Kind == EventKind.RaidWarning));
            Assert.Equal(RejectReason.NoChange, sim.Execute(Command.ActivateShield()).Reason);
        }

        [Fact]
        public void Operation_ComesHome_WithHeatOnTheOwner_AndEndsMercy()
        {
            var sim = new Simulation(25UL);
            sim.State.Fuel = 100;
            sim.State.MercyUntilTick = 10_000;
            int people = sim.State.People;
            Assert.True(sim.Execute(Command.LaunchOp(1, OpKind.Raid, 4)).Accepted);
            Assert.Equal(people - 4, sim.State.People);
            Assert.Equal(sim.State.Tick, sim.State.MercyUntilTick);

            sim.Run(2 * WorldSystem.Sites[1].TravelHours * SimConfig.TicksPerHour);
            SimEvent back = sim.Log.Events.Last(e => e.Kind == EventKind.OpReturned);
            Assert.Empty(sim.State.Ops);
            Assert.True(sim.State.Heat[(int)Faction.Rustborn] > 0);
            Assert.Equal(1, back.B);
        }

        [Fact]
        public void WarlordUltimatum_FiresOnce_ForAHubStuckInTier1_AndTheWaveResolves()
        {
            var sim = new Simulation(31UL);
            int day = sim.Config.Living.UltimatumDay;
            sim.Run(((long)day + 4) * SimConfig.TicksPerDay);

            Assert.Equal(UltimatumStage.Done, sim.State.Ultimatum);
            Assert.Single(sim.Log.Events, e => e.Kind == EventKind.UltimatumIssued);
            SimEvent end = sim.Log.Events.Single(e => e.Kind == EventKind.UltimatumResolved);
            Assert.Equal((int)UltimatumOutcome.Wave, end.A);
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.RaidResolved && e.A == end.B);

            // trade: a hard daily cap per faction
            sim.State.Heat[(int)Faction.Rustborn] = 0;
            sim.State.Energy = 400;
            sim.State.Compute = 0;
            for (int i = 0; i < sim.Config.Living.TradesPerDay; i++)
            {
                Assert.True(sim.Execute(Command.Trade(Faction.Rustborn, TradeGood.Compute)).Accepted);
                sim.State.Compute = 0;
            }

            Assert.Equal(RejectReason.TradeCap, sim.Execute(Command.Trade(Faction.Rustborn, TradeGood.Compute)).Reason);
        }
    }
}
