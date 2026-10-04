namespace Deadswitch.Sim.Config
{
    /// <summary>Battle scars (SPEC-018, doc 04 s4, doc 06 s3): damage that stays until repaired. Placeholders: tune at F-099.</summary>
    public sealed class ScarConfig : IConfigSection
    {
        public int MaxDamage = 3;
        public int OutputPctPerDamage = 15;
        public int HeavyBreachPermille = 500;
        public int RaidDamage = 1;
        public int SiegeDamage = 2;
        public int PurgeDamage = 3;
        public int SiegeWreckage = 2;
        public int PurgeWreckage = 3;
        public int MaxWreckage = 6;
        public int RepelWreckage = 1;
        public int BreachWreckage = 1;
        public int RegrowthPctPerWreck = 10;
        public int RepairEnergyPerPoint = 40;
        public int RepairMinutesPerPoint = 60;
        public int ClearEnergyPerWreck = 20;
        public int BurnHours = 6;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("scars", "Battle scars: facility damage and yard wreckage that stay until repaired (doc 04 s4, doc 06 s3). All (tune).");
            v.Int("max_damage", ref MaxDamage, 1, 10, "Damage points a facility can hold.");
            v.Int("output_pct_per_damage", ref OutputPctPerDamage, 0, 100, "Output lost per damage point (Generator, Server Rack, Turret).");
            v.Int("heavy_breach_permille", ref HeavyBreachPermille, 0, 1_000, "A raid breach at least this deep damages a facility (lighter ones only leave wrecks).");
            v.Int("raid_damage", ref RaidDamage, 0, 10, "Damage a deep raid breach deals.");
            v.Int("siege_damage", ref SiegeDamage, 0, 10, "Damage a breached siege or Warlord wave deals.");
            v.Int("purge_damage", ref PurgeDamage, 0, 10, "Damage a breached purge deals.");
            v.Int("siege_wreckage", ref SiegeWreckage, 0, 10, "Wrecks a breached siege or Warlord wave leaves.");
            v.Int("purge_wreckage", ref PurgeWreckage, 0, 10, "Wrecks a breached purge leaves.");
            v.Int("max_wreckage", ref MaxWreckage, 0, 20, "Wrecks the yard can hold.");
            v.Int("repel_wreckage", ref RepelWreckage, 0, 10, "Wrecks a repelled attack leaves (enemy hulks).");
            v.Int("breach_wreckage", ref BreachWreckage, 0, 10, "Wrecks a breached raid leaves.");
            v.Int("regrowth_pct_per_wreck", ref RegrowthPctPerWreck, 0, 100, "Regrowth lost per wreck in the yard.");
            v.Int("repair_energy_per_point", ref RepairEnergyPerPoint, 0, 100_000, "Repair cost: energy x level x damage.");
            v.Int("repair_minutes_per_point", ref RepairMinutesPerPoint, 1, 10_000, "Repair time per damage point (the damage holds until it finishes).");
            v.Int("clear_energy_per_wreck", ref ClearEnergyPerWreck, 0, 10_000, "Energy to clear one wreck from the yard.");
            v.Int("burn_hours", ref BurnHours, 0, 1_000, "Fresh wrecks burn this long after an attack (visual).");
            v.EndSection();
        }
    }
}
