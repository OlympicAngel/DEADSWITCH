using System.Collections.Generic;

namespace Deadswitch.Sim.Events
{
    /// <summary>
    /// Append-only record of what really happened (ADR-0003). Powers battle reports, the loss ledger,
    /// audits (the player-visible report is a view; AI edits never touch this log) and replays.
    /// </summary>
    public sealed class EventLog
    {
        /// <summary>Bump when an existing event kind changes payload meaning.</summary>
        public const int SchemaVersion = 14;

        private readonly List<SimEvent> _events = new List<SimEvent>();

        public IReadOnlyList<SimEvent> Events => _events;

        public int Count => _events.Count;

        /// <summary>Sequence number the next appended event receives.</summary>
        public long NextSeq { get; private set; }

        /// <summary>Replaces the contents with a loaded log (save restore only).</summary>
        internal void Restore(long nextSeq, List<SimEvent> events)
        {
            _events.Clear();
            _events.AddRange(events);
            NextSeq = nextSeq;
        }

        public SimEvent Append(long tick, EventKind kind, int a = 0, int b = 0, int c = 0, int d = 0)
        {
            var e = new SimEvent(NextSeq, tick, kind, a, b, c, d);
            NextSeq++;
            _events.Add(e);
            return e;
        }
    }
}
