namespace Deadswitch.Sim.Commands
{
    /// <summary>Why a command was refused. The UI turns these into advisor lines; never renumber.</summary>
    public enum RejectReason
    {
        None = 0,

        /// <summary>The command kind is unknown to this build.</summary>
        UnknownCommand = 1,

        /// <summary>An argument is outside its valid set.</summary>
        InvalidArgument = 2,

        /// <summary>The command would change nothing (for example, setting the current level again).</summary>
        NoChange = 3,
    }

    public readonly struct CommandResult
    {
        private CommandResult(RejectReason reason)
        {
            Reason = reason;
        }

        public static CommandResult Ok => new CommandResult(RejectReason.None);

        public bool Accepted => Reason == RejectReason.None;

        public RejectReason Reason { get; }

        public static CommandResult Reject(RejectReason reason)
        {
            return new CommandResult(reason);
        }

        public override string ToString()
        {
            return Accepted ? "accepted" : "rejected: " + Reason;
        }
    }
}
