using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// The Command menu (SPEC-042 finding 1): one drawer that lists every secondary destination (workforce,
    /// dispatch, reports, story, legacy, season, full game, settings) with an icon, a line of meaning and a badge.
    /// Opens from the menu button in the context strip; a tap on the backdrop, the close button or back closes it.
    /// </summary>
    public sealed class CommandMenu
    {
        private readonly VisualElement _root;
        private readonly VisualElement _drawer;
        private readonly VisualElement _list;
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly Label _status;

        public CommandMenu(VisualElement layer)
        {
            _root = new VisualElement();
            _root.AddToClassList("cmenu");
            _root.AddToClassList("is-hidden");
            var backdrop = new VisualElement();
            backdrop.AddToClassList("cmenu__backdrop");
            backdrop.RegisterCallback<ClickEvent>(_ => Close());
            _root.Add(backdrop);

            _drawer = new VisualElement();
            _drawer.AddToClassList("cmenu__drawer");
            Sheen.Attach(_drawer);
            var head = new VisualElement();
            head.AddToClassList("cmenu__head");
            var titles = new VisualElement();
            titles.AddToClassList("cmenu__titles");
            titles.Add(Kit.Label("COMMAND", "cmenu__title"));
            _status = Kit.Label(string.Empty, "cmenu__status");
            titles.Add(_status);
            head.Add(titles);
            var close = new VisualElement();
            close.AddToClassList("ds-iconbtn");
            close.Add(Icons.Create("close", "ds-iconbtn__icon"));
            close.RegisterCallback<ClickEvent>(_ => Close());
            head.Add(close);
            _drawer.Add(head);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("cmenu__scroll");
            _list = scroll.contentContainer;
            _drawer.Add(scroll);
            _drawer.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            _root.Add(_drawer);
            layer.Add(_root);
            Popovers.Register("menu", () => IsOpen, Close, _drawer);
            Back.Register(() =>
            {
                if (!IsOpen)
                {
                    return false;
                }

                Close();
                return true;
            });
        }

        public bool IsOpen { get; private set; }

        /// <summary>Adds a section label.</summary>
        public void Section(string glyph, string title)
        {
            _list.Add(Kit.Section(glyph, title));
        }

        /// <summary>Adds a destination; <paramref name="badge"/> returns the count to show (0 hides).</summary>
        public void Add(string glyph, string title, string meaning, System.Action open, System.Func<int> badge = null)
        {
            var row = new VisualElement();
            row.AddToClassList("cmenu__item");
            var well = new VisualElement();
            well.AddToClassList("cmenu__well");
            well.Add(Icons.Create(glyph, "cmenu__icon"));
            row.Add(well);
            var body = new VisualElement();
            body.AddToClassList("cmenu__body");
            body.Add(Kit.Label(title, "cmenu__name"));
            body.Add(Kit.Label(meaning, "cmenu__meaning"));
            row.Add(body);
            var count = new VisualElement();
            count.AddToClassList("cmenu__badge");
            count.AddToClassList("is-hidden");
            var countLabel = Kit.Label("0", "cmenu__badge-label");
            count.Add(countLabel);
            row.Add(count);
            row.Add(Icons.Create("chevron", "cmenu__chev"));
            row.RegisterCallback<ClickEvent>(_ =>
            {
                Close();
                open();
            });
            _list.Add(row);
            _entries.Add(new Entry { Badge = badge, Count = count, Label = countLabel });
        }

        public void Open(string status)
        {
            Popovers.Opening("menu");
            IsOpen = true;
            _status.text = status;
            _root.RemoveFromClassList("is-hidden");
            Motion.To(_drawer, 0.32f, Ease.OutCubic, t =>
            {
                _drawer.style.translate = new Translate(Length.Percent((t - 1f) * 100f), 0, 0);
                _root.style.opacity = Mathf.Min(1f, t * 2f);
            });
            Choreo.Enter(_drawer);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            Motion.To(_drawer, 0.22f, Ease.OutCubic, t =>
            {
                _drawer.style.translate = new Translate(Length.Percent(-t * 100f), 0, 0);
                _root.style.opacity = 1f - t;
            }, () => _root.AddToClassList("is-hidden"));
        }

        /// <summary>Refreshes the badges; returns their sum (for the menu button).</summary>
        public int Refresh()
        {
            int total = 0;
            foreach (Entry e in _entries)
            {
                int n = e.Badge != null ? e.Badge() : 0;
                total += n;
                e.Count.EnableInClassList("is-hidden", n <= 0);
                e.Label.text = n.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return total;
        }

        private sealed class Entry
        {
            public System.Func<int> Badge;
            public VisualElement Count;
            public Label Label;
        }
    }
}
