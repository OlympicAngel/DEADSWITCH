using Deadswitch.Game.Presentation;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Screens
{
    /// <summary>
    /// FIELD GUIDE (SPEC-043): every term in plain language, grouped by Resources, People, Defense, The AI, The World
    /// and Progress. Opened from the Command menu; content lives in <see cref="Glossary"/>.
    /// </summary>
    public sealed class FieldGuideScreen : IGameScreen
    {
        public FieldGuideScreen(ScreenRouter router)
        {
            Root = new VisualElement();
            Root.AddToClassList("guide");
            var head = new VisualElement();
            head.AddToClassList("ds-head");
            var well = new VisualElement();
            well.AddToClassList("ds-head__well");
            well.Add(Icons.Create("book", "ds-head__icon"));
            head.Add(well);
            var body = new VisualElement();
            body.AddToClassList("ds-head__body");
            body.Add(Kit.Label("FIELD GUIDE", "ds-head__title"));
            body.Add(Kit.Label("EVERY TERM, IN PLAIN WORDS", "ds-head__status"));
            head.Add(body);
            var close = new VisualElement();
            close.AddToClassList("ds-iconbtn");
            close.Add(Icons.Create("close", "ds-iconbtn__icon"));
            close.RegisterCallback<ClickEvent>(_ => router.Return());
            head.Add(close);
            Root.Add(head);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("guide__scroll");
            foreach (Glossary.Group g in Glossary.Groups)
            {
                VisualElement card = Kit.Card(g.Glyph, g.Title);
                foreach (Glossary.Entry e in g.Entries)
                {
                    var row = new VisualElement();
                    row.AddToClassList("ds-row");
                    row.AddToClassList("guide__row");
                    row.Add(Icons.Create(e.Glyph, "ds-row__icon", "guide__icon"));
                    var text = new VisualElement();
                    text.AddToClassList("guide__text");
                    text.Add(Kit.Label(e.Term, "guide__term"));
                    text.Add(Kit.Label(e.Meaning, "ds-prose", "guide__meaning"));
                    row.Add(text);
                    card.Add(row);
                }

                Kit.MarkEnds(card);
                scroll.Add(card);
            }

            Root.Add(scroll);
        }

        public string Id => "guide";

        public VisualElement Root { get; }

        public void OnShow()
        {
        }

        public void OnHide()
        {
        }
    }
}
