using System.Linq;
using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Game.UI.Screens;
using Deadswitch.Host.Narrative;
using Deadswitch.Host.Reports;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// Binds the terminal HUD (Hud.uxml) to the sim: always-visible essentials (doc 08 s4), the raid banner
    /// (doc 10 s4: amber + diamond + countdown), OVERRIDE charges, the advisor line and the command bar.
    /// Reads state only; actions go through <see cref="GameHost.Execute"/>.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        private GameHost _host;
        private UiRoot _ui;
        private VisualElement _hud;

        private readonly System.Collections.Generic.List<ResourcePod> _pods = new System.Collections.Generic.List<ResourcePod>();
        private ResourceSheet _resourceSheet;
        private AwaySummary _away;
        private CommandMenu _menu;
        private QueueSheet _queue;
        private AnimatedNumber _core;
        private AiOrb _orb;
        private AiWave _wave;
        private VisualElement _advisorChips;
        private Label _advisorMood;
        private VisualElement _frame;
        private VisualElement _alarm;
        private VisualElement _job;
        private VisualElement _jobBar;
        private Label _jobCount;
        private string _chipKey = string.Empty;
        private float _speak;
        private float _alarmClock;
        private float _readClock;
        private bool _raidOpen;
        private bool _raidAutoOpened;
        private VisualElement _advisorPanel;
        private Label _coreBand;
        private Label _coreValue;
        private Label _clock;
        private Label _nextLabel;
        private Label _nextTime;
        private Label _raidTime;
        private Label _raidEstimate;
        private VisualElement _raid;
        private VisualElement _raidDetail;
        private VisualElement _coreDot;
        private VisualElement _coreGaugeEl;
        private VisualElement[] _overridePips;
        private ArcGauge _gauge;
        private CorruptionBand _band = (CorruptionBand)(-1);

        public static HudController Instance { get; private set; }

        public AdvisorTicker Advisor { get; private set; }

        private AdvisorVoice _voice;
        private CorruptionBand _trueBand = (CorruptionBand)(-1);
        private ReportScreen _report;
        private Base.BaseScreen _baseScreen;
        private OpeningFlow _opening;
        private VisualElement _reportChip;
        private VisualElement _dispatchChip;
        private int _chipRaid;

        public ScreenRouter Router { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _host = GameHost.Instance;
            _ui = UiRoot.Instance;

            TemplateContainer tree = UiRoot.Load("Hud");
            _hud = tree;
            _ui.Hud.Add(tree);
            Icons.Attach(tree);

            _resourceSheet = new ResourceSheet(_ui.Sheets, OnHelp, slot => FocusSlot(slot, FacilityKind.None));
            _pods.Add(new ResourcePod(_hud, "energy", ResKind.Energy, ToggleResource));
            _pods.Add(new ResourcePod(_hud, "compute", ResKind.Compute, ToggleResource));
            _pods.Add(new ResourcePod(_hud, "people", ResKind.People, ToggleResource));
            _pods.Add(new ResourcePod(_hud, "fuel", ResKind.Fuel, ToggleResource));
            _core = new AnimatedNumber(Q<Label>("core-value"), Fmt.Num);
            _orb = new AiOrb(Q<VisualElement>("advisor-orb"));
            _orb.Assemble();
            _wave = new AiWave(Q<VisualElement>("advisor-wave"));
            _advisorChips = Q<VisualElement>("advisor-chips");
            _advisorChips.Clear();
            _advisorMood = Q<Label>("advisor-mood");
            _frame = Q<VisualElement>("frame");
            _alarm = Q<VisualElement>("frame-alarm");
            _job = Q<VisualElement>("next-timer");
            _jobBar = Q<VisualElement>("next-bar");
            _jobCount = Q<Label>("next-count");
            _job.RegisterCallback<ClickEvent>(_ => _queue.Toggle());
            _advisorPanel = Q<VisualElement>("advisor");
            Q<VisualElement>("advisor-orb").RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                Router.Show("core");
            });
            _advisorPanel.RegisterCallback<ClickEvent>(_ => ExpandComms());
            Q<VisualElement>("raid-banner").RegisterCallback<ClickEvent>(_ => SetRaidOpen(!_raidOpen));
            Toasts.Mount(_ui.Hud);
            _away = new AwaySummary(_ui.Sheets);
            Hints.Mount(_ui.Sheets);
            Hints.Attach(Q<VisualElement>("energy-cell"), "ENERGY", "Everything runs on it. Value / storage, net change per hour, and when it fills or runs dry. Tap for sources, drains and how to get more.");
            Hints.Attach(Q<VisualElement>("compute-cell"), "COMPUTE", "What I think with: research, audits, hacks. Heavy use corrupts me. Tap for the breakdown.");
            Hints.Attach(Q<VisualElement>("people-cell"), "PEOPLE", "Survivors / room for them, and crew on duty. Unstaffed facilities run on me, at lower output. Tap for more.");
            Hints.Attach(Q<VisualElement>("fuel-cell"), "FUEL", "Raids, vehicles and the reactor burn it. It comes from the map. Tap for the breakdown.");
            Hints.Attach(Q<VisualElement>("core-cell"), "CORE CORRUPTION", "What I report about my own state. Audit me in CORE to see what I am not telling you.");
            Hints.Attach(Q<VisualElement>("heat-chip"), "HEAT", "The faction watching us hardest. More heat, bigger and more frequent attacks.");
            Hints.Attach(Q<VisualElement>("override-pips").parent, "OVERRIDE", "Charges to force my hand: lockdowns, silencing me, cancelling what I do.");
            if (_host.LastCatchUp.HasValue)
            {
                _away.Show(_host, _host.LastCatchUp.Value);
            }

            _host.CaughtUp += OnCaughtUp;
            _coreBand = Q<Label>("core-band");
            _coreValue = Q<Label>("core-value");
            _clock = Q<Label>("clock");
            _nextLabel = Q<Label>("next-label");
            _nextTime = Q<Label>("next-time");
            _raid = Q<VisualElement>("raid-banner");
            _raidDetail = Q<VisualElement>("raid-detail");
            _coreDot = Q<VisualElement>("core-dot");
            _raidTime = Q<Label>("raid-time");
            _raidEstimate = Q<Label>("raid-estimate");
            _coreGaugeEl = Q<VisualElement>("core-gauge");
            _gauge = new ArcGauge(_coreGaugeEl);
            _overridePips = Q<VisualElement>("override-pips").Children().ToArray();

            Advisor = new AdvisorTicker(Q<Label>("advisor-text"));
            Advisor.Spoke += _ =>
            {
                _orb.Ping();
                ExpandComms();
            };
            _voice = new AdvisorVoice(_host, Advisor);

            Router = new ScreenRouter(Q<VisualElement>("screen"));
            _baseScreen = new Base.BaseScreen(Router);
            Router.Register(_baseScreen);
            _report = new ReportScreen(Router);
            Router.Register(_report);
            Router.Register(new OpsScreen(OpenReport));
            var season = new SeasonScreen(Router);
            Router.Register(season);
            var premium = new PremiumScreen(Router, () =>
            {
                season.ReturnTo("premium");
                Router.Show("season");
            });
            Router.Register(premium);
            Router.Register(new LegacyScreen(Router));
            Records.Hook(_host);
            Router.Register(new StoryScreen(Router));
            Router.Register(new CoreScreen(() => _voice.History, () => Router.Show("settings"), () =>
            {
                premium.ReturnTo("core");
                Router.Show("premium");
            }, () => Router.Show("legacy"), () => Router.Show("story")));
            Router.Register(new SettingsScreen(Router, () =>
            {
                premium.ReturnTo("settings");
                Router.Show("premium");
            }));
            Store.Theme.Apply(UiRoot.Instance.Root);
            Router.Register(new WorkforceScreen(Router));
            _reportChip = Q<VisualElement>("report-chip");
            _reportChip.RegisterCallback<ClickEvent>(_ => OpenReport(_chipRaid));
            Router.Register(new MapScreen());
            Router.Register(new DispatchScreen(Router));
            Router.Register(new BattleScreen(Router));
            Q<VisualElement>("raid-command").RegisterCallback<ClickEvent>(_ => _host.Execute(Command.TakeCommand(!_host.Sim.State.BattleLive)));
            _dispatchChip = Q<VisualElement>("dispatch-chip");
            _dispatchChip.RegisterCallback<ClickEvent>(_ => Router.Show("dispatch"));
            Q<Label>("feed-id").text = "DRONE_RECON_" + ((_host.Sim.Seed % 89) + 10).ToString(System.Globalization.CultureInfo.InvariantCulture);
            Q<VisualElement>("heat-chip").RegisterCallback<ClickEvent>(_ => Router.Show("map"));
            // back (SPEC-042): a secondary screen or another tab returns to BASE; layers above register later
            Back.Install(_ui.Root);
            Back.Register(() =>
            {
                if (Router.Current == null || Router.Current == "base")
                {
                    return false;
                }

                Router.Show("base");
                return true;
            });
            Back.Register(() => _baseScreen.CloseFocus());
            Back.Register(() =>
            {
                if (!_resourceSheet.IsOpen)
                {
                    return false;
                }

                _resourceSheet.Close();
                return true;
            });
            BuildMenu(premium, season);
            _queue = new QueueSheet(_ui.Sheets, slot => FocusSlot(slot, FacilityKind.None));
            Router.BindTab("base", Q<VisualElement>("tab-base"));
            Router.BindTab("map", Q<VisualElement>("tab-map"));
            Router.BindTab("core", Q<VisualElement>("tab-core"));
            Router.BindTab("ops", Q<VisualElement>("tab-ops"));
            Q<VisualElement>("raid-defend").RegisterCallback<ClickEvent>(_ => Router.Show("ops"));
            Q<VisualElement>("raid-apply").RegisterCallback<ClickEvent>(_ => ApplyAiPlan());
            // The drone-feed frame belongs to BASE; over the other screens its text collides with their headers.
            Router.Changed += id =>
            {
                _frame.EnableInClassList("is-covered", id != "base");
                _resourceSheet.Close();
                MoveTabRail(id, true);
            };
            Q<VisualElement>("tabbar").RegisterCallback<GeometryChangedEvent>(_ => MoveTabRail(Router.Current, false));
            Router.Show("base");
            _opening = new OpeningFlow(_host, _hud, _voice, _baseScreen);

            _host.Ticked += Refresh;
            _host.EventRaised += OnSimEvent;
            _ui.Frame += OnFrame;
            RefreshPods(true);
            Refresh();
            _core.Set(CorruptionSystem.Percent(ProjectSystem.ReportedCorruptionMilli(_host.Sim.State, _host.Sim.Config)), true);
        }

        private void OnDestroy()
        {
            if (_host != null)
            {
                _host.Ticked -= Refresh;
                _host.EventRaised -= OnSimEvent;
                _host.CaughtUp -= OnCaughtUp;
            }

            _voice?.Dispose();

            if (_ui != null)
            {
                _ui.Frame -= OnFrame;
            }
        }

        private T Q<T>(string name)
            where T : VisualElement
        {
            return _hud.Q<T>(name);
        }

        /// <summary>Opens the after-action report for a raid and clears the HUD chip.</summary>
        public void OpenReport(int raidId)
        {
            if (raidId <= 0)
            {
                return;
            }

            _reportChip.AddToClassList("is-hidden");
            _report.Open(raidId);
        }

        private void OnSimEvent(SimEvent e)
        {
            if (e.Kind == EventKind.RaidResolved)
            {
                _chipRaid = e.A;
                _reportChip.Q<Label>("report-chip-label").text = "AFTER-ACTION // " + (BattleReport.Build(_host.Sim.Log.Events, e.A) is BattleReport br ? Names.Attack(br.Kind) : "RAID") + " " + e.A;
                _reportChip.RemoveFromClassList("is-hidden");
            }

            if (e.Kind == EventKind.CycleEnded)
            {
                // a new site (SPEC-022): show what the core carried
                Router.Show("legacy");
            }

            if ((e.Kind == EventKind.ChapterOpened && e.A >= 2) || e.Kind == EventKind.ChapterClosed)
            {
                // a chapter's title card or its payoff (SPEC-024), never over an incoming attack
                if (_host.Sim.State.RaidId == 0)
                {
                    Router.Show("story");
                }
            }

            if (e.Kind == EventKind.BattleStarted)
            {
                // contact with the handler in command: open the fight (SPEC-020)
                Feedback.Alert();
                Router.Show("battle");
            }

            if (e.Kind == EventKind.UltimatumIssued || e.Kind == EventKind.DilemmaOffered)
            {
                Feedback.Alert();
            }

            if (e.Kind == EventKind.RaidWarning)
            {
                Feedback.Signature((AttackKind)e.D);
                Motion.To(_raid, 0.42f, Ease.OutBack, t => _raid.style.scale = new Scale(new Vector3(0.85f + (0.15f * t), 0.85f + (0.15f * t), 1f)));
            }
        }

        private void Refresh()
        {
            Simulation sim = _host.Sim;
            GameState s = sim.State;
            SimConfig c = sim.Config;
            EconomyFlows f = Economy.Flows(s, c);

            RefreshPods(false);
            _resourceSheet.Refresh();
            _queue?.Refresh();
            RefreshComms(s, c);
            RefreshBadges(s, c);

            // The readout is what the core reports (a bold AI under-reports, SPEC-007); the glitch is the true band.
            int reported = ProjectSystem.ReportedCorruptionMilli(s, c);
            int pct = CorruptionSystem.Percent(reported);
            _core.Set(pct);
            _gauge.Set(pct / 100f);
            CorruptionBand band = CorruptionSystem.Band(c, reported);
            CorruptionBand trueBand = CorruptionSystem.Band(c, s.CorruptionMilli);
            if (trueBand != _trueBand)
            {
                _trueBand = trueBand;
                float weight = GlitchText.BandWeight((int)trueBand) * _host.Settings.Effects;
                Advisor.SetGlitch(weight);
                _ui.SetGlitch(weight);
                _orb.Stutter = weight;

            }

            if (band != _band)
            {
                _band = band;
                _coreBand.text = Fmt.BandName(band);
                string tone = band == CorruptionBand.Stable ? "t-phosphor" : (band == CorruptionBand.Glitchy ? "t-amber" : "t-red");
                SetTone(_coreBand, tone);
                SetTone(_coreValue, tone);
                _coreGaugeEl.EnableInClassList("ds-gauge--amber", band == CorruptionBand.Glitchy);
                _coreGaugeEl.EnableInClassList("ds-gauge--red", band >= CorruptionBand.Unstable);
                _coreDot.EnableInClassList("is-hidden", band == CorruptionBand.Stable);
                // the orb wears the band the core reports; its stutter follows the true band (like the glitch)
                VisualElement orbEl = Q<VisualElement>("advisor-orb");
                orbEl.EnableInClassList("ai-orb--amber", band == CorruptionBand.Glitchy);
                orbEl.EnableInClassList("ai-orb--red", band >= CorruptionBand.Unstable);
            }

            _clock.text = Fmt.Clock(s.Tick);
            Faction hot = WorldSystem.Hottest(s);
            HeatLevel heat = WorldSystem.Level(s.Heat[(int)hot]);
            VisualElement heatChip = Q<VisualElement>("heat-chip");
            heatChip.EnableInClassList("hud-heat--watched", heat == HeatLevel.Watched);
            heatChip.EnableInClassList("hud-heat--hunted", heat == HeatLevel.Hunted);
            heatChip.EnableInClassList("hud-heat--marked", heat == HeatLevel.Marked);
            Q<Label>("heat-text").text = "HEAT // " + heat.ToString().ToUpperInvariant() + (heat == HeatLevel.Cold ? string.Empty : " // " + Names.Faction(hot));

            for (int i = 0; i < _overridePips.Length; i++)
            {
                bool visible = i < OverrideSystem.MaxCharges(s, c);
                _overridePips[i].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                _overridePips[i].EnableInClassList("is-on", i < s.OverrideCharges && s.Tick >= s.OverrideCooldownUntil);
                _overridePips[i].EnableInClassList("is-cooldown", i < s.OverrideCharges && s.Tick < s.OverrideCooldownUntil);
            }

            _coreDot.EnableInClassList("is-hidden", band == CorruptionBand.Stable && s.ClimaxAtTick == 0);
            bool raid = s.RaidId != 0;
            _raid.EnableInClassList("is-hidden", !raid);
            if (!raid && (_raidOpen || _raidAutoOpened))
            {
                _raidAutoOpened = false;
                SetRaidOpen(false);
            }
            // signature reads at a glance (doc 10 s4): raid amber diamond, siege red square, purge red diamond
            bool red = s.RaidKind != AttackKind.Raid;
            _raid.EnableInClassList("hud-alert--red", red);
            Q<VisualElement>("raid-pip").EnableInClassList("ds-pip--amber", !red);
            Q<VisualElement>("raid-pip").EnableInClassList("ds-pip--red", red);
            Q<VisualElement>("raid-pip").EnableInClassList("ds-pip--diamond", s.RaidKind != AttackKind.Siege);
            Q<Label>("raid-title").text = Names.Attack(s.RaidKind) + " INCOMING";
            _raidDetail.EnableInClassList("is-hidden", !raid);
            Q<VisualElement>("raid-command").EnableInClassList("is-hidden", !raid || s.BattleEndTick != 0);
            Q<VisualElement>("raid-command").EnableInClassList("is-on", s.BattleLive);
            Q<Label>("raid-command-label").text = s.BattleLive ? "LIVE // ON" : "TAKE COMMAND";
            if (raid)
            {
                _raidEstimate.text = (s.RaidGateReported == RaidGate.None ? "?" : Names.Gate(s.RaidGateReported)) + " // EST " + (s.RaidEstimate > 0 ? Fmt.Num(s.RaidEstimate) : "?") + " // DEF " + Fmt.Num(Defense.Rating(s, c));
                RefreshPlan(s, c);
            }

            // dispatch chip (F-034): an ultimatum outranks a dilemma
            bool ultimatum = s.Ultimatum == UltimatumStage.Issued;
            bool dilemma = s.Dilemma != DilemmaKind.None;
            _dispatchChip.EnableInClassList("is-hidden", !ultimatum && !dilemma);
            _dispatchChip.EnableInClassList("hud-dispatch--red", ultimatum);
            Q<VisualElement>("dispatch-pip").EnableInClassList("ds-pip--red", ultimatum);
            Q<VisualElement>("dispatch-pip").EnableInClassList("ds-pip--amber", !ultimatum);
            Q<Label>("dispatch-title").text = ultimatum ? "WARLORD ULTIMATUM" : "DILEMMA // " + LivingTexts.DilemmaName(s.Dilemma);

            UpdateTimers();
        }

        private void OnFrame(float dt)
        {
            if (_host == null || !_host.IsReady)
            {
                return;
            }

            foreach (ResourcePod pod in _pods)
            {
                pod.Tick(dt);
            }

            _speak = Advisor.Typing ? 1f : Mathf.Max(0f, _speak - (dt * 1.5f));
            _orb.Voice = _speak;
            _orb.Tick(dt);
            _wave.Tick(dt, _speak);
            TickComms(dt);
            _queue?.Tick();
            TickAlarm(dt);
            _away.Tick(dt);
            _core.Tick(dt);
            _gauge.Tick(dt);
            _opening.Tick(dt);
            _voice.Tick(dt);
            Advisor.Tick(dt);
            UpdateTimers();
        }

        /// <summary>Real seconds until a future tick completes, at the current time scale.</summary>
        private double SecondsUntil(long tick)
        {
            long ticks = tick - _host.Sim.State.Tick;
            if (ticks <= 0)
            {
                return 0;
            }

            return _host.SecondsToNextTick + ((ticks - 1) * 60.0 / _host.Settings.DevTimeScale);
        }

        private void UpdateTimers()
        {
            GameState s = _host.Sim.State;
            if (s.RaidId != 0)
            {
                double left = SecondsUntil(s.RaidArriveTick);
                _raidTime.text = Fmt.Countdown(left);
                if (!_raidAutoOpened && (left < ImminentSeconds || s.BattleLive))
                {
                    // an imminent attack opens its own card (SPEC-040 rule 2)
                    _raidAutoOpened = true;
                    SetRaidOpen(true);
                }
            }

            if (s.Ultimatum == UltimatumStage.Issued || s.Dilemma != DilemmaKind.None)
            {
                Q<Label>("dispatch-time").text = Fmt.Countdown(SecondsUntil(s.Ultimatum == UltimatumStage.Issued ? s.UltimatumDeadlineTick : s.DilemmaUntilTick));
            }

            BuildJob next = null;
            foreach (BuildJob job in s.Jobs)
            {
                if (next == null || job.CompleteTick < next.CompleteTick)
                {
                    next = job;
                }
            }

            if (next != null)
            {
                _nextLabel.text = Fmt.FacilityName(next.Kind) + " L" + next.TargetLevel;
                _nextTime.text = Fmt.Countdown(SecondsUntil(next.CompleteTick));
                float total = Mathf.Max(1f, next.CompleteTick - next.StartTick);
                Kit.SetProgress(_jobBar, ((s.Tick - next.StartTick) + _host.TickProgress) / total);
                _job.RemoveFromClassList("is-idle");
                Icons.SetGlyph(Q<VisualElement>("next-icon"), next.TargetLevel > 1 ? "up" : "hammer");
                _jobCount.text = s.Jobs.Count > 1 ? "+" + (s.Jobs.Count - 1) : string.Empty;
            }
            else
            {
                _nextLabel.text = "BUILDERS IDLE";
                _nextTime.text = "TAP TO BUILD";
                _job.AddToClassList("is-idle");
                Icons.SetGlyph(Q<VisualElement>("next-icon"), "hammer");
                _jobCount.text = string.Empty;
            }
        }

        /// <summary>Under this many real seconds to contact the threat card opens by itself.</summary>
        private const double ImminentSeconds = 180;

        /// <summary>Seconds a fully shown advisor line stays expanded before the panel rests as a slim bar.</summary>
        private const float CommsRestSeconds = 6f;

        private void SetRaidOpen(bool open)
        {
            _raidOpen = open;
            _raidDetail.EnableInClassList("is-collapsed", !open);
            if (open)
            {
                Choreo.Enter(_raidDetail);
                Motion.To(_raidDetail, 0.3f, Ease.OutBack, t =>
                {
                    _raidDetail.style.opacity = t;
                    _raidDetail.style.translate = new Translate(0, (1f - t) * -20f, 0);
                });
            }
        }

        private void ExpandComms()
        {
            _readClock = 0f;
            if (_advisorPanel.ClassListContains("is-compact"))
            {
                _advisorPanel.RemoveFromClassList("is-compact");
                Choreo.Enter(_advisorPanel);
            }
        }

        private void TickComms(float dt)
        {
            if (Advisor.Typing || _advisorPanel.ClassListContains("is-compact"))
            {
                _readClock = 0f;
                return;
            }

            _readClock += dt;
            if (_readClock >= CommsRestSeconds)
            {
                _advisorPanel.AddToClassList("is-compact");
            }
        }

        /// <summary>The command bar's indicator glides to the active tab; the tab's icon punches.</summary>
        private void MoveTabRail(string id, bool animate)
        {
            VisualElement tab = id != null ? Q<VisualElement>("tab-" + id) : null;
            VisualElement rail = Q<VisualElement>("tab-rail");
            if (rail == null)
            {
                return;
            }

            if (tab == null)
            {
                rail.style.opacity = 0f;
                return;
            }

            Rect r = tab.layout;
            if (float.IsNaN(r.width) || r.width <= 0f)
            {
                return;
            }

            rail.style.opacity = 1f;
            float fromX = rail.resolvedStyle.left;
            float fromW = rail.resolvedStyle.width;
            if (!animate || float.IsNaN(fromX) || fromW <= 0f)
            {
                rail.style.left = r.x;
                rail.style.width = r.width;
                return;
            }

            Motion.To(rail, 0.3f, Ease.OutCubic, t =>
            {
                rail.style.left = Mathf.Lerp(fromX, r.x, t);
                rail.style.width = Mathf.Lerp(fromW, r.width, t);
            });
            VisualElement icon = tab.Q(className: "hud-tab__icon");
            if (icon != null)
            {
                Choreo.Punch(icon, 0.25f);
            }
        }

        /// <summary>The raid card's plan line (SPEC-042 findings 5-6): what is set now, what the AI would set.</summary>
        private void RefreshPlan(GameState s, SimConfig c)
        {
            Q<Label>("raid-now").text = "NOW  " + Fmt.PostureName(s.Posture) + " // " + s.Garrison + " ON THE WALL";
            AiSystem.Recommend(s, c, out Posture posture, out int garrison);
            bool set = posture == Posture.None || (posture == s.Posture && garrison == s.Garrison);
            VisualElement plan = Q<VisualElement>("raid-apply").parent;
            plan.EnableInClassList("is-set", set);
            Q<Label>("raid-plan").text = set ? "PLAN SET // NOTHING TO CHANGE" : Fmt.PostureName(posture) + " // " + garrison + " DEFENDERS";
            Q<VisualElement>("raid-apply").EnableInClassList("is-hidden", set);
        }

        /// <summary>One tap answers the threat with the AI's recommendation (the same commands as OPS SET &amp; GO).</summary>
        private void ApplyAiPlan()
        {
            GameState s = _host.Sim.State;
            if (s.RaidId == 0)
            {
                return;
            }

            AiSystem.Recommend(s, _host.Sim.Config, out Posture posture, out int garrison);
            if (posture == Posture.None)
            {
                return;
            }

            CommandResult a = _host.Execute(Command.SetGarrison(garrison));
            CommandResult b = _host.Execute(Command.SetPosture(posture));
            bool ok = a.Accepted && b.Accepted;
            Toasts.Show(ok ? "shield" : "alert", ok ? "PLAN SET // " + Fmt.PostureName(posture) : "PLAN REFUSED", ok ? Toasts.Tone.Good : Toasts.Tone.Bad);
            if (!ok)
            {
                Advisor.Say(Texts.Reason(a.Accepted ? b.Reason : a.Reason));
            }

            Refresh();
        }

        /// <summary>The Command menu (SPEC-042 finding 1): every secondary destination in one place.</summary>
        private void BuildMenu(PremiumScreen premium, SeasonScreen season)
        {
            _menu = new CommandMenu(_ui.Sheets);
            _menu.Section("people", "THE HUB");
            _menu.Add("people", "WORKFORCE", "Crews, loyalty and the hard choices.", () => Router.Show("workforce"));
            _menu.Add("mail", "DISPATCH", "Ultimatums and dilemmas waiting for your answer.", () => Router.Show("dispatch"), () => _host.Sim.State.Ultimatum == UltimatumStage.Issued || _host.Sim.State.Dilemma != DilemmaKind.None ? 1 : 0);
            _menu.Add("report", "LAST REPORT", "The latest fight, with the evidence.", () => OpenReport(_chipRaid > 0 ? _chipRaid : LatestRaid()), () => _reportChip.ClassListContains("is-hidden") ? 0 : 1);
            _menu.Section("book", "RECORDS");
            _menu.Add("book", "STORY", "Chapters and the memory fragments you recovered.", () => Router.Show("story"));
            _menu.Add("cycle", "LEGACY", "This cycle's score, perks and relocation.", () => Router.Show("legacy"));
            _menu.Section("gear", "ACCOUNT");
            _menu.Add("star", "SEASON TRACK", "Cosmetic rewards earned by play.", () =>
            {
                season.ReturnTo("base");
                Router.Show("season");
            });
            _menu.Add("diamond", "FULL GAME", "One payment. Nothing else is sold.", () =>
            {
                premium.ReturnTo("base");
                Router.Show("premium");
            });
            _menu.Add("gear", "SETTINGS", "Comfort, accessibility, camera and backup.", () => Router.Show("settings"));
            Q<VisualElement>("menu-btn").RegisterCallback<ClickEvent>(_ =>
            {
                GameState s = _host.Sim.State;
                _menu.Open("HUB S-17 // TIER " + s.Tier + " // " + Fmt.Clock(s.Tick).Split(' ')[0] + " " + Fmt.Clock(s.Tick).Split(' ')[1]);
            });
        }

        /// <summary>The newest resolved raid in the log (0 when none).</summary>
        private int LatestRaid()
        {
            var events = _host.Sim.Log.Events;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                if (events[i].Kind == EventKind.RaidResolved)
                {
                    return events[i].A;
                }
            }

            return 0;
        }

        private void OnCaughtUp(CatchUpReport report)
        {
            _away.Show(_host, report);
        }

        private void RefreshPods(bool instant)
        {
            GameState s = _host.Sim.State;
            SimConfig c = _host.Sim.Config;
            double secPerHour = 3600.0 / _host.Settings.DevTimeScale;
            EconomyFlows f = Economy.Flows(s, c);
            foreach (ResourcePod pod in _pods)
            {
                ResourceInfo r = ResourceInfo.Of(pod.Kind, s, c);
                string rate = null;
                if (pod.Kind == ResKind.People)
                {
                    rate = s.AutomationLoad > 0 ? "AI-RUN " + s.AutomationLoad : "CREW " + f.CrewAssigned + "/" + f.CrewNeeded;
                }
                else if (pod.Kind == ResKind.Energy && s.Blackout)
                {
                    rate = "BLACKOUT";
                }

                if (pod.Kind == ResKind.Fuel)
                {
                    // fuel matters once something burns it or the Hub holds some (Tier 2+ systems)
                    pod.SetVisible(s.Fuel > 0 || r.Drains.Count > 0 || Economy.CountOfKind(s, FacilityKind.FuelDepot) > 0);
                }

                pod.Set(r, secPerHour, instant, rate);
            }
        }

        private void ToggleResource(ResKind kind)
        {
            _resourceSheet.Toggle(kind);
        }

        /// <summary>A "how to get more" shortcut: open its screen or fly to the facility or plot that helps.</summary>
        private void OnHelp(ResHelp help)
        {
            _resourceSheet.Close();
            if (help.Screen != null)
            {
                Router.Show(help.Screen);
                return;
            }

            FocusSlot(help.Slot, help.Kind);
        }

        /// <summary>Shows BASE, flies the drone to a slot and opens it (recommending a facility on an empty plot).</summary>
        public void FocusSlot(int slot, FacilityKind recommend)
        {
            if (slot < 0)
            {
                Toasts.Show("lock", "NO FREE PLOT", Toasts.Tone.Warn);
                return;
            }

            _resourceSheet.Close();
            Router.Show("base");
            _baseScreen.Focus(slot, recommend);
        }

        private void RefreshComms(GameState s, SimConfig c)
        {
            _advisorMood.text = AiSuggestions.Mood(s, c);
            System.Collections.Generic.List<Suggestion> list = AiSuggestions.For(s, c, !_reportChip.ClassListContains("is-hidden"));
            string key = string.Empty;
            foreach (Suggestion sg in list)
            {
                key += sg.Label + "|";
            }

            if (key == _chipKey)
            {
                return;
            }

            _chipKey = key;
            _advisorChips.Clear();
            _advisorChips.EnableInClassList("is-hidden", list.Count == 0);
            foreach (Suggestion sg in list)
            {
                var chip = new VisualElement();
                chip.AddToClassList("ai-chip");
                chip.EnableInClassList("ai-chip--urgent", sg.Urgent);
                chip.Add(Icons.Create(sg.Glyph, "ai-chip__icon"));
                chip.Add(Kit.Label(sg.Label, "ai-chip__label"));
                Suggestion captured = sg;
                chip.RegisterCallback<ClickEvent>(e =>
                {
                    e.StopPropagation();
                    Act(captured);
                });
                _advisorChips.Add(chip);
            }
        }

        private void Act(Suggestion sg)
        {
            switch (sg.Action)
            {
                case SuggestionAction.Resource:
                    Router.Show("base");
                    _resourceSheet.Toggle(sg.Resource);
                    break;
                case SuggestionAction.Plot:
                    FocusSlot(sg.Slot, FacilityKind.None);
                    break;
                default:
                    if (sg.Screen == "report")
                    {
                        OpenReport(_chipRaid);
                    }
                    else
                    {
                        Router.Show(sg.Screen);
                    }

                    break;
            }
        }

        private void RefreshBadges(GameState s, SimConfig c)
        {
            bool threat = s.RaidId != 0 || s.Ultimatum == UltimatumStage.Issued;
            Q<VisualElement>("ops-badge").EnableInClassList("is-hidden", !threat);
            int plot = ResourceInfo.FreePlot(s);
            bool idle = s.Jobs.Count == 0 && plot >= 0;
            Q<VisualElement>("base-badge").EnableInClassList("is-hidden", !idle);
            Q<Label>("base-badge-label").text = "+";
            HeatLevel heat = WorldSystem.Level(s.Heat[(int)WorldSystem.Hottest(s)]);
            Q<VisualElement>("map-badge").EnableInClassList("is-hidden", heat < HeatLevel.Hunted);
            Q<Label>("map-badge-label").text = "!";
            _frame.EnableInClassList("is-alarm", s.RaidId != 0);
            if (_menu != null)
            {
                int pending = _menu.Refresh();
                Q<VisualElement>("menu-badge").EnableInClassList("is-hidden", pending <= 0);
                Q<Label>("menu-badge-label").text = pending.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        /// <summary>The red edge breathes while an attack is inbound (idea 45); still under reduced motion.</summary>
        private void TickAlarm(float dt)
        {
            bool raid = _host.Sim.State.RaidId != 0;
            _alarmClock += dt;
            float weight = raid ? (Motion.Reduced ? 0.6f : 0.35f + (0.35f * Mathf.Sin(_alarmClock * 3f))) * Mathf.Max(0.3f, _host.Settings.Effects) : 0f;
            _alarm.style.opacity = weight;
        }

        private static void SetTone(VisualElement el, string tone)
        {
            el.EnableInClassList("t-phosphor", tone == "t-phosphor");
            el.EnableInClassList("t-amber", tone == "t-amber");
            el.EnableInClassList("t-red", tone == "t-red");
            el.EnableInClassList("t-dim", tone == "t-dim");
        }
    }
}
