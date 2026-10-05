using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Procedural line icons (no texture assets). Elements with class <c>ds-icon ds-icon--name</c> draw the named
    /// glyph in --icon-color. Glyphs live in <c>Resources/UI/Icons.json</c> (0..1 box, y down), shared with
    /// tools/uipreview so previews draw the same set. A line is a point list "x y x y ..."; "o n r cx cy" is an
    /// n-sided polygon; "a r cx cy deg0 deg1 n" is an arc.
    /// </summary>
    public static class Icons
    {
        private static readonly CustomStyleProperty<Color> IconColor = new CustomStyleProperty<Color>("--icon-color");
        private static Dictionary<string, Vector2[][]> _glyphs;

        private static Dictionary<string, Vector2[][]> Glyphs => _glyphs ??= Load();

        /// <summary>Attaches drawing to every <c>.ds-icon</c> under <paramref name="root"/>.</summary>
        public static void Attach(VisualElement root)
        {
            root.Query(className: "ds-icon").ForEach(Attach1);
        }

        /// <summary>A new icon element for code-built UI.</summary>
        public static VisualElement Create(string glyph, params string[] classes)
        {
            var el = new VisualElement { pickingMode = PickingMode.Ignore };
            el.AddToClassList("ds-icon");
            el.AddToClassList("ds-icon--" + glyph);
            foreach (string c in classes)
            {
                if (!string.IsNullOrEmpty(c))
                {
                    el.AddToClassList(c);
                }
            }

            Attach1(el);
            return el;
        }

        /// <summary>Swaps the glyph an attached icon draws.</summary>
        public static void SetGlyph(VisualElement el, string glyph)
        {
            if (el?.userData is IconState state && state.Glyph != glyph)
            {
                el.RemoveFromClassList("ds-icon--" + state.Glyph);
                el.AddToClassList("ds-icon--" + glyph);
                state.Glyph = glyph;
                el.MarkDirtyRepaint();
            }
        }

        /// <summary>Glyph name for a facility kind.</summary>
        public static string ForFacility(Deadswitch.Sim.State.FacilityKind kind)
        {
            switch (kind)
            {
                case Deadswitch.Sim.State.FacilityKind.Generator: return "bolt";
                case Deadswitch.Sim.State.FacilityKind.ServerRack: return "server";
                case Deadswitch.Sim.State.FacilityKind.LifeSupport: return "cross";
                case Deadswitch.Sim.State.FacilityKind.BatteryBank: return "battery";
                case Deadswitch.Sim.State.FacilityKind.Turret: return "turret";
                case Deadswitch.Sim.State.FacilityKind.DroneBay: return "drone";
                case Deadswitch.Sim.State.FacilityKind.MotorPool: return "truck";
                case Deadswitch.Sim.State.FacilityKind.SolarField: return "sun";
                case Deadswitch.Sim.State.FacilityKind.FuelDepot: return "fuel";
                case Deadswitch.Sim.State.FacilityKind.CoolingTower: return "tower";
                case Deadswitch.Sim.State.FacilityKind.MemoryChamber: return "memory";
                case Deadswitch.Sim.State.FacilityKind.Reactor: return "reactor";
                default: return "base";
            }
        }

        private static void Attach1(VisualElement el)
        {
            if (el.userData is IconState)
            {
                return;
            }

            string glyph = null;
            foreach (string c in el.GetClasses())
            {
                if (c.StartsWith("ds-icon--", System.StringComparison.Ordinal))
                {
                    glyph = c.Substring("ds-icon--".Length);
                }
            }

            var state = new IconState { Glyph = glyph, Color = Color.white };
            el.userData = state;
            el.RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                if (e.customStyle.TryGetValue(IconColor, out Color c))
                {
                    state.Color = c;
                    el.MarkDirtyRepaint();
                }
            });
            el.generateVisualContent += ctx => Draw(ctx, el, state);
        }

        private static void Draw(MeshGenerationContext ctx, VisualElement el, IconState state)
        {
            if (state.Glyph == null || !Glyphs.TryGetValue(state.Glyph, out Vector2[][] lines))
            {
                return;
            }

            Rect r = el.contentRect;
            float s = Mathf.Min(r.width, r.height);
            if (s <= 0f)
            {
                return;
            }

            var o = new Vector2(r.center.x - (s / 2f), r.center.y - (s / 2f));
            float width = Mathf.Clamp(s * 0.07f, 2f, 5f);
            foreach (Vector2[] line in lines)
            {
                var pts = new Vector2[line.Length];
                for (int i = 0; i < line.Length; i++)
                {
                    pts[i] = o + (line[i] * s);
                }

                Mesh2D.Polyline(ctx, pts, pts.Length, width, state.Color);
            }
        }

        private static Dictionary<string, Vector2[][]> Load()
        {
            var map = new Dictionary<string, Vector2[][]>();
            var asset = Resources.Load<TextAsset>("UI/Icons");
            if (asset == null)
            {
                Debug.LogError("Icons: Resources/UI/Icons.json is missing");
                return map;
            }

            GlyphFile file = JsonUtility.FromJson<GlyphFile>(asset.text);
            foreach (GlyphDef def in file.glyphs)
            {
                var lines = new Vector2[def.lines.Length][];
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = ParseLine(def.lines[i]);
                }

                map[def.name] = lines;
            }

            return map;
        }

        private static Vector2[] ParseLine(string src)
        {
            string[] t = src.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (t[0] == "o")
            {
                return Arc(F(t[2]), F(t[3]), F(t[4]), 0f, 360f, (int)F(t[1]));
            }

            if (t[0] == "a")
            {
                return Arc(F(t[1]), F(t[2]), F(t[3]), F(t[4]), F(t[5]), (int)F(t[6]));
            }

            var pts = new Vector2[t.Length / 2];
            for (int i = 0; i < pts.Length; i++)
            {
                pts[i] = new Vector2(F(t[i * 2]), F(t[(i * 2) + 1]));
            }

            return pts;
        }

        private static Vector2[] Arc(float radius, float cx, float cy, float deg0, float deg1, int segments)
        {
            var pts = new Vector2[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(deg0, deg1, i / (float)segments) * Mathf.Deg2Rad;
                pts[i] = new Vector2(cx + (Mathf.Cos(a) * radius), cy + (Mathf.Sin(a) * radius));
            }

            return pts;
        }

        private static float F(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

        private sealed class IconState
        {
            public string Glyph;
            public Color Color;
        }

        [System.Serializable]
        private sealed class GlyphFile
        {
            public GlyphDef[] glyphs = new GlyphDef[0];
        }

        [System.Serializable]
        private sealed class GlyphDef
        {
            public string name = string.Empty;
            public string[] lines = new string[0];
        }
    }
}
