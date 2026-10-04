namespace Deadswitch.Sim.Config
{
    /// <summary>Adaptive enemies (SPEC-027, doc 04 s9, doc 05 s2). Placeholders: tune with play data.</summary>
    public sealed class AdaptConfig : IConfigSection
    {
        public int LearnCap = 3;
        public int LearnDecayDays = 4;
        public int TurtleCounterPts = 10;
        public int DarkCounterPts = 8;
        public int EvacuateCounterPts = 10;
        public int FortifyCap = 3;
        public int FortifyPct = 15;
        public int FortifyDecayDays = 3;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("adapt", "Adaptive enemies: each faction learns the postures the Hub leans on against it and counters them, and fortifies the sites the Hub keeps raiding. Both fade when tactics vary. All (tune).");
            v.Int("learn_cap", ref LearnCap, 0, 10, "Highest counter level a faction can learn per posture.");
            v.Int("learn_decay_days", ref LearnDecayDays, 1, 100, "Every this many days each learned counter drops a level.");
            v.Int("turtle_counter_pts", ref TurtleCounterPts, 0, 100, "Turtle defense bonus points lost per level against a faction that learned it (breaching charges).");
            v.Int("dark_counter_pts", ref DarkCounterPts, 0, 100, "Go-dark miss chance points lost per level (they sweep for heat signatures).");
            v.Int("evacuate_counter_pts", ref EvacuateCounterPts, 0, 100, "Extra loot % per level when evacuating (they know where the caches go).");
            v.Int("fortify_cap", ref FortifyCap, 0, 10, "Highest fortification a faction builds against the Hub's raids.");
            v.Int("fortify_pct", ref FortifyPct, 0, 100, "Site defense % added per fortification level.");
            v.Int("fortify_decay_days", ref FortifyDecayDays, 1, 100, "Every this many days each fortification drops a level.");
            v.EndSection();
        }
    }
}
