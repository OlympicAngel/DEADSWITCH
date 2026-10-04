using System;
using System.Numerics;
using Deadswitch.Art.Geometry;

namespace Deadswitch.Art.Models
{
    /// <summary>
    /// The pre-war bunker the compound grew against (docs/agents/environment-art.md). Its blast door is the way
    /// down to the AI core: green phosphor light spills into the courtyard. Decades of survivors have bolted a
    /// catwalk, pipe racks, machinery, a terrace and a rooftop shanty onto the cracked concrete. World space,
    /// facade at z = <see cref="FacadeZ"/>.
    /// </summary>
    public static class Core
    {
        public const float FacadeZ = 12.5f;

        /// <summary>Where the core's light and the "CORE" label sit (in front of the door).</summary>
        public static readonly Vector3 DoorPoint = new Vector3(0, 0, FacadeZ - 0.6f);

        private const float CenterTop = 9.6f;
        private const float WingTop = 7.4f;
        private const float WalkY = 4.3f;

        public static Model Build(uint seed)
        {
            var m = new Model();
            var b = new MeshBuilder(seed) { AoHeight = 4.5f, AoFloor = 0.45f, FaceJitter = 0.1f };
            var rng = new ArtRandom(seed + 3);

            Mass(b, rng);
            Facade(b, rng);
            Door(b, m);
            Catwalk(b, m);
            PipeRack(b);
            GroundMachinery(b, m, rng);
            Terrace(b, m, rng);
            Collapse(b, rng, seed);
            Roof(b, m, rng, seed);
            ScaffoldTower(b);

            m.Static = b.Mesh;
            m.Cones = b.Cones;
            return m;
        }

        /// <summary>Stepped massing: a tall central block between lower, set-back wings, sunk into the hill.</summary>
        private static void Mass(MeshBuilder b, ArtRandom rng)
        {
            const float z = FacadeZ;
            b.BoxOn(0, -0.5f, z + 6f, 17f, CenterTop + 0.5f, 12f, Mat.ConcreteDark, 0.25f);
            foreach (int s in new[] { -1, 1 })
            {
                b.BoxOn(s * 14f, -0.5f, z + 6.6f, 11.2f, WingTop + 0.5f, 12.6f, Mat.ConcreteDark, 0.2f);
                b.BoxOn(s * 8.75f, -0.5f, z + 0.5f, 0.9f, CenterTop - 0.2f, 1.0f, Mat.Concrete, 0.12f);
                b.BoxOn(s * 19.2f, -0.5f, z + 0.9f, 0.8f, WingTop, 1.2f, Mat.Concrete, 0.1f);
            }

            b.BoxOn(0, -0.2f, z + 0.35f, 39f, 0.9f, 0.9f, Mat.Concrete, 0.1f);

            // parapets: one continuous cast wall per block, spalled down to stubs at the breaks (rebar exposed),
            // the broken piece lying on the roof behind
            for (float x = -19.4f; x < 19.4f;)
            {
                float len = rng.Range(2.4f, 4.4f);
                bool center = Math.Abs(x + (len * 0.5f)) < 8.4f;
                float top = center ? CenterTop : WingTop;
                float zf = (center ? z - 0.15f : z + 0.4f) + 0.25f;
                float cx = x + (len * 0.5f);
                if (rng.Next() < 0.25f)
                {
                    float gap = rng.Range(0.8f, 1.6f);
                    float side = (len - gap) * 0.5f;
                    b.BoxOn(x + (side * 0.5f), top - 0.1f, zf, side, 0.75f, 0.45f, Mat.Concrete, 0.05f);
                    b.BoxOn(x + len - (side * 0.5f), top - 0.1f, zf, side, 0.75f, 0.45f, Mat.Concrete, 0.05f);
                    b.BoxOn(cx, top - 0.1f, zf, gap, 0.22f, 0.45f, Mat.ConcreteDark, 0.08f);
                    KitParts.Rebar(b, new Vector3(cx, top + 0.1f, zf), new Vector3(rng.Range(-0.4f, 0.4f), 0.8f, -0.3f), 5, rng.NextUInt());
                    b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(rng.Range(-80f, -60f))) * Matrix4x4.CreateRotationY(MeshBuilder.Deg(rng.Range(-15f, 15f))) * Matrix4x4.CreateTranslation(new Vector3(cx, top + 0.22f, zf + 0.75f)));
                    b.Box(Vector3.Zero, new Vector3(gap * 0.9f, 0.7f, 0.4f), Mat.Concrete, 0.06f);
                    b.Pop();
                }
                else
                {
                    b.BoxOn(cx, top - 0.1f, zf, len, 0.75f, 0.45f, Mat.Concrete, 0.05f);
                }

