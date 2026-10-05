using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    /// <summary>SPEC-008: prerequisites and exclusive pairs hold, research applies its effect, tier gates hold.</summary>
    public class ModuleTests
    {
        [Fact]
        public void Research_RespectsPrereqsAndPairs_AndAppliesItsEffect()
        {
            var sim = new Simulation(3UL);
            sim.State.Energy = 500;
            sim.State.Compute = 100;
            Assert.Equal(RejectReason.Locked, sim.Execute(Command.StartResearch(ModuleNode.LG2A)).Reason);
            Assert.Equal(RejectReason.Locked, sim.Execute(Command.StartResearch(ModuleNode.LG3)).Reason);
            int upkeepBefore = Economy.UpkeepPerHour(sim.State, sim.Config, sim.State.Slots[1]);

            Assert.True(sim.Execute(Command.StartResearch(ModuleNode.LG1)).Accepted);
            // memory sectors restore in their own lane (SPEC-008 rule 3): only money stops M1 now, not the busy field lane
            Assert.Equal(RejectReason.NotEnoughEnergy, sim.Execute(Command.StartResearch(ModuleNode.M1)).Reason);
            sim.Run(sim.Config.Modules.ResearchMinutes[3]);

            Assert.True(Modules.Has(sim.State, ModuleNode.LG1));
            Assert.True(Economy.UpkeepPerHour(sim.State, sim.Config, sim.State.Slots[1]) < upkeepBefore);
            // doc 10 s5: a traded blueprint makes the next field research cheaper, and is spent
            sim.State.Energy = 2_000;
            Assert.True(sim.Execute(Command.Trade(Faction.Rustborn, TradeGood.Blueprints)).Accepted);
            Assert.Equal(1, sim.State.Blueprints);
            sim.State.Energy = 500;
            sim.State.Compute = 100;
            Modules.TryDef(ModuleNode.LG2B, out ModuleDef lg2b);
            Assert.True(sim.Execute(Command.StartResearch(ModuleNode.LG2B)).Accepted);
            Assert.Equal(500 - SimMath.PctFloor(sim.Config.Modules.ResearchEnergy[lg2b.Index], 100 - sim.Config.Living.BlueprintDiscountPct), sim.State.Energy);
            Assert.Equal(0, sim.State.Blueprints);
            sim.Run(sim.Config.Modules.ResearchMinutes[5]);
            Assert.Equal(RejectReason.Excluded, sim.Execute(Command.StartResearch(ModuleNode.LG2A)).Reason);
        }

        [Fact]
        public void KeyModule_NeedsARecoveredFragment_AndUsesItUp()
        {
            // SPEC-037: each field's capstone rebuilds from recovered data
            var sim = new Simulation(3UL);
            GameState s = sim.State;
            s.Tier = 4;
            foreach (ModuleDef d in Modules.Catalog)
            {
                if (d.Field == ModuleField.Warfare && d.Node != ModuleNode.WF6)
                {
                    s.Modules |= 1UL << (int)d.Node;
                }
            }

            s.Energy = 5_000;
            s.Compute = 500;
            Assert.Equal(RejectReason.NeedsFragment, sim.Execute(Command.StartResearch(ModuleNode.WF6)).Reason);
            s.DataFragments = 1;
            Assert.True(sim.Execute(Command.StartResearch(ModuleNode.WF6)).Accepted);
            Assert.Equal(0, s.DataFragments);
        }

        [Fact]
        public void Research_StrainsTheCore_AndCorruptionDecaysWithItsLevel()
        {
            // F-026 (shipped balance): heavy compute use adds corruption; decay scales with the current level
            var sim = new Simulation(4UL, TestConfigs.Get(TestConfigs.Shipped));
            var c = sim.Config.Corruption;
            sim.State.Energy = 500;
            sim.State.Compute = 100;
            Assert.True(sim.Execute(Command.StartResearch(ModuleNode.M1)).Accepted);
            Assert.Equal(sim.Config.Modules.ResearchCompute[0] * c.ComputeMilliPerPoint, sim.State.CorruptionMilli);

            sim.State.CorruptionMilli = 50_000;
            sim.Run(SimConfig.TicksPerHour - (sim.State.Tick % SimConfig.TicksPerHour));
            Assert.Equal(50_000 - c.DecayMilliPerHour - (50_000 * c.DecayPermillePerHour / 1000) + (sim.State.AutomationLoad * c.AutomationMilliPerHour), sim.State.CorruptionMilli);
        }

        [Fact]
        public void TierUp_NeedsAllThreeGates_ThenSpendsPeopleAndRaisesTheCap()
        {
            var sim = new Simulation(3UL);
            Assert.Equal(RejectReason.GateBuild, sim.Execute(Command.TierUp()).Reason);

            foreach (FacilitySlot slot in sim.State.Slots)
            {
                slot.Kind = slot.IsEmpty ? FacilityKind.Generator : slot.Kind;
                slot.Level = 3;
                slot.Powered = true;
                slot.Staffed = true;
            }

            Assert.True(Modules.Gates(sim.State, sim.Config).Build);
            Assert.Equal(RejectReason.GateModule, sim.Execute(Command.TierUp()).Reason);
            sim.State.Modules |= 1UL << (int)ModuleNode.M1;
            int capBefore = Economy.PopulationCap(sim.State, sim.Config);
            sim.State.People = 2;
            Assert.Equal(RejectReason.GatePeople, sim.Execute(Command.TierUp()).Reason);
            sim.State.People = 20;
            int cost = Modules.Gates(sim.State, sim.Config).PeopleCost;
            int slots = sim.State.Slots.Count;

            Assert.True(sim.Execute(Command.TierUp()).Accepted);
            Assert.Equal(2, sim.State.Tier);
            Assert.Equal(20 - cost, sim.State.People);
            Assert.Equal(capBefore + sim.Config.Tier.PopBonus[1], Economy.PopulationCap(sim.State, sim.Config));
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.TierAdvanced && e.A == 2);

            // SPEC-013: the district plots, and raids that hit harder and more often
            Assert.Equal(slots + sim.Config.Tier.SlotsAdded[0], sim.State.Slots.Count);
            Assert.Equal(sim.State.Slots.Count, sim.State.PowerPriority.Count);
            Assert.True(sim.State.Slots[slots].IsEmpty);
            Assert.Equal(sim.Config.Raid.MaxPerDay + sim.Config.Tier.ExtraRaidsPerDay[1], RaidSystem.MaxPerDay(sim.State, sim.Config));
            int tier2 = Defense.BaseRaidStrength(sim.State, sim.Config);
            sim.State.Tier = 1;
            Assert.Equal(SimMath.PctFloor(Defense.BaseRaidStrength(sim.State, sim.Config), sim.Config.Tier.RaidStrengthPct[1]), tier2);
        }
    }
}
