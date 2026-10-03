namespace Deadswitch.Sim.Events
{
    public enum EventKind
    {
        None = 0,
        RaidStarted = 1,
        BlackoutStarted = 2,
    }

    /// <summary>Immutable log entry. A and B are kind-specific payloads.</summary>
    public readonly struct SimEvent
    {
        public SimEvent(long tick, EventKind kind, int a, int b)
        {
            Tick = tick;
            Kind = kind;
            A = a;
            B = b;
        }

        public long Tick { get; }

        public EventKind Kind { get; }

        /// <summary>RaidStarted: energy looted.</summary>
        public int A { get; }

        public int B { get; }
    }
}
