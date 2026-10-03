using System.Collections.Generic;

namespace Deadswitch.Sim.Commands
{
    /// <summary>Every accepted command in order. Seed + config + this log reproduces the run exactly (see <see cref="Replay"/>).</summary>
    public sealed class CommandLog
    {
        private readonly List<RecordedCommand> _commands = new List<RecordedCommand>();

        public IReadOnlyList<RecordedCommand> Commands => _commands;

        public int Count => _commands.Count;

        internal void Add(RecordedCommand command)
        {
            _commands.Add(command);
        }
    }
}
