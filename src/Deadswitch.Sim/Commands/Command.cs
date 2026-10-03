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

        /// <summary>Build a new facility. A: slot, B: <see cref="FacilityKind"/>.</summary>
        Build = 2,

        /// <summary>Upgrade a facility by one level. A: slot.</summary>
        Upgrade = 3,

        /// <summary>Cancel the construction job on a slot (partial refund). A: slot.</summary>
        CancelJob = 4,

        /// <summary>Demolish a facility (partial refund). A: slot.</summary>
        Demolish = 5,

        /// <summary>Switch a facility on or off. A: slot, B: 1 = on, 0 = off.</summary>
        SetFacilityPower = 6,

        /// <summary>Move a slot in the power/crew priority order. A: slot, B: new rank (0 = highest).</summary>
        SetPriority = 7,

        /// <summary>Use an OVERRIDE charge. A: <see cref="OverrideKind"/>.</summary>
        UseOverride = 8,

        /// <summary>Set the defense posture. A: <see cref="Posture"/>.</summary>
        SetPosture = 9,

        /// <summary>Post defenders. A: number of people (0..garrison slots).</summary>
        SetGarrison = 10,

        /// <summary>Host-reported presence. A: 1 = away (logout), 0 = here.</summary>
        SetPresence = 11,
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

        public static Command Build(int slot, FacilityKind kind)
        {
            return new Command(CommandKind.Build, slot, (int)kind);
        }

        public static Command Upgrade(int slot)
        {
            return new Command(CommandKind.Upgrade, slot);
        }

        public static Command CancelJob(int slot)
        {
            return new Command(CommandKind.CancelJob, slot);
        }

        public static Command Demolish(int slot)
        {
            return new Command(CommandKind.Demolish, slot);
        }

        public static Command SetFacilityPower(int slot, bool on)
        {
            return new Command(CommandKind.SetFacilityPower, slot, on ? 1 : 0);
        }

        public static Command SetPriority(int slot, int rank)
        {
            return new Command(CommandKind.SetPriority, slot, rank);
        }

        public static Command UseOverride(OverrideKind kind)
        {
            return new Command(CommandKind.UseOverride, (int)kind);
        }

        public static Command SetPosture(Posture posture)
        {
            return new Command(CommandKind.SetPosture, (int)posture);
        }

        public static Command SetGarrison(int defenders)
        {
            return new Command(CommandKind.SetGarrison, defenders);
        }

        public static Command SetPresence(bool away)
        {
            return new Command(CommandKind.SetPresence, away ? 1 : 0);
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
