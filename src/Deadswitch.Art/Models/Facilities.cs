using System;
using System.Numerics;
using Deadswitch.Art.Geometry;
using Deadswitch.Sim.State;

namespace Deadswitch.Art.Models
{
    /// <summary>
    /// Facility models in the compound style (SPEC-003 rule 4): converted shipping containers, sheds and
    /// emplacements, each growing with its level. Local space: origin at ground center of a 4.8 m plot,
    /// +Y up, front toward -Z (rotated toward the camera by <c>HubScene.SlotYaw</c>).
    /// </summary>
    public static class Facilities
    {
        /// <summary>Footing height; facilities sit on it.</summary>
        public const float PadTop = 0.12f;

        public static Model Build(FacilityKind kind, int level, uint seed)
        {
            var m = new Model();
            var b = new MeshBuilder(seed) { GroundOffset = 0f };
            level = Math.Max(1, level);
            b.Push(new Vector3(0, PadTop, 0));
            switch (kind)
            {
                case FacilityKind.Generator:
                    Generator(b, m, level, seed);
                    break;
                case FacilityKind.ServerRack:
                    Compute(b, m, level, seed);
                    break;
                case FacilityKind.LifeSupport:
                    Habitat(b, m, level, seed);
                    break;
                case FacilityKind.BatteryBank:
                    Battery(b, m, level, seed);
                    break;
                case FacilityKind.Turret:
                    Turret(b, m, level, seed);
                    break;
            }

            b.Pop();
            m.Static = b.Mesh;
            m.Cones = b.Cones;
            for (int i = 0; i < m.Parts.Count; i++)
            {
                AnimPart p = m.Parts[i];
                m.Parts[i] = new AnimPart(p.Mesh, p.Pivot + new Vector3(0, PadTop, 0), p.Kind, p.Speed, p.Range);
            }

            return m;
        }

        /// <summary>Half size of a plot (plots are 6.4 m square).</summary>
        public const float PlotHalf = 3.2f;

        /// <summary>Worn concrete footing. Empty plots get hazard corner marks and survey stakes.</summary>
        public static MeshData Pad(uint seed, bool empty)
        {
            var b = new MeshBuilder(seed) { AoFloor = 0.55f, AoHeight = 0.4f, FaceJitter = 0.12f };
            b.BoxOn(0, -0.25f, 0, PlotHalf * 2f, 0.37f, PlotHalf * 2f, Mat.ConcreteDark, 0.06f);
            for (int i = 1; i < 3; i++)
            {
                b.Box(new Vector3(-PlotHalf + (i * PlotHalf * 2f / 3f), PadTop, 0), new Vector3(0.03f, 0.01f, PlotHalf * 2f), Mat.Rubber, 0f);
            }

            if (empty)
            {
                float c = PlotHalf - 0.35f;
                foreach (int sx in new[] { -1, 1 })
                {
                    foreach (int sz in new[] { -1, 1 })
                    {
                        b.Box(new Vector3(sx * c, PadTop, sz * (c - 0.4f)), new Vector3(0.14f, 0.015f, 0.8f), Mat.Paint, 0f);
                        b.Box(new Vector3(sx * (c - 0.4f), PadTop, sz * c), new Vector3(0.8f, 0.015f, 0.14f), Mat.Paint, 0f);
                        b.Strut(new Vector3(sx * (PlotHalf + 0.1f), 0, sz * (PlotHalf + 0.1f)), new Vector3(sx * (PlotHalf + 0.1f), 0.8f, sz * (PlotHalf + 0.1f)), 0.05f, Mat.Wood);
                        b.Box(new Vector3(sx * (PlotHalf + 0.1f), 0.72f, sz * (PlotHalf + 0.1f)), new Vector3(0.08f, 0.1f, 0.08f), Mat.PaintRed, 0.01f);
                    }
                }

                b.BoxOn(-1.6f, PadTop, 1.4f, 1.2f, 0.12f, 1.0f, Mat.Wood, 0.01f);
                b.BoxOn(-1.6f, PadTop + 0.12f, 1.4f, 1.1f, 0.5f, 0.9f, Mat.Concrete, 0.04f);
                for (int i = 0; i < 4; i++)
                {
                    b.CylinderX(new Vector3(1.2f, PadTop + 0.08f + (i % 2 * 0.15f), -1.6f + (i * 0.17f)), 0.07f, 2.4f, 8, Mat.Rust, 0.01f);
                }
            }

            return b.Mesh;
        }

