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

        /// <summary>Volumetric light cones (additive, no shadows): kept apart from <see cref="Mesh"/>.</summary>
        public MeshData Cones { get; } = new MeshData();

        public ArtRandom Random => _rng;

        /// <summary>Height (world, meters) over which ground contact darkening fades out.</summary>
        public float AoHeight { get; set; } = 1.2f;

        /// <summary>Darkest multiplier at ground contact.</summary>
        public float AoFloor { get; set; } = 0.58f;

        /// <summary>Per-face brightness jitter (+/-).</summary>
        public float FaceJitter { get; set; } = 0.06f;

        /// <summary>When set, the next Face uses these local normals per point (smooth curved surfaces).</summary>
        private Vector3[]? SmoothNormals { get; set; }

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

        /// <summary>Applies the current transform to a local point (for lights and pivots placed inside a Push).</summary>
        public Vector3 TransformPoint(Vector3 local)
        {
            return Vector3.Transform(local, _m);
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
        /// <paramref name="height"/>, with chamfered rims and flat caps. Segment counts are raised for larger radii so nothing reads as a faceted toy.
        /// </summary>
        public void Frustum(Vector3 baseCenter, float rBottom, float rTop, float height, int segments, Mat mat, float bevel = 0.04f, bool caps = true, Mat? capMat = null)
        {
            // round things are round (doc 11 anti-toy rules): faceting only survives on thin parts
            float rMax = Math.Max(rBottom, rTop);
            // (architecture wider than 1.5 m keeps its polygon: an octagonal emplacement is meant to be octagonal)
            if (mat != Mat.Foliage && mat != Mat.Rock && rMax < 1.5f)
            {
                segments = Math.Max(segments, rMax >= 0.25f ? 18 : (rMax >= 0.12f ? 12 : segments));
            }
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
                    if (segments >= 8)
                    {
                        float slope = (rings[k].r - rings[k + 1].r) / Math.Max(0.0001f, rings[k + 1].y - rings[k].y);
                        SmoothNormals = new[] { RadialNormal(a0, slope), RadialNormal(a1, slope), RadialNormal(a1, slope), RadialNormal(a0, slope) };
                    }

                    Face(sideCenter, mat, wear, p00, p01, p11, p10);
                    SmoothNormals = null;
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

            // Vertex color = masks for the salvage shader: R = baked AO/brightness (with per-face jitter), G = worn
            // edge, B = per-part variation (one value for every face of a primitive; picks its repaint color).
            float variation = _rng.Next();
            float jitter = 1f + ((variation - 0.5f) * 2f * FaceJitter);
            float part = PartVariation(wInside);
            float edge = tint > 1.05f ? 1f : 0f;
            float dark = tint > 1.05f ? 1f : tint;
            int first = Mesh.VertexCount;
            for (int i = 0; i < n; i++)
            {
                float y = w[i].Y + GroundOffset;
                float t = Math.Max(0f, Math.Min(1f, y / AoHeight));
                float ao = AoFloor + ((1f - AoFloor) * t * t * (3f - (2f * t)));
                Vector3 vn = SmoothNormals != null ? Vector3.Normalize(Vector3.TransformNormal(SmoothNormals[i], _m)) : normal;
                if (SmoothNormals != null && Vector3.Dot(vn, normal) < 0f)
                {
                    vn = -vn;
                }

                Mesh.AddVertex(w[i], vn, new Vector4(ao * jitter * dark, edge, part, 1f));
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

        /// <summary>A face with explicit local normals per corner (smooth curved surfaces).</summary>
        public void FaceSmooth(Vector3 inside, Mat mat, Vector3[] pts, Vector3[] normals)
        {
            SmoothNormals = normals;
            Face(inside, mat, 1f, pts);
            SmoothNormals = null;
        }

        /// <summary>Smooth UV sphere (ellipsoid with <paramref name="radii"/>).</summary>
        public void Sphere(Vector3 center, Vector3 radii, int lat, int lon, Mat mat, float latFrom = 0f, float latTo = 1f)
        {
            for (int i = 0; i < lat; i++)
            {
                float t0 = latFrom + ((latTo - latFrom) * i / lat);
                float t1 = latFrom + ((latTo - latFrom) * (i + 1) / lat);
                for (int j = 0; j < lon; j++)
                {
                    float a0 = (float)(Math.PI * 2 * j / lon);
                    float a1 = (float)(Math.PI * 2 * (j + 1) / lon);
                    Vector3 d00 = Dir(t0, a0), d01 = Dir(t0, a1), d11 = Dir(t1, a1), d10 = Dir(t1, a0);
                    FaceSmooth(center, mat, new[] { center + (d00 * radii), center + (d01 * radii), center + (d11 * radii), center + (d10 * radii) },
                        new[] { Vector3.Normalize(d00 / radii), Vector3.Normalize(d01 / radii), Vector3.Normalize(d11 / radii), Vector3.Normalize(d10 / radii) });
                }
            }

            static Vector3 Dir(float t, float a)
            {
                double phi = Math.PI * t;
                return new Vector3((float)(Math.Sin(phi) * Math.Cos(a)), (float)Math.Cos(phi), (float)(Math.Sin(phi) * Math.Sin(a)));
            }
        }

        /// <summary>Capsule between two points (limbs, pipes with round ends).</summary>
        public void Capsule(Vector3 a, Vector3 b, float radius, Mat mat, int segments = 10)
        {
            Vector3 d = b - a;
            float len = d.Length();
            Vector3 dir = d / Math.Max(0.0001f, len);
            Vector3 up = Math.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.95f ? Vector3.UnitX : Vector3.UnitY;
            Vector3 x = Vector3.Normalize(Vector3.Cross(up, dir));
            Vector3 y = Vector3.Cross(dir, x);
            var basis = new Matrix4x4(x.X, x.Y, x.Z, 0, dir.X, dir.Y, dir.Z, 0, y.X, y.Y, y.Z, 0, a.X, a.Y, a.Z, 1);
            Push(basis);
            Frustum(Vector3.Zero, radius, radius, len, segments, mat, 0f, false);
            Sphere(new Vector3(0, len, 0), new Vector3(radius), 3, segments, mat, 0f, 0.5f);
            Sphere(Vector3.Zero, new Vector3(radius), 3, segments, mat, 0.5f, 1f);
            Pop();
        }

        /// <summary>
        /// Corrugated sheet in the local XY plane (ribs vertical), facing -Z, spanning x0..x1 and y0..y1 at depth z.
        /// Trapezoid profile with real depth so the ribs read from the isometric camera.
        /// </summary>
        public void Corrugated(float x0, float x1, float y0, float y1, float z, Mat mat, float pitch = 0.28f, float depth = 0.05f)
        {
            int ribs = Math.Max(1, (int)Math.Round((x1 - x0) / pitch));
            float w = (x1 - x0) / ribs;
            var inside = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, z + 1f);
            for (int i = 0; i < ribs; i++)
            {
                float a = x0 + (i * w);
                float[] xs = { a, a + (w * 0.3f), a + (w * 0.42f), a + (w * 0.88f), a + w };
                float[] zs = { z, z, z - depth, z - depth, z };
                for (int k = 0; k < 4; k++)
                {
                    Face(inside, mat, k == 1 || k == 3 ? 1.1f : 1f,
                        new Vector3(xs[k], y0, zs[k]), new Vector3(xs[k + 1], y0, zs[k + 1]), new Vector3(xs[k + 1], y1, zs[k + 1]), new Vector3(xs[k], y1, zs[k]));
                }
            }
        }

        /// <summary>
        /// Soft volumetric light cone under a lamp (additive LightCone material). Vertex mask R fades from the
        /// lamp toward the ground.
        /// </summary>
        public void LightCone(Vector3 apex, float height, float radius, int segments = 16)
        {
            for (int i = 0; i < segments; i++)
            {
                float a0 = (float)(Math.PI * 2 * i / segments);
                float a1 = (float)(Math.PI * 2 * (i + 1) / segments);
                Vector3 p0 = Vector3.Transform(apex + new Vector3((float)Math.Cos(a0) * 0.12f, 0, (float)Math.Sin(a0) * 0.12f), _m);
                Vector3 p1 = Vector3.Transform(apex + new Vector3((float)Math.Cos(a1) * 0.12f, 0, (float)Math.Sin(a1) * 0.12f), _m);
                Vector3 q0 = Vector3.Transform(apex + new Vector3((float)Math.Cos(a0) * radius, -height, (float)Math.Sin(a0) * radius), _m);
                Vector3 q1 = Vector3.Transform(apex + new Vector3((float)Math.Cos(a1) * radius, -height, (float)Math.Sin(a1) * radius), _m);
                Vector3 n = Vector3.Normalize(new Vector3((float)Math.Cos(a0), 0.2f, (float)Math.Sin(a0)));
                int f = Cones.VertexCount;
                Cones.AddVertex(p0, n, new Vector4(1f, 0, 0, 1));
                Cones.AddVertex(p1, n, new Vector4(1f, 0, 0, 1));
                Cones.AddVertex(q1, n, new Vector4(0f, 0, 0, 1));
                Cones.AddVertex(q0, n, new Vector4(0f, 0, 0, 1));
                Cones.AddTriangle(Mat.LightCone, f, f + 1, f + 2);
                Cones.AddTriangle(Mat.LightCone, f, f + 2, f + 3);
                Cones.AddTriangle(Mat.LightCone, f, f + 2, f + 1);
                Cones.AddTriangle(Mat.LightCone, f, f + 3, f + 2);
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
                Mesh.AddVertex(w[i], normal, new Vector4(tints[i], 0f, 0.5f, 1f));
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

        /// <summary>Stable 0..1 value from a primitive's center (quantized to 5 cm), so all its faces share it.</summary>
        public static float PartVariation(Vector3 center)
        {
            unchecked
            {
                uint h = (uint)(int)Math.Round(center.X * 20f) * 73856093u;
                h ^= (uint)(int)Math.Round(center.Y * 20f) * 19349663u;
                h ^= (uint)(int)Math.Round(center.Z * 20f) * 83492791u;
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                return (h & 0xFFFF) / 65535f;
            }
        }

        private static Vector3 RadialNormal(float a, float slope)
        {
            return Vector3.Normalize(new Vector3((float)Math.Cos(a), slope, (float)Math.Sin(a)));
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
