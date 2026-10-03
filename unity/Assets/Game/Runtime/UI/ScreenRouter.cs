using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>A command-bar destination. Screens mount into the HUD's screen area.</summary>
    public interface IGameScreen
    {
        string Id { get; }

        VisualElement Root { get; }

        void OnShow();

        void OnHide();
    }

    /// <summary>Command bar navigation (doc 08 s4: Base, Map, AI Terminal, Operations) with an eased cross-fade.</summary>
    public sealed class ScreenRouter
    {
        private readonly VisualElement _host;
        private readonly Dictionary<string, IGameScreen> _screens = new Dictionary<string, IGameScreen>();
        private readonly Dictionary<string, VisualElement> _tabs = new Dictionary<string, VisualElement>();

        public ScreenRouter(VisualElement host)
        {
            _host = host;
        }

        public string Current { get; private set; }

        public event System.Action<string> Changed;

        public void Register(IGameScreen screen)
        {
            _screens[screen.Id] = screen;
            screen.Root.style.position = Position.Absolute;
            screen.Root.style.left = 0;
            screen.Root.style.right = 0;
            screen.Root.style.top = 0;
            screen.Root.style.bottom = 0;
            screen.Root.style.display = DisplayStyle.None;
            _host.Add(screen.Root);
        }

        public void BindTab(string id, VisualElement tab)
        {
            _tabs[id] = tab;
            tab.RegisterCallback<ClickEvent>(_ => Show(id));
        }

        public void Show(string id)
        {
            if (id == Current || !_screens.TryGetValue(id, out IGameScreen next))
            {
                return;
            }

            if (Current != null && _screens.TryGetValue(Current, out IGameScreen prev))
            {
                prev.OnHide();
                VisualElement old = prev.Root;
                Motion.To(old, 0.16f, Ease.OutCubic, t => old.style.opacity = 1f - t, () => old.style.display = DisplayStyle.None);
            }

            Current = id;
            foreach (KeyValuePair<string, VisualElement> kv in _tabs)
            {
                kv.Value.EnableInClassList("is-active", kv.Key == id);
            }

            VisualElement root = next.Root;
            root.style.display = DisplayStyle.Flex;
            Motion.To(root, 0.24f, Ease.OutCubic, t =>
            {
                root.style.opacity = t;
                root.style.translate = new Translate(0, (1f - t) * 24f, 0);
            });
            next.OnShow();
            Changed?.Invoke(id);
        }
    }
}
