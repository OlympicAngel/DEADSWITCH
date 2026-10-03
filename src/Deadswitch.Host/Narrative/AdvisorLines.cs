using System;
using System.Collections.Generic;
using System.IO;

namespace Deadswitch.Host.Narrative
{
    public enum Tone
    {
        Neutral = 0,
        Warm = 1,
        Cold = 2,
        Bold = 3,
    }

    public sealed class AdvisorLine
    {
        public AdvisorLine(string id, string trigger, Tone tone, string text)
        {
            Id = id;
            Trigger = trigger;
            Tone = tone;
            Text = text;
        }

        public string Id { get; }

        public string Trigger { get; }

        public Tone Tone { get; }

        public string Text { get; }
    }

    /// <summary>
    /// The advisor's line table (SPEC-004 rule 7): <c>id | trigger | tone | text</c> per line, <c>#</c> comments.
    /// Shipped as <c>Resources/AdvisorLines.txt</c> (Unity: Resources.Load; CLI/tests: embedded resource).
    /// </summary>
    public sealed class AdvisorLines
    {
        public const string ResourceName = "AdvisorLines";

        private readonly Dictionary<string, List<AdvisorLine>> _byTrigger = new Dictionary<string, List<AdvisorLine>>(StringComparer.Ordinal);

        private AdvisorLines(List<AdvisorLine> lines, List<string> issues)
        {
            All = lines;
            Issues = issues;
            foreach (AdvisorLine line in lines)
            {
                if (!_byTrigger.TryGetValue(line.Trigger, out List<AdvisorLine>? list))
                {
                    list = new List<AdvisorLine>();
                    _byTrigger.Add(line.Trigger, list);
                }

                list.Add(line);
            }
        }

        public IReadOnlyList<AdvisorLine> All { get; }

        /// <summary>Malformed lines (skipped), with line numbers. Empty when the file is clean.</summary>
        public IReadOnlyList<string> Issues { get; }

        public IReadOnlyList<AdvisorLine> For(string trigger)
        {
            return _byTrigger.TryGetValue(trigger, out List<AdvisorLine>? list) ? list : (IReadOnlyList<AdvisorLine>)Array.Empty<AdvisorLine>();
        }

        public static AdvisorLines Parse(string text)
        {
            var lines = new List<AdvisorLine>();
            var issues = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            string[] rows = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < rows.Length; i++)
            {
                string row = rows[i].Trim();
                if (row.Length == 0 || row[0] == '#')
                {
                    continue;
                }

                string[] cells = row.Split(new[] { '|' }, 4);
                if (cells.Length != 4)
                {
                    issues.Add("line " + (i + 1) + ": expected 'id | trigger | tone | text'");
                    continue;
                }

                string id = cells[0].Trim();
                string trigger = cells[1].Trim();
                string body = cells[3].Trim();
                if (!Enum.TryParse(cells[2].Trim(), true, out Tone tone) || id.Length == 0 || trigger.Length == 0 || body.Length == 0)
                {
                    issues.Add("line " + (i + 1) + ": bad id, trigger, tone or text");
                    continue;
                }

                if (!ids.Add(id))
                {
                    issues.Add("line " + (i + 1) + ": duplicate id '" + id + "'");
                    continue;
                }

                lines.Add(new AdvisorLine(id, trigger, tone, body));
            }

            return new AdvisorLines(lines, issues);
        }

        /// <summary>The shipped table embedded in this assembly (CLI and tests), or null when built without it (Unity).</summary>
        public static AdvisorLines? LoadEmbedded()
        {
            using (Stream? s = typeof(AdvisorLines).Assembly.GetManifestResourceStream("Deadswitch.Host." + ResourceName + ".txt"))
            {
                if (s == null)
                {
                    return null;
                }

                using (var reader = new StreamReader(s))
                {
                    return Parse(reader.ReadToEnd());
                }
            }
        }
    }
}
