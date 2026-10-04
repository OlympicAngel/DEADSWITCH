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

        private AnimatedNumber _energy;
        private AnimatedNumber _compute;
        private AnimatedNumber _people;
        private AnimatedNumber _core;
        private Label _energyCap;
        private Label _energyNet;
        private Label _computeCap;
        private Label _computeRate;
        private Label _peopleCap;
        private Label _peopleCrew;
        private Label _coreBand;
        private Label _coreValue;
        private Label _clock;
        private Label _nextLabel;
        private Label _nextTime;
        private Label _raidTime;
        private Label _raidEstimate;
        private VisualElement _nextPip;
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

            _energy = new AnimatedNumber(Q<Label>("energy-value"), Fmt.Num);
            _compute = new AnimatedNumber(Q<Label>("compute-value"), Fmt.Num);
            _people = new AnimatedNumber(Q<Label>("people-value"), Fmt.Num);
            _core = new AnimatedNumber(Q<Label>("core-value"), Fmt.Num);
            _energyCap = Q<Label>("energy-cap");
            _energyNet = Q<Label>("energy-net");
            _computeCap = Q<Label>("compute-cap");
            _computeRate = Q<Label>("compute-rate");
            _peopleCap = Q<Label>("people-cap");
            _peopleCrew = Q<Label>("people-crew");
            _coreBand = Q<Label>("core-band");
            _coreValue = Q<Label>("core-value");
            _clock = Q<Label>("clock");
            _nextLabel = Q<Label>("next-label");
            _nextTime = Q<Label>("next-time");
            _nextPip = Q<VisualElement>("next-pip");
            _raid = Q<VisualElement>("raid-banner");
            _raidDetail = Q<VisualElement>("raid-detail");
            _coreDot = Q<VisualElement>("core-dot");
            _raidTime = Q<Label>("raid-time");
            _raidEstimate = Q<Label>("raid-estimate");
            _coreGaugeEl = Q<VisualElement>("core-gauge");
            _gauge = new ArcGauge(_coreGaugeEl);
            _overridePips = Q<VisualElement>("override-pips").Children().ToArray();

            Advisor = new AdvisorTicker(Q<Label>("advisor-text"));
            _voice = new AdvisorVoice(_host, Advisor);

            Router = new ScreenRouter(Q<VisualElement>("screen"));
            _baseScreen = new Base.BaseScreen(Router);
            Router.Register(_baseScreen);
            _report = new ReportScreen(Router);
            Router.Register(_report);
            Router.Register(new OpsScreen(OpenReport));
            var premium = new PremiumScreen(Router);
            Router.Register(premium);
            Router.Register(new LegacyScreen(Router));
            Router.Register(new CoreScreen(() => _voice.History, () => Router.Show("settings"), () =>
            {
                premium.ReturnTo("core");
                Router.Show("premium");
            }, () => Router.Show("legacy")));
            Router.Register(new SettingsScreen(Router, () =>
            {
                premium.ReturnTo("settings");
                Router.Show("premium");
            }));
            Store.Theme.Apply(UiRoot.Instance.Root);
            Router.Register(new WorkforceScreen(Router));
            Q<VisualElement>("people-cell").RegisterCallback<ClickEvent>(_ => Router.Show("workforce"));
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
            Router.BindTab("base", Q<VisualElement>("tab-base"));
            Router.BindTab("map", Q<VisualElement>("tab-map"));
            Router.BindTab("core", Q<VisualElement>("tab-core"));
            Router.BindTab("ops", Q<VisualElement>("tab-ops"));
            Q<VisualElement>("raid-defend").RegisterCallback<ClickEvent>(_ => Router.Show("ops"));
            Router.Show("base");
            _opening = new OpeningFlow(_host, _hud, _voice, _baseScreen);

            _host.Ticked += Refresh;
            _host.EventRaised += OnSimEvent;
            _ui.Frame += OnFrame;
            Refresh();
            _energy.Set(_host.Sim.State.Energy, true);
            _compute.Set(_host.Sim.State.Compute, true);
            _people.Set(_host.Sim.State.People, true);
            _core.Set(CorruptionSystem.Percent(ProjectSystem.ReportedCorruptionMilli(_host.Sim.State, _host.Sim.Config)), true);
        }

        private void OnDestroy()
        {
            if (_host != null)
            {
                _host.Ticked -= Refresh;
                _host.EventRaised -= OnSimEvent;
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
                Feedback.Alert();
                Motion.To(_raid, 0.42f, Ease.OutBack, t => _raid.style.scale = new Scale(new Vector3(0.85f + (0.15f * t), 0.85f + (0.15f * t), 1f)));
            }
        }

        private void Refresh()
        {
            Simulation sim = _host.Sim;
            GameState s = sim.State;
            SimConfig c = sim.Config;
            EconomyFlows f = Economy.Flows(s, c);

            _energy.Set(s.Energy);
            _energyCap.text = "/" + Fmt.Num(f.EnergyCap);
            int net = f.NetEnergyPerHour;
            _energyNet.text = s.Blackout ? "BLACKOUT" : Fmt.Signed(net) + "/h";
            SetTone(_energyNet, s.Blackout ? "t-red" : (net >= 0 ? "t-phosphor" : "t-amber"));

            _compute.Set(s.Compute);
            _computeCap.text = "/" + Fmt.Num(c.Compute.Cap);
            _computeRate.text = Fmt.Signed(f.ComputePerHour) + "/h";

            _people.Set(s.People);
            _peopleCap.text = "/" + Fmt.Num(f.PopulationCap);
            _peopleCrew.text = (s.AutomationLoad > 0 ? "AI-RUN " + s.AutomationLoad : "CREW " + f.CrewAssigned + "/" + f.CrewNeeded);
            SetTone(_peopleCrew, s.AutomationLoad > 0 ? "t-amber" : "t-dim");

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
                _raidEstimate.text = (s.RaidGateReported == RaidGate.None ? "?" : Names.Gate(s.RaidGateReported)) + " // EST " + (s.RaidEstimate > 0 ? Fmt.Num(s.RaidEstimate) : "?") + " // DEF " + Fmt.Num(Defense.Rating(s, c)) + " // " + Fmt.PostureName(s.Posture);
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

            _energy.Tick(dt);
            _compute.Tick(dt);
            _people.Tick(dt);
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
                _raidTime.text = Fmt.Countdown(SecondsUntil(s.RaidArriveTick));
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
                _nextPip.EnableInClassList("ds-pip--off", false);
            }
            else
            {
                _nextLabel.text = "BUILD QUEUE IDLE";
                _nextTime.text = string.Empty;
                _nextPip.EnableInClassList("ds-pip--off", true);
            }
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
