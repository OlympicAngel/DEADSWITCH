using System;
using System.Collections.Generic;
using System.Numerics;

namespace Deadswitch.Art.Geometry
{
    /// <summary>
    /// Builds stylized meshes: chunky bevelled primitives with flat-shaded facets and vertex-color weathering
    /// (darker where things meet the ground, slight per-face variation, lighter worn bevels). A transform
    /// stack places parts; all faces orient themselves outward from the primitive's center.
    /// </summary>
    public sealed class MeshBuilder
    {
        private readonly Stack<Matrix4x4> _stack = new Stack<Matrix4x4>();
        private readonly ArtRandom _rng;
        private Matrix4x4 _m = Matrix4x4.Identity;

        public MeshBuilder(uint seed)
        {
            _rng = new ArtRandom(seed);
        }

        public MeshData Mesh { get; } = new MeshData();

        public ArtRandom Random => _rng;

        /// <summary>Height (world, meters) over which ground contact darkening fades out.</summary>
        public float AoHeight { get; set; } = 1.2f;

        /// <summary>Darkest multiplier at ground contact.</summary>
        public float AoFloor { get; set; } = 0.58f;

        /// <summary>Per-face brightness jitter (+/-).</summary>
        public float FaceJitter { get; set; } = 0.06f;

        /// <summary>World-space offset of this builder's origin (for AO when building parts in local space).</summary>
        public float GroundOffset { get; set; }

        public void Push(Matrix4x4 local)
        {
            _stack.Push(_m);
            _m = local * _m;
        }

        public void Push(Vector3 translate, float yawDegrees = 0f, float scale = 1f)
        {
            Push(Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationY(Deg(yawDegrees)) * Matrix4x4.CreateTranslation(translate));
        }

        public void Pop()
        {
            _m = _stack.Pop();
        }

        public static float Deg(float d)
        {
            return d * (float)Math.PI / 180f;
        }

        /// <summary>A box with chamfered edges (bevel in meters, clamped to fit).</summary>
        public void Box(Vector3 center, Vector3 size, Mat mat, float bevel = 0.06f)
        {
            Vector3 h = size * 0.5f;
            float b = Math.Min(bevel, Math.Min(h.X, Math.Min(h.Y, h.Z)) * 0.9f);
            if (b <= 0.001f)
            {
                PlainBox(center, h, mat);
                return;
            }

            // Corner points: for each corner sign, one point on each of the three adjacent faces.
            Vector3 Px(int sx, int sy, int sz) => center + new Vector3(sx * h.X, sy * (h.Y - b), sz * (h.Z - b));
            Vector3 Py(int sx, int sy, int sz) => center + new Vector3(sx * (h.X - b), sy * h.Y, sz * (h.Z - b));
            Vector3 Pz(int sx, int sy, int sz) => center + new Vector3(sx * (h.X - b), sy * (h.Y - b), sz * h.Z);

            int[] s = { -1, 1 };

            // Main faces.
            foreach (int sx in s)
            {
                Face(center, mat, 1f, Px(sx, -1, -1), Px(sx, -1, 1), Px(sx, 1, 1), Px(sx, 1, -1));
            }

            foreach (int sy in s)
            {
                Face(center, mat, 1f, Py(-1, sy, -1), Py(1, sy, -1), Py(1, sy, 1), Py(-1, sy, 1));
            }

            foreach (int sz in s)
            {
                Face(center, mat, 1f, Pz(-1, -1, sz), Pz(1, -1, sz), Pz(1, 1, sz), Pz(-1, 1, sz));
            }

            const float wear = 1.14f;

            // Edge chamfers.
            foreach (int sx in s)
            {
                foreach (int sy in s)
                {
                    Face(center, mat, wear, Px(sx, sy, -1), Px(sx, sy, 1), Py(sx, sy, 1), Py(sx, sy, -1));
                }

                foreach (int sz in s)
                {
                    Face(center, mat, wear, Px(sx, -1, sz), Px(sx, 1, sz), Pz(sx, 1, sz), Pz(sx, -1, sz));
                }
            }

            foreach (int sy in s)
            {
                foreach (int sz in s)
                {
                    Face(center, mat, wear, Py(-1, sy, sz), Py(1, sy, sz), Pz(1, sy, sz), Pz(-1, sy, sz));
                }
            }

            // Corner triangles.
            foreach (int sx in s)
            {
                foreach (int sy in s)
                {
                    foreach (int sz in s)
                    {
                        Face(center, mat, wear, Px(sx, sy, sz), Py(sx, sy, sz), Pz(sx, sy, sz));
                    }
                }
            }
        }

