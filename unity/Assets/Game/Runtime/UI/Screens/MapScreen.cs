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
        private static readonly string[] FactionClass = { "map-site--rust", "map-site--vanguard", "map-site--church" };

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
            _ui.Q("op-minus").RegisterCallback<ClickEvent>(_ => Step(-1));
            _ui.Q("op-plus").RegisterCallback<ClickEvent>(_ => Step(1));
            _ui.Q("op-launch").RegisterCallback<ClickEvent>(_ => Run(Command.LaunchOp(_selected, _kind, _kind == OpKind.Hack ? _compute : _squad)));
            _ui.Q("site-claim").RegisterCallback<ClickEvent>(_ => Run(Command.ClaimOutpost(_selected)));

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
                _squad = SimMath.Clamp(_squad + d, 1, 20);
            }

            Refresh();
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
                case RejectReason.NotClaimable: return "Only ruins we have cleared can hold an outpost.";
                case RejectReason.NotEnoughFuel: return "Not enough fuel for the trip.";
                default: return Texts.Reason(reason);
            }
        }

        private void Refresh()
        {
            GameState s = _host.Sim.State;
            SimConfig c = _host.Sim.Config;

            _ui.Q<Label>("map-ops-count").text = "OPS " + s.Ops.Count + "/" + c.World.MaxOps + " // FUEL " + Fmt.Num(s.Fuel);
            for (int f = 0; f < WorldSystem.FactionCount; f++)
            {
                HeatLevel level = WorldSystem.Level(s.Heat[f]);
                _ui.Q<Label>("heat-" + f + "-level").text = level.ToString().ToUpperInvariant() + " " + WorldSystem.Percent(s.Heat[f]);
                Kit.SetMeter(_ui.Q("heat-" + f + "-meter"), s.Heat[f] / 100_000f);
                _ui.Q("heat-" + f).EnableInClassList("is-hot", level >= HeatLevel.Hunted);
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

            RefreshSheet(s, c);
            RefreshOps();
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
            _ui.Q<Label>("site-def-label").text = st.Scouted ? "DEFENSE (SCOUTED)" : "DEFENSE (AI EST)";
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

            bool hack = _kind == OpKind.Hack;
            _ui.Q<Label>("op-amount").text = (hack ? _compute : _squad).ToString(System.Globalization.CultureInfo.InvariantCulture);
            _ui.Q<Label>("op-amount-unit").text = hack ? "COMPUTE" : "PEOPLE";
            int odds = WorldSystem.Odds(c, d, _kind, _squad, _compute, estimate);
            Label oddsLabel = _ui.Q<Label>("op-odds");
            oddsLabel.text = (st.Scouted ? "ODDS " : "AI ODDS ") + odds + "%";
            oddsLabel.EnableInClassList("t-amber", odds < 60);
            int back = hack ? 1 : 2 * d.TravelHours;
            _ui.Q<Label>("op-cost").text = (hack ? "COMPUTE " + _compute : "FUEL " + WorldSystem.FuelCost(c, d, _kind)) + " // BACK IN " + back + " H";
            string[] verbs = { "SEND SCOUTS", "LAUNCH RAID", "START HACK" };
            Kit.SetButtonText(_ui.Q("op-launch"), verbs[(int)_kind]);
            bool cooling = _kind != OpKind.Scout && s.Tick < st.CooldownUntilTick;
            _ui.Q("op-launch").EnableInClassList("is-disabled", st.Outpost || cooling || s.Ops.Count >= c.World.MaxOps);

            bool claimable = d.Kind == SiteKind.Ruins && st.Cleared && !st.Outpost;
            _ui.Q("site-claim").EnableInClassList("is-hidden", !claimable);
            Kit.SetButtonText(_ui.Q("site-claim"), "CLAIM // " + c.World.OutpostClaimEnergy + " E");
        }

        private void RefreshOps()
        {
            GameState s = _host.Sim.State;
            _ops.Clear();
            string[] kinds = { "SCOUT", "RAID", "HACK" };
            foreach (Operation op in s.Ops)
            {
                var row = new VisualElement();
                row.AddToClassList("row");
                row.AddToClassList("map-op");
                string who = op.Kind == OpKind.Hack ? op.Compute + " COMPUTE" : op.Squad + " SENT";
                row.Add(Kit.Label(kinds[(int)op.Kind] + " // " + WorldSystem.Sites[op.Site].Name + " // " + who, "map-op__text", "grow"));
                row.Add(Kit.Label(Fmt.Countdown(_host.SecondsUntilTick(op.ReturnTick)), "map-op__time"));
                _ops.Add(row);
            }
        }
    }
}
