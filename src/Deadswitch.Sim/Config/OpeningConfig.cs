namespace Deadswitch.Sim.Config
{
    /// <summary>The opening of a run (SPEC-009, doc 01 s7): a beatable first raid, then a protection window.</summary>
    public sealed class OpeningConfig : IConfigSection
    {
        public bool Enabled = true;
        public int RaidAtMinute = 6;
        public int RaidStrength = 12;
        public int ProtectionHours = 10;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("opening", "Opening of a run (SPEC-009): a scripted, beatable first raid and an early protection window. All (tune).");
            v.Bool("enabled", ref Enabled, "Play the opening raid and protection window in new runs.");
            v.Int("raid_at_minute", ref RaidAtMinute, 1, 100_000, "Game minute the opening raid's warning appears.");
            v.Int("raid_strength", ref RaidStrength, 1, 100_000, "Fixed strength of the opening raid (a Turtle garrison beats it).");
            v.Int("protection_hours", ref ProtectionHours, 0, 1_000, "No other raids before this game hour.");
            v.EndSection();
        }
    }
}
