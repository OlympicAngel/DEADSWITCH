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

        // ---------- Power: engine hall, exhaust stacks, fuel farm, transformer yard ----------
        private static void Generator(MeshBuilder b, Model m, int level, uint seed)
        {
            var rng = new ArtRandom(seed + 1);

            // engine hall on a plinth, genset bay under a lean-to in front
            b.BoxOn(-0.5f, 0, 1.45f, 5.4f, 0.2f, 2.8f, Mat.Concrete, 0.05f);
            KitModules.ContainerBlock(b, m, new Vector3(-0.5f, 0.2f, 1.45f), 5.0f, Mat.OliveSteel, true, "POWER", rng, 1, false);
            KitModules.LeanTo(b, new Vector3(-1.2f, 0, 0.23f), 3.4f, 1.9f, 2.75f, 2.3f, Mat.Rust);
            KitParts.Genset(b, new Vector3(-1.35f, 0, -0.85f), 0f);
            Props.Lamp(b, m, new Vector3(-0.3f, 2.2f, -0.2f), true, 1.4f, LightRole.Status);

            // roof: radiator with a fan, louvered vent, railing
            b.BoxOn(1.25f, 2.8f, 1.5f, 1.4f, 0.55f, 1.4f, Mat.DarkSteel, 0.04f);
            for (int i = 0; i < 5; i++)
            {
                b.Box(new Vector3(1.25f, 2.92f + (i * 0.09f), 0.79f), new Vector3(1.3f, 0.03f, 0.03f), Mat.Rust, 0f);
            }

            m.Parts.Add(Fan(new Vector3(1.25f, 3.35f, 1.5f), 0.5f, 400f, seed));
            KitParts.Vent(b, new Vector3(1.25f, 2.8f, 0.5f), 0.7f, 0.45f);

            // exhaust stacks behind the hall, fed through the back wall
            Stack(b, m, new Vector3(-2.35f, 0, 2.95f), 0.24f, 6.4f, level >= 5);
            Stack(b, m, new Vector3(-1.5f, 0, 3.0f), 0.18f, 5.0f, false);
            KitParts.Pipe(b, new[] { new Vector3(-1.9f, 1.9f, 2.6f), new Vector3(-1.9f, 1.9f, 2.95f), new Vector3(-2.12f, 1.9f, 2.95f) }, 0.1f, Mat.Rust);
            KitParts.Pipe(b, new[] { new Vector3(-1.1f, 1.6f, 2.6f), new Vector3(-1.1f, 1.6f, 3.0f), new Vector3(-1.32f, 1.6f, 3.0f) }, 0.08f, Mat.Rust);

            // fuel tank feeding the hall over the roof
            KitParts.TankV(b, new Vector3(2.65f, 0, 1.55f), 0.5f, 2.3f, Mat.SandSteel, seed + 3);
            KitParts.Pipe(b, new[] { new Vector3(2.65f, 2.5f, 1.55f), new Vector3(2.65f, 3.5f, 1.55f), new Vector3(2.65f, 3.5f, 2.4f), new Vector3(0.1f, 3.5f, 2.4f), new Vector3(0.1f, 2.8f, 2.4f) }, 0.06f, Mat.DarkSteel);
            b.Strut(new Vector3(1.2f, 2.8f, 2.4f), new Vector3(1.2f, 3.44f, 2.4f), 0.05f, Mat.DarkSteel);

            // transformer yard: fenced pad, cables up to the hall
            KitParts.Transformer(b, new Vector3(2.1f, 0, -1.75f), 0.9f);
            KitModules.Fence(b, new Vector3(0.95f, 0, -2.95f), new Vector3(3.15f, 0, -2.95f), 1.5f);
            KitModules.Fence(b, new Vector3(3.15f, 0, -2.95f), new Vector3(3.15f, 0, -0.75f), 1.5f);
            for (int i = 0; i < 3; i++)
            {
                KitParts.Cable(b, new Vector3(1.8f + (i * 0.3f), 1.95f, -1.75f), new Vector3(1.55f + (i * 0.15f), 2.7f, 0.2f), 0.25f, 0.025f);
            }

            KitModules.Barrels(b, new Vector3(-2.75f, 0, -2.55f), 3, rng);
            KitModules.Pallet(b, new Vector3(0.3f, 0, -2.5f), 12f, 2);

            if (level >= 2)
            {
                KitParts.TankH(b, new Vector3(1.15f, 0.85f, -0.6f), 0.48f, 1.9f, Mat.Rust);
                KitParts.Pipe(b, new[] { new Vector3(0.2f, 1.0f, -0.6f), new Vector3(-0.2f, 1.0f, -0.6f), new Vector3(-0.2f, 1.2f, -0.85f) }, 0.05f, Mat.DarkSteel);
                KitModules.CrateStack(b, new Vector3(-0.9f, 0, -2.55f), rng);
            }

            if (level >= 3)
            {
                // wind turbine on a lattice mast behind the tank
                Vector3 tb = new Vector3(2.75f, 0, 2.85f);
                KitParts.Mast(b, m, tb, 8.6f, false);
                b.BoxOn(tb.X, 8.6f, tb.Z + 0.1f, 0.45f, 0.45f, 1.1f, Mat.PaintWhite, 0.08f);
                b.Box(new Vector3(tb.X, 9.1f, tb.Z + 0.55f), new Vector3(0.04f, 0.5f, 0.3f), Mat.PaintWhite, 0.01f);
                var rotor = new MeshBuilder(seed + 5);
                rotor.Frustum(Vector3.Zero, 0.2f, 0.1f, 0.35f, 10, Mat.DarkSteel, 0.02f);
                for (int i = 0; i < 3; i++)
                {
                    rotor.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(i * 120f)));
                    rotor.Box(new Vector3(0, 1.75f, 0), new Vector3(0.26f, 3.3f, 0.05f), Mat.PaintWhite, 0.03f);
                    rotor.Box(new Vector3(0, 3.2f, 0.01f), new Vector3(0.2f, 0.35f, 0.055f), Mat.PaintRed, 0.01f);
                    rotor.Pop();
                }

                m.Parts.Add(new AnimPart(rotor.Mesh, new Vector3(tb.X, 8.82f, tb.Z - 0.5f), AnimKind.SpinZ, 70f));
                KitParts.Cable(b, tb + new Vector3(0, 7.5f, 0), new Vector3(2.0f, 2.85f, 2.4f), 0.6f);
            }

            if (level >= 4)
            {
                // control room stacked on the hall, ladder up the door end, roof railing
                b.Push(new Vector3(-1.15f, 2.8f, 1.45f), 3f);
                KitModules.ContainerBlock(b, m, Vector3.Zero, 3.4f, Mat.Rust, true, "CONTROL", rng, 1, true);
                b.Pop();
                KitParts.Ladder(b, new Vector3(-3.12f, 0, 0.9f), 2.8f, 90f);
                Props.Railing(b, new Vector3(0.65f, 2.8f, 0.3f), new Vector3(1.95f, 2.8f, 0.3f));
            }

            if (level >= 5)
            {
                // a third, heavier stack and a pole feeding the transformer yard
                Stack(b, m, new Vector3(-0.55f, 0, 3.05f), 0.3f, 7.8f, true);
                KitParts.Pipe(b, new[] { new Vector3(-0.55f, 2.2f, 2.6f), new Vector3(-0.55f, 2.2f, 2.75f) }, 0.12f, Mat.Rust);
                Vector3 pole = new Vector3(0.5f, 0, -2.9f);
                b.Frustum(pole, 0.13f, 0.1f, 6.2f, 8, Mat.Wood, 0.02f);
                b.Strut(pole + new Vector3(-0.9f, 5.7f, 0), pole + new Vector3(0.9f, 5.7f, 0), 0.1f, Mat.Wood);
                for (int i = 0; i < 3; i++)
                {
                    Vector3 ins = pole + new Vector3(-0.7f + (i * 0.7f), 5.76f, 0);
                    b.Frustum(ins, 0.06f, 0.05f, 0.22f, 8, Mat.PaintWhite, 0.01f);
                    KitParts.Cable(b, ins + new Vector3(0, 0.2f, 0), new Vector3(1.8f + (i * 0.3f), 1.95f, -1.75f), 0.6f, 0.025f);
                }

                KitParts.Spotlight(b, m, pole + new Vector3(0, 4.8f, -0.2f), 180f);
            }

            Beacon(b, m, new Vector3(2.0f, 3.1f, 0.25f));
        }

        // ---------- Compute: server container, antenna mast, dishes, chillers, outdoor racks ----------
        private static void Compute(MeshBuilder b, Model m, int level, uint seed)
        {
            var rng = new ArtRandom(seed + 2);
            b.BoxOn(0.3f, 0, 1.3f, 5.8f, 0.2f, 2.8f, Mat.Concrete, 0.05f);
            KitModules.ContainerBlock(b, m, new Vector3(0.3f, 0.2f, 1.3f), 5.4f, Mat.SandSteel, true, "SERVER", rng, 1, false);
            for (int r = 0; r < 2; r++)
            {
                for (int i = 0; i < 6; i++)
                {
                    b.Box(new Vector3(2.4f + (r * 0.16f), 0.7f + (i * 0.22f), 0.0f), new Vector3(0.06f, 0.04f, 0.02f), i % 4 == 3 ? Mat.LampAmber : Mat.LampPhosphor, 0f);
                }
            }

            // outdoor rack row under a lean-to, phosphor glow on the yard
            KitModules.LeanTo(b, new Vector3(-1.25f, 0, 0.08f), 2.5f, 1.7f, 2.75f, 2.3f, Mat.Rust);
            int racks = level >= 2 ? 4 : 2;
            for (int i = 0; i < racks; i++)
            {
                Rack(b, new Vector3(-2.1f + (i * 0.62f), 0, -0.55f));
            }

            m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(-1.4f, 1.3f, -1.4f)), Model.Phosphor, 1.1f, 4f, LightRole.Status));
            KitParts.Truss(b, new Vector3(-2.4f, 2.25f, -0.2f), new Vector3(-0.1f, 2.25f, -0.2f), 0.14f, 0.025f, Mat.DarkSteel);

            // ground chiller piped into the container
            Chiller(b, m, new Vector3(2.15f, 0, -1.35f), 2, seed + 7);
            KitParts.Pipe(b, new[] { new Vector3(1.55f, 0.5f, -1.0f), new Vector3(1.2f, 0.5f, -1.0f), new Vector3(1.2f, 0.5f, -0.3f), new Vector3(1.2f, 1.1f, -0.05f) }, 0.07f, Mat.DarkSteel);
            KitParts.Pipe(b, new[] { new Vector3(1.55f, 0.8f, -1.3f), new Vector3(0.95f, 0.8f, -1.3f), new Vector3(0.95f, 0.8f, -0.3f), new Vector3(0.95f, 1.3f, -0.05f) }, 0.07f, Mat.DarkSteel);

            // antenna mast with panel antennas, cable bundles down to the roof
            float mastH = level >= 5 ? 12.5f : 7.5f;
            Vector3 mb = new Vector3(-2.7f, 0, 2.75f);
            KitParts.Mast(b, m, mb, mastH, true);
            for (int i = 0; i < (level >= 5 ? 6 : 3); i++)
            {
                float a = MeshBuilder.Deg(i * 120f + 30f);
                float y = mastH - 1.6f - ((i / 3) * 2.4f);
                Vector3 c = mb + new Vector3((float)Math.Cos(a) * 0.32f, y, (float)Math.Sin(a) * 0.32f);
                b.Push(c, -i * 120f - 30f);
                b.Box(Vector3.Zero, new Vector3(0.08f, 1.1f, 0.3f), Mat.PaintWhite, 0.02f);
                b.Pop();
            }

            KitParts.Cable(b, mb + new Vector3(0.1f, mastH - 2f, 0), new Vector3(-1.9f, 2.85f, 1.8f), 0.5f, 0.04f);
            KitParts.Cable(b, mb + new Vector3(0.1f, mastH - 2.3f, 0.05f), new Vector3(-1.8f, 2.85f, 2.0f), 0.6f, 0.04f);
            KitParts.Truss(b, new Vector3(-2.3f, 2.85f, 1.9f), new Vector3(-0.4f, 2.85f, 1.9f), 0.12f, 0.025f, Mat.DarkSteel);

            if (level < 4)
            {
                Condenser(b, m, new Vector3(0.9f, 2.8f, 1.35f), seed + 11);
                Condenser(b, m, new Vector3(2.2f, 2.8f, 1.35f), seed + 12);
            }

            KitModules.CrateStack(b, new Vector3(-2.3f, 0, -2.5f), rng);
            b.CylinderZ(new Vector3(0.2f, 0.45f, -2.45f), 0.45f, 0.08f, 14, Mat.Wood, 0.01f);
            b.CylinderZ(new Vector3(0.2f, 0.45f, -2.0f), 0.45f, 0.08f, 14, Mat.Wood, 0.01f);
            b.CylinderZ(new Vector3(0.2f, 0.45f, -2.22f), 0.32f, 0.4f, 14, Mat.Rubber, 0.02f);

            if (level >= 3)
            {
                Dish(b, m, new Vector3(-1.35f, 2.8f, 1.25f), 0.95f, seed + 13);
            }

            if (level >= 4)
            {
                b.Push(new Vector3(1.3f, 2.8f, 1.45f), -4f);
                KitModules.ContainerBlock(b, m, Vector3.Zero, 3.1f, Mat.OliveSteel, true, "UPLINK", rng, 1, false);
                Condenser(b, m, new Vector3(-0.6f, 2.6f, 0.1f), seed + 14);
                KitParts.AcUnit(b, new Vector3(0.8f, 2.6f, 0.2f));
                b.Pop();
                KitParts.Ladder(b, new Vector3(3.1f, 0, 1.0f), 2.8f, -90f);
                Props.Railing(b, new Vector3(-0.3f, 2.8f, 0.15f), new Vector3(-0.3f, 2.8f, 2.4f));
            }

            if (level >= 5)
            {
                Dish(b, m, new Vector3(1.9f, 5.4f, 1.6f), 0.75f, seed + 15);
            }

            Beacon(b, m, new Vector3(3.0f, 3.0f, 0.1f));
        }

        // ---------- Habitat: stacked homes, porch, balconies, laundry, water tower, greenhouse ----------
        private static void Habitat(MeshBuilder b, Model m, int level, uint seed)
        {
            var rng = new ArtRandom(seed + 3);
            b.BoxOn(-0.2f, 0, 1.35f, 6.0f, 0.2f, 2.8f, Mat.Concrete, 0.05f);
            KitModules.ContainerBlock(b, m, new Vector3(-0.2f, 0.2f, 1.35f), 5.6f, Mat.Rust, true, "MED BAY", rng, 2, false);
            KitModules.LeanTo(b, new Vector3(-1.1f, 0, 0.13f), 2.9f, 1.6f, 2.75f, 2.3f, Mat.OliveSteel);
            b.BoxOn(-1.1f, 0, -0.65f, 2.9f, 0.12f, 1.5f, Mat.Wood, 0.01f);
            Props.Lamp(b, m, new Vector3(-1.1f, 2.15f, -0.4f), true, 1.3f, LightRole.Status);

            // clinic sign: white board, green cross
            b.Strut(new Vector3(1.4f, 2.8f, 0.2f), new Vector3(1.4f, 3.7f, 0.2f), 0.06f, Mat.DarkSteel);
            b.Strut(new Vector3(2.2f, 2.8f, 0.2f), new Vector3(2.2f, 3.7f, 0.2f), 0.06f, Mat.DarkSteel);
            b.Box(new Vector3(1.8f, 3.55f, 0.16f), new Vector3(1.0f, 0.7f, 0.05f), Mat.PaintWhite, 0.02f);
            b.Box(new Vector3(1.8f, 3.55f, 0.12f), new Vector3(0.5f, 0.15f, 0.03f), Mat.PaintGreen, 0f);
            b.Box(new Vector3(1.8f, 3.55f, 0.12f), new Vector3(0.15f, 0.5f, 0.03f), Mat.PaintGreen, 0f);

            // roof: header tank, solar water heater, junk
            KitParts.TankV(b, new Vector3(-2.4f, 2.8f, 1.7f), 0.42f, 0.9f, Mat.OliveSteel, seed + 4);
            SolarPanel(b, new Vector3(-0.9f, 2.8f, 1.6f), 1.5f, 1.1f, 25f);
            KitParts.AcUnit(b, new Vector3(0.6f, 2.8f, 1.9f));
            KitModules.TarpPile(b, new Vector3(1.9f, 2.8f, 1.8f), 1.0f, rng);

            // laundry line from the porch to a pole
            Vector3 pole = new Vector3(2.85f, 0, -2.3f);
            b.Strut(pole, pole + new Vector3(0, 2.3f, 0), 0.06f, Mat.Wood);
            Laundry(b, new Vector3(0.35f, 2.15f, -1.45f), pole + new Vector3(0, 2.2f, 0), rng);

            KitModules.Workbench(b, m, new Vector3(-2.0f, 0, -2.3f), 0f);
            KitModules.Barrels(b, new Vector3(2.55f, 0, -0.5f), 2, rng);
            KitModules.Pallet(b, new Vector3(0.9f, 0, -2.5f), -8f, 1);

            if (level >= 2)
            {
                // bunk container stacked on top with a cantilevered balcony and stairs up the side
                b.Push(new Vector3(0.45f, 2.8f, 1.45f), -3f);
                KitModules.ContainerBlock(b, m, Vector3.Zero, 4.3f, Mat.OliveSteel, true, "BUNKS", rng, 1, true);
                b.Pop();
                b.BoxOn(0.6f, 2.8f, -0.2f, 4.6f, 0.1f, 0.85f, Mat.DarkSteel, 0.01f);
                for (int i = 0; i < 4; i++)
                {
                    float x = -1.5f + (i * 1.4f);
                    b.Strut(new Vector3(x, 2.0f, 0.12f), new Vector3(x, 2.8f, -0.6f), 0.06f, Mat.DarkSteel);
                }

                Props.Railing(b, new Vector3(-1.7f, 2.9f, -0.6f), new Vector3(2.6f, 2.9f, -0.6f));
                Props.Stairs(b, new Vector3(3.05f, 0, 2.4f), 2.8f, 0f);
                Laundry(b, new Vector3(-1.6f, 3.8f, -0.55f), new Vector3(0.4f, 3.75f, -0.55f), rng);
            }

            if (level >= 3)
            {
                // greenhouse tunnel replaces the front-left yard corner
                Shapes.ArchX(b, new Vector3(-1.95f, 0, -2.45f), 0.68f, 2.3f, 8, Mat.Glass, Mat.DarkSteel);
                for (int i = 0; i < 5; i++)
                {
                    b.Sphere(new Vector3(-2.85f + (i * 0.45f), 0.15f, -2.45f), new Vector3(0.2f, 0.28f, 0.2f), 3, 6, Mat.Foliage);
                }

                Shapes.Lamp(b, m, new Vector3(-1.95f, 0.55f, -2.45f), Mat.LampPhosphor, Model.Phosphor, 0.7f, 2.5f, LightRole.Status, 0.07f);
            }

            if (level >= 4)
            {
                // water tower behind the stack
                ElevatedTank(b, new Vector3(-2.55f, 0, 3.05f), 3.6f, 0.62f, 1.5f, Mat.SandSteel, seed + 5);
                KitParts.Pipe(b, new[] { new Vector3(-2.55f, 3.6f, 2.5f), new Vector3(-2.55f, 3.0f, 2.3f), new Vector3(-2.4f, 2.8f, 2.1f) }, 0.05f, Mat.DarkSteel);
            }

            if (level >= 5)
            {
                // rooftop shack on the bunks, aerial, string of lamps
                KitModules.Shed(b, new Vector3(0.9f, 5.4f, 1.6f), 2.2f, 1.6f, 1.9f, 2.2f, Mat.Rust, false);
                b.BoxOn(0.9f, 5.4f, 2.3f, 2.2f, 1.9f, 0.06f, Mat.Rust, 0.01f);
                b.Frustum(new Vector3(-0.6f, 5.4f, 2.1f), 0.04f, 0.02f, 3.2f, 6, Mat.DarkSteel, 0.01f);
                for (int i = 0; i < 4; i++)
                {
                    Shapes.Lamp(b, m, new Vector3(-1.4f + (i * 1.2f), 4.15f - (i % 2 * 0.06f), -0.62f), Mat.LampAmber, Model.Amber, 0.5f, 2.5f, LightRole.Ambient, 0.06f);
                }
            }

            Beacon(b, m, new Vector3(-2.9f, 3.0f, 0.1f));
        }

        // ---------- Battery: solar canopy, cell cabinets, capacitor towers, bus-bar gantry ----------
        private static void Battery(MeshBuilder b, Model m, int level, uint seed)
        {
            var rng = new ArtRandom(seed + 4);
            b.BoxOn(0, 0, 0.35f, 6.3f, 0.15f, 5.2f, Mat.Concrete, 0.05f);
            const float y0 = 0.15f;
            const float zFront = -1.55f;
            const float zBack = 2.45f;
            const float hFront = 3.0f;
            const float hBack = 3.6f;
            KitModules.Shed(b, new Vector3(0, y0, (zFront + zBack) * 0.5f), 6.0f, zBack - zFront, hFront, hBack, Mat.Rust);

            // solar panels following the roof pitch
            float pitch = (float)Math.Atan2(hBack - hFront, zBack - zFront);
            int panels = Math.Min(2 + level, 6);
            for (int i = 0; i < panels; i++)
            {
                float z = zFront + 0.9f + ((i / 3) * 2.0f);
                float y = y0 + hFront + 0.12f + ((z - zFront) * (hBack - hFront) / (zBack - zFront)) + 0.12f;
                b.Push(Matrix4x4.CreateRotationX(-pitch) * Matrix4x4.CreateTranslation(new Vector3(-1.95f + ((i % 3) * 1.95f), y, z)));
                b.Box(new Vector3(0, -0.05f, 0), new Vector3(1.85f, 0.06f, 1.75f), Mat.DarkSteel, 0.01f);
                b.Box(Vector3.Zero, new Vector3(1.75f, 0.04f, 1.65f), Mat.Glass, 0.005f);
                b.Pop();
            }

            // cell cabinets in rows, bus cable along the front beam
            int cabinets = Math.Min(4 + (level * 2), 14);
            for (int i = 0; i < cabinets; i++)
            {
                int row = i / 7;
                float x = -2.4f + ((i % 7) * 0.8f);
                float z = -0.5f + (row * 1.5f);
                b.BoxOn(x, y0, z, 0.66f, 1.4f, 0.7f, Mat.DarkSteel, 0.04f);
                b.Box(new Vector3(x - 0.14f, y0 + 1.44f, z), new Vector3(0.1f, 0.08f, 0.1f), Mat.Copper, 0.02f);
                b.Box(new Vector3(x + 0.14f, y0 + 1.44f, z), new Vector3(0.1f, 0.08f, 0.1f), Mat.Copper, 0.02f);
                b.Box(new Vector3(x, y0 + 1.1f, z - 0.36f), new Vector3(0.1f, 0.06f, 0.02f), Mat.LampPhosphor, 0f);
                b.Box(new Vector3(x, y0 + 0.65f, z - 0.36f), new Vector3(0.4f, 0.3f, 0.02f), Mat.PaintWhite, 0f);
                b.Strut(new Vector3(x, y0 + 1.48f, z), new Vector3(x, y0 + hFront - 0.2f, zFront + 0.3f), 0.025f, Mat.Rubber);
            }

            KitParts.Truss(b, new Vector3(-2.9f, y0 + hFront - 0.35f, zFront + 0.3f), new Vector3(2.9f, y0 + hFront - 0.35f, zFront + 0.3f), 0.16f, 0.025f, Mat.DarkSteel);
            m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(0, 1.4f, -1.4f)), Model.Phosphor, 0.9f, 4f, LightRole.Status));
            b.BoxOn(1.9f, y0 + 1.8f, zFront - 0.05f, 1.7f, 0.5f, 0.05f, Mat.DarkSteel, 0.01f);
            Props.Stencil(b, "CELLS", new Vector3(1.3f, y0 + 1.88f, zFront - 0.09f), 0.055f, Mat.PaintWhite);
            Props.Lamp(b, m, new Vector3(-1.4f, y0 + hFront - 0.15f, zFront + 0.1f), true, 1.2f, LightRole.Status);

            // inverter and the first capacitor tower out front, fenced
            KitParts.Transformer(b, new Vector3(2.35f, y0, -2.45f), 0.7f);
            Capacitor(b, new Vector3(-2.5f, y0, -2.45f), 2.4f);
            KitModules.Fence(b, new Vector3(-3.15f, 0, -3.1f), new Vector3(-1.4f, 0, -3.1f), 1.4f);
            Shapes.Hazard(b, -2.9f, -1.7f, 0.9f, 1.1f, -3.12f, 6);
            KitModules.Barrels(b, new Vector3(-0.6f, y0, -2.6f), 2, rng);

            if (level >= 3)
            {
                // bus-bar gantry: truss on two posts with insulator strings, drops to the towers
                foreach (float x in new[] { -2.95f, 2.95f })
                {
                    KitParts.IBeam(b, new Vector3(x, y0, -2.95f), new Vector3(x, 4.4f, -2.95f), 0.16f, 0.14f, Mat.DarkSteel);
                }

                KitParts.Truss(b, new Vector3(-2.95f, 4.0f, -2.95f), new Vector3(2.95f, 4.0f, -2.95f), 0.4f, 0.04f, Mat.DarkSteel);
                for (int i = 0; i < 3; i++)
                {
                    Vector3 ins = new Vector3(-1.2f + (i * 1.2f), 4.0f, -2.95f);
                    for (int k = 0; k < 3; k++)
                    {
                        b.Frustum(ins - new Vector3(0, 0.12f * (k + 1), 0), 0.08f, 0.05f, 0.08f, 8, Mat.PaintWhite, 0.01f);
                    }

                    KitParts.Cable(b, ins - new Vector3(0, 0.4f, 0), new Vector3(i == 0 ? -2.5f : 2.35f, i == 0 ? 3.0f : 1.9f, -2.45f), 0.3f, 0.025f, Mat.Copper);
                }
            }

            if (level >= 4)
            {
                Capacitor(b, new Vector3(-1.75f, y0, -2.55f), 2.0f);
            }

            if (level >= 5)
            {
                Capacitor(b, new Vector3(1.0f, y0, -2.6f), 2.8f);
                Shapes.Lamp(b, m, new Vector3(0, 4.55f, -2.95f), Mat.LampRed, Model.Red, 0.9f, 3.5f, LightRole.Status, 0.12f);
            }

            Beacon(b, m, new Vector3(2.85f, 3.1f, -1.55f));
        }

        // ---------- Turret: octagonal emplacement, raised gun tower, radar, tank traps ----------
        private static void Turret(MeshBuilder b, Model m, int level, uint seed)
        {
            var rng = new ArtRandom(seed + 5);

            // octagonal concrete emplacement with a broken corner and a ladder
            b.Frustum(Vector3.Zero, 2.35f, 2.15f, 1.1f, 8, Mat.Concrete, 0.08f, true, Mat.ConcreteDark);
            b.Frustum(new Vector3(0, 1.1f, 0), 2.2f, 2.2f, 0.12f, 8, Mat.ConcreteDark, 0.03f);
            KitParts.Rebar(b, new Vector3(1.6f, 1.1f, 1.4f), new Vector3(0.4f, 0.8f, 0.3f), 5, seed + 9);
            KitParts.Ladder(b, new Vector3(0.6f, 0, 2.3f), 1.2f, 180f);
            for (int i = 0; i < 3; i++)
            {
                b.Box(new Vector3(-1.2f + (i * 1.2f), 0.6f, -2.2f), new Vector3(0.6f, 0.1f, 0.06f), Mat.ConcreteDark, 0f);
            }

            float top = 1.22f;
            if (level >= 3)
            {
                // steel gun tower on the emplacement
                const float hw = 1.0f;
                top = 3.6f;
                foreach (int sx in new[] { -1, 1 })
                {
                    foreach (int sz in new[] { -1, 1 })
                    {
                        KitParts.IBeam(b, new Vector3(sx * hw, 1.22f, sz * hw), new Vector3(sx * hw, top, sz * hw), 0.18f, 0.16f, Mat.DarkSteel);
                    }

                    b.Strut(new Vector3(sx * hw, 1.4f, -hw), new Vector3(sx * hw, top - 0.2f, hw), 0.06f, Mat.DarkSteel);
                    b.Strut(new Vector3(-hw, 1.4f, sx * hw), new Vector3(hw, top - 0.2f, sx * hw), 0.06f, Mat.DarkSteel);
                }

                b.BoxOn(0, top - 0.15f, 0, 2.8f, 0.15f, 2.8f, Mat.DarkSteel, 0.02f);
                KitParts.Ladder(b, new Vector3(hw + 0.15f, 1.22f, 0.4f), top - 1.22f, -90f, true);
                for (int i = 0; i < 4; i++)
                {
                    b.Push(new Vector3(0, top, 0), i * 90f);
                    KitParts.Plate(b, new Vector3(0, -0.5f, -1.42f), 2.4f, 0.9f, rng.Range(-3f, 3f), i % 2 == 0 ? Mat.OliveSteel : Mat.Rust);
                    b.Pop();
                }
            }

            Shapes.SandbagRing(b, new Vector3(0, top, 0), level >= 3 ? 1.3f : 1.85f, level >= 3 ? 14 : 18, level >= 5 ? 3 : 2, 70f, 40f);
            b.Frustum(new Vector3(0, top, 0), 0.8f, 0.65f, 0.45f, 14, Mat.DarkSteel, 0.05f);
            float pivotY = top + 0.45f;

            var head = new MeshBuilder(seed + 7) { GroundOffset = pivotY + PadTop };
            float w = level >= 3 ? 1.4f : 1.2f;
            head.BoxOn(0, 0, 0.1f, w, 0.72f, 1.2f, Mat.OliveSteel, 0.1f);
            head.BoxOn(0, 0.72f, 0.2f, w * 0.6f, 0.22f, 0.7f, Mat.OliveSteel, 0.06f);
            head.BoxOn(0, 0.1f, 0.85f, w * 0.8f, 0.5f, 0.35f, Mat.DarkSteel, 0.04f);
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
            head.Box(new Vector3(-w * 0.52f, 0.2f, 0.3f), new Vector3(0.3f, 0.35f, 0.5f), Mat.OliveSteel, 0.03f);
            if (level >= 3)
            {
                head.Box(new Vector3(-w * 0.56f, 0.36f, -0.1f), new Vector3(0.09f, 0.62f, 1.15f), Mat.DarkSteel, 0.02f);
                head.Box(new Vector3(w * 0.56f, 0.36f, -0.1f), new Vector3(0.09f, 0.62f, 1.15f), Mat.DarkSteel, 0.02f);
            }

            m.Parts.Add(new AnimPart(head.Mesh, new Vector3(0, pivotY, 0), AnimKind.SweepY, 0.08f + (level * 0.01f), 55f));
            m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(0.5f, pivotY + 0.7f, -0.9f)), Model.Red, 0.7f, 2.2f, LightRole.Status));

            // ammo store and camo net behind the gun, tank traps out front
            b.Push(new Vector3(-1.9f, 0, 2.2f), 20f);
            b.BoxOn(0, 0, 0, 1.6f, 0.9f, 1.0f, Mat.OliveSteel, 0.04f);
            b.BoxOn(0, 0.9f, 0, 1.2f, 0.4f, 0.8f, Mat.OliveSteel, 0.04f);
            b.Pop();
            KitModules.LeanTo(b, new Vector3(-1.6f, 0, 3.15f), 2.4f, 1.5f, 1.9f, 1.4f, Mat.Tarp);
            Hedgehog(b, new Vector3(-2.6f, 0, -2.6f), 20f);
            Hedgehog(b, new Vector3(2.65f, 0, -2.5f), -35f);
            Shapes.SandbagWall(b, new Vector3(-1.3f, 0, -2.75f), new Vector3(1.4f, 0, -2.85f), 2);
            KitModules.CrateStack(b, new Vector3(2.3f, 0, 2.3f), rng);

            if (level >= 2)
            {
                Vector3 pole = new Vector3(2.6f, 0, 0.6f);
                b.Frustum(pole, 0.1f, 0.08f, 4.6f, 8, Mat.DarkSteel, 0.01f);
                KitParts.Spotlight(b, m, pole + new Vector3(0, 4.5f, -0.25f), 10f);
                KitParts.Cable(b, pole + new Vector3(0, 4.2f, 0), new Vector3(1.0f, top + 0.3f, 1.0f), 0.4f);
            }

            if (level >= 4)
            {
                // rotating search radar on its own mast
                Vector3 mb = new Vector3(-2.55f, 0, 0.4f);
                KitParts.Mast(b, m, mb, 5.2f, false);
                var radar = new MeshBuilder(seed + 17) { GroundOffset = 5.4f };
                radar.Box(new Vector3(0, 0.25f, 0), new Vector3(2.0f, 0.5f, 0.1f), Mat.DarkSteel, 0.02f);
                radar.Box(new Vector3(0, 0.25f, 0.08f), new Vector3(1.8f, 0.4f, 0.06f), Mat.PaintWhite, 0.01f);
                radar.Frustum(new Vector3(0, -0.2f, 0), 0.12f, 0.1f, 0.25f, 8, Mat.DarkSteel, 0.01f);
                m.Parts.Add(new AnimPart(radar.Mesh, mb + new Vector3(0, 5.4f, 0), AnimKind.SpinY, 60f));
            }

            if (level >= 5)
            {
                KitParts.Spotlight(b, m, new Vector3(-1.0f, top + 1.2f, -1.0f), -20f);
                b.Strut(new Vector3(-1.0f, top, -1.0f), new Vector3(-1.0f, top + 1.1f, -1.0f), 0.06f, Mat.DarkSteel);
            }

            Beacon(b, m, new Vector3(-1.5f, top + 0.5f, -1.4f));
        }

        // ---------- shared pieces ----------

        /// <summary>Exhaust stack: tapered rusted flue, collars, soot cap, guy wires, optional warning lamp.</summary>
        private static void Stack(MeshBuilder b, Model m, Vector3 baseCenter, float r, float h, bool lamp)
        {
            b.Frustum(baseCenter, r * 1.3f, r * 1.3f, 0.5f, 12, Mat.Concrete, 0.04f);
            b.Frustum(baseCenter + new Vector3(0, 0.5f, 0), r * 1.1f, r, h - 0.5f, 12, Mat.Rust, 0.02f);
            for (float y = 1.4f; y < h - 0.3f; y += 1.3f)
            {
                b.Frustum(baseCenter + new Vector3(0, y, 0), r + 0.035f, r + 0.035f, 0.1f, 12, Mat.DarkSteel, 0.01f, false);
            }

            b.Frustum(baseCenter + new Vector3(0, h, 0), r * 1.25f, r * 1.2f, 0.16f, 12, Mat.DarkSteel, 0.02f);
            b.Frustum(baseCenter + new Vector3(0, h + 0.16f, 0), r * 0.9f, r * 0.9f, 0.01f, 12, Mat.Rubber, 0f);
            for (int k = 0; k < 3; k++)
            {
                float a = MeshBuilder.Deg((k * 120f) + 40f);
                Vector3 dir = new Vector3((float)Math.Cos(a), 0, (float)Math.Sin(a));
                b.Strut(baseCenter + new Vector3(0, h * 0.7f, 0) + (dir * r), baseCenter + (dir * (r + 1.1f)) + new Vector3(0, 2.5f, 0), 0.015f, Mat.DarkSteel);
            }

            if (lamp)
            {
                Shapes.Lamp(b, m, baseCenter + new Vector3(r + 0.1f, h - 0.2f, 0), Mat.LampRed, Model.Red, 0.8f, 3f, LightRole.Status, 0.08f);
            }
        }

        /// <summary>Tank on a braced four-leg stand with a ladder.</summary>
        private static void ElevatedTank(MeshBuilder b, Vector3 baseCenter, float legH, float r, float h, Mat mat, uint seed)
        {
            float s = r * 0.8f;
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    b.Strut(baseCenter + new Vector3(sx * s * 1.25f, 0, sz * s * 1.25f), baseCenter + new Vector3(sx * s, legH, sz * s), 0.09f, Mat.DarkSteel);
                }

                b.Strut(baseCenter + new Vector3(sx * s * 1.2f, 0.4f, -s * 1.2f), baseCenter + new Vector3(sx * s, legH - 0.4f, s), 0.04f, Mat.DarkSteel);
                b.Strut(baseCenter + new Vector3(-s * 1.2f, 0.4f, sx * s * 1.2f), baseCenter + new Vector3(s, legH - 0.4f, sx * s), 0.04f, Mat.DarkSteel);
            }

            b.BoxOn(baseCenter.X, baseCenter.Y + legH - 0.1f, baseCenter.Z, (r * 2f) + 0.3f, 0.1f, (r * 2f) + 0.3f, Mat.DarkSteel, 0.01f);
            KitParts.TankV(b, baseCenter + new Vector3(0, legH, 0), r, h, mat, seed);
            KitParts.Ladder(b, baseCenter + new Vector3(0, 0, -s * 1.2f), legH, 0f);
        }

        /// <summary>Ground chiller: louvered cabinet with spinning top fans.</summary>
        private static void Chiller(MeshBuilder b, Model m, Vector3 baseCenter, int fans, uint seed)
        {
            float w = 0.95f * fans;
            b.BoxOn(baseCenter.X, baseCenter.Y, baseCenter.Z, w + 0.1f, 0.15f, 1.15f, Mat.DarkSteel, 0.02f);
            b.BoxOn(baseCenter.X, baseCenter.Y + 0.15f, baseCenter.Z, w, 1.0f, 1.05f, Mat.SandSteel, 0.04f);
            for (int i = 0; i < 6; i++)
            {
                b.Box(new Vector3(baseCenter.X, baseCenter.Y + 0.3f + (i * 0.13f), baseCenter.Z - 0.54f), new Vector3(w * 0.9f, 0.04f, 0.03f), Mat.DarkSteel, 0f);
            }

            for (int i = 0; i < fans; i++)
            {
                Vector3 f = baseCenter + new Vector3(-w * 0.5f + 0.475f + (i * 0.95f), 1.15f, 0);
                b.Frustum(f, 0.42f, 0.42f, 0.08f, 14, Mat.DarkSteel, 0.01f, false);
                m.Parts.Add(Fan(f + new Vector3(0, 0.02f, 0), 0.38f, 480f + (i * 40f), seed + (uint)i));
            }
        }

        /// <summary>Rooftop condenser block with one fan.</summary>
        private static void Condenser(MeshBuilder b, Model m, Vector3 baseCenter, uint seed)
        {
            b.BoxOn(baseCenter.X, baseCenter.Y, baseCenter.Z, 1.0f, 0.5f, 1.0f, Mat.DarkSteel, 0.05f);
            b.Frustum(baseCenter + new Vector3(0, 0.5f, 0), 0.4f, 0.4f, 0.05f, 14, Mat.DarkSteel, 0.01f, false);
            m.Parts.Add(Fan(baseCenter + new Vector3(0, 0.52f, 0), 0.36f, 520f, seed));
        }

        /// <summary>Outdoor server rack cabinet with phosphor status lights, facing -Z.</summary>
        private static void Rack(MeshBuilder b, Vector3 baseCenter)
        {
            b.BoxOn(baseCenter.X, baseCenter.Y, baseCenter.Z, 0.56f, 1.95f, 0.7f, Mat.DarkSteel, 0.03f);
            b.Box(new Vector3(baseCenter.X, baseCenter.Y + 1.0f, baseCenter.Z - 0.36f), new Vector3(0.48f, 1.7f, 0.02f), Mat.Screen, 0f);
            for (int i = 0; i < 8; i++)
            {
                b.Box(new Vector3(baseCenter.X - 0.12f + ((i % 2) * 0.2f), baseCenter.Y + 0.3f + (i * 0.2f), baseCenter.Z - 0.38f), new Vector3(0.05f, 0.03f, 0.02f), i % 5 == 4 ? Mat.LampAmber : Mat.LampPhosphor, 0f);
            }

            b.Box(new Vector3(baseCenter.X, baseCenter.Y + 2.0f, baseCenter.Z), new Vector3(0.6f, 0.06f, 0.74f), Mat.DarkSteel, 0.01f);
        }

        /// <summary>Satellite dish on a pedestal, sweeping slowly.</summary>
        private static void Dish(MeshBuilder b, Model m, Vector3 baseCenter, float radius, uint seed)
        {
            b.BoxOn(baseCenter.X, baseCenter.Y, baseCenter.Z, 0.7f, 0.15f, 0.7f, Mat.DarkSteel, 0.02f);
            b.Frustum(baseCenter + new Vector3(0, 0.15f, 0), 0.12f, 0.09f, 0.85f, 10, Mat.DarkSteel, 0.02f);
            var dish = new MeshBuilder(seed) { GroundOffset = baseCenter.Y + 1f };
            dish.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-38f)));
            dish.Frustum(new Vector3(0, 0.05f, 0), 0.12f, radius, radius * 0.34f, 18, Mat.PaintWhite, 0.02f, true, Mat.Concrete);
            dish.Strut(new Vector3(0, 0.1f, 0), new Vector3(0, radius * 0.95f, 0), 0.04f, Mat.DarkSteel);
            dish.Box(new Vector3(0, radius * 0.95f, 0), new Vector3(0.12f, 0.12f, 0.12f), Mat.DarkSteel, 0.02f);
            dish.Pop();
            m.Parts.Add(new AnimPart(dish.Mesh, baseCenter + new Vector3(0, 1.0f, 0), AnimKind.SweepY, 0.05f, 70f));
        }

        /// <summary>Tilted solar panel on a steel frame.</summary>
        private static void SolarPanel(MeshBuilder b, Vector3 baseCenter, float w, float d, float tiltDeg)
        {
            b.Strut(baseCenter + new Vector3(-w * 0.45f, 0, -d * 0.4f), baseCenter + new Vector3(-w * 0.45f, 0.25f, -d * 0.4f), 0.05f, Mat.DarkSteel);
            b.Strut(baseCenter + new Vector3(w * 0.45f, 0, -d * 0.4f), baseCenter + new Vector3(w * 0.45f, 0.25f, -d * 0.4f), 0.05f, Mat.DarkSteel);
            b.Strut(baseCenter + new Vector3(-w * 0.45f, 0, d * 0.4f), baseCenter + new Vector3(-w * 0.45f, 0.75f, d * 0.4f), 0.05f, Mat.DarkSteel);
            b.Strut(baseCenter + new Vector3(w * 0.45f, 0, d * 0.4f), baseCenter + new Vector3(w * 0.45f, 0.75f, d * 0.4f), 0.05f, Mat.DarkSteel);
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-tiltDeg)) * Matrix4x4.CreateTranslation(baseCenter + new Vector3(0, 0.55f, 0)));
            b.Box(new Vector3(0, -0.04f, 0), new Vector3(w, 0.05f, d), Mat.DarkSteel, 0.01f);
            b.Box(Vector3.Zero, new Vector3(w - 0.1f, 0.04f, d - 0.1f), Mat.Glass, 0.005f);
            b.Pop();
        }

        /// <summary>Washing line with hanging cloths in muted colors.</summary>
        private static void Laundry(MeshBuilder b, Vector3 a, Vector3 c, ArtRandom rng)
        {
            KitParts.Cable(b, a, c, 0.15f, 0.012f, Mat.DarkSteel);
            float len = Vector3.Distance(a, c);
            int n = Math.Max(2, (int)(len / 0.6f));
            Mat[] cloth = { Mat.Tarp, Mat.PaintWhite, Mat.TarpBlue, Mat.Sandbag, Mat.OliveSteel };
            for (int i = 1; i < n; i++)
            {
                if (rng.Next() < 0.25f)
                {
                    continue;
                }

                float t = i / (float)n;
                Vector3 p = Vector3.Lerp(a, c, t) - new Vector3(0, 0.6f * t * (1 - t), 0);
                float h = rng.Range(0.35f, 0.7f);
                b.Box(p - new Vector3(0, h * 0.5f, 0), new Vector3(rng.Range(0.3f, 0.5f), h, 0.015f), cloth[rng.Range(0, cloth.Length)], 0f);
            }
        }

        /// <summary>Czech hedgehog tank trap: three welded beams.</summary>
        private static void Hedgehog(MeshBuilder b, Vector3 at, float yaw)
        {
            b.Push(at, yaw);
            const float s = 0.65f;
            KitParts.IBeam(b, new Vector3(-s, 0.05f, 0), new Vector3(s, 1.3f, 0), 0.12f, 0.1f, Mat.Rust);
            KitParts.IBeam(b, new Vector3(0, 0.05f, -s), new Vector3(0, 1.3f, s), 0.12f, 0.1f, Mat.Rust);
            KitParts.IBeam(b, new Vector3(s * 0.7f, 0.05f, s * 0.7f), new Vector3(-s * 0.7f, 1.3f, -s * 0.7f), 0.12f, 0.1f, Mat.Rust);
            b.Pop();
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
