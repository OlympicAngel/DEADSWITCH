using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>Corruption over time (doc 03 s3): idle and proportional decay, automation load, heavy compute use, band changes.</summary>
    public static class CorruptionSystem
    {
        public const int MaxMilli = 100_000;

        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            Config.CorruptionConfig c = ctx.Config.Corruption;
            var m = ctx.Config.Modules;
            int permille = c.DecayPermillePerHour + (Modules.Has(s, State.ModuleNode.CY6) ? m.PartitionPermille : 0);
            int proportional = (int)((long)s.CorruptionMilli * permille / 1000);
            int decay = c.DecayMilliPerHour + (Modules.Has(s, State.ModuleNode.CY3) ? m.CleanRoomMilli : 0);
            int delta = (s.AutomationLoad * c.AutomationMilliPerHour) - decay - proportional;
            Add(ctx, delta);
        }

        /// <summary>Heavy compute use (doc 03 s3): spending compute on research or construction strains the core.</summary>
        public static void ComputeUse(SimContext ctx, int computeSpent)
        {
            if (computeSpent > 0 && ctx.Config.Corruption.ComputeMilliPerPoint > 0)
            {
                // cooling towers (SPEC-038) take the heat off the racks
                int cooling = System.Math.Min(60, Economy.SumOutput(ctx.State, ctx.Config, FacilityKind.CoolingTower));
                Add(ctx, (int)System.Math.Min(MaxMilli, (long)computeSpent * ctx.Config.Corruption.ComputeMilliPerPoint * (100 - cooling) / 100));
            }
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
