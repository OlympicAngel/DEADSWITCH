namespace Deadswitch.Sim.Config
{
    /// <summary>OVERRIDE, the emergency toolkit (doc 03 s6, doc 10 s3).</summary>
    public sealed class OverrideConfig : IConfigSection
    {
        public int StartCharges = 1;
        public int MaxCharges = 3;
        public int RegenMinutes = 720;
        public int CooldownMinutes = 30;
        public int CorruptionMilliPerUse = 8000;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("override", "OVERRIDE: limited charges, shared cooldown, corruption per use (doc 03 s6, doc 10 s3).");
            v.Int("start_charges", ref StartCharges, 0, 10, "Charges at the start of a run.");
            v.Int("max_charges", ref MaxCharges, 1, 10, "Maximum stored charges (v1: 3).");
            v.Int("regen_minutes", ref RegenMinutes, 1, 100_000, "Game minutes per regenerated charge.");
            v.Int("cooldown_minutes", ref CooldownMinutes, 0, 100_000, "Shared cooldown after any use.");
            v.Int("corruption_milli_per_use", ref CorruptionMilliPerUse, 0, 100_000, "Corruption added per use (1000 = 1%).");
            v.EndSection();
        }
    }
}
