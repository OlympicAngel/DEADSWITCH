namespace Deadswitch.Sim.Config
{
    /// <summary>Battle reports: the AI's edits and the Verify tool (SPEC-006, doc 10 s4 + s7).</summary>
    public sealed class ReportConfig : IConfigSection
    {
        public int EditChancePct = 35;
        public int EditShownPct = 60;
        public int VerifyComputeCost = 20;
        public int KeepRaids = 10;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("report", "Battle reports: AI edits to the summary and the Verify tool (SPEC-006). All (tune).");
            v.Int("edit_chance_pct", ref EditChancePct, 0, 100, "Chance the AI edits a breach summary while corruption is Unstable or worse.");
            v.Int("edit_shown_pct", ref EditShownPct, 0, 100, "An edited summary shows this % of the true energy loss.");
            v.Int("verify_compute_cost", ref VerifyComputeCost, 0, 100_000, "Compute spent to verify one report against the sensor log.");
            v.Int("keep_raids", ref KeepRaids, 1, 100, "Reports kept (and verifiable) for the most recent raids.");
            v.EndSection();
        }
    }
}