        /// <summary>Scaffolding and a blue tarp around a facility under construction.</summary>
        public static MeshData Scaffold(float height, uint seed)
        {
            var b = new MeshBuilder(seed);
            float h = Math.Max(2.6f, height);
            float[] xs = { -2.9f, -0.95f, 0.95f, 2.9f };
            float[] zs = { -1.7f, 1.7f };
            foreach (float x in xs)
            {
                foreach (float z in zs)
                {
                    b.Strut(new Vector3(x, 0, z), new Vector3(x, h, z), 0.06f, Mat.Rust);
                }
            }

            for (float y = 0.9f; y < h; y += 1.0f)
            {
                foreach (float z in zs)
                {
                    b.Strut(new Vector3(-2.9f, y, z), new Vector3(2.9f, y, z), 0.05f, Mat.Rust);
                }

                foreach (float x in xs)
                {
                    b.Strut(new Vector3(x, y, -1.7f), new Vector3(x, y, 1.7f), 0.05f, Mat.Rust);
                }

                b.BoxOn(0, y, -1.7f, 5.8f, 0.05f, 0.45f, Mat.Wood, 0.01f);
            }

            b.Strut(new Vector3(-2.9f, 0, -1.7f), new Vector3(-0.95f, h, -1.7f), 0.04f, Mat.Rust);
            b.Strut(new Vector3(0.95f, 0, -1.7f), new Vector3(2.9f, h, -1.7f), 0.04f, Mat.Rust);
            b.Box(new Vector3(-1.9f, h * 0.55f, 1.75f), new Vector3(2.0f, h * 0.85f, 0.04f), Mat.TarpBlue, 0.01f);
            b.Box(new Vector3(2.95f, h * 0.5f, 0.6f), new Vector3(0.04f, h * 0.7f, 2.2f), Mat.TarpBlue, 0.01f);
            b.BoxOn(1.8f, 0, -2.4f, 0.9f, 0.55f, 0.7f, Mat.Wood, 0.04f);
            b.BoxOn(-1.8f, 0, -2.3f, 1.2f, 0.12f, 1.0f, Mat.Wood, 0.01f);
            for (int i = 0; i < 3; i++)
            {
                b.CylinderX(new Vector3(-1.8f, 0.2f + (i * 0.12f), -2.3f), 0.05f, 2.2f, 6, Mat.Rust, 0.01f);
            }

            return b.Mesh;
        }

