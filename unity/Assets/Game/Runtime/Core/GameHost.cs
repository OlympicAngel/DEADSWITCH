using System;
using System.Collections.Generic;
using System.IO;
using Deadswitch.Host.Persistence;
using Deadswitch.Host.Timing;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using UnityEngine;

namespace Deadswitch.Game.Core
{
    /// <summary>Summary of what happened while the handler was away (drives the return report).</summary>
    public readonly struct CatchUpReport
    {
        public CatchUpReport(CatchUpPlan plan, int firstEventIndex, int lastEventIndex)
        {
            Plan = plan;
            FirstEventIndex = firstEventIndex;
            LastEventIndex = lastEventIndex;
        }

        public CatchUpPlan Plan { get; }

        /// <summary>Range of <c>Simulation.Log.Events</c> produced during catch-up: [first, last).</summary>
        public int FirstEventIndex { get; }

        public int LastEventIndex { get; }
    }

    /// <summary>
    /// Owns the running simulation (ADR-0002, ADR-0004): loads the balance file and save, catches up offline
    /// time, ticks in real time (1 tick = 1 real minute), saves crash-safely, and is the only path for player
    /// commands. Presentation reads <see cref="Sim"/> and listens to the events; it never mutates state.
    /// </summary>
    public sealed class GameHost : MonoBehaviour
    {
        private const float SecondsPerTick = 60f;

        private SaveFileStore _store;
        private SessionStampFile _stamp;
        private float _tickAccumulator;
        private float _autosaveTimer;
        private int _dispatchedEvents;
        private bool _paused;

        public static GameHost Instance { get; private set; }

        public Simulation Sim { get; private set; }

        public SimConfig Config { get; private set; }

        public GameSettings Settings { get; private set; }

        /// <summary>Problems found while loading the balance file or save (shown in diagnostics, never fatal).</summary>
        public List<string> BootIssues { get; } = new List<string>();

        /// <summary>0..1 progress toward the next tick, for smooth interpolation of displayed values.</summary>
        public float TickProgress => Mathf.Clamp01(_tickAccumulator / SecondsPerTick);

        /// <summary>Real seconds until the next tick at the current time scale.</summary>
        /// <summary>Real seconds until a future tick completes, at the current time scale.</summary>
        public double SecondsUntilTick(long tick)
        {
            long ticks = tick - Sim.State.Tick;
            return ticks <= 0 ? 0 : SecondsToNextTick + ((ticks - 1) * 60.0 / Settings.DevTimeScale);
        }

        public float SecondsToNextTick => Mathf.Max(0f, (SecondsPerTick - _tickAccumulator) / Settings.DevTimeScale);

        public event Action<SimEvent> EventRaised;

        public event Action Ticked;

        public event Action<CatchUpReport> CaughtUp;

        public event Action<Command, CommandResult> CommandExecuted;

        public bool IsReady => Sim != null;

        /// <summary>True when this session started a fresh run (no save).</summary>
        public bool IsNewRun { get; private set; }

        /// <summary>The last offline catch-up, for listeners that start after it ran.</summary>
        public CatchUpReport? LastCatchUp { get; private set; }

        public CommandResult Execute(Command command)
        {
            CommandResult result = Sim.Execute(command);
            CommandExecuted?.Invoke(command, result);
            DispatchEvents();
            if (result.Accepted)
            {
                Ticked?.Invoke();
            }

            return result;
        }

        /// <summary>Saves now (also called on pause and quit).</summary>
        public void SaveNow()
        {
            if (Sim == null)
            {
                return;
            }

            try
            {
                _store.Save(Sim);
                long now = NowMs();
                _stamp.Write(now - (long)(_tickAccumulator / Settings.DevTimeScale * 1000f));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.LogError("[DEADSWITCH] Save failed: " + ex.Message);
            }
        }

        /// <summary>Deletes the run and starts a new one. Callers confirm with the player first.</summary>
        public void StartNewRun()
        {
            _store.DeleteAll();
            NewRun();
            SaveNow();
            Ticked?.Invoke();
        }

        private static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Settings = GameSettings.Load();
            Config = LoadConfig();

            string dir = Application.persistentDataPath;
            _store = new SaveFileStore(Path.Combine(dir, GameInfo.SaveFileName));
            _stamp = new SessionStampFile(Path.Combine(dir, GameInfo.SessionFileName));

