using System;
using System.Numerics;
using Deadswitch.Art.Geometry;

namespace Deadswitch.Art.Models
{
    /// <summary>Composite shapes used by several models.</summary>
    public static class Shapes
    {
        /// <summary>Half-cylinder roof along X (tents, quonset huts) sitting on y = 0.</summary>
        public static void ArchX(MeshBuilder b, Vector3 baseCenter, float radius, float length, int segments, Mat mat, Mat endMat)
        {
            Vector3 inside = baseCenter + new Vector3(0, radius * 0.4f, 0);
            float x0 = baseCenter.X - (length * 0.5f);
            float x1 = baseCenter.X + (length * 0.5f);
            var front = new Vector3[segments + 1];
            var back = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float a = (float)(Math.PI * i / segments);
                float y = baseCenter.Y + ((float)Math.Sin(a) * radius);
                float z = baseCenter.Z + ((float)Math.Cos(a) * radius);
                front[i] = new Vector3(x0, y, z);
                back[i] = new Vector3(x1, y, z);
            }

            for (int i = 0; i < segments; i++)
            {
                b.Face(inside, mat, i % 2 == 0 ? 1f : 0.93f, front[i], back[i], back[i + 1], front[i + 1]);
            }

            b.Face(inside, endMat, 1f, front);
            b.Face(inside, endMat, 1f, back);

            // Ribs
            for (int r = 0; r <= 3; r++)
            {
                float x = x0 + (length * r / 3f);
                for (int i = 0; i < segments; i++)
                {
                    Vector3 p0 = new Vector3(x, front[i].Y, front[i].Z) * 1f;
                    Vector3 p1 = new Vector3(x, front[i + 1].Y, front[i + 1].Z);
                    Vector3 o0 = baseCenter + ((p0 - baseCenter) * 1.03f);
                    Vector3 o1 = baseCenter + ((p1 - baseCenter) * 1.03f);
                    o0.X = x;
                    o1.X = x;
                    b.Strut(o0, o1, 0.07f, Mat.DarkSteel);
                }
            }
        }

        /// <summary>A ring of sandbags (two staggered layers) on y = 0.</summary>
        public static void SandbagRing(MeshBuilder b, Vector3 center, float radius, int count, int layers, float gapStartDeg = -1f, float gapDeg = 0f)
        {
            for (int layer = 0; layer < layers; layer++)
            {
                float offset = layer % 2 == 0 ? 0f : 0.5f;
                for (int i = 0; i < count; i++)
                {
                    float deg = ((i + offset) * 360f / count) - 90f;
                    if (gapStartDeg >= 0f && Within(deg, gapStartDeg, gapDeg))
                    {
                        continue;
                    }

                    float a = MeshBuilder.Deg(deg);
                    var p = center + new Vector3((float)Math.Cos(a) * radius, 0.13f + (layer * 0.24f), (float)Math.Sin(a) * radius);
                    float len = (float)(2 * Math.PI * radius / count) * 0.96f;
                    b.Push(Matrix4x4.CreateRotationY(-a + MeshBuilder.Deg(90)) * Matrix4x4.CreateTranslation(p));
                    b.Box(Vector3.Zero, new Vector3(len, 0.26f, 0.42f), Mat.Sandbag, 0.11f);
                    b.Pop();
                }
            }
        }

        /// <summary>Straight sandbag wall from a to b on y = 0.</summary>
        public static void SandbagWall(MeshBuilder b, Vector3 from, Vector3 to, int layers)
        {
            Vector3 d = to - from;
            float len = d.Length();
            int count = Math.Max(1, (int)(len / 0.62f));
            float yaw = (float)Math.Atan2(d.X, d.Z);
            for (int layer = 0; layer < layers; layer++)
            {
                for (int i = 0; i < count; i++)
                {
                    float t = (i + 0.5f + (layer % 2 == 0 ? 0f : 0.25f)) / count;
                    if (t > 1f)
                    {
                        continue;
                    }

                    Vector3 p = from + (d * t) + new Vector3(0, 0.13f + (layer * 0.24f), 0);
                    b.Push(Matrix4x4.CreateRotationY(yaw) * Matrix4x4.CreateTranslation(p));
                    b.Box(Vector3.Zero, new Vector3(0.44f, 0.26f, (len / count) * 0.96f), Mat.Sandbag, 0.11f);
                    b.Pop();
                }
            }
        }

        /// <summary>Small emissive lamp housing with a point light.</summary>
        public static void Lamp(MeshBuilder b, Model m, Vector3 p, Mat lamp, Vector3 color, float intensity, float range, LightRole role, float size = 0.12f)
        {
            b.Box(p, new Vector3(size, size, size), lamp, size * 0.25f);
            m.Lights.Add(new LightSpec(p, color, intensity, range, role));
        }

        /// <summary>Hazard striping as alternating thin plates on a vertical face at z (facing -Z).</summary>
        public static void Hazard(MeshBuilder b, float x0, float x1, float y0, float y1, float z, int stripes)
        {
            float w = (x1 - x0) / stripes;
            for (int i = 0; i < stripes; i++)
            {
                b.Box(new Vector3(x0 + (w * (i + 0.5f)), (y0 + y1) * 0.5f, z), new Vector3(w * 0.98f, y1 - y0, 0.03f), i % 2 == 0 ? Mat.Paint : Mat.DarkSteel, 0.005f);
            }
        }

        private static bool Within(float deg, float start, float span)
        {
            float d = ((deg - start) % 360f + 360f) % 360f;
            return d < span;
        }
    }
}
