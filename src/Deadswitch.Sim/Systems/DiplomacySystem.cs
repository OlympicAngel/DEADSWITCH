using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Temporary pacts (SPEC-023, doc 05 s3): one paid ceasefire at a time. While it holds that faction sends no attacks
    /// and trades cheaper; striking its sites breaks it with a heat spike; it then ends and a cooldown follows.
    /// A Marked faction will not talk. No RNG draws.
    /// </summary>
    public static class DiplomacySystem
    {
        public static bool Ceasefire(GameState s, Faction f)
        {
            return s.CeasefireFaction == (int)f && s.Tick < s.CeasefireUntilTick;
        }

        public static bool Allied(GameState s, Faction f)
        {
            return s.AllyFaction == (int)f;
        }

        /// <summary>A faction's rival: the Rustborn and Vanguard hate each other, as do the Church and Halcyon.</summary>
        public static Faction Rival(Faction f)
        {
            return (Faction)((int)f ^ 1);
        }

        /// <summary>Defense the ally's fighters add (SPEC-025): none without an ally or when the ally itself attacks.</summary>
        public static int AllyDefense(GameState s, SimConfig c)
        {
            if (s.AllyFaction < 0 || (s.RaidId != 0 && (int)s.RaidFaction == s.AllyFaction))
            {
                return 0;
            }

            int[] byTier = c.Diplomacy.AllianceDefenseByTier;
            return byTier[SimMath.Clamp(s.Tier, 1, byTier.Length) - 1];
        }

        /// <summary>Alliance (SPEC-025): with a Cold faction, one at a time; paid up front and then daily.</summary>
        public static CommandResult Ally(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            DiplomacyConfig d = ctx.Config.Diplomacy;
            if (cmd.A < 0 || cmd.A >= WorldSystem.FactionCount || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            var f = (Faction)cmd.A;
            if (s.AllyFaction >= 0)
            {
                return CommandResult.Reject(RejectReason.AllianceActive);
            }

            if (WorldSystem.Level(s.Heat[cmd.A]) != HeatLevel.Cold)
            {
                return CommandResult.Reject(RejectReason.NotTrusted);
            }

            if (s.RaidId != 0 && s.RaidFaction == f)
            {
                return CommandResult.Reject(RejectReason.ThreatActive);
            }

            if (s.Energy < d.AllianceEnergy)
            {
                return CommandResult.Reject(RejectReason.NotEnoughEnergy);
            }

            if (s.Fuel < d.AllianceFuel)
            {
                return CommandResult.Reject(RejectReason.NotEnoughFuel);
            }

            s.Energy -= d.AllianceEnergy;
            s.Fuel -= d.AllianceFuel;
            s.AllyFaction = cmd.A;
            s.AllyUpkeepTick = s.Tick + SimConfig.TicksPerDay;
            ctx.Emit(EventKind.AllianceFormed, cmd.A, d.AllianceEnergy, d.AllianceFuel);
            return CommandResult.Ok;
        }

        public static CommandResult EndAlliance(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.AllyFaction < 0)
            {
                return CommandResult.Reject(RejectReason.AllianceActive);
            }

            Dissolve(ctx, AllianceEnd.Dissolved, ctx.Config.Diplomacy.AllianceEndHeat);
            return CommandResult.Ok;
        }

        /// <summary>Ceasefire price (energy, fuel) with a faction, or false when it will not talk.</summary>
        public static bool Price(GameState s, SimConfig c, Faction f, out int energy, out int fuel)
        {
            DiplomacyConfig d = c.Diplomacy;
            HeatLevel level = WorldSystem.Level(s.Heat[(int)f]);
            energy = 0;
            fuel = 0;
            if (level == HeatLevel.Marked)
            {
                return false;
            }

            energy = SimMath.PctFloor(d.CeasefireEnergy, d.PricePctByLevel[(int)level]);
            fuel = SimMath.PctFloor(d.CeasefireFuel, d.PricePctByLevel[(int)level]);
            return true;
        }

        public static CommandResult Propose(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            if (cmd.A < 0 || cmd.A >= WorldSystem.FactionCount || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            var f = (Faction)cmd.A;
            if (!Price(s, c, f, out int energy, out int fuel))
            {
                return CommandResult.Reject(RejectReason.FactionHostile);
            }

            if (s.Tick < s.CeasefireUntilTick || s.Tick < s.CeasefireReadyTick)
            {
                return CommandResult.Reject(RejectReason.PactActive);
            }

            if (s.RaidId != 0 && s.RaidFaction == f)
            {
                return CommandResult.Reject(RejectReason.ThreatActive);
            }

            if (s.Energy < energy)
            {
                return CommandResult.Reject(RejectReason.NotEnoughEnergy);
            }

            if (s.Fuel < fuel)
            {
                return CommandResult.Reject(RejectReason.NotEnoughFuel);
            }

            s.Energy -= energy;
            s.Fuel -= fuel;
            s.CeasefireFaction = cmd.A;
            s.CeasefireUntilTick = s.Tick + ((long)c.Diplomacy.CeasefireDays * SimConfig.TicksPerDay);
            ctx.Emit(EventKind.CeasefireStarted, cmd.A, c.Diplomacy.CeasefireDays, energy, fuel);
            return CommandResult.Ok;
        }

        /// <summary>Hourly: an expired ceasefire ends and the cooldown starts.</summary>
        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            if (s.CeasefireFaction >= 0 && s.Tick >= s.CeasefireUntilTick)
            {
                End(ctx, false);
            }

            AllianceHourly(ctx);
        }

        /// <summary>The ally's daily share, its rival's growing grudge, and the chance it walks out (no RNG draws).</summary>
        private static void AllianceHourly(SimContext ctx)
        {
            GameState s = ctx.State;
            DiplomacyConfig d = ctx.Config.Diplomacy;
            if (s.AllyFaction < 0)
            {
                return;
            }

            if (WorldSystem.Level(s.Heat[s.AllyFaction]) >= HeatLevel.Watched)
            {
                Dissolve(ctx, AllianceEnd.Distrust, 0);
                return;
            }

            if (s.Tick < s.AllyUpkeepTick)
            {
                return;
            }

            if (s.Energy < d.AllianceUpkeepEnergy)
            {
                Dissolve(ctx, AllianceEnd.Unpaid, d.AllianceEndHeat);
                return;
            }

            s.Energy -= d.AllianceUpkeepEnergy;
            s.AllyUpkeepTick += SimConfig.TicksPerDay;
            ctx.Emit(EventKind.AllianceUpkeep, s.AllyFaction, d.AllianceUpkeepEnergy);
            WorldSystem.AddHeat(ctx, Rival((Faction)s.AllyFaction), d.AllianceRivalHeat);
            uint h = SimMath.Hash((uint)s.Tick ^ 0xA11Eu, (uint)(s.Rng.State >> 32));
            if (h % 100 < (uint)d.AllianceWalkoutPct)
            {
                Dissolve(ctx, AllianceEnd.Walkout, 0);
            }
        }

        private static void Dissolve(SimContext ctx, AllianceEnd reason, int heat)
        {
            GameState s = ctx.State;
            int f = s.AllyFaction;
            s.AllyFaction = -1;
            s.AllyUpkeepTick = 0;
            if (heat > 0)
            {
                WorldSystem.AddHeat(ctx, (Faction)f, heat);
            }

            ctx.Emit(EventKind.AllianceEnded, f, (int)reason, heat);
        }

        /// <summary>The Hub struck a faction it has a ceasefire with: the pact breaks and its heat spikes.</summary>
        public static void Struck(SimContext ctx, Faction f)
        {
            GameState s = ctx.State;
            if (Allied(s, f))
            {
                Dissolve(ctx, AllianceEnd.Betrayed, ctx.Config.Diplomacy.AllianceBetrayHeat);
            }

            if (!Ceasefire(s, f))
            {
                return;
            }

            WorldSystem.AddHeat(ctx, f, ctx.Config.Diplomacy.BreakHeat);
            End(ctx, true);
        }

        private static void End(SimContext ctx, bool broken)
        {
            GameState s = ctx.State;
            int f = s.CeasefireFaction;
            s.CeasefireFaction = -1;
            s.CeasefireUntilTick = 0;
            s.CeasefireReadyTick = s.Tick + ((long)ctx.Config.Diplomacy.CooldownDays * SimConfig.TicksPerDay);
            ctx.Emit(EventKind.CeasefireEnded, f, broken ? 1 : 0);
        }
    }
}
