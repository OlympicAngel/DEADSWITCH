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
        private readonly VisualElement _layer;
        private readonly List<Tag> _tags = new List<Tag>();
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
            }
        }

        /// <summary>Marks the plot the opening guide points at (-1 clears).</summary>
        public void Guide(int slot)
        {
            for (int i = 0; i < _tags.Count; i++)
            {
                _tags[i].Root.EnableInClassList("is-guide", i == slot);
            }
        }

        /// <summary>Updates text and state from the sim (after ticks/commands).</summary>
        public void Refresh()
        {
            GameHost host = GameHost.Instance;
            GameState s = host.Sim.State;
            AddTags(s.Slots.Count);
            for (int i = 0; i < _tags.Count && i < s.Slots.Count; i++)
            {
                FacilitySlot slot = s.Slots[i];
                BuildJob job = s.JobForSlot(i);
                Tag t = _tags[i];
                bool empty = slot.IsEmpty && job == null;
                t.Root.EnableInClassList("lbl--empty", empty);
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
                t.State.text = job != null ? "BUILDING" : (!slot.Enabled ? "OFF" : (!slot.Powered ? "NO POWER" : (!slot.Staffed ? "AI-RUN" : string.Empty)));
            }

            _core.Name.text = "CORE";
            _core.Level.text = Fmt.BandName(CorruptionSystem.Band(host.Config, s.CorruptionMilli));
            _core.State.text = CorruptionSystem.Percent(s.CorruptionMilli) + "%";
            _core.Pip.style.display = DisplayStyle.None;
        }

        /// <summary>Positions every tag over its world anchor (call each frame after the camera moved).</summary>
        public void Track(Camera cam, BaseView view, float panelWidth, float panelHeight)
        {
            if (cam == null || view == null || panelWidth <= 0f)
            {
                return;
            }

            for (int i = 0; i < _tags.Count && i < view.SlotCount; i++)
            {
                Place(_tags[i], cam, view.LabelAnchor(i), panelWidth, panelHeight);
            }

            Place(_core, cam, view.CoreAnchor, panelWidth, panelHeight);
        }

        private static void Place(Tag t, Camera cam, Vector3 world, float pw, float ph)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            bool visible = sp.z > 0f;
            t.Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            t.Root.style.left = sp.x / Screen.width * pw;
            t.Root.style.top = (Screen.height - sp.y) / Screen.height * ph;
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
        }
    }
}
