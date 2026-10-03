namespace Deadswitch.Sim.Config
{
    /// <summary>Energy: a continuous flow with upkeep (doc 02 s3, doc 10 s3).</summary>
    public sealed class EnergyConfig : IConfigSection
    {
        public int Start = 200;
        public int Cap = 500;

        /// <summary>doc 10 said 6, but 6 - 4 upkeep - 3 rack = -1/min (blackout in about 3h). Corrected, see doc 10 s11.</summary>
        public int GenPerTick = 8;
        public int UpkeepPerTick = 4;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("energy", "Energy: continuous flow with upkeep (doc 02 s3, doc 10 s3).");
            v.Int("start", ref Start, 0, 1_000_000, "Energy at the start of a run.");
            v.Int("cap", ref Cap, 1, 1_000_000, "Storage cap.");
            v.Int("gen_per_tick", ref GenPerTick, 0, 10_000, "Base generation per tick (1 tick = 1 game minute).");
            v.Int("upkeep_per_tick", ref UpkeepPerTick, 0, 10_000, "Base upkeep per tick before facilities.");
            v.EndSection();
        }
    }
}
