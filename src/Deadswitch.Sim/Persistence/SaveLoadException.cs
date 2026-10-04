using System;

namespace Deadswitch.Sim.Persistence
{
    public enum SaveLoadError
    {
        /// <summary>The bytes are not a DEADSWITCH save (wrong magic or too short).</summary>
        NotASave = 0,

        /// <summary>The file ends early (interrupted write).</summary>
        Truncated = 1,

        /// <summary>Checksum or structure mismatch (disk corruption or tampering).</summary>
        Corrupted = 2,

        /// <summary>Written by a newer build than this one.</summary>
        UnsupportedVersion = 3,
    }

    /// <summary>Raised when save bytes cannot be loaded. Hosts fall back to the backup copy (see <c>SaveFileStore</c>).</summary>
    public sealed class SaveLoadException : Exception
    {
        public SaveLoadException(SaveLoadError error, string message)
            : base(message)
        {
            Error = error;
        }

        public SaveLoadError Error { get; }
    }
}
