using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>Idle corruption recovery (doc 03 s3). Raisers arrive with the AI features.</summary>
    public static class CorruptionSystem
    {
        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            s.Corruption = SimMath.Clamp(s.Corruption - c.Corruption.DecayPerHour, 0, c.Corruption.Cap);
        }
    }
}
