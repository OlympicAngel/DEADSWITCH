using Deadswitch.Game.Base;
using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Base
{
    /// <summary>
    /// Quick actions beside the framed facility (SPEC-039 idea 23): UPGRADE with its cost (the missing amount and
    /// the wait when short, idea 16), REPAIR when damaged, POWER, and INFO to expand the sheet. Tracks the
    /// facility on screen like the labels do. Every action is a sim command; refusals shake the tile.
    /// </summary>
    public sealed class QuickActions
    {
        /// <summary>Panel px kept between the tile row and the screen edge.</summary>
        private const float EdgeMargin = 12f;

        private readonly VisualElement _root;
        private readonly System.Action _onInfo;
        private int _slot = -1;

        public QuickActions(VisualElement layer, System.Action onInfo)
        {
            _onInfo = onInfo;
            _root = new VisualElement();
            _root.AddToClassList("qa");
            _root.AddToClassList("is-hidden");
            _root.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            layer.Add(_root);
        }

        /// <summary>The bar's surface (for the popover coordinator).</summary>
        public VisualElement Root => _root;

        public void Show(int slot)
        {
            _slot = slot;
            Refresh();
            if (slot >= 0)
            {
                Motion.To(_root, 0.28f, Ease.OutBack, t =>
                {
                    _root.style.opacity = t;
                    _root.style.scale = new Scale(new Vector3(0.8f + (0.2f * t), 0.8f + (0.2f * t), 1f));
                });
            }
        }

        public void Refresh()
        {
            _root.Clear();
            GameHost host = GameHost.Instance;
            GameState s = host.Sim.State;
            if (_slot < 0 || _slot >= s.Slots.Count || s.Slots[_slot].IsEmpty || s.JobForSlot(_slot) != null)
            {
                _root.AddToClassList("is-hidden");
                return;
            }

            _root.RemoveFromClassList("is-hidden");
            SimConfig c = host.Config;
            FacilitySlot slot = s.Slots[_slot];
            int index = _slot;
            if (slot.Damage > 0 && !ScarSystem.Repairing(s, slot))
            {
                int cost = ScarSystem.RepairCost(c, slot);
                VisualElement fix = Kit.Tile("wrench", "REPAIR", Need(s.Energy, cost, "ENERGY"), () => Run(Command.Repair(index)), "ds-tile--warn");
                Short(fix, s.Energy < cost || s.RaidId != 0);
                _root.Add(fix);
            }

            if (slot.Level < c.Facility(slot.Kind).MaxLevel)
            {
                Economy.UpgradeCost(c, slot.Kind, slot.Level, out int energy, out int compute);
                string when = Afford.When(s, c, energy, compute, 3600.0 / host.Settings.DevTimeScale);
                string cost = when.Length > 0 ? when : "ENERGY " + Fmt.Compact(energy) + (compute > 0 ? "  COMPUTE " + Fmt.Compact(compute) : string.Empty);
                VisualElement up = Kit.Tile("up", "UPGRADE", cost, () => Run(Command.Upgrade(index)), "ds-tile--primary");
                Short(up, s.Energy < energy || s.Compute < compute);
                _root.Add(up);
            }

            _root.Add(Kit.Tile("power", slot.Enabled ? "POWER OFF" : "POWER ON", null, () => Run(Command.SetFacilityPower(index, !slot.Enabled))));
            _root.Add(Kit.Tile("info", "DETAILS", null, _onInfo));
        }

        /// <summary>Keeps the tiles under the facility (call each frame after the camera moved).</summary>
        public void Track(Camera cam, BaseView view, float panelWidth, float panelHeight)
        {
            if (_slot < 0 || cam == null || view == null || _slot >= view.SlotCount || panelWidth <= 0f)
            {
                return;
            }

            Vector3 sp = cam.WorldToScreenPoint(view.SlotGround(_slot));
            // centred under the facility (translate -50%) but kept whole on screen
            float x = sp.x / Screen.width * panelWidth;
            float half = float.IsNaN(_root.resolvedStyle.width) ? 0f : _root.resolvedStyle.width * 0.5f;
            if (half > 0f && half < (panelWidth * 0.5f) - EdgeMargin)
            {
                x = Mathf.Clamp(x, half + EdgeMargin, panelWidth - half - EdgeMargin);
            }

            _root.style.left = x;
            _root.style.top = (Screen.height - sp.y) / Screen.height * panelHeight;
        }

        /// <summary>"E 400" when affordable, "NEED 140 E" when short.</summary>
        private static string Need(int have, int cost, string unit)
        {
            return have >= cost ? unit + " " + Fmt.Compact(cost) : "NEED " + Fmt.Compact(cost - have) + " " + unit;
        }

        private static void Short(VisualElement tile, bool shortOf)
        {
            tile.EnableInClassList("is-disabled", shortOf);
            tile.Q<Label>(className: "ds-tile__cost")?.EnableInClassList("is-short", shortOf);
        }

        private void Run(Command command)
        {
            GameHost host = GameHost.Instance;
            CommandResult r = host.Execute(command);
            Hud.HudController.Instance?.Advisor.Say(r.Accepted ? Texts.Ack(command) : Texts.Reason(r.Reason));
            Toasts.Show(r.Accepted ? "check" : "alert", r.Accepted ? "ORDER ACCEPTED" : "REFUSED", r.Accepted ? Toasts.Tone.Good : Toasts.Tone.Bad);
            Refresh();
        }
    }
}
