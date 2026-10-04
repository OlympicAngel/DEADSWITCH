using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Hourly population regrowth toward the current cap (base + powered Life Support), paused during
    /// blackouts (doc 10 s1.3). A lowered cap never kills anyone; it only stops regrowth.
    /// </summary>
    public static class PeopleSystem
    {
        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            // people away on operations still hold their place (F-019)
            int cap = Economy.PopulationCap(s, ctx.Config) - WorldSystem.Away(s);
            if (!s.Blackout && s.People < cap)
            {
                int gap = cap - s.People;
                int gain = SimMath.PctCeil(gap, ctx.Config.People.RegrowthPctOfGapPerHour);
                s.People = SimMath.Clamp(s.People + gain, 0, cap);
            }
        }
    }
}
