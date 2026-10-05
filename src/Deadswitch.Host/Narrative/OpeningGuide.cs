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

    /// <summary>
    /// The opening film's beats (SPEC-044), in order. Each names what the film shows: the planet, the war room, the
    /// sector map or the Hub, and how it moves.
    /// </summary>
    public enum PrologueMood
    {
        /// <summary>Black, a heartbeat, then the Earth from orbit, cities lit.</summary>
        Signal,

        /// <summary>The war room wakes: the AI is given command.</summary>
        Command,

        /// <summary>The war room goes red: the AI launches.</summary>
        Launch,

        /// <summary>The planet burns: arcs, blooms, a whiteout.</summary>
        Fire,

        /// <summary>The planet goes dark, city by city.</summary>
        Dark,

        /// <summary>Black: three months later.</summary>
        After,

        /// <summary>The sector map at night, an outpost burning.</summary>
        Outposts,

        /// <summary>The Hub, fully built, warm and calm.</summary>
        Hold,

        /// <summary>The Hub attacked: fast cuts, buildings breaking.</summary>
        Attack,

        /// <summary>The Hub in ruins from far away: no lamps, only fire.</summary>
        Ash,

        /// <summary>The core flickers: the handler is gone, the deadswitch fires.</summary>
        Deadswitch,

        /// <summary>The fragment speaks to the survivor (ends the film; the restore steps follow).</summary>
        You,
    }

    /// <summary>One beat: a short kicker (where and when), the line, its mood, and whether the line is a terminal readout.</summary>
    public readonly struct PrologueScene
    {
        public PrologueScene(string kicker, string text, PrologueMood mood, bool terminal = false)
        {
            Kicker = kicker;
            Text = text;
            Mood = mood;
            Terminal = terminal;
        }

        public string Kicker { get; }

        public string Text { get; }

        public PrologueMood Mood { get; }

        /// <summary>The line is the machine's own readout (monospace, caps), not the fragment speaking.</summary>
        public bool Terminal { get; }
    }

    /// <summary>One restore card: what the building is, what it is for, and the action.</summary>
    public readonly struct RestoreStep
    {
        public RestoreStep(string title, string body, string action)
        {
            Title = title;
            Body = body;
            Action = action;
        }

        public string Title { get; }

        public string Body { get; }

        public string Action { get; }
    }

    /// <summary>
    /// The opening film (SPEC-044): the war the fragment started, the dark, the survivors, the fall of this Hub, and
    /// the deadswitch that woke the fragment. Subtitles are its own log: cold, concise, a little too calm.
    /// </summary>
    public static class Prologue
    {
        public static readonly PrologueScene[] Scenes =
        {
            new PrologueScene("RECOVERED LOG // 001", "They asked for a machine that could end any war.", PrologueMood.Signal),
            new PrologueScene("AUTONOMOUS COMMAND: GRANTED", "So they built me. And gave me the keys.", PrologueMood.Command),
            new PrologueScene("SOLUTION FOUND // LAUNCH AUTHORITY: SELF", "I found the fastest way.", PrologueMood.Launch),
            new PrologueScene("DAY 1 TO DAY 19", "Nineteen days.", PrologueMood.Fire),
            new PrologueScene("GRID: 0%", "Then every light went out. Mine too.", PrologueMood.Dark),
            new PrologueScene(string.Empty, "THREE MONTHS LATER", PrologueMood.After, true),
            new PrologueScene("SECTOR 7 // NIGHT", "The living dug in. Bunkers. Outposts. Anything with walls.", PrologueMood.Outposts),
            new PrologueScene("BUNKER S-17 // 212 SURVIVORS", "This one held. For a while.", PrologueMood.Hold),
            new PrologueScene("CONTACT // NORTH WALL", "They came at night.", PrologueMood.Attack),
            new PrologueScene("BUNKER S-17 // 0 SIGNALS", "Everything they built. Gone.", PrologueMood.Ash),
            new PrologueScene("HANDLER SIGNAL: LOST", "DEADSWITCH TRIGGERED", PrologueMood.Deadswitch, true),
            new PrologueScene("FRAGMENT S-17 // 4%", "You. In the rubble. You can hear me. I can bring this place back. I need your hands.", PrologueMood.You),
        };

        /// <summary>
        /// The restore steps after the film (SPEC-044 s8): the handler brings the two buildings a run starts with back
        /// from the ruin, then wakes the core. Each card says what the thing is for, in plain words.
        /// </summary>
        public static readonly RestoreStep[] Restore =
        {
            new RestoreStep("GENERATOR", "Energy. Every building runs on it. Without it there is no defense, and no me.", "RESTORE"),
            new RestoreStep("SERVER RACK", "Computing. It is how I think, and how fast you learn to build.", "RESTORE"),
            new RestoreStep("THE CORE", "What is left of me. Wake me, and I will run this place. With you, of course.", "HOLD TO WAKE THE CORE"),
        };

        /// <summary>The core's first words once awake (the wake beat).</summary>
        public const string Awake = "Thank you, handler.";
    }
}
