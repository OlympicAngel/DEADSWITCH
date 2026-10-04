using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Deadswitch.Sim.Config
{
    /// <summary>One tunable as plain data: for diffs, debug panels and tooling.</summary>
    public readonly struct ConfigEntry
    {
        public ConfigEntry(string key, string value, string range, string description)
        {
            Key = key;
            Value = value;
            Range = range;
            Description = description;
        }

        /// <summary><c>section.key</c>.</summary>
        public string Key { get; }

        /// <summary>Value formatted exactly as in the balance file.</summary>
        public string Value { get; }

        public string Range { get; }

        public string Description { get; }
    }

    /// <summary>Flattens a config into an ordered list of entries (visit order).</summary>
    public static class ConfigEntries
    {
        public static List<ConfigEntry> Of(SimConfig config)
        {
            var v = new Collector();
            config.Visit(v);
            return v.Entries;
        }

        /// <summary>Entries whose value differs between two configs, in visit order. Returned values are from <paramref name="current"/>.</summary>
        public static List<KeyValuePair<ConfigEntry, ConfigEntry>> Diff(SimConfig baseline, SimConfig current)
        {
            List<ConfigEntry> a = Of(baseline);
            List<ConfigEntry> b = Of(current);
            var result = new List<KeyValuePair<ConfigEntry, ConfigEntry>>();
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].Value != b[i].Value)
                {
                    result.Add(new KeyValuePair<ConfigEntry, ConfigEntry>(a[i], b[i]));
                }
            }

            return result;
        }

        private sealed class Collector : IConfigVisitor
        {
            private string _section = string.Empty;

            public List<ConfigEntry> Entries { get; } = new List<ConfigEntry>();

            public void BeginSection(string name, string description)
            {
                _section = name;
            }

            public void EndSection()
            {
            }

            public void Int(string key, ref int value, int min, int max, string description)
            {
                Add(key, Num(value), Num(min) + ".." + Num(max), description);
            }

            public void Bool(string key, ref bool value, string description)
            {
                Add(key, value ? "true" : "false", "true|false", description);
            }

            public void IntList(string key, ref int[] values, int min, int max, int minCount, int maxCount, string description)
            {
                var sb = new StringBuilder("[");
                for (int i = 0; i < values.Length; i++)
                {
                    sb.Append(i > 0 ? ", " : string.Empty).Append(Num(values[i]));
                }

                Add(key, sb.Append(']').ToString(), Num(min) + ".." + Num(max), description);
            }

            private static string Num(int v)
            {
                return v.ToString(CultureInfo.InvariantCulture);
            }

            private void Add(string key, string value, string range, string description)
            {
                Entries.Add(new ConfigEntry(_section + "." + key, value, range, description));
            }
        }
    }
}
