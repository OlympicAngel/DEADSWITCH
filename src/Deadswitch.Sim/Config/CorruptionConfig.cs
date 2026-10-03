namespace Deadswitch.Sim.Config
{
    /// <summary>Corruption: instability of the AI core (doc 03 s3, doc 10 s3).</summary>
    public sealed class CorruptionConfig : IConfigSection
    {
        public int Cap = 100;
        public int DecayPerHour = 1;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("corruption", "Corruption: instability of the AI core (doc 03 s3, doc 10 s3).");
            v.Int("cap", ref Cap, 1, 1_000, "Maximum corruption.");
            v.Int("decay_per_hour", ref DecayPerHour, 0, 1_000, "Idle recovery per hour.");
            v.EndSection();
        }
    }
}
