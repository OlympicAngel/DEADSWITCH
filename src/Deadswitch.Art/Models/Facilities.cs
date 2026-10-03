using System;
using System.Numerics;
using Deadswitch.Art.Geometry;
using Deadswitch.Sim.State;

namespace Deadswitch.Art.Models
{
    /// <summary>
    /// Facility models (SPEC-003 rule 4). Local space: origin at the pad's top center, +Y up, front toward -Z
    /// (the drone camera). Every level adds parts, so growth is visible at a glance.
    /// </summary>
    public static class Facilities
    {
        /// <summary>Pad top height above the terrain; facilities sit on it.</summary>
        public const float PadTop = 0.25f;

        public static Model Build(FacilityKind kind, int level, uint seed)
        {
            var m = new Model();
            var b = new MeshBuilder(seed) { GroundOffset = PadTop };
            level = Math.Max(1, level);
            switch (kind)
            {
                case FacilityKind.Generator:
                    Generator(b, m, level, seed);
                    break;
                case FacilityKind.ServerRack:
                    ServerRack(b, m, level, seed);
                    break;
                case FacilityKind.LifeSupport:
                    LifeSupport(b, m, level, seed);
                    break;
                case FacilityKind.BatteryBank:
                    Battery(b, m, level);
                    break;
                case FacilityKind.Turret:
                    Turret(b, m, level, seed);
                    break;
            }

            m.Static = b.Mesh;
            return m;
        }

        /// <summary>Concrete slot pad with hazard corner marks, origin at terrain level.</summary>
        public static MeshData Pad(uint seed)
        {
            var b = new MeshBuilder(seed);
            b.BoxOn(0, -0.2f, 0, 4.8f, 0.45f, 4.8f, Mat.Concrete, 0.12f);
            const float t = PadTop + 0.005f;
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    b.Box(new Vector3(sx * 2.05f, t, sz * 1.72f), new Vector3(0.14f, 0.02f, 0.6f), Mat.Paint, 0.01f);
                    b.Box(new Vector3(sx * 1.72f, t, sz * 2.05f), new Vector3(0.6f, 0.02f, 0.14f), Mat.Paint, 0.01f);
                }
            }

