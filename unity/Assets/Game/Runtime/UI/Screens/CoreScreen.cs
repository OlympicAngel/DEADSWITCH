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
        private static readonly string[] DelegationNames = { "YOU DECIDE", "AI ASSISTS", "AI DECIDES" };

        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private readonly System.Func<IReadOnlyList<string>> _history;
        private readonly ModulesView _modules;
        private readonly AiOrb _orb;
        private readonly VisualElement _orbEl;
        private bool _visible;
        private bool _showModules;
        private bool _crisisSeen;
        private System.Action _next;

        public CoreScreen(System.Func<IReadOnlyList<string>> history)
        {
            _host = GameHost.Instance;
            _history = history;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Core");
            Root.Add(tree);
            _ui = tree;
            _ui.Q("audit-run").RegisterCallback<ClickEvent>(_ => Audit());
            _ui.Q("secret-dismantle").RegisterCallback<ClickEvent>(_ =>
            {
                CommandResult r = _host.Execute(Command.DismantleSecrets());
                _ui.Q<Label>("audit-reason").text = r.Accepted ? string.Empty : Texts.Reason(r.Reason);
                Refresh();
            });
            _ui.Q("flush-run").RegisterCallback<ClickEvent>(_ =>
            {
                CommandResult r = _host.Execute(Command.FlushCore());
                _ui.Q<Label>("audit-reason").text = r.Accepted ? string.Empty : r.Reason == RejectReason.NoChange ? "Nothing to flush right now." : Texts.Reason(r.Reason);
                Refresh();
            });
            _ui.Q("climax-purge").RegisterCallback<ClickEvent>(_ => Answer(Command.PurgeCore()));
            _ui.Q("climax-silence").RegisterCallback<ClickEvent>(_ => Answer(Command.UseOverride(OverrideKind.Silence)));
            _ui.Q("climax-cancel").RegisterCallback<ClickEvent>(_ => Answer(Command.CancelProject()));
            _ui.Q("core-next-go").RegisterCallback<ClickEvent>(_ => _next?.Invoke());
            _orbEl = _ui.Q("core-orb");
            _orb = new AiOrb(_orbEl);
            _modules = new ModulesView(_ui.Q("modules-view"));
            // one tab row (SPEC-042 finding 2): MODULES is a page beside PRESENCE, ACTIONS and PROFILE
            Pager.PageShown += (pager, index) =>
            {
                if (pager == _ui.Q("status-pager"))
                {
                    SyncPage();
                    Refresh();
                }
            };
            _host.Ticked += () =>
            {
                if (_visible)
                {
                    Refresh();
                }
            };
            UiRoot.Instance.Frame += dt =>
            {
                if (_visible && _showModules)
                {
                    _modules.Tick();
                }

                if (_visible && !_showModules)
                {
                    TickClimax();
                    _orb.Tick(dt);
                }
            };
        }

        public string Id => "core";

        public VisualElement Root { get; }

        public void OnShow()
        {
            _visible = true;
            _ui.Q<Label>("audit-reason").text = string.Empty;
            SyncPage();
            Refresh();
        }

        public void OnHide()
        {
            _visible = false;
        }

        /// <summary>Opens on the MODULES page (the next-goal card on BASE).</summary>
        public void OpenModules()
        {
            Pager.Show(_ui.Q("status-pager"), "page-modules");
            SyncPage();
            Refresh();
        }

        /// <summary>Whether the modules page is the one showing (it ticks research timers).</summary>
        private void SyncPage()
        {
            _showModules = _ui.Q("page-modules").style.display.value == DisplayStyle.Flex;
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


            // corruption effects (SPEC-021)
            var g = c.Glitch;
            bool takeover = GlitchSystem.TakenOver(s);
            bool flushing = GlitchSystem.Flushing(s);
            bool crisis = takeover || flushing;
            _ui.Q("core-crisis").EnableInClassList("is-hidden", !crisis);
            VisualElement statusPager = _ui.Q("status-pager");
            Pager.Badge(statusPager, "page-actions", crisis ? 1 : 0);
            if (crisis && !_crisisSeen)
            {
                Pager.Show(statusPager, "page-actions");
            }

            _crisisSeen = crisis;
            _ui.Q<Label>("core-crisis-title").text = (takeover ? "AI TAKEOVER // " : "CORE FLUSHED // ") + Fmt.Countdown(_host.SecondsUntilTick(takeover ? s.TakeoverUntilTick : s.FlushUntilTick));
            _ui.Q<Label>("core-crisis-desc").text = takeover
                ? "I am running the Hub. Build, research and defense orders are mine until it passes, or until you flush me."
                : "I am dark. AI-run units are stopped and I can predict nothing until I come back.";
            _ui.Q<Label>("flush-desc").text = "Corruption -" + Fmt.Milli(g.FlushMilli) + "%. For " + g.FlushHours + " h I go dark: AI-run units stop and I predict nothing.";
            Kit.SetButtonText(_ui.Q("flush-run"), "FLUSH THE CORE // " + Fmt.Num(g.FlushEnergy) + " ENERGY");
            _ui.Q("flush-run").EnableInClassList("is-disabled", flushing || s.Energy < g.FlushEnergy || s.RaidId != 0 || s.CorruptionMilli == 0);

            bool window = s.ClimaxAtTick > 0;
            _ui.Q("climax").EnableInClassList("is-hidden", !window);
            Kit.SetButtonText(_ui.Q("climax-purge"), "RESET THE CORE // " + c.Climax.PurgeEnergy + " ENERGY + ALL COMPUTE");
            Kit.SetButtonText(_ui.Q("climax-silence"), "SILENCE THE AI // OVERRIDE, " + c.Climax.SilenceHours + " H");
            Kit.SetButtonText(_ui.Q("climax-cancel"), "CANCEL THE PROJECT // " + c.Climax.CancelCompute + " COMPUTE" + (s.ClimaxAudited ? string.Empty : ", AUDIT FIRST"));
            _ui.Q("climax-cancel").EnableInClassList("is-disabled", !s.ClimaxAudited);
            TickClimax();

            RefreshNext(s, c, crisis);

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
            _orbEl.EnableInClassList("core-orb--amber", band == CorruptionBand.Glitchy);
            _orbEl.EnableInClassList("core-orb--red", band >= CorruptionBand.Unstable);
            _orb.Stutter = GlitchText.BandWeight((int)band) * _host.Settings.Effects;
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

                // hidden nodes (SPEC-034): as of the Audit; still standing ones can be torn down
                SimConfig sc = _host.Sim.Config;
                _ui.Q<Label>("secret-nodes").text = p.HiddenNodes == 0 ? "NONE FOUND" : p.HiddenNodes + " // DRAWING " + (p.HiddenNodes * sc.Secrets.NodeEnergyPerHour) + " E/H";
                int exposed = _host.Sim.State.SecretExposed;
                _ui.Q("secret-dismantle").EnableInClassList("is-hidden", exposed == 0);
                Kit.SetButtonText(_ui.Q("secret-dismantle"), "DISMANTLE " + exposed + (exposed == 1 ? " NODE" : " NODES") + " // PROJECT -" + (exposed * sc.Secrets.DismantleProjectMilli / 1000) + "%");
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

            Kit.MarkEnds(transcript);
            transcript.style.display = transcript.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// The one decision the core wants from you now (SPEC-046 r9), most pressing first: the next tier, a module
        /// that can be restored, an audit that is ready. Hidden during a takeover or flush (ACTIONS leads then).
        /// </summary>
        private void RefreshNext(GameState s, SimConfig c, bool crisis)
        {
            string title = null;
            string body = null;
            string button = null;
            _next = null;
            TierGates g = Modules.Gates(s, c);
            bool busy = s.ResearchNode != 0 || s.MemoryNode != 0;
            int ready = 0;
            foreach (ModuleDef d in Modules.Catalog)
            {
                if (!Modules.IsRestored(s, d.Node) && !Modules.Restoring(s, d.Node) && Modules.Availability(s, d.Node) == RejectReason.None)
                {
                    ready++;
                }
            }

            bool auditReady = s.Tick >= s.AuditReadyTick && s.Compute >= c.Project.AuditComputeCost;
            if (crisis)
            {
                // the ACTIONS page carries the crisis
            }
            else if (s.Tier < c.Tier.MaxTier && g.All)
            {
                title = "NEXT FROM YOU // TIER " + (s.Tier + 1);
                body = "Every gate is met. Say the word and the Hub grows.";
                button = "OPEN MODULES";
                _next = OpenModules;
            }
            else if (!busy && ready > 0)
            {
                title = "NEXT FROM YOU // " + ready + (ready == 1 ? " MODULE READY" : " MODULES READY");
                body = "Nothing is restoring. Give me something to remember.";
                button = "OPEN MODULES";
                _next = OpenModules;
            }
            else if (auditReady)
            {
                title = "NEXT FROM YOU // AUDIT READY";
                body = "Check what I report against what is true. I will not stop you.";
                button = "RUN AUDIT // " + c.Project.AuditComputeCost + " COMPUTE";
                _next = Audit;
            }
            else if (busy)
            {
                var node = (ModuleNode)(s.ResearchNode != 0 ? s.ResearchNode : s.MemoryNode);
                title = "RESTORING // " + ModuleTexts.Name(node);
                body = "Nothing needs you here. I will say when it is done.";
                button = "OPEN MODULES";
                _next = OpenModules;
            }

            VisualElement card = _ui.Q("core-next");
            card.EnableInClassList("is-hidden", title == null);
            if (title != null)
            {
                _ui.Q<Label>("core-next-title").text = title;
                _ui.Q<Label>("core-next-body").text = body;
                Kit.SetButtonText(_ui.Q("core-next-go"), button);
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
