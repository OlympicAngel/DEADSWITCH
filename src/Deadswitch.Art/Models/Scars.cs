using System;
using System.Collections.Generic;
using System.Numerics;
using Deadswitch.Art.Geometry;

namespace Deadswitch.Art.Models
{
    /// <summary>Scar geometry plus where the host should emit fire and smoke (local space of the scarred object).</summary>
    public sealed class ScarSet
    {
        public Model Model { get; } = new Model();

        /// <summary>Flame emitters (embers glow there; the host adds flames, sparks and a flickering light).</summary>
        public List<Vector3> Fires { get; } = new List<Vector3>();

        /// <summary>Smoke emitters. <see cref="SmokeLevel"/> sets how thick the column is.</summary>
        public List<Vector3> Smokes { get; } = new List<Vector3>();

        /// <summary>0 none, 1 thin grey wisp, 2 dark column, 3 heavy black plume.</summary>
        public int SmokeLevel { get; set; }
    }

    /// <summary>
    /// Battle scars (SPEC-018, doc 11 heroic realism): soot, burnt ground, torn panels, rubble and burnt-out hulks.
    /// Weathered and heavy, never cartoon: scorch is irregular, debris sits on the ground, nothing floats.
    /// </summary>
    public static class Scars
    {
        /// <summary>Courtyard spots for yard wrecks, off the walking paths and clear of the plots.</summary>
        private static readonly Vector3[] Spots =
        {
            new Vector3(-3.3f, 0, 3.4f),
            new Vector3(3.4f, 0, -4.4f),
            new Vector3(-3.2f, 0, -5.8f),
            new Vector3(3.3f, 0, 3.8f),
            new Vector3(-4.2f, 0, -11.4f),
            new Vector3(4.4f, 0, -11.8f),
            new Vector3(-0.6f, 0, -16.5f),
            new Vector3(3.0f, 0, -17.2f),
        };

        public static int SpotCount => Spots.Length;

        public static Vector3 Spot(int index)
        {
            return Spots[index % Spots.Length];
        }

        public static float SpotYaw(int index)
        {
            return ((index * 67) + 23) % 180 - 90;
        }

        /// <summary>
        /// Damage on a facility (1..3) in the facility's local space; <paramref name="min"/>/<paramref name="max"/> are
        /// its model bounds. More damage adds scorch, torn panels, rubble, then fire.
        /// </summary>
        public static ScarSet Facility(int damage, Vector3 min, Vector3 max, uint seed)
        {
            var set = new ScarSet();
            if (damage <= 0)
            {
                return set;
            }

            var b = new MeshBuilder(seed) { GroundOffset = 0f };
            var rng = new ArtRandom(seed ^ 0x5CA25CA2u);
            float y = Facilities.PadTop + 0.012f;
            float w = Math.Max(1.2f, max.X - min.X);
            float d = Math.Max(1.2f, max.Z - min.Z);
            float h = Math.Max(1.0f, max.Y);
            var center = new Vector3((min.X + max.X) * 0.5f, 0, (min.Z + max.Z) * 0.5f);

            // burnt ground spreading out from the front and one side
            Scorch(b, new Vector3(center.X + rng.Range(-0.4f, 0.4f), y, min.Z - 0.35f), 0.9f + (0.35f * damage), 0.55f + (0.2f * damage), rng);
            if (damage >= 2)
            {
                float side = rng.Next() < 0.5f ? min.X - 0.3f : max.X + 0.3f;
                Scorch(b, new Vector3(side, y + 0.002f, center.Z + rng.Range(-0.6f, 0.6f)), 0.6f + (0.25f * damage), 0.9f, rng);
            }

            // soot licking up the front face
            float sootW = w * (0.28f + (0.12f * damage));
            float sootX = center.X + rng.Range(-w * 0.2f, w * 0.2f);
            float sootH = Math.Min(h * 0.95f, 0.7f + (0.55f * damage));
            Soot(b, new Vector3(sootX, Facilities.PadTop, min.Z - 0.015f), sootW, sootH, rng);

            // rubble and torn sheet metal on the ground
            int chunks = 2 + (damage * 2);
            for (int i = 0; i < chunks; i++)
            {
                var p = new Vector3(center.X + rng.Range(-w * 0.6f, w * 0.6f), Facilities.PadTop, min.Z - rng.Range(0.2f, 1.1f));
                float r = rng.Range(0.1f, 0.22f);
                Props.Boulder(b, p + new Vector3(0, r * 0.35f, 0), new Vector3(r * 1.3f, r * 0.7f, r), seed + (uint)(i * 17), i % 3 == 0 ? Mat.Char : Mat.ConcreteDark);
            }

            int plates = damage;
            for (int i = 0; i < plates; i++)
            {
                var p = new Vector3(center.X + rng.Range(-w * 0.55f, w * 0.55f), Facilities.PadTop + 0.03f, min.Z - rng.Range(0.3f, 1.0f));
                b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(rng.Range(-8f, 8f))) * Matrix4x4.CreateRotationY(MeshBuilder.Deg(rng.Range(0f, 180f))) * Matrix4x4.CreateTranslation(p));
                b.Box(new Vector3(0, 0.02f, 0), new Vector3(rng.Range(0.6f, 1.1f), 0.035f, rng.Range(0.35f, 0.6f)), i % 2 == 0 ? Mat.Rust : Mat.Char, 0.01f);
                b.Pop();
            }