        /// <summary>Box resting on y = bottom (convenience for stacking parts).</summary>
        public void BoxOn(float x, float bottom, float z, float sx, float sy, float sz, Mat mat, float bevel = 0.06f)
        {
            Box(new Vector3(x, bottom + (sy * 0.5f), z), new Vector3(sx, sy, sz), mat, bevel);
        }

        /// <summary>
        /// A vertical frustum (cylinder when radii match) from <paramref name="baseCenter"/> up by
        /// <paramref name="height"/>, with chamfered rims and flat caps. Low segment counts read as stylized facets.
        /// </summary>
        public void Frustum(Vector3 baseCenter, float rBottom, float rTop, float height, int segments, Mat mat, float bevel = 0.04f, bool caps = true, Mat? capMat = null)
        {
            float b = Math.Min(bevel, Math.Min(height * 0.3f, Math.Min(rBottom, rTop) * 0.5f));
            var rings = new List<(float y, float r)>();
            if (b > 0.001f)
            {
                rings.Add((0f, rBottom - b));
                rings.Add((b, rBottom));
                rings.Add((height - b, rTop));
                rings.Add((height, rTop - b));
            }
            else
            {
                rings.Add((0f, rBottom));
                rings.Add((height, rTop));
            }

            Vector3 center = baseCenter + new Vector3(0, height * 0.5f, 0);
            for (int k = 0; k < rings.Count - 1; k++)
            {
                float wear = (b > 0.001f && k != 1) ? 1.12f : 1f;
                for (int i = 0; i < segments; i++)
                {
                    float a0 = (float)(Math.PI * 2 * i / segments);
                    float a1 = (float)(Math.PI * 2 * (i + 1) / segments);
                    Vector3 p00 = Ring(baseCenter, rings[k].r, rings[k].y, a0);
                    Vector3 p01 = Ring(baseCenter, rings[k].r, rings[k].y, a1);
                    Vector3 p11 = Ring(baseCenter, rings[k + 1].r, rings[k + 1].y, a1);
                    Vector3 p10 = Ring(baseCenter, rings[k + 1].r, rings[k + 1].y, a0);
                    Vector3 sideCenter = baseCenter + new Vector3(0, (rings[k].y + rings[k + 1].y) * 0.5f, 0);
                    Face(sideCenter, mat, wear, p00, p01, p11, p10);
                }
            }

            if (caps)
            {
                Mat cm = capMat ?? mat;
                var top = new Vector3[segments];
                var bottom = new Vector3[segments];
                (float yb, float rb) = rings[0];
                (float yt, float rt) = rings[rings.Count - 1];
                for (int i = 0; i < segments; i++)
                {
                    float a = (float)(Math.PI * 2 * i / segments);
                    bottom[i] = Ring(baseCenter, rb, yb, a);
                    top[i] = Ring(baseCenter, rt, yt, a);
                }

                Face(center, cm, 1f, top);
                Face(center, mat, 1f, bottom);
            }
        }

        /// <summary>Cylinder lying along X (pipes, tanks, barrels on their side).</summary>
        public void CylinderX(Vector3 center, float radius, float length, int segments, Mat mat, float bevel = 0.03f)
        {
            Push(Matrix4x4.CreateRotationZ(Deg(90)) * Matrix4x4.CreateTranslation(center));
            Frustum(new Vector3(0, -length * 0.5f, 0), radius, radius, length, segments, mat, bevel);
            Pop();
        }

        /// <summary>Cylinder lying along Z.</summary>
        public void CylinderZ(Vector3 center, float radius, float length, int segments, Mat mat, float bevel = 0.03f)
        {
            Push(Matrix4x4.CreateRotationX(Deg(90)) * Matrix4x4.CreateTranslation(center));
            Frustum(new Vector3(0, -length * 0.5f, 0), radius, radius, length, segments, mat, bevel);
            Pop();
        }

