using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>Hourly population regrowth toward the cap, paused during blackouts (doc 10 s1.3).</summary>
    public static class PeopleSystem
    {
        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;

            if (s.Energy > 0 && s.People < c.People.Cap)
            {
                int gap = c.People.Cap - s.People;
                int gain = SimMath.PctCeil(gap, c.People.RegrowthPctOfGapPerHour);
                s.People = SimMath.Clamp(s.People + gain, 0, c.People.Cap);
            }
        }
    }
}
