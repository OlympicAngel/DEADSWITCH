using System.Collections.Generic;
using UnityEngine;
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

    /// <summary>
    /// Command bar navigation (doc 08 s4: Base, Map, AI Terminal, Operations) with an eased cross-fade, and swipes
    /// between those layers (doc 08 s4 gestures): a quick horizontal flick moves one layer. On the Base layer only a
    /// flick that starts at a screen edge counts, so it never fights the drone camera's pan.
    /// </summary>
    public sealed class ScreenRouter
    {
        private readonly VisualElement _host;
        private readonly Dictionary<string, IGameScreen> _screens = new Dictionary<string, IGameScreen>();
        private readonly Dictionary<string, VisualElement> _tabs = new Dictionary<string, VisualElement>();
        private readonly List<string> _layers = new List<string>();
        private Vector2 _swipeFrom;
        private float _swipeAt = -1f;
        private int _swipePointer = -1;

        public ScreenRouter(VisualElement host)
        {
            _host = host;
            _host.RegisterCallback<PointerDownEvent>(SwipeStart, TrickleDown.TrickleDown);
            _host.RegisterCallback<PointerUpEvent>(SwipeEnd, TrickleDown.TrickleDown);
        }

        public string Current { get; private set; }

        public event System.Action<string> Changed;

        /// <summary>Every registered screen id, sorted (the smoke run walks them).</summary>
        public List<string> Ids
        {
            get
            {
                var ids = new List<string>(_screens.Keys);
                ids.Sort(System.StringComparer.Ordinal);
                return ids;
            }
        }

        /// <summary>The screen registered under this id, or null.</summary>
        public IGameScreen Get(string id)
        {
            return _screens.TryGetValue(id, out IGameScreen screen) ? screen : null;
        }

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
            if (!_layers.Contains(id))
            {
                _layers.Add(id);
            }

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

        private void SwipeStart(PointerDownEvent e)
        {
            float width = _host.layout.width;
            bool edge = e.position.x < width * 0.08f || e.position.x > width * 0.92f;
            if (!_layers.Contains(Current ?? string.Empty) || (Current == "base" && !edge))
            {
                _swipeAt = -1f;
                return;
            }

            _swipeFrom = e.position;
            _swipeAt = Time.realtimeSinceStartup;
            _swipePointer = e.pointerId;
        }

        private void SwipeEnd(PointerUpEvent e)
        {
            if (_swipeAt < 0f || e.pointerId != _swipePointer)
            {
                return;
            }

            Vector2 d = (Vector2)e.position - _swipeFrom;
            float quick = Time.realtimeSinceStartup - _swipeAt;
            _swipeAt = -1f;
            if (quick > 0.45f || Mathf.Abs(d.x) < _host.layout.width * 0.22f || Mathf.Abs(d.y) > Mathf.Abs(d.x) * 0.5f)
            {
                return;
            }

            int i = _layers.IndexOf(Current);
            int next = i + (d.x < 0 ? 1 : -1);
            if (next >= 0 && next < _layers.Count)
            {
                Show(_layers[next]);
            }
        }
    }
}
