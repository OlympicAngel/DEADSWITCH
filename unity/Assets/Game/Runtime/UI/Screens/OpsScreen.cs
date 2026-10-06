using System.Collections.Generic;
using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Host.Narrative;
using Deadswitch.Host.Reports;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
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

        internal static readonly string[] DelegationLines =
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
        private readonly VisualElement _pager;
        private bool _visible;
        private int _lastThreatId;

        public OpsScreen(System.Action<int> openReport)
        {
            _host = GameHost.Instance;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Ops");
            Root.Add(tree);
            Icons.Attach(tree);
            _ui = tree;
            _pager = _ui.Q("ops-pager");
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
            Q("yard-clear").RegisterCallback<ClickEvent>(_ => Run(Command.ClearWreckage()));
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
                    FocusThreat(_host.Sim.State);
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
            FocusThreat(_host.Sim.State);

            // nothing to answer and nothing to clear: open on DEFEND, the page with the decisions (SPEC-042 finding 10)
            GameState s = _host.Sim.State;
            bool quiet = s.RaidId == 0 && s.PurgeStage == PurgeStage.None && Q("yard").ClassListContains("is-hidden");
            if (quiet && Q("page-threat").style.display.value == DisplayStyle.Flex)
            {
                Pager.Show(_pager, "page-defend");
            }
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
            Pager.Badge(_pager, "page-threat", raid || s.PurgeStage != PurgeStage.None ? 1 : 0);
            Q<Label>("threat-time").EnableInClassList("is-hidden", !raid);
            Q("threat-cap").EnableInClassList("is-hidden", !raid);
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
            Kit.SetButtonText(_lockdown, "EMERGENCY LOCKDOWN  //  OVERRIDE " + s.OverrideCharges + "/" + OverrideSystem.MaxCharges(s, c));

            int last = BattleReport.LatestRaidId(_host.Sim.Log.Events);
            Q("last-report").style.display = last > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            Kit.SetButtonText(Q("last-report"), "LAST REPORT // RAID " + last);

            // posture
            for (int i = 0; i < Postures.Length; i++)
            {
                Q(PostureIds[i]).EnableInClassList("is-selected", s.Posture == Postures[i]);
            }

            // adaptive enemies (SPEC-027): the incoming faction's learned counters, stated plainly
            int ct = AdaptSystem.Counter(s, c, Posture.Turtle);
            int cd = AdaptSystem.Counter(s, c, Posture.Dark);
            int ce = AdaptSystem.Counter(s, c, Posture.Evacuate);
            Q<Label>("posture-turtle-fx").text = "+" + System.Math.Max(0, c.Defense.TurtleDefensePct - ct) + "% DEFENSE" + (ct > 0 ? " // THEY BRING CHARGES" : string.Empty);
            Q<Label>("posture-dark-fx").text = System.Math.Max(0, c.Defense.DarkMissPct - cd) + "% MISS // -" + Fmt.Num(c.Defense.DarkUpkeepPerHour) + "/H ENERGY" + (cd > 0 ? " // THEY SWEEP" : string.Empty);
            Q<Label>("posture-evacuate-fx").text = "NO CASUALTIES // LOOT x" + System.Math.Min(300, c.Defense.EvacuateLootPct + ce) + "%" + (ce > 0 ? " // THEY HUNT CACHES" : string.Empty)
                + (HeldOutposts(s) > 0 ? " // AN OUTPOST COVERS THE HUB" : string.Empty);
            Q<Label>("posture-turtle-fx").EnableInClassList("t-amber", ct > 0);
            Q<Label>("posture-dark-fx").EnableInClassList("t-amber", cd > 0);
            Q<Label>("posture-evacuate-fx").EnableInClassList("t-amber", ce > 0);

            // garrison
            if (_sockets.childCount != c.Defense.GarrisonSlots)
            {
                BuildSockets();
            }

            for (int i = 0; i < _sockets.childCount; i++)
            {
                _sockets[i].EnableInClassList("is-filled", i < s.Garrison);
            }

            Q<Label>("garrison-note").text = "+" + c.Defense.DefensePerDefender + " DEFENSE EACH // " + (s.People - s.Garrison) + " WORKERS";

            // the AI's read and recommendation
            int defense = Defense.Rating(s, c);
            Q<Label>("read-def").text = "DEFENSE " + Fmt.Num(defense);
            Q<Label>("read-est").text = raid ? "~" + Fmt.Num(s.RaidEstimate) + " ATTACKERS" : "ATTACKERS --";
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

            // forces and counters (SPEC-035): the mix once the warning names it, our line by family
            bool forces = raid && ForcesKnown(s);
            Q<Label>("forces").text = !raid ? "FORCES // NO CONTACT" : forces
                ? "FORCES // " + s.RaidInfantryPct + "% INFANTRY  " + s.RaidDronePct + "% DRONES  " + s.RaidVehiclePct + "% VEHICLES"
                : "FORCES // UNKNOWN";
            Q<Label>("forces-line").text = "OUR LINE // " + Line(s, c, UnitFamily.Infantry, "DEFENDERS", forces) + "  " + Line(s, c, UnitFamily.Drones, "DRONES", forces) + "  " + Line(s, c, UnitFamily.Vehicles, "VEHICLES", forces);

            AiSystem.Recommend(s, c, out Posture rec, out int recGarrison);
            Q<Label>("recommend").text = !raid ? "NOTHING TO DEFEND AGAINST"
                : rec == Posture.None ? "KEEP AS IS // NO CHANGE"
                : rec == Posture.Turtle ? "FORTIFY // " + recGarrison + " DEFENDERS"
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

        /// <summary>Jumps to the THREAT page once per new raid, siege or purge stage while the screen is open.</summary>
        private void FocusThreat(GameState s)
        {
            int id = s.RaidId != 0 ? s.RaidId : (s.PurgeStage != PurgeStage.None ? -(int)s.PurgeStage : 0);
            if (id != 0 && id != _lastThreatId)
            {
                Pager.Show(_pager, "page-threat");
            }

            _lastThreatId = id;
        }

        private void RefreshPurge(GameState s, SimConfig c)
        {
            PurgeStage stage = s.PurgeStage;
            Q("purge").EnableInClassList("is-hidden", stage == PurgeStage.None);
            Q("yard").EnableInClassList("is-hidden", s.Wreckage == 0);
            if (s.Wreckage > 0)
            {
                int clear = c.Scars.ClearEnergyPerWreck * s.Wreckage;
                Q<Label>("yard-title").text = "YARD // " + s.Wreckage + (s.Wreckage == 1 ? " WRECK" : " WRECKS");
                Q<Label>("yard-desc").text = "Regrowth -" + (100 - ScarSystem.RegrowthPct(s, c)) + "% while the wrecks stay.";
                Kit.SetButtonText(Q("yard-clear"), "CLEAR // " + Fmt.Num(clear) + " ENERGY");
                Q("yard-clear").EnableInClassList("is-disabled", s.Energy < clear || s.RaidId != 0);
            }
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
            Q<Label>("purge-pay-label").text = "PAY " + Fmt.Num(c.Threats.PurgeTributeEnergy) + " ENERGY + " + Fmt.Num(c.Threats.PurgeTributeCompute) + " COMPUTE";
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
            if (s.Ironman)
            {
                // doc 10 s1.2: Ironman has no shield
                Q<Label>("shield-title").text = "VACATION SHIELD // OFF IN HARDCORE";
                Q<Label>("shield-desc").text = "This run is Hardcore: there is no vacation shield. Plan your absences.";
                Q("shield-toggle").EnableInClassList("is-disabled", true);
                Q<Label>("shield-label").text = "NONE";
            }
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

        private static int HeldOutposts(GameState s)
        {
            int n = 0;
            foreach (SiteState site in s.Sites)
            {
                n += site.Outpost ? 1 : 0;
            }

            return n;
        }

        /// <summary>True when the warning for the current attack named its forces (SPEC-035).</summary>
        private bool ForcesKnown(GameState s)
        {
            IReadOnlyList<SimEvent> log = _host.Sim.Log.Events;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                SimEvent e = log[i];
                if (e.Kind == EventKind.RaidForces && e.A == s.RaidId)
                {
                    return true;
                }

                if (e.Kind == EventKind.RaidWarning && e.A == s.RaidId)
                {
                    return false;
                }
            }

            return false;
        }

        private static string Line(GameState s, SimConfig c, UnitFamily family, string name, bool countered)
        {
            int value = Defense.Family(s, c, family, s.Garrison);
            int pts = UnitSystem.CounterPts(s, c, family);
            return name + " " + Fmt.Num(value) + (countered && value > 0 && pts != 0 ? " (" + (pts > 0 ? "+" : string.Empty) + pts + "%)" : string.Empty);
        }
    }
}
