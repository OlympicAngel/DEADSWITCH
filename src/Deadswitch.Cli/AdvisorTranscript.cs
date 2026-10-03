using System;
using System.IO;
using Deadswitch.Host.Narrative;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Cli
{
    /// <summary>
    /// Prints what the advisor would say over a run (voice review, SPEC-004): Manual uses the scripted handler,
    /// Delegated/Autopilot leave the base to the AI. Real time is simulated as a few seconds per game minute.
    /// </summary>
    public static class AdvisorTranscript
    {
        private const float SecondsPerTick = 2f;

        public static void Write(Simulation sim, long ticks, DelegationLevel delegation, bool away, TextWriter output)
        {
            AdvisorLines lines = AdvisorLines.LoadEmbedded() ?? throw new InvalidOperationException("Advisor lines are not embedded in Deadswitch.Host.");
            var advisor = new Advisor(lines);
            advisor.Boot();
            if (delegation != DelegationLevel.Manual)
            {
                sim.Execute(Command.SetDelegation(delegation));
            }

            if (away)
            {
                sim.Execute(Command.SetPresence(true));
            }

            int seen = sim.Log.Count;
            long end = sim.State.Tick + ticks;
            while (sim.State.Tick < end)
            {
                if (delegation == DelegationLevel.Manual && sim.State.Tick % 20 == 0)
                {
                    Deadswitch.Host.Dev.ScriptedPlayer.Think(sim);
                }

                sim.Step();
                for (; seen < sim.Log.Count; seen++)
                {
                    advisor.Observe(sim.Log.Events[seen], sim.State);
                }

                advisor.ObserveState(sim.State, sim.Config);
                string? line = advisor.Update(SecondsPerTick);
                if (line != null)
                {
                    long t = sim.State.Tick;
                    output.WriteLine("D" + ((t / SimConfig.TicksPerDay) + 1) + " " + ((t % SimConfig.TicksPerDay) / 60).ToString("00") + ":" + (t % 60).ToString("00")
                        + "  [" + advisor.LastId + "] " + line);
                }
            }

            output.WriteLine("-- boldness " + (sim.State.BoldnessMilli / 1000) + "%, lies " + sim.State.LiesTold + ", raids " + (sim.State.NextRaidId - 1));
        }
    }
}
