namespace Deadswitch.Sim.Config
{
    /// <summary>Corruption: instability of the AI core (doc 03 s3, doc 10 s3). Stored in milli-units (100_000 = 100%).</summary>
    public sealed class CorruptionConfig : IConfigSection
    {
        public int DecayMilliPerHour = 1000;
        public int AutomationMilliPerHour = 150;
        public int DecayPermillePerHour;
        public int ComputeMilliPerPoint;
        public int GlitchyFrom = 31;
        public int UnstableFrom = 61;
        public int CriticalFrom = 86;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("corruption", "Corruption: instability of the AI core, 0-100% stored as milli-units (doc 03 s3, doc 10 s3).");
            v.Int("decay_milli_per_hour", ref DecayMilliPerHour, 0, 100_000, "Idle recovery per game hour (1000 = 1%).");
            v.Int("automation_milli_per_hour", ref AutomationMilliPerHour, 0, 100_000, "Added per game hour for each unmanned (AI-run) facility.");
            v.Int("decay_permille_per_hour", ref DecayPermillePerHour, 0, 1_000, "Extra recovery per game hour, per mille of the current level: corruption settles instead of running away.");
            v.Int("compute_milli_per_point", ref ComputeMilliPerPoint, 0, 10_000, "Heavy compute use (doc 03 s3): added per compute point spent on research and construction.");
            v.Int("glitchy_from", ref GlitchyFrom, 1, 100, "Band start (percent): Glitchy.");
            v.Int("unstable_from", ref UnstableFrom, 1, 100, "Band start (percent): Unstable.");
            v.Int("critical_from", ref CriticalFrom, 1, 100, "Band start (percent): Critical.");
            v.EndSection();
        }
    }
}
