namespace Deadswitch.Sim.Config
{
    /// <summary>Host-side timing (ADR-0004). Not used by the sim rules; lives here so every tunable is in one file.</summary>
    public sealed class HostConfig : IConfigSection
    {
        public int MaxCatchUpHours = 168;
        public int DesyncToleranceMinutes = 5;
        public int AutosaveSeconds = 60;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("host", "Host timing: offline catch-up and saving (ADR-0004). Real time: 1 tick = 1 real minute.");
            v.Int("max_catch_up_hours", ref MaxCatchUpHours, 1, 10_000, "Offline time simulated on return is capped here (never grants unlimited progress).");
            v.Int("desync_tolerance_minutes", ref DesyncToleranceMinutes, 0, 10_000, "A device clock that moved backwards by more than this raises a time desync.");
            v.Int("autosave_seconds", ref AutosaveSeconds, 5, 3_600, "Real seconds between autosaves while playing.");
            v.EndSection();
        }
    }
}