        /// <summary>A straight strut between two points (square section), for frames, scaffolds and masts.</summary>
        public void Strut(Vector3 a, Vector3 b, float thickness, Mat mat)
        {
            Vector3 d = b - a;
            float len = d.Length();
            if (len < 0.0001f)
            {
                return;
            }

            Vector3 dir = d / len;
            Vector3 up = Math.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.95f ? Vector3.UnitX : Vector3.UnitY;
            Vector3 x = Vector3.Normalize(Vector3.Cross(up, dir));
            Vector3 y = Vector3.Cross(dir, x);
            var basis = new Matrix4x4(
                x.X, x.Y, x.Z, 0,
                y.X, y.Y, y.Z, 0,
                dir.X, dir.Y, dir.Z, 0,
                a.X + (d.X * 0.5f), a.Y + (d.Y * 0.5f), a.Z + (d.Z * 0.5f), 1);
            Push(basis);
            Box(Vector3.Zero, new Vector3(thickness, thickness, len), mat, thickness * 0.2f);
            Pop();
        }

        /// <summary>
        /// One flat convex face. Points are in local space; <paramref name="inside"/> is any point inside the
        /// solid so the face can orient itself outward. <paramref name="tint"/> scales the weathering color.
        /// </summary>
        public void Face(Vector3 inside, Mat mat, float tint, params Vector3[] pts)
        {
            int n = pts.Length;
            var w = new Vector3[n];
            Vector3 centroid = Vector3.Zero;
            for (int i = 0; i < n; i++)
            {
                w[i] = Vector3.Transform(pts[i], _m);
                centroid += w[i];
            }

            centroid /= n;
            Vector3 wInside = Vector3.Transform(inside, _m);
            Vector3 normal = Vector3.Zero;
            for (int i = 1; i < n - 1; i++)
            {
                normal += Vector3.Cross(w[i] - w[0], w[i + 1] - w[0]);
            }

            if (normal.LengthSquared() < 1e-12f)
            {
                return;
            }

            normal = Vector3.Normalize(normal);
            bool flip = Vector3.Dot(normal, centroid - wInside) < 0f;
            if (flip)
            {
                normal = -normal;
            }

            float jitter = 1f + ((_rng.Next() - 0.5f) * 2f * FaceJitter);
            int first = Mesh.VertexCount;
            for (int i = 0; i < n; i++)
            {
                float y = w[i].Y + GroundOffset;
                float t = Math.Max(0f, Math.Min(1f, y / AoHeight));
                float ao = AoFloor + ((1f - AoFloor) * t * t * (3f - (2f * t)));
                float k = ao * jitter * tint;
                Mesh.AddVertex(w[i], normal, new Vector4(k, k, k, 1f));
            }

            for (int i = 1; i < n - 1; i++)
            {
                if (flip)
                {
                    Mesh.AddTriangle(mat, first, first + i + 1, first + i);
                }
                else
                {
                    Mesh.AddTriangle(mat, first, first + i, first + i + 1);
                }
            }
        }

        /// <summary>A flat face with an explicit color multiplier per corner (terrain), no AO or jitter.</summary>
        public void FaceTinted(Vector3 inside, Mat mat, Vector3[] pts, float[] tints)
        {
            var w = new Vector3[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                w[i] = Vector3.Transform(pts[i], _m);
            }

            Vector3 normal = Vector3.Normalize(Vector3.Cross(w[1] - w[0], w[2] - w[0]));
            Vector3 centroid = (w[0] + w[1] + w[2]) / 3f;
            bool flip = Vector3.Dot(normal, centroid - Vector3.Transform(inside, _m)) < 0f;
            if (flip)
            {
                normal = -normal;
            }

            int first = Mesh.VertexCount;
            for (int i = 0; i < w.Length; i++)
            {
                Mesh.AddVertex(w[i], normal, new Vector4(tints[i], tints[i], tints[i], 1f));
            }

            for (int i = 1; i < w.Length - 1; i++)
            {
                if (flip)
                {
                    Mesh.AddTriangle(mat, first, first + i + 1, first + i);
                }
                else
                {
                    Mesh.AddTriangle(mat, first, first + i, first + i + 1);
                }
            }
        }

        private static Vector3 Ring(Vector3 c, float r, float y, float a)
        {
            return c + new Vector3((float)Math.Cos(a) * r, y, (float)Math.Sin(a) * r);
        }

        private void PlainBox(Vector3 c, Vector3 h, Mat mat)
        {
            Vector3 P(int sx, int sy, int sz) => c + new Vector3(sx * h.X, sy * h.Y, sz * h.Z);
            Face(c, mat, 1f, P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1));
            Face(c, mat, 1f, P(1, -1, -1), P(1, -1, 1), P(1, 1, 1), P(1, 1, -1));
            Face(c, mat, 1f, P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1));
            Face(c, mat, 1f, P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1));
            Face(c, mat, 1f, P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1));
            Face(c, mat, 1f, P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1));
        }
    }
}
