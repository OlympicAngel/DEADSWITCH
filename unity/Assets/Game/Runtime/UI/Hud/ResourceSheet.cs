using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// Resource breakdown (SPEC-039 ideas 14-15): one tap on a pod shows where the resource comes from, where it
    /// goes, when it fills or runs dry, and shortcuts that fly the camera to the building that helps. Producer and
    /// drain rows select their facility. Read-only: every action routes through the base screen or a destination.
    /// </summary>
    public sealed class ResourceSheet
    {
        private readonly VisualElement _root;
        private readonly ScrollView _scroll;
        private readonly System.Action<ResHelp> _onHelp;
        private readonly System.Action<int> _onSlot;
        private ResKind _kind;

        public ResourceSheet(VisualElement layer, System.Action<ResHelp> onHelp, System.Action<int> onSlot)
        {
            _onHelp = onHelp;
            _onSlot = onSlot;
            _root = new VisualElement();
            _root.AddToClassList("ds-sheet");
            _root.AddToClassList("sheet");
            _root.AddToClassList("rsheet");
            _root.AddToClassList("is-hidden");
            var grip = new VisualElement();
            grip.AddToClassList("ds-sheet__grip");
            _root.Add(grip);
            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.AddToClassList("rsheet__scroll");
            _root.Add(_scroll);
            _root.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            layer.Add(_root);
        }

        public bool IsOpen { get; private set; }

        public void Toggle(ResKind kind)
        {
            if (IsOpen && kind == _kind)
            {
                Close();
                return;
            }

            _kind = kind;
            IsOpen = true;
            _root.RemoveFromClassList("is-hidden");
            Refresh();
            _scroll.scrollOffset = UnityEngine.Vector2.zero;
        }

        public void Close()
        {
            IsOpen = false;
            _root.AddToClassList("is-hidden");
        }

        public void Refresh()
        {
            if (!IsOpen)
            {
                return;
            }

            GameHost host = GameHost.Instance;
            ResourceInfo r = ResourceInfo.Of(_kind, host.Sim.State, host.Config);
            double secPerHour = 3600.0 / host.Settings.DevTimeScale;
            VisualElement c = _scroll.contentContainer;
            c.Clear();

            // header: icon, name, value / cap, state
            var head = Row("rsheet__head");
            var well = new VisualElement();
            well.AddToClassList("rsheet__well");
            well.Add(Icons.Create(r.Glyph, "rsheet__icon"));
            head.Add(well);
            var titles = new VisualElement();
            titles.AddToClassList("rsheet__titles");
            titles.Add(Kit.Label(r.Name, "rsheet__name"));
            var amount = Row("row--baseline");
            amount.Add(Kit.Label(Fmt.Num(r.Value), "rsheet__value"));
            amount.Add(Kit.Label(" / " + Fmt.Num(r.Cap), "rsheet__cap"));
            titles.Add(amount);
            head.Add(titles);
            if (r.State != ResState.Ok)
            {
                head.Add(Chip(ResourceInfo.StateName(r.State), r.State == ResState.Short ? "ds-chip--red" : r.State == ResState.Low ? "ds-chip--amber" : "ds-chip--phosphor"));
            }

            var close = new VisualElement();
            close.AddToClassList("sheet__close");
            close.Add(Icons.Create("close", "rsheet__close-icon"));
            close.RegisterCallback<ClickEvent>(_ => Close());
            head.Add(close);
            c.Add(head);

            VisualElement bar = Kit.Progress(r.State == ResState.Short ? "red" : r.State == ResState.Low ? "amber" : r.State == ResState.Full ? "phosphor" : null);
            bar.AddToClassList("rsheet__bar");
            Kit.SetProgress(bar, r.Fill);
            c.Add(bar);

            var rate = Row("rsheet__rate");
            if (r.HasRate)
            {
                rate.Add(Kit.Delta(r.PerHour));
            }

            double hours = r.Hours;
            string when = hours < 0
                ? (r.HasRate && r.PerHour > 0 ? "Storage full: output is being wasted." : r.HasRate ? "Holding steady." : string.Empty)
                : (r.PerHour > 0 ? "Full in " : "Runs dry in ") + Fmt.Span(hours * secPerHour) + ".";
            rate.Add(Kit.Label(when, "rsheet__when"));
            c.Add(rate);
            if (r.Note.Length > 0)
            {
                c.Add(Kit.Label(r.Note, "ds-body", "rsheet__note"));
            }

            if (r.Help.Count > 0)
            {
                c.Add(Kit.Section("up", "HOW TO GET MORE"));
                foreach (ResHelp help in r.Help)
                {
                    c.Add(HelpRow(help));
                }
            }

            Lines(c, "trendup", "PRODUCING", r.Sources, "Nothing produces this right now.");
            if (r.Drains.Count > 0 || r.Kind == ResKind.Energy)
            {
                Lines(c, "trenddown", "DRAINING", r.Drains, "No drains.");
            }
        }

        private void Lines(VisualElement c, string glyph, string title, System.Collections.Generic.List<ResLine> lines, string empty)
        {
            c.Add(Kit.Section(glyph, title));
            if (lines.Count == 0)
            {
                c.Add(Kit.Label(empty, "rsheet__empty"));
                return;
            }

            foreach (ResLine line in lines)
            {
                var row = Row("rsheet__line");
                row.Add(Icons.Create(line.Glyph, "rsheet__line-icon"));
                row.Add(Kit.Label(line.Label, "rsheet__line-label"));
                var pill = Kit.Delta(line.PerHour);
                row.Add(pill);
                if (line.Slot >= 0)
                {
                    int slot = line.Slot;
                    row.AddToClassList("is-link");
                    row.Add(Icons.Create("chevron", "rsheet__chev"));
                    row.RegisterCallback<ClickEvent>(_ => _onSlot(slot));
                }

                c.Add(row);
            }
        }

        private VisualElement HelpRow(ResHelp help)
        {
            var row = Row("rsheet__help");
            var well = new VisualElement();
            well.AddToClassList("rsheet__help-well");
            well.Add(Icons.Create(help.Glyph, "rsheet__help-icon"));
            row.Add(well);
            var body = new VisualElement();
            body.AddToClassList("rsheet__help-body");
            body.Add(Kit.Label(help.Title.ToUpperInvariant(), "rsheet__help-title"));
            body.Add(Kit.Label(help.Why, "rsheet__help-why"));
            row.Add(body);
            bool can = help.Screen != null || help.Slot >= 0;
            row.EnableInClassList("is-disabled", !can);
            row.Add(Icons.Create(can ? "arrow" : "lock", "rsheet__help-go"));
            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (can)
                {
                    _onHelp(help);
                }
                else
                {
                    Kit.Shake(row);
                }
            });
            return row;
        }

        private static VisualElement Row(string cls)
        {
            var r = new VisualElement();
            r.AddToClassList(cls);
            return r;
        }

        private static VisualElement Chip(string text, string tone)
        {
            var chip = new VisualElement();
            chip.AddToClassList("ds-chip");
            chip.AddToClassList(tone);
            chip.Add(Kit.Label(text, "ds-chip__label"));
            return chip;
        }
    }
}
