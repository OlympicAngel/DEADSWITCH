namespace Deadswitch.Sim.Config
{
    /// <summary>Relocation, forced reboot and legacy (SPEC-022, doc 06 s4, doc 10 s1.2 + s6). Placeholders: tune later.</summary>
    public sealed class LegacyConfig : IConfigSection
    {
        public int RelocateMinTier = 2;
        public int ScorePerTier = 100;
        public int PowerDivisor = 10;
        public int ScorePerVeteran = 20;
        public int ScorePerMastery = 50;
        public int ForcedBonusPct = 50;
        public int VoluntaryBonusPct = 100;
        public int BaseVeterans = 2;
        public int VeteranPerPeople = 10;
        public int KeptModules = 2;
        public int HeatKeptPct = 50;
        public int ForcedCorruptionKeptPct = 50;
        public int CollapseHours = 24;
        public int TakeoverCriticalHours = 48;
        public int PerkCost = 100;
        public int PerkMaxLevel = 3;
        public int PerkStartEnergy = 150;
        public int PerkStartFuel = 30;
        public int PerkRegrowthPct = 15;
        public int PerkHeatDecayPct = 25;
        public int MasteryCorruptionMilli = 40_000;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("legacy", "Relocation and reboot cycles, legacy score and perks (doc 06 s4, doc 10 s1.2 + s6). All (tune).");
            v.Int("relocate_min_tier", ref RelocateMinTier, 1, 10, "Lowest tier the Hub must have reached to relocate by choice.");
            v.Int("score_per_tier", ref ScorePerTier, 0, 100_000, "Legacy score per highest tier reached (doc 10: 100).");
            v.Int("power_divisor", ref PowerDivisor, 1, 1_000, "Legacy score: peak power rating divided by this (doc 10: 10).");
            v.Int("score_per_veteran", ref ScorePerVeteran, 0, 10_000, "Legacy score per surviving veteran (doc 10: 20).");
            v.Int("score_per_mastery", ref ScorePerMastery, 0, 10_000, "Legacy score per mastery challenge (doc 10: 50).");
            v.Int("forced_bonus_pct", ref ForcedBonusPct, 0, 100, "Share of the score kept as legacy points after a forced reboot (doc 10: 50%).");
            v.Int("voluntary_bonus_pct", ref VoluntaryBonusPct, 0, 200, "Share kept after a voluntary relocation (doc 10: 100%).");
            v.Int("base_veterans", ref BaseVeterans, 0, 20, "Veterans who always follow the core to the next site.");
            v.Int("veteran_per_people", ref VeteranPerPeople, 1, 1_000, "Plus one veteran per this many people at the move.");
            v.Int("kept_modules", ref KeptModules, 0, 32, "Restored field modules that stay unlocked (legacy modules).");
            v.Int("heat_kept_pct", ref HeatKeptPct, 0, 100, "Faction heat carried into the new cycle (faction scars).");
            v.Int("forced_corruption_kept_pct", ref ForcedCorruptionKeptPct, 0, 100, "Corruption kept after a forced reboot (a relocation starts clean).");
            v.Int("collapse_hours", ref CollapseHours, 1, 1_000, "Population collapse: hours at the people floor before a forced reboot.");
            v.Int("takeover_critical_hours", ref TakeoverCriticalHours, 1, 1_000, "Total AI takeover: hours at Critical corruption before a forced reboot.");
            v.Int("perk_cost", ref PerkCost, 1, 100_000, "Legacy points for the next perk level (x level).");
            v.Int("perk_max_level", ref PerkMaxLevel, 1, 10, "Highest level of each perk.");
            v.Int("perk_start_energy", ref PerkStartEnergy, 0, 100_000, "Starting resources perk: energy per level at each new cycle.");
            v.Int("perk_start_fuel", ref PerkStartFuel, 0, 10_000, "Starting resources perk: fuel per level.");
            v.Int("perk_regrowth_pct", ref PerkRegrowthPct, 0, 1_000, "Regrowth perk: population regrowth +% per level.");
            v.Int("perk_heat_decay_pct", ref PerkHeatDecayPct, 0, 1_000, "Heat perk: faction heat fades +% per level.");
            v.Int("mastery_corruption_milli", ref MasteryCorruptionMilli, 0, 100_000, "Mastery: corruption kept under this for a full tier (doc 10: 40).");
            v.EndSection();
        }
    }
}
