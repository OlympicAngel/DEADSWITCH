namespace Deadswitch.Sim.Config
{
    /// <summary>Hub tiers and the tier-up gates (doc 06 s2, doc 10 s1.3, SPEC-008). Index 0 = advancing from Tier 1.</summary>
    public sealed class TierConfig : IConfigSection
    {
        public int MaxTier = 4;
        public int[] PopBonus = { 0, 25, 80, 200 };
        public int[] LevelsToAdvance = { 10, 25, 45 };
        public int[] NetEnergyToAdvance = { 100, 300, 600 };
        public int[] PeopleCostBase = { 4, 10, 20 };
        public int PeopleCostPerLevels = 4;
        public int[] SlotsAdded = { 4, 4, 4 };
        public int[] RaidStrengthPct = { 100, 135, 175, 225 };
        public int[] ExtraRaidsPerDay = { 0, 1, 1, 2 };

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("tier", "Hub tiers and tier-up gates (doc 06 s2, SPEC-008). All (tune).");
            v.Int("max_tier", ref MaxTier, 1, 4, "Highest tier reachable in this build (doc 10: Bunker, District; Stronghold and Sector in v1.x).");
            v.IntList("pop_bonus", ref PopBonus, 0, 10_000, 4, 4, "Base population cap bonus per tier over people.cap (doc 10: 20 / 45 / 100 / 220).");
            v.IntList("levels_to_advance", ref LevelsToAdvance, 0, 1_000, 3, 3, "Build gate: total facility levels needed to leave tier 1 / 2 / 3.");
            v.IntList("net_energy_to_advance", ref NetEnergyToAdvance, -100_000, 100_000, 3, 3, "Build gate: net energy per hour needed to leave tier 1 / 2 / 3.");
            v.IntList("people_cost_base", ref PeopleCostBase, 0, 1_000, 3, 3, "Human cost: people who leave to expand, before the size term.");
            v.Int("people_cost_per_levels", ref PeopleCostPerLevels, 1, 1_000, "Human cost: one more person per this many total facility levels.");
            v.IntList("slots_added", ref SlotsAdded, 0, 32, 3, 3, "Empty plots added when leaving tier 1 / 2 / 3 (SPEC-013: the district outside the gate).");
            v.IntList("raid_strength_pct", ref RaidStrengthPct, 1, 10_000, 4, 4, "Raid strength scale at tier 1 / 2 / 3 / 4 (percent).");
            v.IntList("extra_raids_per_day", ref ExtraRaidsPerDay, 0, 100, 4, 4, "Added to raid.max_per_day at tier 1 / 2 / 3 / 4 (doc 10 s4: 3 in Tier 1, 4 in Tier 2).");
            v.EndSection();
        }
    }
}
