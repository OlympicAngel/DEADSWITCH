using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Sim.Commands
{
    /// <summary>Defense setup, OVERRIDE and presence commands (SPEC-001).</summary>
    internal static class DefenseCommands
    {
        public static CommandResult UseOverride(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            bool silence = cmd.A == (int)OverrideKind.Silence;
            if ((cmd.A != (int)OverrideKind.Lockdown && !silence) || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.OverrideCharges <= 0)
            {
                return CommandResult.Reject(RejectReason.NoCharges);
            }

            if (s.Tick < s.OverrideCooldownUntil)
            {
                return CommandResult.Reject(RejectReason.OnCooldown);
            }

            if (silence ? ClimaxSystem.Silenced(s) : s.RaidId == 0)
            {
                return silence ? CommandResult.Reject(RejectReason.NoChange) : CommandResult.Reject(RejectReason.NoTarget);
            }

            if (s.OverrideCharges >= OverrideSystem.MaxCharges(s, ctx.Config))
            {
                s.OverrideNextChargeTick = s.Tick + ctx.Config.Override.RegenMinutes;
            }

            s.OverrideCharges--;
            s.OverrideCooldownUntil = s.Tick + ctx.Config.Override.CooldownMinutes;
            int corruption = ctx.Config.Override.CorruptionMilliPerUse;
            ctx.Emit(EventKind.OverrideUsed, cmd.A, s.OverrideCharges, corruption);
            CorruptionSystem.Add(ctx, corruption);
            if (silence)
            {
                ClimaxSystem.Silence(ctx);
            }
            else
            {
                RaidSystem.Lockdown(ctx);
            }

            return CommandResult.Ok;
        }

        public static CommandResult SetPosture(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A < (int)Posture.None || cmd.A > (int)Posture.Evacuate || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if ((int)s.Posture == cmd.A)
            {
                return CommandResult.Reject(RejectReason.NoChange);
            }

            Posture previous = s.Posture;
            s.Posture = (Posture)cmd.A;
            ctx.Emit(EventKind.PostureSet, cmd.A, (int)previous);
            return CommandResult.Ok;
        }

        public static CommandResult SetGarrison(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A < 0 || cmd.A > ctx.Config.Defense.GarrisonSlots || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (cmd.A > s.People)
            {
                return CommandResult.Reject(RejectReason.NotEnoughPeople);
            }

            if (cmd.A == s.Garrison)
            {
                return CommandResult.Reject(RejectReason.NoChange);
            }

            int previous = s.Garrison;
            s.Garrison = cmd.A;
            ctx.Emit(EventKind.GarrisonSet, cmd.A, previous);
            return CommandResult.Ok;
        }

        /// <summary>Verify a report against the sensor log (SPEC-006 rule 5): costs compute, once per raid.</summary>
        public static CommandResult VerifyReport(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            RaidRecord? record = s.RaidRecords.Find(r => r.RaidId == cmd.A);
            if (record == null)
            {
                return CommandResult.Reject(RejectReason.NoReport);
            }

            if (record.Verified)
            {
                return CommandResult.Reject(RejectReason.AlreadyVerified);
            }

            int cost = ctx.Config.Report.VerifyComputeCost;
            if (s.Compute < cost)
            {
                return CommandResult.Reject(RejectReason.NotEnoughCompute);
            }

            s.Compute -= cost;
            record.Verified = true;
            ctx.Emit(EventKind.ReportVerified, record.RaidId, record.LieFlags, cost);
            return CommandResult.Ok;
        }

        /// <summary>Audit (SPEC-007 rule 5): the Core Profile and the hidden drains, for compute, with a cooldown.</summary>
        public static CommandResult Audit(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.Tick < s.AuditReadyTick)
            {
                return CommandResult.Reject(RejectReason.OnCooldown);
            }

            if (s.Compute < c.Project.AuditComputeCost)
            {
                return CommandResult.Reject(RejectReason.NotEnoughCompute);
            }

            s.Compute -= c.Project.AuditComputeCost;
            if (s.ClimaxAtTick > 0)
            {
                s.ClimaxAudited = true;
            }

            s.AuditReadyTick = s.Tick + ((long)c.Project.AuditCooldownHours * SimConfig.TicksPerHour);
            int unverified = 0;
            foreach (RaidRecord r in s.RaidRecords)
            {
                if (!r.Verified && r.LieFlags != 0)
                {
                    unverified++;
                }
            }

            ctx.Emit(EventKind.AuditRun, (int)ProjectSystem.Stage(c, s.ProjectMilli), s.ColdnessMilli, s.BoldnessMilli, s.CorruptionMilli);
            ctx.Emit(EventKind.AuditDrain, s.SkimmedSinceAudit, unverified, c.Project.AuditComputeCost);
            s.SkimmedSinceAudit = 0;
            return CommandResult.Ok;
        }

        public static CommandResult SetPresence(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if ((cmd.A != 0 && cmd.A != 1) || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            bool away = cmd.A == 1;
            if (s.Away == away)
            {
                return CommandResult.Reject(RejectReason.NoChange);
            }

            s.Away = away;
            ctx.Emit(EventKind.PresenceSet, cmd.A);
            if (!away)
            {
                ThreatSystem.OnReturn(ctx);
            }

            return CommandResult.Ok;
        }
    }
}
