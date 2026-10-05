using System;
using System.Collections.Generic;
using System.Numerics;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Art.World
{
    /// <summary>One shape of the illustrated layer: a filled polygon or an open polyline, in picture coordinates (0..1, y down).</summary>
    public sealed class OverlayShape
    {
        public OverlayShape(bool fill, Vector4 color, float width)
        {
            Fill = fill;
            Color = color;
            Width = width;
        }

        public bool Fill { get; }

        /// <summary>UI color (sRGB 0..1) with alpha.</summary>
        public Vector4 Color { get; }

        /// <summary>Stroke width as a fraction of the picture height (lines only).</summary>
        public float Width { get; }

        public List<Vector2> Points { get; } = new List<Vector2>();
    }

    /// <summary>
    /// The illustrated intelligence layer over the sector map render (SPEC-033 rule 3): faction territory tints,
    /// route lines from the Hub, hatched fog of war over unscouted sites and the fallout front as a drifting haze,
    /// plus the anchors the UI hangs its site markers on. Built from the game state; drawn by Unity (Painter2D)
    /// and by the headless preview (canvas) from the same shapes. Lines arrive pre-dashed.
    /// </summary>
    public sealed class SectorOverlay
    {
        /// <summary>Faction tints, sRGB (Tokens.uss: amber, blue, magenta, white).</summary>
        public static readonly Vector3[] FactionTint =
        {
            new Vector3(0.910f, 0.706f, 0.361f),
            new Vector3(0.510f, 0.682f, 0.714f),
            new Vector3(0.757f, 0.463f, 0.706f),
            new Vector3(0.941f, 0.922f, 0.867f),
        };

        private static readonly Vector3 Phosphor = new Vector3(0.659f, 0.835f, 0.541f);
        private static readonly Vector3 Amber = new Vector3(0.910f, 0.706f, 0.361f);
        private static readonly Vector3 Ink = new Vector3(0.847f, 0.820f, 0.737f);
        private static readonly Vector3 Hazard = new Vector3(0.62f, 0.86f, 0.80f);
        private static readonly Vector3 Fallout = new Vector3(0.80f, 0.78f, 0.50f);

        private SectorOverlay(int sites)
        {
            SiteAnchors = new Vector2[sites];
            GroundAnchors = new Vector2[sites];
        }

        private int _hazeStart;
        private int _hazeCount;

        public List<OverlayShape> Shapes { get; } = new List<OverlayShape>();

        /// <summary>Where each site's marker floats (well above its landmark), picture coordinates.</summary>
        public Vector2[] SiteAnchors { get; }

        /// <summary>Where each site's leader line meets the ground.</summary>
        public Vector2[] GroundAnchors { get; }

        public Vector2 HubAnchor { get; private set; }

        /// <summary>The fallout front's anchor, or (-1, -1) when it has not settled.</summary>
        public Vector2 FalloutAnchor { get; private set; } = new Vector2(-1f, -1f);

        /// <summary>Builds the layer for one state, camera and picture aspect; <paramref name="time"/> (seconds) drifts the haze.</summary>
        public static SectorOverlay Build(GameState s, CameraPose cam, float aspect, uint seed, float time)
        {
            int count = WorldSystem.Sites.Count;
            var o = new SectorOverlay(count);
            Vector2 P(Vector3 w) => cam.Project(w, aspect);

            // territory: a loose tint around every faction holding, bigger for dug-in camps
            for (int i = 0; i < count; i++)
            {
                SiteDef d = WorldSystem.Sites[i];
                if (HazardSystem.Wild(d.Kind) || s.Sites[i].Outpost)
                {
                    continue;
                }

                float r = (d.Defense >= 100 ? 19f : 15f) + (s.Fortified[(int)d.Owner] * 1.5f);
                o.Blob(cam, aspect, SectorScene.SitePosition(i, seed), r, seed + (uint)i, new Vector4(FactionTint[(int)d.Owner], 0.13f));
            }

            // our ground: claimed ruins and held outposts carry the Hub's phosphor
            for (int i = 0; i < count; i++)
            {
                if (s.Sites[i].Outpost)
                {
                    o.Blob(cam, aspect, SectorScene.SitePosition(i, seed), 13f, seed + 900 + (uint)i, new Vector4(Phosphor, 0.16f));
                }
            }

            o.Blob(cam, aspect, new Vector3(0, SectorScene.Height(0, 0, seed), 0), 16f, seed + 77, new Vector4(Phosphor, 0.12f));

            // fog of war: what we have not scouted is hatched over
            for (int i = 0; i < count; i++)
            {
                if (!s.Sites[i].Scouted && !s.Sites[i].Outpost)
                {
                    o.Fog(cam, aspect, SectorScene.SitePosition(i, seed), 11f, seed + 300 + (uint)i);
                }
            }

            // routes: worn tracks from the Hub, solid while a team is on them
            var busy = new Dictionary<int, bool>();
            foreach (Operation op in s.Ops)
            {
                busy[op.Site] = op.ByAi;
            }

            for (int i = 0; i < count; i++)
            {
                SiteDef d = WorldSystem.Sites[i];
                bool active = busy.TryGetValue(i, out bool byAi);
                Vector3 color = active ? (byAi ? Amber : Phosphor) : HazardSystem.Wild(d.Kind) ? Hazard : Ink;
                o.Route(cam, aspect, seed, SectorScene.SitePosition(i, seed), new Vector4(color, active ? 0.9f : 0.32f), active ? 0.0042f : 0.0024f, !active);
            }

            // the fallout front: overlapping haze that turns slowly over its site
            o._hazeStart = o.Shapes.Count;
            o.AddHaze(s, cam, aspect, seed, time);
            o._hazeCount = o.Shapes.Count - o._hazeStart;

            for (int i = 0; i < count; i++)
            {
                Vector3 site = SectorScene.SitePosition(i, seed);
                o.SiteAnchors[i] = P(site + new Vector3(0, 14f, 0));
                o.GroundAnchors[i] = P(site + new Vector3(0, 1f, 0));
            }

            o.HubAnchor = P(new Vector3(0, SectorScene.Height(0, 0, seed) + 7f, 0));
            return o;
        }

        /// <summary>
        /// Greedy label placement (shared by the UI and the preview): each label tries right of its pin, then left,
        /// then the same one row lower, and takes the first spot that stays inside the picture and clear of labels
        /// and pins already placed. Sizes are picture fractions; returns 0 right, 1 left, 2 right-low, 3 left-low.
        /// </summary>
        public static int[] PlaceLabels(Vector2[] pins, float[] widths, float height, float pin)
        {
            int n = pins.Length;
            var sides = new int[n];
            var placed = new List<Vector4>();
            for (int i = 0; i < n; i++)
            {
                placed.Add(new Vector4(pins[i].X - pin, pins[i].Y - pin, pins[i].X + pin, pins[i].Y + pin));
            }

            // top to bottom, so labels settle the way the eye reads the map
            var order = new List<int>();
            for (int i = 0; i < n; i++)
            {
                order.Add(i);
            }

            order.Sort((x, y) => pins[x].Y.CompareTo(pins[y].Y) != 0 ? pins[x].Y.CompareTo(pins[y].Y) : x.CompareTo(y));
            foreach (int i in order)
            {
                int best = 0;
                float bestCost = float.MaxValue;
                for (int side = 0; side < 4; side++)
                {
                    Vector4 box = LabelBox(pins[i], widths[i], height, pin, side);
                    float cost = side * 0.01f;
                    if (box.X < 0f || box.Z > 1f || box.Y < 0f || box.W > 1f)
                    {
                        cost += 10f;
                    }

                    for (int k = 0; k < placed.Count; k++)
                    {
                        if (k != i)
                        {
                            cost += Overlap(box, placed[k]) * 1000f;
                        }
                    }

                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        best = side;
                    }
                }

                sides[i] = best;
                placed.Add(LabelBox(pins[i], widths[i], height, pin, best));
            }

            return sides;
        }

        /// <summary>The label rectangle (minX, minY, maxX, maxY) for a pin and a side.</summary>
        public static Vector4 LabelBox(Vector2 at, float width, float height, float pin, int side)
        {
            bool left = side % 2 == 1;
            float y = side >= 2 ? at.Y + (pin * 1.2f) : at.Y - (height * 0.5f);
            float x = left ? at.X - (pin * 1.6f) - width : at.X + (pin * 1.6f);
            return new Vector4(x, y, x + width, y + height);
        }

        private static float Overlap(Vector4 a, Vector4 b)
        {
            float w = Math.Min(a.Z, b.Z) - Math.Max(a.X, b.X);
            float h = Math.Min(a.W, b.W) - Math.Max(a.Y, b.Y);
            return w > 0f && h > 0f ? w * h : 0f;
        }

        /// <summary>
        /// Moves only the fallout haze to a new time (the rest of the layer is static until the state changes): cheap
        /// enough to call several times a second while the map is open.
        /// </summary>
        public void Redrift(GameState s, CameraPose cam, float aspect, uint seed, float time)
        {
            var rest = Shapes.GetRange(_hazeStart + _hazeCount, Shapes.Count - _hazeStart - _hazeCount);
            Shapes.RemoveRange(_hazeStart, Shapes.Count - _hazeStart);
            AddHaze(s, cam, aspect, seed, time);
            _hazeCount = Shapes.Count - _hazeStart;
            Shapes.AddRange(rest);
        }

        private void AddHaze(GameState s, CameraPose cam, float aspect, uint seed, float time)
        {
            if (s.FalloutSite < 0 || s.FalloutSite >= SiteAnchors.Length)
            {
                return;
            }

            Vector3 c = SectorScene.SitePosition(s.FalloutSite, seed);
            for (int k = 0; k < 7; k++)
            {
                float a = (k * 0.9f) + (time * 0.05f * (k % 2 == 0 ? 1f : -1f));
                float rr = 4f + (k * 1.6f);
                var at = c + new Vector3((float)Math.Cos(a) * rr, 0, (float)Math.Sin(a) * rr * 0.8f);
                Blob(cam, aspect, at, 9f + (k * 1.4f), seed + 500 + (uint)k, new Vector4(Fallout, 0.11f));
            }

            FalloutAnchor = cam.Project(c + new Vector3(0, 2f, -12f), aspect);
        }

        /// <summary>A hand-drawn blob: a ground circle with a wobbling edge, projected.</summary>
        private void Blob(CameraPose cam, float aspect, Vector3 center, float radius, uint seed, Vector4 color)
        {
            var shape = new OverlayShape(true, color, 0f);
            const int N = 28;
            for (int k = 0; k < N; k++)
            {
                float a = k * (float)Math.PI * 2f / N;
                float wob = 1f + ((Noise.Value(k * 0.6f, 0.5f, seed) - 0.5f) * 0.28f);
                var p = center + new Vector3((float)Math.Cos(a) * radius * wob, 0, (float)Math.Sin(a) * radius * wob);
                shape.Points.Add(cam.Project(p, aspect));
            }

            Shapes.Add(shape);
        }

        /// <summary>Fog of war: a dark wash with diagonal hatching clipped to its outline.</summary>
        private void Fog(CameraPose cam, float aspect, Vector3 center, float radius, uint seed)
        {
            int before = Shapes.Count;
            Blob(cam, aspect, center, radius, seed, new Vector4(0.05f, 0.06f, 0.055f, 0.34f));
            List<Vector2> outline = Shapes[before].Points;

            // hatch at 45 degrees on screen: work in pixel-like space (x scaled by aspect)
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            foreach (Vector2 p in outline)
            {
                minX = Math.Min(minX, p.X * aspect);
                maxX = Math.Max(maxX, p.X * aspect);
                minY = Math.Min(minY, p.Y);
                maxY = Math.Max(maxY, p.Y);
            }

            const float Gap = 0.011f;
            for (float c = minX - (maxY - minY); c < maxX; c += Gap)
            {
                // line x = c + (y - minY), clipped against the outline
                var hits = new List<float>();
                for (int k = 0; k < outline.Count; k++)
                {
                    Vector2 a = outline[k];
                    Vector2 b = outline[(k + 1) % outline.Count];
                    float ax = (a.X * aspect) - (a.Y - minY);
                    float bx = (b.X * aspect) - (b.Y - minY);
                    if ((ax - c) * (bx - c) < 0f)
                    {
                        float t = (c - ax) / (bx - ax);
                        hits.Add(a.Y + ((b.Y - a.Y) * t));
                    }
                }

                hits.Sort();
                for (int k = 0; k + 1 < hits.Count; k += 2)
                {
                    var line = new OverlayShape(false, new Vector4(Ink, 0.22f), 0.0016f);
                    line.Points.Add(new Vector2((c + (hits[k] - minY)) / aspect, hits[k]));
                    line.Points.Add(new Vector2((c + (hits[k + 1] - minY)) / aspect, hits[k + 1]));
                    Shapes.Add(line);
                }
            }
        }

        /// <summary>A track from the Hub to a site, following the ground with a slight wander; dashed unless in use.</summary>
        private void Route(CameraPose cam, float aspect, uint seed, Vector3 site, Vector4 color, float width, bool dashed)
        {
            const int N = 40;
            var pts = new List<Vector2>();
            for (int k = 0; k <= N; k++)
            {
                float t = k / (float)N;
                float x = site.X * t;
                float z = site.Z * t;
                var side = Vector3.Normalize(new Vector3(-site.Z, 0, site.X));
                float wob = (float)Math.Sin((t * 9f) + site.X) * 0.6f * (float)Math.Sin(t * Math.PI);
                var p = new Vector3(x, 0, z) + (side * wob);
                p.Y = SectorScene.Height(p.X, p.Z, seed) + 0.3f;
                pts.Add(cam.Project(p, aspect));
            }

            // leave the Hub's and the site's own ground clear
            int from = 4;
            int to = N - 3;
            if (!dashed)
            {
                var line = new OverlayShape(false, color, width);
                line.Points.AddRange(pts.GetRange(from, to - from + 1));
                Shapes.Add(line);
                return;
            }

            for (int k = from; k < to; k += 2)
            {
                var dash = new OverlayShape(false, color, width);
                dash.Points.Add(pts[k]);
                dash.Points.Add(pts[k + 1]);
                Shapes.Add(dash);
            }
        }
    }
}
