using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

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
