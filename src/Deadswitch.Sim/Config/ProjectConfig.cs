namespace Deadswitch.Sim.Config
{
    /// <summary>The AI's hidden project, its clues and the Audit tool (SPEC-007, doc 03 s5, doc 10 s2).</summary>
    public sealed class ProjectConfig : IConfigSection
    {
        public int ActiveFrom = 25;
        public int AdvancedFrom = 55;
        public int ImminentFrom = 85;
        public int GrowthPerHourAtFullBoldness = 250;
        public int MilliPerSkimmedCompute = 8;
        public int SkimFromBoldnessPct = 30;
        public int SkimPct = 15;
        public int UnderreportPct = 30;
        public int AuditComputeCost = 60;
        public int AuditCooldownHours = 12;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("project", "The AI's hidden project clock, its clues, and the Audit (SPEC-007). Progress is milli-units: 100000 = 100%. All (tune).");
            v.Int("active_from", ref ActiveFrom, 1, 100, "Project stage Active from this % (Dormant below).");
            v.Int("advanced_from", ref AdvancedFrom, 1, 100, "Project stage Advanced from this %.");
            v.Int("imminent_from", ref ImminentFrom, 1, 100, "Project stage Imminent from this %.");
            v.Int("growth_per_hour_at_full_boldness", ref GrowthPerHourAtFullBoldness, 0, 100_000, "Project progress per game hour at 100% Boldness (scales linearly).");
            v.Int("milli_per_skimmed_compute", ref MilliPerSkimmedCompute, 0, 100_000, "Project progress per compute point the AI skims.");
            v.Int("skim_from_boldness_pct", ref SkimFromBoldnessPct, 0, 100, "The AI skims compute while Boldness is at least this %.");
            v.Int("skim_pct", ref SkimPct, 0, 100, "Share of each hour's compute output the AI skims.");
            v.Int("underreport_pct", ref UnderreportPct, 0, 100, "A bold AI (Boldness >= 50%) shows corruption this % lower than it is.");
            v.Int("audit_compute_cost", ref AuditComputeCost, 0, 100_000, "Compute spent per Audit.");
            v.Int("audit_cooldown_hours", ref AuditCooldownHours, 0, 1_000, "Game hours between Audits.");
            v.EndSection();
        }
    }
}
