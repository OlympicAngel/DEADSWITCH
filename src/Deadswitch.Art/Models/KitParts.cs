using System;
using System.Numerics;
using Deadswitch.Art.Geometry;

namespace Deadswitch.Art.Models
{
    /// <summary>
    /// Structural and mechanical parts for the environment kit (docs/agents/environment-art.md). Every method
    /// builds medium-frequency, real-proportioned geometry into the given builder at its current transform.
    /// </summary>
    public static class KitParts
    {
        /// <summary>H-profile steel beam between two points (flanges perpendicular to <paramref name="up"/>).</summary>
        public static void IBeam(MeshBuilder b, Vector3 a, Vector3 c, float depth, float width, Mat mat)
        {
            Basis(b, a, c, out float len);
            b.Box(new Vector3(0, depth * 0.5f, len * 0.5f), new Vector3(width, depth * 0.12f, len), mat, 0.01f);
            b.Box(new Vector3(0, -depth * 0.5f, len * 0.5f), new Vector3(width, depth * 0.12f, len), mat, 0.01f);
            b.Box(new Vector3(0, 0, len * 0.5f), new Vector3(width * 0.12f, depth, len), mat, 0.005f);
            b.Pop();
        }

        /// <summary>Flat lattice truss between two points (two chords and zig-zag diagonals).</summary>
        public static void Truss(MeshBuilder b, Vector3 a, Vector3 c, float depth, float bar, Mat mat)
        {
            Vector3 up = new Vector3(0, depth, 0);
            b.Strut(a, c, bar, mat);
            b.Strut(a + up, c + up, bar, mat);
            int n = Math.Max(2, (int)(Vector3.Distance(a, c) / Math.Max(0.4f, depth)));
            for (int i = 0; i < n; i++)
            {
                Vector3 p0 = Vector3.Lerp(a, c, i / (float)n);
                Vector3 p1 = Vector3.Lerp(a, c, (i + 1) / (float)n);
                b.Strut(i % 2 == 0 ? p0 : p0 + up, i % 2 == 0 ? p1 + up : p1, bar * 0.7f, mat);
                b.Strut(p0, p0 + up, bar * 0.7f, mat);
            }

            b.Strut(c, c + up, bar * 0.7f, mat);
        }

        /// <summary>Steel ladder from a bottom point straight up, rungs every 0.3 m, optional safety cage.</summary>
        public static void Ladder(MeshBuilder b, Vector3 bottom, float height, float yawDeg, bool cage = false)
        {
            b.Push(bottom, yawDeg);
            b.Strut(new Vector3(-0.22f, 0, 0), new Vector3(-0.22f, height + 0.9f, 0), 0.05f, Mat.DarkSteel);
            b.Strut(new Vector3(0.22f, 0, 0), new Vector3(0.22f, height + 0.9f, 0), 0.05f, Mat.DarkSteel);
            for (float y = 0.3f; y < height; y += 0.3f)
            {
                b.Strut(new Vector3(-0.22f, y, 0), new Vector3(0.22f, y, 0), 0.03f, Mat.DarkSteel);
            }

            if (cage)
            {
                for (float y = 2.2f; y < height; y += 0.9f)
                {
                    b.Strut(new Vector3(-0.35f, y, 0), new Vector3(-0.35f, y, -0.6f), 0.025f, Mat.DarkSteel);
                    b.Strut(new Vector3(0.35f, y, 0), new Vector3(0.35f, y, -0.6f), 0.025f, Mat.DarkSteel);
                    b.Strut(new Vector3(-0.35f, y, -0.6f), new Vector3(0.35f, y, -0.6f), 0.025f, Mat.DarkSteel);
                }
            }

            b.Pop();
        }

        /// <summary>A cable hanging between two points (parabolic sag).</summary>
        public static void Cable(MeshBuilder b, Vector3 a, Vector3 c, float sag, float thickness = 0.03f, Mat mat = Mat.Rubber)
        {
            const int segs = 10;
            Vector3 prev = a;
            for (int i = 1; i <= segs; i++)
            {
                float t = i / (float)segs;
                Vector3 p = Vector3.Lerp(a, c, t) - new Vector3(0, sag * 4f * t * (1 - t), 0);
                b.Strut(prev, p, thickness, mat);
                prev = p;
            }
        }

