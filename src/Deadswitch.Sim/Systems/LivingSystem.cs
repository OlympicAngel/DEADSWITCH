using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// A world that moves on its own (F-034, doc 05 s3-4+s7, doc 10 s4-5): the Warlord Ultimatum for a Hub that
    /// lingers in Tier 1, dilemma events, trade with factions, rotating world events and factions fighting each
    /// other. Runs hourly. Decisions use hashes, never RNG draws.
    /// </summary>
    public static class LivingSystem
    {
        private const int DilemmaKinds = 5;
        private const int WorldEventKinds = 3;

        /// <summary>True while the given world event runs.</summary>
        public static bool Active(GameState s, WorldEventKind kind)
        {
            return s.WorldEvent == kind && s.Tick < s.WorldEventUntilTick;
        }

        /// <summary>Price of one lot from a faction, or -1 when it will not trade (Marked).</summary>
        public static int Price(GameState s, SimConfig c, Faction f, TradeGood good)
        {
            LivingConfig l = c.Living;
            HeatLevel level = WorldSystem.Level(s.Heat[(int)f]);
            if (level == HeatLevel.Marked)
            {
                return -1;
            }

            int basePrice = good == TradeGood.Fuel ? l.TradeFuelPrice : good == TradeGood.EnergyCells ? l.TradeEnergyPrice : l.TradeComputePrice;
            int price = SimMath.PctFloor(basePrice, l.TradePricePctByLevel[(int)level]);
            return Active(s, WorldEventKind.SupplyWindow) ? SimMath.PctFloor(price, 100 - l.SupplyWindowPricePct) : price;
        }

        public static int Lot(SimConfig c, TradeGood good)
        {
            LivingConfig l = c.Living;
            return good == TradeGood.Fuel ? l.TradeFuelLot : good == TradeGood.EnergyCells ? l.TradeEnergyLot : l.TradeComputeLot;
        }

        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            if (s.Tick % SimConfig.TicksPerDay == 0)
            {
                for (int f = 0; f < s.TradesToday.Length; f++)
                {
                    s.TradesToday[f] = 0;
                }
            }

            UltimatumHourly(ctx);
            DilemmaHourly(ctx);
            WorldEventHourly(ctx);
        }

        public static CommandResult PayUltimatum(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            LivingConfig l = ctx.Config.Living;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.Ultimatum != UltimatumStage.Issued)
            {
                return CommandResult.Reject(RejectReason.NothingPending);
            }

            if (s.Energy < l.UltimatumEnergy)
            {
                return CommandResult.Reject(RejectReason.NotEnoughEnergy);
            }

            if (s.Fuel < l.UltimatumFuel)
            {
                return CommandResult.Reject(RejectReason.NotEnoughFuel);
            }

            Pay(ctx);
            return CommandResult.Ok;
        }

        public static CommandResult ResolveDilemma(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A < 0 || cmd.A > 1 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.Dilemma == DilemmaKind.None)
            {
                return CommandResult.Reject(RejectReason.NothingPending);
            }

            if (cmd.A == 0)
            {
                RejectReason cost = CanTake(s, ctx.Config, s.Dilemma);
                if (cost != RejectReason.None)
                {
                    return CommandResult.Reject(cost);
                }
            }

            Answer(ctx, cmd.A, false);
            return CommandResult.Ok;
        }

        public static CommandResult Trade(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            if (cmd.A < 0 || cmd.A >= WorldSystem.FactionCount || cmd.B < 0 || cmd.B > (int)TradeGood.Compute || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            var f = (Faction)cmd.A;
            var good = (TradeGood)cmd.B;
            int price = Price(s, c, f, good);
            if (price < 0)
            {
                return CommandResult.Reject(RejectReason.FactionHostile);
            }

            if (s.TradesToday[cmd.A] >= c.Living.TradesPerDay)
            {
                return CommandResult.Reject(RejectReason.TradeCap);
            }

            int lot = Lot(c, good);
            if (Room(s, c, good) <= 0)
            {
                return CommandResult.Reject(RejectReason.StorageFull);
            }

            int got;
            if (good == TradeGood.EnergyCells)
            {
                if (s.Fuel < price)
                {
                    return CommandResult.Reject(RejectReason.NotEnoughFuel);
                }

                s.Fuel -= price;
                got = AddEnergy(s, c, lot);
            }
            else
            {
                if (s.Energy < price)
                {
                    return CommandResult.Reject(RejectReason.NotEnoughEnergy);
                }

                s.Energy -= price;
                got = good == TradeGood.Fuel ? AddFuel(s, c, lot) : AddCompute(s, c, lot);
            }

            s.TradesToday[cmd.A]++;
            ctx.Emit(EventKind.Traded, cmd.A, cmd.B, got, price);
            return CommandResult.Ok;
        }

        private static void UltimatumHourly(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            LivingConfig l = c.Living;
            long hour = SimConfig.TicksPerHour;
            if (s.Ultimatum == UltimatumStage.None)
            {
                if (s.Tier == 1 && !s.Away && c.Raid.MaxPerDay > 0 && s.Tick - s.CycleStartTick >= (long)l.UltimatumDay * SimConfig.TicksPerDay)
                {
                    s.Ultimatum = UltimatumStage.Issued;
                    s.UltimatumDeadlineTick = s.Tick + (l.UltimatumHours * hour);
                    ctx.Emit(EventKind.UltimatumIssued, l.UltimatumHours * SimConfig.TicksPerHour, l.UltimatumEnergy, l.UltimatumFuel);
                }

                return;
            }

            if (s.Ultimatum != UltimatumStage.Issued)
            {
                return;
            }

            if (s.Tier > 1)
            {
                // the Hub outgrew the threat
                s.Ultimatum = UltimatumStage.Done;
                ctx.Emit(EventKind.UltimatumResolved, (int)UltimatumOutcome.Outgrown, 0);
                return;
            }

            if (s.Tick < s.UltimatumDeadlineTick)
            {
                return;
            }

            // tribute works offline (doc 10 s1.6): a standing tribute order pays for an absent handler if it can
            if (s.Away && s.TributeOrder && s.Energy >= l.UltimatumEnergy && s.Fuel >= l.UltimatumFuel)
            {
                Pay(ctx);
                return;
            }

            bool protectedNow = c.Opening.Enabled && s.Tick < (long)c.Opening.ProtectionHours * hour;
            if (s.RaidId != 0 || s.Tick < s.MercyUntilTick || protectedNow || ThreatSystem.Shielded(s) || s.RaidsToday >= RaidSystem.MaxPerDay(s, c))
            {
                // fairness first (doc 10 s4): the wave waits for the inbound attack, mercy, the shield and the daily cap
                s.UltimatumDeadlineTick = s.Tick + hour;
                return;
            }

            int id = RaidSystem.SpawnWarlord(ctx);
            s.Ultimatum = UltimatumStage.Done;
            ctx.Emit(EventKind.UltimatumResolved, (int)UltimatumOutcome.Wave, id);
        }

        private static void Pay(SimContext ctx)
        {
            GameState s = ctx.State;
            LivingConfig l = ctx.Config.Living;
            s.Energy -= l.UltimatumEnergy;
            s.Fuel -= l.UltimatumFuel;
            s.Ultimatum = UltimatumStage.Done;
            WorldSystem.AddHeat(ctx, Faction.Rustborn, -l.UltimatumPaidHeatDrop);
            ctx.Emit(EventKind.UltimatumResolved, (int)UltimatumOutcome.Paid, 0);
        }

        private static void DilemmaHourly(SimContext ctx)
        {
            GameState s = ctx.State;
            LivingConfig l = ctx.Config.Living;
            long hour = SimConfig.TicksPerHour;
            if (s.Dilemma != DilemmaKind.None)
            {
                if (s.Tick >= s.DilemmaUntilTick)
                {
                    // unanswered: refused, never punished (doc 10: no punitive absence)
                    Answer(ctx, 1, true);
                }

                return;
            }

            if (s.NextDilemmaTick == 0)
            {
                s.NextDilemmaTick = l.DilemmaFirstHour * hour;
            }

            if (s.Tick < s.NextDilemmaTick)
            {
                return;
            }

            if (s.Away)
            {
                // nobody is here to answer: it waits for the handler's return
                return;
            }

            uint h = SimMath.Hash((uint)(s.Tick / hour) ^ 0xD11Eu, (uint)(s.Rng.State >> 32));
            s.NextDilemmaTick = s.Tick + ((l.DilemmaEveryHours + (int)(h % (uint)(l.DilemmaJitterHours + 1))) * hour);

            var kind = (DilemmaKind)(1 + (int)((h >> 8) % DilemmaKinds));
            s.Dilemma = kind;
            s.DilemmaUntilTick = s.Tick + (l.DilemmaExpireHours * hour);
            ctx.Emit(EventKind.DilemmaOffered, (int)kind, l.DilemmaExpireHours * SimConfig.TicksPerHour);
        }

        private static RejectReason CanTake(GameState s, SimConfig c, DilemmaKind kind)
        {
            switch (kind)
            {
                case DilemmaKind.Trader:
                    return s.Energy < c.Living.TraderEnergy ? RejectReason.NotEnoughEnergy : Room(s, c, TradeGood.Fuel) <= 0 ? RejectReason.StorageFull : RejectReason.None;
                case DilemmaKind.Refugees:
                case DilemmaKind.Deserters:
                    return Economy.PopulationCap(s, c) - s.People - WorldSystem.Away(s) - IntelSystem.Away(s, c) <= 0 ? RejectReason.StorageFull : RejectReason.None;
                default:
                    return RejectReason.None;
            }
        }

        private static void Answer(SimContext ctx, int choice, bool expired)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            LivingConfig l = c.Living;
            DilemmaKind kind = s.Dilemma;
            s.Dilemma = DilemmaKind.None;
            s.DilemmaUntilTick = 0;
            uint roll = SimMath.Hash((uint)s.Tick ^ ((uint)kind * 0x9E37u), (uint)(s.Rng.State >> 32)) % 100;
            bool struck = false;
            if (choice == 0)
            {
                switch (kind)
                {
                    case DilemmaKind.Trader:
                        s.Energy -= l.TraderEnergy;
                        struck = roll < (uint)l.TraderTrapPct;
                        if (!struck)
                        {
                            AddFuel(s, c, l.TraderFuel);
                        }

                        break;
                    case DilemmaKind.Refugees:
                        AddPeople(s, c, l.RefugeePeople);
                        struck = roll < (uint)l.RefugeeSpyPct;
                        if (struck)
                        {
                            WorldSystem.AddHeat(ctx, WorldSystem.Hottest(s), l.RefugeeSpyHeat);
                        }

                        break;
                    case DilemmaKind.Shortcut:
                        AddEnergy(s, c, l.ShortcutEnergy);
                        AddCompute(s, c, l.ShortcutCompute);
                        CorruptionSystem.Add(ctx, l.ShortcutCorruption);
                        break;
                    case DilemmaKind.Deserters:
                        AddPeople(s, c, l.DeserterPeople);
                        WorldSystem.AddHeat(ctx, Faction.Vanguard, l.DeserterHeat);
                        break;
                    default:
                        struck = roll < (uint)l.ChurchTaintPct;
                        CorruptionSystem.Add(ctx, struck ? l.ChurchTaintCorruption : -l.ChurchCleanData);
                        break;
                }
            }
            else if (kind == DilemmaKind.Deserters && !expired)
            {
                WorldSystem.AddHeat(ctx, Faction.Vanguard, -l.DeserterReturnHeatDrop);
            }

            ctx.Emit(EventKind.DilemmaResolved, (int)kind, choice, expired ? 1 : 0, struck ? 1 : 0);
        }

        private static void WorldEventHourly(SimContext ctx)
        {
            GameState s = ctx.State;
            LivingConfig l = ctx.Config.Living;
            long hour = SimConfig.TicksPerHour;
            if (s.WorldEvent != WorldEventKind.None && s.Tick >= s.WorldEventUntilTick)
            {
                ctx.Emit(EventKind.WorldEventEnded, (int)s.WorldEvent);
                s.WorldEvent = WorldEventKind.None;
                s.WorldEventUntilTick = 0;
            }

            if (Active(s, WorldEventKind.SignalStorm))
            {
                CorruptionSystem.Add(ctx, l.SignalStormCorruptionMilli);
            }

            if (s.NextWorldEventTick == 0)
            {
                s.NextWorldEventTick = (long)l.WorldEventFirstDay * SimConfig.TicksPerDay;
            }

            if (s.WorldEvent != WorldEventKind.None || s.Tick < s.NextWorldEventTick)
            {
                return;
            }

            uint h = SimMath.Hash((uint)(s.Tick / hour) ^ 0x3E7Eu, (uint)(s.Rng.State >> 32));
            s.WorldEvent = (WorldEventKind)(1 + (int)(h % WorldEventKinds));
            s.WorldEventUntilTick = s.Tick + (l.WorldEventHours * hour);
            s.NextWorldEventTick = s.Tick + (l.WorldEventEveryHours * hour);

            // factions fight each other (doc 05 s7): the hottest is busy with a rival, which warms up
            Faction busy = WorldSystem.Hottest(s);
            var rival = (Faction)(((int)busy + 1 + (int)((h >> 8) % (WorldSystem.FactionCount - 1))) % WorldSystem.FactionCount);
            int cooled = System.Math.Min(l.FactionWarHeat, s.Heat[(int)busy]);
            WorldSystem.AddHeat(ctx, busy, -cooled);
            WorldSystem.AddHeat(ctx, rival, cooled / 2);
            ctx.Emit(EventKind.WorldEventStarted, (int)s.WorldEvent, l.WorldEventHours, (int)busy);
        }

        /// <summary>How much of a good the Hub can still store.</summary>
        private static int Room(GameState s, SimConfig c, TradeGood good)
        {
            return good == TradeGood.Fuel ? c.Fuel.Cap - s.Fuel : good == TradeGood.EnergyCells ? Economy.EnergyCap(s, c) - s.Energy : c.Compute.Cap - s.Compute;
        }

        private static int AddEnergy(GameState s, SimConfig c, int amount)
        {
            int got = System.Math.Max(0, System.Math.Min(amount, Economy.EnergyCap(s, c) - s.Energy));
            s.Energy += got;
            return got;
        }

        private static int AddFuel(GameState s, SimConfig c, int amount)
        {
            int got = System.Math.Max(0, System.Math.Min(amount, c.Fuel.Cap - s.Fuel));
            s.Fuel += got;
            return got;
        }

        private static int AddCompute(GameState s, SimConfig c, int amount)
        {
            int got = System.Math.Max(0, System.Math.Min(amount, c.Compute.Cap - s.Compute));
            s.Compute += got;
            return got;
        }

        private static void AddPeople(GameState s, SimConfig c, int amount)
        {
            s.People += System.Math.Max(0, System.Math.Min(amount, Economy.PopulationCap(s, c) - s.People - WorldSystem.Away(s) - IntelSystem.Away(s, c)));
        }
    }
}
