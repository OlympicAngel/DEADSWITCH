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

        /// <summary>Battle scars, 0..scars.max_damage (SPEC-018). Stays until repaired.</summary>
        public int Damage;

        /// <summary>Repair finishes at this tick (0 = not under repair).</summary>
        public long RepairUntilTick;

        /// <summary>A glitch or collapse stopped it until this tick (SPEC-021).</summary>
        public long StalledUntilTick;

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
            if (v.Version >= 11)
            {
                v.Int(ref Damage);
                v.Long(ref RepairUntilTick);
            }

            if (v.Version >= 14)
            {
                v.Long(ref StalledUntilTick);
            }
        }

        public void Clear()
        {
            Kind = FacilityKind.None;
            Level = 0;
            Enabled = true;
            Powered = false;
            Staffed = false;
            Damage = 0;
            RepairUntilTick = 0;
            StalledUntilTick = 0;
        }
    }
}
