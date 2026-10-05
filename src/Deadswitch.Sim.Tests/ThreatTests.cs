using System.Linq;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.Persistence;
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
            // live battle (SPEC-020): a siege with the handler present is commanded at the wall before it resolves
            SimEvent battle = sim.Log.Events.First(e => e.Kind == EventKind.BattleStarted);
            Assert.Equal(warn.A, battle.A);
            Assert.True(battle.Tick < hit.Tick);

            // battle scars (SPEC-018): the breach leaves damage and wrecks; damage cuts output until repaired
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.FacilityScarred && e.A == warn.A);
            Assert.True(sim.State.Wreckage > 0);
            FacilitySlot gen = sim.State.Slots[0];
            gen.Damage = 0;
            int whole = Economy.EffectiveOutput(sim.State, sim.Config, gen);
            gen.Damage = 2;
            Assert.True(Economy.EffectiveOutput(sim.State, sim.Config, gen) < whole);
            sim.State.Energy = ScarSystem.RepairCost(sim.Config, gen);
            Assert.True(sim.Execute(Command.Repair(0)).Accepted);
            sim.Run(2L * sim.Config.Scars.RepairMinutesPerPoint);
            Assert.Equal(0, gen.Damage);
            Assert.Equal(whole, Economy.EffectiveOutput(sim.State, sim.Config, gen));
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

            // adaptive enemies (SPEC-027): a beaten faction fortifies its sites, so the next raid there meets more
            Assert.Equal(back.C == 1 ? 1 : 0, sim.State.Fortified[(int)Faction.Rustborn]);

            // spies (SPEC-019): a double agent talks sites down until a scout's report exposes it
            sim.State.Spies[(int)Faction.Rustborn] = SpyState.Double;
            Assert.True(WorldSystem.EstimatedDefense(sim.State, sim.Config, 2) < WorldSystem.Sites[2].Defense);
            Assert.True(sim.Execute(Command.LaunchOp(2, OpKind.Scout, 6)).Accepted);
            sim.Run(2 * WorldSystem.Sites[2].TravelHours * SimConfig.TicksPerHour);
            SimEvent scout = sim.Log.Events.Last(e => e.Kind == EventKind.OpReturned);
            Assert.Equal(scout.C == 1 ? SpyState.None : SpyState.Double, sim.State.Spies[(int)Faction.Rustborn]);

            // sabotage (SPEC-026): a small team only; when it lands, the owner's attacks are crippled for a while
            sim.State.Fuel = 100;
            Assert.Equal(RejectReason.InvalidArgument, sim.Execute(Command.LaunchOp(3, OpKind.Sabotage, sim.Config.World.SabotageMaxSquad + 1)).Reason);
            Assert.True(sim.Execute(Command.LaunchOp(3, OpKind.Sabotage, 2)).Accepted);
            sim.Run(2 * WorldSystem.Sites[3].TravelHours * SimConfig.TicksPerHour);
            SimEvent sabotage = sim.Log.Events.Last(e => e.Kind == EventKind.OpReturned);
            Assert.Equal(sabotage.C == 1 ? (int)WorldSystem.Sites[3].Owner : -1, sim.State.SabotageFaction);
        }

        [Fact]
        public void HazardZones_PayAndHurt_WithoutHeat_AndFalloutDrifts()
        {
            // SPEC-032: a wild zone belongs to nobody (no heat, no sabotage); plague pays in survivors and can infect the Hub
            var sim = new Simulation(31UL);
            sim.Config.Hazards.PlagueInfectionPct = 100;
            sim.State.Fuel = 200;
            int plague = Enumerable.Range(0, WorldSystem.Sites.Count).First(i => WorldSystem.Sites[i].Kind == SiteKind.Plague);
            Assert.Equal(RejectReason.InvalidArgument, sim.Execute(Command.LaunchOp(plague, OpKind.Sabotage, 2)).Reason);
            Assert.True(sim.Execute(Command.LaunchOp(plague, OpKind.Raid, 6)).Accepted);
            sim.Run(2 * WorldSystem.Sites[plague].TravelHours * SimConfig.TicksPerHour);
            SimEvent back = sim.Log.Events.Last(e => e.Kind == EventKind.OpReturned);
            Assert.All(sim.State.Heat, h => Assert.Equal(0, h));
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.PlagueInfection);
            Assert.Equal(back.C == 1, sim.Log.Events.Any(e => e.Kind == EventKind.SurvivorsFound));

            // the fallout front settles on a faction site, drifts on schedule, and survives a save
            sim.Run((long)sim.Config.Hazards.FalloutFirstDay * SimConfig.TicksPerDay);
            int first = sim.State.FalloutSite;
            Assert.True(first >= 0 && !HazardSystem.Wild(WorldSystem.Sites[first].Kind));
            Assert.True(WorldSystem.FuelCost(sim.State, sim.Config, first, OpKind.Raid) > 2 * WorldSystem.Sites[first].TravelHours * sim.Config.World.FuelPerTravelHour);
            sim.Run(sim.State.NextFalloutTick - sim.State.Tick + SimConfig.TicksPerHour);
            Assert.NotEqual(first, sim.State.FalloutSite);
            Simulation loaded = SaveGame.Load(SaveGame.Write(sim), sim.Config).Simulation;
            Assert.Equal(StateHasher.Hash(sim.State), StateHasher.Hash(loaded.State));
        }

        [Fact]
        public void UnitFamilies_CounterTheRaidMix()
        {
            // SPEC-035: drones beat infantry, vehicles beat drones; no counters without an attack
            var sim = new Simulation(7UL);
            GameState s = sim.State;
            FacilitySlot bay = s.Slots[2];
            bay.Kind = FacilityKind.DroneBay;
            bay.Level = 1;
            bay.Enabled = true;
            bay.Powered = true;
            bay.Staffed = true;
            int plain = Defense.Family(s, sim.Config, UnitFamily.Drones, 0);
            Assert.Equal(sim.Config.DroneBay.Output[0], plain);
            s.RaidId = 99;
            s.RaidInfantryPct = 80;
            s.RaidDronePct = 10;
            s.RaidVehiclePct = 10;
            Assert.True(Defense.Family(s, sim.Config, UnitFamily.Drones, 0) > plain);
            s.RaidInfantryPct = 10;
            s.RaidVehiclePct = 80;
            Assert.True(Defense.Family(s, sim.Config, UnitFamily.Drones, 0) < plain);

            // a real raid names its forces, and the mix always sums to 100
            var run = new Simulation(11UL);
            run.Run(3L * SimConfig.TicksPerDay);
            SimEvent forces = run.Log.Events.First(e => e.Kind == EventKind.RaidForces);
            Assert.Equal(100, forces.B + forces.C + forces.D);
        }

        [Fact]
        public void Relocation_CarriesTheLegacy_AndTheNewSiteSavesExactly()
        {
            var sim = new Simulation(51UL);
            sim.Config.Chapters.TwistFallbackHours = 12;
            sim.Config.Chapters.PayoffPoints = 1;
            Deadswitch.Host.Dev.ScriptedPlayer.Play(sim, 8L * SimConfig.TicksPerDay);
            Assert.True(sim.State.Tier >= 2);

            // chapters (SPEC-024): First Boot twisted and paid off with a fragment, and fragments outlive the core
            int fragments = sim.State.Fragments;
            Assert.Equal(1, fragments & 1);
            while (!sim.Execute(Command.Relocate(Region.River)).Accepted)
            {
                sim.Run(SimConfig.TicksPerHour);
            }

            // a fresh site (doc 10 s1.2): tier 1 again, full legacy bonus, kept modules, raid ids and the day's cap keep counting
            long moved = sim.State.Tick;
            int raids = sim.State.RaidsToday;
            SimEvent end = sim.Log.Events.Last(e => e.Kind == EventKind.CycleEnded);
            Assert.Equal(1, sim.State.Tier);
            Assert.Equal(fragments, sim.State.Fragments);
            Assert.Equal(Region.River, sim.State.Region);
            Assert.Equal(end.B, end.C);
            Assert.Equal(end.C, sim.State.LegacyPoints);
            Assert.Contains(Modules.Catalog, d => d.Field != ModuleField.Trunk && Modules.IsRestored(sim.State, d.Node));
            if (moved % SimConfig.TicksPerDay != 0)
            {
                Assert.Equal(raids, sim.State.RaidsToday);
            }

            // first-time schedules start from the new site, and the run saves and replays exactly across the move
            sim.Run(SimConfig.TicksPerDay);
            Assert.Contains(sim.Log.Events, e => e.Kind == EventKind.ChapterOpened && e.Tick > moved && e.A == 1);
            Assert.DoesNotContain(sim.Log.Events, e => e.Kind == EventKind.DilemmaOffered && e.Tick > moved && e.Tick < moved + (sim.Config.Living.DilemmaFirstHour * SimConfig.TicksPerHour));
            Simulation loaded = SaveGame.Load(SaveGame.Write(sim), sim.Config).Simulation;
            Assert.Equal(StateHasher.Hash(sim.State), StateHasher.Hash(loaded.State));
            Simulation replay = Replay.Run(51UL, sim.Config, sim.Commands.Commands, sim.State.Tick);
            Assert.Equal(StateHasher.Hash(sim.State), StateHasher.Hash(replay.State));
        }

        [Fact]
        public void BoldAi_RaidsWithoutOrders_AndTheHandlerCanRecallIt()
        {
            // SPEC-030: under delegation a bold AI launches its own raid; recalling brings the squad straight home
            var sim = new Simulation(73UL);
            sim.Config.Ai.InitiativePctPerHour = 100;
            sim.State.Fuel = 100;
            Assert.True(sim.Execute(Command.SetDelegation(DelegationLevel.Delegated)).Accepted);
            sim.State.BoldnessMilli = 100_000;
            sim.Run((long)(sim.Config.Opening.ProtectionHours + 2) * SimConfig.TicksPerHour);
            while (sim.State.Ops.Count == 0 && sim.State.Tick < 10L * SimConfig.TicksPerDay)
            {
                sim.State.Fuel = 100;
                sim.Run(SimConfig.TicksPerHour);
            }

            Operation op = Assert.Single(sim.State.Ops);
            Assert.True(op.ByAi);
            Simulation loaded = SaveGame.Load(SaveGame.Write(sim), sim.Config).Simulation;
            Assert.True(Assert.Single(loaded.State.Ops).ByAi);
            int people = sim.State.People;
            Assert.True(sim.Execute(Command.RecallOp(op.Id)).Accepted);
            Assert.Empty(sim.State.Ops);
            Assert.Equal(people + op.Squad, sim.State.People);
        }

        [Fact]
        public void Ceasefire_KeepsAFactionAway_UntilTheHubStrikesIt()
        {
            var sim = new Simulation(61UL);
            sim.State.Energy = 500;
            Assert.True(sim.Execute(Command.ProposeCeasefire(Faction.Rustborn)).Accepted);
            Assert.Equal(RejectReason.PactActive, sim.Execute(Command.ProposeCeasefire(Faction.Vanguard)).Reason);
            sim.Run(2L * SimConfig.TicksPerDay);
            long since = sim.Log.Events.First(e => e.Kind == EventKind.CeasefireStarted).Tick;
            Assert.DoesNotContain(sim.Log.Events, e => e.Kind == EventKind.AttackerIdentified && e.Tick > since && e.B == (int)Faction.Rustborn);

            // striking its convoy breaks the pact and the heat spikes (SPEC-023)
            sim.State.Fuel = 100;
            int heat = sim.State.Heat[(int)Faction.Rustborn];
            Assert.True(sim.Execute(Command.LaunchOp(1, OpKind.Raid, 4)).Accepted || sim.State.People < 8);
            if (sim.State.Ops.Count > 0)
            {
                Assert.False(DiplomacySystem.Ceasefire(sim.State, Faction.Rustborn));
                Assert.True(sim.State.Heat[(int)Faction.Rustborn] > heat);
            }

            // an alliance (SPEC-025): only with a Cold faction, its fighters man the wall, and it ends unpaid
            var ally = new Simulation(62UL);
            ally.State.Energy = 700;
            ally.State.Fuel = 100;
            int wall = Defense.Rating(ally.State, ally.Config);
            Assert.True(ally.Execute(Command.ProposeAlliance(Faction.Vanguard)).Accepted);
            Assert.Equal(RejectReason.AllianceActive, ally.Execute(Command.ProposeAlliance(Faction.Church)).Reason);
            Assert.Equal(wall + ally.Config.Diplomacy.AllianceDefenseByTier[0], Defense.Rating(ally.State, ally.Config));
            ally.Run(ally.State.AllyUpkeepTick - ally.State.Tick - 1);
            ally.State.Energy = 0;
            ally.Run(2);
            Assert.Equal(-1, ally.State.AllyFaction);
            Assert.Contains(ally.Log.Events, e => e.Kind == EventKind.AllianceEnded && e.B == (int)AllianceEnd.Unpaid);
        }

        [Fact]
        public void CriticalCorruption_TriggersACrisis_AndAFlushPullsItBack()
        {
            var sim = new Simulation(41UL);
            sim.Config.Glitch.CrisisPctPerHour = 100;
            sim.State.CorruptionMilli = 98_000;
            sim.Run(2L * SimConfig.TicksPerHour);
            Assert.Single(sim.Log.Events, e => e.Kind == EventKind.CrisisStruck);

            // one crisis per cooldown, and the flush is the way out (SPEC-021)
            int before = sim.State.CorruptionMilli;
            sim.State.Energy = sim.Config.Glitch.FlushEnergy;
            Assert.True(sim.Execute(Command.FlushCore()).Accepted || sim.State.RaidId != 0);
            if (sim.State.FlushUntilTick > sim.State.Tick)
            {
                Assert.True(sim.State.CorruptionMilli < before);
                Assert.False(GlitchSystem.TakenOver(sim.State));
            }
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
