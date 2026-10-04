using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// DISPATCH (F-034): what the world wants from the Hub. The Warlord Ultimatum (pay or prepare), the pending dilemma
    /// with both choices and their risks stated, and the running world event. Opened from the HUD dispatch chip; never
    /// forced over the player. Layout: Resources/UI/Dispatch.uxml.
    /// </summary>
    public sealed class DispatchScreen : IGameScreen
    {
        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private readonly Label _reason;
        private bool _visible;

        public DispatchScreen(ScreenRouter router)
        {
            _host = GameHost.Instance;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Dispatch");
            Root.Add(tree);
            _ui = tree;
            _reason = _ui.Q<Label>("dsp-reason");
            _ui.Q("dsp-close").RegisterCallback<ClickEvent>(_ => router.Show("base"));
            _ui.Q("dsp-ult-pay").RegisterCallback<ClickEvent>(_ => Run(Command.PayUltimatum()));
            _ui.Q("dsp-ult-prepare").RegisterCallback<ClickEvent>(_ => router.Show("ops"));
            _ui.Q("dsp-dil-take").RegisterCallback<ClickEvent>(_ => Run(Command.ResolveDilemma(0)));
            _ui.Q("dsp-dil-refuse").RegisterCallback<ClickEvent>(_ => Run(Command.ResolveDilemma(1)));

            _host.Ticked += () =>
            {
                if (_visible)
                {
                    Refresh();
                }
            };
            UiRoot.Instance.Frame += _ =>
            {
                if (_visible)
                {
                    Timers();
                }
            };
        }

        public string Id => "dispatch";

        public VisualElement Root { get; }

        public void OnShow()
        {
            _visible = true;
            _reason.text = string.Empty;
            Refresh();
        }

        public void OnHide()
        {
            _visible = false;
        }

        private void Run(Command command)
        {
            CommandResult r = _host.Execute(command);
            _reason.text = r.Accepted ? string.Empty : r.Reason == RejectReason.NothingPending ? "It is already settled." : Texts.Reason(r.Reason);
            Refresh();
        }

        private void Refresh()
        {
            GameState s = _host.Sim.State;
            SimConfig c = _host.Sim.Config;
            var l = c.Living;

            bool world = s.WorldEvent != WorldEventKind.None && s.Tick < s.WorldEventUntilTick;
            _ui.Q("dsp-world").EnableInClassList("is-hidden", !world);
            if (world)
            {
                _ui.Q<Label>("dsp-world-desc").text = LivingTexts.EventEffect(s.WorldEvent, c);
            }

            bool ult = s.Ultimatum == UltimatumStage.Issued;
            _ui.Q("dsp-ultimatum").EnableInClassList("is-hidden", !ult);
            if (ult)
            {
                _ui.Q<Label>("dsp-ult-text").text = "Mother Kess has watched us stay small for " + l.UltimatumDay + " days. Pay, or her whole clan comes over the wall.";
                _ui.Q<Label>("dsp-ult-price").text = Fmt.Num(l.UltimatumEnergy) + " E  " + Fmt.Num(l.UltimatumFuel) + " F";
                _ui.Q<Label>("dsp-ult-wave").text = "WAVE x" + Fmt.Milli(l.UltimatumStrengthPct * 10);
                _ui.Q("dsp-ult-pay").EnableInClassList("is-disabled", s.Energy < l.UltimatumEnergy || s.Fuel < l.UltimatumFuel);
            }

            bool dilemma = s.Dilemma != DilemmaKind.None;
            _ui.Q("dsp-dilemma").EnableInClassList("is-hidden", !dilemma);
            if (dilemma)
            {
                _ui.Q<Label>("dsp-dil-name").text = LivingTexts.DilemmaName(s.Dilemma);
                _ui.Q<Label>("dsp-dil-text").text = LivingTexts.DilemmaText(s.Dilemma);
                for (int choice = 0; choice < 2; choice++)
                {
                    string id = choice == 0 ? "dsp-dil-take" : "dsp-dil-refuse";
                    (string label, string effect) = LivingTexts.Choice(s.Dilemma, choice, c);
                    _ui.Q<Label>(id + "-label").text = label;
                    _ui.Q<Label>(id + "-fx").text = effect;
                }

                _ui.Q("dsp-dil-take").EnableInClassList("is-disabled", s.Dilemma == DilemmaKind.Trader && s.Energy < l.TraderEnergy);
            }

            _ui.Q("dsp-empty").EnableInClassList("is-hidden", ult || dilemma);
            Timers();
        }

        private void Timers()
        {
            GameState s = _host.Sim.State;
            if (s.WorldEvent != WorldEventKind.None)
            {
                _ui.Q<Label>("dsp-world-title").text = LivingTexts.EventName(s.WorldEvent) + " // " + Fmt.Countdown(_host.SecondsUntilTick(s.WorldEventUntilTick));
            }

            if (s.Ultimatum == UltimatumStage.Issued)
            {
                _ui.Q<Label>("dsp-ult-time").text = Fmt.Countdown(_host.SecondsUntilTick(s.UltimatumDeadlineTick));
            }

            if (s.Dilemma != DilemmaKind.None)
            {
                _ui.Q<Label>("dsp-dil-time").text = Fmt.Countdown(_host.SecondsUntilTick(s.DilemmaUntilTick));
            }
        }
    }
}
