using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>The single raid signature of the pressure loop (SPEC-001). Loot is capped, never a full wipe.</summary>
    public static class RaidSystem
    {
        public static void StartOfTick(SimContext ctx)
        {
            if (ctx.State.Tick % SimConfig.TicksPerDay == 0)
            {
                ctx.State.RaidsToday = 0;
            }
        }

        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;

            // Always consume exactly one RNG draw per tick so cap checks never desync the stream.
            bool roll = s.Rng.NextBelow((uint)c.Raid.MeanIntervalTicks) == 0;
            if (!roll || s.RaidsToday >= c.Raid.MaxPerDay)
            {
                return;
            }

            int loot = SimMath.PctFloor(s.Energy, c.Raid.LootPctOfEnergy);
            if (loot > c.Raid.LootCap)
            {
                loot = c.Raid.LootCap;
            }

            s.Energy -= loot;
            s.RaidsToday++;
            ctx.Emit(EventKind.RaidStarted, loot);
        }
    }
}
