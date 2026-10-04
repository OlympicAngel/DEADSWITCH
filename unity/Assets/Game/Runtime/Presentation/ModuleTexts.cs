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
                case ModuleNode.WF1: return "FIRE CONTROL";
                case ModuleNode.WF2A: return "DRILLED MILITIA";
                case ModuleNode.WF2B: return "KILL ZONES";
                case ModuleNode.WF3: return "COMBAT SIMULATIONS";
                case ModuleNode.WF4: return "HARDENED WALLS";
                case ModuleNode.WF5A: return "ASSAULT DOCTRINE";
                case ModuleNode.WF5B: return "RAPID RESPONSE";
                case ModuleNode.WF6: return "VETERAN CADRE";
                case ModuleNode.CY1: return "FIREWALL";
                case ModuleNode.CY2A: return "COUNTER-INTRUSION";
                case ModuleNode.CY2B: return "SIGNAL INTERCEPTION";
                case ModuleNode.CY3: return "CLEAN ROOM";
                case ModuleNode.CY4: return "BLACK ICE";
                case ModuleNode.CY5A: return "WORM FACTORY";
                case ModuleNode.CY5B: return "GHOST PROTOCOL";
                case ModuleNode.CY6: return "CORE PARTITION";
                case ModuleNode.ST1: return "SIGNAL MASKING";
                case ModuleNode.ST2A: return "LONG-RANGE EYES";
                case ModuleNode.ST2B: return "QUIET ROUTES";
                case ModuleNode.ST3: return "HEAT SINK";
                case ModuleNode.ST4: return "DECOYS";
                case ModuleNode.ST5A: return "PREDICTION ENGINE";
                case ModuleNode.ST5B: return "FALSE TRAILS";
                case ModuleNode.ST6: return "GHOST NETWORK";
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
                case ModuleNode.WF1: return "Turret output +" + m.FireControlPct + "%.";
                case ModuleNode.WF2A: return "Each defender adds +" + m.MilitiaPerDefender + " defense. Excludes Kill Zones.";
                case ModuleNode.WF2B: return "Turtle posture bonus +" + m.KillZonePts + " points. Excludes Drilled Militia.";
                case ModuleNode.WF3: return "Raid odds +" + m.CombatSimsPts + " points.";
                case ModuleNode.WF4: return "Every attack except raids lands " + m.HardenedPct + "% weaker.";
                case ModuleNode.WF5A: return "Raid casualties -" + m.AssaultCasualtyPct + "%. Excludes Rapid Response.";
                case ModuleNode.WF5B: return "Attack warnings +" + m.RapidResponseMinutes + " min. Excludes Assault Doctrine.";
                case ModuleNode.WF6: return "Each person on an operation counts +" + m.VeteranStrength + " strength.";
                case ModuleNode.CY1: return "Virus strength -" + m.FirewallPct + "%.";
                case ModuleNode.CY2A: return "Hack odds +" + m.CounterIntrusionPts + " points. Excludes Signal Interception.";
                case ModuleNode.CY2B: return "Attack strength estimate error -" + m.InterceptionPct + "%. Excludes Counter-intrusion.";
                case ModuleNode.CY3: return "Corruption fades " + Fmt.Milli(m.CleanRoomMilli) + "% faster per hour.";
                case ModuleNode.CY4: return "A virus that gets through locks no module.";
                case ModuleNode.CY5A: return "Hacks bring back +" + m.WormPct + "% compute. Excludes Ghost Protocol.";
                case ModuleNode.CY5B: return "Hacks raise no heat. Excludes Worm Factory.";
                case ModuleNode.CY6: return "Corruption settles lower: +" + Fmt.Milli(m.PartitionPermille * 100) + "% of it fades each hour.";
                case ModuleNode.ST1: return "Dark posture: attacks miss the Hub +" + m.MaskingPts + " points more often.";
                case ModuleNode.ST2A: return "Site defense is known without scouting. Excludes Quiet Routes.";
                case ModuleNode.ST2B: return "Operations use " + m.QuietRoutesPct + "% less fuel. Excludes Long-range Eyes.";
                case ModuleNode.ST3: return "Faction heat fades +" + m.HeatSinkPct + "% faster.";
                case ModuleNode.ST4: return "Every attack lands " + m.DecoyPct + "% weaker.";
                case ModuleNode.ST5A: return "Attack warnings +" + m.PredictionMinutes + " min. Excludes False Trails.";
                case ModuleNode.ST5B: return "Operations raise " + m.FalseTrailsPct + "% less heat. Excludes Prediction Engine.";
                case ModuleNode.ST6: return "+" + m.GhostOps + " operation slot.";
                default: return string.Empty;
            }
        }

        public static string FieldName(ModuleField field)
        {
            switch (field)
            {
                case ModuleField.Logistics: return "LOGISTICS";
                case ModuleField.Warfare: return "WARFARE";
                case ModuleField.Cyber: return "CYBER";
                case ModuleField.Stealth: return "STEALTH";
                default: return "TRUNK";
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
