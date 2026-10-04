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
            // laid bags are flat (0.15 m): more courses than the old layer count, staggered, slightly irregular
            int courses = (int)Math.Round(layers * 1.6f);
            int bags = Math.Max(count, (int)(2 * Math.PI * radius / 0.5f));
            for (int layer = 0; layer < courses; layer++)
            {
                float offset = layer % 2 == 0 ? 0f : 0.5f;
                float r = radius - (layer * 0.03f);
                for (int i = 0; i < bags; i++)
                {
                    float deg = ((i + offset) * 360f / bags) - 90f;
                    if (gapStartDeg >= 0f && Within(deg, gapStartDeg, gapDeg))
                    {
                        continue;
                    }

                    float a = MeshBuilder.Deg(deg);
                    var p = center + new Vector3((float)Math.Cos(a) * r, 0.075f + (layer * 0.145f), (float)Math.Sin(a) * r);
                    float len = (float)(2 * Math.PI * r / bags) * 1.02f;
                    Sandbag(b, p, -a + MeshBuilder.Deg(90), len, (uint)((layer * 131) + i));
                }
            }
        }

        /// <summary>Straight sandbag wall from a to b on y = 0.</summary>
        public static void SandbagWall(MeshBuilder b, Vector3 from, Vector3 to, int layers)
        {
            Vector3 d = to - from;
            float len = d.Length();
            int count = Math.Max(1, (int)(len / 0.5f));
            float yaw = (float)Math.Atan2(d.X, d.Z);
            int courses = (int)Math.Round(layers * 1.6f);
            for (int layer = 0; layer < courses; layer++)
            {
                for (int i = 0; i < count; i++)
                {
                    float t = (i + 0.5f + (layer % 2 == 0 ? 0f : 0.5f)) / count;
                    if (t > 1f)
                    {
                        continue;
                    }

                    Vector3 p = from + (d * t) + new Vector3(0, 0.075f + (layer * 0.145f), 0);
                    // bags lie across the wall line (headers): their length runs along it
                    Sandbag(b, p, yaw + MeshBuilder.Deg(90), (len / count) * 1.02f, (uint)((layer * 131) + i));
                }
            }
        }

        /// <summary>
        /// One filled sandbag lying on its side (along local X, length <paramref name="len"/>): a squashed,
        /// bevelled body with pinched, tied ends and a little sag and jitter so courses never look molded.
        /// </summary>
        public static void Sandbag(MeshBuilder b, Vector3 center, float yaw, float len, uint seed)
        {
            uint h = (seed * 2654435761u) ^ 0x9E3779B9u;
            float j1 = ((h & 0xFF) / 255f) - 0.5f;
            float j2 = (((h >> 8) & 0xFF) / 255f) - 0.5f;
            b.Push(Matrix4x4.CreateRotationZ(j1 * 0.06f) * Matrix4x4.CreateRotationY(yaw + (j2 * 0.08f)) * Matrix4x4.CreateTranslation(center));
            float body = len * 0.8f;
            b.Box(Vector3.Zero, new Vector3(body, 0.15f, 0.34f), Mat.Sandbag, 0.06f);
            b.Box(new Vector3((body * 0.5f) + (len * 0.05f), -0.01f, 0), new Vector3(len * 0.12f, 0.1f, 0.24f), Mat.Sandbag, 0.035f);
            b.Box(new Vector3((-body * 0.5f) - (len * 0.05f), -0.01f, 0), new Vector3(len * 0.12f, 0.1f, 0.24f), Mat.Sandbag, 0.035f);
            b.Pop();
        }

        /// <summary>Small emissive lamp housing with a point light.</summary>
        public static void Lamp(MeshBuilder b, Model m, Vector3 p, Mat lamp, Vector3 color, float intensity, float range, LightRole role, float size = 0.12f)
        {
            b.Box(p, new Vector3(size, size, size), lamp, size * 0.25f);
            m.Lights.Add(new LightSpec(b.TransformPoint(p), color, intensity, range, role));
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
