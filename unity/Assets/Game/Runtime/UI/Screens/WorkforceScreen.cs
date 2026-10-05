using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// WORKFORCE (SPEC-012): population, crew, garrison and loyalty, plus the three ruthless shortcuts with every
    /// cost shown up front. A lethal choice arms on the first tap and runs on the second. Opened from the HUD
    /// people cell. Layout: Resources/UI/Workforce.uxml.
    /// </summary>
    public sealed class WorkforceScreen : IGameScreen
    {
        private const long ArmMs = 4000;

        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private readonly VisualElement _meter;
        private readonly Label _reason;
        private VisualElement _armed;
        private int _cleanseN = 1;
        private bool _visible;

        public WorkforceScreen(ScreenRouter router)
        {
            _host = GameHost.Instance;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Workforce");
            Root.Add(tree);
            _ui = tree;
            _meter = _ui.Q("wf-loyalty-meter");
            Kit.BuildMeter(_meter);
            _reason = _ui.Q<Label>("wf-reason");

            _ui.Q("wf-close").RegisterCallback<ClickEvent>(_ => router.Return());
            Lethal("wf-forced-btn", () => Command.ForcedLabor(), () => _host.Sim.Config.PeopleChoices.SurgeDeaths + " DIE");
            Lethal("wf-cleanse-btn", () => Command.NeuralCleanse(_cleanseN), () => _cleanseN + " DIE");
            Lethal("wf-crackdown-btn", () => Command.Crackdown(), () => _host.Sim.Config.PeopleChoices.CrackdownPeople + " REMOVED");
            _ui.Q("wf-cleanse-minus").RegisterCallback<ClickEvent>(_ => Step(-1));
            _ui.Q("wf-cleanse-plus").RegisterCallback<ClickEvent>(_ => Step(1));

            _host.Ticked += () =>
            {
                if (_visible)
                {
                    Refresh();
                }
            };
        }

        public string Id => "workforce";

        public VisualElement Root { get; }

        public void OnShow()
        {
            _visible = true;
            _reason.text = string.Empty;
            Disarm();
            Refresh();
        }

        public void OnHide()
        {
            _visible = false;
            Disarm();
        }

        private void Lethal(string name, System.Func<Command> command, System.Func<string> toll)
        {
            VisualElement button = _ui.Q(name);
            string idle = button.Q<Label>().text;
            button.userData = idle;
            button.RegisterCallback<ClickEvent>(_ =>
            {
                if (button.ClassListContains("is-disabled"))
                {
                    _reason.text = button.tooltip;
                    return;
                }

                if (_armed != button)
                {
                    Disarm();
                    _armed = button;
                    button.AddToClassList("is-armed");
                    Card(button).AddToClassList("is-armed");
                    Kit.SetButtonText(button, "CONFIRM // " + toll());
                    button.schedule.Execute(() =>
                    {
                        if (_armed == button)
                        {
                            Disarm();
                        }
                    }).ExecuteLater(ArmMs);
                    return;
                }

                Disarm();
                CommandResult r = _host.Execute(command());
                _reason.text = r.Accepted ? string.Empty : Reason(r.Reason);
                Refresh();
            });
        }

        private void Disarm()
        {
            if (_armed == null)
            {
                return;
            }

            _armed.RemoveFromClassList("is-armed");
            Card(_armed).RemoveFromClassList("is-armed");
            Kit.SetButtonText(_armed, (string)_armed.userData);
            _armed = null;
        }

        private void Step(int d)
        {
            PeopleChoiceConfig p = _host.Sim.Config.PeopleChoices;
            _cleanseN = SimMath.Clamp(_cleanseN + d, 1, p.CleanseMax);
            Disarm();
            Refresh();
        }

        private void Refresh()
        {
            GameState s = _host.Sim.State;
            SimConfig c = _host.Sim.Config;
            PeopleChoiceConfig p = c.PeopleChoices;
            EconomyFlows f = Economy.Flows(s, c);

            _ui.Q<Label>("wf-pop").text = Fmt.Num(s.People) + "/" + Fmt.Num(f.PopulationCap);
            _ui.Q<Label>("wf-crew").text = f.CrewAssigned + "/" + f.CrewNeeded;
            _ui.Q<Label>("wf-garrison").text = Fmt.Num(s.Garrison);

            // loyalty: color + word + number (never color alone)
            LoyaltyStatus status = PeopleChoices.Status(s, c);
            string tone = status == LoyaltyStatus.Mutinous ? "red" : status == LoyaltyStatus.Strained ? "amber" : "phosphor";
            int pct = s.LoyaltyMilli / 1000;
            var band = _ui.Q<Label>("wf-loyalty-band");
            var pctLabel = _ui.Q<Label>("wf-loyalty-pct");
            band.text = status == LoyaltyStatus.Mutinous ? "MUTINOUS" : status == LoyaltyStatus.Strained ? "STRAINED" : "STEADY";
            pctLabel.text = pct + "%";
            foreach (string t in new[] { "red", "amber", "phosphor" })
            {
                band.EnableInClassList("t-" + t, t == tone);
                pctLabel.EnableInClassList("t-" + t, t == tone);
                _meter.EnableInClassList("ds-meter--" + t, t == tone && t != "phosphor");
            }

            Kit.SetMeter(_meter, s.LoyaltyMilli / 100_000f);
            string recover = "Recovers " + Fmt.Milli(p.RecoverPerHour) + "% per hour without a surge.";
            _ui.Q<Label>("wf-loyalty-fx").text = status == LoyaltyStatus.Mutinous
                ? "Generator and Server Rack output -" + p.MutinousOutputPts + "%. Operators may desert at noon. " + recover
                : status == LoyaltyStatus.Strained
                    ? "Generator and Server Rack output -" + p.StrainedOutputPts + "%. " + recover
                    : "They work. They do not ask questions. Yet.";

            bool surging = PeopleChoices.Surging(s);
            var surge = _ui.Q<Label>("wf-surge");
            surge.EnableInClassList("is-hidden", !surging);
            if (surging)
            {
                surge.text = "SURGE RUNNING // +" + p.SurgeOutputPts + "% OUTPUT // " + Fmt.Countdown(_host.SecondsUntilTick(s.SurgeUntilTick)) + " LEFT";
            }

            // forced labor
            bool cooling = s.Tick < s.SurgeReadyTick;
            _ui.Q<Label>("wf-forced-gain").text = "+" + p.SurgeOutputPts + "% OUTPUT // " + p.SurgeHours + "H";
            _ui.Q<Label>("wf-forced-cost").text = p.SurgeDeaths + " DIE // LOYALTY -" + (p.SurgeLoyalty / 1000) + " // COLDER"
                + (cooling ? " // READY IN " + Fmt.Countdown(_host.SecondsUntilTick(s.SurgeReadyTick)) : string.Empty);
            SetEnabled("wf-forced-btn", cooling ? "They are still recovering from the last surge." : Floor(s, p, p.SurgeDeaths));

            // neural cleansing
            int removed = System.Math.Min(s.CorruptionMilli, _cleanseN * p.CleansePerPerson);
            _ui.Q<Label>("wf-cleanse-n").text = _cleanseN.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _ui.Q<Label>("wf-cleanse-gain").text = "CORE -" + Fmt.Milli(removed) + "%";
            _ui.Q<Label>("wf-cleanse-cost").text = _cleanseN + " DIE // LOYALTY -" + (_cleanseN * p.CleanseLoyalty / 1000) + " // COLDER";
            SetEnabled("wf-cleanse-btn", s.CorruptionMilli <= 0 ? "The core is clean. There is nothing to burn out." : Floor(s, p, _cleanseN));

            // crackdown
            _ui.Q<Label>("wf-crackdown-gain").text = "LOYALTY +" + (p.CrackdownLoyalty / 1000);
            _ui.Q<Label>("wf-crackdown-cost").text = p.CrackdownPeople + " REMOVED // COLDER"
                + (status == LoyaltyStatus.Steady ? " // ONLY WHEN THEY ARE RESTLESS" : string.Empty);
            SetEnabled("wf-crackdown-btn", status == LoyaltyStatus.Steady ? Texts.Reason(RejectReason.LoyaltyHolds) : Floor(s, p, p.CrackdownPeople));
        }

        /// <summary>Null when the population can pay; otherwise why not.</summary>
        private static string Floor(GameState s, PeopleChoiceConfig p, int cost)
        {
            return s.People - cost >= p.MinPeople ? null : "Too few left. I will not go below " + p.MinPeople + " people.";
        }

        private static VisualElement Card(VisualElement button)
        {
            VisualElement e = button;
            while (e.parent != null && !e.ClassListContains("wf-choice"))
            {
                e = e.parent;
            }

            return e;
        }

        /// <summary>Enables a choice when <paramref name="why"/> is null; otherwise disables it and keeps the reason for a tap.</summary>
        private void SetEnabled(string name, string why)
        {
            VisualElement button = _ui.Q(name);
            bool on = why == null;
            button.tooltip = why ?? string.Empty;
            button.EnableInClassList("is-disabled", !on);
            if (!on && _armed == button)
            {
                Disarm();
            }
        }

        private static string Reason(RejectReason reason)
        {
            return reason == RejectReason.OnCooldown ? "They are still recovering from the last surge." : Texts.Reason(reason);
        }
    }
}
