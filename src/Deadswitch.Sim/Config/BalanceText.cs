using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Deadswitch.Sim.Config
{
    public enum BalanceReadMode
    {
        /// <summary>Every key must be present and valid. Used by tests, CI and the CLI.</summary>
        Strict = 0,

        /// <summary>Missing keys keep their code default (warning). Used by the game so an older file still loads.</summary>
        Lenient = 1,
    }

    /// <summary>Outcome of reading a balance file. <see cref="Config"/> is always usable: invalid or missing values keep the code default.</summary>
    public sealed class BalanceReadResult
    {
        internal BalanceReadResult(SimConfig config, List<ConfigIssue> issues)
        {
            Config = config;
            Issues = issues;
        }

        public SimConfig Config { get; }

        public IReadOnlyList<ConfigIssue> Issues { get; }

        public bool HasErrors
        {
            get
            {
                foreach (ConfigIssue issue in Issues)
                {
                    if (issue.Severity == ConfigIssueSeverity.Error)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }

    /// <summary>
    /// Reads and writes the human-editable balance file: a strict TOML subset.
    /// <code>
    /// # comment
    /// [section]
    /// key = 42          # integers (underscores allowed: 1_000), may be negative
    /// flag = true       # booleans
    /// table = [20, 45]  # integer lists
    /// </code>
    /// Pure string processing: no I/O, so the sim stays engine-agnostic. Callers load the text.
    /// </summary>
    public static class BalanceText
    {
        public const string FileName = "DeadswitchBalance.toml";

        public static string Write(SimConfig config)
        {
            var writer = new Writer();
            config.Visit(writer);
            return writer.ToString();
        }

        /// <summary>Strict read that throws <see cref="BalanceConfigException"/> when the text has any error.</summary>
        public static SimConfig Parse(string text)
        {
            BalanceReadResult result = Read(text, BalanceReadMode.Strict);
            if (result.HasErrors)
            {
                throw new BalanceConfigException(result.Issues);
            }

            return result.Config;
        }

        public static BalanceReadResult Read(string text, BalanceReadMode mode)
        {
            var issues = new List<ConfigIssue>();
            Dictionary<string, Entry> entries = Tokenize(text ?? string.Empty, issues);
            var config = new SimConfig();
            var reader = new Reader(entries, issues, mode);
            config.Visit(reader);
            reader.ReportUnknownKeys();
            return new BalanceReadResult(config, issues);
        }

        private sealed class Entry
        {
            public Entry(int line, string key, string raw)
            {
                Line = line;
                Key = key;
                Raw = raw;
            }

            public int Line { get; }

            public string Key { get; }

            public string Raw { get; }

            public bool Consumed { get; set; }
        }

        private static Dictionary<string, Entry> Tokenize(string text, List<ConfigIssue> issues)
        {
            var entries = new Dictionary<string, Entry>();
            string section = string.Empty;
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                int lineNo = i + 1;
                string line = lines[i];
                int hash = line.IndexOf('#');
                if (hash >= 0)
                {
                    line = line.Substring(0, hash);
                }

                line = line.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (line[0] == '[')
                {
                    if (line[line.Length - 1] != ']' || !IsName(line.Substring(1, line.Length - 2).Trim()))
                    {
                        issues.Add(Error(lineNo, string.Empty, "malformed section header '" + line + "'. Expected [lower_snake_case]."));
                        section = "\u0000invalid";
                        continue;
                    }

                    section = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    issues.Add(Error(lineNo, string.Empty, "expected 'key = value', got '" + line + "'."));
                    continue;
                }

                string key = line.Substring(0, eq).Trim();
                string raw = line.Substring(eq + 1).Trim();
                if (!IsName(key))
                {
                    issues.Add(Error(lineNo, key, "invalid key name. Use lower_snake_case."));
                    continue;
                }

                if (section.Length == 0)
                {
                    issues.Add(Error(lineNo, key, "key outside of any [section]."));
                    continue;
                }

                if (section[0] == '\u0000')
                {
                    continue;
                }

                string fullKey = section + "." + key;
                if (entries.TryGetValue(fullKey, out Entry? existing))
                {
                    issues.Add(Error(lineNo, fullKey, "duplicate key (first defined on line " + Num(existing.Line) + ")."));
                    continue;
                }

                entries.Add(fullKey, new Entry(lineNo, fullKey, raw));
            }

            return entries;
        }

        private static bool IsName(string s)
        {
            if (s.Length == 0)
            {
                return false;
            }

            foreach (char c in s)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (!ok)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseInt(string raw, out int value)
        {
            value = 0;
            string s = raw.Replace("_", string.Empty);
            if (s.Length == 0 || raw.StartsWith("_", System.StringComparison.Ordinal) || raw.EndsWith("_", System.StringComparison.Ordinal))
            {
                return false;
            }

            return int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }

        private static ConfigIssue Error(int line, string key, string message)
        {
            return new ConfigIssue(ConfigIssueSeverity.Error, line, key, message);
        }

        private static string Num(int v)
        {
            return v.ToString(CultureInfo.InvariantCulture);
        }

        private static string Range(int min, int max)
        {
            return Num(min) + ".." + Num(max);
        }

        private sealed class Reader : IConfigVisitor
        {
            private readonly Dictionary<string, Entry> _entries;
            private readonly List<ConfigIssue> _issues;
            private readonly BalanceReadMode _mode;
            private string _section = string.Empty;

            public Reader(Dictionary<string, Entry> entries, List<ConfigIssue> issues, BalanceReadMode mode)
            {
                _entries = entries;
                _issues = issues;
                _mode = mode;
            }

            public void BeginSection(string name, string description)
            {
                _section = name;
            }

            public void EndSection()
            {
                _section = string.Empty;
            }

            public void Int(string key, ref int value, int min, int max, string description)
            {
                Entry? e = Take(key);
                if (e == null)
                {
                    return;
                }

                if (!TryParseInt(e.Raw, out int parsed))
                {
                    _issues.Add(Error(e.Line, e.Key, "'" + e.Raw + "' is not an integer."));
                    return;
                }

                if (parsed < min || parsed > max)
                {
                    _issues.Add(Error(e.Line, e.Key, Num(parsed) + " is out of range " + Range(min, max) + "."));
                    return;
                }

                value = parsed;
            }

            public void Bool(string key, ref bool value, string description)
            {
                Entry? e = Take(key);
                if (e == null)
                {
                    return;
                }

                if (e.Raw == "true")
                {
                    value = true;
                }
                else if (e.Raw == "false")
                {
                    value = false;
                }
                else
                {
                    _issues.Add(Error(e.Line, e.Key, "'" + e.Raw + "' is not a boolean (true or false)."));
                }
            }

            public void IntList(string key, ref int[] values, int min, int max, int minCount, int maxCount, string description)
            {
                Entry? e = Take(key);
                if (e == null)
                {
                    return;
                }

                string raw = e.Raw;
                if (raw.Length < 2 || raw[0] != '[' || raw[raw.Length - 1] != ']')
                {
                    _issues.Add(Error(e.Line, e.Key, "'" + raw + "' is not a list. Expected [a, b, c]."));
                    return;
                }

                string inner = raw.Substring(1, raw.Length - 2).Trim();
                var parsed = new List<int>();
                if (inner.Length > 0)
                {
                    foreach (string part in inner.Split(','))
                    {
                        string p = part.Trim();
                        if (!TryParseInt(p, out int item))
                        {
                            _issues.Add(Error(e.Line, e.Key, "list item '" + p + "' is not an integer."));
                            return;
                        }

                        if (item < min || item > max)
                        {
                            _issues.Add(Error(e.Line, e.Key, "list item " + Num(item) + " is out of range " + Range(min, max) + "."));
                            return;
                        }

                        parsed.Add(item);
                    }
                }

                if (parsed.Count < minCount || parsed.Count > maxCount)
                {
                    _issues.Add(Error(e.Line, e.Key, "list has " + Num(parsed.Count) + " items, expected " + Range(minCount, maxCount) + "."));
                    return;
                }

                values = parsed.ToArray();
            }

            public void ReportUnknownKeys()
            {
                var unknown = new List<Entry>();
                foreach (Entry e in _entries.Values)
                {
                    if (!e.Consumed)
                    {
                        unknown.Add(e);
                    }
                }

                // Report in file order so messages are stable and readable.
                unknown.Sort((a, b) => a.Line.CompareTo(b.Line));
                foreach (Entry e in unknown)
                {
                    ConfigIssueSeverity severity = _mode == BalanceReadMode.Strict ? ConfigIssueSeverity.Error : ConfigIssueSeverity.Warning;
                    _issues.Add(new ConfigIssue(severity, e.Line, e.Key, "unknown key (typo, or removed from the sim?)."));
                }
            }

            private Entry? Take(string key)
            {
                string fullKey = _section + "." + key;
                if (!_entries.TryGetValue(fullKey, out Entry? e))
                {
                    ConfigIssueSeverity severity = _mode == BalanceReadMode.Strict ? ConfigIssueSeverity.Error : ConfigIssueSeverity.Warning;
                    _issues.Add(new ConfigIssue(severity, 0, fullKey, "missing key; using the code default."));
                    return null;
                }

                e.Consumed = true;
                return e;
            }
        }

        private sealed class Writer : IConfigVisitor
        {
            private readonly StringBuilder _sb = new StringBuilder();

            public Writer()
            {
                _sb.Append("# DEADSWITCH balance file\n");
                _sb.Append("#\n");
                _sb.Append("# Every tunable number in the simulation. 1 tick = 1 game minute. Integers only.\n");
                _sb.Append("# Units are in the key names: _per_tick, _per_hour, _ticks, _pct (0-100), _permille (0-1000), _bp (0-10000).\n");
                _sb.Append("# Each value shows its valid range in [min..max]. Edits are checked strictly by the tests and\n");
                _sb.Append("# `dotnet run --project src/Deadswitch.Cli -- config check`. See docs/agents/balance-tuning.md.\n");
            }

            public void BeginSection(string name, string description)
            {
                _sb.Append('\n');
                _sb.Append("# ").Append(new string('-', 98)).Append('\n');
                _sb.Append("# ").Append(description).Append('\n');
                _sb.Append('[').Append(name).Append("]\n");
            }

            public void EndSection()
            {
            }

            public void Int(string key, ref int value, int min, int max, string description)
            {
                Comment(description + " [" + Range(min, max) + "]");
                _sb.Append(key).Append(" = ").Append(Num(value)).Append('\n');
            }

            public void Bool(string key, ref bool value, string description)
            {
                Comment(description + " [true|false]");
                _sb.Append(key).Append(" = ").Append(value ? "true" : "false").Append('\n');
            }

            public void IntList(string key, ref int[] values, int min, int max, int minCount, int maxCount, string description)
            {
                Comment(description + " [" + Num(minCount) + ".." + Num(maxCount) + " items, each " + Range(min, max) + "]");
                _sb.Append(key).Append(" = [");
                for (int i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                    {
                        _sb.Append(", ");
                    }

                    _sb.Append(Num(values[i]));
                }

                _sb.Append("]\n");
            }

            public override string ToString()
            {
                return _sb.ToString();
            }

            private void Comment(string text)
            {
                _sb.Append("\n# ").Append(text).Append('\n');
            }
        }
    }
}
