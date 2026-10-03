using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>Corruption over time (doc 03 s3): idle decay, automation load from unmanned facilities, band changes.</summary>
    public static class CorruptionSystem
    {
        public const int MaxMilli = 100_000;

        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            int delta = (s.AutomationLoad * ctx.Config.Corruption.AutomationMilliPerHour) - ctx.Config.Corruption.DecayMilliPerHour;
            Add(ctx, delta);
        }

        /// <summary>Adds (or removes) corruption, clamped, emitting a band change event when the band moves.</summary>
        public static void Add(SimContext ctx, int milli)
        {
            GameState s = ctx.State;
            CorruptionBand before = Band(ctx.Config, s.CorruptionMilli);
            s.CorruptionMilli = SimMath.Clamp(s.CorruptionMilli + milli, 0, MaxMilli);
            CorruptionBand after = Band(ctx.Config, s.CorruptionMilli);
            if (after != before)
            {
                ctx.Emit(EventKind.CorruptionBandChanged, (int)after, (int)before, s.CorruptionMilli);
            }
        }

        /// <summary>Whole percent shown to the handler (rounded down).</summary>
        public static int Percent(int milli)
        {
            return milli / 1000;
        }

        public static CorruptionBand Band(SimConfig c, int milli)
        {
            int pct = Percent(milli);
            if (pct >= c.Corruption.CriticalFrom)
            {
                return CorruptionBand.Critical;
            }

            if (pct >= c.Corruption.UnstableFrom)
            {
                return CorruptionBand.Unstable;
            }

            return pct >= c.Corruption.GlitchyFrom ? CorruptionBand.Glitchy : CorruptionBand.Stable;
        }
    }
}
