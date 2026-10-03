namespace Deadswitch.Sim.State
{
    /// <summary>What a recent raid's report hides (SPEC-006): which lies Verify can expose, and whether it was verified.</summary>
    public sealed class RaidRecord
    {
        /// <summary>Verify finding bits (also the ReportVerified payload).</summary>
        public const int GateLie = 1;
        public const int SummaryEdit = 2;

        public int RaidId;

        public int LieFlags;

        public bool Verified;

        public void Visit(IStateVisitor v)
        {
            v.Int(ref RaidId);
            v.Int(ref LieFlags);
            v.Bool(ref Verified);
        }
    }
}
