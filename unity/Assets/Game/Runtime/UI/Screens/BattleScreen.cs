using Deadswitch.Game.Base;
using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Host.Narrative;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// LIVE BATTLE (F-037, SPEC-020): the handler commands the fight at the wall. Forces read (our defense against
    /// the AI's estimate, both after abilities), the time until it resolves, and four one-shot abilities with their
    /// costs. Opens on contact; the 3D fight plays through the middle. Layout: Resources/UI/Battle.uxml.
    /// </summary>
    public sealed class BattleScreen : IGameScreen
    {
        private static readonly string[] Fx = { "DEF +{0}%", "THEM -{0}%", "DEF +{0}%", "THEM -{0}% // CORRUPTS ME" };

        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private readonly Label _reason;
        private bool _visible;

        public BattleScreen(ScreenRouter router)
        {
            _host = GameHost.Instance;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Battle");
            Root.Add(tree);
            _ui = tree;
            _reason = _ui.Q<Label>("bat-reason");
            for (int i = 0; i < 4; i++)
            {
                var ability = (BattleAbility)i;
                _ui.Q("ab-" + i).RegisterCallback<ClickEvent>(_ => Run(Command.UseBattleAbility(ability)));
            }

            _ui.Q("bat-slow").RegisterCallback<ClickEvent>(_ =>
            {
                if (LiveBattle.Instance != null)
                {
                    LiveBattle.Instance.SlowMotion = !LiveBattle.Instance.SlowMotion;
                }

                Refresh();
            });
            _ui.Q("bat-posture").RegisterCallback<ClickEvent>(_ => router.Show("ops"));
            _host.Ticked += () =>
            {
                if (!_visible)
                {
                    return;
                }

                if (!BattleSystem.InBattle(_host.Sim.State))
                {
                    router.Show("base");
                    return;
                }

                Refresh();
            };
            UiRoot.Instance.Frame += _ =>
            {
                if (_visible)
                {
                    _ui.Q<Label>("bat-time").text = Fmt.Countdown(_host.SecondsUntilTick(_host.Sim.State.BattleEndTick));
                }
            };
        }

        public string Id => "battle";

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
            _reason.text = r.Accepted ? string.Empty : r.Reason == RejectReason.AbilityUsed ? "Already spent this fight." : Texts.Reason(r.Reason);
            Refresh();
        }

        private void Refresh()
        {
            GameState s = _host.Sim.State;
            SimConfig c = _host.Sim.Config;
            var b = c.Battle;
            bool red = s.RaidKind != AttackKind.Raid;
            _ui.Q("bat-pip").EnableInClassList("ds-pip--red", red);
            _ui.Q("bat-pip").EnableInClassList("ds-pip--amber", !red);
            _ui.Q<Label>("bat-title").text = "CONTACT // " + Names.Attack(s.RaidKind) + " // " + (s.RaidGate == RaidGate.None ? "?" : Names.Gate(s.RaidGate));

            int def = s.BattleDefensePct > 0 ? SimMath.PctFloor(Defense.Rating(s, c), 100 + s.BattleDefensePct) : Defense.Rating(s, c);
            int est = s.RaidEstimate > 0 ? SimMath.PctFloor(s.RaidEstimate, 100 - s.BattleStrengthCutPct) : 0;
            _ui.Q<Label>("bat-def").text = Fmt.Num(def);
            _ui.Q<Label>("bat-att").text = est > 0 ? "~" + Fmt.Num(est) : "?";
            float share = est > 0 ? (float)def / (def + est) : 0.5f;
            _ui.Q("bat-bar-def").style.width = Length.Percent(share * 100f);
            _ui.Q<Label>("bat-read").text = est <= 0 ? "AI: I cannot read them. Trust the wall." : def >= est ? "AI: The wall should hold. Spend only if you want certainty." : def * 10 >= est * 8 ? "AI: Close. One more push decides it." : "AI: We are outgunned. Spend what you have.";

            int[] values = { b.FocusDefensePct, b.BarrageStrengthPct, b.TakeoverDefensePct, b.SeizeStrengthPct };
            string charges = "OVERRIDE " + s.OverrideCharges + "/" + OverrideSystem.MaxCharges(s, c);
            string[] costs = { b.FocusCompute + " C", b.BarrageEnergy + " E", charges, charges };
            bool[] afford = { s.Compute >= b.FocusCompute, s.Energy >= b.BarrageEnergy, OverrideSystem.Ready(s), OverrideSystem.Ready(s) };
            for (int i = 0; i < 4; i++)
            {
                bool used = BattleSystem.Used(s, (BattleAbility)i);
                _ui.Q<Label>("ab-" + i + "-fx").text = string.Format(System.Globalization.CultureInfo.InvariantCulture, Fx[i], values[i]);
                _ui.Q<Label>("ab-" + i + "-cost").text = used ? "SPENT" : costs[i];
                _ui.Q("ab-" + i).EnableInClassList("is-used", used);
                _ui.Q("ab-" + i).EnableInClassList("is-disabled", !used && !afford[i]);
            }

            bool slow = LiveBattle.Instance != null && LiveBattle.Instance.SlowMotion;
            _ui.Q("bat-slow").EnableInClassList("is-on", slow);
            _ui.Q<Label>("bat-slow-label").text = slow ? "SLOW-MO ON" : "SLOW-MO OFF";
        }
    }
}
