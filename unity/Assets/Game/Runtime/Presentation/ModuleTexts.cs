using Deadswitch.Sim;
using Deadswitch.Sim.State;

namespace Deadswitch.Game.Presentation
{
    /// <summary>Module names and effect lines (SPEC-008), with numbers read from the balance.</summary>
    public static class ModuleTexts
    {
        public static string Name(ModuleNode node)
        {
            switch (node)
            {
                case ModuleNode.M1: return "MEMORY SECTOR 1";
                case ModuleNode.M2: return "MEMORY SECTOR 2";
                case ModuleNode.M3: return "MEMORY SECTOR 3";
                case ModuleNode.LG1: return "LOAD BALANCING";
                case ModuleNode.LG2A: return "OVERCLOCKED RACKS";
                case ModuleNode.LG2B: return "DEEP CELLS";
                case ModuleNode.LG3: return "HABITAT MGMT";
                case ModuleNode.LG4: return "PREFAB ASSEMBLY";
                case ModuleNode.LG5A: return "SALVAGE DOCTRINE";
                case ModuleNode.LG5B: return "FUEL CELLS";
                case ModuleNode.LG6: return "AUTOMATION PROTOCOLS";
                default: return "UNKNOWN";
            }
        }

        public static string Effect(ModuleNode node, SimConfig c)
        {
            var m = c.Modules;
            switch (node)
            {
                case ModuleNode.M1: return "Restores a memory sector. Required to advance to Tier 2.";
                case ModuleNode.M2: return "Restores a memory sector. Required to advance to Tier 3.";
                case ModuleNode.M3: return "Restores a memory sector. Required to advance to Tier 4.";
                case ModuleNode.LG1: return "Facility upkeep -" + m.LoadBalancingPct + "% (generators excluded).";
                case ModuleNode.LG2A: return "Server Rack output +" + m.OverclockPct + "%. Excludes Deep Cells.";
                case ModuleNode.LG2B: return "Battery Bank capacity +" + m.DeepCellsPct + "%. Excludes Overclocked Racks.";
                case ModuleNode.LG3: return "Population cap +" + m.HabitatPop + ".";
                case ModuleNode.LG4: return "Construction time -" + m.PrefabPct + "%.";
                case ModuleNode.LG5A: return "Cancel and demolish refunds +" + m.SalvagePts + " points. Excludes Fuel Cells.";
                case ModuleNode.LG5B: return "Generator output +" + m.FuelCellsPct + "%. Excludes Salvage Doctrine.";
                case ModuleNode.LG6: return "Unmanned facilities run at " + m.AutomationUnmannedPct + "% instead of " + c.Crew.UnmannedOutputPct + "%.";
                default: return string.Empty;
            }
        }

        public static string TierName(int tier)
        {
            switch (tier)
            {
                case 1: return "BUNKER";
                case 2: return "DISTRICT";
                case 3: return "STRONGHOLD";
                default: return "SECTOR";
            }
        }
    }
}