            if (damage >= 2)
            {
                // a torn panel leaning on the wall and a twisted strut
                float px = rng.Next() < 0.5f ? min.X + (w * 0.2f) : max.X - (w * 0.2f);
                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-18f)) * Matrix4x4.CreateRotationY(MeshBuilder.Deg(rng.Range(-12f, 12f))) * Matrix4x4.CreateTranslation(new Vector3(px, Facilities.PadTop, min.Z - 0.42f)));
                b.Box(new Vector3(0, 0.7f, 0), new Vector3(1.0f, 1.4f, 0.04f), Mat.Rust, 0.01f);
                b.Pop();
                var s0 = new Vector3(center.X + rng.Range(-w * 0.4f, w * 0.4f), Facilities.PadTop + 0.05f, min.Z - 0.9f);
                b.Strut(s0, s0 + new Vector3(rng.Range(0.6f, 1.0f), 0.18f, rng.Range(-0.4f, 0.2f)), 0.06f, Mat.DarkSteel);
                b.Strut(s0 + new Vector3(0.8f, 0.18f, 0), s0 + new Vector3(1.3f, 0.04f, 0.3f), 0.06f, Mat.DarkSteel);
            }

            if (damage >= 3)
            {
                // a blown-out breach in the front skin, glowing inside
                var hole = new Vector3(sootX, Facilities.PadTop + Math.Min(h * 0.45f, 1.1f), min.Z - 0.02f);
                b.Box(hole, new Vector3(0.7f, 0.55f, 0.03f), Mat.Char, 0.02f);
                b.Box(hole + new Vector3(0, -0.12f, -0.012f), new Vector3(0.42f, 0.22f, 0.02f), Mat.Ember, 0.01f);
                set.Fires.Add(hole + new Vector3(0, -0.1f, -0.15f));
            }

            if (damage >= 2)
            {
                // embers on the roof edge and at the foot of the wall: flames come from here
                var roof = new Vector3(Math.Clamp(sootX, min.X + 0.3f, max.X - 0.3f), Math.Min(h, 3.2f), min.Z + 0.25f);
                Embers(b, roof, 0.35f, rng);
                set.Fires.Add(roof + new Vector3(0, 0.05f, 0));
                var foot = new Vector3(center.X + rng.Range(-w * 0.4f, w * 0.4f), Facilities.PadTop, min.Z - 0.55f);
                Embers(b, foot, 0.3f, rng);
                if (damage >= 3)
                {
                    set.Fires.Add(foot + new Vector3(0, 0.05f, 0));
                }
            }

            set.Smokes.Add(new Vector3(Math.Clamp(sootX, min.X + 0.3f, max.X - 0.3f), Math.Min(h, 3.2f) + 0.1f, center.Z));
            set.SmokeLevel = Math.Min(3, damage);
            foreach (Vector3 f in set.Fires)
            {
                set.Model.Lights.Add(new LightSpec(f + new Vector3(0, 0.4f, -0.2f), Model.FireColor, damage >= 3 ? 3.2f : 2.0f, damage >= 3 ? 7f : 5f, LightRole.Fire));
            }

            set.Model.Static = b.Mesh;
            return set;
        }

        /// <summary>
        /// One yard wreck at its spot (local space, origin on the ground): a burnt-out vehicle hulk or a shell crater
        /// with debris. A fresh wreck still burns.
        /// </summary>
        public static ScarSet Wreck(int index, bool burning, uint seed)
        {
            var set = new ScarSet();
            var b = new MeshBuilder(seed + (uint)(index * 7919)) { GroundOffset = 0f };
            var rng = new ArtRandom(seed ^ (uint)(index * 104729));
            Scorch(b, new Vector3(0, 0.015f, 0), 1.9f, 1.5f, rng);
            if (index % 3 == 2)
            {
                Crater(b, rng, seed + (uint)index);
                if (burning)
                {
                    Embers(b, new Vector3(0.2f, 0.05f, 0.1f), 0.45f, rng);
                    set.Fires.Add(new Vector3(0.2f, 0.1f, 0.1f));
                }
            }
            else
            {
                Hulk(b, rng, seed + (uint)index);
                if (burning)
                {
                    Embers(b, new Vector3(0f, 1.0f, -0.6f), 0.4f, rng);
                    set.Fires.Add(new Vector3(0f, 1.05f, -0.6f));
                    set.Fires.Add(new Vector3(0.3f, 0.75f, 0.9f));
                }
            }

            set.Smokes.Add(new Vector3(0, 1.2f, 0));
            set.SmokeLevel = burning ? 2 : 1;
            foreach (Vector3 f in set.Fires)
            {
                set.Model.Lights.Add(new LightSpec(f + new Vector3(0, 0.5f, 0), Model.FireColor, 2.6f, 6f, LightRole.Fire));
            }

            set.Model.Static = b.Mesh;
            return set;
        }

        /// <summary>Irregular burnt patch flat on the ground (a jagged fan of soot).</summary>
        /// <summary>
        /// A facility brought down (SPEC-044): over the footprint it stood on (<paramref name="min"/>/<paramref name="max"/>),
        /// broken wall stumps, a pile of concrete slabs and rubble, a fallen roof sheet, bent rebar, fires in the pile and
        /// a heavy plume. Heavy and settled: everything rests on the ground or leans on something.
        /// </summary>
        public static ScarSet Rubble(Vector3 min, Vector3 max, uint seed)
        {
            var set = new ScarSet();
            var b = new MeshBuilder(seed) { GroundOffset = 0f };
            var rng = new ArtRandom(seed ^ 0x2B0B1E5u);
            float y = Facilities.PadTop;
            float w = Math.Max(1.6f, max.X - min.X);
            float d = Math.Max(1.6f, max.Z - min.Z);
            float h = Math.Clamp(max.Y, 1.2f, 4f);
            var center = new Vector3((min.X + max.X) * 0.5f, 0, (min.Z + max.Z) * 0.5f);

            Scorch(b, new Vector3(center.X, y + 0.012f, center.Z), (w * 0.55f) + 0.6f, (d * 0.55f) + 0.6f, rng);

            // wall stumps: what is left of two or three walls, torn at uneven heights
            int stumps = 2 + (int)(rng.Next() * 2f);
            for (int i = 0; i < stumps; i++)
            {
                bool alongX = i % 2 == 0;
                float len = (alongX ? w : d) * rng.Range(0.35f, 0.7f);
                float sh = h * rng.Range(0.25f, 0.6f);
                var at = alongX
                    ? new Vector3(center.X + rng.Range(-w * 0.2f, w * 0.2f), y, i == 0 ? min.Z + 0.1f : max.Z - 0.1f)
                    : new Vector3(rng.Next() < 0.5f ? min.X + 0.1f : max.X - 0.1f, y, center.Z + rng.Range(-d * 0.2f, d * 0.2f));
                b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(rng.Range(-4f, 4f))) * Matrix4x4.CreateRotationY(MeshBuilder.Deg(alongX ? 0f : 90f)) * Matrix4x4.CreateTranslation(at));
                b.Box(new Vector3(0, sh * 0.5f, 0), new Vector3(len, sh, 0.22f), Mat.ConcreteDark, 0.02f);
                // a jagged top: two broken teeth
                b.Box(new Vector3(-len * 0.25f, sh + 0.18f, 0), new Vector3(len * 0.22f, 0.36f, 0.2f), Mat.ConcreteDark, 0.02f);
                b.Box(new Vector3(len * 0.3f, sh + 0.1f, 0), new Vector3(len * 0.15f, 0.2f, 0.2f), Mat.Char, 0.02f);
                b.Pop();
                Soot(b, at + new Vector3(0, 0, -0.13f), len * 0.6f, sh, rng);
            }

            // the collapsed mass: slabs piled at angles, rubble around them
            int slabs = 5 + (int)(w * d * 0.25f);
            for (int i = 0; i < slabs; i++)
            {
                float tilt = rng.Range(8f, 34f);
                var p = new Vector3(center.X + rng.Range(-w * 0.35f, w * 0.35f), y + (0.12f * (i % 3)), center.Z + rng.Range(-d * 0.35f, d * 0.35f));
                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(tilt)) * Matrix4x4.CreateRotationY(MeshBuilder.Deg(rng.Range(0f, 180f))) * Matrix4x4.CreateTranslation(p));
                b.Box(new Vector3(0, 0.1f, 0), new Vector3(rng.Range(0.7f, 1.5f), 0.18f, rng.Range(0.5f, 1.1f)), i % 4 == 0 ? Mat.Char : Mat.ConcreteDark, 0.02f);
                b.Pop();
            }

            int stones = 10 + (int)(w * d * 0.6f);
            for (int i = 0; i < stones; i++)
            {
                var p = new Vector3(center.X + rng.Range(-w * 0.6f, w * 0.6f), y, center.Z + rng.Range(-d * 0.6f, d * 0.6f));
                float r = rng.Range(0.1f, 0.3f);
                Props.Boulder(b, p + new Vector3(0, r * 0.35f, 0), new Vector3(r * 1.3f, r * 0.7f, r), seed + (uint)(i * 23), i % 3 == 0 ? Mat.Char : Mat.ConcreteDark);
            }

            // the roof came down in one sheet, leaning on a stump
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(rng.Range(18f, 30f))) * Matrix4x4.CreateRotationY(MeshBuilder.Deg(rng.Range(-20f, 20f))) * Matrix4x4.CreateTranslation(new Vector3(center.X + rng.Range(-0.4f, 0.4f), y + 0.5f, center.Z)));
            b.Box(Vector3.Zero, new Vector3(w * 0.7f, 0.06f, d * 0.55f), Mat.Rust, 0.01f);
            b.Pop();

            // bent rebar and struts sticking out of the pile
            for (int i = 0; i < 4; i++)
            {
                var s0 = new Vector3(center.X + rng.Range(-w * 0.3f, w * 0.3f), y + 0.2f, center.Z + rng.Range(-d * 0.3f, d * 0.3f));
                var s1 = s0 + new Vector3(rng.Range(-0.5f, 0.5f), rng.Range(0.7f, 1.4f), rng.Range(-0.5f, 0.5f));
                b.Strut(s0, s1, 0.035f, Mat.DarkSteel);
                b.Strut(s1, s1 + new Vector3(rng.Range(-0.4f, 0.4f), -0.25f, rng.Range(-0.4f, 0.4f)), 0.035f, Mat.DarkSteel);
            }

            // it still burns in the pile
            for (int i = 0; i < 2; i++)
            {
                var f = new Vector3(center.X + rng.Range(-w * 0.25f, w * 0.25f), y + 0.25f, center.Z + rng.Range(-d * 0.25f, d * 0.25f));
                Embers(b, f, 0.45f, rng);
                set.Fires.Add(f + new Vector3(0, 0.1f, 0));
                if (i == 0)
                {
                    // one light per pile: a ruined block of these must not blow the night out
                    set.Model.Lights.Add(new LightSpec(f + new Vector3(0, 0.6f, 0), Model.FireColor, 2.2f, 7f, LightRole.Fire));
                }
            }

            set.Smokes.Add(new Vector3(center.X, y + 1f, center.Z));
            set.SmokeLevel = 3;
            set.Model.Static = b.Mesh;
            return set;
        }

        private static void Scorch(MeshBuilder b, Vector3 c, float rx, float rz, ArtRandom rng)
        {
            const int n = 14;
            var ring = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = (float)(Math.PI * 2 * i / n);
                float k = rng.Range(0.55f, 1.05f);
                ring[i] = c + new Vector3((float)Math.Cos(a) * rx * k, 0, (float)Math.Sin(a) * rz * k);
            }

            Vector3 below = c - new Vector3(0, 1f, 0);
            for (int i = 0; i < n; i++)
            {
                b.Face(below, Mat.Char, 0.85f + (rng.Next() * 0.3f), c, ring[i], ring[(i + 1) % n]);
            }
        }

        /// <summary>Soot rising up a wall: a jagged flame-shaped stain hugging the face (normal toward -Z).</summary>
        private static void Soot(MeshBuilder b, Vector3 baseCenter, float width, float height, ArtRandom rng)
        {
            const int cols = 7;
            Vector3 inside = baseCenter + new Vector3(0, height * 0.5f, 1f);
            float prevTop = height * rng.Range(0.35f, 0.6f);
            for (int i = 0; i < cols; i++)
            {
                float x0 = baseCenter.X - (width * 0.5f) + (width * i / cols);
                float x1 = baseCenter.X - (width * 0.5f) + (width * (i + 1) / cols);
                float mid = 1f - Math.Abs(((i + 0.5f) / cols) - 0.5f) * 1.6f;
                float top = height * Math.Max(0.2f, mid) * rng.Range(0.7f, 1.05f);
                b.Face(inside, Mat.Char, 0.8f + (rng.Next() * 0.4f),
                    new Vector3(x0, baseCenter.Y, baseCenter.Z), new Vector3(x1, baseCenter.Y, baseCenter.Z),
                    new Vector3(x1, baseCenter.Y + top, baseCenter.Z), new Vector3(x0, baseCenter.Y + prevTop, baseCenter.Z));
                prevTop = top;
            }
        }

        /// <summary>A small heap of glowing coals and charred bits.</summary>
        private static void Embers(MeshBuilder b, Vector3 c, float r, ArtRandom rng)
        {
            for (int i = 0; i < 6; i++)
            {
                var p = c + new Vector3(rng.Range(-r, r), 0.03f, rng.Range(-r, r) * 0.7f);
                float s = rng.Range(0.05f, 0.12f);
                b.Box(p + new Vector3(0, s * 0.5f, 0), new Vector3(s * 1.4f, s, s), i % 2 == 0 ? Mat.Ember : Mat.Char, 0.01f);
            }
        }

        /// <summary>Burnt-out light truck: blackened, rusting body on its rims, doors and glass gone.</summary>
        internal static void Hulk(MeshBuilder b, ArtRandom rng, uint seed)
        {
            float tilt = rng.Range(-4f, 4f);
            b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(tilt)) * Matrix4x4.CreateTranslation(new Vector3(0, 0.02f, 0)));
            // chassis rails and rims (tyres burnt away)
            b.Box(new Vector3(-0.45f, 0.42f, 0), new Vector3(0.12f, 0.14f, 3.6f), Mat.DarkSteel, 0.02f);
            b.Box(new Vector3(0.45f, 0.42f, 0), new Vector3(0.12f, 0.14f, 3.6f), Mat.DarkSteel, 0.02f);
            foreach (float z in new[] { -1.15f, 1.2f })
            {
                b.CylinderX(new Vector3(-0.82f, 0.3f, z), 0.26f, 0.18f, 10, Mat.Rust);
                b.CylinderX(new Vector3(0.82f, 0.3f, z), 0.26f, 0.18f, 10, Mat.Rust);
            }

            // cab: shell with the windscreen gone, roof caved in
            b.BoxOn(0f, 0.52f, -1.25f, 1.78f, 0.62f, 1.25f, Mat.Char, 0.05f);
            b.Box(new Vector3(-0.84f, 1.38f, -1.25f), new Vector3(0.08f, 0.62f, 1.2f), Mat.Rust, 0.02f);
            b.Box(new Vector3(0.84f, 1.38f, -1.25f), new Vector3(0.08f, 0.62f, 1.2f), Mat.Char, 0.02f);
            b.Box(new Vector3(0f, 1.38f, -0.66f), new Vector3(1.7f, 0.62f, 0.08f), Mat.Char, 0.02f);
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(9f)) * Matrix4x4.CreateTranslation(new Vector3(0, 1.66f, -1.3f)));
            b.Box(Vector3.Zero, new Vector3(1.74f, 0.06f, 1.2f), Mat.Rust, 0.02f);
            b.Pop();
            // bonnet buckled open
            b.BoxOn(0f, 0.52f, -2.15f, 1.7f, 0.42f, 0.6f, Mat.Rust, 0.04f);
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-28f)) * Matrix4x4.CreateTranslation(new Vector3(0, 1.0f, -2.35f)));
            b.Box(Vector3.Zero, new Vector3(1.6f, 0.05f, 0.7f), Mat.Char, 0.02f);
            b.Pop();
            // cargo bed: floor and broken sides
            b.BoxOn(0f, 0.52f, 0.55f, 1.84f, 0.1f, 2.3f, Mat.Rust, 0.03f);
            b.BoxOn(-0.88f, 0.62f, 0.55f, 0.06f, 0.48f, 2.3f, Mat.Char, 0.02f);
            b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(-35f)) * Matrix4x4.CreateTranslation(new Vector3(0.95f, 0.62f, 0.55f)));
            b.Box(new Vector3(0.2f, 0.18f, 0), new Vector3(0.06f, 0.46f, 2.2f), Mat.Rust, 0.02f);
            b.Pop();
            b.Pop();

            // debris thrown around it
            for (int i = 0; i < 5; i++)
            {
                var p = new Vector3(rng.Range(-1.7f, 1.7f), 0.03f, rng.Range(-2.6f, 2.6f));
                if (Math.Abs(p.X) < 1.1f)
                {
                    p.X = Math.Sign(p.X + 0.01f) * 1.3f;
                }

                b.Push(Matrix4x4.CreateRotationY(MeshBuilder.Deg(rng.Range(0f, 180f))) * Matrix4x4.CreateTranslation(p));
                b.Box(new Vector3(0, 0.02f, 0), new Vector3(rng.Range(0.3f, 0.7f), 0.03f, rng.Range(0.2f, 0.45f)), i % 2 == 0 ? Mat.Rust : Mat.Char, 0.01f);
                b.Pop();
            }

            Props.Boulder(b, new Vector3(1.4f, 0.08f, -2.4f), new Vector3(0.22f, 0.14f, 0.18f), seed + 3, Mat.ConcreteDark);
        }

        /// <summary>Shell crater: a raised lip of churned earth and broken concrete around a dark pit.</summary>
        private static void Crater(MeshBuilder b, ArtRandom rng, uint seed)
        {
            const int n = 12;
            for (int i = 0; i < n; i++)
            {
                float a = (float)(Math.PI * 2 * i / n);
                float r = rng.Range(0.95f, 1.25f);
                var p = new Vector3((float)Math.Cos(a) * r, 0.06f, (float)Math.Sin(a) * r);
                float s = rng.Range(0.22f, 0.36f);
                Props.Boulder(b, p, new Vector3(s * 1.4f, s * 0.55f, s), seed + (uint)(i * 13), i % 4 == 0 ? Mat.ConcreteDark : Mat.Ground);
            }

            // the pit: a dark, slightly sunken disk
            const int m = 12;
            var c = new Vector3(0, 0.025f, 0);
            for (int i = 0; i < m; i++)
            {
                float a0 = (float)(Math.PI * 2 * i / m);
                float a1 = (float)(Math.PI * 2 * (i + 1) / m);
                b.Face(c - new Vector3(0, 1f, 0), Mat.Char, 0.7f, c, new Vector3((float)Math.Cos(a0) * 0.95f, 0.03f, (float)Math.Sin(a0) * 0.95f), new Vector3((float)Math.Cos(a1) * 0.95f, 0.03f, (float)Math.Sin(a1) * 0.95f));
            }

            for (int i = 0; i < 4; i++)
            {
                var p = new Vector3(rng.Range(-2.2f, 2.2f), 0.03f, rng.Range(-2.2f, 2.2f));
                b.Push(Matrix4x4.CreateRotationY(MeshBuilder.Deg(rng.Range(0f, 180f))) * Matrix4x4.CreateTranslation(p));
                b.Box(new Vector3(0, 0.05f, 0), new Vector3(rng.Range(0.4f, 0.8f), 0.1f, rng.Range(0.3f, 0.5f)), Mat.ConcreteDark, 0.03f);
                b.Pop();
            }
        }
    }
}
