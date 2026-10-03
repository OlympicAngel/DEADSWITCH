using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Sim
{
    /// <summary>
    /// Deterministic tick loop (1 tick = 1 game minute) orchestrating the systems in a fixed order.
    /// Invariant: Run(a) then Run(b) must equal Run(a + b) (offline catch-up relies on it).
    /// </summary>
    public sealed class Simulation
    {
        public Simulation(ulong seed, SimConfig? config = null)
        {
            Seed = seed;
            Config = config ?? SimConfig.Tier1();
            State = new GameState(seed, Config);
            Log = new EventLog();
            Context = new SimContext(State, Config, Log);
            Commands = new CommandLog();
        }

        public ulong Seed { get; }

        public SimConfig Config { get; }

        public GameState State { get; }

        public EventLog Log { get; }

        public SimContext Context { get; }

        public CommandLog Commands { get; }

        /// <summary>
        /// Validates and applies a player command at the current tick boundary (after tick <c>State.Tick</c>).
        /// Accepted commands are recorded for saves and replays; rejected ones change nothing.
        /// </summary>
        public CommandResult Execute(Command command)
        {
            CommandResult result = CommandProcessor.Execute(Context, command);
            if (result.Accepted)
            {
                Commands.Add(new RecordedCommand(State.Tick, command));
            }

            return result;
        }

        public void Run(long ticks)
        {
            for (long i = 0; i < ticks; i++)
            {
                Step();
            }
        }

        /// <summary>Advances one tick. System order is part of the rules: changing it changes outcomes.</summary>
        public void Step()
        {
            SimContext ctx = Context;
            ctx.State.Tick++;

            RaidSystem.StartOfTick(ctx);
            EnergySystem.Tick(ctx);

            if (ctx.State.Tick % SimConfig.TicksPerHour == 0)
            {
                CorruptionSystem.Hourly(ctx);
                PeopleSystem.Hourly(ctx);
            }

            RaidSystem.Tick(ctx);
        }
    }
}
