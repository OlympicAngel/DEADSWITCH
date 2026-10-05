using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Runtime behaviors for the component kit (Components.uss). UXML stays plain; this adds corner brackets,
    /// meter segments and custom-drawn elements. tools/uipreview mirrors these so previews match.
    /// </summary>
    public static class Kit
    {
        public const int MeterSegments = 20;

        public static void Decorate(VisualElement root)
        {
            root.Query(className: "ds-panel").ForEach(AddCorners);
            root.Query(className: "ds-meter").ForEach(BuildMeter);
            root.Query().ForEach(MarkEnds);
            Pager.Decorate(root);
            Sheen.Decorate(root);
            root.Query(className: "ds-switch").ForEach(AddKnob);
        }

        /// <summary>
        /// Tags the first and last content children with <c>is-first</c> / <c>is-last</c>: USS has no
        /// <c>:first-child</c>, <c>:last-child</c> or sibling combinators. Call again after rebuilding a list
        /// in code. Corner brackets are skipped.
        /// </summary>
        public static void MarkEnds(VisualElement parent)
        {
            VisualElement first = null;
            VisualElement last = null;
            foreach (VisualElement child in parent.Children())
            {
                child.RemoveFromClassList("is-first");
                child.RemoveFromClassList("is-last");
                if (!child.ClassListContains("ds-corner"))
                {
                    first ??= child;
                    last = child;
                }
            }

            first?.AddToClassList("is-first");
            last?.AddToClassList("is-last");
        }

        /// <summary>The sliding knob of a <c>.ds-switch</c> (its position follows <c>is-on</c> in USS).</summary>
        public static void AddKnob(VisualElement sw)
        {
            if (sw.Q(className: "ds-switch__knob") != null)
            {
                return;
            }

            var knob = new VisualElement { pickingMode = PickingMode.Ignore };
            knob.AddToClassList("ds-switch__knob");
            sw.Add(knob);
        }

        public static void AddCorners(VisualElement panel)
        {
            if (panel.Q(className: "ds-corner") != null)
            {
                return;
            }

            foreach (string c in new[] { "tl", "tr", "bl", "br" })
            {
                var corner = new VisualElement { pickingMode = PickingMode.Ignore };
                corner.AddToClassList("ds-corner");
                corner.AddToClassList("ds-corner--" + c);
                panel.Add(corner);
            }
        }

        public static void BuildMeter(VisualElement meter)
        {
            if (meter.childCount > 0)
            {
                return;
            }

            for (int i = 0; i < MeterSegments; i++)
            {
                var seg = new VisualElement { pickingMode = PickingMode.Ignore };
                seg.AddToClassList("ds-meter__seg");
                meter.Add(seg);
            }
        }

        /// <summary>Lights segments for a 0..1 fill (rounded to whole segments; a non-zero value shows at least one).</summary>
        public static void SetMeter(VisualElement meter, float fill)
        {
            int n = meter.childCount;
            int on = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(fill) * n), fill > 0f ? 1 : 0, n);
            for (int i = 0; i < n; i++)
            {
                meter[i].EnableInClassList("is-on", i < on);
            }
        }

        public static VisualElement Panel(params string[] classes)
        {
            var p = new VisualElement();
            p.AddToClassList("ds-panel");
            foreach (string c in classes)
            {
                p.AddToClassList(c);
            }

            AddCorners(p);
            return p;
        }

        public static Label Label(string text, params string[] classes)
        {
            var l = new Label(text);
            foreach (string c in classes)
            {
                l.AddToClassList(c);
            }

            return l;
        }

        /// <summary>A kit button (VisualElement + label) with a click callback and press feedback.</summary>
        public static VisualElement Button(string text, System.Action onClick, params string[] classes)
        {
            var b = new VisualElement();
            b.AddToClassList("ds-btn");
            foreach (string c in classes)
            {
                b.AddToClassList(c);
            }

            b.Add(Label(text, "ds-btn__label"));
            Sheen.Attach(b);
            if (b.ClassListContains("ds-btn--primary"))
            {
                Choreo.Shimmer(b);
            }

            b.RegisterCallback<ClickEvent>(_ =>
            {
                if (!b.ClassListContains("is-disabled"))
                {
                    onClick?.Invoke();
                }
            });
            return b;
        }

        /// <summary>A grouped card (SPEC-039): icon well, title, optional subtitle; add content to the returned card.</summary>
        public static VisualElement Card(string glyph, string title, string sub = null, string tone = null)
        {
            var card = new VisualElement();
            card.AddToClassList("ds-card");
            if (!string.IsNullOrEmpty(tone))
            {
                card.AddToClassList("ds-card--" + tone);
            }

            var head = new VisualElement();
            head.AddToClassList("ds-card__head");
            var well = new VisualElement();
            well.AddToClassList("ds-card__well");
            well.Add(Icons.Create(glyph, "ds-card__icon"));
            head.Add(well);
            var titles = new VisualElement();
            titles.AddToClassList("ds-card__titles");
            titles.Add(Label(title, "ds-card__title"));
            if (!string.IsNullOrEmpty(sub))
            {
                titles.Add(Label(sub, "ds-card__sub"));
            }

            head.Add(titles);
            card.Add(head);
            Sheen.Attach(card);
            return card;
        }

        /// <summary>A section label with an icon and a rule.</summary>
        public static VisualElement Section(string glyph, string title)
        {
            var row = new VisualElement();
            row.AddToClassList("ds-section");
            row.Add(Icons.Create(glyph, "ds-section__icon"));
            row.Add(Label(title, "ds-section__title"));
            var rule = new VisualElement();
            rule.AddToClassList("ds-section__rule");
            row.Add(rule);
            return row;
        }

        /// <summary>A rate pill (+12/h up, -30/h down, 0 flat) with a trend glyph.</summary>
        public static VisualElement Delta(long perHour, string suffix = "/h")
        {
            var pill = new VisualElement();
            pill.AddToClassList("ds-delta");
            pill.Add(Icons.Create(perHour >= 0 ? "trendup" : "trenddown", "ds-delta__icon"));
            pill.Add(Label(string.Empty, "ds-delta__label"));
            SetDelta(pill, perHour, suffix);
            return pill;
        }

        public static void SetDelta(VisualElement pill, long perHour, string suffix = "/h")
        {
            pill.EnableInClassList("ds-delta--down", perHour < 0);
            pill.EnableInClassList("ds-delta--flat", perHour == 0);
            Icons.SetGlyph(pill.Q(className: "ds-delta__icon"), perHour > 0 ? "trendup" : perHour < 0 ? "trenddown" : "minus");
            pill.Q<Label>(className: "ds-delta__label").text = Presentation.Fmt.Signed(perHour) + suffix;
        }

        /// <summary>An icon-over-label action tile with an optional cost line.</summary>
        public static VisualElement Tile(string glyph, string label, string cost, System.Action onClick, params string[] classes)
        {
            var t = new VisualElement();
            t.AddToClassList("ds-tile");
            foreach (string c in classes)
            {
                t.AddToClassList(c);
            }

            Sheen.Attach(t);
            t.Add(Icons.Create(glyph, "ds-tile__icon"));
            t.Add(Label(label, "ds-tile__label"));
            if (!string.IsNullOrEmpty(cost))
            {
                t.Add(Label(cost, "ds-tile__cost"));
            }

            t.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                if (!t.ClassListContains("is-disabled"))
                {
                    onClick?.Invoke();
                }
                else
                {
                    Shake(t);
                }
            });
            return t;
        }

        /// <summary>A progress bar; set the fill with <see cref="SetProgress"/>.</summary>
        public static VisualElement Progress(string tone = null)
        {
            var bar = new VisualElement();
            bar.AddToClassList("ds-progress");
            if (!string.IsNullOrEmpty(tone))
            {
                bar.AddToClassList("ds-progress--" + tone);
            }

            var fill = new VisualElement();
            fill.AddToClassList("ds-progress__fill");
            bar.Add(fill);
            return bar;
        }

        public static void SetProgress(VisualElement bar, float fill01)
        {
            VisualElement fill = bar.Q(className: "ds-progress__fill");
            if (fill != null)
            {
                fill.style.width = Length.Percent(Mathf.Clamp01(fill01) * 100f);
            }
        }

        /// <summary>A short refusal wiggle (no-op under reduced motion).</summary>
        public static void Shake(VisualElement el)
        {
            Motion.To(el, 0.32f, Ease.Linear, t => el.style.translate = new Translate(Mathf.Sin(t * Mathf.PI * 6f) * (1f - t) * 10f, 0, 0), () => el.style.translate = new Translate(0, 0, 0));
        }

        public static void SetButtonText(VisualElement button, string text)
        {
            Label l = button.Q<Label>(className: "ds-btn__label");
            if (l != null)
            {
                l.text = text;
            }
        }
    }

    /// <summary>Line chart of recent values (energy history). Colors come from --line-color / --fill-color on the element.</summary>
    public sealed class Sparkline
    {
        private static readonly CustomStyleProperty<Color> LineColor = new CustomStyleProperty<Color>("--line-color");
        private static readonly CustomStyleProperty<Color> FillColor = new CustomStyleProperty<Color>("--fill-color");

        private readonly VisualElement _el;
        private readonly List<float> _values = new List<float>();
        private readonly int _capacity;
        private Color _line = new Color(0.66f, 0.84f, 0.54f);
        private Color _fill = new Color(0.66f, 0.84f, 0.54f, 0.25f);
        private Vector2[] _pts = new Vector2[0];

        public Sparkline(VisualElement el, int capacity = 60)
        {
            _el = el;
            _capacity = capacity;
            el.generateVisualContent += Draw;
            el.RegisterCallback<CustomStyleResolvedEvent>(OnStyle);
        }

        /// <summary>Fixed vertical range; when min == max the range follows the data.</summary>
        public float Min { get; set; }

        public float Max { get; set; }

        public void Push(float v)
        {
            _values.Add(v);
            if (_values.Count > _capacity)
            {
                _values.RemoveAt(0);
            }

            _el.MarkDirtyRepaint();
        }

        private void OnStyle(CustomStyleResolvedEvent e)
        {
            if (e.customStyle.TryGetValue(LineColor, out Color l))
            {
                _line = l;
            }

            if (e.customStyle.TryGetValue(FillColor, out Color f))
            {
                _fill = f;
            }

            _el.MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext ctx)
        {
            int n = _values.Count;
            Rect r = _el.contentRect;
            if (n < 2 || r.width <= 0f)
            {
                return;
            }

            float lo = Min;
            float hi = Max;
            if (hi <= lo)
            {
                lo = float.MaxValue;
                hi = float.MinValue;
                foreach (float v in _values)
                {
                    lo = Mathf.Min(lo, v);
                    hi = Mathf.Max(hi, v);
                }

                float pad = Mathf.Max(1f, (hi - lo) * 0.15f);
                lo -= pad;
                hi += pad;
            }

            if (_pts.Length < n)
            {
                _pts = new Vector2[_capacity];
            }

            for (int i = 0; i < n; i++)
            {
                float x = r.xMin + (r.width * i / (_capacity - 1));
                float y = r.yMax - (r.height * Mathf.InverseLerp(lo, hi, _values[i]));
                _pts[i] = new Vector2(x, y);
            }

            Mesh2D.AreaUnder(ctx, _pts, n, r.yMax, _fill);
            Mesh2D.Polyline(ctx, _pts, n, 3f, _line);
        }
    }

    /// <summary>270-degree arc gauge (corruption, confidence). Colors from --track-color / --fill-color.</summary>
    public sealed class ArcGauge
    {
        private static readonly CustomStyleProperty<Color> TrackColor = new CustomStyleProperty<Color>("--track-color");
        private static readonly CustomStyleProperty<Color> FillColor = new CustomStyleProperty<Color>("--fill-color");

        private readonly VisualElement _el;
        private Color _track = new Color(0.66f, 0.84f, 0.54f, 0.3f);
        private Color _fill = new Color(0.66f, 0.84f, 0.54f);
        private float _value;
        private float _shown;

        public ArcGauge(VisualElement el)
        {
            _el = el;
            el.generateVisualContent += Draw;
            el.RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                if (e.customStyle.TryGetValue(TrackColor, out Color t))
                {
                    _track = t;
                }

                if (e.customStyle.TryGetValue(FillColor, out Color f))
                {
                    _fill = f;
                }

                _el.MarkDirtyRepaint();
            });
        }

        public void Set(float value01)
        {
            _value = Mathf.Clamp01(value01);
            if (Motion.Reduced)
            {
                _shown = _value;
            }

            _el.MarkDirtyRepaint();
        }

        public void Tick(float dt)
        {
            if (Mathf.Abs(_shown - _value) > 0.0005f)
            {
                _shown = Mathf.Lerp(_shown, _value, 1f - Mathf.Exp(-dt * 6f));
                _el.MarkDirtyRepaint();
            }
        }

        private void Draw(MeshGenerationContext ctx)
        {
            Rect r = _el.contentRect;
            float size = Mathf.Min(r.width, r.height);
            if (size <= 0f)
            {
                return;
            }

            var c = new Vector2(r.center.x, r.center.y);
            float radius = (size * 0.5f) - 8f;
            const float start = Mathf.PI * 0.75f;
            const float sweep = Mathf.PI * 1.5f;
            Mesh2D.Arc(ctx, c, radius, 8f, start, start + sweep, _track);
            Mesh2D.Arc(ctx, c, radius, 8f, start, start + (sweep * _shown), _fill);
        }
    }
}
