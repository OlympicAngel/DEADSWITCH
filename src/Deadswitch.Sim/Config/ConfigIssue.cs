namespace Deadswitch.Sim.Config
{
    public enum ConfigIssueSeverity
    {
        Warning = 0,
        Error = 1,
    }

    /// <summary>One problem found while reading or validating a balance file.</summary>
    public readonly struct ConfigIssue
    {
        public ConfigIssue(ConfigIssueSeverity severity, int line, string key, string message)
        {
            Severity = severity;
            Line = line;
            Key = key;
            Message = message;
        }

        public ConfigIssueSeverity Severity { get; }

        /// <summary>1-based line in the source text, or 0 when the issue is not tied to a line (for example a missing key).</summary>
        public int Line { get; }

        /// <summary>Full key as <c>section.key</c>, or empty.</summary>
        public string Key { get; }

        public string Message { get; }

        public override string ToString()
        {
            string where = Line > 0 ? "line " + Line.ToString(System.Globalization.CultureInfo.InvariantCulture) : "file";
            string key = string.IsNullOrEmpty(Key) ? string.Empty : " [" + Key + "]";
            return Severity.ToString().ToLowerInvariant() + " (" + where + ")" + key + ": " + Message;
        }
    }
}