                b.Box(new Vector3(x + len, top + 0.27f, zf - 0.23f), new Vector3(0.04f, 0.75f, 0.02f), Mat.ConcreteDark, 0f);
                x += len;
            }
        }

        /// <summary>Pilasters, formwork seams, rain streaks, spalled patches with exposed rebar, stencils, vents.</summary>
        private static void Facade(MeshBuilder b, ArtRandom rng)
        {
            const float z = FacadeZ;
            float[] pilasters = { -16.5f, -12.6f, -4.6f, 4.6f, 12.4f, 16.8f };
            foreach (float x in pilasters)
            {
                float top = Math.Abs(x) < 8.2f ? CenterTop - 0.8f : WingTop - 0.6f;
                b.BoxOn(x, 0, z - 0.25f, 0.9f, top, 0.6f, Mat.Concrete, 0.12f);
            }

            for (int i = 0; i < 18; i++)
            {
                float jx = -18.5f + (i * 2.2f);
                if (Math.Abs(jx) > 3.8f)
                {
                    float top = Math.Abs(jx) < 8.2f ? CenterTop - 0.8f : WingTop - 0.6f;
                    b.Box(new Vector3(jx, top * 0.5f, z - 0.02f), new Vector3(0.05f, top, 0.03f), Mat.ConcreteDark, 0f);
                }
            }

            b.Box(new Vector3(0, 3.3f, z - 0.02f), new Vector3(38f, 0.05f, 0.03f), Mat.ConcreteDark, 0f);
            for (int i = 0; i < 26; i++)
            {
                float gx = rng.Range(-18.5f, 18.5f);
                if (Math.Abs(gx) < 4f)
                {
                    continue;
                }

                float top = Math.Abs(gx) < 8.2f ? CenterTop - 0.8f : WingTop - 0.6f;
                float gh = rng.Range(1.2f, 4.5f);
                b.Box(new Vector3(gx, top - (gh * 0.5f), z - 0.02f), new Vector3(rng.Range(0.3f, 1.4f), gh, 0.03f), rng.Next() < 0.6f ? Mat.ConcreteDark : Mat.Rust, 0f);
            }

            // spalled patches: recessed dark concrete with bent rebar
            foreach (Vector3 p in new[] { new Vector3(-7.0f, 6.9f, 0), new Vector3(6.2f, 2.2f, 0), new Vector3(-14.8f, 5.6f, 0), new Vector3(10.4f, 6.0f, 0) })
            {
                b.Box(new Vector3(p.X, p.Y, z - 0.01f), new Vector3(1.6f, 1.1f, 0.03f), Mat.Rubber, 0f);
                b.Box(new Vector3(p.X + 0.2f, p.Y - 0.1f, z - 0.02f), new Vector3(1.1f, 0.7f, 0.03f), Mat.ConcreteDark, 0f);
                for (int k = 0; k < 4; k++)
                {
                    b.Strut(new Vector3(p.X - 0.7f, p.Y - 0.4f + (k * 0.25f), z - 0.06f), new Vector3(p.X + 0.7f, p.Y - 0.4f + (k * 0.25f) + rng.Range(-0.1f, 0.1f), z - 0.06f), 0.025f, Mat.Rust);
                }

                KitParts.Rebar(b, new Vector3(p.X + 0.6f, p.Y - 0.3f, z - 0.1f), new Vector3(0.2f, -0.6f, -0.8f), 3, rng.NextUInt());
            }

            Props.Stencil(b, "S-17", new Vector3(-16.2f, 1.6f, z - 0.03f), 0.3f, Mat.PaintWhite);
            Shapes.Hazard(b, 10.8f, 14.6f, 0.3f, 0.8f, z - 0.03f, 10);

            // big louvered intakes and utility boxes on the facade
            foreach (Vector3 v in new[] { new Vector3(-6.6f, 1.2f, z - 0.1f), new Vector3(6.9f, 4.9f, z - 0.1f), new Vector3(-11.0f, 5.2f, z - 0.1f) })
            {
                KitParts.Vent(b, v, 1.5f, 1.2f);
            }

            foreach (Vector3 u in new[] { new Vector3(-3.9f, 1.6f, z - 0.4f), new Vector3(-3.3f, 1.5f, z - 0.4f), new Vector3(5.2f, 1.7f, z - 0.4f), new Vector3(9.6f, 1.6f, z - 0.15f) })
            {
                KitParts.UtilityBox(b, u, rng.Next() < 0.5f ? Mat.SandSteel : Mat.OliveSteel);
            }
        }

        /// <summary>Portal, green-lit interior, half-open blast door, steel hood, the AI's eye and wall lamps.</summary>
        private static void Door(MeshBuilder b, Model m)
        {
            const float z = FacadeZ;
            b.BoxOn(0, 0, z - 0.9f, 7.4f, 6.2f, 1.6f, Mat.Concrete, 0.2f);
            // dark recessed portal: stepped reveals give depth; the phosphor glow only carries at night
            b.BoxOn(0, 0.2f, z - 1.75f, 4.6f, 4.4f, 0.2f, Mat.Portal, 0.02f);
            b.BoxOn(-2.2f, 0.2f, z - 1.95f, 0.22f, 4.4f, 0.22f, Mat.ConcreteDark, 0.03f);
            b.BoxOn(2.2f, 0.2f, z - 1.95f, 0.22f, 4.4f, 0.22f, Mat.ConcreteDark, 0.03f);
            b.BoxOn(0, 4.38f, z - 1.95f, 4.6f, 0.22f, 0.22f, Mat.ConcreteDark, 0.03f);
            for (int i = 0; i < 3; i++)
            {
                b.Box(new Vector3(1.0f, 1.1f + (i * 1.3f), z - 1.88f), new Vector3(1.6f, 0.04f, 0.03f), Mat.LampPhosphor, 0f);
            }

            // a neon cyan frame line on the portal: the base's one bit of pre-war signage still lit
            b.Box(new Vector3(0, 4.52f, z - 2.08f), new Vector3(4.2f, 0.04f, 0.04f), Mat.NeonCyan, 0f);
            b.BoxOn(0, 4.6f, z - 1.8f, 5.0f, 0.5f, 0.3f, Mat.DarkSteel, 0.05f);
            b.BoxOn(-2.45f, 0, z - 1.8f, 0.3f, 4.6f, 0.3f, Mat.DarkSteel, 0.05f);
            b.BoxOn(2.45f, 0, z - 1.8f, 0.3f, 4.6f, 0.3f, Mat.DarkSteel, 0.05f);
            b.BoxOn(-1.25f, 0.05f, z - 2.15f, 2.6f, 4.3f, 0.45f, Mat.DarkSteel, 0.1f);
            Shapes.Hazard(b, -2.5f, 2.5f, 4.65f, 5.0f, z - 1.98f, 14);
            for (int i = 0; i < 4; i++)
            {
                b.Box(new Vector3(-1.25f, 0.7f + (i * 1.0f), z - 2.4f), new Vector3(2.3f, 0.12f, 0.05f), Mat.Rust, 0.01f);
            }

            // door track and hydraulic rams
            b.Box(new Vector3(-2.6f, 4.55f, z - 2.3f), new Vector3(5.6f, 0.18f, 0.18f), Mat.DarkSteel, 0.02f);
            b.Capsule(new Vector3(-3.4f, 0.6f, z - 2.0f), new Vector3(-2.65f, 3.6f, z - 2.3f), 0.09f, Mat.OliveSteel, 8);
            b.Capsule(new Vector3(3.4f, 0.6f, z - 2.0f), new Vector3(2.65f, 3.6f, z - 2.3f), 0.09f, Mat.OliveSteel, 8);

            // welded steel hood over the portal
            foreach (int s in new[] { -1, 1 })
            {
                b.Strut(new Vector3(s * 3.4f, 6.2f, z - 1.7f), new Vector3(s * 3.4f, 6.6f, z - 3.0f), 0.12f, Mat.DarkSteel);
            }

            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(12f)) * Matrix4x4.CreateTranslation(new Vector3(0, 6.55f, z - 2.4f)));
            b.Box(Vector3.Zero, new Vector3(7.2f, 0.12f, 1.6f), Mat.Rust, 0.02f);
            b.Pop();

            m.Lights.Add(new LightSpec(new Vector3(0.9f, 1.8f, z - 3.2f), Model.Phosphor, 3.2f, 11f, LightRole.Ambient));
            m.Lights.Add(new LightSpec(new Vector3(0.9f, 2.4f, z - 1.4f), Model.Phosphor, 2.0f, 5f, LightRole.Ambient));

            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-90f)) * Matrix4x4.CreateTranslation(new Vector3(0, 5.6f, z - 1.75f)));
            b.Frustum(Vector3.Zero, 0.55f, 0.5f, 0.2f, 12, Mat.DarkSteel, 0.03f);
            b.Frustum(new Vector3(0, 0.2f, 0), 0.32f, 0.28f, 0.06f, 12, Mat.LampPhosphor, 0.01f);
            b.Pop();

            foreach (int sx in new[] { -1, 1 })
            {
                b.Strut(new Vector3(sx * 3.1f, 3.7f, z - 1.8f), new Vector3(sx * 3.1f, 3.7f, z - 2.5f), 0.08f, Mat.DarkSteel);
                Props.Lamp(b, m, new Vector3(sx * 3.1f, 3.6f, z - 2.55f), true, 2.2f);
            }

            Shapes.SandbagWall(b, new Vector3(-4.6f, 0, z - 2.6f), new Vector3(-3.4f, 0, z - 3.6f), 3);
            Shapes.SandbagWall(b, new Vector3(3.4f, 0, z - 3.6f), new Vector3(4.6f, 0, z - 2.6f), 3);
        }

        /// <summary>Bracketed catwalk along the facade on both sides of the door, stairs down, ladders to the roof.</summary>
        private static void Catwalk(MeshBuilder b, Model m)
        {
            const float z = FacadeZ;
            const float w = 1.1f;
            foreach (int s in new[] { -1, 1 })
            {
                float x0 = s * 3.9f;
                float x1 = s * 11.6f;
                float cx = (x0 + x1) * 0.5f;
                float len = Math.Abs(x1 - x0);
                b.BoxOn(cx, WalkY - 0.12f, z - 0.25f - (w * 0.5f), len, 0.12f, w, Mat.DarkSteel, 0.01f);
                for (float x = Math.Min(x0, x1) + 0.2f; x < Math.Max(x0, x1); x += 0.3f)
                {
                    b.Box(new Vector3(x, WalkY + 0.005f, z - 0.25f - (w * 0.5f)), new Vector3(0.03f, 0.01f, w - 0.1f), Mat.Rust, 0f);
                }

                for (float x = Math.Min(x0, x1) + 0.5f; x < Math.Max(x0, x1); x += 2.1f)
                {
                    b.Strut(new Vector3(x, WalkY - 1.4f, z - 0.05f), new Vector3(x, WalkY - 0.1f, z - 0.25f - w), 0.08f, Mat.DarkSteel);
                    b.Box(new Vector3(x, WalkY - 1.45f, z - 0.08f), new Vector3(0.3f, 0.3f, 0.06f), Mat.DarkSteel, 0.01f);
                }

                Props.Railing(b, new Vector3(x0, WalkY, z - 0.25f - w), new Vector3(x1, WalkY, z - 0.25f - w));
                Props.Stairs(b, new Vector3(x1 + (s * 0.1f), 0, z - 0.25f - w - (((int)(WalkY / 0.22f)) * 0.26f)), WalkY, 180f, 0.9f);
                KitParts.Ladder(b, new Vector3(s * 7.6f, WalkY, z - 0.12f), (s < 0 ? CenterTop : CenterTop) - WalkY, 0f, true);
                Props.Lamp(b, m, new Vector3(s * 8.4f, WalkY + 1.9f, z - 0.45f), true, 1.6f);
            }
        }

        /// <summary>Pipe rack above the catwalk: three runs on brackets, dropping into the ground at the ends.</summary>
        private static void PipeRack(MeshBuilder b)
        {
            const float z = FacadeZ;
            foreach (int s in new[] { -1, 1 })
            {
                float x0 = s * 4.2f;
                float x1 = s * 12.4f;
                for (int i = 0; i < 3; i++)
                {
                    float y = 6.0f + (i * 0.38f);
                    float dz = z - 0.35f - (i * 0.12f);
                    float r = i == 0 ? 0.2f : 0.11f;
                    KitParts.Pipe(b, new[] { new Vector3(x0, y, z - 0.05f), new Vector3(x0, y, dz), new Vector3(x1, y, dz), new Vector3(x1 + (s * 0.6f), y, dz - 0.4f), new Vector3(x1 + (s * 0.6f), 0.2f, dz - 0.4f) }, r, i == 0 ? Mat.Rust : Mat.DarkSteel);
                }

                for (float x = Math.Min(x0, x1) + 0.6f; x < Math.Max(x0, x1); x += 2.4f)
                {
                    b.Strut(new Vector3(x, 5.75f, z - 0.02f), new Vector3(x, 5.75f, z - 0.75f), 0.08f, Mat.DarkSteel);
                    b.Strut(new Vector3(x, 5.0f, z - 0.02f), new Vector3(x, 5.75f, z - 0.75f), 0.05f, Mat.DarkSteel);
                }
            }
        }

        /// <summary>Generators, a fenced transformer, cable bundles and stores along the foot of the facade.</summary>
        private static void GroundMachinery(MeshBuilder b, Model m, ArtRandom rng)
        {
            const float z = FacadeZ;
            KitParts.Genset(b, new Vector3(-5.7f, 0, z - 1.2f), 0f);
            KitModules.LeanTo(b, new Vector3(-5.6f, 0, z - 0.3f), 3.6f, 1.9f, 3.2f, 2.7f, Mat.Rust);
            KitParts.Pipe(b, new[] { new Vector3(-6.3f, 2.0f, z - 1.05f), new Vector3(-6.3f, 3.3f, z - 1.05f), new Vector3(-6.3f, 3.3f, z - 0.1f) }, 0.08f, Mat.Rust);
            KitParts.Transformer(b, new Vector3(6.4f, 0, z - 1.2f), 1.1f);
            KitModules.Fence(b, new Vector3(5.0f, 0, z - 2.4f), new Vector3(8.0f, 0, z - 2.4f), 1.8f);
            KitModules.Fence(b, new Vector3(8.0f, 0, z - 2.4f), new Vector3(8.0f, 0, z - 0.6f), 1.8f);
            for (int i = 0; i < 3; i++)
            {
                KitParts.Cable(b, new Vector3(6.04f + (i * 0.36f), 2.4f, z - 1.2f), new Vector3(5.6f + (i * 0.3f), 4.0f, z - 0.05f), 0.25f, 0.03f);
            }

            // cable bundles down the facade into the ground
            foreach (float x in new[] { -3.6f, 3.7f, 9.9f })
            {
                for (int k = 0; k < 3; k++)
                {
                    b.Capsule(new Vector3(x + (k * 0.09f), 6.0f, z - 0.08f), new Vector3(x + (k * 0.09f) + 0.1f, 0.05f, z - 0.25f - (k * 0.05f)), 0.035f, Mat.Rubber, 6);
                }
            }

            KitModules.Barrels(b, new Vector3(-10.4f, 0, z - 2.4f), 4, rng);
            KitModules.CrateStack(b, new Vector3(9.6f, 0, z - 1.6f), rng);
            KitModules.Pallet(b, new Vector3(-2.9f, 0, z - 4.4f), 20f, 3);
        }

        /// <summary>Left wing: retaining-walled terrace with a stores container, stairs and a lamp.</summary>
        private static void Terrace(MeshBuilder b, Model m, ArtRandom rng)
        {
            const float z = FacadeZ;
            const float h = 2.4f;
            const float cx = -15.6f;
            KitModules.RetainingWall(b, new Vector3(cx, 0, z - 4.2f), 8.4f, h, rng);
            b.BoxOn(cx, 0, z - 2.0f, 8.4f, h, 4.0f, Mat.ConcreteDark, 0.05f);
            b.BoxOn(cx, h, z - 2.0f, 8.4f, 0.06f, 4.0f, Mat.Ground, 0.02f);
            KitModules.ContainerBlock(b, m, new Vector3(cx - 0.6f, h, z - 1.6f), 6.0f, Mat.TarpBlue, true, "STORES", rng, 1, true);
            Props.Stairs(b, new Vector3(cx + 3.6f, 0, z - 4.2f - (((int)(h / 0.22f)) * 0.26f)), h, 180f, 0.9f);
            Props.Railing(b, new Vector3(cx - 4.2f, h, z - 3.95f), new Vector3(cx + 4.0f, h, z - 3.95f));
            KitModules.Barrels(b, new Vector3(cx + 2.8f, h, z - 3.5f), 2, rng);
            KitParts.Spotlight(b, m, new Vector3(cx + 3.8f, h + 3.2f, z - 3.7f), 20f);
            b.Strut(new Vector3(cx + 3.8f, h, z - 3.7f), new Vector3(cx + 3.8f, h + 3.1f, z - 3.6f), 0.08f, Mat.DarkSteel);
        }

        /// <summary>Right wing: roof slab sheared onto a rubble slope, rebar, a bent beam.</summary>
        private static void Collapse(MeshBuilder b, ArtRandom rng, uint seed)
        {
            const float z = FacadeZ;
            b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(-22f)) * Matrix4x4.CreateRotationY(MeshBuilder.Deg(6f)) * Matrix4x4.CreateTranslation(new Vector3(16f, 4.8f, z + 0.9f)));
            b.Box(Vector3.Zero, new Vector3(8f, 0.9f, 4.4f), Mat.Concrete, 0.12f);
            KitParts.Rebar(b, new Vector3(-3.9f, 0.3f, -1.5f), new Vector3(-0.8f, 0.4f, -0.3f), 8, seed + 77);
            b.Pop();
            for (int i = 0; i < 18; i++)
            {
                Props.Boulder(b, new Vector3(rng.Range(11.5f, 19f), rng.Range(0.2f, 2.6f) * (1f - (i / 22f)), z - rng.Range(0.2f, 3.0f)), new Vector3(rng.Range(0.5f, 1.3f), rng.Range(0.35f, 0.8f), rng.Range(0.5f, 1.1f)), seed + (uint)i, i % 3 == 0 ? Mat.ConcreteDark : Mat.Concrete);
            }

            KitParts.IBeam(b, new Vector3(12.2f, 0.3f, z - 2.6f), new Vector3(15.5f, 3.4f, z - 0.4f), 0.3f, 0.2f, Mat.Rust);
            KitModules.Debris(b, new Vector3(13.2f, 0, z - 3.6f), 1.2f, rng);
        }

        /// <summary>Rooftop shanty: stacked containers, mast, water tank, gun nest, floodlights, moss.</summary>
        private static void Roof(MeshBuilder b, Model m, ArtRandom rng, uint seed)
        {
            const float z = FacadeZ;
            KitModules.ContainerBlock(b, m, new Vector3(-4.4f, CenterTop, z + 2.6f), 6.0f, Mat.Rust, true, "COMMS", rng, 2, false);
            b.Push(new Vector3(-3.6f, CenterTop + 2.6f, z + 3.4f), 82f);
            KitModules.ContainerBlock(b, m, Vector3.Zero, 3.2f, Mat.OliveSteel, false, string.Empty, rng, 1, true);
            b.Pop();
            Props.Stairs(b, new Vector3(-0.6f, CenterTop, z + 4.4f), 2.6f, 180f);

            Vector3 mast = new Vector3(-0.6f, CenterTop, z + 1.6f);
            KitParts.Mast(b, m, mast, 11f, true);
            foreach (Vector3 g in new[] { new Vector3(-3.0f, 0, -1.2f), new Vector3(3.2f, 0, -1.0f), new Vector3(0.6f, 0, 4.0f) })
            {
                b.Strut(mast + new Vector3(0, 10.2f, 0), mast + g, 0.015f, Mat.DarkSteel);
            }

            KitParts.TankV(b, new Vector3(3.6f, CenterTop, z + 4.2f), 0.9f, 2.0f, Mat.SandSteel, seed + 31);
            KitParts.AcUnit(b, new Vector3(1.6f, CenterTop, z + 2.0f));
            KitParts.Vent(b, new Vector3(6.0f, CenterTop, z + 6.0f), 1.2f, 1.0f);

            // old gun nest on the right wing, floodlights on the parapet
            Shapes.SandbagRing(b, new Vector3(11.5f, WingTop, z + 2.8f), 1.8f, 16, 2, 250f, 40f);
            b.Frustum(new Vector3(11.5f, WingTop, z + 2.8f), 0.6f, 0.5f, 0.6f, 12, Mat.DarkSteel, 0.03f);
            b.CylinderZ(new Vector3(11.5f, WingTop + 0.9f, z + 1.7f), 0.09f, 2.0f, 8, Mat.DarkSteel, 0.01f);
            foreach (float x in new[] { 5.4f, -8.0f })
            {
                b.Strut(new Vector3(x, CenterTop - 0.8f, z + 0.3f), new Vector3(x, CenterTop + 1.6f, z + 0.3f), 0.1f, Mat.DarkSteel);
                Shapes.Lamp(b, m, new Vector3(x, CenterTop + 1.7f, z - 0.05f), Mat.LampAmber, Model.Amber, 2.0f, 10f, LightRole.Ambient, 0.3f);
            }

            KitModules.TarpPile(b, new Vector3(-14f, WingTop, z + 2.4f), 1.6f, rng);
            KitModules.Barrels(b, new Vector3(-17.4f, WingTop, z + 1.8f), 3, rng);
            for (int i = 0; i < 30; i++)
            {
                float x = rng.Range(-18f, 18f);
                float top = Math.Abs(x) < 8.2f ? CenterTop : WingTop;
                Props.Boulder(b, new Vector3(x, top - 0.05f, z + rng.Range(0.8f, 9f)), new Vector3(rng.Range(0.4f, 1.1f), rng.Range(0.15f, 0.3f), rng.Range(0.4f, 1.0f)), seed + 40 + (uint)i, Mat.Foliage);
            }
        }

        /// <summary>Scaffold tower with a blue tarp: repairs on the left of the door.</summary>
        private static void ScaffoldTower(MeshBuilder b)
        {
            const float z = FacadeZ;
            const float sx0 = -9.6f;
            foreach (float x in new[] { sx0 - 1.2f, sx0 + 1.2f })
            {
                foreach (float dz in new[] { -3.4f, -1.6f })
                {
                    b.Strut(new Vector3(x, 0, z + dz), new Vector3(x, 7.4f, z + dz), 0.09f, Mat.Rust);
                }
            }

            for (float y = 1.6f; y < 7.4f; y += 1.5f)
            {
                b.BoxOn(sx0, y, z - 2.5f, 2.6f, 0.07f, 1.9f, Mat.Wood, 0.01f);
                b.Strut(new Vector3(sx0 - 1.2f, y + 0.9f, z - 3.4f), new Vector3(sx0 + 1.2f, y + 0.9f, z - 3.4f), 0.04f, Mat.Paint);
                b.Strut(new Vector3(sx0 - 1.2f, y - 1.4f, z - 3.4f), new Vector3(sx0 + 1.2f, y, z - 3.4f), 0.035f, Mat.Rust);
            }

            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(8f)) * Matrix4x4.CreateTranslation(new Vector3(sx0 + 0.3f, 5.4f, z - 3.55f)));
            b.Box(Vector3.Zero, new Vector3(2.4f, 2.6f, 0.05f), Mat.TarpBlue, 0.01f);
            b.Pop();
        }
    }
}
