using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Popovers, sheets and drawers (F-109): only one group is open at a time (opening one closes the others), and
    /// a tap outside every open one closes them. A drag is not a tap, so panning the camera leaves them alone. The
    /// controls that toggle a panel carry <see cref="OpenerClass"/>, so a tap on them still toggles it instead of
    /// closing and reopening it.
    /// </summary>
    public static class Popovers
    {
        /// <summary>Marks a control that opens or closes a panel itself.</summary>
        public const string OpenerClass = "ds-opener";

        private const float TapSlop = 24f;
        private static readonly List<Entry> Entries = new List<Entry>();
        private static Vector2 _down;
        private static bool _tracking;

        /// <summary>Listens for taps on the UI root (call once; panels may register before or after).</summary>
        public static void Install(VisualElement root)
        {
            root.RegisterCallback<PointerDownEvent>(e =>
            {
                _down = e.position;
                _tracking = true;
            }, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!_tracking)
                {
                    return;
                }

                _tracking = false;
                if (((Vector2)e.position - _down).sqrMagnitude > TapSlop * TapSlop)
                {
                    return;
                }

                var target = e.target as VisualElement;
                if (Inside(target) || Opener(target))
                {
                    return;
                }

                foreach (Entry entry in Entries)
                {
                    if (entry.IsOpen())
                    {
                        entry.Close();
                    }
                }
            }, TrickleDown.TrickleDown);
        }

        /// <summary>Adds a panel: its group (panels shown together share one), how to read and close it, and its surfaces.</summary>
        public static void Register(string group, Func<bool> isOpen, Action close, params VisualElement[] roots)
        {
            Entries.Add(new Entry { Group = group, IsOpen = isOpen, Close = close, Roots = roots });
        }

        /// <summary>A panel of this group is opening: close every other group.</summary>
        public static void Opening(string group)
        {
            foreach (Entry entry in Entries)
            {
                if (entry.Group != group && entry.IsOpen())
                {
                    entry.Close();
                }
            }
        }

        private static bool Inside(VisualElement target)
        {
            for (VisualElement el = target; el != null; el = el.parent)
            {
                foreach (Entry entry in Entries)
                {
                    if (!entry.IsOpen())
                    {
                        continue;
                    }

                    foreach (VisualElement root in entry.Roots)
                    {
                        if (root == el)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool Opener(VisualElement target)
        {
            for (VisualElement el = target; el != null; el = el.parent)
            {
                if (el.ClassListContains(OpenerClass))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class Entry
        {
            public string Group;
            public Func<bool> IsOpen;
            public Action Close;
            public VisualElement[] Roots;
        }
    }
}