        private static void Generator(MeshBuilder b, Model m, int level, uint seed)
        {
            Props.Container(b, m, new Vector3(0, 0, 0.6f), 6.0f, 2.6f, 2.44f, Mat.OliveSteel, false, seed, "POWER");
            for (int i = 0; i < 4; i++)
            {
                b.Box(new Vector3(1.0f + (i * 0.42f), 1.35f, -0.65f), new Vector3(0.32f, 1.2f, 0.06f), Mat.DarkSteel, 0.02f);
                for (int k = 0; k < 6; k++)
                {
                    b.Box(new Vector3(1.0f + (i * 0.42f), 0.85f + (k * 0.2f), -0.69f), new Vector3(0.28f, 0.03f, 0.04f), Mat.Rubber, 0f);
                }
            }

            Shapes.Hazard(b, -2.6f, -0.9f, 0.2f, 0.36f, -0.68f, 8);
            b.Frustum(new Vector3(-2.1f, 2.6f, 1.2f), 0.14f, 0.12f, 1.6f, 10, Mat.Rust, 0.02f);
            b.Frustum(new Vector3(-1.6f, 2.6f, 1.2f), 0.12f, 0.1f, 1.2f, 10, Mat.Rust, 0.02f);
            b.Frustum(new Vector3(-2.1f, 4.2f, 1.2f), 0.18f, 0.18f, 0.12f, 10, Mat.Rust, 0.01f, false);
            b.BoxOn(1.4f, 2.6f, 0.6f, 1.4f, 0.5f, 1.2f, Mat.DarkSteel, 0.05f);
            m.Parts.Add(Fan(new Vector3(1.4f, 3.1f, 0.6f), 0.48f, 400f, seed));
            Props.Lamp(b, m, new Vector3(-0.3f, 2.45f, -0.75f), true, 1.4f, LightRole.Status);
            Drums(b, new Vector3(-2.7f, 0, -1.6f), 3, seed);

            if (level >= 2)
            {
                b.BoxOn(-2.0f, 0, 2.65f, 0.3f, 0.5f, 0.8f, Mat.DarkSteel, 0.03f);
                b.BoxOn(0.8f, 0, 2.65f, 0.3f, 0.5f, 0.8f, Mat.DarkSteel, 0.03f);
                b.CylinderX(new Vector3(-0.6f, 1.05f, 2.65f), 0.58f, 3.6f, 16, Mat.Rust, 0.06f);
                b.CylinderX(new Vector3(-0.6f, 1.05f, 2.65f), 0.61f, 0.14f, 16, Mat.PaintRed, 0.01f);
                b.Strut(new Vector3(-1.2f, 1.4f, 2.1f), new Vector3(-1.2f, 0.4f, 1.9f), 0.08f, Mat.Rubber);
            }

            if (level >= 3)
            {
                Vector3 tb = new Vector3(2.6f, 0, 2.6f);
                b.Frustum(tb, 0.24f, 0.13f, 8.5f, 12, Mat.PaintWhite, 0.02f);
                b.BoxOn(tb.X, 8.5f, tb.Z + 0.15f, 0.4f, 0.4f, 1.0f, Mat.PaintWhite, 0.06f);
                var rotor = new MeshBuilder(seed + 5);
                rotor.Frustum(Vector3.Zero, 0.18f, 0.1f, 0.3f, 10, Mat.DarkSteel, 0.02f);
                for (int i = 0; i < 3; i++)
                {
                    rotor.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(i * 120f)));
                    rotor.Box(new Vector3(0, 1.75f, 0), new Vector3(0.24f, 3.3f, 0.05f), Mat.PaintWhite, 0.03f);
                    rotor.Pop();
                }

                m.Parts.Add(new AnimPart(rotor.Mesh, new Vector3(tb.X, 8.7f, tb.Z - 0.4f), AnimKind.SpinZ, 70f));
            }

            if (level >= 4)
            {
                b.BoxOn(-0.9f, 2.6f, 0.6f, 2.4f, 0.9f, 1.6f, Mat.OliveSteel, 0.06f);
                b.Frustum(new Vector3(-0.9f, 3.5f, 0.6f), 0.12f, 0.1f, 0.9f, 10, Mat.Rust, 0.02f);
                Props.Railing(b, new Vector3(-2.9f, 2.6f, -0.55f), new Vector3(2.9f, 2.6f, -0.55f));
                Props.Stairs(b, new Vector3(3.3f, 0, 0.0f), 2.6f, 90f);
            }

            if (level >= 5)
            {
                b.BoxOn(-2.3f, 0, -2.5f, 1.3f, 1.3f, 1.0f, Mat.DarkSteel, 0.06f);
                for (int i = 0; i < 3; i++)
                {
                    b.Frustum(new Vector3(-2.7f + (i * 0.4f), 1.3f, -2.5f), 0.08f, 0.05f, 0.5f, 8, Mat.PaintWhite, 0.01f);
                }

                Shapes.Lamp(b, m, new Vector3(2.6f, 9.2f, 2.75f), Mat.LampRed, Model.Red, 1.2f, 3.5f, LightRole.Status, 0.16f);
            }

