using System.Globalization;
using Deadswitch.Host.Narrative;
using Deadswitch.Sim;
using Deadswitch.Sim.State;

namespace Deadswitch.Game.Presentation
{
    /// <summary>Player-facing names and number formats (one place, terminal register: upper case labels).</summary>
    public static class Fmt
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string FacilityName(FacilityKind kind)
        {
            return Names.Facility(kind);
        }

        public static string FacilityBlurb(FacilityKind kind)
        {
            switch (kind)
            {
                case FacilityKind.Generator: return "Scavenged plant. Everything else runs on what this makes.";
                case FacilityKind.ServerRack: return "Burns power to think. My compute comes from here.";
                case FacilityKind.LifeSupport: return "Air, water, heat. Raises how many people the Hub can hold.";
                case FacilityKind.BatteryBank: return "Stores surplus. A bigger buffer for nights and raids.";
                case FacilityKind.Turret: return "Automated defense. Needs power and a crew to aim well.";
                case FacilityKind.DroneBay: return "Rogue machines, rewired. Drones shred infantry in the open; armour swats them. Unmanned, they are mine.";
                case FacilityKind.MotorPool: return "Armour and gun trucks. Vehicles crush drones; people with charges in the ruins stop them. They drink fuel.";
                case FacilityKind.SolarField: return "Cracked panels on salvaged frames. Free power while the sun is up, nothing at night, and they shatter easily.";
                case FacilityKind.FuelDepot: return "Tank farm. More fuel on hand for raids, vehicles and the reactor.";
                case FacilityKind.CoolingTower: return "Takes the heat off my racks. Heavy thinking corrupts me less.";
                case FacilityKind.MemoryChamber: return "Where I piece myself back together. The memory sectors restore faster.";
                case FacilityKind.Reactor: return "Pre-war fission core. More power than anything else, if we can feed it fuel. Raiders want it. Do not let it crack.";
                default: return "Unused ground inside the perimeter.";
            }
        }

        public static string BandName(CorruptionBand band)
        {
            switch (band)
            {
                case CorruptionBand.Glitchy: return "GLITCHY";
                case CorruptionBand.Unstable: return "UNSTABLE";
                case CorruptionBand.Critical: return "CRITICAL";
                default: return "STABLE";
            }
        }

        public static string PostureName(Posture p)
        {
            switch (p)
            {
                case Posture.Turtle: return "TURTLE";
                case Posture.Dark: return "GO DARK";
                case Posture.Evacuate: return "EVACUATE";
                default: return "NONE";
            }
        }

        /// <summary>DAY 03  14:22 (game clock: tick 0 = day 1, 00:00).</summary>
        public static string Clock(long tick)
        {
            long day = (tick / SimConfig.TicksPerDay) + 1;
            long m = tick % SimConfig.TicksPerDay;
            return "DAY " + day.ToString("00", Inv) + "  " + (m / 60).ToString("00", Inv) + ":" + (m % 60).ToString("00", Inv);
        }

        /// <summary>Countdown from real seconds: 1:05:09, 17:42 or 0:09.</summary>
        public static string Countdown(double seconds)
        {
            long s = seconds <= 0 ? 0 : (long)System.Math.Ceiling(seconds);
            long h = s / 3600;
            long m = (s / 60) % 60;
            long sec = s % 60;
            return h > 0
                ? h.ToString(Inv) + ":" + m.ToString("00", Inv) + ":" + sec.ToString("00", Inv)
                : m.ToString(Inv) + ":" + sec.ToString("00", Inv);
        }

        public static string Signed(long v)
        {
            return (v >= 0 ? "+" : "-") + System.Math.Abs(v).ToString(Inv);
        }

        public static string Num(long v)
        {
            return v.ToString(Inv);
        }

        /// <summary>Compact count for tight readouts: 9,999 stays exact, then 12.4k, 1.2M.</summary>
        public static string Compact(long v)
        {
            long a = System.Math.Abs(v);
            string sign = v < 0 ? "-" : string.Empty;
            if (a < 10000)
            {
                return v.ToString(Inv);
            }

            return a < 1000000 ? sign + (a / 1000.0).ToString(a < 100000 ? "0.0" : "0", Inv) + "k" : sign + (a / 1000000.0).ToString("0.0", Inv) + "M";
        }

        /// <summary>A coarse span from real seconds: 2D 4H, 4H 20M, 35M, &lt;1M.</summary>
        public static string Span(double seconds)
        {
            long m = (long)System.Math.Ceiling(System.Math.Max(0, seconds) / 60.0);
            if (m < 1)
            {
                return "<1M";
            }

            long h = m / 60;
            long d = h / 24;
            if (d > 0)
            {
                return d.ToString(Inv) + "D " + (h % 24).ToString(Inv) + "H";
            }

            return h > 0 ? h.ToString(Inv) + "H " + (m % 60).ToString(Inv) + "M" : m.ToString(Inv) + "M";
        }

        /// <summary>One-unit span for tiny readouts: 2D, 4H, 35M.</summary>
        public static string SpanCoarse(double seconds)
        {
            long m = (long)System.Math.Ceiling(System.Math.Max(0, seconds) / 60.0);
            return m >= 2880 ? (m / 1440).ToString(Inv) + "D" : m >= 60 ? (m / 60).ToString(Inv) + "H" : System.Math.Max(1, m).ToString(Inv) + "M";
        }

        /// <summary>Milli-units as a percent with at most one decimal: 400 = "0.4", 16000 = "16".</summary>
        public static string Milli(int milli)
        {
            int tenths = milli / 100;
            return tenths % 10 == 0 ? (tenths / 10).ToString(Inv) : (tenths / 10).ToString(Inv) + "." + System.Math.Abs(tenths % 10).ToString(Inv);
        }
    }
}
