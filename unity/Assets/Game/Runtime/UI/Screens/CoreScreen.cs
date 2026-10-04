using System.Collections.Generic;
using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Host.Reports;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// CORE: the AI terminal (SPEC-007). What the core reports about itself, the Audit, the Core Profile from the
    /// last Audit (the truth at that time), and the recent transcript. Layout: Resources/UI/Core.uxml + Core.uss.
    /// </summary>
    public sealed class CoreScreen : IGameScreen
    {
        private static readonly string[] StageNames = { "DORMANT", "ACTIVE", "ADVANCED", "IMMINENT" };
        private static readonly string[] DelegationNames = { "MANUAL", "ROUTINES", "AUTOPILOT" };

        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private readonly System.Func<IReadOnlyList<string>> _history;
        private readonly ModulesView _modules;
        private bool _visible;
        private bool _showModules;

        public CoreScreen(System.Func<IReadOnlyList<string>> history, System.Action openSettings, System.Action openPremium, System.Action openLegacy)
        {
            _host = GameHost.Instance;
            _history = history;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Core");
            Root.Add(tree);
            _ui = tree;
            _ui.Q("audit-run").RegisterCallback<ClickEvent>(_ => Audit());
            _ui.Q("open-legacy").RegisterCallback<ClickEvent>(_ => openLegacy());
            _ui.Q("flush-run").RegisterCallback<ClickEvent>(_ =>
            {
                CommandResult r = _host.Execute(Command.FlushCore());
                _ui.Q<Label>("audit-reason").text = r.Accepted ? string.Empty : r.Reason == RejectReason.NoChange ? "Nothing to flush right now." : Texts.Reason(r.Reason);
                Refresh();
            });
            _ui.Q("open-settings").RegisterCallback<ClickEvent>(_ => openSettings());
            _ui.Q("climax-purge").RegisterCallback<ClickEvent>(_ => Answer(Command.PurgeCore()));
            _ui.Q("climax-silence").RegisterCallback<ClickEvent>(_ => Answer(Command.UseOverride(OverrideKind.Silence)));
            _ui.Q("climax-cancel").RegisterCallback<ClickEvent>(_ => Answer(Command.CancelProject()));
            _modules = new ModulesView(_ui.Q("modules-view"), openPremium);
            _ui.Q("view-status").RegisterCallback<ClickEvent>(_ => ShowModules(false));
            _ui.Q("view-modules").RegisterCallback<ClickEvent>(_ => ShowModules(true));
            _host.Ticked += () =>
            {
                if (_visible)
                {
                    Refresh();
                }
            };
            UiRoot.Instance.Frame += _ =>
            {
                if (_visible && _showModules)
                {
                    _modules.Tick();
                }

                if (_visible && !_showModules)
                {
                    TickClimax();
                }
            };
        }

        public string Id => "core";

        public VisualElement Root { get; }

        public void OnShow()
        {
            _visible = true;
            _ui.Q<Label>("audit-reason").text = string.Empty;
            Refresh();
        }

        public void OnHide()
        {
            _visible = false;
        }

        private void ShowModules(bool on)
        {
            _showModules = on;
            _ui.Q("view-status").EnableInClassList("is-selected", !on);
            _ui.Q("view-modules").EnableInClassList("is-selected", on);
            _ui.Q("status-view").EnableInClassList("is-hidden", on);
            _ui.Q("modules-view").EnableInClassList("is-hidden", !on);
            Refresh();
        }

        private void Refresh()
        {
            if (_showModules)
            {
                _modules.Refresh();
                return;
            }

            GameState s = _host.Sim.State;
            SimConfig c = _host.Sim.Config;

            Kit.SetButtonText(_ui.Q("open-legacy"), "LEGACY // CYCLE " + (s.Cycle + 1) + " // " + Fmt.Num(s.LegacyPoints) + " LP");

            // corruption effects (SPEC-021)
            var g = c.Glitch;
            bool takeover = GlitchSystem.TakenOver(s);
            bool flushing = GlitchSystem.Flushing(s);
            _ui.Q("core-crisis").EnableInClassList("is-hidden", !takeover && !flushing);
            _ui.Q<Label>("core-crisis-title").text = (takeover ? "AI TAKEOVER // " : "CORE FLUSHED // ") + Fmt.Countdown(_host.SecondsUntilTick(takeover ? s.TakeoverUntilTick : s.FlushUntilTick));
            _ui.Q<Label>("core-crisis-desc").text = takeover
                ? "I am running the Hub. Build, research and posture orders are mine until it passes, or until you flush me."
                : "I am dark. AI-run units are stopped and I can predict nothing until I come back.";
            _ui.Q<Label>("flush-desc").text = "Corruption -" + Fmt.Milli(g.FlushMilli) + "%. For " + g.FlushHours + " h I go dark: AI-run units stop and I predict nothing.";
            Kit.SetButtonText(_ui.Q("flush-run"), "FLUSH THE CORE // " + Fmt.Num(g.FlushEnergy) + " ENERGY");
            _ui.Q("flush-run").EnableInClassList("is-disabled", flushing || s.Energy < g.FlushEnergy || s.RaidId != 0 || s.CorruptionMilli == 0);

            bool window = s.ClimaxAtTick > 0;
            _ui.Q("climax").EnableInClassList("is-hidden", !window);
            Kit.SetButtonText(_ui.Q("climax-purge"), "PURGE CORE // " + c.Climax.PurgeEnergy + " ENERGY + ALL COMPUTE");
            Kit.SetButtonText(_ui.Q("climax-silence"), "SILENCE THE AI // OVERRIDE, " + c.Climax.SilenceHours + " H");
            Kit.SetButtonText(_ui.Q("climax-cancel"), "CANCEL THE PROJECT // " + c.Climax.CancelCompute + " COMPUTE" + (s.ClimaxAudited ? string.Empty : ", AUDIT FIRST"));
            _ui.Q("climax-cancel").EnableInClassList("is-disabled", !s.ClimaxAudited);
            TickClimax();

            int reported = ProjectSystem.ReportedCorruptionMilli(s, c);
            CorruptionBand band = CorruptionSystem.Band(c, reported);
            _ui.Q<Label>("core-reported").text = CorruptionSystem.Percent(reported).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var bandLabel = _ui.Q<Label>("core-band");
            bandLabel.text = Fmt.BandName(band);
            bandLabel.EnableInClassList("t-phosphor", band == CorruptionBand.Stable);
            bandLabel.EnableInClassList("t-amber", band == CorruptionBand.Glitchy);
            bandLabel.EnableInClassList("t-red", band >= CorruptionBand.Unstable);
            VisualElement meter = _ui.Q("core-meter");
            meter.EnableInClassList("ds-meter--amber", band == CorruptionBand.Glitchy);
            meter.EnableInClassList("ds-meter--red", band >= CorruptionBand.Unstable);
            Kit.SetMeter(meter, CorruptionSystem.Percent(reported) / 100f);
            _ui.Q<Label>("core-deleg").text = DelegationNames[(int)s.Delegation];
            _ui.Q<Label>("core-ovr").text = s.OverrideCharges + " / " + OverrideSystem.MaxCharges(s, c);

            bool ready = s.Tick >= s.AuditReadyTick;
            VisualElement run = _ui.Q("audit-run");
            run.EnableInClassList("is-disabled", !ready || s.Compute < c.Project.AuditComputeCost);
            Kit.SetButtonText(run, ready
                ? "RUN AUDIT // " + c.Project.AuditComputeCost + " COMPUTE"
                : "AUDIT RECHARGING // " + Fmt.Countdown(_host.SecondsUntilTick(s.AuditReadyTick)));

            CoreProfile p = CoreProfile.Latest(_host.Sim.Log.Events);
            _ui.Q("profile-none").EnableInClassList("is-hidden", p != null);
            _ui.Q("profile-body").EnableInClassList("is-hidden", p == null);
            _ui.Q<Label>("profile-when").text = p != null ? "AUDIT D" + ((p.Tick / SimConfig.TicksPerDay) + 1) + " " + Clock(p.Tick) : string.Empty;
            _ui.Q<Label>("core-lies").text = p != null ? p.UnverifiedLies.ToString(System.Globalization.CultureInfo.InvariantCulture) : "?";
            if (p != null)
            {
                _ui.Q<Label>("dial-cold").text = (p.ColdnessMilli / 1000) + "%";
                _ui.Q<Label>("dial-bold").text = (p.BoldnessMilli / 1000) + "%";
                Kit.SetMeter(_ui.Q("dial-cold-meter"), p.ColdnessMilli / 100000f);
                Kit.SetMeter(_ui.Q("dial-bold-meter"), p.BoldnessMilli / 100000f);
                _ui.Q<Label>("true-corruption").text = CorruptionSystem.Percent(p.TrueCorruptionMilli) + "%";
                _ui.Q<Label>("skimmed").text = Fmt.Num(p.Skimmed);
                _ui.Q<Label>("lies").text = p.UnverifiedLies.ToString(System.Globalization.CultureInfo.InvariantCulture);
                _ui.Q<Label>("stage").text = StageNames[(int)p.Stage];
                VisualElement steps = _ui.Q("stage-steps");
                for (int i = 0; i < steps.childCount; i++)
                {
                    steps[i].EnableInClassList("is-on", i <= (int)p.Stage);
                }
            }

            VisualElement transcript = _ui.Q("transcript");
            transcript.Clear();
            foreach (string line in _history())
            {
                transcript.Add(Kit.Label("> " + line, "core-transcript__line"));
            }
        }

        private void TickClimax()
        {
            GameState s = _host.Sim.State;
            if (s.ClimaxAtTick > 0)
            {
                _ui.Q<Label>("climax-time").text = Fmt.Countdown(_host.SecondsUntilTick(s.ClimaxAtTick));
            }

            bool silenced = ClimaxSystem.Silenced(s);
            var label = _ui.Q<Label>("silenced");
            label.EnableInClassList("is-hidden", !silenced);
            if (silenced)
            {
                label.text = "SILENCED // " + Fmt.Countdown(_host.SecondsUntilTick(s.SilencedUntilTick)) + " // no advice, no predictions";
            }
        }

        private void Answer(Command command)
        {
            CommandResult r = _host.Execute(command);
            _ui.Q<Label>("climax-reason").text = r.Accepted ? string.Empty : Texts.Reason(r.Reason);
            Refresh();
        }

        private void Audit()
        {
            CommandResult r = _host.Execute(Command.Audit());
            _ui.Q<Label>("audit-reason").text = r.Accepted ? string.Empty
                : r.Reason == RejectReason.OnCooldown ? "The sweep is still recharging."
                : Texts.Reason(r.Reason);
            Refresh();
        }

        private static string Clock(long tick)
        {
            long m = tick % SimConfig.TicksPerDay;
            return (m / 60).ToString("00") + ":" + (m % 60).ToString("00");
        }
    }
}
