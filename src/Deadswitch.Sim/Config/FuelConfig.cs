namespace Deadswitch.Sim.Config
{
    /// <summary>Fuel: consumable for operations. No passive income at the start (doc 10 s3, s5).</summary>
    public sealed class FuelConfig : IConfigSection
    {
        public int Start = 100;
        public int Cap = 300;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("fuel", "Fuel: consumable for vehicles, raids, outposts and operations (doc 10 s3, s5).");
            v.Int("start", ref Start, 0, 1_000_000, "Fuel at the start of a run.");
            v.Int("cap", ref Cap, 1, 1_000_000, "Storage cap.");
            v.EndSection();
        }
    }
}
