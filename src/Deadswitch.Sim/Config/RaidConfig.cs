namespace Deadswitch.Sim.Config
{
    /// <summary>Raids: fast, frequent, loot-focused attacks (doc 04 s3, doc 10 s4).</summary>
    public sealed class RaidConfig : IConfigSection
    {
        /// <summary>Raid chance each tick is 1 / MeanIntervalTicks (about every 6h in Tier 1).</summary>
        public int MeanIntervalTicks = 360;
        public int LootPctOfEnergy = 20;
        public int LootCap = 60;
        public int MaxPerDay = 3;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("raid", "Raids: fast, frequent, loot-focused attacks (doc 04 s3, doc 10 s4).");
            v.Int("mean_interval_ticks", ref MeanIntervalTicks, 1, 1_000_000, "A raid triggers each tick with chance 1 / this value.");
            v.Int("loot_pct_of_energy", ref LootPctOfEnergy, 0, 100, "Share of current energy a raid takes.");
            v.Int("loot_cap", ref LootCap, 0, 1_000_000, "Maximum energy a single raid can take (never a full wipe).");
            v.Int("max_per_day", ref MaxPerDay, 0, 100, "Offline attack cap per 24h (doc 10 s4: 3 in Tier 1).");
            v.EndSection();
        }
    }
}
