namespace Deadswitch.Sim.Config
{
    /// <summary>Compute: the AI's processing power, produced by server racks that burn energy (doc 02 s4).</summary>
    public sealed class ComputeConfig : IConfigSection
    {
        public int Start = 50;
        public int Cap = 100;
        public int PerTick = 1;
        public int RackEnergyCostPerTick = 3;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("compute", "Compute: the AI's processing power. Server racks turn energy into compute (doc 02 s4).");
            v.Int("start", ref Start, 0, 1_000_000, "Compute at the start of a run.");
            v.Int("cap", ref Cap, 1, 1_000_000, "Storage cap.");
            v.Int("per_tick", ref PerTick, 0, 10_000, "Compute produced per tick while racks are powered.");
            v.Int("rack_energy_cost_per_tick", ref RackEnergyCostPerTick, 0, 10_000, "Energy the racks burn per tick to produce compute.");
            v.EndSection();
        }
    }
}
