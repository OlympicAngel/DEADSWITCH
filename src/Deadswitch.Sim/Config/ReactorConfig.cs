namespace Deadswitch.Sim.Config
{
    /// <summary>Reactor rules (SPEC-029, doc 02 s3 risky power sources). Placeholders: tune with play data.</summary>
    public sealed class ReactorConfig : IConfigSection
    {
        public int MinTier = 3;
        public int MaxCount = 1;
        public int[] FuelPerHour = { 10, 14, 18 };
        public int TargetPct = 50;
        public int LeakDamage = 2;
        public int LeakPeoplePerDay = 3;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("reactor", "Reactor rules: fuel-fed, one per Hub from Tier 3, the first thing raiders aim for, and a radiation leak while badly damaged (SPEC-029). All (tune).");
            v.Int("min_tier", ref MinTier, 1, 4, "Tier needed to build a reactor.");
            v.Int("max_count", ref MaxCount, 0, 10, "Reactors allowed per Hub.");
            v.IntList("fuel_per_hour", ref FuelPerHour, 0, 10_000, 1, 20, "Fuel burned per game hour, per level; without it the reactor scrams (no output).");
            v.Int("target_pct", ref TargetPct, 0, 100, "Chance each point of breach damage goes to the reactor when it can take more.");
            v.Int("leak_damage", ref LeakDamage, 1, 10, "Damage at which a reactor leaks radiation until repaired.");
            v.Int("leak_people_per_day", ref LeakPeoplePerDay, 0, 1_000, "People lost each midnight while it leaks (never below the people floor).");
            v.EndSection();
        }
    }
}
