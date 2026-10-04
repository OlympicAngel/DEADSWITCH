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
        }

        /// <summary>The Hub struck a faction it has a ceasefire with: the pact breaks and its heat spikes.</summary>
        public static void Struck(SimContext ctx, Faction f)
        {
            GameState s = ctx.State;
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
