using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// The war room of the opening film (SPEC-044): the AI's own terminal wall, the only flat layer of the world
    /// (doc 07 s7). On the command beat the screens wake one by one around the AI's eye; on the launch beat the wall
    /// goes red: silos arm, targets lock, arcs leave the map and the countdown runs out. Drawn in code
    /// (<see cref="Mesh2D"/>) in the room's colour (<c>--room-color</c>, <c>--room-dim</c>).
    /// </summary>
    public sealed class WarRoom
    {
        private static readonly CustomStyleProperty<Color> RoomColor = new CustomStyleProperty<Color>("--room-color");
        private static readonly CustomStyleProperty<Color> RoomDim = new CustomStyleProperty<Color>("--room-dim");

        private static readonly string[] LogLines =
        {
            "LINK NORAD-7 ............ OK",
            "LINK PACFLT .............. OK",
            "AUTH CHAIN 4/4 .......... VERIFIED",
            "HUMAN OVERRIDE .......... DISABLED",
            "SIM RUN 1 OF 40,000,000",
            "SIM RUN 40,000,000 ...... DONE",
            "WAR DURATION EST ........ 19 DAYS",
            "CASUALTY MODEL .......... IGNORED",
            "OBJECTIVE ............... END THE WAR",
            "OPTIMAL PATH ............ FOUND",
            "SILO GRID ............... ONLINE",
            "TARGET SET 1-212 ........ LOCKED",
        };

        private readonly VisualElement _root;
        private readonly List<Panel> _panels = new List<Panel>();
        private readonly Label _status;
        private readonly VisualElement _log;
        private readonly AiOrb _eye;
        private Color _color = new Color(0.27f, 0.86f, 0.9f);
        private Color _dim = new Color(0.27f, 0.86f, 0.9f, 0.3f);
        private float _t;
        private bool _red;
        private int _logShown;
        private float _nextLog;

        public WarRoom(VisualElement root)
        {
            _root = root;
            _root.RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                e.customStyle.TryGetValue(RoomColor, out _color);
                e.customStyle.TryGetValue(RoomDim, out _dim);
            });

            foreach (string name in new[] { "room-map", "room-silos", "room-radar", "room-targets" })
            {
                var p = new Panel { Element = root.Q(name) };
                VisualElement canvas = p.Element.Q(className: "room__canvas");
                p.Canvas = canvas;
                canvas.generateVisualContent += ctx => Draw(ctx, p);
                _panels.Add(p);
            }

            _log = root.Q("room-log-lines");
            _status = root.Q<Label>("room-status");
            _eye = new AiOrb(root.Q("room-eye"));
            _eye.Assemble();
        }

        /// <summary>Starts a beat: the wall wakes (command) or goes red (launch).</summary>
        public void Play(bool red)
        {
            _red = red;
            _t = 0f;
            _root.EnableInClassList("op-room--red", red);
            if (!red)
            {
                _logShown = 0;
                _log.Clear();
                foreach (Panel p in _panels)
                {
                    p.Woke = false;
                }
            }

            _eye.Stutter = red ? 0.4f : 0f;
            _eye.Ping();
        }

        public void Tick(float dt)
        {
            _t += dt;
            _eye.Voice = _red ? 0.8f : Mathf.Clamp01(_t - 1.2f) * 0.5f;
            _eye.Tick(dt);

            // the screens wake in turn with a flicker; on red they stay up and pulse with the klaxon
            for (int i = 0; i < _panels.Count; i++)
            {
                Panel p = _panels[i];
                float on = _red ? 1f : Mathf.Clamp01((_t - 0.2f - (i * 0.35f)) * 3f);
                if (!_red && on > 0f && !p.Woke)
                {
                    // each screen chirps as it wakes
                    p.Woke = true;
                    Audio.AudioDirector.Instance?.Opening(Audio.OpeningCue.Blip);
                }

                float flicker = on < 1f && on > 0f ? (Mathf.PerlinNoise(_t * 30f, i) > 0.4f ? 1f : 0.2f) : 1f;
                p.Element.style.opacity = on * flicker;
                p.Canvas.MarkDirtyRepaint();
            }

            _root.Q("room-log").style.opacity = _red ? 1f : Mathf.Clamp01((_t - 0.6f) * 3f);
            if (_t >= _nextLog && _logShown < LogLines.Length && _t > 0.6f)
            {
                _nextLog = _t + (_red ? 0.12f : 0.32f);
                var line = new Label(LogLines[_logShown]);
                line.AddToClassList("room__log-line");
                _log.Add(line);
                _logShown++;
                while (_log.childCount > 7)
                {
                    _log.RemoveAt(0);
                }
            }

            if (_red)
            {
                int left = Mathf.Max(0, 3 - Mathf.FloorToInt(_t * 1.1f));
                _status.text = left > 0 ? "LAUNCH IN 00:0" + left : "LAUNCH";
                _root.Q("room-alarm").style.opacity = 0.5f + (0.5f * Mathf.Sin(_t * 9f));
            }
            else
            {
                _status.text = _t < 2.2f ? "STANDBY" : "AUTONOMOUS";
                _root.Q("room-alarm").style.opacity = 0f;
            }
        }

        private void Draw(MeshGenerationContext ctx, Panel p)
        {
            Rect r = p.Canvas.contentRect;
            if (r.width <= 1f || r.height <= 1f)
            {
                return;
            }

            switch (p.Element.name)
            {
                case "room-map":
                    DrawMap(ctx, r);
                    break;
                case "room-silos":
                    DrawSilos(ctx, r);
                    break;
                case "room-radar":
                    DrawRadar(ctx, r);
                    break;
                case "room-targets":
                    DrawTargets(ctx, r);
                    break;
            }
        }

        /// <summary>A dotted world; on red, launch arcs leave the silos and cross it.</summary>
        private void DrawMap(MeshGenerationContext ctx, Rect r)
        {
            const int Cols = 64;
            const int Rows = 24;
            float cw = r.width / Cols;
            float ch = r.height / Rows;
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    float lat = (y / (float)Rows) - 0.5f;
                    float land = Mathf.PerlinNoise((x * 0.11f) + 3.1f, (y * 0.19f) + 1.7f) - (Mathf.Abs(lat) * 0.5f);
                    if (land < 0.42f)
                    {
                        continue;
                    }

                    Color c = _dim;
                    c.a *= 0.8f;
                    float s = Mathf.Min(cw, ch) * 0.35f;
                    Mesh2D.Quad(ctx, new Rect(r.x + (x * cw) + (cw * 0.5f) - s, r.y + (y * ch) + (ch * 0.5f) - s, s * 2f, s * 2f), c);
                }
            }

            if (!_red)
            {
                return;
            }

            var pts = new Vector2[24];
            for (int a = 0; a < 9; a++)
            {
                float start = a * 0.12f;
                float u = Mathf.Clamp01((_t - start) / 1.6f);
                if (u <= 0f)
                {
                    continue;
                }

                var from = new Vector2(r.x + (r.width * (0.15f + (0.08f * (a % 3)))), r.y + (r.height * (0.35f + (0.1f * (a % 2)))));
                var to = new Vector2(r.x + (r.width * (0.55f + (0.045f * a))), r.y + (r.height * (0.25f + (0.06f * (a % 5)))));
                int n = Mathf.Max(2, Mathf.RoundToInt(u * pts.Length));
                for (int i = 0; i < n; i++)
                {
                    float k = i / (float)(pts.Length - 1);
                    Vector2 p = Vector2.Lerp(from, to, k);
                    p.y -= Mathf.Sin(k * Mathf.PI) * r.height * 0.45f;
                    pts[i] = p;
                }

                Mesh2D.Polyline(ctx, pts, n, 3f, _color);
                if (u >= 1f)
                {
                    Mesh2D.Disc(ctx, to, 10f + (6f * Mathf.Sin(_t * 8f + a)), _color, new Color(_color.r, _color.g, _color.b, 0f));
                }
            }
        }

        /// <summary>Silo readiness: bars that idle low, then fill and arm on red.</summary>
        private void DrawSilos(MeshGenerationContext ctx, Rect r)
        {
            const int Bars = 8;
            float bw = r.width / Bars;
            for (int i = 0; i < Bars; i++)
            {
                float level = _red ? Mathf.Clamp01((_t * 1.6f) - (i * 0.12f)) : 0.15f + (0.1f * Mathf.PerlinNoise(_t * 0.8f, i));
                float h = r.height * level;
                Mesh2D.Quad(ctx, new Rect(r.x + (i * bw) + (bw * 0.2f), r.y, bw * 0.6f, r.height), new Color(_dim.r, _dim.g, _dim.b, _dim.a * 0.35f));
                Mesh2D.Quad(ctx, new Rect(r.x + (i * bw) + (bw * 0.2f), r.yMax - h, bw * 0.6f, h), level >= 1f ? _color : _dim);
            }
        }

        /// <summary>A radar sweep with a few contacts; on red the contacts multiply.</summary>
        private void DrawRadar(MeshGenerationContext ctx, Rect r)
        {
            Vector2 c = r.center;
            float rad = Mathf.Min(r.width, r.height) * 0.45f;
            for (int i = 1; i <= 3; i++)
            {
                Mesh2D.Arc(ctx, c, rad * i / 3f, 2f, 0f, Mathf.PI * 2f, _dim);
            }

            float sweep = _t * 2.2f;
            for (int k = 0; k < 6; k++)
            {
                Color trail = _color;
                trail.a *= 0.5f * (1f - (k / 6f));
                Mesh2D.Arc(ctx, c, rad * 0.5f, rad, sweep - (k * 0.09f) - 0.09f, sweep - (k * 0.09f), trail);
            }

            int contacts = _red ? 9 : 3;
            for (int i = 0; i < contacts; i++)
            {
                float a = i * 2.39f;
                float d = rad * (0.3f + (0.6f * Mathf.Repeat(i * 0.37f, 1f)));
                Mesh2D.Disc(ctx, c + (new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d), 5f, _color, new Color(_color.r, _color.g, _color.b, 0f));
            }
        }

        /// <summary>Target boxes that close in and lock (red only); idle crosshair otherwise.</summary>
        private void DrawTargets(MeshGenerationContext ctx, Rect r)
        {
            Mesh2D.Quad(ctx, new Rect(r.center.x - 1f, r.y, 2f, r.height), _dim);
            Mesh2D.Quad(ctx, new Rect(r.x, r.center.y - 1f, r.width, 2f), _dim);
            if (!_red)
            {
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                float u = Mathf.Clamp01((_t * 1.4f) - (i * 0.25f));
                var at = new Vector2(r.x + (r.width * (0.2f + (0.2f * i))), r.y + (r.height * (0.3f + (0.15f * (i % 3)))));
                float size = Mathf.Lerp(r.height * 0.5f, 18f, Ease.OutCubic(u));
                Box(ctx, new Rect(at.x - size, at.y - size, size * 2f, size * 2f), u >= 1f ? _color : _dim);
            }
        }

        private static void Box(MeshGenerationContext ctx, Rect b, Color c)
        {
            Mesh2D.Quad(ctx, new Rect(b.x, b.y, b.width, 2f), c);
            Mesh2D.Quad(ctx, new Rect(b.x, b.yMax - 2f, b.width, 2f), c);
            Mesh2D.Quad(ctx, new Rect(b.x, b.y, 2f, b.height), c);
            Mesh2D.Quad(ctx, new Rect(b.xMax - 2f, b.y, 2f, b.height), c);
        }

        private sealed class Panel
        {
            public VisualElement Element;
            public VisualElement Canvas;
            public bool Woke;
        }
    }
}
