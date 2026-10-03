namespace Deadswitch.Sim.State
{
    /// <summary>One Hub slot. Empty when <see cref="Kind"/> is None (Level 0).</summary>
    public sealed class FacilitySlot
    {
        public FacilityKind Kind;

        /// <summary>1-based level; 0 when empty.</summary>
        public int Level;

        /// <summary>Handler's switch. Off = no upkeep, no output, no crew.</summary>
        public bool Enabled = true;

        /// <summary>Whether the facility received power this tick (false = shed or blackout).</summary>
        public bool Powered;

        /// <summary>Whether the facility has its full crew this tick (false = unmanned, AI-run).</summary>
        public bool Staffed;

        public bool IsEmpty => Kind == FacilityKind.None;

        public void Visit(IStateVisitor v)
        {
            int kind = (int)Kind;
            v.Int(ref kind);
            Kind = (FacilityKind)kind;
            v.Int(ref Level);
            v.Bool(ref Enabled);
            v.Bool(ref Powered);
            v.Bool(ref Staffed);
        }

        public void Clear()
        {
            Kind = FacilityKind.None;
            Level = 0;
            Enabled = true;
            Powered = false;
            Staffed = false;
        }
    }
}
