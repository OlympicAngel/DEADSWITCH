using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Short stacked confirmations under the resource bar (SPEC-039 idea 56): an icon, a few words, a tone.
    /// They never take input and fade on their own; at most <see cref="InterfaceConfig.ToastRules.max"/> show.
    /// </summary>
    public static class Toasts
    {
        private static readonly List<Entry> Live = new List<Entry>();
        private static VisualElement _stack;

        public enum Tone
        {
            Info,
            Good,
            Warn,
            Bad,
        }

        public static void Mount(VisualElement layer)
        {
            _stack = new VisualElement { pickingMode = PickingMode.Ignore };
            _stack.AddToClassList("ds-toasts");
            layer.Add(_stack);
            UiRoot.Instance.Frame += Tick;
        }

        public static void Show(string glyph, string text, Tone tone = Tone.Info)
        {
            if (_stack == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            InterfaceConfig.ToastRules rules = InterfaceConfig.Current.toast;
            while (Live.Count >= rules.max)
            {
                Live[0].Root.RemoveFromHierarchy();
                Live.RemoveAt(0);
            }

            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.AddToClassList("ds-toast");
            root.AddToClassList("is-gone");
            if (tone != Tone.Info)
            {
                root.AddToClassList("ds-toast--" + tone.ToString().ToLowerInvariant());
            }

            root.Add(Icons.Create(glyph, "ds-toast__icon"));
            root.Add(Kit.Label(text, "ds-toast__label"));
            _stack.Add(root);
            root.schedule.Execute(() => root.RemoveFromClassList("is-gone")).StartingIn(16);
            Live.Add(new Entry { Root = root, Left = rules.seconds });
        }

        private static void Tick(float dt)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                Entry e = Live[i];
                e.Left -= dt;
                if (e.Left <= 0.3f && !e.Fading)
                {
                    e.Fading = true;
                    e.Root.AddToClassList("is-gone");
                }

                if (e.Left <= 0f)
                {
                    e.Root.RemoveFromHierarchy();
                    Live.RemoveAt(i);
                }
            }
        }

        private sealed class Entry
        {
            public VisualElement Root;
            public float Left;
            public bool Fading;
        }
    }
}
