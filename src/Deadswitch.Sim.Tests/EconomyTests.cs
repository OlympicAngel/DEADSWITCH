using System.Linq;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.Persistence;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    public class EconomyTests
    {
        [Fact]
        public void Reactor_IsTierLocked_OnePerHub_AndScramsWithoutFuel()
        {
            // SPEC-029: from Tier 3 only, one per Hub, and no fuel means no output
            var sim = new Simulation(17UL);
            int plot = sim.State.Slots.FindIndex(x => x.IsEmpty);
            Assert.Equal(RejectReason.Locked, sim.Execute(Command.Build(plot, FacilityKind.Reactor)).Reason);

            sim.State.Tier = 3;
            sim.State.Energy = Economy.EnergyCap(sim.State, sim.Config);
            sim.State.Compute = sim.Config.Compute.Cap;
            sim.State.Slots[plot].Kind = FacilityKind.Reactor;
            sim.State.Slots[plot].Level = 1;
            int other = sim.State.Slots.FindIndex(x => x.IsEmpty);
            Assert.Equal(RejectReason.Locked, sim.Execute(Command.Build(other, FacilityKind.Reactor)).Reason);

            // fuel burns every minute it runs: an hour's need is gone within the hour, then it scrams
            sim.State.Fuel = sim.Config.ReactorRules.FuelPerHour[0];
            sim.Run(2L * SimConfig.TicksPerHour);
            Assert.False(sim.State.ReactorFueled);
            Assert.Equal(0, Economy.EffectiveOutput(sim.State, sim.Config, sim.State.Slots[plot]));
            Simulation loaded = SaveGame.Load(SaveGame.Write(sim), sim.Config).Simulation;
            Assert.Equal(StateHasher.Hash(sim.State), StateHasher.Hash(loaded.State));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(59)]
        [InlineData(60)]
        [InlineData(61)]
        [InlineData(240)]
        [InlineData(480)]
        [InlineData(1_000_003)]
        public void PerHourRates_DeliverExactlyTheRateEveryHour_Evenly(int perHour)
        {
            for (long start = 0; start < 3 * 60; start += 17)
            {
                long sum = 0;
                int min = int.MaxValue;
                int max = int.MinValue;
                for (long t = start; t < start + 60; t++)
                {
                    int a = Rates.PerTick(perHour, t);
                    sum += a;
                    min = System.Math.Min(min, a);
                    max = System.Math.Max(max, a);
                }

                Assert.Equal(perHour, sum);
                Assert.True(max - min <= 1, "uneven delivery");
            }
        }

        [Fact]
        public void StartingLayout_ReproducesDoc10Rates()
        {
            var sim = new Simulation(1UL);
            sim.Step();

            EconomyFlows f = Economy.Flows(sim.State, sim.Config);

            Assert.Equal(480, f.GenerationPerHour);
            Assert.Equal(240, f.CoreUpkeepPerHour);
            Assert.Equal(180, f.FacilityUpkeepPerHour);
            Assert.Equal(60, f.NetEnergyPerHour);
            Assert.Equal(60, f.ComputePerHour);
            Assert.Equal(500, f.EnergyCap);
            Assert.Equal(20, f.PopulationCap);
            Assert.Equal(0, f.AutomationLoad);
        }

        [Fact]
        public void StartingLayout_NetsSixtyEnergyPerHour()
        {
            Simulation sim = Quiet(2UL);

            sim.Run(SimConfig.TicksPerHour);

            Assert.Equal(200 + 60, sim.State.Energy);
            Assert.Equal(100, sim.State.Compute);
        }

        [Fact]
        public void Build_PaysUpFront_AndCompletesAfterBuildMinutes()
        {
            Simulation sim = Quiet(3UL);
            sim.Run(10);
            int energy = sim.State.Energy;

            CommandResult r = sim.Execute(Command.Build(2, FacilityKind.BatteryBank));

            Assert.True(r.Accepted, r.ToString());
            Assert.Equal(energy - 100, sim.State.Energy);
            BuildJob job = Assert.Single(sim.State.Jobs);
            Assert.Equal(10 + 15, job.CompleteTick);
            sim.Run(14);
            Assert.True(sim.State.Slots[2].IsEmpty);
            sim.Run(1);
            Assert.Equal(FacilityKind.BatteryBank, sim.State.Slots[2].Kind);
            Assert.Equal(1, sim.State.Slots[2].Level);
            Assert.Empty(sim.State.Jobs);
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.BuildCompleted && e.A == 2 && e.C == 1);
            Assert.Equal(750, Economy.EnergyCap(sim.State, sim.Config));
        }

        [Fact]
        public void Build_Validation()
        {
            Simulation sim = Quiet(4UL);

            Assert.Equal(RejectReason.SlotOccupied, sim.Execute(Command.Build(0, FacilityKind.BatteryBank)).Reason);
            Assert.Equal(RejectReason.InvalidSlot, sim.Execute(Command.Build(99, FacilityKind.BatteryBank)).Reason);
            Assert.Equal(RejectReason.InvalidSlot, sim.Execute(Command.Build(-1, FacilityKind.BatteryBank)).Reason);
            Assert.Equal(RejectReason.InvalidArgument, sim.Execute(Command.Build(2, FacilityKind.None)).Reason);
            Assert.Equal(RejectReason.InvalidArgument, sim.Execute(Command.Build(2, (FacilityKind)77)).Reason);
            Assert.Equal(RejectReason.NotEnoughEnergy, sim.Execute(Command.Build(2, FacilityKind.Generator)).Reason);
            Assert.True(sim.Execute(Command.Build(2, FacilityKind.BatteryBank)).Accepted);
            Assert.Equal(RejectReason.JobInProgress, sim.Execute(Command.Build(2, FacilityKind.BatteryBank)).Reason);
            Assert.Equal(RejectReason.QueueFull, sim.Execute(Command.Build(3, FacilityKind.BatteryBank)).Reason);
            Assert.Single(sim.Commands.Commands);
        }

        [Fact]
        public void Build_SecondOfAKind_CostsMore()
        {
            Simulation sim = Quiet(5UL);
            sim.State.Energy = 500;

            Economy.BuildCost(sim.State, sim.Config, FacilityKind.ServerRack, out int energy, out _);
            Assert.Equal(240, energy);
            Economy.BuildCost(sim.State, sim.Config, FacilityKind.LifeSupport, out int lifeSupport, out _);
            Assert.Equal(200, lifeSupport);

            Assert.True(sim.Execute(Command.Build(2, FacilityKind.ServerRack)).Accepted);
            Assert.Equal(500 - 240, sim.State.Energy);
        }

        [Fact]
        public void Upgrade_UsesNextLevelCost_KeepsRunningAtOldLevel()
        {
            Simulation sim = Quiet(6UL);
            sim.State.Energy = 500;

            Assert.True(sim.Execute(Command.Upgrade(0)).Accepted);
            Assert.Equal(500 - 260, sim.State.Energy);

            sim.Run(44);
            Assert.Equal(1, sim.State.Slots[0].Level);
            Assert.Equal(480, Economy.Flows(sim.State, sim.Config).GenerationPerHour);
            sim.Run(1);
            Assert.Equal(2, sim.State.Slots[0].Level);
            Assert.Equal(660, Economy.Flows(sim.State, sim.Config).GenerationPerHour);
        }

        [Fact]
        public void Upgrade_Validation()
        {
            Simulation sim = Quiet(7UL);
            sim.State.Slots[0].Level = sim.Config.Generator.MaxLevel;

            Assert.Equal(RejectReason.MaxLevel, sim.Execute(Command.Upgrade(0)).Reason);
            Assert.Equal(RejectReason.SlotEmpty, sim.Execute(Command.Upgrade(4)).Reason);
            Assert.Equal(RejectReason.InvalidSlot, sim.Execute(Command.Upgrade(6)).Reason);
            Assert.Equal(0, sim.Commands.Count);
        }

        [Fact]
        public void Upgrade_NeedsCompute()
        {
            Simulation sim = Quiet(8UL);
            sim.State.Energy = 500;
            sim.State.Compute = 14;

            Assert.Equal(RejectReason.NotEnoughCompute, sim.Execute(Command.Upgrade(1)).Reason);
            sim.State.Compute = 15;
            Assert.True(sim.Execute(Command.Upgrade(1)).Accepted);
            Assert.Equal(0, sim.State.Compute);
        }

        [Fact]
        public void Cancel_RefundsHalf()
        {
            Simulation sim = Quiet(9UL);
            sim.State.Energy = 500;
            sim.Execute(Command.Upgrade(0));

            Assert.Equal(RejectReason.NoJob, sim.Execute(Command.CancelJob(1)).Reason);
            Assert.True(sim.Execute(Command.CancelJob(0)).Accepted);

            Assert.Equal(500 - 260 + 130, sim.State.Energy);
            Assert.Empty(sim.State.Jobs);
            Assert.Equal(1, sim.State.Slots[0].Level);
        }

        [Fact]
        public void Demolish_RefundsAQuarter_AndFreesTheSlot()
        {
            Simulation sim = Quiet(10UL);
            int energy = sim.State.Energy;

            Assert.True(sim.Execute(Command.Demolish(1)).Accepted);

            Assert.True(sim.State.Slots[1].IsEmpty);
            Assert.Equal(energy + 30, sim.State.Energy);
            Assert.Equal(RejectReason.SlotEmpty, sim.Execute(Command.Demolish(1)).Reason);
        }

        [Fact]
        public void Demolish_WhileUpgrading_IsRejected()
        {
            Simulation sim = Quiet(11UL);
            sim.State.Energy = 500;
            sim.Execute(Command.Upgrade(0));

            Assert.Equal(RejectReason.JobInProgress, sim.Execute(Command.Demolish(0)).Reason);
        }

        [Fact]
        public void Shortage_ShedsLowestPriorityFirst_AndPriorityCanBeChanged()
        {
            Simulation sim = Quiet(12UL);
            Place(sim, 2, FacilityKind.ServerRack, 1);
            Place(sim, 3, FacilityKind.ServerRack, 1);
            sim.State.Energy = 0;

            // Generation 480 - core 240 leaves 240/h: only one of three racks (180/h each) fits.
            sim.Run(5);
            Assert.True(sim.State.Slots[1].Powered);
            Assert.False(sim.State.Slots[2].Powered);
            Assert.False(sim.State.Slots[3].Powered);
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.FacilityShed && e.A == 2);

            Assert.True(sim.Execute(Command.SetPriority(3, 0)).Accepted);
            Assert.Equal(new[] { 3, 0, 1, 2, 4, 5 }, sim.State.PowerPriority);
            sim.State.Slots[3].Powered = true; // already running from the handler's point of view
            sim.State.Slots[1].Powered = false;
            sim.Run(5);
            Assert.True(sim.State.Slots[3].Powered);
            Assert.False(sim.State.Slots[1].Powered);
        }

        [Fact]
        public void ShedFacility_RestartsOnlyWithAnHourOfUpkeepInHand()
        {
            Simulation sim = Quiet(13UL);
            Place(sim, 2, FacilityKind.ServerRack, 1);
            Place(sim, 3, FacilityKind.ServerRack, 1);
            sim.State.Energy = 0;
            sim.Run(5);
            Assert.False(sim.State.Slots[2].Powered);

            // Switch the first rack off: 240/h surplus now, so rack 2 restarts once stock >= 3 + 180.
            sim.Execute(Command.SetFacilityPower(1, false));
            long restartTick = 0;
            for (int i = 0; i < 120 && restartTick == 0; i++)
            {
                int before = sim.State.Energy;
                sim.Step();
                if (sim.State.Slots[2].Powered)
                {
                    restartTick = sim.State.Tick;
                    Assert.True(before + 8 - 4 >= 3 + 180);
                }
            }

            Assert.NotEqual(0, restartTick);
            int restarts = sim.Log.Events.Count(e => e.Kind == EventKind.FacilityRestored && e.A == 2);
            sim.Run(SimConfig.TicksPerDay);
            Assert.Equal(restarts, sim.Log.Events.Count(e => e.Kind == EventKind.FacilityRestored && e.A == 2));
        }

        [Fact]
        public void NoGeneration_DrainsStock_ThenBlackout_ThenRecovers()
        {
            Simulation sim = Quiet(14UL);
            Place(sim, 2, FacilityKind.LifeSupport, 1);
            sim.Execute(Command.SetFacilityPower(0, false));

            sim.Run(3 * SimConfig.TicksPerHour);

            Assert.True(sim.State.Blackout);
            Assert.Equal(0, sim.State.Energy);
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.BlackoutStarted);
            Assert.All(sim.State.Slots.Skip(1).Where(x => !x.IsEmpty), x => Assert.False(x.Powered));
            Assert.Equal(20, Economy.PopulationCap(sim.State, sim.Config));

            int people = sim.State.People = 10;
            sim.Run(SimConfig.TicksPerHour);
            Assert.Equal(people, sim.State.People);

            sim.Execute(Command.SetFacilityPower(0, true));
            sim.Run(5);
            Assert.False(sim.State.Blackout);
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.BlackoutEnded);
        }

        [Fact]
        public void SwitchedOffFacility_CostsNothing_AndProducesNothing()
        {
            Simulation sim = Quiet(15UL);
            sim.State.Compute = 0;
            sim.Execute(Command.SetFacilityPower(1, false));

            sim.Run(SimConfig.TicksPerHour);

            Assert.Equal(0, sim.State.Compute);
            Assert.Equal(200 + 240, sim.State.Energy);
            Assert.Equal(RejectReason.NoChange, sim.Execute(Command.SetFacilityPower(1, false)).Reason);
            Assert.Equal(RejectReason.InvalidArgument, sim.Execute(new Command(CommandKind.SetFacilityPower, 1, 2)).Reason);
            Assert.Equal(RejectReason.SlotEmpty, sim.Execute(Command.SetFacilityPower(4, true)).Reason);
        }

        [Fact]
        public void ShortHanded_FacilitiesRunUnmanned_AtReducedOutput()
        {
            Simulation sim = Quiet(16UL);
            sim.State.People = 2;
            sim.State.Compute = 0;

            sim.Run(SimConfig.TicksPerHour - 1);
            Assert.True(sim.State.Slots[0].Staffed);
            Assert.False(sim.State.Slots[1].Staffed);
            Assert.Equal(1, sim.State.AutomationLoad);
            Assert.Equal(30, Economy.Flows(sim.State, sim.Config).ComputePerHour);

            sim.Execute(Command.SetPriority(1, 0));
            sim.Step();
            Assert.True(sim.State.Slots[1].Staffed);
            Assert.False(sim.State.Slots[0].Staffed);
            Assert.Equal(240, Economy.Flows(sim.State, sim.Config).GenerationPerHour);
        }

        [Fact]
        public void LifeSupport_RaisesPopulationCap_OnlyWhilePowered()
        {
            Simulation sim = Quiet(17UL);
            Place(sim, 0, FacilityKind.Generator, 2); // 660 - 240 core - 180 rack - 120 life support = +120/h
            Place(sim, 2, FacilityKind.LifeSupport, 1);
            sim.Run(SimConfig.TicksPerDay);

            Assert.Equal(26, Economy.PopulationCap(sim.State, sim.Config));
            Assert.Equal(26, sim.State.People);

            sim.Execute(Command.SetFacilityPower(2, false));
            sim.Run(SimConfig.TicksPerHour * 3);
            Assert.Equal(20, Economy.PopulationCap(sim.State, sim.Config));
            Assert.Equal(26, sim.State.People);
        }

        [Fact]
        public void EconomyRun_SaveLoadReplay_AreExact()
        {
            Simulation live = Builder(18UL, SimConfig.TicksPerDay);
            byte[] bytes = SaveGame.Write(live);
            Simulation loaded = SaveGame.Load(bytes, SimConfig.Tier1()).Simulation;
            Simulation replay = Replay.Run(18UL, SimConfig.Tier1(), live.Commands.Commands, live.State.Tick);

            Assert.Equal(StateHasher.Hash(live.State), StateHasher.Hash(loaded.State));
            Assert.Equal(StateHasher.Hash(live.State), StateHasher.Hash(replay.State));

            ContinueBuilding(live, SimConfig.TicksPerDay);
            ContinueBuilding(loaded, SimConfig.TicksPerDay);
            Assert.Equal(StateHasher.Hash(live.State), StateHasher.Hash(loaded.State));
            Assert.Equal(live.Log.Events, loaded.Log.Events);
        }

        [Theory]
        [InlineData(TestConfigs.Defaults)]
        [InlineData(TestConfigs.Shipped)]
        public void SensibleBuilder_GrowsForAWeek_WithoutBlackouts(string config)
        {
            var sim = new Simulation(19UL, TestConfigs.Get(config));
            ContinueBuilding(sim, 7L * SimConfig.TicksPerDay);

            Assert.DoesNotContain(sim.Log.Events, e => e.Kind == EventKind.BlackoutStarted);
            int levels = sim.State.Slots.Sum(x => x.Level);
            Assert.True(levels >= 10, "builder only reached " + levels + " total levels");
            Assert.True(Economy.Flows(sim.State, sim.Config).NetEnergyPerHour > 0);
        }

        internal static Simulation Builder(ulong seed, long ticks)
        {
            var sim = new Simulation(seed);
            ContinueBuilding(sim, ticks);
            return sim;
        }

        /// <summary>
        /// A scripted "sensible builder": every 30 minutes, if the queue is free, buy the first affordable
        /// item from a plan that keeps net energy positive (power first, then storage, people, compute).
        /// </summary>
        internal static void ContinueBuilding(Simulation sim, long ticks)
        {
            long end = sim.State.Tick + ticks;
            while (sim.State.Tick < end)
            {
                if (sim.State.Jobs.Count == 0)
                {
                    TryPlan(sim);
                }

                sim.Run(System.Math.Min(30, end - sim.State.Tick));
            }
        }

        private static void TryPlan(Simulation sim)
        {
            GameState s = sim.State;
            for (int i = 0; i < s.Slots.Count; i++)
            {
                if (s.Slots[i].Damage > 0 && sim.Execute(Command.Repair(i)).Accepted)
                {
                    return;
                }
            }

            EconomyFlows f = Economy.Flows(s, sim.Config);
            if (f.NetEnergyPerHour < 120 && sim.Execute(Command.Upgrade(0)).Accepted)
            {
                return;
            }

            int battery = s.Slots.FindIndex(x => x.Kind == FacilityKind.BatteryBank);
            if (battery < 0)
            {
                sim.Execute(Command.Build(2, FacilityKind.BatteryBank));
                return;
            }

            int life = s.Slots.FindIndex(x => x.Kind == FacilityKind.LifeSupport);
            if (life < 0 && f.NetEnergyPerHour > 200)
            {
                sim.Execute(Command.Build(3, FacilityKind.LifeSupport));
                return;
            }

            if (f.NetEnergyPerHour > 300 && sim.Execute(Command.Upgrade(1)).Accepted)
            {
                return;
            }

            if (sim.Execute(Command.Upgrade(battery)).Accepted)
            {
                return;
            }

            sim.Execute(Command.Upgrade(0));
        }

        /// <summary>A run with raids effectively disabled, for exact economy arithmetic.</summary>
        private static Simulation Quiet(ulong seed)
        {
            SimConfig c = SimConfig.Tier1();
            c.Raid.MaxPerDay = 0;
            return new Simulation(seed, c);
        }

        private static void Place(Simulation sim, int slot, FacilityKind kind, int level)
        {
            FacilitySlot x = sim.State.Slots[slot];
            x.Kind = kind;
            x.Level = level;
            x.Enabled = true;
            x.Powered = true;
            x.Staffed = true;
        }
    }
}
