using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Host.Narrative;
using Deadswitch.Host.Reports;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// OPS: defense setup (SPEC-005). Threat card, posture cards, garrison sockets, the AI's Confidence and
    /// recommendation (Set &amp; Go), lockdown and the delegation ladder. Every control is a sim command; refusals
    /// are explained under the controls. Layout and styles: Resources/UI/Ops.uxml + Ops.uss.
    /// </summary>
    public sealed class OpsScreen : IGameScreen
    {
        private static readonly Posture[] Postures = { Posture.None, Posture.Turtle, Posture.Dark, Posture.Evacuate };
        private static readonly string[] PostureIds = { "posture-none", "posture-turtle", "posture-dark", "posture-evacuate" };
        private static readonly string[] DelegationIds = { "deleg-manual", "deleg-routines", "deleg-autopilot" };

        private static readonly string[] DelegationLines =
        {
            "I only advise. You run everything.",
            "I run the build queue and routine upkeep. You keep the final word.",
            "I also set the defense when a raid comes while you are away. I may get it wrong.",
        };

        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private readonly VisualElement _threat;
        private readonly VisualElement _sockets;
        private readonly VisualElement _meter;
        private readonly VisualElement _setGo;
        private readonly VisualElement _lockdown;
        private readonly Label _reason;
        private bool _visible;

        public OpsScreen(System.Action<int> openReport)
        {
            _host = GameHost.Instance;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Ops");
            Root.Add(tree);
            Icons.Attach(tree);
            _ui = tree;
            _threat = Q("threat");
            _sockets = Q("sockets");
            _meter = Q("confidence-meter");
            _setGo = Q("set-go");
            _lockdown = Q("lockdown");
            _reason = Q<Label>("ops-reason");

            for (int i = 0; i < Postures.Length; i++)
            {
                Posture p = Postures[i];
                Q(PostureIds[i]).RegisterCallback<ClickEvent>(_ => Run(Command.SetPosture(p)));
            }

            for (int i = 0; i < DelegationIds.Length; i++)
            {
                var level = (DelegationLevel)i;
                Q(DelegationIds[i]).RegisterCallback<ClickEvent>(_ => Run(Command.SetDelegation(level)));
            }

            _setGo.RegisterCallback<ClickEvent>(_ => SetAndGo());
            Q("alerts-toggle").RegisterCallback<ClickEvent>(_ =>
            {
                Notifications.LocalAlerts.SetEnabled(!Notifications.LocalAlerts.Enabled);
                Refresh();
            });
            _lockdown.RegisterCallback<ClickEvent>(_ => Run(Command.UseOverride(OverrideKind.Lockdown)));
            Q("purge-pay").RegisterCallback<ClickEvent>(_ => Run(Command.PayPurgeTribute()));
            Q("purge-retreat").RegisterCallback<ClickEvent>(_ =>
            {
                Run(Command.SetGarrison(0));
                Run(Command.SetPosture(Posture.Evacuate));
            });
            Q("purge-prepare").RegisterCallback<ClickEvent>(_ =>
            {
                Run(Command.SetGarrison(_host.Sim.Config.Defense.GarrisonSlots));
                Run(Command.SetPosture(Posture.Turtle));
            });
            Q("shield-toggle").RegisterCallback<ClickEvent>(_ => Run(Command.ActivateShield()));
            Q("tribute-toggle").RegisterCallback<ClickEvent>(_ => Run(Command.SetTributeOrder(!_host.Sim.State.TributeOrder)));
            Q("last-report").RegisterCallback<ClickEvent>(_ => openReport(BattleReport.LatestRaidId(_host.Sim.Log.Events)));
            BuildSockets();

            _host.Ticked += () =>
            {
                if (_visible)
                {
                    Refresh();
                }
            };
            UiRoot.Instance.Frame += _ =>
            {
                if (_visible && _host.Sim.State.RaidId != 0)
                {
                    Q<Label>("threat-time").text = Fmt.Countdown(_host.SecondsUntilTick(_host.Sim.State.RaidArriveTick));
                }

                if (_visible && _host.Sim.State.PurgeStage >= PurgeStage.Staging)
                {
                    Q<Label>("purge-time").text = Fmt.Countdown(_host.SecondsUntilTick(_host.Sim.State.PurgeAtTick));
                }
            };
        }

        public string Id => "ops";

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

        private VisualElement Q(string name)
        {
            return _ui.Q(name);
        }

        private T Q<T>(string name)
            where T : VisualElement
        {
            return _ui.Q<T>(name);
        }

        private void BuildSockets()
        {
            _sockets.Clear();
            int slots = _host.Sim.Config.Defense.GarrisonSlots;
            for (int i = 0; i < slots; i++)
            {
                int index = i;
                var socket = new VisualElement();
                socket.AddToClassList("ops-socket");
                socket.style.width = Length.Percent((100f / slots) - 1.5f);
                socket.Add(Icons.Create("people", "ops-socket__icon"));
                socket.RegisterCallback<ClickEvent>(_ =>
                {
                    int garrison = _host.Sim.State.Garrison;
                    Run(Command.SetGarrison(garrison == index + 1 ? index : index + 1));
                });
                _sockets.Add(socket);
            }
        }

        private void Refresh()
        {
            GameState s = _host.Sim.State;
            SimConfig c = _host.Sim.Config;
            bool raid = s.RaidId != 0;

            // threat: signature by color + shape + label (doc 10 s4)
            bool red = raid && s.RaidKind != AttackKind.Raid;
            _threat.EnableInClassList("ds-panel--amber", raid && !red);
            _threat.EnableInClassList("ds-panel--red", red);
            Q("threat-pip").EnableInClassList("ds-pip--amber", raid && !red);
            Q("threat-pip").EnableInClassList("ds-pip--red", red);
            Q("threat-pip").EnableInClassList("ds-pip--diamond", s.RaidKind != AttackKind.Siege);
            Q("threat-pip").EnableInClassList("ds-pip--off", !raid);
            Q<Label>("threat-kind").text = raid ? Names.Attack(s.RaidKind) + " INCOMING" : "NO CONTACT";
            Q<Label>("threat-kind").EnableInClassList("t-amber", raid && !red);
            Q<Label>("threat-kind").EnableInClassList("t-red", red);
            Q<Label>("threat-time").EnableInClassList("t-amber", !red);
            Q<Label>("threat-time").EnableInClassList("t-red", red);
            RefreshPurge(s, c);
            RefreshAway(s, c);
            Q<Label>("threat-time").EnableInClassList("is-hidden", !raid);
            Q("threat-intel").EnableInClassList("is-hidden", !raid);
            Q("threat-quiet").EnableInClassList("is-hidden", raid);
            if (raid)
            {
                Q<Label>("threat-gate").text = s.RaidGateReported == RaidGate.None ? "?" : Names.Gate(s.RaidGateReported);
                Q<Label>("threat-est").text = s.RaidEstimate > 0 ? Fmt.Num(s.RaidEstimate) : "?";
                Q<Label>("threat-time").text = Fmt.Countdown(_host.SecondsUntilTick(s.RaidArriveTick));
            }

            bool lockReady = raid && s.OverrideCharges > 0 && s.Tick >= s.OverrideCooldownUntil;
            _lockdown.EnableInClassList("is-disabled", !lockReady);
            Kit.SetButtonText(_lockdown, "EMERGENCY LOCKDOWN  //  OVR " + s.OverrideCharges + "/" + OverrideSystem.MaxCharges(s, c));

            int last = BattleReport.LatestRaidId(_host.Sim.Log.Events);
            Q("last-report").style.display = last > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            Kit.SetButtonText(Q("last-report"), "LAST REPORT // RAID " + last);

            // posture
            for (int i = 0; i < Postures.Length; i++)
            {
                Q(PostureIds[i]).EnableInClassList("is-selected", s.Posture == Postures[i]);
            }

            Q<Label>("posture-turtle-fx").text = "+" + c.Defense.TurtleDefensePct + "% DEFENSE";
            Q<Label>("posture-dark-fx").text = c.Defense.DarkMissPct + "% MISS // -" + Fmt.Num(c.Defense.DarkUpkeepPerHour) + "/H ENERGY";
            Q<Label>("posture-evacuate-fx").text = "NO CASUALTIES // LOOT x" + c.Defense.EvacuateLootPct + "%";

            // garrison
            if (_sockets.childCount != c.Defense.GarrisonSlots)
            {
                BuildSockets();
            }

            for (int i = 0; i < _sockets.childCount; i++)
            {
                _sockets[i].EnableInClassList("is-filled", i < s.Garrison);
            }

            Q<Label>("garrison-note").text = "+" + c.Defense.DefensePerDefender + " DEF EACH // " + (s.People - s.Garrison) + " ON CREW DUTY";

            // the AI's read and recommendation
            int defense = Defense.Rating(s, c);
            Q<Label>("read-def").text = "DEF " + Fmt.Num(defense);
            Q<Label>("read-est").text = raid ? "EST " + Fmt.Num(s.RaidEstimate) : "EST --";
            var band = Q<Label>("confidence-band");
            if (raid && s.RaidEstimate > 0)
            {
                int conf = AiSystem.ConfidencePct(defense, s.RaidEstimate);
                Q<Label>("confidence").text = conf.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string tone = conf < 35 ? "red" : (conf < 70 ? "amber" : "phosphor");
                band.text = conf < 35 ? "LOW" : (conf < 70 ? "FAIR" : "HIGH");
                SetTone(band, _meter, tone);
                Kit.SetMeter(_meter, conf / 100f);
            }
            else
            {
                Q<Label>("confidence").text = "--";
                band.text = "NO CONTACT";
                SetTone(band, _meter, "dim");
                Kit.SetMeter(_meter, 0f);
            }

            AiSystem.Recommend(s, c, out Posture rec, out int recGarrison);
            Q<Label>("recommend").text = !raid ? "NOTHING TO DEFEND AGAINST"
                : rec == Posture.None ? "HOLD // NO CHANGE"
                : rec == Posture.Turtle ? "TURTLE // " + recGarrison + " DEFENDERS"
                : Fmt.PostureName(rec);
            _setGo.EnableInClassList("is-disabled", !raid);

            // delegation
            for (int i = 0; i < DelegationIds.Length; i++)
            {
                Q(DelegationIds[i]).EnableInClassList("is-selected", (int)s.Delegation == i);
            }

            Q<Label>("deleg-desc").text = DelegationLines[(int)s.Delegation];
            bool alerts = Notifications.LocalAlerts.Enabled;
            Q("alerts-toggle").EnableInClassList("is-on", alerts);
            Q<Label>("alerts-label").text = alerts ? "ON" : "OFF";
        }

        private void RefreshPurge(GameState s, SimConfig c)
        {
            PurgeStage stage = s.PurgeStage;
            Q("purge").EnableInClassList("is-hidden", stage == PurgeStage.None);
            if (stage == PurgeStage.None)
            {
                return;
            }

            string[] names = { string.Empty, "PURGE // RUMOR", "PURGE // STAGING CONFIRMED", "PURGE // ULTIMATUM" };
            string[] lines =
            {
                string.Empty,
                "Chatter about a purge. It may be nothing. If it is real, staging comes next.",
                "A purge force is gathering. Build defense now; the ultimatum comes " + c.Threats.PurgeUltimatumHours + " h before the strike.",
                "Pay tribute, pull everyone out, or hold the wall. They take everything they can reach.",
            };
            Q<Label>("purge-stage").text = names[(int)stage];
            Q<Label>("purge-desc").text = lines[(int)stage];
            Q<Label>("purge-time").EnableInClassList("is-hidden", stage == PurgeStage.Rumor);
            Q<Label>("purge-time").text = Fmt.Countdown(_host.SecondsUntilTick(s.PurgeAtTick));
            Q("purge-answers").EnableInClassList("is-hidden", stage != PurgeStage.Ultimatum);
            Q<Label>("purge-pay-label").text = "PAY " + Fmt.Num(c.Threats.PurgeTributeEnergy) + " E + " + Fmt.Num(c.Threats.PurgeTributeCompute) + " C";
            Q("purge-pay").EnableInClassList("is-disabled", s.Energy < c.Threats.PurgeTributeEnergy || s.Compute < c.Threats.PurgeTributeCompute);
            Q("purge-retreat").EnableInClassList("is-selected", s.Posture == Posture.Evacuate);
            Q("purge-prepare").EnableInClassList("is-selected", s.Posture == Posture.Turtle);
        }

        private void RefreshAway(GameState s, SimConfig c)
        {
            bool armed = s.ShieldUntilTick > s.Tick;
            bool holding = ThreatSystem.Shielded(s);
            Q<Label>("shield-title").text = "VACATION SHIELD // " + (holding ? "HOLDING" : armed ? "RISING" : s.ShieldCharges + (s.ShieldCharges == 1 ? " CHARGE" : " CHARGES"));
            Q<Label>("shield-desc").text = holding
                ? "No attacks until " + Fmt.Clock(s.ShieldUntilTick) + ". Upkeep halved; timers keep running."
                : armed
                    ? "Rises at " + Fmt.Clock(s.ShieldFromTick) + ". Anything already moving still comes."
                    : "Pauses attacks for " + c.Threats.ShieldMaxHours + " h after a " + (c.Threats.ShieldDelayMinutes / 60) + " h delay. Upkeep halved. A charge every " + c.Threats.ShieldRegenDays + " days (max " + c.Threats.ShieldMaxCharges + ").";
            Q("shield-toggle").EnableInClassList("is-on", armed);
            Q("shield-toggle").EnableInClassList("is-disabled", !armed && (s.ShieldCharges < 1 || s.RaidId != 0 || s.PurgeStage != PurgeStage.None));
            Q<Label>("shield-label").text = holding ? "ON" : armed ? "RISING" : "RAISE";
            Q<Label>("tribute-desc").text = "When a raid arrives while you are away, pay " + c.Threats.TributePct + "% of stored energy (at least " + c.Threats.TributeMinEnergy + ") and it leaves. Not sieges or purges.";
            Q("tribute-toggle").EnableInClassList("is-on", s.TributeOrder);
            Q<Label>("tribute-label").text = s.TributeOrder ? "ON" : "OFF";
        }

        private void SetAndGo()
        {
            GameState s = _host.Sim.State;
            if (s.RaidId == 0)
            {
                return;
            }

            AiSystem.Recommend(s, _host.Sim.Config, out Posture posture, out int garrison);
            if (posture == Posture.None)
            {
                _reason.text = "Current setup holds. I would change nothing.";
                return;
            }

            Run(Command.SetGarrison(garrison));
            Run(Command.SetPosture(posture));
        }

        private void Run(Command command)
        {
            CommandResult r = _host.Execute(command);
            _reason.text = r.Accepted || r.Reason == RejectReason.NoChange ? string.Empty : Texts.Reason(r.Reason);
            Refresh();
        }

        private static void SetTone(Label band, VisualElement meter, string tone)
        {
            foreach (string t in new[] { "red", "amber", "phosphor" })
            {
                band.EnableInClassList("t-" + t, t == tone);
                meter.EnableInClassList("ds-meter--" + t, t == tone && t != "phosphor");
            }

            band.EnableInClassList("t-dim", tone == "dim");
        }
    }
}
