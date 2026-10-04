namespace Deadswitch.Sim.Config
{
    /// <summary>Temporary pacts (SPEC-023, doc 05 s3). Placeholders: tune with play data.</summary>
    public sealed class DiplomacyConfig : IConfigSection
    {
        public int CeasefireDays = 3;
        public int CeasefireEnergy = 400;
        public int CeasefireFuel = 60;
        public int[] PricePctByLevel = { 80, 100, 160 };
        public int CooldownDays = 5;
        public int BreakHeat = 30_000;
        public int TradeDiscountPct = 20;
        public int AllianceEnergy = 600;
        public int AllianceFuel = 100;
        public int AllianceUpkeepEnergy = 120;
        public int[] AllianceDefenseByTier = { 40, 80, 140, 220 };
        public int AllianceRivalHeat = 3_000;
        public int AllianceWalkoutPct = 5;
        public int AllianceEndHeat = 5_000;
        public int AllianceBetrayHeat = 40_000;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("diplomacy", "Temporary pacts: a paid ceasefire and an alliance, one of each at a time, that either side can break (doc 05 s3). Heat is milli. All (tune).");
            v.Int("ceasefire_days", ref CeasefireDays, 1, 100, "How long a ceasefire holds.");
            v.Int("ceasefire_energy", ref CeasefireEnergy, 0, 100_000, "Ceasefire price: energy (before the heat multiplier).");
            v.Int("ceasefire_fuel", ref CeasefireFuel, 0, 10_000, "Ceasefire price: fuel (before the heat multiplier).");
            v.IntList("price_pct_by_level", ref PricePctByLevel, 1, 10_000, 3, 3, "Price % by that faction's heat level (Cold, Watched, Hunted); a Marked faction will not talk.");
            v.Int("cooldown_days", ref CooldownDays, 0, 100, "Days after a ceasefire ends before any new one.");
            v.Int("break_heat", ref BreakHeat, 0, 100_000, "Heat added when the Hub breaks a ceasefire by striking that faction.");
            v.Int("trade_discount_pct", ref TradeDiscountPct, 0, 99, "Trade with a faction under ceasefire or alliance is this much cheaper.");
            v.Int("alliance_energy", ref AllianceEnergy, 0, 100_000, "Alliance price: energy (only with a Cold faction).");
            v.Int("alliance_fuel", ref AllianceFuel, 0, 10_000, "Alliance price: fuel.");
            v.Int("alliance_upkeep_energy", ref AllianceUpkeepEnergy, 0, 100_000, "Energy the ally takes each day; unpaid, the alliance lapses.");
            v.IntList("alliance_defense_by_tier", ref AllianceDefenseByTier, 0, 100_000, 4, 4, "Defense the ally's fighters add to the wall, by tier.");
            v.Int("alliance_rival_heat", ref AllianceRivalHeat, 0, 100_000, "Heat per day with the ally's rival (Rustborn and Vanguard, Church and Halcyon).");
            v.Int("alliance_walkout_pct", ref AllianceWalkoutPct, 0, 100, "Chance per day that the ally walks out on its own.");
            v.Int("alliance_end_heat", ref AllianceEndHeat, 0, 100_000, "Heat with the ally when the Hub dissolves the alliance or cannot pay.");
            v.Int("alliance_betray_heat", ref AllianceBetrayHeat, 0, 100_000, "Heat with the ally when the Hub strikes its sites.");
            v.EndSection();
        }
    }
}
