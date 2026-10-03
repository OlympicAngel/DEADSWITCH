using System.Collections.Generic;

namespace Deadswitch.Sim.Events
{
    /// <summary>Append-only event log. Powers battle reports, replays and later server verification.</summary>
    public sealed class EventLog
    {
        private readonly List<SimEvent> _events = new List<SimEvent>();

        public IReadOnlyList<SimEvent> Events => _events;

        public int Count => _events.Count;

        public void Append(SimEvent e)
        {
            _events.Add(e);
        }
    }
}
