namespace Deadswitch.Sim.Config
{
    /// <summary>Luck swings (SPEC-028, doc 04 s9): hidden streaks, AI half-warnings, opportunity windows. Placeholders.</summary>
    public sealed class LuckConfig : IConfigSection
    {
        public int StreakHours = 12;
        public int CalmPct = 25;
        public int RestlessPct = 25;
        public int CalmIntervalPct = 60;
        public int RestlessIntervalPct = -30;
        public int RestlessStrengthPct = 10;
        public int HunchPct = 60;
        public int HunchAccuracyPct = 70;
        public int RegroupMarginPct = 150;
        public int RegroupHours = 8;
        public int RegroupDefensePct = 25;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("luck", "Luck swings: hidden calm and restless streaks the AI half-warns about, and regrouping windows after a crushing defense (doc 04 s9). All (tune).");
            v.Int("streak_hours", ref StreakHours, 1, 1_000, "Length of one hidden streak window.");
            v.Int("calm_pct", ref CalmPct, 0, 100, "Chance a window is calm.");
            v.Int("restless_pct", ref RestlessPct, 0, 100, "Chance a window is restless (calm_pct + restless_pct <= 100).");
            v.Int("calm_interval_pct", ref CalmIntervalPct, 0, 1_000, "Calm windows: the mean time between raids grows by this %.");
            v.Int("restless_interval_pct", ref RestlessIntervalPct, -90, 0, "Restless windows: the mean time between raids changes by this % (negative = more raids).");
            v.Int("restless_strength_pct", ref RestlessStrengthPct, 0, 100, "Restless windows: raids come this much stronger.");
            v.Int("hunch_pct", ref HunchPct, 0, 100, "Chance the AI voices a hunch at the start of a calm or restless window.");
            v.Int("hunch_accuracy_pct", ref HunchAccuracyPct, 0, 100, "Chance the hunch names the window's real mood (otherwise it names the opposite).");
            v.Int("regroup_margin_pct", ref RegroupMarginPct, 100, 1_000, "A raid repelled with defense at least this % of its strength sends that faction regrouping.");
            v.Int("regroup_hours", ref RegroupHours, 1, 1_000, "How long a beaten faction regroups.");
            v.Int("regroup_defense_pct", ref RegroupDefensePct, 0, 90, "A regrouping faction's sites are this much weaker (an opportunity window).");
            v.EndSection();
        }
    }
}
