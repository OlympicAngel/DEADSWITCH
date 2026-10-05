using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Progressive disclosure for dense screens (SPEC-040 ideas 70-71).
    /// <para>Pages: a <c>.ds-pager</c> whose <c>.ds-page</c> children each start with a <c>.ds-page__title</c> label
    /// (and optionally a <c>.ds-page__glyph</c> label naming an icon) becomes a tab strip that shows one page at a
    /// time. The chosen page is remembered per pager name for the session.</para>
    /// <para>Folds: a <c>.ds-card--fold</c> card collapses to its header on a header tap; <c>is-folded</c> in UXML
    /// makes it start collapsed. The state is remembered per card name in PlayerPrefs.</para>
    /// </summary>
    public static class Pager
    {
        private static readonly Dictionary<string, int> Chosen = new Dictionary<string, int>();

        /// <summary>Raised when a page is shown (pager, page index).</summary>
        public static event System.Action<VisualElement, int> PageShown;

        public static void Decorate(VisualElement root)
        {
            root.Query(className: "ds-pager").ForEach(Build);
            root.Query(className: "ds-card--fold").ForEach(Fold);
        }

        /// <summary>Shows a page by its element name (for example the threat page when a raid starts).</summary>
        public static void Show(VisualElement pager, string pageName)
        {
            List<VisualElement> pages = Pages(pager);
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].name == pageName)
                {
                    Select(pager, i, true);
                    return;
                }
            }
        }

        /// <summary>Sets a small count badge on a page tab (0 hides it).</summary>
        public static void Badge(VisualElement pager, string pageName, int count)
        {
            List<VisualElement> pages = Pages(pager);
            VisualElement tabs = pager.Q(className: "ds-pager__tabs");
            for (int i = 0; i < pages.Count && tabs != null && i < tabs.childCount; i++)
            {
                if (pages[i].name == pageName)
                {
                    VisualElement badge = tabs[i].Q(className: "ds-pager__badge");
                    badge.EnableInClassList("is-hidden", count <= 0);
                    badge.Q<Label>().text = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            }
        }

        private static List<VisualElement> Pages(VisualElement pager)
        {
            var pages = new List<VisualElement>();
            foreach (VisualElement child in pager.Children())
            {
                if (child.ClassListContains("ds-page"))
                {
                    pages.Add(child);
                }
            }

            return pages;
        }

        private static void Build(VisualElement pager)
        {
            if (pager.Q(className: "ds-pager__tabs") != null)
            {
                return;
            }

            List<VisualElement> pages = Pages(pager);
            var tabs = new VisualElement();
            tabs.AddToClassList("ds-pager__tabs");
            for (int i = 0; i < pages.Count; i++)
            {
                Label title = pages[i].Q<Label>(className: "ds-page__title");
                Label glyph = pages[i].Q<Label>(className: "ds-page__glyph");
                var tab = new VisualElement();
                tab.AddToClassList("ds-pager__tab");
                if (glyph != null)
                {
                    tab.Add(Icons.Create(glyph.text, "ds-pager__icon"));
                }

                tab.Add(Kit.Label(title != null ? title.text : "PAGE " + (i + 1), "ds-pager__label"));
                var badge = new VisualElement { pickingMode = PickingMode.Ignore };
                badge.AddToClassList("ds-pager__badge");
                badge.AddToClassList("is-hidden");
                badge.Add(Kit.Label("0", "ds-pager__badge-label"));
                tab.Add(badge);
                int index = i;
                tab.RegisterCallback<ClickEvent>(_ => Select(pager, index, true));
                tabs.Add(tab);
            }

            var rail = new VisualElement { pickingMode = PickingMode.Ignore };
            rail.AddToClassList("ds-pager__rail");
            tabs.Add(rail);
            pager.Insert(0, tabs);
            int start = !string.IsNullOrEmpty(pager.name) && Chosen.TryGetValue(pager.name, out int saved) ? saved : 0;
            Select(pager, Mathf.Clamp(start, 0, Mathf.Max(0, pages.Count - 1)), false);
            tabs.RegisterCallback<GeometryChangedEvent>(_ => PlaceRail(pager, false));
        }

        private static void Select(VisualElement pager, int index, bool animate)
        {
            List<VisualElement> pages = Pages(pager);
            VisualElement tabs = pager.Q(className: "ds-pager__tabs");
            for (int i = 0; i < pages.Count; i++)
            {
                pages[i].style.display = i == index ? DisplayStyle.Flex : DisplayStyle.None;
                if (tabs != null && i < tabs.childCount)
                {
                    tabs[i].EnableInClassList("is-active", i == index);
                }
            }

            pager.userData = index;
            if (!string.IsNullOrEmpty(pager.name))
            {
                Chosen[pager.name] = index;
            }

            PlaceRail(pager, animate);
            if (animate && index < pages.Count)
            {
                Choreo.Enter(pages[index]);
                PageShown?.Invoke(pager, index);
            }
        }

        /// <summary>The underline glides to the active tab.</summary>
        private static void PlaceRail(VisualElement pager, bool animate)
        {
            VisualElement tabs = pager.Q(className: "ds-pager__tabs");
            VisualElement rail = tabs?.Q(className: "ds-pager__rail");
            int index = pager.userData is int i ? i : 0;
            if (rail == null || index >= tabs.childCount - 1)
            {
                return;
            }

            Rect target = tabs[index].layout;
            if (float.IsNaN(target.width) || target.width <= 0f)
            {
                return;
            }

            float fromX = rail.resolvedStyle.left;
            float fromW = rail.resolvedStyle.width;
            if (!animate || float.IsNaN(fromX) || fromW <= 0f)
            {
                rail.style.left = target.x;
                rail.style.width = target.width;
                return;
            }

            Motion.To(rail, 0.28f, Ease.OutCubic, t =>
            {
                rail.style.left = Mathf.Lerp(fromX, target.x, t);
                rail.style.width = Mathf.Lerp(fromW, target.width, t);
            });
        }

        private static void Fold(VisualElement card)
        {
            VisualElement head = card.Q(className: "ds-card__head");
            if (head == null || head.Q(className: "ds-fold__chev") != null)
            {
                return;
            }

            head.Add(Icons.Create("chevron", "ds-fold__chev"));
            head.AddToClassList("ds-fold__head");
            string key = "ds.fold." + card.name;
            bool folded = !string.IsNullOrEmpty(card.name) && PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) == 1 : card.ClassListContains("is-folded");
            Apply(card, head, folded);
            head.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                bool now = !card.ClassListContains("is-folded");
                Apply(card, head, now);
                if (!string.IsNullOrEmpty(card.name))
                {
                    PlayerPrefs.SetInt(key, now ? 1 : 0);
                }

                if (!now)
                {
                    Choreo.Enter(card);
                }
            });
        }

        private static void Apply(VisualElement card, VisualElement head, bool folded)
        {
            card.EnableInClassList("is-folded", folded);
            foreach (VisualElement child in card.Children())
            {
                if (child != head && !child.ClassListContains("ds-corner"))
                {
                    child.style.display = folded ? new StyleEnum<DisplayStyle>(DisplayStyle.None) : new StyleEnum<DisplayStyle>(StyleKeyword.Null);
                }
            }

            VisualElement chev = head.Q(className: "ds-fold__chev");
            chev.style.rotate = new Rotate(new Angle(folded ? 0f : 90f, AngleUnit.Degree));
        }
    }
}
