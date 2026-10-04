namespace Deadswitch.Sim.Config
{
    /// <summary>What corruption does (SPEC-021, doc 03 s3-4, doc 10 bands). Placeholders: tune at F-099.</summary>
    public sealed class GlitchConfig : IConfigSection
    {
        public int[] GlitchPctByBand = { 0, 3, 8, 15 };
        public int StallHours = 2;
        public int DrainEnergy = 60;
        public int DefectionPct = 25;
        public int CrisisPctPerHour = 6;
        public int CrisisCooldownHours = 12;
        public int CollapseEnergyPct = 40;
        public int CollapseStallHours = 3;
        public int TakeoverHours = 4;
        public int RollbackHours = 24;
        public int SwarmStrengthPct = 160;
        public int FlushEnergy = 600;
        public int FlushMilli = 25_000;
        public int FlushHours = 3;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("glitch", "Corruption effects: AI-run unit glitches, defection, the Critical crisis ladder and the core flush (doc 03 s3-4). Corruption is milli. All (tune).");
            v.IntList("glitch_pct_by_band", ref GlitchPctByBand, 0, 100, 4, 4, "Hourly chance each AI-run producer glitches, per band (Stable, Glitchy, Unstable, Critical).");
            v.Int("stall_hours", ref StallHours, 1, 100, "Disobedience: a glitched facility stops for this long.");
            v.Int("drain_energy", ref DrainEnergy, 0, 10_000, "Overextension: energy a glitched facility burns.");
            v.Int("defection_pct", ref DefectionPct, 0, 100, "Unstable or worse: chance an AI-run turret is hijacked in a fight (its guns turn on us).");
            v.Int("crisis_pct_per_hour", ref CrisisPctPerHour, 0, 100, "Critical: hourly chance a crisis triggers.");
            v.Int("crisis_cooldown_hours", ref CrisisCooldownHours, 1, 1_000, "Hours between crises.");
            v.Int("collapse_energy_pct", ref CollapseEnergyPct, 0, 100, "Collapse: share of stored energy lost.");
            v.Int("collapse_stall_hours", ref CollapseStallHours, 1, 100, "Collapse: every producer stops this long.");
            v.Int("takeover_hours", ref TakeoverHours, 1, 100, "AI takeover: the AI ignores build, research and posture orders this long.");
            v.Int("rollback_hours", ref RollbackHours, 1, 1_000, "Forced rollback: a restored module is locked this long.");
            v.Int("swarm_strength_pct", ref SwarmStrengthPct, 1, 10_000, "Rival swarm: attack strength as % of a raid.");
            v.Int("flush_energy", ref FlushEnergy, 0, 100_000, "Core flush: energy it costs.");
            v.Int("flush_milli", ref FlushMilli, 0, 100_000, "Core flush: corruption it removes.");
            v.Int("flush_hours", ref FlushHours, 1, 100, "Core flush: the AI is offline this long (AI-run units stop, no predictions).");
            v.EndSection();
        }
    }
}
