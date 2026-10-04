using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// The AI acting without orders (SPEC-030, doc 03 s5): once bold enough under delegation, it sometimes sends a
    /// raid of its own at the softest target its estimate likes (its estimate can be wrong). Every such op is
    /// announced, and the handler can recall it before it lands: survivors walk home, the fuel is spent, no heat.
    /// No RNG draws.
    /// </summary>
    public static class InitiativeSystem
    {
        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            AiConfig a = c.Ai;
            // never during mercy or the opening protection: launching offense would end the handler's safety window
            bool sheltered = s.Tick < s.MercyUntilTick || s.Tick - s.CycleStartTick < (long)c.Opening.ProtectionHours * SimConfig.TicksPerHour;
            // only while the handler is around to see it and recall it: never on autopilot or while away
            if (s.Delegation != DelegationLevel.Delegated || s.Away || s.BoldnessMilli < a.InitiativeBoldness || s.RaidId != 0 || s.Blackout || sheltered)
            {
                return;
            }

            foreach (Operation op in s.Ops)
            {
                if (op.ByAi)
                {
                    return;
                }
            }

            uint h = SimMath.Hash((uint)(s.Tick / SimConfig.TicksPerHour) ^ 0xA1A1u, (uint)(s.Rng.State >> 32));
            if (h % 100 >= (uint)a.InitiativePctPerHour)
            {
                return;
            }

            int best = -1;
            int bestDefense = int.MaxValue;
            for (int i = 0; i < WorldSystem.Sites.Count; i++)
            {
                SiteDef d = WorldSystem.Sites[i];
                if (HazardSystem.Wild(d.Kind) || s.Sites[i].Outpost || s.Tick < s.Sites[i].CooldownUntilTick || DiplomacySystem.Ceasefire(s, d.Owner) || DiplomacySystem.Allied(s, d.Owner))
                {
                    continue;
                }

                int estimate = WorldSystem.EstimatedDefense(s, c, i);
                if (WorldSystem.Odds(s, c, d, OpKind.Raid, a.InitiativeSquad, 0, estimate) >= a.InitiativeMinOdds && estimate < bestDefense)
                {
                    best = i;
                    bestDefense = estimate;
                }
            }

            // it never spends the fuel the reactor needs for the next half day
            bool keepsReactorFed = best < 0 || s.Fuel - WorldSystem.FuelCost(s, c, best, OpKind.Raid) >= ReactorSystem.FuelPerHour(s, c) * 12;
            if (best < 0 || !keepsReactorFed || !WorldSystem.Launch(ctx, Command.LaunchOp(best, OpKind.Raid, a.InitiativeSquad)).Accepted)
            {
                return;
            }

            Operation launched = s.Ops[s.Ops.Count - 1];
            launched.ByAi = true;
            ctx.Emit(EventKind.AiActed, (int)AiActionKind.LaunchOp, launched.Id, best, a.InitiativeSquad);
        }

        /// <summary>The handler calls an AI-launched op back: survivors return now, no heat, the fuel is gone.</summary>
        public static CommandResult Recall(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            for (int i = 0; i < s.Ops.Count; i++)
            {
                Operation op = s.Ops[i];
                if (op.Id == cmd.A && op.ByAi)
                {
                    s.Ops.RemoveAt(i);
                    s.People += op.Squad;
                    // the target saw them coming: it stays off the AI's list for a while
                    s.Sites[op.Site].CooldownUntilTick = System.Math.Max(s.Sites[op.Site].CooldownUntilTick, s.Tick + ((long)ctx.Config.World.RaidCooldownHours * SimConfig.TicksPerHour));
                    ctx.Emit(EventKind.OpRecalled, op.Id, op.Site, op.Squad);
                    return CommandResult.Ok;
                }
            }

            return CommandResult.Reject(RejectReason.NoSuchOp);
        }
    }
}
