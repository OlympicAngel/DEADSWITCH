using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Sim.Commands
{
    /// <summary>
    /// Validates and applies commands. Validation reads state only; a rejected command changes nothing
    /// (no state, no RNG draws, no events). Handlers are deterministic so replays reach the same verdict.
    /// </summary>
    public static class CommandProcessor
    {
        public static CommandResult Execute(SimContext ctx, Command command)
        {
            switch (command.Kind)
            {
                case CommandKind.SetDelegation:
                    return SetDelegation(ctx, command);
                case CommandKind.Build:
                    return EconomyCommands.Build(ctx, command);
                case CommandKind.Upgrade:
                    return EconomyCommands.Upgrade(ctx, command);
                case CommandKind.CancelJob:
                    return EconomyCommands.CancelJob(ctx, command);
                case CommandKind.Demolish:
                    return EconomyCommands.Demolish(ctx, command);
                case CommandKind.SetFacilityPower:
                    return EconomyCommands.SetFacilityPower(ctx, command);
                case CommandKind.SetPriority:
                    return EconomyCommands.SetPriority(ctx, command);
                case CommandKind.UseOverride:
                    return DefenseCommands.UseOverride(ctx, command);
                case CommandKind.SetPosture:
                    return DefenseCommands.SetPosture(ctx, command);
                case CommandKind.SetGarrison:
                    return DefenseCommands.SetGarrison(ctx, command);
                case CommandKind.SetPresence:
                    return DefenseCommands.SetPresence(ctx, command);
                case CommandKind.VerifyReport:
                    return DefenseCommands.VerifyReport(ctx, command);
                case CommandKind.Audit:
                    return DefenseCommands.Audit(ctx, command);
                case CommandKind.StartResearch:
                    return Modules.Start(ctx, command);
                case CommandKind.CancelResearch:
                    return Modules.Cancel(ctx, command);
                case CommandKind.TierUp:
                    return Modules.TierUp(ctx, command);
                default:
                    return CommandResult.Reject(RejectReason.UnknownCommand);
            }
        }

        private static CommandResult SetDelegation(SimContext ctx, Command command)
        {
            int level = command.A;
            if (level < (int)DelegationLevel.Manual || level > (int)DelegationLevel.Autopilot || command.B != 0 || command.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            DelegationLevel previous = ctx.State.Delegation;
            if ((int)previous == level)
            {
                return CommandResult.Reject(RejectReason.NoChange);
            }

            ctx.State.Delegation = (DelegationLevel)level;
            ctx.Emit(EventKind.DelegationChanged, level, (int)previous);
            return CommandResult.Ok;
        }
    }
}
