using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Small immediate-mode helpers for UI Toolkit custom drawing (generateVisualContent): quads, feathered
    /// polylines, arcs. Triangles are emitted clockwise in UI space (y down). Feathering fades the edges to
    /// transparent for cheap anti-aliasing on phone screens.
    /// </summary>
    public static class Mesh2D
    {
        public const float Feather = 1.5f;

        public static void Quad(MeshGenerationContext ctx, Rect r, Color32 color)
        {
            MeshWriteData m = ctx.Allocate(4, 6);
            m.SetNextVertex(V(r.xMin, r.yMin, color));
            m.SetNextVertex(V(r.xMax, r.yMin, color));
            m.SetNextVertex(V(r.xMax, r.yMax, color));
            m.SetNextVertex(V(r.xMin, r.yMax, color));
            m.SetNextIndex(0);
            m.SetNextIndex(1);
            m.SetNextIndex(2);
            m.SetNextIndex(0);
            m.SetNextIndex(2);
            m.SetNextIndex(3);
        }

        /// <summary>Quad with a vertical gradient (top color to bottom color).</summary>
        public static void GradientQuad(MeshGenerationContext ctx, Rect r, Color32 top, Color32 bottom)
        {
            MeshWriteData m = ctx.Allocate(4, 6);
            m.SetNextVertex(V(r.xMin, r.yMin, top));
            m.SetNextVertex(V(r.xMax, r.yMin, top));
            m.SetNextVertex(V(r.xMax, r.yMax, bottom));
            m.SetNextVertex(V(r.xMin, r.yMax, bottom));
            m.SetNextIndex(0);
            m.SetNextIndex(1);
            m.SetNextIndex(2);
            m.SetNextIndex(0);
            m.SetNextIndex(2);
            m.SetNextIndex(3);
        }

        /// <summary>A polyline of the given width with feathered edges.</summary>
        public static void Polyline(MeshGenerationContext ctx, Vector2[] pts, int count, float width, Color32 color)
        {
            if (count < 2)
            {
                return;
            }

            Color32 clear = new Color32(color.r, color.g, color.b, 0);
            float half = width * 0.5f;

            // Per point: outer-left, inner-left, inner-right, outer-right.
            MeshWriteData m = ctx.Allocate(count * 4, (count - 1) * 36);
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = i == 0 ? pts[1] - pts[0] : (i == count - 1 ? pts[i] - pts[i - 1] : pts[i + 1] - pts[i - 1]);
                Vector2 n = new Vector2(-dir.y, dir.x).normalized;
                Vector2 p = pts[i];
                m.SetNextVertex(V(p + (n * (half + Feather)), clear));
                m.SetNextVertex(V(p + (n * half), color));
                m.SetNextVertex(V(p - (n * half), color));
                m.SetNextVertex(V(p - (n * (half + Feather)), clear));
            }

            for (int i = 0; i < count - 1; i++)
            {
                int a = i * 4;
                int b = a + 4;
                for (int k = 0; k < 3; k++)
                {
                    Strip(m, (ushort)(a + k), (ushort)(a + k + 1), (ushort)(b + k), (ushort)(b + k + 1));
                }
            }
        }

        /// <summary>Filled area under a polyline down to <paramref name="baseY"/>, fading from top color to transparent.</summary>
        public static void AreaUnder(MeshGenerationContext ctx, Vector2[] pts, int count, float baseY, Color32 color)
        {
            if (count < 2)
            {
                return;
            }

            Color32 clear = new Color32(color.r, color.g, color.b, 0);
            MeshWriteData m = ctx.Allocate(count * 2, (count - 1) * 12);
            for (int i = 0; i < count; i++)
            {
                m.SetNextVertex(V(pts[i], color));
                m.SetNextVertex(V(new Vector2(pts[i].x, baseY), clear));
            }

            for (int i = 0; i < count - 1; i++)
            {
                ushort a = (ushort)(i * 2);
                Strip(m, a, (ushort)(a + 1), (ushort)(a + 2), (ushort)(a + 3));
            }
        }

        /// <summary>Arc band between radii, from angle a0 to a1 (radians, clockwise on screen), feathered.</summary>
        public static void Arc(MeshGenerationContext ctx, Vector2 center, float radius, float thickness, float a0, float a1, Color32 color)
        {
            float sweep = a1 - a0;
            if (sweep <= 0.0001f)
            {
                return;
            }

            int segs = Mathf.Clamp(Mathf.CeilToInt(sweep * radius / 6f), 4, 128);
            Color32 clear = new Color32(color.r, color.g, color.b, 0);
            float rIn = radius - (thickness * 0.5f);
            float rOut = radius + (thickness * 0.5f);
            MeshWriteData m = ctx.Allocate((segs + 1) * 4, segs * 36);
            for (int i = 0; i <= segs; i++)
            {
                float a = a0 + (sweep * i / segs);
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                m.SetNextVertex(V(center + (d * (rOut + Feather)), clear));
                m.SetNextVertex(V(center + (d * rOut), color));
                m.SetNextVertex(V(center + (d * rIn), color));
                m.SetNextVertex(V(center + (d * (rIn - Feather)), clear));
            }

            for (int i = 0; i < segs; i++)
            {
                int a = i * 4;
                int b = a + 4;
                for (int k = 0; k < 3; k++)
                {
                    Strip(m, (ushort)(a + k), (ushort)(a + k + 1), (ushort)(b + k), (ushort)(b + k + 1));
                }
            }
        }

        /// <summary>Filled disc with a radial gradient (center color to rim color): glows and orb cores.</summary>
        public static void Disc(MeshGenerationContext ctx, Vector2 center, float radius, Color32 inner, Color32 rim)
        {
            if (radius <= 0.5f)
            {
                return;
            }

            int segs = Mathf.Clamp(Mathf.CeilToInt(radius), 16, 64);
            MeshWriteData m = ctx.Allocate(segs + 2, segs * 6);
            m.SetNextVertex(V(center, inner));
            for (int i = 0; i <= segs; i++)
            {
                float a = Mathf.PI * 2f * i / segs;
                m.SetNextVertex(V(center + (new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius), rim));
            }

            for (int i = 0; i < segs; i++)
            {
                m.SetNextIndex(0);
                m.SetNextIndex((ushort)(i + 1));
                m.SetNextIndex((ushort)(i + 2));
                m.SetNextIndex(0);
                m.SetNextIndex((ushort)(i + 2));
                m.SetNextIndex((ushort)(i + 1));
            }
        }

        public static Vertex V(Vector2 p, Color32 c)
        {
            return new Vertex { position = new Vector3(p.x, p.y, Vertex.nearZ), tint = c };
        }

        public static Vertex V(float x, float y, Color32 c)
        {
            return new Vertex { position = new Vector3(x, y, Vertex.nearZ), tint = c };
        }

        /// <summary>Two triangles for the quad (a0, a1) -> (b0, b1); both windings so orientation never culls it.</summary>
        private static void Strip(MeshWriteData m, ushort a0, ushort a1, ushort b0, ushort b1)
        {
            m.SetNextIndex(a0);
            m.SetNextIndex(b0);
            m.SetNextIndex(a1);
            m.SetNextIndex(a1);
            m.SetNextIndex(b0);
            m.SetNextIndex(b1);
            m.SetNextIndex(a0);
            m.SetNextIndex(a1);
            m.SetNextIndex(b0);
            m.SetNextIndex(a1);
            m.SetNextIndex(b1);
            m.SetNextIndex(b0);
        }
    }
}
