namespace Deadswitch.Sim.Events
{
    /// <summary>
    /// What happened, as recorded truth. Numeric values are stable forever (saves and replays store them):
    /// never renumber or reuse a value; add new kinds at the end and bump <see cref="EventLog.SchemaVersion"/>
    /// if a payload meaning changes. Payload meaning per kind is documented on each member (A, B, C, D).
    /// </summary>
    public enum EventKind
    {
        None = 0,

        /// <summary>A raid hit. A: energy looted.</summary>
        RaidStarted = 1,

        /// <summary>The AI core lost power: no facility runs, regrowth pauses. No payload.</summary>
        BlackoutStarted = 2,

        /// <summary>Delegation level changed by command. A: new level, B: previous level (see DelegationLevel).</summary>
        DelegationChanged = 3,

        /// <summary>The AI core is powered again. No payload.</summary>
        BlackoutEnded = 4,

        /// <summary>Construction started. A: slot, B: FacilityKind, C: target level, D: build minutes.</summary>
        BuildStarted = 5,

        /// <summary>Construction finished. A: slot, B: FacilityKind, C: new level.</summary>
        BuildCompleted = 6,

        /// <summary>Construction cancelled. A: slot, B: energy refunded, C: compute refunded.</summary>
        BuildCancelled = 7,

        /// <summary>Facility demolished. A: slot, B: FacilityKind, C: energy refunded, D: compute refunded.</summary>
        FacilityDemolished = 8,

        /// <summary>Facility lost power to priority shedding. A: slot, B: FacilityKind.</summary>
        FacilityShed = 9,

        /// <summary>Facility powered again. A: slot, B: FacilityKind.</summary>
        FacilityRestored = 10,

        /// <summary>Handler switched a facility. A: slot, B: 1 = on, 0 = off.</summary>
        FacilityPowerSet = 11,

        /// <summary>Power priority changed. A: slot, B: new rank (0 = highest).</summary>
        PriorityChanged = 12,
    }

    /// <summary>Immutable log entry. <see cref="Seq"/> is unique and increasing across the whole run.</summary>
    public readonly struct SimEvent
    {
        public SimEvent(long seq, long tick, EventKind kind, int a, int b, int c, int d)
        {
            Seq = seq;
            Tick = tick;
            Kind = kind;
            A = a;
            B = b;
            C = c;
            D = d;
        }

        public long Seq { get; }

        public long Tick { get; }

        public EventKind Kind { get; }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public int D { get; }

        public override string ToString()
        {
            return "#" + Seq + " t" + Tick + " " + Kind + " (" + A + ", " + B + ", " + C + ", " + D + ")";
        }
    }
}
