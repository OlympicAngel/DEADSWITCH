using System.Collections.Generic;
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
    /// MODULES view inside CORE (SPEC-008): tier gate checklist and tier-up, research progress, the tree (trunk + one
    /// field tab at a time with its exclusive pairs) and the selected node's detail. Layout: Resources/UI/Modules.uxml.
    /// </summary>
    public sealed class ModulesView
    {
        private readonly GameHost _host;
        private readonly VisualElement _ui;
        private readonly VisualElement _body;
        private readonly Dictionary<ModuleNode, VisualElement> _nodes = new Dictionary<ModuleNode, VisualElement>();
        private ModuleNode _selected = ModuleNode.M1;
        private ModuleField _field = ModuleField.Logistics;
        private readonly System.Action _openPremium;

        public ModulesView(VisualElement mount, System.Action openPremium)
        {
            _host = GameHost.Instance;
            _openPremium = openPremium;
            TemplateContainer tree = UiRoot.Load("Modules");
            mount.Add(tree);
            _ui = tree;
            _body = _ui.Q("mod-field-body");
            foreach (ModuleNode node in new[] { ModuleNode.M1, ModuleNode.M2, ModuleNode.M3 })
            {
                Hook(_ui.Q("node-" + node), node);
                _nodes[node] = _ui.Q("node-" + node);
            }

            for (int f = (int)ModuleField.Logistics; f <= (int)ModuleField.Stealth; f++)
            {
                var field = (ModuleField)f;
                _ui.Q("tab-" + f).RegisterCallback<ClickEvent>(_ => ShowField(field));
            }

            BuildField();
            _ui.Q("detail-start").RegisterCallback<ClickEvent>(_ => StartOrCancel());
            _ui.Q("tier-up").RegisterCallback<ClickEvent>(_ =>
            {
                // free demo (ADR-0006): Tier 1 is the demo; going further needs the one-time unlock
                if (!Store.Entitlements.Instance.HasPremium)
                {
                    _openPremium();
                    return;
                }

                Run(Command.TierUp());
            });
        }

        public void Refresh()
        {
            GameState s = _host.Sim.State;
            SimConfig c = _host.Sim.Config;

            // tier gates
            TierGates g = Modules.Gates(s, c);
            _ui.Q<Label>("tier-title").text = "TIER " + s.Tier + " // " + ModuleTexts.TierName(s.Tier);
            bool final = s.Tier >= c.Tier.MaxTier;
            _ui.Q<Label>("tier-next").text = final ? "FINAL TIER IN THIS BUILD" : "NEXT: " + ModuleTexts.TierName(s.Tier + 1);
            foreach (string gate in new[] { "gate-build", "gate-module", "gate-people" })
            {
                _ui.Q(gate).parent.style.display = final ? DisplayStyle.None : DisplayStyle.Flex;
            }

            _ui.Q("tier-up").style.display = final ? DisplayStyle.None : DisplayStyle.Flex;
            Gate("gate-build", g.Build, "FACILITY LEVELS " + g.Levels + " / " + g.LevelsNeeded + "  //  NET POWER " + Fmt.Signed(g.NetEnergy) + " / " + Fmt.Signed(g.NetEnergyNeeded) + " H");
            Gate("gate-module", g.ModuleRestored, (g.Module == ModuleNode.None ? "NO FURTHER MEMORY" : ModuleTexts.Name(g.Module)) + (g.ModuleRestored ? " RESTORED" : " NOT RESTORED"));
            Gate("gate-people", g.PeopleAvailable, "HUMAN COST: " + g.PeopleCost + " PEOPLE LEAVE TO EXPAND");
            VisualElement up = _ui.Q("tier-up");
            up.EnableInClassList("is-disabled", !g.All);
            up.EnableInClassList("ds-btn--primary", g.All);
            bool demo = !Store.Entitlements.Instance.HasPremium;
            Kit.SetButtonText(up, demo && g.All ? "UNLOCK FULL GAME TO ADVANCE" : "ADVANCE TO TIER " + (s.Tier + 1));

            // research in progress
            // two lanes (memory sectors beside field research): the bar follows the field lane, else the memory lane
            bool busy = s.ResearchNode != 0 || s.MemoryNode != 0;
            _ui.Q("research").EnableInClassList("is-hidden", !busy);
            if (busy)
            {
                var node = (ModuleNode)(s.ResearchNode != 0 ? s.ResearchNode : s.MemoryNode);
                string memory = s.ResearchNode != 0 && s.MemoryNode != 0 ? "  +  " + (ModuleNode)s.MemoryNode : string.Empty;
                _ui.Q<Label>("research-name").text = node + " " + ModuleTexts.Name(node) + memory;
            }

            Tick();

            // tree
            foreach (ModuleDef d in Modules.Catalog)
            {
                if (!_nodes.TryGetValue(d.Node, out VisualElement el))
                {
                    continue;
                }

                RejectReason why = Modules.Availability(s, d.Node);
                bool restored = Modules.IsRestored(s, d.Node);
                bool active = Modules.Restoring(s, d.Node);
                el.EnableInClassList("is-restored", restored);
                el.EnableInClassList("is-active", active);
                el.EnableInClassList("is-available", !restored && !active && why == RejectReason.None);
                el.EnableInClassList("is-locked", why == RejectReason.Locked);
                el.EnableInClassList("is-excluded", why == RejectReason.Excluded);
                el.EnableInClassList("is-selected", d.Node == _selected);
                el.Q<Label>("node-" + d.Node + "-state").text = restored ? (Modules.Has(s, d.Node) ? "RESTORED" : "LOCKED") : active ? "RESTORING" : why == RejectReason.Excluded ? "EXCLUDED"
                    : why == RejectReason.Locked ? (s.Tier < d.Tier ? "TIER " + d.Tier : "NEEDS " + d.Prereq) : "AVAILABLE";
            }

            for (int f = (int)ModuleField.Logistics; f <= (int)ModuleField.Stealth; f++)
            {
                _ui.Q("tab-" + f).EnableInClassList("is-selected", f == (int)_field);
            }

            // detail
            Modules.TryDef(_selected, out ModuleDef sel);
            RejectReason state = Modules.Availability(s, _selected);
            bool selActive = Modules.Restoring(s, _selected);
            _ui.Q<Label>("detail-title").text = _selected + " // " + ModuleTexts.Name(_selected);
            _ui.Q<Label>("detail-state").text = Modules.IsRestored(s, _selected) ? (Modules.Has(s, _selected) ? "RESTORED" : "LOCKED // INTRUSION") : selActive ? "RESTORING" : state == RejectReason.None ? (Modules.NeedsFragment(_selected) ? "AVAILABLE // USES 1 OF " + s.DataFragments + " DATA FRAGMENTS" : "AVAILABLE")
                : state == RejectReason.NeedsFragment ? "NEEDS A DATA FRAGMENT // RAID A DEAD DATA CENTER" : state.ToString().ToUpperInvariant();
            _ui.Q<Label>("detail-desc").text = ModuleTexts.Effect(_selected, c);
            int energy = c.Modules.ResearchEnergy[sel.Index];
            int compute = c.Modules.ResearchCompute[sel.Index];
            bool blueprint = sel.Field != ModuleField.Trunk && s.Blueprints > 0;
            if (blueprint)
            {
                // a traded blueprint is spent on the next field research (doc 10 s5)
                energy = SimMath.PctFloor(energy, 100 - c.Living.BlueprintDiscountPct);
                compute = SimMath.PctFloor(compute, 100 - c.Living.BlueprintDiscountPct);
            }

            var e = _ui.Q<Label>("detail-energy");
            e.text = Fmt.Num(energy) + " ENERGY" + (blueprint ? " // BLUEPRINT -" + c.Living.BlueprintDiscountPct + "%" : string.Empty);
            e.EnableInClassList("is-short", s.Energy < energy);
            var cp = _ui.Q<Label>("detail-compute");
            cp.text = Fmt.Num(compute) + " COMPUTE";
            cp.EnableInClassList("is-short", s.Compute < compute);
            _ui.Q<Label>("detail-time").text = Fmt.Countdown(c.Modules.ResearchMinutes[sel.Index] * 60.0 / _host.Settings.DevTimeScale);
            VisualElement start = _ui.Q("detail-start");
            bool laneBusy = sel.Field == ModuleField.Trunk ? s.MemoryNode != 0 : s.ResearchNode != 0;
            bool canStart = state == RejectReason.None && !laneBusy;
            start.style.display = Modules.IsRestored(s, _selected) ? DisplayStyle.None : DisplayStyle.Flex;
            start.EnableInClassList("ds-btn--primary", canStart);
            start.EnableInClassList("ds-btn--ghost", selActive);
            start.EnableInClassList("is-disabled", !canStart && !selActive);
            Kit.SetButtonText(start, selActive ? "CANCEL RESTORATION" : "RESTORE");
        }

        /// <summary>Live research bar and time left.</summary>
        public void Tick()
        {
            GameState s = _host.Sim.State;
            if (s.ResearchNode == 0 && s.MemoryNode == 0)
            {
                return;
            }

            bool field = s.ResearchNode != 0;
            long start = field ? s.ResearchStartTick : s.MemoryStartTick;
            long complete = field ? s.ResearchCompleteTick : s.MemoryCompleteTick;
            float total = complete - start;
            float done = (s.Tick - start) + _host.TickProgress;
            _ui.Q("research-fill").style.width = Length.Percent(UnityEngine.Mathf.Clamp01(done / UnityEngine.Mathf.Max(1f, total)) * 100f);
            _ui.Q<Label>("research-eta").text = Fmt.Countdown(_host.SecondsUntilTick(complete));
        }

        private void ShowField(ModuleField field)
        {
            _field = field;
            BuildField();
            foreach (ModuleDef d in Modules.Catalog)
            {
                if (d.Field == field)
                {
                    _selected = d.Node;
                    break;
                }
            }

            _ui.Q<Label>("mod-reason").text = string.Empty;
            Refresh();
        }

        /// <summary>
        /// Rebuilds the selected field's nodes from the catalog. Every field has the same shape (SPEC-008):
        /// Tier 1 = node 1 then an exclusive pair; Tier 2 = nodes 3 and 4, an exclusive pair, then node 6.
        /// </summary>
        private void BuildField()
        {
            foreach (ModuleDef d in Modules.Catalog)
            {
                if (d.Field != ModuleField.Trunk)
                {
                    _nodes.Remove(d.Node);
                }
            }

            _body.Clear();
            var defs = new List<ModuleDef>();
            foreach (ModuleDef d in Modules.Catalog)
            {
                if (d.Field == _field)
                {
                    defs.Add(d);
                }
            }

            int i = 0;
            int tier = 0;
            while (i < defs.Count)
            {
                ModuleDef d = defs[i];
                if (d.Tier != tier)
                {
                    tier = d.Tier;
                    _body.Add(Kit.Label("TIER " + tier, "mod-tierlabel"));
                }

                var row = new VisualElement();
                row.AddToClassList("row");
                row.AddToClassList("mod-row");
                row.Add(Node(d));
                i++;
                if (d.Pair != ModuleNode.None && i < defs.Count && defs[i].Node == d.Pair)
                {
                    row.AddToClassList("mod-pair");
                    row.Add(Kit.Label("OR", "mod-or"));
                    row.Add(Node(defs[i]));
                    i++;
                }
                else if (d.Pair == ModuleNode.None && i < defs.Count && defs[i].Pair == ModuleNode.None && defs[i].Tier == d.Tier && d.Tier > 1 && i + 1 < defs.Count)
                {
                    row.Add(Node(defs[i]));
                    i++;
                }

                Kit.MarkEnds(row);
                _body.Add(row);
            }
        }

        private VisualElement Node(ModuleDef d)
        {
            var el = new VisualElement { name = "node-" + d.Node };
            el.AddToClassList("mod-node");
            el.Add(Kit.Label(d.Node.ToString(), "mod-node__code"));
            el.Add(Kit.Label(ModuleTexts.Name(d.Node), "mod-node__name"));
            Label state = Kit.Label(string.Empty, "mod-node__state");
            state.name = "node-" + d.Node + "-state";
            el.Add(state);
            Hook(el, d.Node);
            _nodes[d.Node] = el;
            return el;
        }

        private void Hook(VisualElement el, ModuleNode node)
        {
            el.RegisterCallback<ClickEvent>(_ =>
            {
                _selected = node;
                _ui.Q<Label>("mod-reason").text = string.Empty;
                Refresh();
            });
        }

        private void Gate(string name, bool ok, string text)
        {
            _ui.Q(name + "-pip").EnableInClassList("is-ok", ok);
            _ui.Q<Label>(name).text = text;
        }

        private void StartOrCancel()
        {
            GameState s = _host.Sim.State;
            Run(s.ResearchNode == (int)_selected ? Command.CancelResearch() : s.MemoryNode == (int)_selected ? Command.CancelMemory() : Command.StartResearch(_selected));
        }

        private void Run(Command command)
        {
            CommandResult r = _host.Execute(command);
            _ui.Q<Label>("mod-reason").text = r.Accepted ? string.Empty : Texts.Reason(r.Reason);
            Refresh();
        }
    }
}
