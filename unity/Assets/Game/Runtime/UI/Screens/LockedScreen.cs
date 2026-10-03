using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// A destination whose systems are still offline in this run (for example long-range sensors before the
    /// module that restores them). Tells the handler, in the AI's voice, what brings it online.
    /// </summary>
    public sealed class LockedScreen : IGameScreen
    {
        public LockedScreen(string id, string title, string line, string requirement)
        {
            Id = id;
            Root = new VisualElement { pickingMode = PickingMode.Ignore };
            Root.AddToClassList("screen-locked");

            VisualElement panel = Kit.Panel("screen-locked__panel");
            panel.Add(Kit.Label(title, "ds-title"));
            panel.Add(Kit.Label("OFFLINE", "ds-label", "t-amber", "screen-locked__state"));
            panel.Add(Kit.Label(line, "ds-body", "t-phosphor", "screen-locked__line"));
            var req = new VisualElement();
            req.AddToClassList("ds-chip");
            req.AddToClassList("screen-locked__req");
            var pip = new VisualElement();
            pip.AddToClassList("ds-pip");
            pip.AddToClassList("ds-pip--off");
            req.Add(pip);
            req.Add(Kit.Label(requirement, "ds-chip__label"));
            panel.Add(req);
            Root.Add(panel);
        }

        public string Id { get; }

        public VisualElement Root { get; }

        public void OnShow()
        {
        }

        public void OnHide()
        {
        }
    }
}
