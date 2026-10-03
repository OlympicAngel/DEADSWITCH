namespace Deadswitch.Sim.Config
{
    /// <summary>People: scarce, slow-growing, can be lost (doc 02 s5, doc 10 s1.3).</summary>
    public sealed class PeopleConfig : IConfigSection
    {
        public int Start = 12;
        public int Cap = 20;
        public int RegrowthPctOfGapPerHour = 5;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("people", "People: scarce, slow-growing, can be lost (doc 02 s5, doc 10 s1.3).");
            v.Int("start", ref Start, 0, 100_000, "Population at the start of a run.");
            v.Int("cap", ref Cap, 1, 100_000, "Tier 1 base population cap.");
            v.Int("regrowth_pct_of_gap_per_hour", ref RegrowthPctOfGapPerHour, 0, 100, "Each hour, regain this percent of the gap to the cap (rounded up). Paused during blackouts.");
            v.EndSection();
        }
    }
}
