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
            // people away on operations and in faction camps still hold their place (F-019, SPEC-019)
            int cap = Economy.PopulationCap(s, ctx.Config) - WorldSystem.Away(s) - IntelSystem.Away(s, ctx.Config);
            if (!s.Blackout && s.People < cap)
            {
                int gap = cap - s.People;
                // wrecks in the yard slow regrowth (SPEC-018)
                int pct = SimMath.PctFloor(ctx.Config.People.RegrowthPctOfGapPerHour, 100 + (s.Perks[(int)Perk.Regrowth] * ctx.Config.Legacy.PerkRegrowthPct));
                int gain = (int)((((long)gap * pct * ScarSystem.RegrowthPct(s, ctx.Config)) + 9_999) / 10_000);
                s.People = SimMath.Clamp(s.People + gain, 0, cap);
            }
        }
    }
}
