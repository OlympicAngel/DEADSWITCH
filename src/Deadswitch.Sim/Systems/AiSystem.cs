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
                || s.Tick < s.PlanHoldUntilTick || s.Blackout)
            {
                return;
            }

            // scars first (SPEC-018): repairs and clearing the yard are not build jobs
            if (TryRepair(ctx) || TryClearYard(ctx) || s.Jobs.Count >= c.Build.QueueSlots)
            {
                return;
            }

            Plan(ctx);
        }

        /// <summary>Offline autopilot (rule 4): defends from the AI's own estimate, which carries corruption error.</summary>
        public static void OnRaidWarning(SimContext ctx)
        {
            GameState s = ctx.State;
            if (s.Delegation != DelegationLevel.Autopilot || !s.Away || s.Posture != Posture.None)
            {
                return;
            }

            Recommend(s, ctx.Config, out Posture posture, out int garrison);
            if (posture == Posture.None)
            {
                return;
            }

            DefenseCommands.SetGarrison(ctx, Command.SetGarrison(garrison));
            DefenseCommands.SetPosture(ctx, Command.SetPosture(posture));
            ctx.Emit(EventKind.AiActed, (int)AiActionKind.Defend, (int)s.Posture, s.Garrison, s.RaidEstimate);
        }

        /// <summary>
        /// The AI's defense advice for the incoming raid (autopilot and Set &amp; Go, SPEC-005 rule 5), judged by its
        /// estimate: enough defense -> keep things as they are (None); outmatched -> Turtle with the full garrison;
        /// hopeless -> Evacuate. Without a raid: None.
        /// </summary>
        public static void Recommend(GameState s, SimConfig c, out Posture posture, out int garrison)
        {
            if (s.RaidId == 0)
            {
                posture = Posture.None;
                garrison = 0;
                return;
            }

            long estimate = (long)s.RaidEstimate * 100;
            int full = SimMath.Clamp(s.People, 0, c.Defense.GarrisonSlots);
            int turtleDefense = Defense.Rating(s, c, Posture.Turtle, full);
            if (estimate <= (long)Defense.Rating(s, c) * c.Ai.AutopilotTurtlePct)
            {
                posture = Posture.None;
                garrison = s.Posture == Posture.None ? s.Garrison : 0;
            }
            else if (estimate <= (long)turtleDefense * c.Ai.AutopilotEvacuatePct)
            {
                posture = Posture.Turtle;
                garrison = full;
            }
            else
            {
                posture = Posture.Evacuate;
                garrison = 0;
            }
        }

        /// <summary>
        /// The AI's Confidence in a defense against its own estimate (SPEC-005 rule 4), 0..100 in steps of 5:
        /// (DEF / estimate - 40%) mapped over 120 points. Wrong whenever the estimate is.
        /// </summary>
        public static int ConfidencePct(int defense, int estimate)
        {
            if (estimate <= 0)
            {
                return 100;
            }

            long ratio = (long)defense * 100 / estimate;
            int pct = (int)SimMath.Clamp((int)System.Math.Min(ratio, 1000) - 40, 0, 120) * 100 / 120;
            return (pct + 2) / 5 * 5;
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

        /// <summary>Repairs the most damaged producer when half the stock covers it.</summary>
        private static bool TryRepair(SimContext ctx)
        {
            GameState s = ctx.State;
            int worst = -1;
            for (int i = 0; i < s.Slots.Count; i++)
            {
                FacilitySlot f = s.Slots[i];
                if (ScarSystem.Affects(f.Kind) && f.Damage > 0 && !ScarSystem.Repairing(s, f) && (worst < 0 || f.Damage > s.Slots[worst].Damage))
                {
                    worst = i;
                }
            }

            if (worst < 0 || ScarSystem.RepairCost(ctx.Config, s.Slots[worst]) * 2 > s.Energy || !ScarSystem.Repair(ctx, Command.Repair(worst)).Accepted)
            {
                return false;
            }

            ctx.Emit(EventKind.AiActed, (int)AiActionKind.Repair, worst, (int)s.Slots[worst].Kind, s.Slots[worst].Damage);
            return true;
        }

        /// <summary>Clears the yard's wrecks when half the stock covers it (no punitive absence for delegated play).</summary>
        private static bool TryClearYard(SimContext ctx)
        {
            GameState s = ctx.State;
            if (s.Wreckage == 0 || ctx.Config.Scars.ClearEnergyPerWreck * s.Wreckage * 2 > s.Energy)
            {
                return false;
            }

            int wrecks = s.Wreckage;
            if (!ScarSystem.ClearWreckage(ctx, Command.ClearWreckage()).Accepted)
            {
                return false;
            }

            ctx.Emit(EventKind.AiActed, (int)AiActionKind.ClearYard, wrecks);
            return true;
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
