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
        private readonly ScreenRouter _router;
        private bool _visible;

        public BaseScreen(ScreenRouter router)
        {
            _router = router;
            Root = new VisualElement { pickingMode = PickingMode.Ignore };
            UiRoot ui = UiRoot.Instance;
            _labels = new BaseLabels(ui.World, Select, () => _router.Show("core"));
            _sheet = new SlotSheet(ui.Sheets, () => Select(-1));

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

            GameHost.Instance.Ticked += () =>
            {
                _labels.Refresh();
                _sheet.Refresh();
            };
            ui.Frame += _ =>
            {
                if (_visible)
                {
                    _labels.Track(cam.Camera, BaseView.Instance, ui.Root.layout.width, ui.Root.layout.height);
                    _sheet.Tick();
                }
            };
            _labels.Refresh();
        }

        public string Id => "base";

        public VisualElement Root { get; }

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

        private void Select(int slot)
        {
            _labels.Select(slot);
            BaseView.Instance?.Select(slot);
            if (slot < 0)
            {
                _sheet.Close();
                return;
            }

            _sheet.Open(slot);
            DroneCamera.Instance.Focus(BaseView.Instance.LabelAnchor(slot));
        }
    }
}
