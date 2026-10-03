using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Procedural line icons (no texture assets). Elements with class <c>ds-icon ds-icon--name</c> draw the named
    /// glyph in --icon-color. Geometry is in a 0..1 box; tools/uipreview mirrors the same point lists.
    /// </summary>
    public static class Icons
    {
        private static readonly CustomStyleProperty<Color> IconColor = new CustomStyleProperty<Color>("--icon-color");

        private static readonly Dictionary<string, Vector2[][]> Glyphs = new Dictionary<string, Vector2[][]>
        {
            ["base"] = new[]
            {
                P(0.1f, 0.9f, 0.1f, 0.45f, 0.5f, 0.15f, 0.9f, 0.45f, 0.9f, 0.9f, 0.1f, 0.9f),
                P(0.4f, 0.9f, 0.4f, 0.62f, 0.6f, 0.62f, 0.6f, 0.9f),
            },
            ["map"] = new[]
            {
                Polygon(6, 0.42f, 0f),
                P(0.44f, 0.44f, 0.56f, 0.44f, 0.56f, 0.56f, 0.44f, 0.56f, 0.44f, 0.44f),
            },
            ["core"] = new[]
            {
                Polygon(32, 0.4f, 0f),
                P(0.5f, 0.3f, 0.7f, 0.5f, 0.5f, 0.7f, 0.3f, 0.5f, 0.5f, 0.3f),
            },
            ["ops"] = new[]
            {
                Polygon(28, 0.3f, 0f),
                P(0.5f, 0.02f, 0.5f, 0.25f),
                P(0.5f, 0.75f, 0.5f, 0.98f),
                P(0.02f, 0.5f, 0.25f, 0.5f),
                P(0.75f, 0.5f, 0.98f, 0.5f),
            },
        };

        /// <summary>Attaches drawing to every <c>.ds-icon</c> under <paramref name="root"/>.</summary>
        public static void Attach(VisualElement root)
        {
            root.Query(className: "ds-icon").ForEach(Attach1);
        }

        private static void Attach1(VisualElement el)
        {
            string glyph = null;
            foreach (string c in el.GetClasses())
            {
                if (c.StartsWith("ds-icon--", System.StringComparison.Ordinal))
                {
                    glyph = c.Substring("ds-icon--".Length);
                }
            }

            if (glyph == null || !Glyphs.TryGetValue(glyph, out Vector2[][] lines))
            {
                return;
            }

            Color color = Color.white;
            el.RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                if (e.customStyle.TryGetValue(IconColor, out Color c))
                {
                    color = c;
                    el.MarkDirtyRepaint();
                }
            });
            el.generateVisualContent += ctx =>
            {
                Rect r = el.contentRect;
                float s = Mathf.Min(r.width, r.height);
                var o = new Vector2(r.center.x - (s / 2f), r.center.y - (s / 2f));
                foreach (Vector2[] line in lines)
                {
                    var pts = new Vector2[line.Length];
                    for (int i = 0; i < line.Length; i++)
                    {
                        pts[i] = o + (line[i] * s);
                    }

                    Mesh2D.Polyline(ctx, pts, pts.Length, 3f, color);
                }
            };
        }

        private static Vector2[] P(params float[] xy)
        {
            var pts = new Vector2[xy.Length / 2];
            for (int i = 0; i < pts.Length; i++)
            {
                pts[i] = new Vector2(xy[i * 2], xy[(i * 2) + 1]);
            }

            return pts;
        }

        private static Vector2[] Polygon(int sides, float radius, float phase)
        {
            var pts = new Vector2[sides + 1];
            for (int i = 0; i <= sides; i++)
            {
                float a = phase + (Mathf.PI * 2f * i / sides);
                pts[i] = new Vector2(0.5f + (Mathf.Cos(a) * radius), 0.5f + (Mathf.Sin(a) * radius));
            }

            return pts;
        }
    }
}
