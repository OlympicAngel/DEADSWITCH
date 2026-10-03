namespace Deadswitch.Sim.Config
{
    /// <summary>Host-side timing (ADR-0004). Not used by the sim rules; lives here so every tunable is in one file.</summary>
    public sealed class HostConfig : IConfigSection
    {
        public int MaxCatchUpHours = 168;
        public int DesyncToleranceMinutes = 5;
        public int AutosaveSeconds = 60;
        public int NotifyProjectHours = 24;
        public int NotifyMax = 6;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("host", "Host timing: offline catch-up and saving (ADR-0004). Real time: 1 tick = 1 real minute.");
            v.Int("max_catch_up_hours", ref MaxCatchUpHours, 1, 10_000, "Offline time simulated on return is capped here (never grants unlimited progress).");
            v.Int("desync_tolerance_minutes", ref DesyncToleranceMinutes, 0, 10_000, "A device clock that moved backwards by more than this raises a time desync.");
            v.Int("autosave_seconds", ref AutosaveSeconds, 5, 3_600, "Real seconds between autosaves while playing.");
            v.Int("notify_project_hours", ref NotifyProjectHours, 1, 168, "Game hours the logout projection runs ahead to schedule notifications (SPEC-010).");
            v.Int("notify_max", ref NotifyMax, 0, 64, "Most local notifications scheduled per logout.");
            v.EndSection();
        }
    }
}
