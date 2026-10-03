using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Commands
{
    /// <summary>
    /// Kinds of player input. Values are stored in saves and replays: never renumber or reuse;
    /// argument meaning per kind is documented on each member.
    /// </summary>
    public enum CommandKind
    {
        None = 0,

        /// <summary>A: target <see cref="DelegationLevel"/>.</summary>
        SetDelegation = 1,
    }

    /// <summary>
    /// One player input as plain data (kind + three int arguments) so it serializes compactly and replays exactly.
    /// Build instances with the static factories, which document the arguments.
    /// </summary>
    public readonly struct Command
    {
        public Command(CommandKind kind, int a = 0, int b = 0, int c = 0)
        {
            Kind = kind;
            A = a;
            B = b;
            C = c;
        }

        public CommandKind Kind { get; }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public static Command SetDelegation(DelegationLevel level)
        {
            return new Command(CommandKind.SetDelegation, (int)level);
        }

        public override string ToString()
        {
            return Kind + "(" + A + ", " + B + ", " + C + ")";
        }
    }

    /// <summary>A command as accepted at a tick boundary: it was applied after tick <see cref="Tick"/> completed.</summary>
    public readonly struct RecordedCommand
    {
        public RecordedCommand(long tick, Command command)
        {
            Tick = tick;
            Command = command;
        }

        public long Tick { get; }

        public Command Command { get; }
    }
}
