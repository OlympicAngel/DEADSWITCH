using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>Resource output of powered facilities (Server Racks -> compute).</summary>
    public static class ProductionSystem
    {
        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;

            int computePerHour = 0;
            foreach (FacilitySlot slot in s.Slots)
            {
                if (slot.Kind == FacilityKind.ServerRack && Economy.IsRunning(slot))
                {
                    computePerHour += Economy.EffectiveOutput(c, slot);
                }
            }

            s.Compute = SimMath.Clamp(s.Compute + Rates.PerTick(computePerHour, s.Tick), 0, c.Compute.Cap);
        }
    }
}
