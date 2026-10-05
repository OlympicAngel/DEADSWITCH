using System.Collections.Generic;
using Deadswitch.Game.Presentation;
using Deadswitch.Sim;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>What a suggestion chip does when tapped.</summary>
    public enum SuggestionAction
    {
        Screen,
        Resource,
        Plot,
    }

    /// <summary>One actionable chip under the advisor line (SPEC-039 idea 49).</summary>
    public sealed class Suggestion
    {
        public string Glyph;
        public string Label;
        public SuggestionAction Action;
        public string Screen;
        public ResKind Resource;
        public int Slot = -1;
        public bool Urgent;
    }

    /// <summary>
    /// The AI's proactive read of the Hub (JARVIS layer): the two most useful one-tap next steps right now, and a
    /// mood word for the comms panel. Pure read of the sim; ranked threat first, then shortages, then idle work.
    /// </summary>
    public static class AiSuggestions
    {
        public const int Max = 2;

        public static List<Suggestion> For(GameState s, SimConfig c, bool reportReady)
        {
            var list = new List<Suggestion>();
            if (s.RaidId != 0)
            {
                list.Add(new Suggestion { Glyph = "shield", Label = "SET DEFENSE", Action = SuggestionAction.Screen, Screen = "ops", Urgent = true });
            }

            if (s.Ultimatum == UltimatumStage.Issued || s.Dilemma != DilemmaKind.None)
            {
                list.Add(new Suggestion { Glyph = "mail", Label = s.Ultimatum == UltimatumStage.Issued ? "ULTIMATUM" : "DILEMMA", Action = SuggestionAction.Screen, Screen = "dispatch", Urgent = true });
            }

            if (reportReady)
            {
                list.Add(new Suggestion { Glyph = "report", Label = "READ REPORT", Action = SuggestionAction.Screen, Screen = "report" });
            }

            ResourceInfo worst = null;
            foreach (ResKind kind in new[] { ResKind.Energy, ResKind.Compute, ResKind.Fuel, ResKind.People })
            {
                if (kind == ResKind.Fuel && s.Fuel <= 0 && Economy.CountOfKind(s, FacilityKind.Reactor) == 0 && Economy.CountOfKind(s, FacilityKind.MotorPool) == 0)
                {
                    continue;
                }

                ResourceInfo r = ResourceInfo.Of(kind, s, c);
                if (r.State != ResState.Ok && (worst == null || Rank(r.State) > Rank(worst.State)))
                {
                    worst = r;
                }
            }

            if (worst != null)
            {
                string verb = worst.State == ResState.Full ? "FULL" : worst.State == ResState.Short ? "SHORT" : "LOW";
                list.Add(new Suggestion { Glyph = worst.Glyph, Label = worst.Name + " " + verb, Action = SuggestionAction.Resource, Resource = worst.Kind, Urgent = worst.State == ResState.Short });
            }

            int plot = ResourceInfo.FreePlot(s);
            if (s.Jobs.Count == 0 && plot >= 0 && s.RaidId == 0)
            {
                list.Add(new Suggestion { Glyph = "hammer", Label = "BUILD", Action = SuggestionAction.Plot, Slot = plot });
            }

            if (CorruptionSystem.Band(c, s.CorruptionMilli) >= CorruptionBand.Unstable)
            {
                list.Add(new Suggestion { Glyph = "core", Label = "CHECK CORE", Action = SuggestionAction.Screen, Screen = "core" });
            }

            if (list.Count > Max)
            {
                list.RemoveRange(Max, list.Count - Max);
            }

            return list;
        }

        /// <summary>A word for how the AI reads the moment (comms panel tag).</summary>
        public static string Mood(GameState s, SimConfig c)
        {
            if (s.BattleLive || BattleSystem.InBattle(s))
            {
                return "ENGAGED";
            }

            if (s.RaidId != 0)
            {
                return "ALERT";
            }

            if (s.Blackout)
            {
                return "STRAINED";
            }

            CorruptionBand band = CorruptionSystem.Band(c, s.CorruptionMilli);
            return band >= CorruptionBand.Unstable ? "UNSTABLE" : band == CorruptionBand.Glitchy ? "NOISY" : "CALM";
        }

        private static int Rank(ResState s)
        {
            return s == ResState.Short ? 3 : s == ResState.Low ? 2 : s == ResState.Full ? 1 : 0;
        }
    }
}
