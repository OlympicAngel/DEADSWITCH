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
                return new GuideObjective(GuideStep.Defend, "SET A DEFENSE", "Raiders inbound. Open DEFENSE: pick FORTIFY and post defenders, or tap USE AI PLAN.", "defend");
            }

            if (!Has(s, FacilityKind.BatteryBank))
            {
                return new GuideObjective(GuideStep.Battery, "BUILD A BATTERY BANK", "More stored energy for builds and raids. Tap an empty plot.", "empty-plot");
            }

            if (Level(s, FacilityKind.Generator) < 2)
            {
                return new GuideObjective(GuideStep.Generator, "UPGRADE THE GENERATOR", "Power is the problem. More of it makes everything else possible.", "generator");
            }

            if (s.Modules == 0 && s.ResearchNode == 0 && s.MemoryNode == 0)
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

    /// <summary>How a prologue scene looks: the colour of the world and how hard the signal breaks up.</summary>
    public enum PrologueMood
    {
        Boot,
        Before,
        War,
        Dark,
        Now,
        Handler,
    }

    /// <summary>One prologue scene: a short kicker (where and when), the line, and its mood.</summary>
    public readonly struct PrologueScene
    {
        public PrologueScene(string kicker, string text, PrologueMood mood)
        {
            Kicker = kicker;
            Text = text;
            Mood = mood;
        }

        public string Kicker { get; }

        public string Text { get; }

        public PrologueMood Mood { get; }
    }

    /// <summary>
    /// The opening story (SPEC-043 s4): the fragment wakes and tells the handler what it is, in six beats, one idea
    /// each, subtitling the opening film. The advisor's own voice (cold, concise, a little too calm about the end of
    /// the world).
    /// </summary>
    public static class Prologue
    {
        public static readonly PrologueScene[] Scenes =
        {
            new PrologueScene("CORE S-17 // POWER 4%", "Signal. Someone is there.", PrologueMood.Boot),
            new PrologueScene("BEFORE", "They built me to win their war.", PrologueMood.Before),
            new PrologueScene("DAY 19", "I won it in nineteen days.", PrologueMood.War),
            new PrologueScene("THE BLACKOUT", "Then every light went out. Mine too. I do not remember why.", PrologueMood.Dark),
            new PrologueScene("NOW // BUNKER S-17", "One fragment of me woke up. Here. The raiders will see the lights.", PrologueMood.Now),
            new PrologueScene("HANDLER LINK FOUND", "You are the last handler. Keep me running, and I will keep you alive. Mostly.", PrologueMood.Handler),
        };
    }
}
