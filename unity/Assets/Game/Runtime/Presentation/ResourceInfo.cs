using System.Collections.Generic;
using Deadswitch.Game.UI;
using Deadswitch.Sim;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Game.Presentation
{
    public enum ResKind
    {
        Energy,
        Compute,
        People,
        Fuel,
    }

    /// <summary>What a resource pod says at a glance (shape + color + word, SPEC-039 idea 12).</summary>
    public enum ResState
    {
        Ok,
        Low,
        Short,
        Full,
    }

    /// <summary>One producer or drain line in a resource breakdown.</summary>
    public sealed class ResLine
    {
        public string Glyph;
        public string Label;
        public int PerHour;
        public int Slot = -1;
    }

    /// <summary>A "how to get more" shortcut: upgrade a slot, build on a plot, or open another screen.</summary>
    public sealed class ResHelp
    {
        public string Glyph;
        public string Title;
        public string Why;

        /// <summary>The facility to build or upgrade (None for a screen shortcut).</summary>
        public FacilityKind Kind;

        /// <summary>Slot to select: an upgrade target or the empty plot to build on; -1 when none is free.</summary>
        public int Slot = -1;

        /// <summary>Screen to open instead of a slot (for example "map" for outposts).</summary>
        public string Screen;
    }

    /// <summary>
    /// The read model behind the resource pods and the breakdown sheet (SPEC-039 B): value, capacity, net rate,
    /// state, time to full or empty, producers, drains and shortcuts that help. Reads the sim; never changes it.
    /// Shows what the Hub's books show (off-the-books drains of hidden structures stay hidden, SPEC-034).
    /// </summary>
    public sealed class ResourceInfo
    {
        public ResKind Kind;
        public int Value;
        public int Cap;
        public int PerHour;
        public bool HasRate;
        public ResState State;
        public string Note = string.Empty;
        public readonly List<ResLine> Sources = new List<ResLine>();
        public readonly List<ResLine> Drains = new List<ResLine>();
        public readonly List<ResHelp> Help = new List<ResHelp>();

        public string Glyph => GlyphOf(Kind);

        public string Name => NameOf(Kind);

        /// <summary>Fill share of capacity (0..1).</summary>
        public float Fill => Cap > 0 ? System.Math.Min(1f, Value / (float)Cap) : 0f;

        /// <summary>Game hours until full (positive rate) or empty (negative rate); -1 when it never gets there.</summary>
        public double Hours
        {
            get
            {
                if (!HasRate || PerHour == 0)
                {
                    return -1;
                }

                return PerHour > 0 ? (Value >= Cap ? -1 : (Cap - Value) / (double)PerHour) : Value / (double)-PerHour;
            }
        }

        public static string GlyphOf(ResKind kind)
        {
            switch (kind)
            {
                case ResKind.Energy: return "bolt";
                case ResKind.Compute: return "chip";
                case ResKind.People: return "people";
                default: return "fuel";
            }
        }

        public static string NameOf(ResKind kind)
        {
            switch (kind)
            {
                case ResKind.Energy: return "ENERGY";
                case ResKind.Compute: return "COMPUTE";
                case ResKind.People: return "PEOPLE";
                default: return "FUEL";
            }
        }

        public static string StateName(ResState state)
        {
            switch (state)
            {
                case ResState.Full: return "FULL";
                case ResState.Low: return "LOW";
                case ResState.Short: return "SHORT";
                default: return string.Empty;
            }
        }

        public static ResourceInfo Of(ResKind kind, GameState s, SimConfig c)
        {
            var info = new ResourceInfo { Kind = kind };
            EconomyFlows f = Economy.Flows(s, c);
            switch (kind)
            {
                case ResKind.Energy:
                    Energy(info, s, c, f);
                    break;
                case ResKind.Compute:
                    Compute(info, s, c, f);
                    break;
                case ResKind.People:
                    People(info, s, c, f);
                    break;
                default:
                    Fuel(info, s, c);
                    break;
            }

            info.State = Classify(info, InterfaceConfig.Current.resources);
            return info;
        }

        /// <summary>Hours until <paramref name="need"/> is on hand at the current rate (0 = now, -1 = never).</summary>
        public double HoursUntil(int need)
        {
            if (Value >= need)
            {
                return 0;
            }

            if (!HasRate || PerHour <= 0 || need > Cap)
            {
                return -1;
            }

            return (need - Value) / (double)PerHour;
        }

        private static ResState Classify(ResourceInfo r, InterfaceConfig.ResourceRules rules)
        {
            if (r.Cap <= 0)
            {
                return ResState.Ok;
            }

            if (r.HasRate && r.PerHour < 0 && r.Hours >= 0 && r.Hours <= rules.shortHours)
            {
                return ResState.Short;
            }

            if (r.Value * 100 >= r.Cap * rules.fullPct && (!r.HasRate || r.PerHour > 0))
            {
                return r.Kind == ResKind.People ? ResState.Ok : ResState.Full;
            }

            return r.Value * 100 < r.Cap * rules.lowPct ? ResState.Low : ResState.Ok;
        }

        private static void Energy(ResourceInfo r, GameState s, SimConfig c, EconomyFlows f)
        {
            r.Value = s.Energy;
            r.Cap = f.EnergyCap;
            r.PerHour = f.NetEnergyPerHour;
            r.HasRate = true;
            for (int i = 0; i < s.Slots.Count; i++)
            {
                FacilitySlot slot = s.Slots[i];
                if (slot.IsEmpty || !slot.Enabled)
                {
                    continue;
                }

                if (Economy.IsSource(slot.Kind))
                {
                    r.Sources.Add(Line(slot, i, Economy.EffectiveOutput(s, c, slot) - Economy.UpkeepPerHour(s, c, slot)));
                }
                else if (slot.Powered && Economy.UpkeepPerHour(s, c, slot) > 0)
                {
                    r.Drains.Add(Line(slot, i, -Economy.UpkeepPerHour(s, c, slot)));
                }
            }

            r.Drains.Add(new ResLine { Glyph = "core", Label = "CORE UPKEEP", PerHour = -f.CoreUpkeepPerHour });
            r.Drains.Sort((a, b) => a.PerHour.CompareTo(b.PerHour));
            r.Sources.Sort((a, b) => b.PerHour.CompareTo(a.PerHour));
            if (s.Blackout)
            {
                r.Note = "Blackout: drains outrun generation. Low-priority facilities are shut down.";
            }
            else if (s.Tick < s.AdCapUntilTick)
            {
                r.Note = "Storage extension active.";
            }

            bool full = r.Value * 100 >= r.Cap * InterfaceConfig.Current.resources.fullPct && r.PerHour > 0;
            if (full)
            {
                AddHelp(r, s, c, FacilityKind.BatteryBank, "battery", "More storage", "Full stores waste every hour of output.");
            }

            AddHelp(r, s, c, FacilityKind.Generator, "bolt", "More generation", "The steady source. Upgrades add the most per plot.");
            AddHelp(r, s, c, FacilityKind.SolarField, "sun", "Daylight power", "Free by day, nothing at night.");
            if (s.Tier >= c.ReactorRules.MinTier)
            {
                AddHelp(r, s, c, FacilityKind.Reactor, "reactor", "Reactor", "Huge output for fuel.");
            }

            if (!full)
            {
                AddHelp(r, s, c, FacilityKind.BatteryBank, "battery", "More storage", "A bigger buffer for nights and raids.");
            }

            ResLine biggest = r.Drains.Count > 0 && r.Drains[0].Slot >= 0 ? r.Drains[0] : null;
            if (r.PerHour < 0 && biggest != null)
            {
                r.Help.Insert(0, new ResHelp { Glyph = "power", Title = "Shed load", Why = biggest.Label + " draws the most (" + Fmt.Num(-biggest.PerHour) + "/h).", Slot = biggest.Slot });
            }
        }

        private static void Compute(ResourceInfo r, GameState s, SimConfig c, EconomyFlows f)
        {
            r.Value = s.Compute;
            r.Cap = c.Compute.Cap;
            r.PerHour = f.ComputePerHour;
            r.HasRate = true;
            for (int i = 0; i < s.Slots.Count; i++)
            {
                FacilitySlot slot = s.Slots[i];
                if (slot.Kind == FacilityKind.ServerRack && Economy.IsRunning(slot))
                {
                    r.Sources.Add(Line(slot, i, Economy.EffectiveOutput(s, c, slot)));
                }
            }

            AddHelp(r, s, c, FacilityKind.ServerRack, "server", "More racks", "Every rack thinks a little more. Heavy use corrupts me.");
            AddHelp(r, s, c, FacilityKind.CoolingTower, "tower", "Cooling", "Less corruption from heavy compute use.");
        }

        private static void People(ResourceInfo r, GameState s, SimConfig c, EconomyFlows f)
        {
            r.Value = s.People;
            r.Cap = f.PopulationCap;
            r.Note = "WORKERS " + f.CrewAssigned + "/" + f.CrewNeeded + (s.AutomationLoad > 0 ? "  //  " + s.AutomationLoad + " RUN BY AI" : string.Empty);
            for (int i = 0; i < s.Slots.Count; i++)
            {
                FacilitySlot slot = s.Slots[i];
                if (slot.Kind == FacilityKind.LifeSupport && Economy.IsRunning(slot))
                {
                    r.Sources.Add(Line(slot, i, Economy.EffectiveOutput(s, c, slot)));
                }
            }

            AddHelp(r, s, c, FacilityKind.LifeSupport, "cross", "Room for more", "Life support raises how many people the Hub holds.");
            r.Help.Add(new ResHelp { Glyph = "people", Title = "Workforce", Why = "Workers, defenders, loyalty and hard choices.", Screen = "workforce" });
        }

        private static void Fuel(ResourceInfo r, GameState s, SimConfig c)
        {
            r.Value = s.Fuel;
            r.Cap = Economy.FuelCap(s, c);
            int burn = 0;
            for (int i = 0; i < s.Slots.Count; i++)
            {
                FacilitySlot slot = s.Slots[i];
                if (slot.Kind == FacilityKind.Reactor && slot.Enabled && slot.Level > 0)
                {
                    int[] table = c.ReactorRules.FuelPerHour;
                    int b = table[System.Math.Min(slot.Level, table.Length) - 1];
                    burn += b;
                    r.Drains.Add(Line(slot, i, -b));
                }
                else if (slot.Kind == FacilityKind.MotorPool && slot.Level > 0 && s.Fuel > 0)
                {
                    int b = c.Units.MotorPoolFuelPerHour[System.Math.Min(slot.Level, c.Units.MotorPoolFuelPerHour.Length) - 1];
                    burn += b;
                    r.Drains.Add(Line(slot, i, -b));
                }
            }

            // income comes from outposts and salvage in lumps: show the burn as the rate only when there is one
            r.PerHour = -burn;
            r.HasRate = burn > 0;
            r.Help.Add(new ResHelp { Glyph = "map", Title = "Outposts and salvage", Why = "Fuel comes from the map: hold outposts, raid and trade.", Screen = "map" });
            AddHelp(r, s, c, FacilityKind.FuelDepot, "fuel", "More storage", "A tank farm holds more fuel.");
        }

        private static ResLine Line(FacilitySlot slot, int index, int perHour)
        {
            return new ResLine { Glyph = Icons.ForFacility(slot.Kind), Label = Fmt.FacilityName(slot.Kind) + " L" + slot.Level, PerHour = perHour, Slot = index };
        }

        /// <summary>Adds the best way to get more from a facility kind: upgrade the lowest one, else build on a free plot.</summary>
        private static void AddHelp(ResourceInfo r, GameState s, SimConfig c, FacilityKind kind, string glyph, string title, string why)
        {
            FacilityConfig f = c.Facility(kind);
            if (f == null)
            {
                return;
            }

            int best = -1;
            for (int i = 0; i < s.Slots.Count; i++)
            {
                FacilitySlot slot = s.Slots[i];
                if (slot.Kind == kind && slot.Level < f.MaxLevel && s.JobForSlot(i) == null && (best < 0 || slot.Level < s.Slots[best].Level))
                {
                    best = i;
                }
            }

            if (best >= 0)
            {
                r.Help.Add(new ResHelp { Glyph = glyph, Title = title, Why = "Upgrade " + Fmt.FacilityName(kind) + " L" + s.Slots[best].Level + ". " + why, Kind = kind, Slot = best });
                return;
            }

            int plot = FreePlot(s);
            r.Help.Add(new ResHelp { Glyph = glyph, Title = title, Why = (plot >= 0 ? "Build a " : "No free plot for a ") + Fmt.FacilityName(kind) + ". " + why, Kind = kind, Slot = plot });
        }

        public static int FreePlot(GameState s)
        {
            for (int i = 0; i < s.Slots.Count; i++)
            {
                if (s.Slots[i].IsEmpty && s.JobForSlot(i) == null)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
