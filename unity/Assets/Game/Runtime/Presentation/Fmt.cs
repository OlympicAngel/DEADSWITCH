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
    }
}