            Beacon(b, m, new Vector3(2.7f, 2.85f, -0.4f));
        }

        private static void Compute(MeshBuilder b, Model m, int level, uint seed)
        {
            Props.Container(b, m, new Vector3(0, 0, 0.6f), 6.0f, 2.6f, 2.44f, Mat.SandSteel, true, seed, "SERVER");
            for (int r = 0; r < 2; r++)
            {
                for (int i = 0; i < 7; i++)
                {
                    b.Box(new Vector3(-1.4f + (r * 0.3f), 0.45f + (i * 0.26f), -0.6f), new Vector3(0.06f, 0.04f, 0.02f), i % 4 == 3 ? Mat.LampAmber : Mat.LampPhosphor, 0f);
                }
            }

            m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(-1.2f, 1.4f, -1.4f)), Model.Phosphor, 1.0f, 3.5f, LightRole.Status));
            for (int i = 0; i < 2; i++)
            {
                b.BoxOn(0.9f + (i * 1.2f), 2.6f, 0.6f, 1.0f, 0.5f, 1.0f, Mat.DarkSteel, 0.05f);
                m.Parts.Add(Fan(new Vector3(0.9f + (i * 1.2f), 3.1f, 0.6f), 0.36f, 520f, seed + (uint)i));
            }

            b.Frustum(new Vector3(-2.6f, 2.6f, 1.5f), 0.05f, 0.03f, 2.6f, 6, Mat.DarkSteel, 0.01f);
            b.Capsule(new Vector3(-2.75f, 0.2f, 2.0f), new Vector3(-2.75f, 2.5f, 2.0f), 0.07f, Mat.Rubber, 6);
            Crates(b, new Vector3(2.4f, 0, -2.2f), seed);
            CableReel(b, new Vector3(-2.6f, 0, -2.2f));

            if (level >= 2)
            {
                b.BoxOn(3.4f, 0, 0.8f, 0.6f, 2.1f, 1.1f, Mat.DarkSteel, 0.04f);
                for (int i = 0; i < 6; i++)
                {
                    b.Box(new Vector3(3.08f, 0.5f + (i * 0.27f), 0.5f), new Vector3(0.02f, 0.04f, 0.07f), Mat.LampPhosphor, 0f);
                }
            }

            if (level >= 3)
            {
                b.Frustum(new Vector3(2.2f, 2.6f, 1.4f), 0.1f, 0.08f, 1.0f, 10, Mat.DarkSteel, 0.02f);
                var dish = new MeshBuilder(seed + 11);
                dish.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-38f)));
                dish.Frustum(new Vector3(0, 0.05f, 0), 0.12f, 0.95f, 0.32f, 18, Mat.PaintWhite, 0.02f, true, Mat.Concrete);
                dish.Strut(new Vector3(0, 0.1f, 0), new Vector3(0, 0.9f, 0), 0.04f, Mat.DarkSteel);
                dish.Pop();
                m.Parts.Add(new AnimPart(dish.Mesh, new Vector3(2.2f, 3.6f, 1.4f), AnimKind.SweepY, 0.05f, 70f));
            }

            if (level >= 4)
            {
                b.Push(Matrix4x4.CreateRotationY(MeshBuilder.Deg(6f)) * Matrix4x4.CreateTranslation(new Vector3(-0.6f, 2.6f, 0.75f)));
                Props.Container(b, m, Vector3.Zero, 4.4f, 2.4f, 2.2f, Mat.TarpBlue, true, seed + 2, "UPLINK");
                b.Pop();
                Props.Stairs(b, new Vector3(3.3f, 0, -0.4f), 2.6f, 90f);
            }

            if (level >= 5)
            {
                b.Frustum(new Vector3(-2.8f, 0, 2.6f), 0.12f, 0.06f, 9.5f, 8, Mat.DarkSteel, 0.01f);
                for (int i = 1; i <= 6; i++)
                {
                    b.Strut(new Vector3(-3.15f, i * 1.4f, 2.6f), new Vector3(-2.45f, i * 1.4f, 2.6f), 0.03f, Mat.DarkSteel);
                }

                Shapes.Lamp(b, m, new Vector3(-2.8f, 9.6f, 2.6f), Mat.LampPhosphor, Model.Phosphor, 1.4f, 3.5f, LightRole.Status, 0.16f);
            }

            Beacon(b, m, new Vector3(2.8f, 2.85f, -0.4f));
        }

        private static void Habitat(MeshBuilder b, Model m, int level, uint seed)
        {
            Props.Container(b, m, new Vector3(0, 0, 0.6f), 6.0f, 2.6f, 2.44f, Mat.Rust, true, seed, "MED BAY");
            b.Box(new Vector3(0.6f, 2.66f, 0.6f), new Vector3(1.8f, 0.02f, 0.5f), Mat.PaintGreen, 0f);
            b.Box(new Vector3(0.6f, 2.66f, 0.6f), new Vector3(0.5f, 0.02f, 1.8f), Mat.PaintGreen, 0f);
            Drums(b, new Vector3(2.7f, 0, 2.5f), 2, seed);
            b.BoxOn(-2.6f, 0, -2.2f, 1.0f, 0.75f, 0.7f, Mat.Wood, 0.04f);
            b.BoxOn(-1.4f, 0, -2.3f, 1.0f, 0.12f, 0.5f, Mat.DarkSteel, 0.01f);
            b.BoxOn(-1.4f, 0.75f, -2.3f, 1.0f, 0.05f, 0.5f, Mat.Wood, 0.01f);
            foreach (int s in new[] { -1, 1 })
            {
                b.Strut(new Vector3(-1.4f + (s * 0.45f), 0, -2.3f), new Vector3(-1.4f + (s * 0.45f), 0.75f, -2.3f), 0.04f, Mat.DarkSteel);
            }

            if (level >= 2)
            {
                Vector3 t = new Vector3(2.8f, 0, -1.0f);
                foreach (int sx in new[] { -1, 1 })
                {
                    foreach (int sz in new[] { -1, 1 })
                    {
                        b.Strut(t + new Vector3(sx * 0.45f, 0, sz * 0.45f), t + new Vector3(sx * 0.38f, 2.6f, sz * 0.38f), 0.07f, Mat.DarkSteel);
                    }
                }

                b.Frustum(t + new Vector3(0, 2.6f, 0), 0.68f, 0.66f, 1.4f, 16, Mat.OliveSteel, 0.05f);
                b.Frustum(t + new Vector3(0, 4.0f, 0), 0.66f, 0.15f, 0.3f, 16, Mat.OliveSteel, 0.02f);
                b.Capsule(t + new Vector3(-0.3f, 2.6f, 0), new Vector3(2.0f, 1.8f, -0.6f), 0.05f, Mat.DarkSteel, 6);
            }

            if (level >= 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(18f)) * Matrix4x4.CreateTranslation(new Vector3(-2.0f + (i * 1.3f), 2.9f, 0.9f)));
                    b.Box(Vector3.Zero, new Vector3(1.2f, 0.05f, 1.5f), Mat.Glass, 0.01f);
                    b.Pop();
                }

                b.Strut(new Vector3(-2.9f, 2.5f, -1.3f), new Vector3(-2.9f, 0, -2.0f), 0.05f, Mat.DarkSteel);
                b.Strut(new Vector3(-0.3f, 2.5f, -1.3f), new Vector3(-0.3f, 0, -2.0f), 0.05f, Mat.DarkSteel);
                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-14f)) * Matrix4x4.CreateTranslation(new Vector3(-1.6f, 2.3f, -1.15f)));
                b.Box(Vector3.Zero, new Vector3(2.9f, 0.04f, 1.2f), Mat.TarpBlue, 0.01f);
                b.Pop();
            }

            if (level >= 4)
            {
                b.Push(Matrix4x4.CreateRotationY(MeshBuilder.Deg(-5f)) * Matrix4x4.CreateTranslation(new Vector3(-0.4f, 2.6f, 1.0f)));
                Props.Container(b, m, Vector3.Zero, 4.4f, 2.4f, 2.2f, Mat.OliveSteel, true, seed + 4, "BUNKS");
                b.Pop();
                Props.Stairs(b, new Vector3(-3.3f, 0, -0.4f), 2.6f, -90f);
            }

            if (level >= 5)
            {
                Shapes.ArchX(b, new Vector3(1.0f, 0, -2.5f), 0.7f, 2.6f, 8, Mat.Glass, Mat.DarkSteel);
                Shapes.Lamp(b, m, new Vector3(1.0f, 0.6f, -2.5f), Mat.LampPhosphor, Model.Phosphor, 0.9f, 3f, LightRole.Status, 0.08f);
            }

            Beacon(b, m, new Vector3(-2.8f, 2.85f, -0.4f));
        }

        private static void Battery(MeshBuilder b, Model m, int level, uint seed)
        {
            float[] px = { -2.8f, 0f, 2.8f };
            foreach (float x in px)
            {
                b.BoxOn(x, 0, -1.9f, 0.18f, 2.7f, 0.18f, Mat.DarkSteel, 0.02f);
                b.BoxOn(x, 0, 2.2f, 0.18f, 3.2f, 0.18f, Mat.DarkSteel, 0.02f);
            }

            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-7f)) * Matrix4x4.CreateTranslation(new Vector3(0, 2.9f, 0.15f)));
            b.Box(Vector3.Zero, new Vector3(6.2f, 0.06f, 4.7f), Mat.Rust, 0.01f);
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-90f)));
            b.Corrugated(-3.1f, 3.1f, -2.35f, 2.35f, -0.04f, Mat.Rust, 0.35f, 0.04f);
            b.Pop();
            int panels = Math.Min(level + 1, 6);
            for (int i = 0; i < panels; i++)
            {
                b.Box(new Vector3(-2.1f + ((i % 3) * 2.1f), 0.12f, -1.0f + ((i / 3) * 2.0f)), new Vector3(1.9f, 0.05f, 1.8f), Mat.Glass, 0.01f);
            }

            b.Pop();

            int cabinets = Math.Min(4 + (level * 2), 14);
            for (int i = 0; i < cabinets; i++)
            {
                int row = i / 7;
                int col = i % 7;
                float x = -2.4f + (col * 0.8f);
                float z = -0.6f + (row * 1.3f);
                b.BoxOn(x, 0, z, 0.66f, 1.3f, 0.7f, Mat.DarkSteel, 0.04f);
                b.Box(new Vector3(x - 0.14f, 1.34f, z), new Vector3(0.1f, 0.08f, 0.1f), Mat.Copper, 0.02f);
                b.Box(new Vector3(x + 0.14f, 1.34f, z), new Vector3(0.1f, 0.08f, 0.1f), Mat.Copper, 0.02f);
                b.Box(new Vector3(x, 1.0f, z - 0.36f), new Vector3(0.1f, 0.06f, 0.02f), Mat.LampPhosphor, 0f);
                b.Box(new Vector3(x, 0.6f, z - 0.36f), new Vector3(0.4f, 0.3f, 0.02f), Mat.PaintWhite, 0f);
            }

            m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(0, 1.3f, -1.6f)), Model.Phosphor, 0.9f, 4f, LightRole.Status));
            b.Strut(new Vector3(-2.8f, 1.5f, -0.6f), new Vector3(2.8f, 1.5f, -0.6f), 0.06f, Mat.Rubber);
            b.BoxOn(2.2f, 1.6f, -1.95f, 1.6f, 0.5f, 0.05f, Mat.DarkSteel, 0.01f);
            Props.Stencil(b, "CELLS", new Vector3(1.6f, 1.68f, -1.99f), 0.055f, Mat.PaintWhite);
            Props.Lamp(b, m, new Vector3(-1.4f, 2.55f, -1.7f), true, 1.2f, LightRole.Status);

            if (level >= 3)
            {
                Capacitor(b, new Vector3(2.8f, 0, -2.7f), 2.4f);
            }

            if (level >= 4)
            {
                Capacitor(b, new Vector3(-2.8f, 0, -2.7f), 2.0f);
            }

            if (level >= 5)
            {
                Shapes.Hazard(b, -1.2f, 1.2f, 0.1f, 0.5f, -2.95f, 8);
            }

            Beacon(b, m, new Vector3(2.8f, 2.9f, -1.9f));
        }

        private static void Turret(MeshBuilder b, Model m, int level, uint seed)
        {
            b.BoxOn(0, 0, 0, 3.6f, 0.55f, 3.6f, Mat.Concrete, 0.08f);
            Shapes.SandbagRing(b, new Vector3(0, 0.55f, 0), 1.75f, 18, level >= 5 ? 3 : 2, 70f, 40f);
            b.Frustum(new Vector3(0, 0.55f, 0), 0.8f, 0.65f, 0.85f, 14, Mat.DarkSteel, 0.05f);

            var head = new MeshBuilder(seed + 7) { GroundOffset = 1.4f + PadTop };
            float w = level >= 3 ? 1.4f : 1.2f;
            head.BoxOn(0, 0, 0.1f, w, 0.72f, 1.2f, Mat.OliveSteel, 0.1f);
            head.BoxOn(0, 0.72f, 0.2f, w * 0.6f, 0.22f, 0.7f, Mat.OliveSteel, 0.06f);
            int barrels = level >= 5 ? 4 : (level >= 2 ? 2 : 1);
            for (int i = 0; i < barrels; i++)
            {
                float x = barrels == 1 ? 0f : (-0.16f * (barrels - 1)) + (i * 0.32f);
                float y = barrels == 4 ? (i % 2 == 0 ? 0.25f : 0.45f) : 0.36f;
                float xx = x * (barrels == 4 ? 0.6f : 1f);
                head.CylinderZ(new Vector3(xx, y, -1.05f), 0.075f, 1.5f, 10, Mat.DarkSteel, 0.01f);
                head.CylinderZ(new Vector3(xx, y, -1.75f), 0.11f, 0.22f, 10, Mat.DarkSteel, 0.02f);
            }

            head.Box(new Vector3(w * 0.45f, 0.64f, -0.52f), new Vector3(0.22f, 0.2f, 0.22f), Mat.DarkSteel, 0.03f);
            head.Box(new Vector3(w * 0.45f, 0.64f, -0.64f), new Vector3(0.12f, 0.08f, 0.02f), Mat.LampRed, 0f);
            if (level >= 3)
            {
                head.Box(new Vector3(-w * 0.56f, 0.36f, -0.1f), new Vector3(0.09f, 0.62f, 1.15f), Mat.DarkSteel, 0.02f);
                head.Box(new Vector3(w * 0.56f, 0.36f, -0.1f), new Vector3(0.09f, 0.62f, 1.15f), Mat.DarkSteel, 0.02f);
            }

            if (level >= 4)
            {
                head.Frustum(new Vector3(0, 0.94f, 0.45f), 0.05f, 0.05f, 0.45f, 8, Mat.DarkSteel, 0.01f);
                head.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-70f)) * Matrix4x4.CreateTranslation(new Vector3(0, 1.42f, 0.45f)));
                head.Frustum(Vector3.Zero, 0.05f, 0.44f, 0.13f, 14, Mat.PaintWhite, 0.01f);
                head.Pop();
            }

            m.Parts.Add(new AnimPart(head.Mesh, new Vector3(0, 1.4f, 0), AnimKind.SweepY, 0.08f + (level * 0.01f), 55f));
            m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(0.5f, 2.1f, -0.9f)), Model.Red, 0.7f, 2.2f, LightRole.Status));
            Crates(b, new Vector3(2.3f, 0, 2.2f), seed);
            Drums(b, new Vector3(-2.6f, 0, 2.0f), 2, seed);
            Beacon(b, m, new Vector3(-1.5f, 1.5f, -1.4f));
        }

        private static void CableReel(MeshBuilder b, Vector3 at)
        {
            b.CylinderZ(at + new Vector3(0, 0.45f, 0), 0.45f, 0.08f, 14, Mat.Wood, 0.01f);
            b.CylinderZ(at + new Vector3(0, 0.45f, 0.45f), 0.45f, 0.08f, 14, Mat.Wood, 0.01f);
            b.CylinderZ(at + new Vector3(0, 0.45f, 0.22f), 0.32f, 0.4f, 14, Mat.Rubber, 0.02f);
        }

        private static void Drums(MeshBuilder b, Vector3 at, int count, uint seed)
        {
            for (int i = 0; i < count; i++)
            {
                Mat mat = i % 3 == 0 ? Mat.PaintRed : (i % 3 == 1 ? Mat.OliveSteel : Mat.Rust);
                b.Frustum(at + new Vector3((i % 2) * 0.62f, 0, (i / 2) * 0.62f), 0.29f, 0.29f, 0.88f, 10, mat, 0.04f);
                b.Frustum(at + new Vector3((i % 2) * 0.62f, 0.3f, (i / 2) * 0.62f), 0.3f, 0.3f, 0.05f, 10, Mat.DarkSteel, 0.01f, false);
            }
        }

        private static void Crates(MeshBuilder b, Vector3 at, uint seed)
        {
            var rng = new ArtRandom(seed + 23);
            b.BoxOn(at.X, at.Y, at.Z, 0.8f, 0.6f, 0.8f, Mat.Wood, 0.05f);
            b.Push(new Vector3(at.X + 0.05f, at.Y + 0.6f, at.Z), rng.Range(-20f, 20f));
            b.BoxOn(0, 0, 0, 0.6f, 0.45f, 0.6f, Mat.Wood, 0.05f);
            b.Pop();
            b.BoxOn(at.X - 0.75f, at.Y, at.Z + 0.1f, 0.5f, 0.4f, 0.7f, Mat.OliveSteel, 0.04f);
        }

        private static void Capacitor(MeshBuilder b, Vector3 baseCenter, float height)
        {
            b.Frustum(baseCenter, 0.42f, 0.32f, 0.3f, 10, Mat.Concrete, 0.04f);
            int rings = (int)(height / 0.3f);
            for (int i = 0; i < rings; i++)
            {
                float y = 0.3f + (i * 0.3f);
                b.Frustum(baseCenter + new Vector3(0, y, 0), 0.22f, 0.22f, 0.2f, 10, Mat.PaintWhite, 0.02f);
                b.Frustum(baseCenter + new Vector3(0, y + 0.2f, 0), 0.34f, 0.34f, 0.06f, 10, Mat.Copper, 0.01f);
            }

            b.Box(baseCenter + new Vector3(0, 0.36f + (rings * 0.3f), 0), new Vector3(0.2f, 0.2f, 0.2f), Mat.Copper, 0.04f);
        }

        private static AnimPart Fan(Vector3 pivot, float radius, float degPerSecond, uint seed)
        {
            var f = new MeshBuilder(seed + 101) { GroundOffset = pivot.Y };
            f.Frustum(Vector3.Zero, radius, radius, 0.05f, 12, Mat.DarkSteel, 0.01f, false);
            for (int i = 0; i < 4; i++)
            {
                f.Push(Matrix4x4.CreateRotationY(MeshBuilder.Deg(i * 45f)));
                f.Box(new Vector3(0, 0.07f, 0), new Vector3(radius * 1.8f, 0.03f, radius * 0.22f), Mat.SandSteel, 0.005f);
                f.Pop();
            }

            f.Frustum(new Vector3(0, 0.05f, 0), 0.08f, 0.06f, 0.08f, 8, Mat.DarkSteel, 0.01f);
            return new AnimPart(f.Mesh, pivot, AnimKind.SpinY, degPerSecond);
        }

        /// <summary>Amber beacon that blinks while the facility runs unmanned (SPEC-003 rule 5).</summary>
        private static void Beacon(MeshBuilder b, Model m, Vector3 p)
        {
            m.Lights.Add(new LightSpec(b.TransformPoint(p), Model.Amber, 2.4f, 3.8f, LightRole.Beacon));
        }
    }
}
