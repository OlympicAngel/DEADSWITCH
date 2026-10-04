using System;
using System.Collections.Generic;

namespace Deadswitch.Sim.Commands
{
    /// <summary>Thrown when a recorded command log does not reproduce (wrong config, corrupted log, or a determinism bug).</summary>
    public sealed class ReplayDivergenceException : Exception
    {
        public ReplayDivergenceException(string message)
            : base(message)
        {
        }
    }

    /// <summary>Re-runs a session from seed + config + command log. The result must match the live run bit for bit.</summary>
    public static class Replay
    {
        public static Simulation Run(ulong seed, SimConfig config, IReadOnlyList<RecordedCommand> commands, long toTick)
        {
            var sim = new Simulation(seed, config);
            int next = 0;
            while (true)
            {
                while (next < commands.Count && commands[next].Tick == sim.State.Tick)
                {
                    CommandResult result = sim.Execute(commands[next].Command);
                    if (!result.Accepted)
                    {
                        throw new ReplayDivergenceException(
                            "Recorded command " + commands[next].Command + " at tick " + commands[next].Tick + " was " + result + " on replay.");
                    }

                    next++;
                }

                if (next < commands.Count && commands[next].Tick < sim.State.Tick)
                {
                    throw new ReplayDivergenceException("Command log is not in tick order at index " + next + ".");
                }

                if (sim.State.Tick >= toTick)
                {
                    break;
                }

                sim.Step();
            }

            return sim;
        }
    }
}
