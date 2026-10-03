using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// The AI acting on its own (SPEC-004): hidden dials, delegated routines (build queue) and offline autopilot
    /// (defense while the handler is away). Its actions go through the same validated command handlers as the
    /// handler's, are derived inside the tick (never recorded as commands) and are announced with AiActed.
    /// </summary>
    public static class AiSystem
    {
        /// <summary>Boldness follows reliance: up while the AI runs things, slowly down while the handler does.</summary>
        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            AiConfig c = ctx.Config.Ai;
            int delta = s.Delegation switch
            {
                DelegationLevel.Delegated => c.BoldnessPerHourDelegated,
                DelegationLevel.Autopilot => c.BoldnessPerHourAutopilot,
                _ => -c.BoldnessDecayPerHourManual,
            };
            s.BoldnessMilli = SimMath.Clamp(s.BoldnessMilli + delta, 0, 100_000);
        }

        /// <summary>Delegated routines: one build decision every plan interval while the queue has room (rule 3).</summary>
        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            if (s.Delegation == DelegationLevel.Manual || s.Tick % c.Ai.PlanEveryMinutes != 0 || s.RaidId != 0
                || s.Tick < s.PlanHoldUntilTick || s.Jobs.Count >= c.Build.QueueSlots || s.Blackout)
            {
                return;
            }

            Plan(ctx);
        }

        /// <summary>Offline autopilot (rule 4): defends from the AI's own estimate, which carries corruption error.</summary>
        public static void OnRaidWarning(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            if (s.Delegation != DelegationLevel.Autopilot || !s.Away || s.Posture != Posture.None)
            {
                return;
            }

            long estimate = (long)s.RaidEstimate * 100;
            int defense = Defense.Rating(s, c);
            if (estimate > (long)defense * c.Ai.AutopilotEvacuatePct)
            {
                DefenseCommands.SetGarrison(ctx, Command.SetGarrison(0));
                DefenseCommands.SetPosture(ctx, Command.SetPosture(Posture.Evacuate));
            }
            else if (estimate > (long)defense * c.Ai.AutopilotTurtlePct)
            {
                int garrison = System.Math.Min(c.Defense.GarrisonSlots, s.People);
                DefenseCommands.SetGarrison(ctx, Command.SetGarrison(garrison));
                DefenseCommands.SetPosture(ctx, Command.SetPosture(Posture.Turtle));
            }
            else
            {
                return;
            }

            ctx.Emit(EventKind.AiActed, (int)AiActionKind.Defend, (int)s.Posture, s.Garrison, s.RaidEstimate);
        }

        /// <summary>Power first, then the missing basics, then the lowest-level facility the energy budget can carry.</summary>
        private static void Plan(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            int margin = c.Ai.PlanEnergyMargin;
            int net = Economy.Flows(s, c).NetEnergyPerHour;

            if (net < margin)
            {
                if (!TryUpgradeLowest(ctx, FacilityKind.Generator))
                {
                    TryBuild(ctx, FacilityKind.Generator);
                }

                return;
            }

            FacilityKind[] basics = { FacilityKind.BatteryBank, FacilityKind.Turret, FacilityKind.LifeSupport };
            foreach (FacilityKind kind in basics)
            {
                if (Economy.CountOfKind(s, kind) == 0 && net - c.Facility(kind)!.UpkeepPerHour[0] >= margin && TryBuild(ctx, kind))
                {
                    return;
                }
            }

            int best = -1;
            for (int i = 0; i < s.PowerPriority.Count; i++)
            {
                int slot = s.PowerPriority[i];
                FacilitySlot f = s.Slots[slot];
                FacilityConfig? table = c.Facility(f.Kind);
                if (table == null || f.Level >= table.MaxLevel || s.JobForSlot(slot) != null)
                {
                    continue;
                }

                int extraUpkeep = table.UpkeepPerHour[f.Level] - table.UpkeepPerHour[f.Level - 1];
                if (net - extraUpkeep < margin)
                {
                    continue;
                }

                if (best < 0 || f.Level < s.Slots[best].Level)
                {
                    best = slot;
                }
            }

            if (best >= 0)
            {
                Act(ctx, best, s.Slots[best].Kind, s.Slots[best].Level + 1, EconomyCommands.Upgrade(ctx, Command.Upgrade(best)));
            }
        }

        private static bool TryUpgradeLowest(SimContext ctx, FacilityKind kind)
        {
            GameState s = ctx.State;
            FacilityConfig table = ctx.Config.Facility(kind)!;
            int best = -1;
            for (int i = 0; i < s.Slots.Count; i++)
            {
                FacilitySlot f = s.Slots[i];
                if (f.Kind == kind && f.Level < table.MaxLevel && s.JobForSlot(i) == null && (best < 0 || f.Level < s.Slots[best].Level))
                {
                    best = i;
                }
            }

            return best >= 0 && Act(ctx, best, kind, s.Slots[best].Level + 1, EconomyCommands.Upgrade(ctx, Command.Upgrade(best)));
        }

        private static bool TryBuild(SimContext ctx, FacilityKind kind)
        {
            GameState s = ctx.State;
            for (int i = 0; i < s.Slots.Count; i++)
            {
                if (s.Slots[i].IsEmpty && s.JobForSlot(i) == null)
                {
                    return Act(ctx, i, kind, 1, EconomyCommands.Build(ctx, Command.Build(i, kind)));
                }
            }

            return false;
        }

        private static bool Act(SimContext ctx, int slot, FacilityKind kind, int level, CommandResult result)
        {
            if (result.Accepted)
            {
                ctx.Emit(EventKind.AiActed, (int)AiActionKind.Build, slot, (int)kind, level);
            }

            return result.Accepted;
        }
    }
}
