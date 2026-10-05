using Deadswitch.Game.Core;
using Deadswitch.Host.Narrative;
using Deadswitch.Sim.Events;
using UnityEngine;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// Connects the engine-agnostic <see cref="Advisor"/> (SPEC-004) to the game: feeds it live events, the offline
    /// catch-up summary and state checks, and hands the lines it picks to the ticker.
    /// </summary>
    public sealed class AdvisorVoice
    {
        /// <summary>Offline time shorter than this resumes silently (game minutes).</summary>
        private const long SummaryMinutes = 30;

        private const float StateCheckSeconds = 1f;

        private readonly GameHost _host;
        private readonly AdvisorTicker _ticker;
        private readonly Advisor _advisor;
        private const int HistoryLength = 6;

        private readonly System.Collections.Generic.List<string> _history = new System.Collections.Generic.List<string>();
        private float _stateClock;

        public AdvisorVoice(GameHost host, AdvisorTicker ticker)
        {
            _host = host;
            _ticker = ticker;
            var asset = Resources.Load<TextAsset>(AdvisorLines.ResourceName);
            AdvisorLines lines = AdvisorLines.Parse(asset != null ? asset.text : string.Empty);
            if (asset == null)
            {
                Debug.LogError("[DEADSWITCH] Advisor lines not found in Resources; the AI will be silent.");
            }

            foreach (string issue in lines.Issues)
            {
                Debug.LogWarning("[DEADSWITCH] Advisor lines: " + issue);
            }

            _advisor = new Advisor(lines);
            if (host.IsNewRun)
            {
                _advisor.Boot();
            }

            if (host.LastCatchUp.HasValue)
            {
                OnCaughtUp(host.LastCatchUp.Value);
            }

            host.EventRaised += OnEvent;
            host.CaughtUp += OnCaughtUp;
        }

        /// <summary>The most recent lines, oldest first (CORE transcript).</summary>
        public System.Collections.Generic.IReadOnlyList<string> History => _history;

        /// <summary>Holds lines back (e.g. during the prologue); queued lines play afterwards.</summary>
        public bool Paused { get; set; }

        /// <summary>A line for something the UI noticed (guide objectives).</summary>
        public void Notify(string trigger)
        {
            _advisor.Notify(trigger);
        }

        public void Dispose()
        {
            _host.EventRaised -= OnEvent;
            _host.CaughtUp -= OnCaughtUp;
        }

        public void Tick(float dt)
        {
            if (Paused)
            {
                return;
            }

            _stateClock += dt;
            if (_stateClock >= StateCheckSeconds)
            {
                _stateClock = 0f;
                _advisor.ObserveState(_host.Sim.State, _host.Sim.Config);
            }

            var line = _advisor.Update(dt);
            if (line != null)
            {
                _ticker.Say(line);
                _history.Add(line);
                if (_history.Count > HistoryLength)
                {
                    _history.RemoveAt(0);
                }
            }
        }

        private void OnEvent(SimEvent e)
        {
            _advisor.Observe(e, _host.Sim.State);
        }

        private void OnCaughtUp(CatchUpReport report)
        {
            // clock-cheat protection shows as the AI's own glitch (doc 08 s1, ADR-0004)
            if (report.Plan.Desync)
            {
                _advisor.Notify("time_desync");
            }
            else if (report.Plan.Capped)
            {
                _advisor.Notify("time_capped");
            }

            if (report.Plan.Minutes >= SummaryMinutes)
            {
                _advisor.ObserveCatchUp(_host.Sim.Log.Events, report.FirstEventIndex, report.LastEventIndex, _host.Sim.State);
            }
        }
    }
}
