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

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("diplomacy", "Temporary pacts: a paid ceasefire with one faction at a time, broken by striking its sites (doc 05 s3). Heat is milli. All (tune).");
            v.Int("ceasefire_days", ref CeasefireDays, 1, 100, "How long a ceasefire holds.");
            v.Int("ceasefire_energy", ref CeasefireEnergy, 0, 100_000, "Ceasefire price: energy (before the heat multiplier).");
            v.Int("ceasefire_fuel", ref CeasefireFuel, 0, 10_000, "Ceasefire price: fuel (before the heat multiplier).");
            v.IntList("price_pct_by_level", ref PricePctByLevel, 1, 10_000, 3, 3, "Price % by that faction's heat level (Cold, Watched, Hunted); a Marked faction will not talk.");
            v.Int("cooldown_days", ref CooldownDays, 0, 100, "Days after a ceasefire ends before any new one.");
            v.Int("break_heat", ref BreakHeat, 0, 100_000, "Heat added when the Hub breaks a ceasefire by striking that faction.");
            v.Int("trade_discount_pct", ref TradeDiscountPct, 0, 99, "Trade with a faction under ceasefire is this much cheaper.");
            v.EndSection();
        }
    }
}
