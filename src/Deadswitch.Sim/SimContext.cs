using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim
{
    /// <summary>What every system and command handler works on. One instance per <see cref="Simulation"/>.</summary>
    public sealed class SimContext
    {
        public SimContext(GameState state, SimConfig config, EventLog log)
        {
            State = state;
            Config = config;
            Log = log;
        }

        public GameState State { get; }

        public SimConfig Config { get; }

        public EventLog Log { get; }

        /// <summary>Appends an event stamped with the current tick.</summary>
        public void Emit(EventKind kind, int a = 0, int b = 0, int c = 0, int d = 0)
        {
            Log.Append(State.Tick, kind, a, b, c, d);
        }
    }
}
