namespace Deadswitch.Sim.Config
{
    /// <summary>Hub tiers and the tier-up gates (doc 06 s2, doc 10 s1.3, SPEC-008). Index 0 = advancing from Tier 1.</summary>
    public sealed class TierConfig : IConfigSection
    {
        public int[] PopBonus = { 0, 25, 80, 200 };
        public int[] LevelsToAdvance = { 10, 25, 45 };
        public int[] NetEnergyToAdvance = { 100, 300, 600 };
        public int[] PeopleCostBase = { 4, 10, 20 };
        public int PeopleCostPerLevels = 4;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("tier", "Hub tiers and tier-up gates (doc 06 s2, SPEC-008). All (tune).");
            v.IntList("pop_bonus", ref PopBonus, 0, 10_000, 4, 4, "Base population cap bonus per tier over people.cap (doc 10: 20 / 45 / 100 / 220).");
            v.IntList("levels_to_advance", ref LevelsToAdvance, 0, 1_000, 3, 3, "Build gate: total facility levels needed to leave tier 1 / 2 / 3.");
            v.IntList("net_energy_to_advance", ref NetEnergyToAdvance, -100_000, 100_000, 3, 3, "Build gate: net energy per hour needed to leave tier 1 / 2 / 3.");
            v.IntList("people_cost_base", ref PeopleCostBase, 0, 1_000, 3, 3, "Human cost: people who leave to expand, before the size term.");
            v.Int("people_cost_per_levels", ref PeopleCostPerLevels, 1, 1_000, "Human cost: one more person per this many total facility levels.");
            v.EndSection();
        }
    }
}
