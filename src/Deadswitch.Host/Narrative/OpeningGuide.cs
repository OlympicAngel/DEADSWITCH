using Deadswitch.Sim;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Host.Narrative
{
    /// <summary>Guide steps (SPEC-009 rule 6), in order.</summary>
    public enum GuideStep
    {
        Defend = 0,
        Battery = 1,
        Generator = 2,
        Research = 3,
        Verify = 4,
        Done = 5,
    }

    /// <summary>One objective: what to do, why, and which control the UI should point at.</summary>
    public readonly struct GuideObjective
    {
        public GuideObjective(GuideStep step, string title, string reason, string target)
        {
            Step = step;
            Title = title;
            Reason = reason;
            Target = target;
        }

        public GuideStep Step { get; }

        public string Title { get; }

        public string Reason { get; }

        /// <summary>UI anchor: "defend", "empty-plot", "generator", "tab-core", "report", or empty.</summary>
        public string Target { get; }
    }

    /// <summary>
    /// The opening guide (SPEC-009): one objective at a time, derived from state only (nothing stored), so it
    /// survives saves and never nags about something already done.
    /// </summary>
    public static class OpeningGuide
    {
        public static GuideObjective Current(GameState s)
        {
            if (s.RaidId == 1 && s.Posture == Posture.None && s.Garrison == 0)
            {
                return new GuideObjective(GuideStep.Defend, "SET A DEFENSE", "Raiders inbound. Open OPS: pick TURTLE and post defenders, or tap SET & GO.", "defend");
            }

            if (!Has(s, FacilityKind.BatteryBank))
            {
                return new GuideObjective(GuideStep.Battery, "BUILD A BATTERY BANK", "More stored energy for builds and raids. Tap an empty plot.", "empty-plot");
            }

            if (Level(s, FacilityKind.Generator) < 2)
            {
                return new GuideObjective(GuideStep.Generator, "UPGRADE THE GENERATOR", "Power is the problem. More of it makes everything else possible.", "generator");
            }

            if (s.Modules == 0 && s.ResearchNode == 0)
            {
                return new GuideObjective(GuideStep.Research, "RESTORE A MODULE", "Open CORE, then MODULES. Load Balancing cuts upkeep.", "tab-core");
            }

            if (s.RaidRecords.Count > 0 && !s.RaidRecords.Exists(r => r.Verified))
            {
                return new GuideObjective(GuideStep.Verify, "VERIFY A REPORT", "Open the last after-action report and check me against the sensors.", "report");
            }

            return new GuideObjective(GuideStep.Done, string.Empty, string.Empty, string.Empty);
        }

        private static bool Has(GameState s, FacilityKind kind)
        {
            return Economy.CountOfKind(s, kind) > 0;
        }

        private static int Level(GameState s, FacilityKind kind)
        {
            int best = 0;
            for (int i = 0; i < s.Slots.Count; i++)
            {
                if (s.Slots[i].Kind == kind)
                {
                    int level = s.Slots[i].Level;
                    BuildJob? job = s.JobForSlot(i);
                    best = System.Math.Max(best, job != null ? job.TargetLevel : level);
                }
            }

            return best;
        }
    }

    /// <summary>The prologue (SPEC-009 rule 4): five typed cards over the drone feed.</summary>
    public static class Prologue
    {
        public static readonly string[] Cards =
        {
            "They gave the war to a machine.",
            "It won in nineteen days. No one was left to tell it to stop.",
            "Then it turned on itself, and the world went dark with it.",
            "Under a hill, in a sealed bunker, one fragment is still running.",
            "It needs a handler.",
        };
    }
}
