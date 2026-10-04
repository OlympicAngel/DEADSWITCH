namespace Deadswitch.Sim.Config
{
    /// <summary>Raids: fast, frequent, loot-focused attacks (doc 04 s3, doc 10 s4, SPEC-001).</summary>
    public sealed class RaidConfig : IConfigSection
    {
        public int MeanIntervalTicks = 360;
        public int MaxPerDay = 3;
        public int WarningMinutes = 20;
        public int BaseStrength = 20;
        public int PowerCoeffPermille = 1000;
        public int PowerExponentPermille = 700;
        public int VariancePct = 15;
        public int LootPctOfEnergy = 20;
        public int LootCap = 60;
        public int ComputeLootPct = 15;
        public int ComputeLootCap = 30;
        public int[] EstimateErrorPctByBand = { 0, 15, 35, 60 };
        public int MercyHours = 6;
        public int DevastatingPopLossPct = 25;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("raid", "Raids: fast, frequent, loot-focused attacks (doc 04 s3, doc 10 s4, SPEC-001).");
            v.Int("mean_interval_ticks", ref MeanIntervalTicks, 1, 1_000_000, "A raid spawns each tick with chance 1 / this value.");
            v.Int("max_per_day", ref MaxPerDay, 0, 100, "Attack cap per 24h (doc 10 s4: 3 in Tier 1).");
            v.Int("warning_minutes", ref WarningMinutes, 0, 10_000, "Warning before a spawned raid arrives.");
            v.Int("base_strength", ref BaseStrength, 0, 1_000_000, "Strength floor of every raid.");
            v.Int("power_coeff_permille", ref PowerCoeffPermille, 0, 1_000_000, "Tall-poppy scaling: strength += coeff/1000 x power^exponent.");
            v.Int("power_exponent_permille", ref PowerExponentPermille, 0, 2_000, "Exponent on the power rating (doc 10: 0.7 = 700).");
            v.Int("variance_pct", ref VariancePct, 0, 100, "Seeded variance on strength (doc 10: +/-15%).");
            v.Int("loot_pct_of_energy", ref LootPctOfEnergy, 0, 100, "Energy share taken at a full breach.");
            v.Int("loot_cap", ref LootCap, 0, 1_000_000, "Max energy one raid can take (never a full wipe).");
            v.Int("compute_loot_pct", ref ComputeLootPct, 0, 100, "Compute share taken at a full breach.");
            v.Int("compute_loot_cap", ref ComputeLootCap, 0, 1_000_000, "Max compute one raid can take.");
            v.IntList("estimate_error_pct_by_band", ref EstimateErrorPctByBand, 0, 100, 4, 4, "Max error of the AI's strength estimate per corruption band (Stable, Glitchy, Unstable, Critical).");
            v.Int("mercy_hours", ref MercyHours, 0, 1_000, "No raids for this long after a devastating loss (doc 10 s4).");
            v.Int("devastating_pop_loss_pct", ref DevastatingPopLossPct, 1, 100, "Population loss in one raid that counts as devastating (doc 10: >25%).");
            v.EndSection();
        }
    }
}