            return b.Mesh;
        }

        /// <summary>Scaffolding and a tarp half-cover around a facility under construction.</summary>
        public static MeshData Scaffold(float height, uint seed)
        {
            var b = new MeshBuilder(seed) { GroundOffset = PadTop };
            float h = Math.Max(1.6f, height);
            float[] xs = { -1.9f, 0f, 1.9f };
            float[] zs = { -1.9f, 1.9f };
            foreach (float x in xs)
            {
                foreach (float z in zs)
                {
                    b.Strut(new Vector3(x, 0, z), new Vector3(x, h, z), 0.07f, Mat.Rust);
                }
            }

            for (float y = 0.7f; y < h; y += 0.9f)
            {
                foreach (float z in zs)
                {
                    b.Strut(new Vector3(-1.9f, y, z), new Vector3(1.9f, y, z), 0.06f, Mat.Rust);
                }

                foreach (float x in xs)
                {
                    b.Strut(new Vector3(x, y, -1.9f), new Vector3(x, y, 1.9f), 0.06f, Mat.Rust);
                }

                b.BoxOn(0, y, -1.9f, 3.8f, 0.05f, 0.35f, Mat.Wood, 0.01f);
            }

            b.Strut(new Vector3(-1.9f, 0, -1.9f), new Vector3(0, h, -1.9f), 0.05f, Mat.Rust);
            b.Box(new Vector3(-0.95f, h * 0.55f, 1.95f), new Vector3(1.9f, h * 0.8f, 0.04f), Mat.Tarp, 0.01f);
            b.BoxOn(1.2f, 0, -1.2f, 0.8f, 0.5f, 0.6f, Mat.Wood, 0.05f);
            b.BoxOn(-1.3f, 0, -0.9f, 0.6f, 0.35f, 0.9f, Mat.Concrete, 0.04f);
            return b.Mesh;
        }

        private static void Generator(MeshBuilder b, Model m, int level, uint seed)
        {
            // L1: diesel genset on a skid
            b.BoxOn(0.4f, 0, -0.7f, 2.8f, 0.18f, 1.6f, Mat.DarkSteel, 0.04f);
            b.BoxOn(0.4f, 0.18f, -0.7f, 2.3f, 1.2f, 1.3f, Mat.OliveSteel, 0.12f);
            b.BoxOn(-0.75f, 0.3f, -0.7f, 0.12f, 0.95f, 1.1f, Mat.DarkSteel, 0.03f);
            for (int i = 0; i < 6; i++)
            {
                b.Box(new Vector3(-0.83f, 0.45f + (i * 0.15f), -0.7f), new Vector3(0.04f, 0.06f, 1.0f), Mat.DarkSteel, 0.01f);
            }

            b.BoxOn(1.0f, 0.45f, -1.38f, 0.6f, 0.55f, 0.08f, Mat.SandSteel, 0.03f);
            Shapes.Lamp(b, m, new Vector3(1.15f, 0.88f, -1.43f), Mat.LampAmber, Model.Amber, 0.9f, 2.5f, LightRole.Status, 0.1f);
            b.Box(new Vector3(0.85f, 0.65f, -1.43f), new Vector3(0.22f, 0.14f, 0.02f), Mat.Screen, 0.005f);
            b.Frustum(new Vector3(-0.2f, 1.38f, -0.4f), 0.1f, 0.1f, 0.9f, 8, Mat.Rust, 0.02f);
            b.CylinderX(new Vector3(-0.2f, 1.62f, -0.4f), 0.16f, 0.6f, 10, Mat.Rust);
            Shapes.Hazard(b, -0.6f, 1.5f, 0.2f, 0.32f, -1.36f, 9);
            m.Parts.Add(Fan(new Vector3(0.9f, 1.38f, -0.7f), 0.42f, 360f, seed));

            if (level >= 2)
            {
                // fuel tank on cradles behind the genset
                b.BoxOn(-0.6f, 0, 1.15f, 0.25f, 0.45f, 1.0f, Mat.DarkSteel, 0.03f);
                b.BoxOn(1.2f, 0, 1.15f, 0.25f, 0.45f, 1.0f, Mat.DarkSteel, 0.03f);
                b.CylinderX(new Vector3(0.3f, 0.85f, 1.15f), 0.5f, 2.6f, 14, Mat.Rust, 0.06f);
                b.CylinderX(new Vector3(0.3f, 0.85f, 1.15f), 0.52f, 0.12f, 14, Mat.Paint, 0.01f);
                b.Strut(new Vector3(-0.2f, 0.9f, 0.6f), new Vector3(-0.2f, 0.9f, -0.05f), 0.08f, Mat.Rubber);
            }

            if (level >= 3)
            {
                // transformer with insulators and cables
                b.BoxOn(-1.55f, 0, -0.75f, 0.95f, 1.0f, 0.95f, Mat.DarkSteel, 0.08f);
                for (int i = 0; i < 3; i++)
                {
                    float x = -1.85f + (i * 0.3f);
                    b.Frustum(new Vector3(x, 1.0f, -0.75f), 0.08f, 0.05f, 0.42f, 8, Mat.Concrete, 0.01f);
                    b.Box(new Vector3(x, 1.45f, -0.75f), new Vector3(0.07f, 0.07f, 0.07f), Mat.Copper, 0.01f);
                }

                b.Strut(new Vector3(-1.55f, 1.45f, -0.75f), new Vector3(-0.6f, 1.2f, -0.75f), 0.04f, Mat.Rubber);
            }

            if (level >= 4)
            {
                // cooling tower with a fan
                b.Frustum(new Vector3(-1.35f, 0, 1.2f), 0.85f, 0.6f, 2.0f, 10, Mat.Concrete, 0.06f);
                b.Frustum(new Vector3(-1.35f, 2.0f, 1.2f), 0.66f, 0.66f, 0.12f, 10, Mat.Rust, 0.02f, false);
                m.Parts.Add(Fan(new Vector3(-1.35f, 2.05f, 1.2f), 0.5f, 520f, seed + 3));
                b.Strut(new Vector3(-0.8f, 0.6f, 1.0f), new Vector3(-0.2f, 0.6f, 0.9f), 0.12f, Mat.DarkSteel);
            }

            if (level >= 5)
            {
                // tall stack with a red aviation beacon and a catwalk
                b.Frustum(new Vector3(1.7f, 0, 1.6f), 0.28f, 0.2f, 3.6f, 10, Mat.Rust, 0.03f);
                b.Frustum(new Vector3(1.7f, 2.4f, 1.6f), 0.31f, 0.31f, 0.18f, 10, Mat.Paint, 0.01f, false);
                Shapes.Lamp(b, m, new Vector3(1.7f, 3.7f, 1.6f), Mat.LampRed, Model.Red, 1.2f, 3.5f, LightRole.Status, 0.14f);
                b.BoxOn(0.4f, 1.38f, -0.7f, 2.4f, 0.05f, 1.6f, Mat.DarkSteel, 0.01f);
                for (int i = 0; i < 5; i++)
                {
                    b.Strut(new Vector3(-0.8f + (i * 0.6f), 1.43f, -1.5f), new Vector3(-0.8f + (i * 0.6f), 1.9f, -1.5f), 0.04f, Mat.Paint);
                }

                b.Strut(new Vector3(-0.8f, 1.9f, -1.5f), new Vector3(1.6f, 1.9f, -1.5f), 0.04f, Mat.Paint);
            }

            Beacon(b, m, new Vector3(-0.55f, 1.45f, -1.25f));
        }

        private static void ServerRack(MeshBuilder b, Model m, int level, uint seed)
        {
            Container(b, m, new Vector3(0.3f, 0, -0.55f), 3.0f, 1.6f, 1.7f, Mat.SandSteel);
            b.BoxOn(0.95f, 1.6f, -0.55f, 1.0f, 0.42f, 0.9f, Mat.DarkSteel, 0.06f);
            m.Parts.Add(Fan(new Vector3(0.95f, 2.02f, -0.55f), 0.32f, 480f, seed));

            if (level >= 2)
            {
                // second cabinet block + cable tray
                b.BoxOn(-1.65f, 0, -0.5f, 0.75f, 1.8f, 1.1f, Mat.DarkSteel, 0.06f);
                for (int i = 0; i < 5; i++)
                {
                    Shapes.Lamp(b, m, new Vector3(-1.65f, 0.4f + (i * 0.28f), -1.07f), Mat.LampPhosphor, Model.Phosphor, 0f, 0f, LightRole.Status, 0.05f);
                }

                m.Lights.Add(new LightSpec(new Vector3(-1.65f, 1.0f, -1.3f), Model.Phosphor, 0.6f, 2f, LightRole.Status));
                b.BoxOn(-0.5f, 1.85f, -0.55f, 2.0f, 0.06f, 0.3f, Mat.DarkSteel, 0.01f);
            }

            if (level >= 3)
            {
                // satellite dish on a mast, slowly scanning
                b.Frustum(new Vector3(-1.2f, 0, 1.25f), 0.12f, 0.1f, 2.0f, 8, Mat.DarkSteel, 0.02f);
                var dish = new MeshBuilder(seed + 11) { GroundOffset = PadTop + 2.0f };
                dish.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-35f)));
                dish.Frustum(new Vector3(0, 0.05f, 0), 0.12f, 0.75f, 0.28f, 14, Mat.SandSteel, 0.02f, true, Mat.Concrete);
                dish.Strut(new Vector3(0, 0.1f, 0), new Vector3(0, 0.75f, 0), 0.04f, Mat.DarkSteel);
                dish.Pop();
                m.Parts.Add(new AnimPart(dish.Mesh, new Vector3(-1.2f, 2.0f, 1.25f), AnimKind.SweepY, 0.05f, 70f));
            }

            if (level >= 4)
            {
                // second container stacked across the first
                Container(b, m, new Vector3(0.3f, 1.6f, 0.55f), 2.6f, 1.3f, 1.3f, Mat.OliveSteel);
            }

            if (level >= 5)
            {
                b.Frustum(new Vector3(1.75f, 0, 1.4f), 0.09f, 0.05f, 4.2f, 6, Mat.DarkSteel, 0.01f);
                for (int i = 1; i <= 3; i++)
                {
                    b.Strut(new Vector3(1.45f, i * 1.0f, 1.4f), new Vector3(2.05f, i * 1.0f, 1.4f), 0.03f, Mat.DarkSteel);
                }

                Shapes.Lamp(b, m, new Vector3(1.75f, 4.25f, 1.4f), Mat.LampPhosphor, Model.Phosphor, 1.2f, 3f, LightRole.Status, 0.12f);
                for (int i = 0; i < 4; i++)
                {
                    b.BoxOn(-1.9f + (i * 0.32f), 0, 1.75f, 0.06f, 1.4f, 0.8f, Mat.DarkSteel, 0.01f);
                }
            }

            Beacon(b, m, new Vector3(1.6f, 1.75f, -1.3f));
        }

        private static void LifeSupport(MeshBuilder b, Model m, int level, uint seed)
        {
            b.BoxOn(-0.2f, 0, -0.5f, 3.2f, 0.12f, 2.0f, Mat.Wood, 0.02f);
            Shapes.ArchX(b, new Vector3(-0.2f, 0.12f, -0.5f), 0.95f, 3.0f, 8, Mat.Tarp, Mat.Tarp);
            b.BoxOn(-1.72f, 0.12f, -0.5f, 0.06f, 0.8f, 0.55f, Mat.DarkSteel, 0.01f);
            Shapes.Lamp(b, m, new Vector3(-1.76f, 0.75f, -0.85f), Mat.LampAmber, Model.Amber, 1.0f, 3f, LightRole.Status, 0.1f);
            b.Frustum(new Vector3(1.75f, 0, -0.9f), 0.5f, 0.5f, 1.5f, 12, Mat.OliveSteel, 0.05f);
            b.Frustum(new Vector3(1.75f, 0.6f, -0.9f), 0.52f, 0.52f, 0.12f, 12, Mat.Rust, 0.01f, false);
            b.BoxOn(1.7f, 0, 0.6f, 0.9f, 0.8f, 0.8f, Mat.SandSteel, 0.06f);
            m.Parts.Add(Fan(new Vector3(1.7f, 0.8f, 0.6f), 0.3f, 420f, seed));

            if (level >= 2)
            {
                b.Frustum(new Vector3(1.75f, 0, 1.6f), 0.4f, 0.4f, 1.1f, 12, Mat.OliveSteel, 0.05f);
                b.Strut(new Vector3(1.75f, 0.6f, 1.25f), new Vector3(1.75f, 0.6f, -0.45f), 0.08f, Mat.DarkSteel);
                b.Strut(new Vector3(1.3f, 0.3f, -0.9f), new Vector3(1.2f, 0.3f, -0.6f), 0.08f, Mat.DarkSteel);
            }

            if (level >= 3)
            {
                b.BoxOn(-0.6f, 0, 1.25f, 2.2f, 0.12f, 1.3f, Mat.Wood, 0.02f);
                Shapes.ArchX(b, new Vector3(-0.6f, 0.12f, 1.25f), 0.6f, 2.0f, 6, Mat.Tarp, Mat.Tarp);
            }

            if (level >= 4)
            {
                // solar panels on frames, catching the cold light
                for (int i = 0; i < 3; i++)
                {
                    float x = -1.6f + (i * 1.05f);
                    b.Strut(new Vector3(x, 0, -2.0f), new Vector3(x, 0.55f, -2.0f), 0.05f, Mat.DarkSteel);
                    b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(28f)) * Matrix4x4.CreateTranslation(new Vector3(x, 0.65f, -2.0f)));
                    b.Box(Vector3.Zero, new Vector3(0.95f, 0.04f, 0.55f), Mat.Glass, 0.01f);
                    b.Pop();
                }
            }

            if (level >= 5)
            {
                Shapes.ArchX(b, new Vector3(-0.2f, 0.12f, -0.5f), 1.25f, 1.0f, 8, Mat.Glass, Mat.DarkSteel);
                Shapes.Lamp(b, m, new Vector3(-0.2f, 1.2f, -0.5f), Mat.LampPhosphor, Model.Phosphor, 0.9f, 3.5f, LightRole.Status, 0.08f);
            }

            Beacon(b, m, new Vector3(1.75f, 1.6f, -0.9f));
        }

        private static void Battery(MeshBuilder b, Model m, int level)
        {
            int cabinets = Math.Min(4 + ((level - 1) * 2), 10);
            for (int i = 0; i < cabinets; i++)
            {
                int row = i / 5;
                int col = i % 5;
                float x = -1.6f + (col * 0.7f);
                float z = -1.1f + (row * 1.05f);
                b.BoxOn(x, 0, z, 0.58f, 1.1f, 0.62f, Mat.DarkSteel, 0.06f);
                b.Box(new Vector3(x - 0.12f, 1.14f, z), new Vector3(0.1f, 0.08f, 0.1f), Mat.Copper, 0.02f);
                b.Box(new Vector3(x + 0.12f, 1.14f, z), new Vector3(0.1f, 0.08f, 0.1f), Mat.Copper, 0.02f);
                Shapes.Lamp(b, m, new Vector3(x, 0.85f, z - 0.32f), Mat.LampPhosphor, Model.Phosphor, 0f, 0f, LightRole.Status, 0.05f);
                b.Box(new Vector3(x, 0.55f, z - 0.32f), new Vector3(0.36f, 0.3f, 0.02f), Mat.SandSteel, 0.005f);
            }

            m.Lights.Add(new LightSpec(new Vector3(-0.2f, 1.0f, -1.7f), Model.Phosphor, 0.7f, 3f, LightRole.Status));
            b.Strut(new Vector3(-1.6f, 1.2f, -1.1f), new Vector3(1.2f, 1.2f, -1.1f), 0.05f, Mat.Rubber);

            if (level >= 3)
            {
                CapacitorTower(b, new Vector3(1.6f, 0, 1.3f), 2.2f);
            }

            if (level >= 4)
            {
                CapacitorTower(b, new Vector3(0.6f, 0, 1.55f), 1.8f);
            }

            if (level >= 5)
            {
                b.BoxOn(-1.3f, 0, 1.55f, 1.2f, 1.3f, 0.9f, Mat.OliveSteel, 0.08f);
                Shapes.Hazard(b, -1.85f, -0.75f, 0.9f, 1.05f, 1.08f, 6);
                Shapes.Lamp(b, m, new Vector3(-1.3f, 1.45f, 1.55f), Mat.LampAmber, Model.Amber, 0.8f, 2.5f, LightRole.Status, 0.1f);
            }

            Beacon(b, m, new Vector3(1.65f, 1.25f, -1.1f));
        }

        private static void Turret(MeshBuilder b, Model m, int level, uint seed)
        {
            Shapes.SandbagRing(b, Vector3.Zero, 1.75f, 18, level >= 5 ? 3 : 2, 60f, 40f);
            b.Frustum(Vector3.Zero, 0.75f, 0.6f, 0.75f, 10, Mat.DarkSteel, 0.05f);

            var head = new MeshBuilder(seed + 7) { GroundOffset = PadTop + 0.75f };
            float w = level >= 3 ? 1.25f : 1.05f;
            head.BoxOn(0, 0, 0.05f, w, 0.62f, 1.05f, Mat.OliveSteel, 0.1f);
            head.BoxOn(0, 0.62f, 0.15f, w * 0.6f, 0.2f, 0.6f, Mat.OliveSteel, 0.05f);
            int barrels = level >= 5 ? 4 : (level >= 2 ? 2 : 1);
            for (int i = 0; i < barrels; i++)
            {
                float x = barrels == 1 ? 0f : (-0.18f * (barrels - 1)) + (i * 0.36f);
                float y = barrels == 4 ? (i % 2 == 0 ? 0.22f : 0.42f) : 0.32f;
                head.CylinderZ(new Vector3(x * (barrels == 4 ? 0.6f : 1f), y, -0.95f), 0.07f, 1.3f, 8, Mat.DarkSteel, 0.01f);
                head.CylinderZ(new Vector3(x * (barrels == 4 ? 0.6f : 1f), y, -1.55f), 0.1f, 0.18f, 8, Mat.DarkSteel, 0.02f);
            }

            head.Box(new Vector3(w * 0.45f, 0.55f, -0.45f), new Vector3(0.2f, 0.18f, 0.2f), Mat.DarkSteel, 0.03f);
            head.Box(new Vector3(w * 0.45f, 0.55f, -0.56f), new Vector3(0.1f, 0.08f, 0.02f), Mat.LampRed, 0.005f);
            if (level >= 3)
            {
                head.Box(new Vector3(-w * 0.55f, 0.3f, -0.1f), new Vector3(0.08f, 0.55f, 1.0f), Mat.DarkSteel, 0.02f);
                head.Box(new Vector3(w * 0.55f, 0.3f, -0.1f), new Vector3(0.08f, 0.55f, 1.0f), Mat.DarkSteel, 0.02f);
            }

            if (level >= 4)
            {
                head.Frustum(new Vector3(0, 0.82f, 0.35f), 0.05f, 0.05f, 0.4f, 6, Mat.DarkSteel, 0.01f);
                head.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-70f)) * Matrix4x4.CreateTranslation(new Vector3(0, 1.25f, 0.35f)));
                head.Frustum(Vector3.Zero, 0.05f, 0.38f, 0.12f, 10, Mat.SandSteel, 0.01f);
                head.Pop();
            }

            m.Parts.Add(new AnimPart(head.Mesh, new Vector3(0, 0.75f, 0), AnimKind.SweepY, 0.08f + (level * 0.01f), 55f));
            m.Lights.Add(new LightSpec(new Vector3(0.5f, 1.35f, -0.8f), Model.Red, 0.6f, 2.0f, LightRole.Status));

            if (level >= 3)
            {
                b.BoxOn(1.3f, 0, 1.25f, 0.7f, 0.42f, 0.5f, Mat.OliveSteel, 0.05f);
                b.BoxOn(1.3f, 0.42f, 1.25f, 0.6f, 0.35f, 0.45f, Mat.OliveSteel, 0.05f);
            }

            Beacon(b, m, new Vector3(-1.3f, 0.75f, -1.0f));
        }

        /// <summary>Corrugated container module with a door and a row of status LEDs on its front.</summary>
        private static void Container(MeshBuilder b, Model m, Vector3 baseCenter, float sx, float sy, float sz, Mat mat)
        {
            b.BoxOn(baseCenter.X, baseCenter.Y, baseCenter.Z, sx, sy, sz, mat, 0.08f);
            float front = baseCenter.Z - (sz * 0.5f) - 0.02f;
            int ribs = (int)(sx / 0.28f);
            for (int i = 1; i < ribs; i++)
            {
                float x = baseCenter.X - (sx * 0.5f) + (i * sx / ribs);
                b.Box(new Vector3(x, baseCenter.Y + (sy * 0.5f), front), new Vector3(0.06f, sy * 0.86f, 0.04f), mat, 0.01f);
            }

            b.Box(new Vector3(baseCenter.X - (sx * 0.28f), baseCenter.Y + (sy * 0.42f), front - 0.03f), new Vector3(0.62f, sy * 0.78f, 0.04f), Mat.DarkSteel, 0.02f);
            for (int i = 0; i < 6; i++)
            {
                b.Box(new Vector3(baseCenter.X + (sx * 0.05f) + (i * 0.14f), baseCenter.Y + (sy * 0.72f), front - 0.04f), new Vector3(0.07f, 0.05f, 0.02f), i % 3 == 2 ? Mat.LampAmber : Mat.LampPhosphor, 0.005f);
            }

            m.Lights.Add(new LightSpec(new Vector3(baseCenter.X + (sx * 0.2f), baseCenter.Y + (sy * 0.7f), front - 0.4f), Model.Phosphor, 0.55f, 2.2f, LightRole.Status));
        }

        private static void CapacitorTower(MeshBuilder b, Vector3 baseCenter, float height)
        {
            b.Frustum(baseCenter, 0.4f, 0.3f, 0.3f, 10, Mat.Concrete, 0.04f);
            int rings = (int)(height / 0.3f);
            for (int i = 0; i < rings; i++)
            {
                float y = 0.3f + (i * 0.3f);
                b.Frustum(baseCenter + new Vector3(0, y, 0), 0.22f, 0.22f, 0.2f, 10, Mat.Concrete, 0.02f);
                b.Frustum(baseCenter + new Vector3(0, y + 0.2f, 0), 0.32f, 0.32f, 0.06f, 10, Mat.Copper, 0.01f);
            }

            b.Box(baseCenter + new Vector3(0, 0.36f + (rings * 0.3f), 0), new Vector3(0.18f, 0.18f, 0.18f), Mat.Copper, 0.04f);
        }

        /// <summary>Spinning fan disc with blades, pivot at its base center.</summary>
        private static AnimPart Fan(Vector3 pivot, float radius, float degPerSecond, uint seed)
        {
            var f = new MeshBuilder(seed + 101) { GroundOffset = PadTop + pivot.Y };
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
            b.Frustum(p - new Vector3(0, 0.12f, 0), 0.07f, 0.07f, 0.12f, 8, Mat.DarkSteel, 0.01f);
            m.Lights.Add(new LightSpec(p + new Vector3(0, 0.08f, 0), Model.Amber, 2.2f, 3.5f, LightRole.Beacon));
        }
    }
}
