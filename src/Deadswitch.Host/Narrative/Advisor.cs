using System.Collections.Generic;
using System.Text;
using Deadswitch.Sim;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Host.Narrative
{
    /// <summary>
    /// The AI's voice (SPEC-004 rule 7): turns sim events into short lines from <see cref="AdvisorLines"/>, shaded
    /// by the hidden dials, paced for a one-line ticker. Engine-agnostic: the host feeds events and real time and
    /// shows whatever <see cref="Update"/> returns. Lines only quote numbers the events carry.
    /// </summary>
    public sealed class Advisor
    {
        /// <summary>Minimum seconds a line stays before a normal line replaces it (urgent lines may interrupt).</summary>
        public const float MinHoldSeconds = 4.5f;

        /// <summary>Quiet seconds before an ambient line.</summary>
        public const float IdleSeconds = 45f;

        /// <summary>Normal lines older than this are dropped (the moment has passed).</summary>
        public const float StaleSeconds = 20f;

        /// <summary>Every trigger the advisor can raise; each needs at least one neutral line.</summary>
        public static readonly string[] Triggers =
        {
            "boot", "return", "idle", "raid_warning", "raid_repelled", "raid_breached", "raid_missed", "raid_lockdown",
            "mercy", "lie_deflect", "low_energy", "shed", "restored", "blackout", "blackout_end", "build_started",
            "build_done", "build_cancelled", "demolished", "band_glitchy", "band_unstable", "band_critical", "band_down",
            "override", "delegation_manual", "delegation_delegated", "delegation_autopilot", "ai_build", "ai_defend",
        };

        private const int MaxQueue = 4;

        private readonly AdvisorLines _lines;
        private readonly List<Pending> _queue = new List<Pending>();
        private readonly Dictionary<string, int> _cursor = new Dictionary<string, int>();
        private readonly HashSet<int> _liedRaids = new HashSet<int>();
        private readonly Dictionary<int, int> _contactGate = new Dictionary<int, int>();
        private Pending? _breach;
        private int _warnMinutes;
        private int _warnEstimate;
        private float _sinceShown;
        private Priority _shownPriority;
        private string _lastId = string.Empty;
        private bool _lowEnergyArmed = true;
        private HashSet<Tone> _tones = new HashSet<Tone> { Tone.Neutral, Tone.Warm };

        public Advisor(AdvisorLines lines)
        {
            _lines = lines;
        }

        private enum Priority
        {
            Ambient = 0,
            Normal = 1,
            Urgent = 2,
        }

        /// <summary>Id of the last line returned (for tests and the transcript).</summary>
        public string LastId => _lastId;

        /// <summary>A new run plays the boot lines in order.</summary>
        public void Boot()
        {
            foreach (AdvisorLine line in _lines.For("boot"))
            {
                _queue.Add(new Pending("boot", Priority.Urgent) { FixedId = line.Id });
            }
        }

        /// <summary>Reads one live event.</summary>
        public void Observe(SimEvent e, GameState state)
        {
            _tones = TonesFor(state);
            On(e);
        }

        /// <summary>
        /// Offline catch-up (events [<paramref name="from"/>, <paramref name="to"/>)): summarized in one return line
        /// instead of replaying every event.
        /// </summary>
        public void ObserveCatchUp(IReadOnlyList<SimEvent> events, int from, int to, GameState state)
        {
            _tones = TonesFor(state);
            int raids = 0;
            for (int i = from; i < to && i < events.Count; i++)
            {
                if (events[i].Kind == EventKind.RaidResolved)
                {
                    raids++;
                }
            }

            _queue.Clear();
            _breach = null;
            _liedRaids.Clear();
            _contactGate.Clear();
            Enqueue(new Pending("return", Priority.Urgent).With("raids", raids.ToString()));
        }

        /// <summary>State-driven lines (low energy, with hysteresis).</summary>
        public void ObserveState(GameState state, SimConfig config)
        {
            int cap = Economy.EnergyCap(state, config);
            bool low = state.Energy * 100 < cap * 15 && Economy.Flows(state, config).NetEnergyPerHour < 0;
            if (low && _lowEnergyArmed)
            {
                _lowEnergyArmed = false;
                Enqueue(new Pending("low_energy", Priority.Normal));
            }
            else if (state.Energy * 100 > cap * 30)
            {
                _lowEnergyArmed = true;
            }
        }

        /// <summary>Advances real time; returns the next line to show, or null to keep the current one.</summary>
        public string? Update(float dt)
        {
            _sinceShown += dt;
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                _queue[i].Age += dt;
                if (_queue[i].Priority != Priority.Urgent && _queue[i].Age > StaleSeconds)
                {
                    _queue.RemoveAt(i);
                }
            }

            if (_queue.Count > 0)
            {
                int pick = 0;
                for (int i = 1; i < _queue.Count; i++)
                {
                    if (_queue[i].Priority > _queue[pick].Priority)
                    {
                        pick = i;
                    }
                }

                Pending p = _queue[pick];
                bool interrupt = p.Priority == Priority.Urgent && _shownPriority != Priority.Urgent;
                if (_sinceShown < MinHoldSeconds && !interrupt)
                {
                    return null;
                }

                _queue.RemoveAt(pick);
                if (p == _breach)
                {
                    _breach = null;
                }

                string? text = Say(p);
                if (text != null)
                {
                    return text;
                }
            }

            if (_queue.Count == 0 && _sinceShown >= IdleSeconds)
            {
                return Say(new Pending("idle", Priority.Ambient));
            }

            return null;
        }

        private void On(SimEvent e)
        {
            switch (e.Kind)
            {
                case EventKind.RaidWarning:
                    _warnMinutes = e.B;
                    _warnEstimate = e.C;
                    break;
                case EventKind.RaidVector:
                    Enqueue(new Pending("raid_warning", Priority.Urgent)
                        .With("gate", Names.Gate((RaidGate)e.B))
                        .With("est", _warnEstimate.ToString())
                        .With("min", _warnMinutes.ToString()));
                    break;
                case EventKind.AdvisorLied:
                    if (e.A == (int)LieKind.RaidGate)
                    {
                        _liedRaids.Add(e.B);
                    }

                    break;
                case EventKind.RaidContact:
                    _contactGate[e.A] = e.B;
                    break;
                case EventKind.RaidResolved:
                    OnResolved(e);
                    break;
                case EventKind.LossLine:
                    if (_breach != null)
                    {
                        string part = "-" + e.C + " " + ((LossResource)e.B).ToString().ToUpperInvariant();
                        _breach.Vars["lost"] = _breach.Vars.TryGetValue("lost", out string? so) ? so + ", " + part : part;
                    }

                    break;
                case EventKind.MercyStarted:
                    Enqueue(new Pending("mercy", Priority.Normal));
                    break;
                case EventKind.BlackoutStarted:
                    Enqueue(new Pending("blackout", Priority.Urgent));
                    break;
                case EventKind.BlackoutEnded:
                    Enqueue(new Pending("blackout_end", Priority.Normal));
                    break;
                case EventKind.FacilityShed:
                    Enqueue(new Pending("shed", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.B)));
                    break;
                case EventKind.FacilityRestored:
                    Enqueue(new Pending("restored", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.B)));
                    break;
                case EventKind.BuildStarted:
                    Enqueue(new Pending("build_started", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.B)).With("level", e.C.ToString()));
                    break;
                case EventKind.BuildCompleted:
                    Enqueue(new Pending("build_done", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.B)).With("level", e.C.ToString()));
                    break;
                case EventKind.BuildCancelled:
                    Enqueue(new Pending("build_cancelled", Priority.Normal));
                    break;
                case EventKind.FacilityDemolished:
                    Enqueue(new Pending("demolished", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.B)));
                    break;
                case EventKind.CorruptionBandChanged:
                    if (e.A > e.B)
                    {
                        string[] up = { string.Empty, "band_glitchy", "band_unstable", "band_critical" };
                        Enqueue(new Pending(up[System.Math.Min(e.A, 3)], e.A >= 3 ? Priority.Urgent : Priority.Normal));
                    }
                    else
                    {
                        Enqueue(new Pending("band_down", Priority.Normal));
                    }

                    break;
                case EventKind.OverrideUsed:
                    Enqueue(new Pending("override", Priority.Normal));
                    break;
                case EventKind.DelegationChanged:
                    string[] levels = { "delegation_manual", "delegation_delegated", "delegation_autopilot" };
                    if (e.A >= 0 && e.A < levels.Length)
                    {
                        Enqueue(new Pending(levels[e.A], Priority.Normal));
                    }

                    break;
                case EventKind.AiActed:
                    if (e.A == (int)AiActionKind.Build)
                    {
                        _queue.RemoveAll(p => p.Trigger == "build_started");
                        Enqueue(new Pending("ai_build", Priority.Normal).With("kind", Names.Facility((FacilityKind)e.C)).With("level", e.D.ToString()));
                    }
                    else if (e.A == (int)AiActionKind.Defend)
                    {
                        Enqueue(new Pending("ai_defend", Priority.Urgent).With("posture", Names.Posture((Posture)e.B)));
                    }

                    break;
            }
        }

        private void OnResolved(SimEvent e)
        {
            // Resolution lines name the gate the raid really used: the on-screen cross-check (lie rule 2).
            string hit = _contactGate.TryGetValue(e.A, out int contact) ? Names.Gate((RaidGate)contact) : string.Empty;
            switch ((RaidOutcome)e.B)
            {
                case RaidOutcome.Repelled:
                    Enqueue(WithGate(new Pending("raid_repelled", Priority.Urgent), hit));
                    break;
                case RaidOutcome.Breached:
                    _breach = WithGate(new Pending("raid_breached", Priority.Urgent), hit);
                    Enqueue(_breach);
                    break;
                case RaidOutcome.Missed:
                    Enqueue(WithGate(new Pending("raid_missed", Priority.Urgent), hit));
                    break;
                case RaidOutcome.Lockdown:
                    Enqueue(new Pending("raid_lockdown", Priority.Normal));
                    break;
            }

            // The lie is exposed by contact; the next line deflects and never admits intent.
            if (_liedRaids.Remove(e.A) && _contactGate.TryGetValue(e.A, out int gate))
            {
                Enqueue(new Pending("lie_deflect", Priority.Urgent).With("gate", Names.Gate((RaidGate)gate)));
            }

            _contactGate.Remove(e.A);
        }

        private static Pending WithGate(Pending p, string gate)
        {
            return gate.Length > 0 ? p.With("gate", gate) : p;
        }

        private void Enqueue(Pending p)
        {
            // Coalesce repeats of the same trigger (several sheds at once): keep the latest values.
            for (int i = 0; i < _queue.Count; i++)
            {
                if (_queue[i].Trigger == p.Trigger && _queue[i].FixedId == null && p.Trigger != "raid_breached")
                {
                    _queue[i] = p;
                    return;
                }
            }

            if (_queue.Count >= MaxQueue)
            {
                int drop = -1;
                for (int i = 0; i < _queue.Count; i++)
                {
                    if (_queue[i].Priority < p.Priority && (drop < 0 || _queue[i].Priority < _queue[drop].Priority))
                    {
                        drop = i;
                    }
                }

                if (drop < 0)
                {
                    return;
                }

                _queue.RemoveAt(drop);
            }

            _queue.Add(p);
        }

        private string? Say(Pending p)
        {
            AdvisorLine? line = p.FixedId != null ? Find(p.FixedId) : Choose(p);
            if (line == null)
            {
                return null;
            }

            _lastId = line.Id;
            _sinceShown = 0f;
            _shownPriority = p.Priority;
            return Format(line.Text, p.Vars);
        }

        private AdvisorLine? Find(string id)
        {
            foreach (AdvisorLine line in _lines.All)
            {
                if (line.Id == id)
                {
                    return line;
                }
            }

            return null;
        }

        /// <summary>Rotates through the neutral lines and those of the active tones, skipping lines with unknown placeholders.</summary>
        private AdvisorLine? Choose(Pending p)
        {
            var pool = new List<AdvisorLine>();
            foreach (AdvisorLine line in _lines.For(p.Trigger))
            {
                if (_tones.Contains(line.Tone) && HasAll(line.Text, p.Vars))
                {
                    pool.Add(line);
                }
            }

            if (pool.Count == 0)
            {
                return null;
            }

            _cursor.TryGetValue(p.Trigger, out int k);
            AdvisorLine pick = pool[k % pool.Count];
            if (pick.Id == _lastId && pool.Count > 1)
            {
                k++;
                pick = pool[k % pool.Count];
            }

            _cursor[p.Trigger] = k + 1;
            return pick;
        }

        private static HashSet<Tone> TonesFor(GameState s)
        {
            var tones = new HashSet<Tone> { Tone.Neutral };
            if (s.ColdnessMilli >= 50_000)
            {
                tones.Add(Tone.Cold);
            }

            if (s.BoldnessMilli >= 50_000)
            {
                tones.Add(Tone.Bold);
            }

            if (s.ColdnessMilli < 25_000 && s.BoldnessMilli < 25_000)
            {
                tones.Add(Tone.Warm);
            }

            return tones;
        }

        private static bool HasAll(string text, Dictionary<string, string> vars)
        {
            int i = 0;
            while ((i = text.IndexOf('{', i)) >= 0)
            {
                int end = text.IndexOf('}', i);
                if (end < 0 || !vars.ContainsKey(text.Substring(i + 1, end - i - 1)))
                {
                    return false;
                }

                i = end;
            }

            return true;
        }

        private static string Format(string text, Dictionary<string, string> vars)
        {
            var sb = new StringBuilder(text);
            foreach (KeyValuePair<string, string> kv in vars)
            {
                sb.Replace("{" + kv.Key + "}", kv.Value);
            }

            return sb.ToString();
        }

        private sealed class Pending
        {
            public Pending(string trigger, Priority priority)
            {
                Trigger = trigger;
                Priority = priority;
            }

            public string Trigger { get; }

            public Priority Priority { get; }

            public string? FixedId { get; set; }

            public float Age { get; set; }

            public Dictionary<string, string> Vars { get; } = new Dictionary<string, string>();

            public Pending With(string key, string value)
            {
                Vars[key] = value;
                return this;
            }
        }
    }
}
