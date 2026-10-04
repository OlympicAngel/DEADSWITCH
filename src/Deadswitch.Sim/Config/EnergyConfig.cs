namespace Deadswitch.Sim.Config
{
    /// <summary>Energy: a continuous flow with upkeep (doc 02 s3, doc 10 s3). Generation comes from Generator facilities.</summary>
    public sealed class EnergyConfig : IConfigSection
    {
        public int Start = 200;
        public int Cap = 500;

        /// <summary>doc 10 s3: -4/min. The AI core is always powered first.</summary>
        public int CoreUpkeepPerHour = 240;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("energy", "Energy: continuous flow with upkeep (doc 02 s3, doc 10 s3). Generation comes from Generators.");
            v.Int("start", ref Start, 0, 1_000_000, "Energy at the start of a run.");
            v.Int("cap", ref Cap, 1, 1_000_000, "Storage cap before Battery Banks.");
            v.Int("core_upkeep_per_hour", ref CoreUpkeepPerHour, 0, 1_000_000, "AI core upkeep per game hour, paid before any facility. Unpaid = blackout.");
            v.EndSection();
        }
    }
}
