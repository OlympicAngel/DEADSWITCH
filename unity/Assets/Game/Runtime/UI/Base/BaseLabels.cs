using System.Collections.Generic;
using Deadswitch.Game.Base;
using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Base
{
    /// <summary>
    /// Bracketed tags floating above each facility and the core door (owner reference): name, level and state.
    /// They track the drone camera every frame and select the slot when tapped.
    /// </summary>
    public sealed class BaseLabels
    {
        /// <summary>Panel px kept between a tag and the screen edge.</summary>
        private const float EdgeMargin = 12f;

        private readonly VisualElement _layer;
        private readonly List<Tag> _tags = new List<Tag>();
        private readonly List<Tag> _order = new List<Tag>();
        private readonly Tag _core;
        private readonly System.Action<int> _onSlot;
        private int _selected = -1;

        public BaseLabels(VisualElement layer, System.Action<int> onSlot, System.Action onCore)
        {
            _layer = layer;
            _onSlot = onSlot;
            AddTags(GameHost.Instance.Sim.State.Slots.Count);
            _core = NewTag();
            _core.Root.AddToClassList("lbl--core");
            _core.Root.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                onCore();
            });
        }

        /// <summary>Adds tags for plots the Hub gained (tier-up, SPEC-013).</summary>
        private void AddTags(int slots)
        {
            for (int i = _tags.Count; i < slots; i++)
            {
                int slot = i;
                Tag t = NewTag();
                t.Root.RegisterCallback<ClickEvent>(e =>
                {
                    e.StopPropagation();
                    _onSlot(slot);
                });
                _tags.Add(t);
            }
        }

        public void Select(int slot)
        {
            _selected = slot;
            for (int i = 0; i < _tags.Count; i++)
            {
                _tags[i].Root.EnableInClassList("is-selected", i == slot);
                _tags[i].Root.EnableInClassList("is-faded", slot >= 0 && i != slot);
                _tags[i].Hidden = slot >= 0;
            }

            // a quiet open plot shows its label while it is the one selected
            Refresh();
        }

        /// <summary>Marks the plot the opening guide points at (-1 clears).</summary>
        public void Guide(int slot)
        {
            for (int i = 0; i < _tags.Count; i++)
            {
                _tags[i].Root.EnableInClassList("is-guide", i == slot);
            }
        }

        /// <summary>
        /// The one open plot that carries the build prompt (F-108): the first free plot in order, so the prompt moves
        /// on as each one is built. -1 when none is free. The other open plots stay quiet but still open the picker.
        /// </summary>
        public static int NextOpenPlot(GameState s)
        {
            for (int i = 0; i < s.Slots.Count; i++)
            {
                if (s.Slots[i].IsEmpty && s.JobForSlot(i) == null)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Updates text and state from the sim (after ticks/commands).</summary>
        public void Refresh()
        {
            GameHost host = GameHost.Instance;
            GameState s = host.Sim.State;
            AddTags(s.Slots.Count);
            int prompt = NextOpenPlot(s);
            for (int i = 0; i < _tags.Count && i < s.Slots.Count; i++)
            {
                FacilitySlot slot = s.Slots[i];
                BuildJob job = s.JobForSlot(i);
                Tag t = _tags[i];
                bool empty = slot.IsEmpty && job == null;
                t.Root.EnableInClassList("lbl--empty", empty);
                t.Root.EnableInClassList("is-quiet", empty && i != prompt && i != _selected);
                t.Root.EnableInClassList("lbl--off", !slot.IsEmpty && (!slot.Enabled || !slot.Powered));
                t.Root.EnableInClassList("lbl--warn", !slot.IsEmpty && slot.Enabled && slot.Powered && !slot.Staffed);
                if (empty)
                {
                    t.Name.text = "OPEN PLOT";
                    t.Level.text = "+ BUILD";
                    t.State.text = string.Empty;
                    t.Pip.style.display = DisplayStyle.None;
                    continue;
                }

                FacilityKind kind = slot.IsEmpty ? job.Kind : slot.Kind;
                t.Name.text = Fmt.FacilityName(kind);
                t.Level.text = slot.IsEmpty ? "LVL 0" : "LVL " + slot.Level;
                t.Pip.style.display = DisplayStyle.Flex;
                t.Pip.EnableInClassList("ds-pip--amber", job != null || !slot.Staffed);
                t.Pip.EnableInClassList("ds-pip--diamond", job != null || !slot.Staffed);
                t.Pip.EnableInClassList("ds-pip--red", !slot.IsEmpty && (!slot.Enabled || !slot.Powered));
                t.State.text = job != null ? "BUILDING" : (!slot.Enabled ? "OFF" : (!slot.Powered ? "NO POWER" : (!slot.Staffed ? "RUN BY AI" : string.Empty)));
            }

            _core.Name.text = "CORE";
            _core.Level.text = Fmt.BandName(CorruptionSystem.Band(host.Config, s.CorruptionMilli));
            _core.State.text = CorruptionSystem.Percent(s.CorruptionMilli) + "%";
            _core.Pip.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Positions every tag over its world anchor (call each frame after the camera moved). Tags stay below
        /// <paramref name="topClear"/> (panel px; the HUD status rows) and step out of <paramref name="keepOut"/> (the
        /// alert rail) so a world tag never sits under the HUD. A building under the bottom docks (from
        /// <paramref name="bottomClear"/> down) shows no tag.
        /// </summary>
        public void Track(Camera cam, BaseView view, float panelWidth, float panelHeight, float topClear, float bottomClear, Rect keepOut)
        {
            if (cam == null || view == null || panelWidth <= 0f)
            {
                return;
            }

            for (int i = 0; i < _tags.Count && i < view.SlotCount; i++)
            {
                Place(_tags[i], cam, view.LabelAnchor(i), panelWidth, panelHeight, topClear, bottomClear, keepOut);
            }

            _core.Hidden = _selected >= 0;
            Place(_core, cam, view.CoreAnchor, panelWidth, panelHeight, topClear, bottomClear, keepOut);
            Declutter(view.SlotCount);
        }

        /// <summary>
        /// Seen from the district the old compound's tags crowd into one row. The selected tag keeps its place, then
        /// the tags nearest the camera (lowest on screen); a tag that would overlap one already kept steps out.
        /// </summary>
        private void Declutter(int slots)
        {
            _order.Clear();
            for (int i = 0; i < _tags.Count && i < slots; i++)
            {
                _order.Add(_tags[i]);
            }

            _order.Add(_core);
            Tag selected = _selected >= 0 && _selected < _tags.Count ? _tags[_selected] : null;
            _order.Sort((a, b) =>
            {
                int rank = (a == selected ? 0 : 1).CompareTo(b == selected ? 0 : 1);
                return rank != 0 ? rank : b.Box.yMax.CompareTo(a.Box.yMax);
            });
            for (int i = 0; i < _order.Count; i++)
            {
                Tag t = _order[i];
                if (!t.Shown)
                {
                    continue;
                }

                for (int j = 0; j < i; j++)
                {
                    if (_order[j].Shown && _order[j].Box.Overlaps(t.Box))
                    {
                        // visibility, not display: the tag keeps its size for next frame's test
                        t.Shown = false;
                        t.Root.style.visibility = Visibility.Hidden;
                        break;
                    }
                }
            }
        }

        private static void Place(Tag t, Camera cam, Vector3 world, float pw, float ph, float topClear, float bottomClear, Rect keepOut)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            // while a facility is framed every tag steps out: the close shot pushes them under the HUD and the sheet names it
            bool visible = sp.z > 0f && !t.Hidden;
            // the tag hangs above its anchor (translate -50% -100%)
            float y = (Screen.height - sp.y) / Screen.height * ph;
            // a building under the HUD loses its tag: clamping them all to the top row piles them up
            visible &= y > topClear && y < bottomClear;
            t.Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            // quiet open plots are already hidden by style and never push a tag aside
            t.Shown = visible && !t.Root.ClassListContains("is-quiet");
            t.Root.style.visibility = StyleKeyword.Null;

            // the tag is centred on its anchor (translate -50%); keep it whole on screen so a plot near the edge
            // still shows its full name
            float x = sp.x / Screen.width * pw;
            float half = float.IsNaN(t.Root.resolvedStyle.width) ? 0f : t.Root.resolvedStyle.width * 0.5f;
            if (half > 0f && half < (pw * 0.5f) - EdgeMargin)
            {
                x = Mathf.Clamp(x, half + EdgeMargin, pw - half - EdgeMargin);
            }

            float h = float.IsNaN(t.Root.resolvedStyle.height) ? 0f : t.Root.resolvedStyle.height;
            y = Mathf.Max(y, topClear + h + EdgeMargin);
            if (half > 0f && keepOut.width > 0f && x + half > keepOut.xMin && x - half < keepOut.xMax && y > keepOut.yMin && y - h < keepOut.yMax)
            {
                // step aside the shorter way: left of the rail, or down below it
                float left = x + half - keepOut.xMin + EdgeMargin;
                float down = keepOut.yMax + h + EdgeMargin - y;
                if (left <= down && x - left - half >= EdgeMargin)
                {
                    x -= left;
                }
                else
                {
                    y += down;
                }
            }

            t.Root.style.left = x;
            t.Root.style.top = y;
            t.Box = new Rect(x - half, y - h, half * 2f, h);
        }

        private Tag NewTag()
        {
            var root = new VisualElement();
            root.AddToClassList("lbl");
            var name = Kit.Label("FACILITY", "lbl__name");
            var sub = new VisualElement();
            sub.AddToClassList("lbl__sub");
            var pip = new VisualElement();
            pip.AddToClassList("ds-pip");
            var level = Kit.Label("LVL 1", "lbl__level");
            var state = Kit.Label(string.Empty, "lbl__state");
            sub.Add(pip);
            sub.Add(level);
            sub.Add(state);
            root.Add(name);
            root.Add(sub);
            Kit.AddCorners(root);
            _layer.Add(root);
            return new Tag { Root = root, Name = name, Level = level, State = state, Pip = pip };
        }

        private sealed class Tag
        {
            public VisualElement Root;
            public Label Name;
            public Label Level;
            public Label State;
            public VisualElement Pip;
            public bool Hidden;
            public bool Shown;
            public Rect Box;
        }
    }
}
