using System.Collections.Generic;
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
    /// SECTOR MAP (F-019): faction heat, the sites around the Hub, operations in the field and the selected site's
    /// sheet (scout, raid, hack with the AI's possibly wrong odds; claim cleared ruins as an outpost). Every action
    /// is a sim command. Layout: Resources/UI/Map.uxml + Map.uss.
    /// </summary>
    public sealed class MapScreen : IGameScreen
    {
        private static readonly string[] FactionClass = { "map-site--rust", "map-site--vanguard", "map-site--church", "map-site--holdout" };

        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private readonly VisualElement _sites;
        private readonly VisualElement _ops;
        private readonly Label _reason;
        private readonly List<VisualElement> _markers = new List<VisualElement>();
        private int _selected;
        private OpKind _kind = OpKind.Raid;
        private int _squad = 4;
        private int _compute = 40;
        private bool _visible;
        private long _allyArmedAt = -1;
        private readonly List<Label> _opTimes = new List<Label>();
        private string _opsSignature;

        public MapScreen()
        {
            _host = GameHost.Instance;
            Root = new VisualElement();
            TemplateContainer tree = UiRoot.Load("Map");
            Root.Add(tree);
            _ui = tree;
            _sites = _ui.Q("map-sites");
            _ops = _ui.Q("map-ops");
            _reason = _ui.Q<Label>("map-reason");
            for (int f = 0; f < WorldSystem.FactionCount; f++)
            {
                Kit.BuildMeter(_ui.Q("heat-" + f + "-meter"));
            }

            BuildMarkers();
            _ui.Q("op-scout").RegisterCallback<ClickEvent>(_ => Pick(OpKind.Scout));
            _ui.Q("op-raid").RegisterCallback<ClickEvent>(_ => Pick(OpKind.Raid));
            _ui.Q("op-hack").RegisterCallback<ClickEvent>(_ => Pick(OpKind.Hack));
            _ui.Q("op-sabotage").RegisterCallback<ClickEvent>(_ => Pick(OpKind.Sabotage));
            _ui.Q("op-minus").RegisterCallback<ClickEvent>(_ => Step(-1));
            _ui.Q("op-plus").RegisterCallback<ClickEvent>(_ => Step(1));
            _ui.Q("op-launch").RegisterCallback<ClickEvent>(_ => Run(Command.LaunchOp(_selected, _kind, _kind == OpKind.Hack ? _compute : _squad)));
            _ui.Q("site-claim").RegisterCallback<ClickEvent>(_ => Run(Command.ClaimOutpost(_selected)));
            for (int f = 0; f < WorldSystem.FactionCount; f++)
            {
                var faction = (Faction)f;
                _ui.Q("spy-" + f + "-a").RegisterCallback<ClickEvent>(_ => Run(IntelSystem.Has(_host.Sim.State, faction) ? Command.FrameFaction(faction) : Command.PlantSpy(faction)));
                _ui.Q("spy-" + f + "-b").RegisterCallback<ClickEvent>(_ => Run(Command.RecallSpy(faction)));
                _ui.Q("pact-" + f).RegisterCallback<ClickEvent>(_ => Run(Command.ProposeCeasefire(faction)));
                _ui.Q("ally-" + f).RegisterCallback<ClickEvent>(_ => Ally(faction));
            }

            for (int g = 0; g <= (int)TradeGood.Compute; g++)
            {
                var good = (TradeGood)g;
                _ui.Q("trade-" + g).RegisterCallback<ClickEvent>(_ => Run(Command.Trade(WorldSystem.Sites[_selected].Owner, good)));
            }

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
                    RefreshOps();
                }
            };
        }

        public string Id => "map";

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

        private void BuildMarkers()
        {
            _sites.Clear();
            _markers.Clear();
            for (int i = 0; i < WorldSystem.Sites.Count; i++)
            {
                SiteDef d = WorldSystem.Sites[i];
                int index = i;
                var marker = new VisualElement();
                marker.AddToClassList("map-site");
                marker.AddToClassList(FactionClass[(int)d.Owner]);
                marker.style.left = Length.Percent(50f + (d.MapX * 0.45f));
                marker.style.top = Length.Percent(50f - (d.MapY * 0.45f));
                var mark = new VisualElement();
                mark.AddToClassList("map-site__mark");
                marker.Add(mark);
                marker.Add(Kit.Label(d.Name, "map-site__name"));
                marker.RegisterCallback<ClickEvent>(_ =>
                {
                    _selected = index;
                    _reason.text = string.Empty;
                    Refresh();
                });
                _sites.Add(marker);
                _markers.Add(marker);
            }
        }

        private void Pick(OpKind kind)
        {
            _kind = kind;
            if (kind == OpKind.Sabotage)
            {
                _squad = SimMath.Clamp(_squad, 1, _host.Sim.Config.World.SabotageMaxSquad);
            }

            Refresh();
        }

        private void Step(int d)
        {
            if (_kind == OpKind.Hack)
            {
                _compute = SimMath.Clamp(_compute + (d * 10), 10, _host.Sim.Config.Compute.Cap);
            }
            else
            {
                _squad = SimMath.Clamp(_squad + d, 1, _kind == OpKind.Sabotage ? _host.Sim.Config.World.SabotageMaxSquad : 20);
            }

            Refresh();
        }

        /// <summary>Ally with a faction, or end the alliance with two taps (it costs heat).</summary>
        private void Ally(Faction faction)
        {
            if (!DiplomacySystem.Allied(_host.Sim.State, faction))
            {
                Run(Command.ProposeAlliance(faction));
                return;
            }

            long now = System.Environment.TickCount;
            if (_allyArmedAt < 0 || now - _allyArmedAt > 4000)
            {
                _allyArmedAt = now;
                _reason.text = "Tap again to end the alliance. They will remember it.";
                Refresh();
                return;
            }

            _allyArmedAt = -1;
            Run(Command.EndAlliance());
        }

        private void Run(Command command)
        {
            CommandResult r = _host.Execute(command);
            _reason.text = r.Accepted ? string.Empty : Reason(r.Reason);
            Refresh();
        }

        private static string Reason(RejectReason reason)
        {
            switch (reason)
            {
                case RejectReason.OpsBusy: return "Every team is already out. Wait for one to come back.";
                case RejectReason.SiteCooldown: return "Nothing left there to take. Not yet.";
                case RejectReason.NotClaimable: return "Only ground we have just beaten can be taken: cleared ruins, or a faction outpost after a won raid.";
                case RejectReason.NotEnoughFuel: return "Not enough fuel.";
                case RejectReason.SpyActive: return "We already have someone in that camp.";
                case RejectReason.PactActive: return "One ceasefire at a time, and not so soon after the last.";
                case RejectReason.NoSpy: return "Nobody of ours is in that camp.";
                case RejectReason.NotTrusted: return "They only stand with a Hub that has given them no reason to hate it. Cool their heat first.";
                case RejectReason.AllianceActive: return "One alliance at a time.";
                default: return Texts.Reason(reason);
            }
        }

        private void Refresh()
        {
            GameState s = _host.Sim.State;
            SimConfig c = _host.Sim.Config;

            _ui.Q<Label>("map-ops-count").text = "OPS " + s.Ops.Count + "/" + WorldSystem.MaxOps(s, c) + " // FUEL " + Fmt.Num(s.Fuel);
            for (int f = 0; f < WorldSystem.FactionCount; f++)
            {
                HeatLevel level = WorldSystem.Level(s.Heat[f]);
                bool crippled = s.SabotageFaction == f && s.Tick < s.SabotageUntilTick;
                _ui.Q<Label>("heat-" + f + "-level").text = level.ToString().ToUpperInvariant() + " " + WorldSystem.Percent(s.Heat[f]) + (crippled ? " // CRIPPLED" : string.Empty);
                Kit.SetMeter(_ui.Q("heat-" + f + "-meter"), s.Heat[f] / 100_000f);
                _ui.Q("heat-" + f).EnableInClassList("is-hot", level >= HeatLevel.Hunted);

                // adaptive enemies (SPEC-027): what they learned about us, and how dug in their sites are
                string learned = string.Empty;
                foreach (Posture p in new[] { Posture.Turtle, Posture.Dark, Posture.Evacuate })
                {
                    int lv = AdaptSystem.Learned(s, (Faction)f, p);
                    learned += lv > 0 ? (learned.Length > 0 ? " " : "KNOWS ") + Fmt.PostureName(p) + " " + lv : string.Empty;
                }

                int fort = s.Fortified[f];
                string adapt = learned + (fort > 0 ? (learned.Length > 0 ? " // " : string.Empty) + "FORT " + fort : string.Empty);
                if (LuckSystem.Regrouping(s, (Faction)f))
                {
                    // an opportunity window (SPEC-028): their sites are thin right now
                    adapt = "REGROUPING " + Fmt.Countdown(_host.SecondsUntilTick(s.RegroupUntilTick)) + (adapt.Length > 0 ? " // " + adapt : string.Empty);
                }
                _ui.Q<Label>("adapt-" + f).text = adapt;
                _ui.Q("adapt-" + f).EnableInClassList("is-hidden", adapt.Length == 0);

                // spies (SPEC-019): loyalty is hidden; only a scout's cross-check or a backfire reveals it
                bool spy = IntelSystem.Has(s, (Faction)f);
                Label spyState = _ui.Q<Label>("spy-" + f + "-state");
                spyState.text = spy ? "AGENT INSIDE" : "NO AGENT";
                spyState.EnableInClassList("is-on", spy);
                _ui.Q<Label>("spy-" + f + "-a-label").text = spy ? "FRAME" : "PLANT " + Fmt.Num(c.Intel.SpyEnergy) + " E";
                _ui.Q("spy-" + f + "-a").EnableInClassList("is-disabled", !spy && s.Energy < c.Intel.SpyEnergy);
                _ui.Q("spy-" + f + "-b").EnableInClassList("is-hidden", !spy);

                // ceasefire (SPEC-023): one at a time, priced by heat, none with a Marked faction
                bool peace = DiplomacySystem.Ceasefire(s, (Faction)f);
                bool talks = DiplomacySystem.Price(s, c, (Faction)f, out int pe, out int pf);
                bool cooling = s.Tick < s.CeasefireReadyTick || (s.CeasefireFaction >= 0 && !peace);
                _ui.Q("pact-" + f).EnableInClassList("is-on", peace);
                _ui.Q<Label>("pact-" + f + "-label").text = peace ? "PACT // " + Fmt.Countdown(_host.SecondsUntilTick(s.CeasefireUntilTick))
                    : !talks ? "WILL NOT TALK" : "PACT // " + Fmt.Num(pe) + " E " + Fmt.Num(pf) + " F";
                _ui.Q("pact-" + f).EnableInClassList("is-disabled", !peace && (!talks || cooling || s.CeasefireFaction >= 0 || s.Energy < pe || s.Fuel < pf));

                // alliance (SPEC-025): only with a Cold faction; their fighters man the wall for a daily share
                var d = c.Diplomacy;
                bool allied = DiplomacySystem.Allied(s, (Faction)f);
                bool armed = allied && _allyArmedAt >= 0 && System.Environment.TickCount - _allyArmedAt <= 4000;
                VisualElement ally = _ui.Q("ally-" + f);
                ally.EnableInClassList("is-on", allied);
                ally.EnableInClassList("is-armed", armed);
                _ui.Q("heat-" + f).EnableInClassList("is-allied", allied);
                _ui.Q<Label>("ally-" + f + "-label").text = armed ? "CONFIRM // END" : allied ? "ALLIED // +" + DiplomacySystem.AllyDefense(s, c) + " DEF"
                    : level != HeatLevel.Cold ? "ALLY // NEEDS COLD" : "ALLY // " + Fmt.Num(d.AllianceEnergy) + " E " + Fmt.Num(d.AllianceFuel) + " F";
                ally.EnableInClassList("is-disabled", !allied && (level != HeatLevel.Cold || s.AllyFaction >= 0 || s.Energy < d.AllianceEnergy || s.Fuel < d.AllianceFuel));
            }

            var busy = new HashSet<int>();
            foreach (Operation op in s.Ops)
            {
                busy.Add(op.Site);
            }

            for (int i = 0; i < _markers.Count; i++)
            {
                SiteState st = s.Sites[i];
                _markers[i].EnableInClassList("is-selected", i == _selected);
                _markers[i].EnableInClassList("map-site--outpost", st.Outpost);
                _markers[i].EnableInClassList("map-site--cooldown", s.Tick < st.CooldownUntilTick);
                _markers[i].EnableInClassList("map-site--op", busy.Contains(i));
                _markers[i].Q<Label>().text = WorldSystem.Sites[i].Name + (st.Outpost ? " // OUTPOST" : st.Cleared ? " // CLEARED" : string.Empty);
            }

            bool world = s.WorldEvent != WorldEventKind.None && s.Tick < s.WorldEventUntilTick;
            _ui.Q("map-event").EnableInClassList("is-hidden", !world);
            if (world)
            {
                _ui.Q<Label>("map-event-text").text = LivingTexts.EventName(s.WorldEvent) + " // " + LivingTexts.EventEffect(s.WorldEvent, c).ToUpperInvariant();
            }

            RefreshSheet(s, c);
            RefreshTrade(s, c);
            RefreshOps();
        }

        private void RefreshTrade(GameState s, SimConfig c)
        {
            Faction owner = WorldSystem.Sites[_selected].Owner;
            HeatLevel level = WorldSystem.Level(s.Heat[(int)owner]);
            bool hostile = level == HeatLevel.Marked;
            int left = System.Math.Max(0, c.Living.TradesPerDay - s.TradesToday[(int)owner]);
            _ui.Q<Label>("trade-left").text = hostile ? Names.Faction(owner) + " WILL NOT TRADE" : left + "/" + c.Living.TradesPerDay + " TODAY // " + level.ToString().ToUpperInvariant();
            for (int g = 0; g <= (int)TradeGood.Compute; g++)
            {
                var good = (TradeGood)g;
                int price = LivingSystem.Price(s, c, owner, good);
                bool payFuel = good == TradeGood.EnergyCells;
                _ui.Q<Label>("trade-" + g + "-get").text = "+" + Fmt.Num(LivingSystem.Lot(c, good)) + " " + LivingTexts.Good(good);
                _ui.Q<Label>("trade-" + g + "-pay").text = hostile ? "-" : Fmt.Num(price) + (payFuel ? " F" : " E");
                bool afford = price >= 0 && (payFuel ? s.Fuel >= price : s.Energy >= price);
                _ui.Q("trade-" + g).EnableInClassList("is-disabled", hostile || left == 0 || !afford);
            }
        }

        private void RefreshSheet(GameState s, SimConfig c)
        {
            SiteDef d = WorldSystem.Sites[_selected];
            SiteState st = s.Sites[_selected];
            string[] kinds = { "CONVOY", "OUTPOST", "DATA CENTER", "RUINS" };
            _ui.Q<Label>("site-name").text = d.Name;
            _ui.Q<Label>("site-owner").text = Names.Faction(d.Owner) + " // " + kinds[(int)d.Kind];
            _ui.Q<Label>("site-travel").text = d.TravelHours + " H";
            int estimate = WorldSystem.EstimatedDefense(s, c, _selected);
            _ui.Q<Label>("site-def-label").text = st.Scouted ? "DEFENSE (SCOUTED)" : IntelSystem.Has(s, d.Owner) ? "DEFENSE (AGENT)" : "DEFENSE (AI EST)";
            _ui.Q<Label>("site-def").text = (st.Scouted ? string.Empty : "~") + estimate + (d.Cyber > 0 ? "  CYBER " + d.Cyber : string.Empty);
            _ui.Q<Label>("site-loot").text = d.Energy + " E  " + d.Fuel + " F  " + d.Compute + " C" + (d.CleanData > 0 ? "  + CLEAN DATA" : string.Empty);

            if (_kind == OpKind.Hack && d.Cyber == 0)
            {
                _kind = OpKind.Raid;
            }

            _ui.Q("op-scout").EnableInClassList("is-selected", _kind == OpKind.Scout);
            _ui.Q("op-raid").EnableInClassList("is-selected", _kind == OpKind.Raid);
            _ui.Q("op-hack").EnableInClassList("is-selected", _kind == OpKind.Hack);
            _ui.Q("op-hack").EnableInClassList("is-disabled", d.Cyber == 0);
            _ui.Q("op-sabotage").EnableInClassList("is-selected", _kind == OpKind.Sabotage);

            bool hack = _kind == OpKind.Hack;
            _ui.Q<Label>("op-amount").text = (hack ? _compute : _squad).ToString(System.Globalization.CultureInfo.InvariantCulture);
            _ui.Q<Label>("op-amount-unit").text = hack ? "COMPUTE" : "PEOPLE";
            int odds = WorldSystem.Odds(s, c, d, _kind, _squad, _compute, estimate);
            Label oddsLabel = _ui.Q<Label>("op-odds");
            oddsLabel.text = (st.Scouted ? "ODDS " : "AI ODDS ") + odds + "%";
            oddsLabel.EnableInClassList("t-amber", odds < 60);
            int back = hack ? 1 : 2 * d.TravelHours;
            _ui.Q<Label>("op-cost").text = (hack ? "COMPUTE " + _compute : "FUEL " + WorldSystem.FuelCost(s, c, d, _kind)) + " // BACK IN " + back + " H"
                + (_kind == OpKind.Sabotage ? " // " + Names.Faction(d.Owner) + " -" + c.World.SabotageStrengthPct + "% FOR " + c.World.SabotageHours + " H" : string.Empty);
            string[] verbs = { "SEND SCOUTS", "LAUNCH RAID", "START HACK", "SEND SABOTEURS" };
            Kit.SetButtonText(_ui.Q("op-launch"), verbs[(int)_kind]);
            bool cooling = _kind != OpKind.Scout && s.Tick < st.CooldownUntilTick;
            _ui.Q("op-launch").EnableInClassList("is-disabled", st.Outpost || cooling || s.Ops.Count >= WorldSystem.MaxOps(s, c));

            bool seize = d.Kind == SiteKind.Outpost;
            bool claimable = (d.Kind == SiteKind.Ruins || seize) && st.Cleared && !st.Outpost;
            _ui.Q("site-claim").EnableInClassList("is-hidden", !claimable);
            Kit.SetButtonText(_ui.Q("site-claim"), seize ? "SEIZE AND HOLD // " + c.World.SeizeEnergy + " E" : "CLAIM // " + c.World.OutpostClaimEnergy + " E");
        }

        /// <summary>Rebuilds the ops list only when the set of ops changes (buttons must survive between frames); timers update in place.</summary>
        private void RefreshOps()
        {
            GameState s = _host.Sim.State;
            string signature = string.Empty;
            foreach (Operation op in s.Ops)
            {
                signature += op.Id + (op.ByAi ? "a," : ",");
            }

            if (signature != _opsSignature)
            {
                _opsSignature = signature;
                _ops.Clear();
                _opTimes.Clear();
                string[] kinds = { "SCOUT", "RAID", "HACK", "SABOTAGE" };
                foreach (Operation op in s.Ops)
                {
                    var row = new VisualElement();
                    row.AddToClassList("row");
                    row.AddToClassList("map-op");
                    string who = op.Kind == OpKind.Hack ? op.Compute + " COMPUTE" : op.Squad + " SENT";
                    row.Add(Kit.Label((op.ByAi ? "AI // " : string.Empty) + kinds[(int)op.Kind] + " // " + WorldSystem.Sites[op.Site].Name + " // " + who, "map-op__text", "grow"));
                    Label time = Kit.Label(string.Empty, "map-op__time");
                    row.Add(time);
                    _opTimes.Add(time);
                    if (op.ByAi)
                    {
                        // the AI acted without orders (SPEC-030): the handler can call it back
                        int id = op.Id;
                        row.EnableInClassList("map-op--ai", true);
                        row.Add(Kit.Button("RECALL", () => Run(Command.RecallOp(id)), "ds-btn--ghost", "map-op__recall"));
                    }

                    _ops.Add(row);
                }
            }

            for (int i = 0; i < _opTimes.Count && i < s.Ops.Count; i++)
            {
                _opTimes[i].text = Fmt.Countdown(_host.SecondsUntilTick(s.Ops[i].ReturnTick));
            }
        }
    }
}