        /// <summary>Pipe following a polyline, with flanges at every joint and support saddles.</summary>
        public static void Pipe(MeshBuilder b, Vector3[] pts, float radius, Mat mat)
        {
            for (int i = 0; i < pts.Length - 1; i++)
            {
                b.Capsule(pts[i], pts[i + 1], radius, mat, 10);
            }

            for (int i = 0; i < pts.Length; i++)
            {
                Vector3 dir = Vector3.Normalize(i < pts.Length - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1]);
                b.Capsule(pts[i] - (dir * 0.04f), pts[i] + (dir * 0.04f), radius * 1.45f, Mat.DarkSteel, 10);
            }
        }

        /// <summary>Louvered vent box with a rain hood, facing -Z.</summary>
        public static void Vent(MeshBuilder b, Vector3 baseCenter, float w, float h)
        {
            b.BoxOn(baseCenter.X, baseCenter.Y, baseCenter.Z, w, h, 0.35f, Mat.DarkSteel, 0.03f);
            for (float y = 0.12f; y < h - 0.1f; y += 0.12f)
            {
                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-30f)) * Matrix4x4.CreateTranslation(baseCenter + new Vector3(0, y, -0.2f)));
                b.Box(Vector3.Zero, new Vector3(w * 0.88f, 0.02f, 0.12f), Mat.DarkSteel, 0f);
                b.Pop();
            }

            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-20f)) * Matrix4x4.CreateTranslation(baseCenter + new Vector3(0, h + 0.05f, -0.1f)));
            b.Box(Vector3.Zero, new Vector3(w + 0.12f, 0.03f, 0.6f), Mat.DarkSteel, 0.01f);
            b.Pop();
        }

        /// <summary>Wall-mounted utility box with a conduit running down, facing -Z.</summary>
        public static void UtilityBox(MeshBuilder b, Vector3 center, Mat mat = Mat.SandSteel)
        {
            b.Box(center, new Vector3(0.5f, 0.65f, 0.22f), mat, 0.03f);
            b.Box(center + new Vector3(0, 0, -0.115f), new Vector3(0.44f, 0.6f, 0.01f), Mat.DarkSteel, 0f);
            b.Box(center + new Vector3(0.12f, 0.05f, -0.13f), new Vector3(0.04f, 0.1f, 0.03f), Mat.DarkSteel, 0f);
            b.Capsule(center + new Vector3(-0.12f, -0.33f, 0), new Vector3(center.X - 0.12f, 0.1f, center.Z), 0.03f, Mat.DarkSteel, 6);
        }

        /// <summary>Vertical storage tank with hoop bands, a top hatch and a side ladder.</summary>
        public static void TankV(MeshBuilder b, Vector3 baseCenter, float radius, float height, Mat mat, uint seed)
        {
            b.Frustum(baseCenter, radius, radius, height, 18, mat, 0.04f);
            b.Frustum(baseCenter + new Vector3(0, height, 0), radius, radius * 0.3f, radius * 0.35f, 18, mat, 0.02f);
            for (int i = 1; i < 4; i++)
            {
                b.Frustum(baseCenter + new Vector3(0, height * i / 4f, 0), radius + 0.03f, radius + 0.03f, 0.08f, 18, Mat.DarkSteel, 0.01f, false);
            }

            b.Frustum(baseCenter + new Vector3(0, height + (radius * 0.35f), 0), radius * 0.3f, radius * 0.3f, 0.12f, 12, Mat.DarkSteel, 0.02f);
            float a = new ArtRandom(seed).Range(0f, 6.28f);
            Vector3 lp = baseCenter + new Vector3((float)Math.Cos(a) * (radius + 0.1f), 0, (float)Math.Sin(a) * (radius + 0.1f));
            Ladder(b, lp, height, -a * 180f / (float)Math.PI + 90f);
        }

        /// <summary>Horizontal tank on saddles (along X).</summary>
        public static void TankH(MeshBuilder b, Vector3 center, float radius, float length, Mat mat)
        {
            b.CylinderX(center, radius, length, 18, mat, 0.06f);
            foreach (float x in new[] { -length * 0.32f, length * 0.32f })
            {
                b.BoxOn(center.X + x, center.Y - radius - 0.3f, center.Z, 0.25f, radius + 0.15f, radius * 1.6f, Mat.DarkSteel, 0.02f);
                b.CylinderX(center + new Vector3(x, 0, 0), radius + 0.025f, 0.1f, 18, Mat.DarkSteel, 0.01f);
            }

            b.Frustum(center + new Vector3(length * 0.2f, radius - 0.05f, 0), 0.18f, 0.16f, 0.2f, 10, Mat.DarkSteel, 0.02f);
        }

        /// <summary>Pad-mounted transformer: finned tank, three bushings, cable stubs.</summary>
        public static void Transformer(MeshBuilder b, Vector3 baseCenter, float scale = 1f)
        {
            float w = 1.1f * scale;
            float h = 1.3f * scale;
            float d = 0.8f * scale;
            b.BoxOn(baseCenter.X, baseCenter.Y, baseCenter.Z, w + 0.3f, 0.2f, d + 0.3f, Mat.Concrete, 0.03f);
            b.BoxOn(baseCenter.X, baseCenter.Y + 0.2f, baseCenter.Z, w, h, d, Mat.OliveSteel, 0.04f);
            for (int i = 0; i < 7; i++)
            {
                float x = baseCenter.X - (w * 0.42f) + (i * w * 0.14f);
                b.Box(new Vector3(x, baseCenter.Y + 0.2f + (h * 0.45f), baseCenter.Z - (d * 0.5f) - 0.08f), new Vector3(0.03f, h * 0.7f, 0.16f), Mat.OliveSteel, 0f);
                b.Box(new Vector3(x, baseCenter.Y + 0.2f + (h * 0.45f), baseCenter.Z + (d * 0.5f) + 0.08f), new Vector3(0.03f, h * 0.7f, 0.16f), Mat.OliveSteel, 0f);
            }

            for (int i = 0; i < 3; i++)
            {
                Vector3 p = new Vector3(baseCenter.X - (w * 0.3f) + (i * w * 0.3f), baseCenter.Y + 0.2f + h, baseCenter.Z);
                for (int k = 0; k < 4; k++)
                {
                    b.Frustum(p + new Vector3(0, k * 0.1f, 0), 0.09f * scale, 0.06f * scale, 0.08f, 10, Mat.PaintWhite, 0.01f);
                }

                b.Box(p + new Vector3(0, 0.45f, 0), new Vector3(0.06f, 0.06f, 0.06f), Mat.Copper, 0.01f);
            }

            Shapes.Hazard(b, baseCenter.X - (w * 0.4f), baseCenter.X + (w * 0.4f), baseCenter.Y + 0.4f, baseCenter.Y + 0.55f, baseCenter.Z - (d * 0.5f) - 0.01f, 6);
        }

        /// <summary>Diesel genset on a skid: engine, radiator with grille, exhaust and muffler, control panel.</summary>
        public static void Genset(MeshBuilder b, Vector3 baseCenter, float yawDeg)
        {
            b.Push(baseCenter, yawDeg);
            b.BoxOn(0, 0, 0, 2.6f, 0.16f, 1.1f, Mat.DarkSteel, 0.02f);
            b.BoxOn(-0.3f, 0.16f, 0, 1.4f, 0.9f, 0.8f, Mat.Rust, 0.06f);
            b.BoxOn(0.75f, 0.16f, 0, 0.5f, 1.1f, 1.0f, Mat.OliveSteel, 0.04f);
            for (int i = 0; i < 6; i++)
            {
                b.Box(new Vector3(1.01f, 0.32f + (i * 0.15f), 0), new Vector3(0.02f, 0.07f, 0.85f), Mat.DarkSteel, 0f);
            }

            b.BoxOn(-1.1f, 0.16f, -0.25f, 0.35f, 0.75f, 0.35f, Mat.SandSteel, 0.03f);
            b.Box(new Vector3(-1.1f, 0.65f, -0.43f), new Vector3(0.2f, 0.12f, 0.02f), Mat.Screen, 0f);
            b.Capsule(new Vector3(-0.6f, 1.06f, 0.15f), new Vector3(-0.6f, 1.9f, 0.15f), 0.07f, Mat.Rust, 8);
            b.CylinderX(new Vector3(-0.35f, 1.95f, 0.15f), 0.13f, 0.6f, 10, Mat.Rust, 0.02f);
            b.Pop();
        }

        /// <summary>Rooftop AC unit with a fan grille.</summary>
        public static void AcUnit(MeshBuilder b, Vector3 baseCenter)
        {
            b.BoxOn(baseCenter.X, baseCenter.Y, baseCenter.Z, 1.0f, 0.55f, 0.75f, Mat.SandSteel, 0.04f);
            b.Frustum(baseCenter + new Vector3(0.1f, 0.55f, 0), 0.3f, 0.3f, 0.04f, 14, Mat.DarkSteel, 0.01f);
            for (int i = 0; i < 4; i++)
            {
                b.Box(new Vector3(baseCenter.X - 0.51f, baseCenter.Y + 0.12f + (i * 0.1f), baseCenter.Z), new Vector3(0.02f, 0.05f, 0.6f), Mat.DarkSteel, 0f);
            }
        }

        /// <summary>Welded patch plate (thin, slightly proud of a wall facing -Z).</summary>
        /// <summary>
        /// Solar module in local space (centered, facing +Y, w x d): aluminium frame, dark glass with a visible cell
        /// grid and bus lines, so it reads as a real panel rather than a flat slab.
        /// </summary>
        public static void SolarModule(MeshBuilder b, float w, float d, bool cracked = false)
        {
            b.Box(new Vector3(0, -0.05f, 0), new Vector3(w, 0.05f, d), Mat.DarkSteel, 0.01f);
            b.Box(Vector3.Zero, new Vector3(w - 0.08f, 0.035f, d - 0.08f), Mat.Glass, 0.004f);
            const float cell = 0.16f;
            for (float x = (-w * 0.5f) + 0.04f + cell; x < (w * 0.5f) - 0.06f; x += cell)
            {
                b.Box(new Vector3(x, 0.019f, 0), new Vector3(0.012f, 0.004f, d - 0.1f), Mat.DarkSteel, 0f);
            }

            for (float z = (-d * 0.5f) + 0.04f + (cell * 2f); z < (d * 0.5f) - 0.06f; z += cell * 2f)
            {
                b.Box(new Vector3(0, 0.019f, z), new Vector3(w - 0.1f, 0.004f, 0.008f), Mat.PaintWhite, 0f);
            }

            if (cracked)
            {
                b.Box(new Vector3(w * 0.18f, 0.022f, -d * 0.12f), new Vector3(w * 0.3f, 0.004f, d * 0.26f), Mat.ConcreteDark, 0f);
            }
        }

        /// <summary>200-litre steel drum (0.58 m x 0.88 m): rolling hoops, rolled rims, recessed lid with bungs.</summary>
        public static void Barrel(MeshBuilder b, Vector3 baseCenter, Mat mat, bool lyingAlongZ = false)
        {
            if (lyingAlongZ)
            {
                b.Push(Matrix4x4.CreateTranslation(new Vector3(0, -0.44f, 0)) * Matrix4x4.CreateRotationX(MeshBuilder.Deg(90)) * Matrix4x4.CreateTranslation(baseCenter + new Vector3(0, 0.29f, 0)));
                Barrel(b, Vector3.Zero, mat);
                b.Pop();
                return;
            }

            b.Frustum(baseCenter, 0.285f, 0.285f, 0.88f, 20, mat, 0.02f);
            foreach (float y in new[] { 0.0f, 0.29f, 0.57f, 0.855f })
            {
                b.Frustum(baseCenter + new Vector3(0, y, 0), 0.297f, 0.297f, 0.028f, 20, mat, 0.008f, false);
            }

            b.Frustum(baseCenter + new Vector3(0, 0.875f, 0), 0.25f, 0.25f, 0.012f, 20, Mat.DarkSteel, 0f);
            b.Frustum(baseCenter + new Vector3(0.12f, 0.885f, 0.05f), 0.035f, 0.035f, 0.02f, 8, Mat.DarkSteel, 0.004f);
            b.Frustum(baseCenter + new Vector3(-0.15f, 0.885f, -0.02f), 0.022f, 0.022f, 0.016f, 8, Mat.DarkSteel, 0.004f);
        }

        /// <summary>
        /// A tarp draped over a heap (center on the ground, footprint w x d, height h): a smooth folded sheet that
        /// sags to the ground with a flared hem, two tie-down ropes and stakes. Never a blob (doc 11 anti-toy rules).
        /// </summary>
        public static void Tarp(MeshBuilder b, Vector3 center, float w, float d, float h, Mat mat, uint seed)
        {
            const int nx = 10;
            const int nz = 8;
            var rng = new ArtRandom(seed);
            float foldA = rng.Range(0f, 6.28f);
            float lump1 = rng.Range(-0.3f, 0.3f);
            float lump2 = rng.Range(-0.3f, 0.3f);
            var pts = new Vector3[nx + 1, nz + 1];
            for (int i = 0; i <= nx; i++)
            {
                for (int k = 0; k <= nz; k++)
                {
                    float u = (i / (float)nx * 2f) - 1f;
                    float v = (k / (float)nz * 2f) - 1f;
                    float edge = Math.Max(Math.Abs(u), Math.Abs(v));
                    float heap = (float)Math.Pow(Math.Max(0f, 1f - Math.Pow(edge, 3.0)), 0.6);
                    float lumps = 1f + (0.18f * (float)Math.Sin((u - lump1) * 3.1f)) + (0.12f * (float)Math.Cos((v + lump2) * 2.7f));
                    float fold = 0.035f * (float)Math.Sin(((u * Math.Cos(foldA)) + (v * Math.Sin(foldA))) * 9.0) * (1f - (edge * 0.5f));
                    float flare = 1f + (0.08f * (float)Math.Pow(edge, 4.0));
                    pts[i, k] = center + new Vector3(u * w * 0.5f * flare, Math.Max(0.02f, (h * heap * lumps) + fold), v * d * 0.5f * flare);
                }
            }

            var inside = center + new Vector3(0, h * 0.3f, 0);
            for (int i = 0; i < nx; i++)
            {
                for (int k = 0; k < nz; k++)
                {
                    Vector3[] q = { pts[i, k], pts[i + 1, k], pts[i + 1, k + 1], pts[i, k + 1] };
                    b.FaceSmooth(inside, mat, q, new[] { Normal(pts, i, k), Normal(pts, i + 1, k), Normal(pts, i + 1, k + 1), Normal(pts, i, k + 1) });
                }
            }

            // tie-down ropes over the top, staked at both ends
            foreach (int k in new[] { 2, nz - 2 })
            {
                for (int i = 0; i < nx; i++)
                {
                    b.Strut(pts[i, k] + new Vector3(0, 0.015f, 0), pts[i + 1, k] + new Vector3(0, 0.015f, 0), 0.016f, Mat.Rubber);
                }

                b.Strut(pts[0, k], pts[0, k] + new Vector3(-0.15f, -0.1f, 0), 0.02f, Mat.Wood);
                b.Strut(pts[nx, k], pts[nx, k] + new Vector3(0.15f, -0.1f, 0), 0.02f, Mat.Wood);
            }
        }

        private static Vector3 Normal(Vector3[,] pts, int i, int k)
        {
            int nx = pts.GetLength(0) - 1;
            int nz = pts.GetLength(1) - 1;
            Vector3 du = pts[Math.Min(i + 1, nx), k] - pts[Math.Max(i - 1, 0), k];
            Vector3 dv = pts[i, Math.Min(k + 1, nz)] - pts[i, Math.Max(k - 1, 0)];
            Vector3 n = Vector3.Cross(dv, du);
            return n.Y < 0 ? -Vector3.Normalize(n) : Vector3.Normalize(n);
        }

        public static void Plate(MeshBuilder b, Vector3 center, float w, float h, float tiltDeg, Mat mat = Mat.Rust)
        {
            b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(tiltDeg)) * Matrix4x4.CreateTranslation(center));
            b.Box(Vector3.Zero, new Vector3(w, h, 0.03f), mat, 0.008f);
            b.Pop();
        }

        /// <summary>Bundle of bent rebar sticking out of broken concrete.</summary>
        public static void Rebar(MeshBuilder b, Vector3 at, Vector3 dir, int count, uint seed)
        {
            var rng = new ArtRandom(seed);
            for (int i = 0; i < count; i++)
            {
                Vector3 o = at + new Vector3(rng.Range(-0.3f, 0.3f), rng.Range(-0.1f, 0.1f), rng.Range(-0.2f, 0.2f));
                Vector3 mid = o + (dir * rng.Range(0.3f, 0.6f));
                Vector3 end = mid + (dir * rng.Range(0.2f, 0.5f)) + new Vector3(rng.Range(-0.3f, 0.3f), rng.Range(-0.4f, 0.1f), rng.Range(-0.3f, 0.3f));
                b.Strut(o, mid, 0.025f, Mat.Rust);
                b.Strut(mid, end, 0.025f, Mat.Rust);
            }
        }

        /// <summary>Lattice antenna mast with cross arms and guy wires.</summary>
        public static void Mast(MeshBuilder b, Model m, Vector3 baseCenter, float height, bool beacon)
        {
            float w = 0.35f;
            for (int k = 0; k < 3; k++)
            {
                float a = MeshBuilder.Deg(k * 120f);
                b.Strut(baseCenter + new Vector3((float)Math.Cos(a) * w, 0, (float)Math.Sin(a) * w), baseCenter + new Vector3((float)Math.Cos(a) * w * 0.3f, height, (float)Math.Sin(a) * w * 0.3f), 0.04f, Mat.DarkSteel);
            }

            for (float y = 0.8f; y < height; y += 0.8f)
            {
                float r = w * (1f - (0.7f * y / height));
                b.Strut(baseCenter + new Vector3(r, y, 0), baseCenter + new Vector3(-r * 0.5f, y + 0.4f, r * 0.86f), 0.02f, Mat.DarkSteel);
            }

            b.Strut(baseCenter + new Vector3(-0.8f, height * 0.75f, 0), baseCenter + new Vector3(0.8f, height * 0.75f, 0), 0.03f, Mat.DarkSteel);
            b.Strut(baseCenter + new Vector3(0, height * 0.88f, -0.6f), baseCenter + new Vector3(0, height * 0.88f, 0.6f), 0.03f, Mat.DarkSteel);
            if (beacon)
            {
                Shapes.Lamp(b, m, baseCenter + new Vector3(0, height + 0.1f, 0), Mat.LampRed, Model.Red, 1.0f, 4f, LightRole.Ambient, 0.14f);
            }
        }

        /// <summary>Pole-mounted floodlight aimed down/forward.</summary>
        public static void Spotlight(MeshBuilder b, Model m, Vector3 at, float yawDeg)
        {
            b.Push(at, yawDeg);
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(35f)));
            b.Box(Vector3.Zero, new Vector3(0.4f, 0.3f, 0.3f), Mat.DarkSteel, 0.04f);
            b.Box(new Vector3(0, 0, -0.16f), new Vector3(0.32f, 0.22f, 0.02f), Mat.LampAmber, 0f);
            b.Pop();
            b.Pop();
            m.Lights.Add(new LightSpec(b.TransformPoint(at + new Vector3(0, -0.4f, -0.4f)), Model.Amber, 1.6f, 9f, LightRole.Ambient));
        }

        /// <summary>Pushes a basis whose local +Z runs from a to c (length returned); caller pops.</summary>
        private static void Basis(MeshBuilder b, Vector3 a, Vector3 c, out float len)
        {
            Vector3 d = c - a;
            len = d.Length();
            Vector3 dir = d / Math.Max(0.0001f, len);
            Vector3 up = Math.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.95f ? Vector3.UnitX : Vector3.UnitY;
            Vector3 x = Vector3.Normalize(Vector3.Cross(up, dir));
            Vector3 y = Vector3.Cross(dir, x);
            b.Push(new Matrix4x4(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, dir.X, dir.Y, dir.Z, 0, a.X, a.Y, a.Z, 1));
        }
    }
}
