namespace Deadswitch.Sim.Config
{
    /// <summary>The project's climax and its counterplay (SPEC-011, doc 03 s5-6, doc 10 s2).</summary>
    public sealed class ClimaxConfig : IConfigSection
    {
        public int WindowHours = 24;
        public int PurgeEnergy = 300;
        public int SilenceHours = 48;
        public int CancelCompute = 80;
        public int CancelToPct = 55;
        public int BetrayalStrengthPct = 250;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("climax", "The project's climax and counterplay (SPEC-011). All (tune).");
            v.Int("window_hours", ref WindowHours, 1, 1_000, "Final warning window at Imminent, in game hours (doc 10: 24 real hours).");
            v.Int("purge_energy", ref PurgeEnergy, 0, 100_000, "Purge the core: energy cost (all compute is lost too).");
            v.Int("silence_hours", ref SilenceHours, 1, 1_000, "Silence the AI (OVERRIDE): game hours the agenda stops and the AI goes quiet.");
            v.Int("cancel_compute", ref CancelCompute, 0, 100_000, "Cancel the project (after an Audit in the window): compute cost.");
            v.Int("cancel_to_pct", ref CancelToPct, 0, 100, "Cancel the project: progress drops back to this %.");
            v.Int("betrayal_strength_pct", ref BetrayalStrengthPct, 100, 1_000, "Betrayal: strength of the raid the AI lets in, in % of a normal raid (turrets stay offline for it).");
            v.EndSection();
        }
    }
}
