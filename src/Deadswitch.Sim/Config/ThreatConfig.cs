namespace Deadswitch.Sim.Config
{
    /// <summary>Threat signatures and protection tools (SPEC-015, doc 04 s3-5, doc 10 s4). All placeholders (tune at F-099).</summary>
    public sealed class ThreatConfig : IConfigSection
    {
        public int SiegeIntervalHours = 48;
        public int SiegeJitterPct = 25;
        public int SiegeWarningMinutes = 120;
        public int SiegeStrengthPct = 140;
        public int SiegeLootPct = 30;
        public int SiegeDamagePerBreachPermille = 400;

        public int VirusIntervalHours = 72;
        public int VirusJitterPct = 25;
        public int VirusStrength = 40;
        public int VirusStrengthPerTier = 20;
        public int VirusCorruption = 8_000;
        public int VirusLockHours = 24;
        public int FalseIntelPct = 50;

        public int PurgeIntervalHours = 168;
        public int PurgeRumorHours = 12;
        public int PurgeStagingHours = 24;
        public int PurgeUltimatumHours = 6;
        public int RumorFalsePct = 30;
        public int PurgeStrikeWarningMinutes = 30;
        public int AmbushPct = 15;
        public int PurgeStrengthPct = 220;
        public int PurgePopulationPct = 15;
        public int PurgeDowngrades = 2;
        public int PurgeTributeEnergy = 800;
        public int PurgeTributeCompute = 60;

        public int ShieldStartCharges = 1;
        public int ShieldMaxCharges = 2;
        public int ShieldRegenDays = 60;
        public int ShieldMaxHours = 72;
        public int ShieldDelayMinutes = 120;
        public int ShieldUpkeepPct = 50;

        public int TributePct = 25;
        public int TributeMinEnergy = 60;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("threats", "Threat signatures and protection tools (SPEC-015, doc 04 s3-5, doc 10 s4). All (tune).");
            v.Int("siege_interval_hours", ref SiegeIntervalHours, 1, 10_000, "Tier 2+: a siege is due this often (doc 10: about every 2 days).");
            v.Int("siege_jitter_pct", ref SiegeJitterPct, 0, 90, "Siege interval varies by up to this share either way.");
            v.Int("siege_warning_minutes", ref SiegeWarningMinutes, 1, 10_000, "Warning before a siege lands (longer than a raid).");
            v.Int("siege_strength_pct", ref SiegeStrengthPct, 1, 10_000, "Siege strength as a share of raid strength.");
            v.Int("siege_loot_pct", ref SiegeLootPct, 0, 1_000, "Siege loot as a share of raid loot (sieges break buildings, not stores).");
            v.Int("siege_damage_per_breach_permille", ref SiegeDamagePerBreachPermille, 1, 1_000, "A breached siege downgrades one facility per this much breach (at least one).");
            v.Int("virus_interval_hours", ref VirusIntervalHours, 1, 10_000, "After M1: a virus is due this often (doc 10: about every 3 days).");
            v.Int("virus_jitter_pct", ref VirusJitterPct, 0, 90, "Virus interval varies by up to this share either way.");
            v.Int("virus_strength", ref VirusStrength, 0, 100_000, "Compute the firewall must burn to stop a virus.");
            v.Int("virus_strength_per_tier", ref VirusStrengthPerTier, 0, 100_000, "Added virus strength per tier above 1.");
            v.Int("virus_corruption", ref VirusCorruption, 0, 100_000, "Corruption added by an infection (milli).");
            v.Int("virus_lock_hours", ref VirusLockHours, 0, 1_000, "An infection locks one restored field module for this long.");
            v.Int("false_intel_pct", ref FalseIntelPct, 1, 1_000, "After an infection the next raid estimate is scaled by this (false intel).");
            v.Int("purge_interval_hours", ref PurgeIntervalHours, 1, 100_000, "Tier 2+: a purge ladder starts this often (rare) until faction heat drives it (F-019).");
            v.Int("purge_rumor_hours", ref PurgeRumorHours, 1, 1_000, "Rumor stage length before staging is confirmed.");
            v.Int("purge_staging_hours", ref PurgeStagingHours, 1, 1_000, "Staging confirmed: hours before the strike (doc 10: 24).");
            v.Int("purge_ultimatum_hours", ref PurgeUltimatumHours, 1, 1_000, "Ultimatum: final hours before the strike (doc 10: 6).");
            v.Int("rumor_false_pct", ref RumorFalsePct, 0, 100, "Chance a purge rumor is wrong and fizzles.");
            v.Int("purge_strike_warning_minutes", ref PurgeStrikeWarningMinutes, 1, 1_000, "Contact warning when the purge force moves in.");
            v.Int("ambush_pct", ref AmbushPct, 0, 100, "Share of raids from a Hunted-or-worse faction that come as ambushes: no warning, no estimate (doc 10 s2).");
            v.Int("purge_strength_pct", ref PurgeStrengthPct, 1, 10_000, "Purge strength as a share of raid strength.");
            v.Int("purge_population_pct", ref PurgePopulationPct, 0, 100, "A full purge breach also kills this share of the population (scaled by breach).");
            v.Int("purge_downgrades", ref PurgeDowngrades, 0, 10, "Facilities downgraded by a breached purge.");
            v.Int("purge_tribute_energy", ref PurgeTributeEnergy, 0, 100_000, "Ultimatum tribute: energy.");
            v.Int("purge_tribute_compute", ref PurgeTributeCompute, 0, 100_000, "Ultimatum tribute: compute.");
            v.Int("shield_start_charges", ref ShieldStartCharges, 0, 10, "Vacation shield charges at the start of a run (doc 10: 1).");
            v.Int("shield_max_charges", ref ShieldMaxCharges, 1, 10, "Most shield charges held at once (doc 10: 2).");
            v.Int("shield_regen_days", ref ShieldRegenDays, 1, 1_000, "Game days per new shield charge (doc 10: 60).");
            v.Int("shield_max_hours", ref ShieldMaxHours, 1, 1_000, "Attack pause per shield (doc 10: 72 h; timers keep running).");
            v.Int("shield_delay_minutes", ref ShieldDelayMinutes, 0, 10_000, "The shield rises this long after activation so it cannot dodge a visible attack (doc 10: 2 h).");
            v.Int("shield_upkeep_pct", ref ShieldUpkeepPct, 0, 100, "Facility upkeep while the shield holds, percent (doc 10: halved).");
            v.Int("tribute_pct", ref TributePct, 0, 100, "Tribute standing order: share of stored energy paid to send a raid away.");
            v.Int("tribute_min_energy", ref TributeMinEnergy, 0, 100_000, "Tribute standing order: minimum payment (no tribute if energy is below it).");
            v.EndSection();
        }
    }
}
