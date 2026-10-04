using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>Loyalty status (doc 10 s1.3). Stored in events: never renumber.</summary>
    public enum LoyaltyStatus
    {
        Steady = 0,
        Strained = 1,
        Mutinous = 2,
    }

    /// <summary>
    /// Ruthless choices and loyalty (SPEC-012): forced labor, neural cleansing and crackdowns trade lives and trust
    /// for power, and are the only things that make the AI colder. Low loyalty cuts output and can turn an
    /// operator rogue.
    /// </summary>
    public static class PeopleChoices
    {
        public static LoyaltyStatus Status(GameState s, SimConfig c)
        {
            int pct = s.LoyaltyMilli / 1000;
            return pct < c.PeopleChoices.MutinousBelow ? LoyaltyStatus.Mutinous : pct < c.PeopleChoices.StrainedBelow ? LoyaltyStatus.Strained : LoyaltyStatus.Steady;
        }

        public static bool Surging(GameState s)
        {
            return s.Tick < s.SurgeUntilTick;
        }

        /// <summary>Output points added to Generators and Server Racks by a surge and taken by low loyalty.</summary>
        public static int OutputPts(GameState s, SimConfig c)
        {
            PeopleChoiceConfig p = c.PeopleChoices;
            int pts = Surging(s) ? p.SurgeOutputPts : 0;
            switch (Status(s, c))
            {
                case LoyaltyStatus.Strained:
                    return pts - p.StrainedOutputPts;
                case LoyaltyStatus.Mutinous:
                    return pts - p.MutinousOutputPts;
                default:
                    return pts;
            }
        }

        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            if (!Surging(s))
            {
                SetLoyalty(ctx, s.LoyaltyMilli + c.PeopleChoices.RecoverPerHour);
            }

            // Rule 5: once a day at noon, a mutinous Hub may lose an operator (hash: no RNG draw).
            if (s.Tick % SimConfig.TicksPerDay == SimConfig.TicksPerDay / 2 && Status(s, c) == LoyaltyStatus.Mutinous
                && s.People > c.PeopleChoices.MinPeople
                && SimMath.Hash((uint)(s.Tick / SimConfig.TicksPerDay), (uint)(s.Rng.State >> 32)) % 100 < (uint)c.PeopleChoices.RogueChancePct)
            {
                int energy = System.Math.Min(s.Energy, c.PeopleChoices.RogueEnergy);
                s.People--;
                s.Garrison = System.Math.Min(s.Garrison, s.People);
                s.Energy -= energy;
                ctx.Emit(EventKind.RogueOperator, energy);
            }
        }

        public static CommandResult ForcedLabor(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            PeopleChoiceConfig p = ctx.Config.PeopleChoices;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.Tick < s.SurgeReadyTick)
            {
                return CommandResult.Reject(RejectReason.OnCooldown);
            }

            if (s.People - p.SurgeDeaths < p.MinPeople)
            {
                return CommandResult.Reject(RejectReason.NotEnoughPeople);
            }

            Lose(s, p.SurgeDeaths);
            s.SurgeUntilTick = s.Tick + ((long)p.SurgeHours * SimConfig.TicksPerHour);
            s.SurgeReadyTick = s.Tick + ((long)p.SurgeCooldownHours * SimConfig.TicksPerHour);
            SetLoyalty(ctx, s.LoyaltyMilli - p.SurgeLoyalty);
            s.ColdnessMilli = SimMath.Clamp(s.ColdnessMilli + p.SurgeColdness, 0, 100_000);
            ctx.Emit(EventKind.ForcedLabor, p.SurgeDeaths, p.SurgeHours);
            return CommandResult.Ok;
        }

        public static CommandResult NeuralCleanse(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            PeopleChoiceConfig p = ctx.Config.PeopleChoices;
            int n = cmd.A;
            if (n < 1 || n > p.CleanseMax || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.CorruptionMilli <= 0)
            {
                return CommandResult.Reject(RejectReason.NoTarget);
            }

            if (s.People - n < p.MinPeople)
            {
                return CommandResult.Reject(RejectReason.NotEnoughPeople);
            }

            int removed = System.Math.Min(s.CorruptionMilli, n * p.CleansePerPerson);
            Lose(s, n);
            CorruptionSystem.Add(ctx, -removed);
            SetLoyalty(ctx, s.LoyaltyMilli - (n * p.CleanseLoyalty));
            s.ColdnessMilli = SimMath.Clamp(s.ColdnessMilli + (n * p.CleanseColdness), 0, 100_000);
            ctx.Emit(EventKind.NeuralCleanse, n, removed);
            return CommandResult.Ok;
        }

        public static CommandResult Crackdown(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            PeopleChoiceConfig p = ctx.Config.PeopleChoices;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (Status(s, ctx.Config) == LoyaltyStatus.Steady)
            {
                return CommandResult.Reject(RejectReason.LoyaltyHolds);
            }

            if (s.People - p.CrackdownPeople < p.MinPeople)
            {
                return CommandResult.Reject(RejectReason.NotEnoughPeople);
            }

            Lose(s, p.CrackdownPeople);
            SetLoyalty(ctx, s.LoyaltyMilli + p.CrackdownLoyalty);
            s.ColdnessMilli = SimMath.Clamp(s.ColdnessMilli + p.CrackdownColdness, 0, 100_000);
            ctx.Emit(EventKind.Crackdown, p.CrackdownPeople, s.LoyaltyMilli);
            return CommandResult.Ok;
        }

        private static void Lose(GameState s, int n)
        {
            s.People -= n;
            s.Garrison = System.Math.Min(s.Garrison, s.People);
        }

        private static void SetLoyalty(SimContext ctx, int milli)
        {
            GameState s = ctx.State;
            LoyaltyStatus before = Status(s, ctx.Config);
            s.LoyaltyMilli = SimMath.Clamp(milli, 0, 100_000);
            LoyaltyStatus after = Status(s, ctx.Config);
            if (after != before)
            {
                ctx.Emit(EventKind.LoyaltyChanged, (int)after, (int)before);
            }
        }
    }
}
