using Deadswitch.Sim;
using Deadswitch.Sim.State;

namespace Deadswitch.Game.Presentation
{
    /// <summary>Dilemma, world event and trade wording (F-034), with numbers read from the balance. Every risk is stated up front.</summary>
    public static class LivingTexts
    {
        public static string DilemmaName(DilemmaKind kind)
        {
            switch (kind)
            {
                case DilemmaKind.Trader: return "A TRADER AT THE GATE";
                case DilemmaKind.Refugees: return "REFUGEES AT THE GATE";
                case DilemmaKind.Shortcut: return "THE AI'S SHORTCUT";
                case DilemmaKind.Deserters: return "VANGUARD DESERTERS";
                case DilemmaKind.ChurchSignal: return "A SIGNAL FROM THE CHURCH";
                default: return string.Empty;
            }
        }

        public static string DilemmaText(DilemmaKind kind)
        {
            switch (kind)
            {
                case DilemmaKind.Trader: return "A lone trader with a fuel truck. His price is good. Too good, maybe. I cannot vouch for him.";
                case DilemmaKind.Refugees: return "People outside the north gate. Hands we need. One of them may not be what they seem.";
                case DilemmaKind.Shortcut: return "I found a way to pull power and compute out of the old grid. It costs me some clarity. Let me.";
                case DilemmaKind.Deserters: return "Soldiers who walked away from Colonel Vale. Trained, armed, hunted. Vale wants them back.";
                case DilemmaKind.ChurchSignal: return "The Church is broadcasting clean archives on our band. Or something wearing them.";
                default: return string.Empty;
            }
        }

        /// <summary>Label and effect line for choice 0 (take) or 1 (refuse).</summary>
        public static (string Label, string Effect) Choice(DilemmaKind kind, int choice, SimConfig c)
        {
            var l = c.Living;
            if (choice == 1)
            {
                switch (kind)
                {
                    case DilemmaKind.Trader: return ("SEND HIM OFF", "NOTHING CHANGES");
                    case DilemmaKind.Refugees: return ("TURN THEM AWAY", "NOTHING CHANGES");
                    case DilemmaKind.Shortcut: return ("NOT LIKE THIS", "NOTHING CHANGES");
                    case DilemmaKind.Deserters: return ("HAND THEM BACK", "VANGUARD HEAT -" + Pct(l.DeserterReturnHeatDrop));
                    default: return ("JAM THE SIGNAL", "NOTHING CHANGES");
                }
            }

            switch (kind)
            {
                case DilemmaKind.Trader: return ("BUY THE FUEL", "-" + l.TraderEnergy + " ENERGY // +" + l.TraderFuel + " FUEL // " + l.TraderTrapPct + "% A TRAP: NOTHING BACK");
                case DilemmaKind.Refugees: return ("TAKE THEM IN", "+" + l.RefugeePeople + " PEOPLE // " + l.RefugeeSpyPct + "% A SPY: HEAT +" + Pct(l.RefugeeSpyHeat) + " ON THE HOTTEST FACTION");
                case DilemmaKind.Shortcut: return ("TAKE THE SHORTCUT", "+" + l.ShortcutEnergy + " ENERGY // +" + l.ShortcutCompute + " COMPUTE // CORRUPTION +" + Fmt.Milli(l.ShortcutCorruption) + "%");
                case DilemmaKind.Deserters: return ("ARM THEM", "+" + l.DeserterPeople + " PEOPLE // VANGUARD HEAT +" + Pct(l.DeserterHeat));
                default: return ("LISTEN", "CORRUPTION -" + Fmt.Milli(l.ChurchCleanData) + "% // " + l.ChurchTaintPct + "% TAINTED: +" + Fmt.Milli(l.ChurchTaintCorruption) + "% INSTEAD");
            }
        }

        public static string EventName(WorldEventKind kind)
        {
            switch (kind)
            {
                case WorldEventKind.SignalStorm: return "SIGNAL STORM";
                case WorldEventKind.SupplyWindow: return "SUPPLY WINDOW";
                case WorldEventKind.DeadWeek: return "DEAD WEEK";
                case WorldEventKind.FalloutWave: return "FALLOUT WAVE";
                case WorldEventKind.PlagueOutbreak: return "PLAGUE OUTBREAK";
                case WorldEventKind.RollingBlackouts: return "ROLLING BLACKOUTS";
                case WorldEventKind.MachineSurge: return "MACHINE SURGE";
                default: return string.Empty;
            }
        }

        public static string EventEffect(WorldEventKind kind, SimConfig c)
        {
            var l = c.Living;
            switch (kind)
            {
                case WorldEventKind.SignalStorm: return "Hack odds -" + l.SignalStormOddsPts + ". Corruption creeps up " + Fmt.Milli(l.SignalStormCorruptionMilli) + "% an hour.";
                case WorldEventKind.SupplyWindow: return "Operations bring back +" + l.SupplyWindowLootPct + "%. Traders sell " + l.SupplyWindowPricePct + "% cheaper.";
                case WorldEventKind.DeadWeek: return "The wastes go quiet. Time between attacks +" + l.DeadWeekIntervalPct + "%.";
                case WorldEventKind.FalloutWave: return "Outposts send " + c.Phases.FalloutOutpostPct + "%. The crater gives up +" + c.Phases.FalloutSalvagePct + "% salvage.";
                case WorldEventKind.PlagueOutbreak: return "No regrowth. Any squad may bring infection home (" + c.Phases.PlagueInfectionPct + "%).";
                case WorldEventKind.RollingBlackouts: return "Generators and the reactor lose " + c.Phases.BlackoutGenerationPct + "% of their output.";
                case WorldEventKind.MachineSurge: return "Attackers bring more drones. Unmanned machines may turn on us.";
                default: return string.Empty;
            }
        }

        public static string Good(TradeGood good)
        {
            switch (good)
            {
                case TradeGood.Fuel: return "FUEL";
                case TradeGood.EnergyCells: return "ENERGY";
                case TradeGood.Blueprints: return "BLUEPRINT";
                default: return "COMPUTE";
            }
        }

        private static string Pct(int heatMilli)
        {
            return (heatMilli / 1000).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
