namespace Deadswitch.Sim.Config
{
    /// <summary>Defense setup: garrison and postures (doc 04 s5, doc 10 s4, SPEC-001 rules 5-6).</summary>
    public sealed class DefenseConfig : IConfigSection
    {
        public int GarrisonSlots = 5;
        public int DefensePerDefender = 4;
        public int TurtleDefensePct = 50;
        public int DarkMissPct = 40;
        public int DarkUpkeepPerHour = 120;
        public int EvacuateLootPct = 150;
        public int CasualtyPct = 50;
        public int OfflineUnpreparedPct = 150;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("defense", "Defense setup: garrison slots and postures (doc 04 s5, doc 10 s4).");
            v.Int("garrison_slots", ref GarrisonSlots, 0, 100, "People that can be posted as defenders (doc 10: 3-5).");
            v.Int("defense_per_defender", ref DefensePerDefender, 0, 10_000, "Defense each garrisoned person adds.");
            v.Int("turtle_defense_pct", ref TurtleDefensePct, 0, 1_000, "Turtle posture: extra defense in percent.");
            v.Int("dark_miss_pct", ref DarkMissPct, 0, 100, "Dark posture: chance a raid fails to find the Hub.");
            v.Int("dark_upkeep_per_hour", ref DarkUpkeepPerHour, 0, 100_000, "Dark posture: extra energy per game hour (signal masking).");
            v.Int("evacuate_loot_pct", ref EvacuateLootPct, 0, 1_000, "Evacuate posture: loot multiplier in percent (no casualties).");
            v.Int("casualty_pct", ref CasualtyPct, 0, 100, "Share of the garrison lost at a full breach (scales with breach).");
            v.Int("offline_unprepared_pct", ref OfflineUnpreparedPct, 100, 1_000, "Raid strength multiplier when the handler is away with no posture and no garrison.");
            v.EndSection();
        }
    }
}
