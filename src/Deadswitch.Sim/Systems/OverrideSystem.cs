using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>OVERRIDE charge regeneration (doc 10 s3).</summary>
    public static class OverrideSystem
    {
        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            if (s.OverrideCharges >= MaxCharges(s, c))
            {
                s.OverrideNextChargeTick = s.Tick + c.Override.RegenMinutes;
                return;
            }

            if (s.Tick >= s.OverrideNextChargeTick)
            {
                s.OverrideCharges++;
                s.OverrideNextChargeTick = s.Tick + c.Override.RegenMinutes;
            }
        }

        /// <summary>Charge cap after a fork took one away (never below one).</summary>
        public static int MaxCharges(GameState s, SimConfig c)
        {
            return System.Math.Max(1, c.Override.MaxCharges + s.Perks[(int)Perk.Override] - s.OverrideMaxPenalty);
        }

        public static bool Ready(GameState s)
        {
            return s.OverrideCharges > 0 && s.Tick >= s.OverrideCooldownUntil;
        }
    }
}
