namespace Deadswitch.Sim.Config
{
    /// <summary>Rewarded-ad convenience grants (doc 10 s1.1, ADR-0006): never safety, defense, timer skips or combat power.</summary>
    public sealed class AdConfig : IConfigSection
    {
        public int SalvageEnergy = 120;
        public int SalvageFuel = 12;
        public int CapPct = 15;
        public int CapHours = 24;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("ads", "Rewarded-ad convenience grants (doc 10 s1.1, ADR-0006): Tier 1 only, never during a threat. All (tune).");
            v.Int("salvage_energy", ref SalvageEnergy, 0, 10_000, "Extra salvage roll (once per day): energy.");
            v.Int("salvage_fuel", ref SalvageFuel, 0, 10_000, "Extra salvage roll: fuel.");
            v.Int("cap_pct", ref CapPct, 0, 100, "Idle-cap extension: energy storage +this % while it runs.");
            v.Int("cap_hours", ref CapHours, 1, 1_000, "How long the idle-cap extension runs (game hours).");
            v.EndSection();
        }
    }
}
