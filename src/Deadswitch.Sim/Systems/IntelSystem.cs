using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Spies (SPEC-019, doc 10 s5): one per faction camp. Each has a hidden loyalty roll at planting (10% base,
    /// more with that faction's heat and the AI's Coldness). A loyal spy gives early, exact warnings and true site
    /// reports; a double agent feeds false ones until a scout or hack at that faction's sites cross-checks and
    /// exposes it. Hash decisions, no RNG draws.
    /// </summary>
    public static class IntelSystem
    {
        public static bool Loyal(GameState s, Faction f)
        {
            return s.Spies[(int)f] == SpyState.Loyal;
        }

        public static bool Double(GameState s, Faction f)
        {
            return s.Spies[(int)f] == SpyState.Double;
        }

        public static bool Has(GameState s, Faction f)
        {
            return s.Spies[(int)f] != SpyState.None;
        }

        /// <summary>People away in faction camps.</summary>
        public static int Away(GameState s, SimConfig c)
        {
            int n = 0;
            for (int f = 0; f < s.Spies.Length; f++)
            {
                n += s.Spies[f] != SpyState.None ? c.Intel.SpyPeople : 0;
            }

            return n;
        }

        /// <summary>Double-agent chance in percent for a spy planted now.</summary>
        public static int DoubleChancePct(GameState s, SimConfig c, Faction f)
        {
            IntelConfig i = c.Intel;
            long pct = i.DoubleBasePct + ((long)WorldSystem.Percent(s.Heat[(int)f]) * i.DoubleHeatPermille / 1000) + ((long)(s.ColdnessMilli / 1000) * i.DoubleColdPermille / 1000);
            return (int)System.Math.Min(100, pct);
        }

        /// <summary>The spy's read of an attack from its faction (loyal: exact; double: low), or -1 with no spy.</summary>
        public static int AttackEstimate(GameState s, SimConfig c, Faction f, int strength)
        {
            if (Loyal(s, f))
            {
                return strength;
            }

            return Double(s, f) ? System.Math.Max(1, SimMath.PctFloor(strength, c.Intel.DoubleEstimatePct)) : -1;
        }

        public static CommandResult Plant(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            if (cmd.A < 0 || cmd.A >= WorldSystem.FactionCount || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            var f = (Faction)cmd.A;
            if (Has(s, f))
            {
                return CommandResult.Reject(RejectReason.SpyActive);
            }

            if (s.People - s.Garrison - c.Intel.SpyPeople < c.PeopleChoices.MinPeople)
            {
                return CommandResult.Reject(RejectReason.NotEnoughPeople);
            }

            if (s.Energy < c.Intel.SpyEnergy)
            {
                return CommandResult.Reject(RejectReason.NotEnoughEnergy);
            }

            s.Energy -= c.Intel.SpyEnergy;
            s.People -= c.Intel.SpyPeople;
            uint roll = SimMath.Hash((uint)s.Tick ^ ((uint)cmd.A * 0x51A7u), (uint)(s.Rng.State >> 32)) % 100;
            s.Spies[cmd.A] = roll < (uint)DoubleChancePct(s, c, f) ? SpyState.Double : SpyState.Loyal;
            ctx.Emit(EventKind.SpyPlanted, cmd.A, c.Intel.SpyEnergy);
            return CommandResult.Ok;
        }

        public static CommandResult Recall(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A < 0 || cmd.A >= WorldSystem.FactionCount || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (!Has(s, (Faction)cmd.A))
            {
                return CommandResult.Reject(RejectReason.NoSpy);
            }

            s.Spies[cmd.A] = SpyState.None;
            s.People += ctx.Config.Intel.SpyPeople;
            ctx.Emit(EventKind.SpyRecalled, cmd.A);
            return CommandResult.Ok;
        }

        /// <summary>Plant false intel (doc 05 s3): heat moves off the spy's faction onto a rival. A double agent turns it on us.</summary>
        public static CommandResult Frame(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            IntelConfig i = ctx.Config.Intel;
            if (cmd.A < 0 || cmd.A >= WorldSystem.FactionCount || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            var f = (Faction)cmd.A;
            if (!Has(s, f))
            {
                return CommandResult.Reject(RejectReason.NoSpy);
            }

            uint h = SimMath.Hash((uint)s.Tick ^ 0xF4A3u ^ ((uint)cmd.A << 8), (uint)(s.Rng.State >> 32));
            bool backfired = Double(s, f);
            if (backfired)
            {
                WorldSystem.AddHeat(ctx, f, i.FrameHeat);
            }
            else
            {
                var rival = (Faction)((cmd.A + 1 + (int)((h >> 8) % (WorldSystem.FactionCount - 1))) % WorldSystem.FactionCount);
                int moved = System.Math.Min(i.FrameHeat, s.Heat[cmd.A]);
                WorldSystem.AddHeat(ctx, f, -moved);
                WorldSystem.AddHeat(ctx, rival, moved / 2);
            }

            ctx.Emit(EventKind.SpyFramed, cmd.A, backfired ? 1 : 0);
            if (backfired || h % 100 < (uint)i.FrameCatchPct)
            {
                Lose(ctx, f, backfired ? SpyLoss.Exposed : SpyLoss.Caught);
            }

            return CommandResult.Ok;
        }

        /// <summary>Hunting factions sweep their camps: a spy may be caught.</summary>
        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            for (int f = 0; f < s.Spies.Length; f++)
            {
                if (s.Spies[f] == SpyState.None || WorldSystem.Level(s.Heat[f]) < HeatLevel.Hunted)
                {
                    continue;
                }

                uint h = SimMath.Hash((uint)(s.Tick / SimConfig.TicksPerHour) ^ ((uint)f * 0x9E37u) ^ 0xC47Cu, (uint)(s.Rng.State >> 32));
                if (h % 100 < (uint)ctx.Config.Intel.CaughtPctPerHourHunted)
                {
                    Lose(ctx, (Faction)f, SpyLoss.Caught);
                }
            }
        }

        /// <summary>A scout or hack that comes back from a faction's site cross-checks the spy there (doc 10 s5).</summary>
        public static void CrossCheck(SimContext ctx, Faction f)
        {
            if (Double(ctx.State, f))
            {
                Lose(ctx, f, SpyLoss.Exposed);
            }
        }

        private static void Lose(SimContext ctx, Faction f, SpyLoss why)
        {
            GameState s = ctx.State;
            s.Spies[(int)f] = SpyState.None;
            if (why == SpyLoss.Caught)
            {
                WorldSystem.AddHeat(ctx, f, ctx.Config.Intel.CaughtHeat);
            }

            ctx.Emit(EventKind.SpyLost, (int)f, (int)why);
        }
    }
}
