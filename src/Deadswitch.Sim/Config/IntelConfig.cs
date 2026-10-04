namespace Deadswitch.Sim.Config
{
    /// <summary>Spies in faction camps (SPEC-019, doc 05 s3+s5, doc 10 s5). Placeholders: tune at F-099.</summary>
    public sealed class IntelConfig : IConfigSection
    {
        public int SpyEnergy = 150;
        public int SpyPeople = 1;
        public int DoubleBasePct = 10;
        public int DoubleHeatPermille = 300;
        public int DoubleColdPermille = 200;
        public int SpyWarningMinutes = 30;
        public int DoubleEstimatePct = 55;
        public int DoubleSiteDefensePct = 60;
        public int CaughtPctPerHourHunted = 1;
        public int CaughtHeat = 10_000;
        public int FrameHeat = 20_000;
        public int FrameCatchPct = 35;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("intel", "Spies in faction camps: plant, recall, frame; hidden double agents exposed by scouts (doc 05, doc 10 s5). Heat is milli. All (tune).");
            v.Int("spy_energy", ref SpyEnergy, 0, 100_000, "Energy to plant a spy.");
            v.Int("spy_people", ref SpyPeople, 1, 10, "People a spy takes out of the Hub (they come back on recall).");
            v.Int("double_base_pct", ref DoubleBasePct, 0, 100, "Base chance a new spy is a double agent (doc 10: 10%).");
            v.Int("double_heat_permille", ref DoubleHeatPermille, 0, 10_000, "Extra double-agent chance per point of that faction's heat %, in permille of a point.");
            v.Int("double_cold_permille", ref DoubleColdPermille, 0, 10_000, "Extra double-agent chance per point of the AI's Coldness %, in permille of a point.");
            v.Int("spy_warning_minutes", ref SpyWarningMinutes, 0, 1_000, "A loyal spy warns of that faction's attacks this much earlier, with an exact estimate.");
            v.Int("double_estimate_pct", ref DoubleEstimatePct, 1, 100, "A double agent's attack estimate, as % of the true strength.");
            v.Int("double_site_defense_pct", ref DoubleSiteDefensePct, 1, 100, "A double agent's site defense report, as % of the truth.");
            v.Int("caught_pct_per_hour_hunted", ref CaughtPctPerHourHunted, 0, 100, "Hourly chance a spy is caught while that faction hunts the Hub (Hunted or worse).");
            v.Int("caught_heat", ref CaughtHeat, 0, 100_000, "Heat a caught spy adds.");
            v.Int("frame_heat", ref FrameHeat, 0, 100_000, "Framing: heat moved off the spy's faction (half lands on a rival); a double agent's frame adds it instead.");
            v.Int("frame_catch_pct", ref FrameCatchPct, 0, 100, "Chance the spy is caught while framing.");
            v.EndSection();
        }
    }
}
