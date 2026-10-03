namespace Deadswitch.Sim.Config
{
    /// <summary>Construction queue rules (SPEC-002 rule 4).</summary>
    public sealed class BuildConfig : IConfigSection
    {
        public int QueueSlots = 1;
        public int CancelRefundPct = 50;
        public int DemolishRefundPct = 25;
        public int DuplicateCostPct = 100;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("build", "Construction queue (SPEC-002 rule 4).");
            v.Int("queue_slots", ref QueueSlots, 1, 16, "Construction jobs that can run at the same time.");
            v.Int("cancel_refund_pct", ref CancelRefundPct, 0, 100, "Share of the paid cost returned when a job is cancelled.");
            v.Int("demolish_refund_pct", ref DemolishRefundPct, 0, 100, "Share of the current level's cost returned when a facility is demolished.");
            v.Int("duplicate_cost_pct", ref DuplicateCostPct, 0, 1_000, "Diminishing returns: building another facility of a kind you already have costs +this percent per existing one.");
            v.EndSection();
        }
    }
}
