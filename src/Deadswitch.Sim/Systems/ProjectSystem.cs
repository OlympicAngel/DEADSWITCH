using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// The AI's hidden project (SPEC-007): grows with Boldness and with compute the AI skims from the stock;
    /// stage changes are logged for the Audit. A bold AI also shows corruption lower than it is.
    /// </summary>
    public static class ProjectSystem
    {
        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            ProjectConfig p = c.Project;
            if (ClimaxSystem.Silenced(s))
            {
                return;
            }

            int skim = 0;
            if (s.BoldnessMilli >= p.SkimFromBoldnessPct * 1000)
            {
                skim = SimMath.Clamp(SimMath.PctFloor(Economy.Flows(s, c).ComputePerHour, p.SkimPct), 0, s.Compute);
                s.Compute -= skim;
                s.SkimmedSinceAudit += skim;
            }

            ProjectStage before = Stage(c, s.ProjectMilli);
            long growth = ((long)s.BoldnessMilli * p.GrowthPerHourAtFullBoldness / 100_000) + ((long)skim * p.MilliPerSkimmedCompute);
            s.ProjectMilli = (int)System.Math.Min(100_000L, s.ProjectMilli + growth);
            ProjectStage after = Stage(c, s.ProjectMilli);
            if (after != before)
            {
                ctx.Emit(EventKind.ProjectStage, (int)after, (int)before, s.ProjectMilli);
            }

            // Every hour at Imminent without a window opens one (also after loading an older save or a shallow cancel).
            ClimaxSystem.OnStage(ctx, after);
        }

        public static ProjectStage Stage(SimConfig c, int milli)
        {
            int pct = milli / 1000;
            if (pct >= c.Project.ImminentFrom)
            {
                return ProjectStage.Imminent;
            }

            if (pct >= c.Project.AdvancedFrom)
            {
                return ProjectStage.Advanced;
            }

            return pct >= c.Project.ActiveFrom ? ProjectStage.Active : ProjectStage.Dormant;
        }

        /// <summary>The corruption the AI displays (SPEC-007 rule 4): lower than the truth while it is bold.</summary>
        public static int ReportedCorruptionMilli(GameState s, SimConfig c)
        {
            return s.BoldnessMilli >= 50_000 ? s.CorruptionMilli - SimMath.PctFloor(s.CorruptionMilli, c.Project.UnderreportPct) : s.CorruptionMilli;
        }
    }
}
