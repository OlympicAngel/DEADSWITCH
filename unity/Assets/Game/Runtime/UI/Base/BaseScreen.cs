using Deadswitch.Game.Base;
using Deadswitch.Game.Core;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Base
{
    /// <summary>
    /// The BASE destination: the 3D compound itself. Taps on plots or their labels select a slot and open its
    /// sheet; the bunker door opens the CORE screen; empty ground closes the sheet.
    /// </summary>
    public sealed class BaseScreen : IGameScreen
    {
        private readonly BaseLabels _labels;
        private readonly SlotSheet _sheet;
        private readonly QuickActions _quick;
        private readonly ScreenRouter _router;
        private bool _visible;

        public BaseScreen(ScreenRouter router)
        {
            _router = router;
            Root = new VisualElement { pickingMode = PickingMode.Ignore };
            UiRoot ui = UiRoot.Instance;
            _labels = new BaseLabels(ui.World, Select, () => _router.Show("core"));
            _sheet = new SlotSheet(ui.Sheets, () => Select(-1));
            _quick = new QuickActions(ui.World, () => _sheet.Expand());
            // the slot sheet and its quick-action bar open and close together
            Popovers.Register("slot", () => _sheet.IsOpen, () => Select(-1), _sheet.Root, _quick.Root);

            DroneCamera cam = DroneCamera.Instance;
            cam.SlotTapped += slot =>
            {
                if (_visible)
                {
                    Select(slot);
                }
            };
            cam.CoreTapped += () =>
            {
                if (_visible)
                {
                    _router.Show("core");
                }
            };
            cam.NothingTapped += () => Select(-1);
            cam.Hopped += dir =>
            {
                if (_visible && _sheet.IsOpen)
                {
                    Select(Neighbour(_sheet.Slot, dir));
                }
            };

            GameHost.Instance.Ticked += () =>
            {
                _labels.Refresh();
                _sheet.Refresh();
                _quick.Refresh();
            };
            ui.Frame += _ =>
            {
                if (_visible)
                {
                    _labels.Track(cam.Camera, BaseView.Instance, ui.Root.layout.width, ui.Root.layout.height);
                    _quick.Track(cam.Camera, BaseView.Instance, ui.Root.layout.width, ui.Root.layout.height);
                    _sheet.Tick();
                }
            };
            _labels.Refresh();
        }

        public string Id => "base";

        /// <summary>Points the opening guide at a plot (-1 clears).</summary>
        public void Guide(int slot)
        {
            _labels.Guide(slot);
        }

        public VisualElement Root { get; }

        /// <summary>Back: closes the facility sheet and flies out; false when nothing was selected.</summary>
        public bool CloseFocus()
        {
            if (!_sheet.IsOpen)
            {
                return false;
            }

            Select(-1);
            return true;
        }

        /// <summary>Selects a slot and frames it (resource shortcuts, suggestion chips); recommends a facility on an empty plot.</summary>
        public void Focus(int slot, Deadswitch.Sim.State.FacilityKind recommend)
        {
            Select(slot);
            _sheet.Recommend(recommend);
        }

        public void OnShow()
        {
            _visible = true;
            UiRoot.Instance.World.style.display = DisplayStyle.Flex;
        }

        public void OnHide()
        {
            _visible = false;
            Select(-1);
            UiRoot.Instance.World.style.display = DisplayStyle.None;
        }

        /// <summary>The built facility nearest on screen in a direction (-1 left, +1 right), or the same slot.</summary>
        private static int Neighbour(int from, int dir)
        {
            BaseView view = BaseView.Instance;
            UnityEngine.Camera cam = DroneCamera.Instance.Camera;
            var slots = GameHost.Instance.Sim.State.Slots;
            float x0 = cam.WorldToScreenPoint(view.SlotGround(from)).x;
            int best = from;
            float bestDx = float.MaxValue;
            for (int i = 0; i < view.SlotCount && i < slots.Count; i++)
            {
                if (i == from || slots[i].IsEmpty)
                {
                    continue;
                }

                float dx = (cam.WorldToScreenPoint(view.SlotGround(i)).x - x0) * dir;
                if (dx > 1f && dx < bestDx)
                {
                    bestDx = dx;
                    best = i;
                }
            }

            return best;
        }

        private void Select(int slot)
        {
            _labels.Select(slot);
            BaseView.Instance?.Select(slot);
            BaseFx.Instance?.Focus(slot);
            _quick.Show(slot);
            if (slot < 0)
            {
                _sheet.Close();
                DroneCamera.Instance.ClearFocus();
                return;
            }

            _sheet.Open(slot);
            DroneCamera.Instance.FocusOn(BaseView.Instance.FocusPoint(slot));
        }
    }
}
