namespace Deadswitch.Sim.Config
{
    /// <summary>World map: factions and heat, operations, outposts (SPEC-016, doc 04 s8, doc 05, doc 10 s4-5). Placeholders: tune at F-099.</summary>
    public sealed class WorldConfig : IConfigSection
    {
        public int HeatDecayPerHour = 150;
        public int HeatStrengthPermille = 800;
        public int HeatPerRaid = 15_000;
        public int HeatPerHack = 8_000;
        public int HeatPerScout = 2_000;
        public int MaxOps = 2;
        public int FuelPerTravelHour = 4;
        public int StrengthPerPerson = 6;
        public int RaidCooldownHours = 24;
        public int EstimateErrorPct = 40;
        public int CasualtyPctWin = 10;
        public int CasualtyPctLoss = 50;
        public int OutpostClaimEnergy = 300;
        public int OutpostEnergyPerHour = 60;
        public int OutpostFuelPerHour = 6;
        public int OutpostLossPctPerHour = 2;
        public int SeizeEnergy = 600;
        public int SeizeHeat = 20_000;
        public int SabotageMaxSquad = 3;
        public int SabotageHours = 48;
        public int SabotageStrengthPct = 30;
        public int SabotageTracePct = 35;
        public int SabotageSpyPts = 20;
        public int HeldEnergyPerHour = 120;
        public int HeldFuelPerHour = 15;
        public int HeldLossPctPerHour = 4;
        public int AttackerWeightRustborn = 40;
        public int AttackerWeightVanguardTier2 = 40;
        public int AttackerWeightChurch = 10;
        public int AttackerWeightHoldoutsTier3 = 30;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("world", "World map: faction heat, operations and outposts (doc 04 s8, doc 05, doc 10). Heat is milli: 100000 = 100. All (tune).");
            v.Int("heat_decay_per_hour", ref HeatDecayPerHour, 0, 100_000, "Heat each faction loses per game hour.");
            v.Int("heat_strength_permille", ref HeatStrengthPermille, 0, 10_000, "Attack strength x (1 + this/1000 x heat) (doc 10: 0.8).");
            v.Int("heat_per_raid", ref HeatPerRaid, 0, 100_000, "Heat a raid adds to the site's owner.");
            v.Int("heat_per_hack", ref HeatPerHack, 0, 100_000, "Heat a hack adds to the site's owner.");
            v.Int("heat_per_scout", ref HeatPerScout, 0, 100_000, "Heat a scouting run adds to the site's owner.");
            v.Int("max_ops", ref MaxOps, 1, 10, "Operations in the field at once.");
            v.Int("fuel_per_travel_hour", ref FuelPerTravelHour, 0, 1_000, "Fuel per hour of travel (out and back) for a scout or raid.");
            v.Int("strength_per_person", ref StrengthPerPerson, 1, 1_000, "Squad strength per person sent.");
            v.Int("raid_cooldown_hours", ref RaidCooldownHours, 0, 1_000, "A raided site has nothing to take for this long.");
            v.Int("estimate_error_pct", ref EstimateErrorPct, 0, 100, "The AI's site defense estimate is off by up to this much until scouted.");
            v.Int("casualty_pct_win", ref CasualtyPctWin, 0, 100, "Share of a squad lost on a won raid.");
            v.Int("casualty_pct_loss", ref CasualtyPctLoss, 0, 100, "Share of a squad lost on a failed raid.");
            v.Int("outpost_claim_energy", ref OutpostClaimEnergy, 0, 100_000, "Energy to set up an outpost on cleared ruins.");
            v.Int("outpost_energy_per_hour", ref OutpostEnergyPerHour, 0, 10_000, "Energy an outpost sends home per hour.");
            v.Int("outpost_fuel_per_hour", ref OutpostFuelPerHour, 0, 10_000, "Fuel an outpost sends home per hour.");
            v.Int("outpost_loss_pct_per_hour", ref OutpostLossPctPerHour, 0, 100, "Hourly chance a Hunted-or-worse faction takes an outpost back.");
            v.Int("seize_energy", ref SeizeEnergy, 0, 100_000, "Conquer and hold: energy to garrison a raided faction outpost as ours.");
            v.Int("seize_heat", ref SeizeHeat, 0, 100_000, "Heat a seized outpost adds to its old owner.");
            v.Int("sabotage_max_squad", ref SabotageMaxSquad, 1, 50, "Sabotage is a small elite team: at most this many people.");
            v.Int("sabotage_hours", ref SabotageHours, 1, 10_000, "A successful sabotage cripples that faction's attacks for this long.");
            v.Int("sabotage_strength_pct", ref SabotageStrengthPct, 0, 90, "Strength taken off that faction's attacks launched while crippled.");
            v.Int("sabotage_trace_pct", ref SabotageTracePct, 0, 100, "Chance a successful sabotage is traced back to the Hub (raid heat instead of scout heat).");
            v.Int("sabotage_spy_pts", ref SabotageSpyPts, 0, 100, "Odds points from a loyal agent inside the target's faction.");
            v.Int("held_energy_per_hour", ref HeldEnergyPerHour, 0, 10_000, "Energy a held (seized) outpost sends home per hour.");
            v.Int("held_fuel_per_hour", ref HeldFuelPerHour, 0, 10_000, "Fuel a held outpost sends home per hour.");
            v.Int("held_loss_pct_per_hour", ref HeldLossPctPerHour, 0, 100, "Hourly chance the old owner (Watched or worse) takes a held outpost back.");
            v.Int("attacker_weight_rustborn", ref AttackerWeightRustborn, 0, 1_000, "Base weight for the Rustborn as attacker (plus heat %).");
            v.Int("attacker_weight_vanguard_tier2", ref AttackerWeightVanguardTier2, 0, 1_000, "Base weight for Vanguard Command from Tier 2 (plus heat %).");
            v.Int("attacker_weight_church", ref AttackerWeightChurch, 0, 1_000, "Base weight for the Church of the Last Signal after M1 (plus heat %).");
            v.Int("attacker_weight_holdouts_tier3", ref AttackerWeightHoldoutsTier3, 0, 1_000, "Base weight for Halcyon Dynamics from Tier 3 (plus heat %).");
            v.EndSection();
        }
    }
}
