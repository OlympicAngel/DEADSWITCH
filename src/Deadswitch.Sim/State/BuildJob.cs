namespace Deadswitch.Sim.State
{
    /// <summary>A construction or upgrade in progress. Costs were paid when it started (refunds use them).</summary>
    public sealed class BuildJob
    {
        public int Slot;

        public FacilityKind Kind;

        /// <summary>Level the slot reaches when the job completes (1 = new build).</summary>
        public int TargetLevel;

        public long StartTick;

        public long CompleteTick;

        public int PaidEnergy;

        public int PaidCompute;

        public void Visit(IStateVisitor v)
        {
            v.Int(ref Slot);
            int kind = (int)Kind;
            v.Int(ref kind);
            Kind = (FacilityKind)kind;
            v.Int(ref TargetLevel);
            v.Long(ref StartTick);
            v.Long(ref CompleteTick);
            v.Int(ref PaidEnergy);
            v.Int(ref PaidCompute);
        }
    }
}
