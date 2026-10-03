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
            if (s.OverrideCharges >= c.Override.MaxCharges)
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

        public static bool Ready(GameState s)
        {
            return s.OverrideCharges > 0 && s.Tick >= s.OverrideCooldownUntil;
        }
    }
}
