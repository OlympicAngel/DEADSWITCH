using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>Per-tick energy flow, then server racks turn energy into compute (doc 02 s3-4).</summary>
    public static class EnergySystem
    {
        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;

            bool hadEnergy = s.Energy > 0;
            s.Energy = SimMath.Clamp(s.Energy + c.Energy.GenPerTick - c.Energy.UpkeepPerTick, 0, c.Energy.Cap);

            if (s.Energy >= c.Compute.RackEnergyCostPerTick)
            {
                s.Energy -= c.Compute.RackEnergyCostPerTick;
                s.Compute = SimMath.Clamp(s.Compute + c.Compute.PerTick, 0, c.Compute.Cap);
            }

            if (hadEnergy && s.Energy == 0)
            {
                ctx.Emit(EventKind.BlackoutStarted);
            }
        }
    }
}
