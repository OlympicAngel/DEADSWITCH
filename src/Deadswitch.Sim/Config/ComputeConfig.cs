namespace Deadswitch.Sim.Config
{
    /// <summary>Compute: the AI's processing power, produced by Server Racks (doc 02 s4).</summary>
    public sealed class ComputeConfig : IConfigSection
    {
        public int Start = 50;
        public int Cap = 100;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("compute", "Compute: the AI's processing power, produced by Server Racks (doc 02 s4).");
            v.Int("start", ref Start, 0, 1_000_000, "Compute at the start of a run.");
            v.Int("cap", ref Cap, 1, 1_000_000, "Storage cap.");
            v.EndSection();
        }
    }
}
