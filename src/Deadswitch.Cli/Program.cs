using System;
using System.Globalization;
using System.Linq;
using Deadswitch.Sim;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Cli
{
    /// <summary>
    /// Headless sim runner for the paper prototype.
    /// Usage: dotnet run --project src/Deadswitch.Cli -- [seed] [hours]
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            ulong seed = args.Length > 0 ? ulong.Parse(args[0], CultureInfo.InvariantCulture) : 42UL;
            long hours = args.Length > 1 ? long.Parse(args[1], CultureInfo.InvariantCulture) : 24L;

            var sim = new Simulation(seed);
            sim.Run(hours * SimConfig.TicksPerHour);

            GameState s = sim.State;
            Console.WriteLine("seed=" + seed + " hours=" + hours + " tick=" + s.Tick);
            Console.WriteLine("energy=" + s.Energy + " fuel=" + s.Fuel + " compute=" + s.Compute
                + " people=" + s.People + " corruption=" + s.Corruption);
            Console.WriteLine("raids=" + sim.Log.Events.Count(e => e.Kind == EventKind.RaidStarted)
                + " blackouts=" + sim.Log.Events.Count(e => e.Kind == EventKind.BlackoutStarted));
            Console.WriteLine("hash=" + StateHasher.Hash(s).ToString("x16", CultureInfo.InvariantCulture));
            return 0;
        }
    }
}
