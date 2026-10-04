namespace Deadswitch.Sim.Config
{
    /// <summary>A living world (SPEC-017, doc 05 s3-4+s7, doc 10 s4-5): ultimatum, dilemmas, trade, world events. Placeholders: tune at F-099.</summary>
    public sealed class LivingConfig : IConfigSection
    {
        public int UltimatumDay = 10;
        public int UltimatumHours = 24;
        public int UltimatumEnergy = 400;
        public int UltimatumFuel = 100;
        public int UltimatumStrengthPct = 250;
        public int UltimatumPaidHeatDrop = 10_000;
        public int DilemmaFirstHour = 36;
        public int DilemmaEveryHours = 30;
        public int DilemmaJitterHours = 12;
        public int DilemmaExpireHours = 12;
        public int TraderFuel = 80;
        public int TraderEnergy = 300;
        public int TraderTrapPct = 25;
        public int RefugeePeople = 3;
        public int RefugeeSpyPct = 30;
        public int RefugeeSpyHeat = 15_000;
        public int ShortcutEnergy = 400;
        public int ShortcutCompute = 40;
        public int ShortcutCorruption = 4_000;
        public int DeserterPeople = 2;
        public int DeserterHeat = 12_000;
        public int DeserterReturnHeatDrop = 10_000;
        public int ChurchCleanData = 5_000;
        public int ChurchTaintPct = 30;
        public int ChurchTaintCorruption = 6_000;
        public int TradeFuelLot = 20;
        public int TradeFuelPrice = 100;
        public int TradeEnergyLot = 200;
        public int TradeEnergyPrice = 40;
        public int TradeComputeLot = 20;
        public int TradeComputePrice = 150;
        public int[] TradePricePctByLevel = { 80, 100, 150 };
        public int TradesPerDay = 3;
        public int WorldEventFirstDay = 2;
        public int WorldEventEveryHours = 72;
        public int WorldEventHours = 24;
        public int SignalStormOddsPts = 20;
        public int SignalStormCorruptionMilli = 150;
        public int SupplyWindowLootPct = 50;
        public int SupplyWindowPricePct = 25;
        public int DeadWeekIntervalPct = 100;
        public int FactionWarHeat = 10_000;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("living", "Living world: Warlord Ultimatum, dilemmas, trade, world events (doc 05, doc 10 s4-5). Heat and corruption are milli. All (tune).");
            v.Int("ultimatum_day", ref UltimatumDay, 1, 1_000, "Still in Tier 1 on this day: the Warlord Ultimatum arrives (doc 10: about 10).");
            v.Int("ultimatum_hours", ref UltimatumHours, 1, 1_000, "Hours to pay before the wave comes.");
            v.Int("ultimatum_energy", ref UltimatumEnergy, 0, 100_000, "Ultimatum price: energy.");
            v.Int("ultimatum_fuel", ref UltimatumFuel, 0, 100_000, "Ultimatum price: fuel.");
            v.Int("ultimatum_strength_pct", ref UltimatumStrengthPct, 1, 10_000, "Warlord wave strength as % of a normal raid.");
            v.Int("ultimatum_paid_heat_drop", ref UltimatumPaidHeatDrop, 0, 100_000, "Rustborn heat a paid ultimatum takes off.");
            v.Int("dilemma_first_hour", ref DilemmaFirstHour, 1, 10_000, "Hour of the first dilemma.");
            v.Int("dilemma_every_hours", ref DilemmaEveryHours, 1, 10_000, "Hours between dilemmas (plus jitter).");
            v.Int("dilemma_jitter_hours", ref DilemmaJitterHours, 0, 10_000, "Up to this many extra hours between dilemmas.");
            v.Int("dilemma_expire_hours", ref DilemmaExpireHours, 1, 1_000, "An unanswered dilemma is refused after this long.");
            v.Int("trader_fuel", ref TraderFuel, 0, 10_000, "Trader's offer: fuel you get.");
            v.Int("trader_energy", ref TraderEnergy, 0, 100_000, "Trader's offer: energy you pay.");
            v.Int("trader_trap_pct", ref TraderTrapPct, 0, 100, "Chance the trader's offer is a trap (paid, nothing back).");
            v.Int("refugee_people", ref RefugeePeople, 0, 100, "Refugees taken in.");
            v.Int("refugee_spy_pct", ref RefugeeSpyPct, 0, 100, "Chance a spy is among them.");
            v.Int("refugee_spy_heat", ref RefugeeSpyHeat, 0, 100_000, "Heat a spy adds to the hottest faction.");
            v.Int("shortcut_energy", ref ShortcutEnergy, 0, 100_000, "AI shortcut: energy now.");
            v.Int("shortcut_compute", ref ShortcutCompute, 0, 100_000, "AI shortcut: compute now.");
            v.Int("shortcut_corruption", ref ShortcutCorruption, 0, 100_000, "AI shortcut: corruption it costs.");
            v.Int("deserter_people", ref DeserterPeople, 0, 100, "Vanguard deserters taken in.");
            v.Int("deserter_heat", ref DeserterHeat, 0, 100_000, "Vanguard heat for sheltering deserters.");
            v.Int("deserter_return_heat_drop", ref DeserterReturnHeatDrop, 0, 100_000, "Vanguard heat taken off for handing them back.");
            v.Int("church_clean_data", ref ChurchCleanData, 0, 100_000, "Church signal: corruption a clean broadcast scrubs.");
            v.Int("church_taint_pct", ref ChurchTaintPct, 0, 100, "Chance the broadcast hides something.");
            v.Int("church_taint_corruption", ref ChurchTaintCorruption, 0, 100_000, "Corruption a tainted broadcast adds.");
            v.Int("trade_fuel_lot", ref TradeFuelLot, 1, 10_000, "Fuel per trade.");
            v.Int("trade_fuel_price", ref TradeFuelPrice, 0, 100_000, "Base energy price of a fuel lot.");
            v.Int("trade_energy_lot", ref TradeEnergyLot, 1, 100_000, "Energy per trade (energy cells).");
            v.Int("trade_energy_price", ref TradeEnergyPrice, 0, 10_000, "Base fuel price of an energy lot.");
            v.Int("trade_compute_lot", ref TradeComputeLot, 1, 10_000, "Compute per trade.");
            v.Int("trade_compute_price", ref TradeComputePrice, 0, 100_000, "Base energy price of a compute lot.");
            v.IntList("trade_price_pct_by_level", ref TradePricePctByLevel, 1, 1_000, 3, 3, "Price % by the seller's heat level (Cold, Watched, Hunted; Marked will not trade). doc 10: 0.8-1.5.");
            v.Int("trades_per_day", ref TradesPerDay, 0, 100, "Trades per faction per day.");
            v.Int("world_event_first_day", ref WorldEventFirstDay, 1, 1_000, "Day of the first world event.");
            v.Int("world_event_every_hours", ref WorldEventEveryHours, 1, 10_000, "Hours from one world event's start to the next.");
            v.Int("world_event_hours", ref WorldEventHours, 1, 10_000, "How long a world event lasts.");
            v.Int("signal_storm_odds_pts", ref SignalStormOddsPts, 0, 100, "Signal Storm: hack odds minus this.");
            v.Int("signal_storm_corruption_milli", ref SignalStormCorruptionMilli, 0, 100_000, "Signal Storm: extra corruption per hour.");
            v.Int("supply_window_loot_pct", ref SupplyWindowLootPct, 0, 1_000, "Supply Window: operation loot +%.");
            v.Int("supply_window_price_pct", ref SupplyWindowPricePct, 0, 99, "Supply Window: trade prices -%.");
            v.Int("dead_week_interval_pct", ref DeadWeekIntervalPct, 0, 1_000, "Dead Week: time between attacks +%.");
            v.Int("faction_war_heat", ref FactionWarHeat, 0, 100_000, "Factions fight each other: at each world event the hottest cools by this, a rival warms by half.");
            v.EndSection();
        }
    }
}
