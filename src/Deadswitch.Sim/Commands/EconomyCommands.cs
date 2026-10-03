using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Sim.Commands
{
    /// <summary>Hub construction and power commands (SPEC-002 rules 4, 5, 8). Validate fully before changing anything.</summary>
    internal static class EconomyCommands
    {
        public static CommandResult Build(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            if (!ValidSlot(s, cmd.A) || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidSlot);
            }

            var kind = (FacilityKind)cmd.B;
            if (c.Facility(kind) == null)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (!s.Slots[cmd.A].IsEmpty)
            {
                return CommandResult.Reject(RejectReason.SlotOccupied);
            }

            CommandResult queue = CheckQueue(s, c, cmd.A);
            if (!queue.Accepted)
            {
                return queue;
            }

            Economy.BuildCost(s, c, kind, out int energy, out int compute);
            CommandResult afford = CheckAfford(s, energy, compute);
            if (!afford.Accepted)
            {
                return afford;
            }

            StartJob(ctx, cmd.A, kind, 1, energy, compute);
            return CommandResult.Ok;
        }

        public static CommandResult Upgrade(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            if (!ValidSlot(s, cmd.A) || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidSlot);
            }

            FacilitySlot slot = s.Slots[cmd.A];
            if (slot.IsEmpty)
            {
                return CommandResult.Reject(RejectReason.SlotEmpty);
            }

            FacilityConfig f = c.Facility(slot.Kind)!;
            if (slot.Level >= f.MaxLevel)
            {
                return CommandResult.Reject(RejectReason.MaxLevel);
            }

            CommandResult queue = CheckQueue(s, c, cmd.A);
            if (!queue.Accepted)
            {
                return queue;
            }

            Economy.UpgradeCost(c, slot.Kind, slot.Level, out int energy, out int compute);
            CommandResult afford = CheckAfford(s, energy, compute);
            if (!afford.Accepted)
            {
                return afford;
            }

            StartJob(ctx, cmd.A, slot.Kind, slot.Level + 1, energy, compute);
            return CommandResult.Ok;
        }

        public static CommandResult CancelJob(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (!ValidSlot(s, cmd.A) || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidSlot);
            }

            BuildJob? job = s.JobForSlot(cmd.A);
            if (job == null)
            {
                return CommandResult.Reject(RejectReason.NoJob);
            }

            int energy = SimMath.PctFloor(job.PaidEnergy, ctx.Config.Build.CancelRefundPct);
            int compute = SimMath.PctFloor(job.PaidCompute, ctx.Config.Build.CancelRefundPct);
            Refund(ctx, energy, compute);
            s.Jobs.Remove(job);
            ctx.Emit(EventKind.BuildCancelled, cmd.A, energy, compute);
            return CommandResult.Ok;
        }

        public static CommandResult Demolish(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            if (!ValidSlot(s, cmd.A) || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidSlot);
            }

            FacilitySlot slot = s.Slots[cmd.A];
            if (slot.IsEmpty)
            {
                return CommandResult.Reject(RejectReason.SlotEmpty);
            }

            if (s.JobForSlot(cmd.A) != null)
            {
                return CommandResult.Reject(RejectReason.JobInProgress);
            }

            FacilityConfig f = c.Facility(slot.Kind)!;
            int energy = SimMath.PctFloor(f.CostEnergy[slot.Level - 1], c.Build.DemolishRefundPct);
            int compute = SimMath.PctFloor(f.CostCompute[slot.Level - 1], c.Build.DemolishRefundPct);
            FacilityKind kind = slot.Kind;
            slot.Clear();
            Refund(ctx, energy, compute);
            ctx.Emit(EventKind.FacilityDemolished, cmd.A, (int)kind, energy, compute);
            return CommandResult.Ok;
        }

        public static CommandResult SetFacilityPower(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (!ValidSlot(s, cmd.A) || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidSlot);
            }

            if (cmd.B != 0 && cmd.B != 1)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            FacilitySlot slot = s.Slots[cmd.A];
            if (slot.IsEmpty)
            {
                return CommandResult.Reject(RejectReason.SlotEmpty);
            }

            bool on = cmd.B == 1;
            if (slot.Enabled == on)
            {
                return CommandResult.Reject(RejectReason.NoChange);
            }

            slot.Enabled = on;
            if (!on)
            {
                slot.Powered = false;
                slot.Staffed = false;
            }

            ctx.Emit(EventKind.FacilityPowerSet, cmd.A, cmd.B);
            return CommandResult.Ok;
        }

        public static CommandResult SetPriority(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (!ValidSlot(s, cmd.A) || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidSlot);
            }

            if (cmd.B < 0 || cmd.B >= s.PowerPriority.Count)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            int current = s.PowerPriority.IndexOf(cmd.A);
            if (current == cmd.B)
            {
                return CommandResult.Reject(RejectReason.NoChange);
            }

            s.PowerPriority.RemoveAt(current);
            s.PowerPriority.Insert(cmd.B, cmd.A);
            ctx.Emit(EventKind.PriorityChanged, cmd.A, cmd.B);
            return CommandResult.Ok;
        }

        private static bool ValidSlot(GameState s, int slot)
        {
            return slot >= 0 && slot < s.Slots.Count;
        }

        private static CommandResult CheckQueue(GameState s, SimConfig c, int slot)
        {
            if (s.JobForSlot(slot) != null)
            {
                return CommandResult.Reject(RejectReason.JobInProgress);
            }

            if (s.Jobs.Count >= c.Build.QueueSlots)
            {
                return CommandResult.Reject(RejectReason.QueueFull);
            }

            return CommandResult.Ok;
        }

        private static CommandResult CheckAfford(GameState s, int energy, int compute)
        {
            if (s.Energy < energy)
            {
                return CommandResult.Reject(RejectReason.NotEnoughEnergy);
            }

            if (s.Compute < compute)
            {
                return CommandResult.Reject(RejectReason.NotEnoughCompute);
            }

            return CommandResult.Ok;
        }

        private static void StartJob(SimContext ctx, int slot, FacilityKind kind, int targetLevel, int energy, int compute)
        {
            GameState s = ctx.State;
            int minutes = ctx.Config.Facility(kind)!.BuildMinutes[targetLevel - 1];
            s.Energy -= energy;
            s.Compute -= compute;
            s.Jobs.Add(new BuildJob
            {
                Slot = slot,
                Kind = kind,
                TargetLevel = targetLevel,
                StartTick = s.Tick,
                CompleteTick = s.Tick + minutes,
                PaidEnergy = energy,
                PaidCompute = compute,
            });
            ctx.Emit(EventKind.BuildStarted, slot, (int)kind, targetLevel, minutes);
        }

        private static void Refund(SimContext ctx, int energy, int compute)
        {
            GameState s = ctx.State;
            s.Energy = SimMath.Clamp(s.Energy + energy, 0, Economy.EnergyCap(s, ctx.Config));
            s.Compute = SimMath.Clamp(s.Compute + compute, 0, ctx.Config.Compute.Cap);
        }
    }
}