            SaveLoadReport report = _store.Load(Config);
            foreach (string problem in report.Problems)
            {
                BootIssues.Add("save: " + problem);
                Debug.LogWarning("[DEADSWITCH] " + problem);
            }

            if (report.Game != null)
            {
                Sim = report.Game.Simulation;
                if (report.Game.ConfigChanged)
                {
                    BootIssues.Add("balance changed since the last save");
                }
            }
            else
            {
                NewRun();
            }

            _dispatchedEvents = Sim.Log.Count;
        }

        private void Start()
        {
            CatchUp();
        }

        private void NewRun()
        {
            IsNewRun = true;
            ulong seed = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() ^ ((ulong)UnityEngine.Random.Range(int.MinValue, int.MaxValue) << 20);
            Sim = new Simulation(seed, Config);
            _dispatchedEvents = 0;
            _tickAccumulator = 0f;
            _stamp.Write(NowMs());
        }

        private SimConfig LoadConfig()
        {
            var asset = Resources.Load<TextAsset>(Path.GetFileNameWithoutExtension(BalanceText.FileName));
            if (asset == null)
            {
                BootIssues.Add("balance file missing; using code defaults");
                Debug.LogError("[DEADSWITCH] Balance file not found in Resources; using code defaults.");
                return SimConfig.Tier1();
            }

            BalanceReadResult result = BalanceText.Read(asset.text, BalanceReadMode.Lenient);
            foreach (ConfigIssue issue in result.Issues)
            {
                BootIssues.Add("balance: " + issue);
                Debug.LogWarning("[DEADSWITCH] Balance: " + issue);
            }

            return result.Config;
        }

        private void CatchUp()
        {
            Notifications.LocalAlerts.OnReturn();
            long now = NowMs();
            if (!_stamp.TryRead(out long last))
            {
                last = now;
            }

            CatchUpPlan plan = OfflineClock.Plan(last, now, Config.Host);
            int first = _dispatchedEvents;
            if (plan.Minutes > 0)
            {
                Sim.Run(plan.Minutes);
            }

            long next = OfflineClock.NextStamp(last, now, plan);
            _tickAccumulator = Mathf.Clamp((now - next) / 1000f, 0f, SecondsPerTick - 0.001f);
            _stamp.Write(next);

            if (Sim.State.Away)
            {
                Sim.Execute(Command.SetPresence(false));
            }

            int last2 = Sim.Log.Count;
            _dispatchedEvents = last2;
            LastCatchUp = new CatchUpReport(plan, first, last2);
            CaughtUp?.Invoke(LastCatchUp.Value);
            Ticked?.Invoke();
        }

        private void Update()
        {
            if (Sim == null || _paused)
            {
                return;
            }

            _tickAccumulator += Time.unscaledDeltaTime * Settings.DevTimeScale;
            int steps = 0;
            while (_tickAccumulator >= SecondsPerTick && steps < 600)
            {
                _tickAccumulator -= SecondsPerTick;
                Sim.Step();
                steps++;
            }

            if (steps > 0)
            {
                DispatchEvents();
                Ticked?.Invoke();
            }

            _autosaveTimer += Time.unscaledDeltaTime;
            if (_autosaveTimer >= Config.Host.AutosaveSeconds)
            {
                _autosaveTimer = 0f;
                SaveNow();
            }
        }

        private void DispatchEvents()
        {
            IReadOnlyList<SimEvent> events = Sim.Log.Events;
            while (_dispatchedEvents < events.Count)
            {
                SimEvent e = events[_dispatchedEvents++];
                EventRaised?.Invoke(e);
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (Sim == null || pause == _paused)
            {
                return;
            }

            _paused = pause;
            if (pause)
            {
                GoAway();
            }
            else
            {
                CatchUp();
            }
        }

        private void OnApplicationQuit()
        {
            if (Sim != null && !_paused)
            {
                GoAway();
            }
        }

        private void GoAway()
        {
            if (!Sim.State.Away)
            {
                Sim.Execute(Command.SetPresence(true));
            }

            SaveNow();
            Notifications.LocalAlerts.OnLeave(this);
        }
    }
}
