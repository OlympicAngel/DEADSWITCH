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
    /// MODULES view inside CORE (SPEC-008): tier gate checklist and tier-up, research progress, the tree (trunk +
    /// Logistics with its exclusive pairs) and the selected node's detail. Layout: Resources/UI/Modules.uxml.
    /// </summary>
    public sealed class ModulesView
    {
        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private ModuleNode _selected = ModuleNode.M1;

        public ModulesView(VisualElement mount)
        {
            _host = GameHost.Instance;
            TemplateContainer tree = UiRoot.Load("Modules");
            mount.Add(tree);
            _ui = tree;
            foreach (ModuleDef d in Modules.Catalog)
            {
                ModuleNode node = d.Node;
                _ui.Q("node-" + node).RegisterCallback<ClickEvent>(_ =>
                {
                    _selected = node;
                    _ui.Q<Label>("mod-reason").text = string.Empty;
                    Refresh();
                });
            }

            _ui.Q("detail-start").RegisterCallback<ClickEvent>(_ => StartOrCancel());
            _ui.Q("tier-up").RegisterCallback<ClickEvent>(_ => Run(Command.TierUp()));
        }

        public void Refresh()
        {
            GameState s = _host.Sim.State;
            SimConfig c = _host.Sim.Config;

            // tier gates
            TierGates g = Modules.Gates(s, c);
            _ui.Q<Label>("tier-title").text = "TIER " + s.Tier + " // " + ModuleTexts.TierName(s.Tier);
            Gate("gate-build", g.Build, "FACILITY LEVELS " + g.Levels + " / " + g.LevelsNeeded + "  //  NET POWER " + Fmt.Signed(g.NetEnergy) + " / " + Fmt.Signed(g.NetEnergyNeeded) + " H");
            Gate("gate-module", g.ModuleRestored, (g.Module == ModuleNode.None ? "NO FURTHER MEMORY" : ModuleTexts.Name(g.Module)) + (g.ModuleRestored ? " RESTORED" : " NOT RESTORED"));
            Gate("gate-people", g.PeopleAvailable, "HUMAN COST: " + g.PeopleCost + " PEOPLE LEAVE TO EXPAND");
            VisualElement up = _ui.Q("tier-up");
            up.EnableInClassList("is-disabled", !g.All);
            up.EnableInClassList("ds-btn--primary", g.All);
            Kit.SetButtonText(up, "ADVANCE TO TIER " + (s.Tier + 1));

            // research in progress
            bool busy = s.ResearchNode != 0;
            _ui.Q("research").EnableInClassList("is-hidden", !busy);
            if (busy)
            {
                var node = (ModuleNode)s.ResearchNode;
                _ui.Q<Label>("research-name").text = node + " " + ModuleTexts.Name(node);
            }

            Tick();

            // tree
            foreach (ModuleDef d in Modules.Catalog)
            {
                VisualElement el = _ui.Q("node-" + d.Node);
                RejectReason why = Modules.Availability(s, d.Node);
                bool restored = Modules.Has(s, d.Node);
                bool active = s.ResearchNode == (int)d.Node;
                el.EnableInClassList("is-restored", restored);
                el.EnableInClassList("is-active", active);
                el.EnableInClassList("is-available", !restored && !active && why == RejectReason.None);
                el.EnableInClassList("is-locked", why == RejectReason.Locked);
                el.EnableInClassList("is-excluded", why == RejectReason.Excluded);
                el.EnableInClassList("is-selected", d.Node == _selected);
                _ui.Q<Label>("node-" + d.Node + "-state").text = restored ? "RESTORED" : active ? "RESTORING" : why == RejectReason.Excluded ? "EXCLUDED"
                    : why == RejectReason.Locked ? (s.Tier < d.Tier ? "TIER " + d.Tier : "NEEDS " + d.Prereq) : "AVAILABLE";
            }

            // detail
            Modules.TryDef(_selected, out ModuleDef sel);
            RejectReason state = Modules.Availability(s, _selected);
            bool selActive = s.ResearchNode == (int)_selected;
            _ui.Q<Label>("detail-title").text = _selected + " // " + ModuleTexts.Name(_selected);
            _ui.Q<Label>("detail-state").text = Modules.Has(s, _selected) ? "RESTORED" : selActive ? "RESTORING" : state == RejectReason.None ? "AVAILABLE" : state.ToString().ToUpperInvariant();
            _ui.Q<Label>("detail-desc").text = ModuleTexts.Effect(_selected, c);
            int energy = c.Modules.ResearchEnergy[sel.Index];
            int compute = c.Modules.ResearchCompute[sel.Index];
            var e = _ui.Q<Label>("detail-energy");
            e.text = Fmt.Num(energy) + " ENERGY";
            e.EnableInClassList("is-short", s.Energy < energy);
            var cp = _ui.Q<Label>("detail-compute");
            cp.text = Fmt.Num(compute) + " COMPUTE";
            cp.EnableInClassList("is-short", s.Compute < compute);
            _ui.Q<Label>("detail-time").text = Fmt.Countdown(c.Modules.ResearchMinutes[sel.Index] * 60.0 / _host.Settings.DevTimeScale);
            VisualElement start = _ui.Q("detail-start");
            bool canStart = state == RejectReason.None && !busy;
            start.style.display = Modules.Has(s, _selected) ? DisplayStyle.None : DisplayStyle.Flex;
            start.EnableInClassList("ds-btn--primary", canStart);
            start.EnableInClassList("ds-btn--ghost", selActive);
            start.EnableInClassList("is-disabled", !canStart && !selActive);
            Kit.SetButtonText(start, selActive ? "CANCEL RESTORATION" : "RESTORE");
        }

        /// <summary>Live research bar and time left.</summary>
        public void Tick()
        {
            GameState s = _host.Sim.State;
            if (s.ResearchNode == 0)
            {
                return;
            }

            float total = s.ResearchCompleteTick - s.ResearchStartTick;
            float done = (s.Tick - s.ResearchStartTick) + _host.TickProgress;
            _ui.Q("research-fill").style.width = Length.Percent(UnityEngine.Mathf.Clamp01(done / UnityEngine.Mathf.Max(1f, total)) * 100f);
            _ui.Q<Label>("research-eta").text = Fmt.Countdown(_host.SecondsUntilTick(s.ResearchCompleteTick));
        }

        private void Gate(string name, bool ok, string text)
        {
            _ui.Q(name + "-pip").EnableInClassList("is-ok", ok);
            _ui.Q<Label>(name).text = text;
        }

        private void StartOrCancel()
        {
            Run(_host.Sim.State.ResearchNode == (int)_selected ? Command.CancelResearch() : Command.StartResearch(_selected));
        }

        private void Run(Command command)
        {
            CommandResult r = _host.Execute(command);
            _ui.Q<Label>("mod-reason").text = r.Accepted ? string.Empty : Texts.Reason(r.Reason);
            Refresh();
        }
    }
}
