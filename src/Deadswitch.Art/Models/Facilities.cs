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

        /// <param name="stage">Preview only: a SPEC-045 visual stage (1-10) for kinds that have one; 0 uses the level.</param>
        public static Model Build(FacilityKind kind, int level, uint seed, int stage = 0)
        {
            var m = new Model();
            var b = new MeshBuilder(seed) { GroundOffset = 0f };
            level = Math.Max(1, level);
            b.Push(new Vector3(0, PadTop, 0));
            switch (kind)
            {
                case FacilityKind.Generator when stage > 0:
                    GeneratorStage(b, m, stage, seed);
                    break;
                case FacilityKind.Generator:
                    Generator(b, m, level, seed);
                    break;
                case FacilityKind.ServerRack when stage > 0:
                    ComputeStage(b, m, stage, seed);
                    break;
                case FacilityKind.ServerRack:
                    Compute(b, m, level, seed);
                    break;
                case FacilityKind.LifeSupport when stage > 0:
                    HabitatStage(b, m, stage, seed);
                    break;
                case FacilityKind.LifeSupport:
                    Habitat(b, m, level, seed);
                    break;
                case FacilityKind.BatteryBank when stage > 0:
                    BatteryStage(b, m, stage, seed);
                    break;
                case FacilityKind.BatteryBank:
                    Battery(b, m, level, seed);
                    break;
                case FacilityKind.Turret when stage > 0:
                    TurretStage(b, m, stage, seed);
                    break;
                case FacilityKind.Turret:
                    Turret(b, m, level, seed);
                    break;
                case FacilityKind.Reactor when stage > 0:
                    ReactorStage(b, m, stage, seed);
                    break;
                case FacilityKind.Reactor:
                    Reactor(b, m, level, seed);
                    break;
                case FacilityKind.DroneBay when stage > 0:
                    Military.DroneBayStage(b, m, stage, seed);
                    break;
                case FacilityKind.DroneBay:
                    Military.DroneBay(b, m, level, seed);
                    break;
                case FacilityKind.MotorPool when stage > 0:
                    Military.MotorPoolStage(b, m, stage, seed);
                    break;
                case FacilityKind.MotorPool:
                    Military.MotorPool(b, m, level, seed);
                    break;
                case FacilityKind.SolarField when stage > 0:
                    Utilities.SolarFieldStage(b, m, stage, seed);
                    break;
                case FacilityKind.SolarField:
                    Utilities.SolarField(b, m, level, seed);
                    break;
                case FacilityKind.FuelDepot when stage > 0:
                    Utilities.FuelDepotStage(b, m, stage, seed);
                    break;
                case FacilityKind.FuelDepot:
                    Utilities.FuelDepot(b, m, level, seed);
                    break;
                case FacilityKind.CoolingTower when stage > 0:
                    Utilities.CoolingTowerStage(b, m, stage, seed);
                    break;
                case FacilityKind.CoolingTower:
                    Utilities.CoolingTower(b, m, level, seed);
                    break;
                case FacilityKind.MemoryChamber when stage > 0:
                    Utilities.MemoryChamberStage(b, m, stage, seed);
                    break;
                case FacilityKind.MemoryChamber:
                    Utilities.MemoryChamber(b, m, level, seed);
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

        /// <summary>
        /// Worn concrete footing under a facility. An empty plot has no slab or outline (SPEC-045 E): only a small stack
        /// of building material and a survey flag on the levelled ground, so the yard does not read as a grid of
        /// rectangles; the next free plot is marked by the AI's ring and the build prompt instead.
        /// </summary>
        /// <param name="slab">False for improvised stages (SPEC-045 stages 1-2): duckboards on the bare ground, no slab.</param>
        public static MeshData Pad(uint seed, bool empty, bool slab = true)
        {
            var b = new MeshBuilder(seed) { AoFloor = 0.55f, AoHeight = 0.4f, FaceJitter = 0.12f };
            if (!empty && !slab)
            {
                for (int i = 0; i < 3; i++)
                {
                    b.BoxOn(0.4f, 0.02f, -0.9f - (i * 0.55f), 1.6f, 0.05f, 0.4f, Mat.Wood, 0.01f);
                }

                return b.Mesh;
            }

            if (empty)
            {
                b.BoxOn(-1.6f, 0, 1.4f, 1.2f, 0.12f, 1.0f, Mat.Wood, 0.01f);
                b.BoxOn(-1.6f, 0.12f, 1.4f, 1.1f, 0.5f, 0.9f, Mat.Concrete, 0.04f);
                for (int i = 0; i < 4; i++)
                {
                    b.CylinderX(new Vector3(1.2f, 0.08f + (i % 2 * 0.15f), -1.6f + (i * 0.17f)), 0.07f, 2.4f, 8, Mat.Rust, 0.01f);
                }

                var flag = new Vector3(PlotHalf - 0.6f, 0, PlotHalf - 0.6f);
                b.Strut(flag, flag + new Vector3(0, 1.5f, 0), 0.03f, Mat.DarkSteel);
                b.Box(flag + new Vector3(0.16f, 1.38f, 0), new Vector3(0.3f, 0.2f, 0.01f), Mat.PaintRed, 0f);
                return b.Mesh;
            }

            b.BoxOn(0, -0.25f, 0, PlotHalf * 2f, 0.37f, PlotHalf * 2f, Mat.ConcreteDark, 0.06f);
            for (int i = 1; i < 3; i++)
            {
                b.Box(new Vector3(-PlotHalf + (i * PlotHalf * 2f / 3f), PadTop, 0), new Vector3(0.03f, 0.01f, PlotHalf * 2f), Mat.Rubber, 0f);
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
            KitParts.Pipe(b, new[] { new Vector3(-1.9f, 1.9f, 2.6f), new Vector3(-1.9f, 1.9f, 2.95f), new Vector3(-2.12f, 1.9f, 2.95f) }, 0.1f, Mat.Copper);
            KitParts.Pipe(b, new[] { new Vector3(-1.1f, 1.6f, 2.6f), new Vector3(-1.1f, 1.6f, 3.0f), new Vector3(-1.32f, 1.6f, 3.0f) }, 0.08f, Mat.Copper);

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
                KitParts.Pipe(b, new[] { new Vector3(-0.55f, 2.2f, 2.6f), new Vector3(-0.55f, 2.2f, 2.75f) }, 0.12f, Mat.Copper);
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

        /// <summary>
        /// Generator by visual stage 1-10 (SPEC-045 A.12, plan in SPEC-045-facility-stages.md). Preview only until the
        /// level-to-stage curve is set: then <see cref="Generator"/> becomes this with a stage per level band.
        /// Stages 1-3 are the improvised gensets before the hall; from stage 4 each stage adds to the one before.
        /// </summary>
        private static void GeneratorStage(MeshBuilder b, Model m, int stage, uint seed)
        {
            var rng = new ArtRandom(seed + 1);
            stage = Math.Min(10, Math.Max(1, stage));
            if (stage <= 3)
            {
                GeneratorYard(b, m, stage, rng);
                Beacon(b, m, new Vector3(0.9f, 2.1f, -0.6f));
                return;
            }

            // 4: engine hall on a plinth, genset bay under a lean-to, two stacks
            b.BoxOn(-0.5f, 0, 1.45f, 5.4f, 0.2f, 2.8f, Mat.Concrete, 0.05f);
            KitModules.ContainerBlock(b, m, new Vector3(-0.5f, 0.2f, 1.45f), 5.0f, Mat.OliveSteel, true, "POWER", rng, 1, false);
            KitModules.LeanTo(b, new Vector3(-1.2f, 0, 0.23f), 3.4f, 1.9f, 2.75f, 2.3f, Mat.Rust);
            KitParts.Genset(b, new Vector3(-1.35f, 0, -0.85f), 0f);
            Props.Lamp(b, m, new Vector3(-0.3f, 2.2f, -0.2f), true, 1.4f, LightRole.Status);
            Stack(b, m, new Vector3(-2.35f, 0, 2.95f), 0.24f, 6.4f, stage >= 9);
            Stack(b, m, new Vector3(-1.5f, 0, 3.0f), 0.18f, 5.0f, false);
            KitParts.Pipe(b, new[] { new Vector3(-1.9f, 1.9f, 2.6f), new Vector3(-1.9f, 1.9f, 2.95f), new Vector3(-2.12f, 1.9f, 2.95f) }, 0.1f, Mat.Copper);
            KitParts.Pipe(b, new[] { new Vector3(-1.1f, 1.6f, 2.6f), new Vector3(-1.1f, 1.6f, 3.0f), new Vector3(-1.32f, 1.6f, 3.0f) }, 0.08f, Mat.Copper);
            KitModules.Barrels(b, new Vector3(-2.75f, 0, -2.55f), 3, rng);
            KitModules.Pallet(b, new Vector3(0.3f, 0, -2.5f), 12f, 2);

            if (stage >= 5)
            {
                // roof radiator with a fan and the fuel tank feeding the hall over the roof
                b.BoxOn(1.25f, 2.8f, 1.5f, 1.4f, 0.55f, 1.4f, Mat.DarkSteel, 0.04f);
                for (int i = 0; i < 5; i++)
                {
                    b.Box(new Vector3(1.25f, 2.92f + (i * 0.09f), 0.79f), new Vector3(1.3f, 0.03f, 0.03f), Mat.Rust, 0f);
                }

                m.Parts.Add(Fan(new Vector3(1.25f, 3.35f, 1.5f), 0.5f, 400f, seed));
                KitParts.Vent(b, new Vector3(1.25f, 2.8f, 0.5f), 0.7f, 0.45f);
                KitParts.TankV(b, new Vector3(2.65f, 0, 1.55f), 0.5f, 2.3f, Mat.SandSteel, seed + 3);
                KitParts.Pipe(b, new[] { new Vector3(2.65f, 2.5f, 1.55f), new Vector3(2.65f, 3.5f, 1.55f), new Vector3(2.65f, 3.5f, 2.4f), new Vector3(0.1f, 3.5f, 2.4f), new Vector3(0.1f, 2.8f, 2.4f) }, 0.06f, Mat.DarkSteel);
                b.Strut(new Vector3(1.2f, 2.8f, 2.4f), new Vector3(1.2f, 3.44f, 2.4f), 0.05f, Mat.DarkSteel);
            }

            if (stage >= 6)
            {
                // transformer yard behind a fence, day tank beside the genset
                KitParts.Transformer(b, new Vector3(2.1f, 0, -1.75f), 0.9f);
                KitModules.Fence(b, new Vector3(0.95f, 0, -2.95f), new Vector3(3.15f, 0, -2.95f), 1.5f);
                KitModules.Fence(b, new Vector3(3.15f, 0, -2.95f), new Vector3(3.15f, 0, -0.75f), 1.5f);
                for (int i = 0; i < 3; i++)
                {
                    KitParts.Cable(b, new Vector3(1.8f + (i * 0.3f), 1.95f, -1.75f), new Vector3(1.55f + (i * 0.15f), 2.7f, 0.2f), 0.25f, 0.025f);
                }

                KitParts.TankH(b, new Vector3(1.15f, 0.85f, -0.6f), 0.48f, 1.9f, Mat.Rust);
                KitParts.Pipe(b, new[] { new Vector3(0.2f, 1.0f, -0.6f), new Vector3(-0.2f, 1.0f, -0.6f), new Vector3(-0.2f, 1.2f, -0.85f) }, 0.05f, Mat.DarkSteel);
                KitModules.CrateStack(b, new Vector3(-0.9f, 0, -2.55f), rng);
            }

            if (stage >= 7)
            {
                // wind turbine on a lattice mast behind the fuel tank
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

            if (stage >= 8)
            {
                // control cab stacked on the hall, ladder up the door end, roof railing
                b.Push(new Vector3(-1.15f, 2.8f, 1.45f), 3f);
                KitModules.ContainerBlock(b, m, Vector3.Zero, 3.4f, Mat.Rust, true, "CONTROL", rng, 1, true);
                b.Pop();
                KitParts.Ladder(b, new Vector3(-3.12f, 0, 0.9f), 2.8f, 90f);
                Props.Railing(b, new Vector3(0.65f, 2.8f, 0.3f), new Vector3(1.95f, 2.8f, 0.3f));
            }

            if (stage >= 9)
            {
                // military: heavy third stack, sandbag revetment on the open flank, armour plates over the genset bay
                Stack(b, m, new Vector3(-0.55f, 0, 3.05f), 0.3f, 7.8f, true);
                KitParts.Pipe(b, new[] { new Vector3(-0.55f, 2.2f, 2.6f), new Vector3(-0.55f, 2.2f, 2.75f) }, 0.12f, Mat.Copper);
                Shapes.SandbagWall(b, new Vector3(-3.05f, 0, -2.0f), new Vector3(-3.05f, 0, 0.1f), 3);
                for (int i = 0; i < 3; i++)
                {
                    KitParts.Plate(b, new Vector3(-2.45f + (i * 1.1f), 1.05f, -1.75f), 1.0f, 1.9f, 6f, Mat.DarkSteel);
                }

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

            if (stage >= 10)
            {
                // integrated with the AI: a shielded conduit to the plot edge (toward the Core) and cyan status lines on
                // the control cab and the transformer only; the hall stays steel and rust
                b.BoxOn(2.6f, 0, -0.3f, 0.5f, 0.32f, 5.6f, Mat.DarkSteel, 0.02f);
                b.Box(new Vector3(2.6f, 0.33f, -0.3f), new Vector3(0.1f, 0.02f, 5.4f), Mat.NeonCyan, 0f);
                b.Box(new Vector3(-1.15f, 5.3f, 0.2f), new Vector3(3.2f, 0.05f, 0.05f), Mat.NeonCyan, 0f);
                b.Box(new Vector3(2.1f, 1.98f, -2.25f), new Vector3(0.7f, 0.05f, 0.04f), Mat.NeonCyan, 0f);
                Shapes.Lamp(b, m, new Vector3(-1.15f, 5.6f, 0.4f), Mat.NeonCyan, Model.Neon, 0.9f, 3.5f, LightRole.Status, 0.1f);
            }

            Beacon(b, m, new Vector3(2.0f, 3.1f, 0.25f));
        }

        /// <summary>Generator stages 1-3: gensets in the open, then on a plinth under a roof (SPEC-045 stage plan).</summary>
        private static void GeneratorYard(MeshBuilder b, Model m, int stage, ArtRandom rng)
        {
            float y = 0f;
            if (stage >= 3)
            {
                // stabilized: concrete plinth, block back wall and a lean-to roof over both sets, cable tray out front
                y = 0.2f;
                b.BoxOn(-0.4f, 0, 0.6f, 5.6f, y, 2.9f, Mat.Concrete, 0.05f);
                b.BoxOn(-0.4f, y, 2.0f, 5.6f, 2.6f, 0.25f, Mat.ConcreteDark, 0.06f);
                KitModules.LeanTo(b, new Vector3(-0.4f, y, 1.85f), 5.4f, 2.6f, 2.6f, 2.15f, Mat.Rust);
                for (int i = 0; i < 4; i++)
                {
                    b.Strut(new Vector3(-2.5f + (i * 1.5f), 0, -1.6f), new Vector3(-2.5f + (i * 1.5f), 0.7f, -1.6f), 0.03f, Mat.DarkSteel);
                }

                b.BoxOn(-0.25f, 0.7f, -1.6f, 4.7f, 0.08f, 0.3f, Mat.DarkSteel, 0.01f);
                Props.Lamp(b, m, new Vector3(-0.4f, 2.0f, 0.05f), true, 1.4f, LightRole.Status);
            }
            else
            {
                // improvised: a blue tarp on timber poles over the first set, one bare bulb
                float[] px = { -2.1f, 0.9f };
                float[] pz = { -0.15f, 1.45f };
                foreach (float x in px)
                {
                    foreach (float z in pz)
                    {
                        b.Strut(new Vector3(x, 0, z), new Vector3(x, z > 0.5f ? 2.5f : 2.1f, z), 0.06f, Mat.Wood);
                    }
                }

                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-14f)) * Matrix4x4.CreateTranslation(new Vector3(-0.6f, 2.33f, 0.65f)));
                b.Box(Vector3.Zero, new Vector3(3.5f, 0.03f, 1.95f), Mat.TarpBlue, 0.01f);
                b.Pop();
                Props.Lamp(b, m, new Vector3(-0.6f, 2.0f, 0.1f), false, 0.9f, LightRole.Status);
            }

            KitParts.Genset(b, new Vector3(-0.6f, y, 0.6f), 0f);
            Stack(b, m, new Vector3(-1.85f, y, 1.75f), 0.13f, stage >= 3 ? 4.2f : 3.0f, false);
            KitModules.Barrels(b, new Vector3(-2.7f, 0, -2.4f), stage >= 2 ? 4 : 2, rng);
            if (stage >= 2)
            {
                // a second, mismatched set and a cable drum feeding the hub by hand-run cable
                KitParts.Genset(b, new Vector3(1.75f, y, stage >= 3 ? 0.6f : -0.2f), stage >= 3 ? 0f : 90f);
                b.CylinderX(new Vector3(0.6f, 0.45f, -2.3f), 0.45f, 0.5f, 14, Mat.Wood, 0.02f);
                KitModules.CrateStack(b, new Vector3(-1.1f, 0, -2.5f), rng);
            }

            KitParts.Cable(b, new Vector3(0.4f, y + 0.8f, 0.6f), new Vector3(0.6f, 0.05f, -3.1f), 0.1f, 0.04f);
            KitModules.Pallet(b, new Vector3(2.2f, 0, -2.4f), 8f, 1);
        }

        // ---------- Reactor (SPEC-029): containment drum and dome, cooling tower, coolant loop, control room ----------
        private static void Reactor(MeshBuilder b, Model m, int level, uint seed)
        {
            ReactorCore(b, m, seed, level >= 2, level >= 3, 0);
        }

        /// <summary>
        /// Reactor by visual stage 1-10 (SPEC-045 stage plan). Preview only, like <see cref="GeneratorStage"/>. Stages 1-3
        /// are salvaged RTG casks and a first small drum; 4-6 are the level models, then pumps, bunker, berm and rings.
        /// </summary>
        private static void ReactorStage(MeshBuilder b, Model m, int stage, uint seed)
        {
            stage = Math.Min(10, Math.Max(1, stage));
            if (stage <= 3)
            {
                ReactorCasks(b, m, stage, seed);
                return;
            }

            ReactorCore(b, m, seed, stage >= 5, stage >= 6, stage);
        }

        /// <param name="stage">SPEC-045 stage for the stage-only extras (7 pumps, 8 bunker, 9 berm, 10 rings); 0 for the level models.</param>
        private static void ReactorCore(MeshBuilder b, Model m, uint seed, bool secondTower, bool gantry, int stage)
        {
            var rng = new ArtRandom(seed + 9);
            b.BoxOn(0f, 0, 0.2f, 6.0f, 0.22f, 5.8f, Mat.Concrete, 0.05f);

            // containment: a stained concrete drum on a stepped base, banded in steel, under a shallow dome
            Vector3 core = new Vector3(-0.85f, 0.22f, 0.95f);
            b.Frustum(core, 1.75f, 1.7f, 0.35f, 20, Mat.ConcreteDark, 0.05f);
            b.Frustum(core + new Vector3(0, 0.35f, 0), 1.5f, 1.45f, 3.0f, 20, Mat.Concrete, 0.04f);
            foreach (float y in new[] { 0.9f, 1.9f, 2.9f })
            {
                b.Frustum(core + new Vector3(0, y, 0), 1.53f, 1.52f, 0.12f, 20, Mat.DarkSteel, 0.01f, false);
            }

            b.Frustum(core + new Vector3(0, 0.55f, 0), 1.535f, 1.53f, 0.22f, 20, Mat.PaintRed, 0.01f, false);
            b.Sphere(core + new Vector3(0, 3.35f, 0), new Vector3(1.45f, 0.85f, 1.45f), 6, 20, Mat.ConcreteDark, 0f, 0.5f);
            b.Frustum(core + new Vector3(0, 4.1f, 0), 0.3f, 0.26f, 0.35f, 10, Mat.DarkSteel, 0.02f);
            Shapes.Lamp(b, m, core + new Vector3(0, 4.5f, 0), Mat.LampAmber, Model.Amber, 1.0f, 4f, LightRole.Status, 0.1f);

            // personnel airlock and a ladder up the drum
            b.BoxOn(core.X + 0.2f, 0.22f, core.Z - 1.55f, 1.0f, 2.0f, 0.5f, Mat.DarkSteel, 0.04f);
            b.Box(new Vector3(core.X + 0.2f, 1.2f, core.Z - 1.81f), new Vector3(0.7f, 1.6f, 0.04f), Mat.OliveSteel, 0.02f);
            Props.Stencil(b, "RX1", new Vector3(core.X - 0.03f, 2.0f, core.Z - 1.81f), 0.06f, Mat.PaintWhite);
            KitParts.Ladder(b, new Vector3(core.X - 1.48f, 0.22f, core.Z - 0.3f), 3.3f, 90f, true);

            // cooling tower: hyperbolic concrete shell, streaked
            Vector3 tower = new Vector3(1.85f, 0.22f, 1.75f);
            b.Frustum(tower, 1.05f, 0.62f, 2.3f, 18, Mat.Concrete, 0.03f);
            b.Frustum(tower + new Vector3(0, 2.3f, 0), 0.62f, 0.8f, 1.4f, 18, Mat.Concrete, 0.03f, true, Mat.Char);
            b.Frustum(tower + new Vector3(0, 3.7f, 0), 0.84f, 0.83f, 0.1f, 18, Mat.ConcreteDark, 0.01f, true, Mat.Char);
            b.Frustum(tower + new Vector3(0, 3.79f, 0), 0.72f, 0.72f, 0.02f, 18, Mat.Char, 0f);
            for (int i = 0; i < 8; i++)
            {
                float a = MeshBuilder.Deg(i * 45f);
                b.Strut(tower + new Vector3((float)Math.Cos(a) * 1.15f, 0f, (float)Math.Sin(a) * 1.15f), tower + new Vector3((float)Math.Cos(a) * 0.95f, 0.45f, (float)Math.Sin(a) * 0.95f), 0.06f, Mat.ConcreteDark);
            }

            // primary coolant loop: drum to heat exchanger to tower
            KitParts.TankH(b, new Vector3(1.55f, 0.75f, -1.2f), 0.42f, 2.2f, Mat.SandSteel);
            KitParts.Pipe(b, new[] { new Vector3(core.X + 1.45f, 1.4f, core.Z - 0.4f), new Vector3(0.6f, 1.4f, -0.2f), new Vector3(0.6f, 0.95f, -1.2f), new Vector3(0.45f, 0.95f, -1.2f) }, 0.12f, Mat.Rust);
            KitParts.Pipe(b, new[] { new Vector3(2.65f, 0.95f, -1.2f), new Vector3(2.85f, 0.95f, -1.2f), new Vector3(2.85f, 0.95f, 0.9f), new Vector3(tower.X + 0.6f, 0.95f, tower.Z - 0.6f) }, 0.1f, Mat.Rust);
            KitParts.Pipe(b, new[] { new Vector3(core.X + 1.4f, 2.2f, core.Z + 0.5f), new Vector3(tower.X - 0.9f, 2.2f, tower.Z), new Vector3(tower.X - 0.6f, 2.2f, tower.Z) }, 0.09f, Mat.DarkSteel);
            for (int i = 0; i < 3; i++)
            {
                b.Strut(new Vector3(0.6f, 0.22f, -0.2f - (i * 0.45f)), new Vector3(0.6f, 0.85f + (i == 0 ? 0.45f : 0f), -0.2f - (i * 0.45f)), 0.04f, Mat.DarkSteel);
            }

            // control room facing the yard, hazard fence along the front
            KitModules.ContainerBlock(b, m, new Vector3(-1.45f, 0.22f, -2.05f), 3.0f, Mat.SandSteel, true, "CONTROL", rng, 2, false);
            Props.Lamp(b, m, new Vector3(-0.2f, 2.5f, -2.7f), true, 1.3f, LightRole.Status);
            KitModules.Fence(b, new Vector3(0.3f, 0.22f, -3.05f), new Vector3(3.05f, 0.22f, -3.05f), 1.6f);
            KitModules.Fence(b, new Vector3(3.05f, 0.22f, -3.05f), new Vector3(3.05f, 0.22f, 2.9f), 1.6f);
            b.BoxOn(1.6f, 1.1f, -3.08f, 0.9f, 0.6f, 0.03f, Mat.Paint, 0.01f);
            Props.Stencil(b, "DANGER", new Vector3(1.22f, 1.25f, -3.1f), 0.045f, Mat.PaintRed);
            KitModules.Barrels(b, new Vector3(2.4f, 0.22f, -2.3f), 3, rng);
            KitParts.Transformer(b, new Vector3(-2.55f, 0.22f, 2.6f), 0.8f);

            if (secondTower)
            {
                // a second, smaller tower and a steam relief stack
                Vector3 t2 = new Vector3(2.35f, 0.22f, -0.2f);
                b.Frustum(t2, 0.6f, 0.38f, 1.5f, 14, Mat.Concrete, 0.03f);
                b.Frustum(t2 + new Vector3(0, 1.5f, 0), 0.38f, 0.48f, 0.9f, 14, Mat.Concrete, 0.03f, true, Mat.Char);
                b.Frustum(t2 + new Vector3(0, 2.4f, 0), 0.4f, 0.4f, 0.02f, 14, Mat.Char, 0f);
                Stack(b, m, new Vector3(-2.6f, 0.22f, -0.4f), 0.16f, 5.2f, true);
            }

            if (gantry)
            {
                // a gantry crane straddling the dome for refuelling
                foreach (int s in new[] { -1, 1 })
                {
                    Vector3 leg = core + new Vector3(s * 1.95f, 0, -0.2f);
                    b.Strut(leg, leg + new Vector3(0, 5.0f, 0), 0.12f, Mat.Paint);
                    b.Strut(leg + new Vector3(0, 0, 0.8f), leg + new Vector3(0, 5.0f, 0.4f), 0.08f, Mat.Paint);
                }

                b.BoxOn(core.X, 5.0f, core.Z - 0.1f, 4.2f, 0.35f, 0.5f, Mat.Paint, 0.03f);
                b.BoxOn(core.X + 0.4f, 4.65f, core.Z - 0.1f, 0.6f, 0.35f, 0.6f, Mat.DarkSteel, 0.03f);
                b.Strut(new Vector3(core.X + 0.4f, 4.65f, core.Z - 0.1f), new Vector3(core.X + 0.4f, 4.3f, core.Z - 0.1f), 0.02f, Mat.DarkSteel);
            }

            if (stage >= 7)
            {
                // coolant pump skid between the drum and the exchanger
                Chiller(b, m, new Vector3(0.2f, 0.22f, 2.75f), 1, seed + 21);
                KitParts.Pipe(b, new[] { new Vector3(0.2f, 0.8f, 2.2f), new Vector3(0.2f, 0.8f, 1.6f), new Vector3(core.X + 1.3f, 0.8f, 1.6f) }, 0.08f, Mat.Rust);
            }

            if (stage >= 8)
            {
                // radiation placards and a sandbagged entrance to the control room
                foreach (float x in new[] { -2.6f, -0.3f })
                {
                    b.Box(new Vector3(x, 1.9f, -3.33f), new Vector3(0.45f, 0.45f, 0.03f), Mat.PaintYellow, 0.01f);
                    b.Frustum(new Vector3(x, 1.9f, -3.36f), 0.07f, 0.07f, 0.01f, 10, Mat.Rubber, 0f, false);
                }

                Shapes.SandbagWall(b, new Vector3(-2.95f, 0.22f, -3.0f), new Vector3(-2.95f, 0.22f, -1.0f), 2);
            }

            if (stage >= 9)
            {
                // blast berm: retaining walls behind the drum and along the open side
                KitModules.RetainingWall(b, new Vector3(-1.2f, 0, 3.15f), 3.6f, 1.4f, new ArtRandom(seed + 23));
                b.Push(new Vector3(-3.15f, 0, 1.0f), 90f);
                KitModules.RetainingWall(b, Vector3.Zero, 2.8f, 1.4f, new ArtRandom(seed + 24));
                b.Pop();
            }

            if (stage >= 10)
            {
                // integrated: cyan containment rings on the drum and a shielded conduit to the plot edge
                foreach (float y in new[] { 1.4f, 2.4f })
                {
                    b.Frustum(core + new Vector3(0, y, 0), 1.545f, 1.545f, 0.05f, 20, Mat.NeonCyan, 0f, false);
                }

                b.BoxOn(-2.9f, 0, -1.3f, 0.45f, 0.3f, 3.6f, Mat.DarkSteel, 0.02f);
                b.Box(new Vector3(-2.9f, 0.31f, -1.3f), new Vector3(0.1f, 0.02f, 3.4f), Mat.NeonCyan, 0f);
            }

            Beacon(b, m, new Vector3(-0.2f, 2.9f, -2.0f));
        }

        /// <summary>Reactor stages 1-3: RTG casks on a pallet, then a shielded cask rack, then a small drum on a plinth.</summary>
        private static void ReactorCasks(MeshBuilder b, Model m, int stage, uint seed)
        {
            var rng = new ArtRandom(seed + 9);
            int casks = stage == 1 ? 2 : 4;
            if (stage >= 3)
            {
                // a first containment drum on a plinth, its control hut and a short exhaust
                b.BoxOn(0f, 0, 0.6f, 5.2f, 0.22f, 4.4f, Mat.Concrete, 0.05f);
                Vector3 drum = new Vector3(-0.9f, 0.22f, 1.2f);
                b.Frustum(drum, 1.15f, 1.1f, 0.3f, 18, Mat.ConcreteDark, 0.05f);
                b.Frustum(drum + new Vector3(0, 0.3f, 0), 0.95f, 0.9f, 1.9f, 18, Mat.Concrete, 0.04f);
                b.Frustum(drum + new Vector3(0, 0.6f, 0), 0.96f, 0.95f, 0.18f, 18, Mat.PaintRed, 0.01f, false);
                b.Sphere(drum + new Vector3(0, 2.2f, 0), new Vector3(0.9f, 0.5f, 0.9f), 6, 18, Mat.ConcreteDark, 0f, 0.5f);
                Shapes.Lamp(b, m, drum + new Vector3(0, 2.8f, 0), Mat.LampAmber, Model.Amber, 0.8f, 3.5f, LightRole.Status, 0.08f);
                Props.Container(b, m, new Vector3(1.4f, 0.22f, -1.1f), 2.4f, 2.4f, 1.8f, Mat.SandSteel, true, rng.NextUInt(), "RX");
                Stack(b, m, new Vector3(1.6f, 0.22f, 1.9f), 0.14f, 3.6f, false);
                KitParts.Pipe(b, new[] { new Vector3(drum.X + 0.9f, 1.2f, drum.Z), new Vector3(1.6f, 1.2f, drum.Z), new Vector3(1.6f, 1.2f, 1.7f) }, 0.08f, Mat.Rust);
                casks = 0;
            }

            // RTG casks: finned drums on a pallet, behind a lead-sheet shield from stage 2
            for (int i = 0; i < casks; i++)
            {
                Vector3 c = new Vector3(-1.4f + ((i % 2) * 0.9f), 0.14f, -0.2f + ((i / 2) * 0.9f));
                b.Frustum(c, 0.32f, 0.32f, 0.9f, 12, Mat.OliveSteel, 0.03f);
                for (int k = 0; k < 4; k++)
                {
                    b.Push(c, k * 45f);
                    b.Box(new Vector3(0, 0.45f, 0), new Vector3(0.84f, 0.7f, 0.03f), Mat.DarkSteel, 0f);
                    b.Pop();
                }

                b.Box(c + new Vector3(0, 0.93f, 0), new Vector3(0.24f, 0.06f, 0.24f), Mat.PaintYellow, 0.01f);
            }

            if (casks > 0)
            {
                KitModules.Pallet(b, new Vector3(-0.95f, 0, 0.25f), 0f, 1);
                Props.Lamp(b, m, new Vector3(0.3f, 1.4f, -1.0f), false, 0.7f, LightRole.Status);
            }

            if (stage == 2)
            {
                b.BoxOn(-0.95f, 0, -1.1f, 2.4f, 1.5f, 0.12f, Mat.DarkSteel, 0.01f);
                b.BoxOn(-0.95f, 0, -1.25f, 2.6f, 0.15f, 0.25f, Mat.Concrete, 0.02f);
            }

            // hazard fence and placard around the yard
            KitModules.Fence(b, new Vector3(-2.9f, 0, -2.6f), new Vector3(2.9f, 0, -2.6f), 1.6f);
            b.Box(new Vector3(0.2f, 1.1f, -2.64f), new Vector3(0.45f, 0.45f, 0.03f), Mat.PaintYellow, 0.01f);
            KitModules.Barrels(b, new Vector3(2.3f, 0, 2.3f), 2, rng);
            Beacon(b, m, new Vector3(1.2f, 1.8f, -1.8f));
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

        /// <summary>
        /// Server Rack by visual stage 1-10 (SPEC-045 stage plan). Preview only, like <see cref="GeneratorStage"/>.
        /// Stages 1-3 are open rack rows before the container; from stage 4 each stage adds to the one before.
        /// </summary>
        private static void ComputeStage(MeshBuilder b, Model m, int stage, uint seed)
        {
            var rng = new ArtRandom(seed + 2);
            stage = Math.Min(10, Math.Max(1, stage));
            if (stage <= 3)
            {
                ComputeYard(b, m, stage, rng, seed);
                Beacon(b, m, new Vector3(1.6f, 2.0f, -0.4f));
                return;
            }

            // 4: server container, two outdoor racks under a lean-to, roof condensers, ground chiller, antenna mast
            b.BoxOn(0.3f, 0, 1.3f, 5.8f, 0.2f, 2.8f, Mat.Concrete, 0.05f);
            KitModules.ContainerBlock(b, m, new Vector3(0.3f, 0.2f, 1.3f), 5.4f, Mat.SandSteel, true, "SERVER", rng, 1, false);
            for (int r = 0; r < 2; r++)
            {
                for (int i = 0; i < 6; i++)
                {
                    b.Box(new Vector3(2.4f + (r * 0.16f), 0.7f + (i * 0.22f), 0.0f), new Vector3(0.06f, 0.04f, 0.02f), i % 4 == 3 ? Mat.LampAmber : Mat.LampPhosphor, 0f);
                }
            }

            KitModules.LeanTo(b, new Vector3(-1.25f, 0, 0.08f), 2.5f, 1.7f, 2.75f, 2.3f, Mat.Rust);
            for (int i = 0; i < (stage >= 5 ? 4 : 2); i++)
            {
                Rack(b, new Vector3(-2.1f + (i * 0.62f), 0, -0.55f));
            }

            m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(-1.4f, 1.3f, -1.4f)), Model.Phosphor, 1.1f, 4f, LightRole.Status));
            KitParts.Truss(b, new Vector3(-2.4f, 2.25f, -0.2f), new Vector3(-0.1f, 2.25f, -0.2f), 0.14f, 0.025f, Mat.DarkSteel);
            Chiller(b, m, new Vector3(2.15f, 0, -1.35f), 2, seed + 7);
            KitParts.Pipe(b, new[] { new Vector3(1.55f, 0.5f, -1.0f), new Vector3(1.2f, 0.5f, -1.0f), new Vector3(1.2f, 0.5f, -0.3f), new Vector3(1.2f, 1.1f, -0.05f) }, 0.07f, Mat.DarkSteel);
            KitParts.Pipe(b, new[] { new Vector3(1.55f, 0.8f, -1.3f), new Vector3(0.95f, 0.8f, -1.3f), new Vector3(0.95f, 0.8f, -0.3f), new Vector3(0.95f, 1.3f, -0.05f) }, 0.07f, Mat.DarkSteel);

            // 8 raises the mast and doubles its panel antennas
            float mastH = stage >= 8 ? 12.5f : 7.5f;
            Vector3 mb = new Vector3(-2.7f, 0, 2.75f);
            KitParts.Mast(b, m, mb, mastH, true);
            for (int i = 0; i < (stage >= 8 ? 6 : 3); i++)
            {
                float a = MeshBuilder.Deg((i * 120f) + 30f);
                float y = mastH - 1.6f - ((i / 3) * 2.4f);
                Vector3 c = mb + new Vector3((float)Math.Cos(a) * 0.32f, y, (float)Math.Sin(a) * 0.32f);
                b.Push(c, (-i * 120f) - 30f);
                b.Box(Vector3.Zero, new Vector3(0.08f, 1.1f, 0.3f), Mat.PaintWhite, 0.02f);
                b.Pop();
            }

            KitParts.Cable(b, mb + new Vector3(0.1f, mastH - 2f, 0), new Vector3(-1.9f, 2.85f, 1.8f), 0.5f, 0.04f);
            KitParts.Cable(b, mb + new Vector3(0.1f, mastH - 2.3f, 0.05f), new Vector3(-1.8f, 2.85f, 2.0f), 0.6f, 0.04f);
            KitParts.Truss(b, new Vector3(-2.3f, 2.85f, 1.9f), new Vector3(-0.4f, 2.85f, 1.9f), 0.12f, 0.025f, Mat.DarkSteel);
            if (stage < 7)
            {
                Condenser(b, m, new Vector3(0.9f, 2.8f, 1.35f), seed + 11);
                Condenser(b, m, new Vector3(2.2f, 2.8f, 1.35f), seed + 12);
            }

            KitModules.CrateStack(b, new Vector3(-2.3f, 0, -2.5f), rng);
            b.CylinderZ(new Vector3(0.2f, 0.45f, -2.45f), 0.45f, 0.08f, 14, Mat.Wood, 0.01f);
            b.CylinderZ(new Vector3(0.2f, 0.45f, -2.0f), 0.45f, 0.08f, 14, Mat.Wood, 0.01f);
            b.CylinderZ(new Vector3(0.2f, 0.45f, -2.22f), 0.32f, 0.4f, 14, Mat.Rubber, 0.02f);

            if (stage >= 6)
            {
                Dish(b, m, new Vector3(-1.35f, 2.8f, 1.25f), 0.95f, seed + 13);
            }

            if (stage >= 7)
            {
                // uplink container stacked on the server hall, ladder and railing
                b.Push(new Vector3(1.3f, 2.8f, 1.45f), -4f);
                KitModules.ContainerBlock(b, m, Vector3.Zero, 3.1f, Mat.OliveSteel, true, "UPLINK", rng, 1, false);
                Condenser(b, m, new Vector3(-0.6f, 2.6f, 0.1f), seed + 14);
                KitParts.AcUnit(b, new Vector3(0.8f, 2.6f, 0.2f));
                b.Pop();
                KitParts.Ladder(b, new Vector3(3.1f, 0, 1.0f), 2.8f, -90f);
                Props.Railing(b, new Vector3(-0.3f, 2.8f, 0.15f), new Vector3(-0.3f, 2.8f, 2.4f));
            }

            if (stage >= 8)
            {
                Dish(b, m, new Vector3(1.9f, 5.4f, 1.6f), 0.75f, seed + 15);
            }

            if (stage >= 9)
            {
                // military: armour plates in front of the rack row, sandbags on the open flank, a camera over the yard
                for (int i = 0; i < 2; i++)
                {
                    KitParts.Plate(b, new Vector3(-1.85f + (i * 1.25f), 0.95f, -1.45f), 1.15f, 1.7f, 6f, Mat.DarkSteel);
                }

                Shapes.SandbagWall(b, new Vector3(-3.05f, 0, -2.1f), new Vector3(-3.05f, 0, 0.0f), 3);
                Vector3 cam = new Vector3(3.0f, 0, -2.9f);
                b.Strut(cam, cam + new Vector3(0, 3.4f, 0), 0.06f, Mat.DarkSteel);
                b.Box(cam + new Vector3(0, 3.45f, 0.12f), new Vector3(0.18f, 0.16f, 0.4f), Mat.PaintWhite, 0.02f);
                Shapes.Lamp(b, m, cam + new Vector3(0, 3.45f, -0.1f), Mat.LampRed, Model.Red, 0.3f, 1.2f, LightRole.Status, 0.04f);
            }

            if (stage >= 10)
            {
                // integrated: cyan status rails on the rack row and the uplink, a shielded conduit to the plot edge
                b.Box(new Vector3(-1.25f, 2.1f, -0.92f), new Vector3(2.4f, 0.04f, 0.04f), Mat.NeonCyan, 0f);
                b.Box(new Vector3(1.3f, 5.42f, 0.2f), new Vector3(2.9f, 0.05f, 0.05f), Mat.NeonCyan, 0f);
                b.BoxOn(-0.2f, 0, -2.0f, 0.45f, 0.3f, 2.2f, Mat.DarkSteel, 0.02f);
                b.Box(new Vector3(-0.2f, 0.31f, -2.0f), new Vector3(0.1f, 0.02f, 2.0f), Mat.NeonCyan, 0f);
                Shapes.Lamp(b, m, new Vector3(-1.25f, 2.0f, -1.0f), Mat.NeonCyan, Model.Neon, 0.8f, 3.5f, LightRole.Status, 0.08f);
            }

            Beacon(b, m, new Vector3(3.0f, 3.0f, 0.1f));
        }

        /// <summary>Server Rack stages 1-3: racks on pallets under a tarp, then a roofed row on a plinth.</summary>
        private static void ComputeYard(MeshBuilder b, Model m, int stage, ArtRandom rng, uint seed)
        {
            int racks = stage == 1 ? 2 : (stage == 2 ? 3 : 4);
            float y = 0f;
            if (stage >= 3)
            {
                // stabilized: plinth, block back wall, lean-to with a truss, two-fan chiller piped to the row
                y = 0.2f;
                b.BoxOn(-0.4f, 0, 0.3f, 5.4f, y, 2.6f, Mat.Concrete, 0.05f);
                b.BoxOn(-0.4f, y, 1.5f, 5.4f, 2.6f, 0.25f, Mat.ConcreteDark, 0.06f);
                KitModules.LeanTo(b, new Vector3(-1.1f, y, 1.35f), 3.2f, 2.1f, 2.6f, 2.2f, Mat.Rust);
                KitParts.Truss(b, new Vector3(-2.6f, 2.3f, -0.2f), new Vector3(0.4f, 2.3f, -0.2f), 0.14f, 0.025f, Mat.DarkSteel);
                Chiller(b, m, new Vector3(1.75f, y, 0.3f), 2, seed + 7);
                KitParts.Pipe(b, new[] { new Vector3(0.8f, y + 0.6f, 0.3f), new Vector3(0.45f, y + 0.6f, 0.3f), new Vector3(0.45f, y + 0.6f, 0.95f) }, 0.07f, Mat.DarkSteel);
            }
            else
            {
                // improvised: pallets, a blue tarp on timber poles, a box fan blowing on the racks
                float[] px = { -2.6f, 0.2f };
                float[] pz = { -1.0f, 0.8f };
                foreach (float x in px)
                {
                    foreach (float z in pz)
                    {
                        b.Strut(new Vector3(x, 0, z), new Vector3(x, z > 0f ? 2.6f : 2.25f, z), 0.06f, Mat.Wood);
                    }
                }

                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-12f)) * Matrix4x4.CreateTranslation(new Vector3(-1.2f, 2.45f, -0.1f)));
                b.Box(Vector3.Zero, new Vector3(3.3f, 0.03f, 2.1f), Mat.TarpBlue, 0.01f);
                b.Pop();
                for (int i = 0; i < racks; i++)
                {
                    KitModules.Pallet(b, new Vector3(-2.1f + (i * 0.62f), 0, -0.2f), 0f, 1);
                }

                y = 0.14f;
                b.BoxOn(-0.1f, 0, -0.9f, 0.5f, 0.5f, 0.2f, Mat.SandSteel, 0.03f);
                m.Parts.Add(Fan(new Vector3(-0.1f, 0.5f, -0.9f), 0.2f, 600f, seed + 9));
                KitParts.Barrel(b, new Vector3(0.9f, 0, -0.9f), Mat.Rust);
            }

            for (int i = 0; i < racks; i++)
            {
                Rack(b, new Vector3(-2.1f + (i * 0.62f), y, -0.2f));
            }

            m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(-1.4f, 1.3f, -1.1f)), Model.Phosphor, stage >= 3 ? 1.0f : 0.7f, 3.5f, LightRole.Status));
            if (stage >= 2)
            {
                // car batteries on a bench and a short antenna pole
                b.BoxOn(1.6f, 0, -1.9f, 1.4f, 0.55f, 0.6f, Mat.Wood, 0.03f);
                for (int i = 0; i < 4; i++)
                {
                    b.BoxOn(1.1f + (i * 0.33f), 0.55f, -1.9f, 0.26f, 0.2f, 0.17f, Mat.Rubber, 0.01f);
                }

                b.Strut(new Vector3(-2.9f, 0, 1.6f), new Vector3(-2.9f, 4.5f, 1.6f), 0.05f, Mat.DarkSteel);
                b.Box(new Vector3(-2.9f, 4.2f, 1.6f), new Vector3(0.08f, 0.8f, 0.25f), Mat.PaintWhite, 0.02f);
                KitParts.Cable(b, new Vector3(-2.9f, 3.8f, 1.6f), new Vector3(-2.1f, y + 2.0f, -0.2f), 0.4f, 0.03f);
                KitModules.CrateStack(b, new Vector3(-2.3f, 0, -2.5f), rng);
            }

            KitParts.Cable(b, new Vector3(-1.5f, y + 0.2f, -0.2f), new Vector3(-1.2f, 0.05f, -3.1f), 0.1f, 0.04f);
            b.CylinderZ(new Vector3(0.2f, 0.45f, -2.3f), 0.45f, 0.08f, 14, Mat.Wood, 0.01f);
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

            if (level < 3)
            {
                // the greenhouse takes this corner from level 3
                KitModules.Workbench(b, m, new Vector3(-2.0f, 0, -2.3f), 0f);
            }

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

        /// <summary>
        /// Life Support by visual stage 1-10 (SPEC-045 stage plan). Preview only, like <see cref="GeneratorStage"/>.
        /// Stages 1-2 are tents; the med bay container arrives at 3 and each later stage adds to the one before.
        /// </summary>
        private static void HabitatStage(MeshBuilder b, Model m, int stage, uint seed)
        {
            var rng = new ArtRandom(seed + 3);
            stage = Math.Min(10, Math.Max(1, stage));
            if (stage <= 2)
            {
                HabitatCamp(b, m, stage, rng);
                Beacon(b, m, new Vector3(1.5f, 1.9f, 0.4f));
                return;
            }

            // 3: med bay container on a plinth, decked porch under a lean-to, clinic sign, laundry, workbench
            b.BoxOn(-0.2f, 0, 1.35f, 6.0f, 0.2f, 2.8f, Mat.Concrete, 0.05f);
            KitModules.ContainerBlock(b, m, new Vector3(-0.2f, 0.2f, 1.35f), 5.6f, Mat.Rust, true, "MED BAY", rng, 2, false);
            KitModules.LeanTo(b, new Vector3(-1.1f, 0, 0.13f), 2.9f, 1.6f, 2.75f, 2.3f, Mat.OliveSteel);
            b.BoxOn(-1.1f, 0, -0.65f, 2.9f, 0.12f, 1.5f, Mat.Wood, 0.01f);
            Props.Lamp(b, m, new Vector3(-1.1f, 2.15f, -0.4f), true, 1.3f, LightRole.Status);
            b.Strut(new Vector3(1.4f, 2.8f, 0.2f), new Vector3(1.4f, 3.7f, 0.2f), 0.06f, Mat.DarkSteel);
            b.Strut(new Vector3(2.2f, 2.8f, 0.2f), new Vector3(2.2f, 3.7f, 0.2f), 0.06f, Mat.DarkSteel);
            b.Box(new Vector3(1.8f, 3.55f, 0.16f), new Vector3(1.0f, 0.7f, 0.05f), Mat.PaintWhite, 0.02f);
            b.Box(new Vector3(1.8f, 3.55f, 0.12f), new Vector3(0.5f, 0.15f, 0.03f), Mat.PaintGreen, 0f);
            b.Box(new Vector3(1.8f, 3.55f, 0.12f), new Vector3(0.15f, 0.5f, 0.03f), Mat.PaintGreen, 0f);
            Vector3 pole = new Vector3(2.85f, 0, -2.3f);
            b.Strut(pole, pole + new Vector3(0, 2.3f, 0), 0.06f, Mat.Wood);
            Laundry(b, new Vector3(0.35f, 2.15f, -1.45f), pole + new Vector3(0, 2.2f, 0), rng);
            if (stage < 6)
            {
                // the greenhouse takes this corner from stage 6
                KitModules.Workbench(b, m, new Vector3(-2.0f, 0, -2.3f), 0f);
            }

            KitModules.Barrels(b, new Vector3(2.55f, 0, -0.5f), 2, rng);
            KitModules.Pallet(b, new Vector3(0.9f, 0, -2.5f), -8f, 1);

            if (stage >= 4)
            {
                // roof services: header tank, solar water heater, AC unit
                KitParts.TankV(b, new Vector3(-2.4f, 2.8f, 1.7f), 0.42f, 0.9f, Mat.OliveSteel, seed + 4);
                SolarPanel(b, new Vector3(-0.9f, 2.8f, 1.6f), 1.5f, 1.1f, 25f);
                KitParts.AcUnit(b, new Vector3(0.6f, 2.8f, 1.9f));
                KitModules.TarpPile(b, new Vector3(1.9f, 2.8f, 1.8f), 1.0f, rng);
            }

            if (stage >= 5)
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

            if (stage >= 6)
            {
                // greenhouse tunnel in the front-left yard corner
                Shapes.ArchX(b, new Vector3(-1.95f, 0, -2.45f), 0.68f, 2.3f, 8, Mat.Glass, Mat.DarkSteel);
                for (int i = 0; i < 5; i++)
                {
                    b.Sphere(new Vector3(-2.85f + (i * 0.45f), 0.15f, -2.45f), new Vector3(0.2f, 0.28f, 0.2f), 3, 6, Mat.Foliage);
                }

                Shapes.Lamp(b, m, new Vector3(-1.95f, 0.55f, -2.45f), Mat.LampPhosphor, Model.Phosphor, 0.7f, 2.5f, LightRole.Status, 0.07f);
            }

            if (stage >= 7)
            {
                ElevatedTank(b, new Vector3(-2.55f, 0, 3.05f), 3.6f, 0.62f, 1.5f, Mat.SandSteel, seed + 5);
                KitParts.Pipe(b, new[] { new Vector3(-2.55f, 3.6f, 2.5f), new Vector3(-2.55f, 3.0f, 2.3f), new Vector3(-2.4f, 2.8f, 2.1f) }, 0.05f, Mat.DarkSteel);
            }

            if (stage >= 8)
            {
                // rooftop shack on the bunks, aerial, string of lamps along the balcony
                KitModules.Shed(b, new Vector3(0.9f, 5.4f, 1.6f), 2.2f, 1.6f, 1.9f, 2.2f, Mat.Rust, false);
                b.BoxOn(0.9f, 5.4f, 2.3f, 2.2f, 1.9f, 0.06f, Mat.Rust, 0.01f);
                b.Frustum(new Vector3(-0.6f, 5.4f, 2.1f), 0.04f, 0.02f, 3.2f, 6, Mat.DarkSteel, 0.01f);
                for (int i = 0; i < 4; i++)
                {
                    Shapes.Lamp(b, m, new Vector3(-1.4f + (i * 1.2f), 4.15f - (i % 2 * 0.06f), -0.62f), Mat.LampAmber, Model.Amber, 0.5f, 2.5f, LightRole.Ambient, 0.06f);
                }
            }

            if (stage >= 9)
            {
                // hardened: sandbag wall across the yard front, armour shutters on the balcony, extra AC on the bunks
                Shapes.SandbagWall(b, new Vector3(0.3f, 0, -3.0f), new Vector3(2.4f, 0, -3.0f), 3);
                for (int i = 0; i < 3; i++)
                {
                    KitParts.Plate(b, new Vector3(-0.9f + (i * 1.5f), 3.4f, -0.68f), 1.3f, 0.9f, 4f, Mat.DarkSteel);
                }

                KitParts.AcUnit(b, new Vector3(-1.3f, 5.4f, 1.9f));
            }

            if (stage >= 10)
            {
                // integrated: cyan status line under the balcony, shielded conduit out, cyan lamp over the porch
                b.Box(new Vector3(0.6f, 2.74f, -0.62f), new Vector3(4.4f, 0.04f, 0.04f), Mat.NeonCyan, 0f);
                b.BoxOn(2.9f, 0, -0.9f, 0.45f, 0.3f, 3.6f, Mat.DarkSteel, 0.02f);
                b.Box(new Vector3(2.9f, 0.31f, -0.9f), new Vector3(0.1f, 0.02f, 3.4f), Mat.NeonCyan, 0f);
                Shapes.Lamp(b, m, new Vector3(-1.1f, 2.0f, -0.1f), Mat.NeonCyan, Model.Neon, 0.7f, 3.0f, LightRole.Status, 0.07f);
            }

            Beacon(b, m, new Vector3(-2.9f, 3.0f, 0.1f));
        }

        /// <summary>Life Support stages 1-2: a canvas tent with a water barrel and hand pump, then a second tent and rain catcher.</summary>
        private static void HabitatCamp(MeshBuilder b, Model m, int stage, ArtRandom rng)
        {
            Shapes.ArchX(b, new Vector3(-1.2f, 0, 0.9f), 1.05f, 2.6f, 8, Mat.Tarp, Mat.Tarp);
            Props.Lamp(b, m, new Vector3(-1.2f, 0.9f, -0.5f), false, 0.8f, LightRole.Status);
            KitParts.Barrel(b, new Vector3(0.9f, 0, -0.6f), Mat.TarpBlue);
            b.Strut(new Vector3(1.4f, 0, -0.6f), new Vector3(1.4f, 0.95f, -0.6f), 0.05f, Mat.DarkSteel);
            b.Strut(new Vector3(1.4f, 0.95f, -0.6f), new Vector3(1.0f, 1.05f, -0.6f), 0.03f, Mat.DarkSteel);
            Vector3 pole = new Vector3(2.6f, 0, 1.4f);
            b.Strut(pole, pole + new Vector3(0, 2.0f, 0), 0.05f, Mat.Wood);
            Laundry(b, new Vector3(0.2f, 1.4f, 1.6f), pole + new Vector3(0, 1.9f, 0), rng);
            KitModules.CrateStack(b, new Vector3(-2.6f, 0, -1.9f), rng);
            if (stage >= 2)
            {
                // second tent across the yard, a tarp catching rain into barrels, a workbench in the open
                Shapes.ArchX(b, new Vector3(1.3f, 0, -2.0f), 0.85f, 2.2f, 8, Mat.Tarp, Mat.Tarp);
                b.Strut(new Vector3(-2.9f, 0, 2.4f), new Vector3(-2.9f, 2.2f, 2.4f), 0.05f, Mat.Wood);
                b.Strut(new Vector3(-1.3f, 0, 2.4f), new Vector3(-1.3f, 2.2f, 2.4f), 0.05f, Mat.Wood);
                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(25f)) * Matrix4x4.CreateTranslation(new Vector3(-2.1f, 1.7f, 2.8f)));
                b.Box(Vector3.Zero, new Vector3(1.9f, 0.02f, 1.2f), Mat.TarpBlue, 0.01f);
                b.Pop();
                KitParts.Barrel(b, new Vector3(-2.4f, 0, 3.2f), Mat.Rust);
                KitParts.Barrel(b, new Vector3(-1.8f, 0, 3.2f), Mat.Rust);
                KitModules.Workbench(b, m, new Vector3(-1.0f, 0, -2.4f), 0f);
            }
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
                KitParts.SolarModule(b, 1.85f, 1.75f, rng.Next() < 0.25f);
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

        /// <summary>
        /// Battery Bank by visual stage 1-10 (SPEC-045 stage plan). Preview only, like <see cref="GeneratorStage"/>.
        /// Stages 1-2 are car batteries on benches; the cell shed arrives at 3 and fills with cabinets, panels and towers.
        /// </summary>
        private static void BatteryStage(MeshBuilder b, Model m, int stage, uint seed)
        {
            var rng = new ArtRandom(seed + 4);
            stage = Math.Min(10, Math.Max(1, stage));
            if (stage <= 2)
            {
                BatteryBenches(b, m, stage, rng);
                Beacon(b, m, new Vector3(1.4f, 1.6f, -0.2f));
                return;
            }

            b.BoxOn(0, 0, 0.35f, 6.3f, 0.15f, 5.2f, Mat.Concrete, 0.05f);
            const float y0 = 0.15f;
            const float zFront = -1.55f;
            const float zBack = 2.45f;
            const float hFront = 3.0f;
            const float hBack = 3.6f;
            KitModules.Shed(b, new Vector3(0, y0, (zFront + zBack) * 0.5f), 6.0f, zBack - zFront, hFront, hBack, Mat.Rust);

            // roof panels from 4, cabinets fill the shed two at a time
            float pitch = (float)Math.Atan2(hBack - hFront, zBack - zFront);
            int panels = stage >= 4 ? Math.Min(stage - 1, 6) : 0;
            for (int i = 0; i < panels; i++)
            {
                float z = zFront + 0.9f + ((i / 3) * 2.0f);
                float y = y0 + hFront + 0.12f + ((z - zFront) * (hBack - hFront) / (zBack - zFront)) + 0.12f;
                b.Push(Matrix4x4.CreateRotationX(-pitch) * Matrix4x4.CreateTranslation(new Vector3(-1.95f + ((i % 3) * 1.95f), y, z)));
                KitParts.SolarModule(b, 1.85f, 1.75f, rng.Next() < 0.25f);
                b.Pop();
            }

            int cabinets = Math.Min(4 + ((stage - 3) * 2), 14);
            for (int i = 0; i < cabinets; i++)
            {
                int row = i / 7;
                float x = -2.4f + ((i % 7) * 0.8f);
                float z = -0.5f + (row * 1.5f);
                b.BoxOn(x, y0, z, 0.66f, 1.4f, 0.7f, Mat.DarkSteel, 0.04f);
                b.Box(new Vector3(x - 0.14f, y0 + 1.44f, z), new Vector3(0.1f, 0.08f, 0.1f), Mat.Copper, 0.02f);
                b.Box(new Vector3(x + 0.14f, y0 + 1.44f, z), new Vector3(0.1f, 0.08f, 0.1f), Mat.Copper, 0.02f);
                b.Box(new Vector3(x, y0 + 1.1f, z - 0.36f), new Vector3(stage >= 10 ? 0.4f : 0.1f, 0.06f, 0.02f), stage >= 10 ? Mat.NeonCyan : Mat.LampPhosphor, 0f);
                b.Box(new Vector3(x, y0 + 0.65f, z - 0.36f), new Vector3(0.4f, 0.3f, 0.02f), Mat.PaintWhite, 0f);
                b.Strut(new Vector3(x, y0 + 1.48f, z), new Vector3(x, y0 + hFront - 0.2f, zFront + 0.3f), 0.025f, Mat.Rubber);
            }

            KitParts.Truss(b, new Vector3(-2.9f, y0 + hFront - 0.35f, zFront + 0.3f), new Vector3(2.9f, y0 + hFront - 0.35f, zFront + 0.3f), 0.16f, 0.025f, Mat.DarkSteel);
            m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(0, 1.4f, -1.4f)), stage >= 10 ? Model.Neon : Model.Phosphor, 0.9f, 4f, LightRole.Status));
            b.BoxOn(1.9f, y0 + 1.8f, zFront - 0.05f, 1.7f, 0.5f, 0.05f, Mat.DarkSteel, 0.01f);
            Props.Stencil(b, "CELLS", new Vector3(1.3f, y0 + 1.88f, zFront - 0.09f), 0.055f, Mat.PaintWhite);
            Props.Lamp(b, m, new Vector3(-1.4f, y0 + hFront - 0.15f, zFront + 0.1f), true, 1.2f, LightRole.Status);
            KitModules.Barrels(b, new Vector3(-0.6f, y0, -2.6f), 2, rng);

            if (stage >= 4)
            {
                // inverter and the first capacitor tower out front, fenced
                KitParts.Transformer(b, new Vector3(2.35f, y0, -2.45f), 0.7f);
                Capacitor(b, new Vector3(-2.5f, y0, -2.45f), 2.4f);
                KitModules.Fence(b, new Vector3(-3.15f, 0, -3.1f), new Vector3(-1.4f, 0, -3.1f), 1.4f);
                Shapes.Hazard(b, -2.9f, -1.7f, 0.9f, 1.1f, -3.12f, 6);
            }

            if (stage >= 6)
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

            if (stage >= 7)
            {
                Capacitor(b, new Vector3(-1.75f, y0, -2.55f), 2.0f);
            }

            if (stage >= 8)
            {
                Capacitor(b, new Vector3(1.0f, y0, -2.6f), 2.8f);
                Shapes.Lamp(b, m, new Vector3(0, 4.55f, -2.95f), Mat.LampRed, Model.Red, 0.9f, 3.5f, LightRole.Status, 0.12f);
            }

            if (stage >= 9)
            {
                // blast wall between the two cabinet rows and sandbags along the open sides
                b.BoxOn(0, y0, 0.25f, 5.6f, 1.6f, 0.2f, Mat.ConcreteDark, 0.04f);
                Shapes.Hazard(b, -2.6f, 2.6f, y0 + 1.45f, y0 + 1.6f, 0.14f, 14);
                Shapes.SandbagWall(b, new Vector3(-3.1f, 0, -1.9f), new Vector3(-3.1f, 0, 2.2f), 2);
                Shapes.SandbagWall(b, new Vector3(3.1f, 0, -1.2f), new Vector3(3.1f, 0, 2.2f), 2);
            }

            if (stage >= 10)
            {
                // integrated: cyan charge meters (above), a cyan ring on each capacitor tower, a shielded conduit out
                foreach (float x in new[] { -2.5f, -1.75f, 1.0f })
                {
                    b.Frustum(new Vector3(x, y0 + 1.1f, x > 0 ? -2.6f : (x < -2f ? -2.45f : -2.55f)), 0.36f, 0.36f, 0.05f, 10, Mat.NeonCyan, 0f, false);
                }

                b.BoxOn(0.1f, 0, -2.95f, 2.0f, 0.3f, 0.45f, Mat.DarkSteel, 0.02f);
                b.Box(new Vector3(0.1f, 0.31f, -2.95f), new Vector3(1.8f, 0.02f, 0.1f), Mat.NeonCyan, 0f);
            }

            Beacon(b, m, new Vector3(2.85f, 3.1f, -1.55f));
        }

        /// <summary>Battery Bank stages 1-2: car batteries on a bench under a tarp, then a second bench and a propped panel.</summary>
        private static void BatteryBenches(MeshBuilder b, Model m, int stage, ArtRandom rng)
        {
            for (int k = 0; k < stage; k++)
            {
                float z = -0.3f + (k * 1.4f);
                b.BoxOn(-0.8f, 0, z, 2.6f, 0.75f, 0.8f, Mat.Wood, 0.03f);
                for (int i = 0; i < 6; i++)
                {
                    float x = -1.85f + (i * 0.42f);
                    b.BoxOn(x, 0.75f, z, 0.32f, 0.22f, 0.2f, Mat.Rubber, 0.01f);
                    b.Box(new Vector3(x - 0.08f, 0.99f, z), new Vector3(0.05f, 0.03f, 0.05f), Mat.PaintRed, 0f);
                }

                KitParts.Cable(b, new Vector3(-1.85f, 1.0f, z - 0.1f), new Vector3(0.25f, 1.0f, z - 0.1f), 0.12f, 0.02f, Mat.PaintRed);
            }

            // tarp roof on timber poles over the benches
            float zBack = -0.3f + ((stage - 1) * 1.4f) + 0.7f;
            foreach (float x in new[] { -2.4f, 0.8f })
            {
                b.Strut(new Vector3(x, 0, -1.0f), new Vector3(x, 2.1f, -1.0f), 0.06f, Mat.Wood);
                b.Strut(new Vector3(x, 0, zBack), new Vector3(x, 2.4f, zBack), 0.06f, Mat.Wood);
            }

            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-10f)) * Matrix4x4.CreateTranslation(new Vector3(-0.8f, 2.3f, (zBack - 1.0f) * 0.5f)));
            b.Box(Vector3.Zero, new Vector3(3.5f, 0.03f, zBack + 1.3f), Mat.TarpBlue, 0.01f);
            b.Pop();
            KitParts.UtilityBox(b, new Vector3(1.5f, 0.6f, -0.3f));
            b.Strut(new Vector3(1.5f, 0, -0.3f), new Vector3(1.5f, 0.35f, -0.3f), 0.04f, Mat.DarkSteel);
            Props.Lamp(b, m, new Vector3(-0.8f, 2.0f, -0.6f), false, 0.8f, LightRole.Status);
            KitModules.CrateStack(b, new Vector3(2.3f, 0, -2.3f), rng);
            if (stage >= 2)
            {
                // one salvaged panel propped against a crate, wired to the bench
                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-35f)) * Matrix4x4.CreateTranslation(new Vector3(1.6f, 0.6f, 1.8f)));
                KitParts.SolarModule(b, 1.6f, 1.1f, true);
                b.Pop();
                KitParts.Cable(b, new Vector3(1.0f, 0.4f, 1.6f), new Vector3(0.2f, 0.9f, 1.1f), 0.2f, 0.02f);
                KitModules.Barrels(b, new Vector3(-2.6f, 0, -2.3f), 2, rng);
            }
        }

        // ---------- Turret: octagonal emplacement, raised gun tower, radar, tank traps ----------
        private static void Turret(MeshBuilder b, Model m, int level, uint seed)
        {
            TurretCore(b, m, seed, level >= 3, level >= 5 ? 4 : (level >= 2 ? 2 : 1), level >= 5 ? 3 : 2, level >= 2, level >= 4, level >= 5, 0.08f + (level * 0.01f), 0);
        }

        /// <summary>
        /// Turret by visual stage 1-10 (SPEC-045 stage plan). Preview only, like <see cref="GeneratorStage"/>. Stages 1-2
        /// are a machine-gun nest; the emplacement arrives at 3 and the level features follow, then bunker, armour, laser.
        /// </summary>
        private static void TurretStage(MeshBuilder b, Model m, int stage, uint seed)
        {
            stage = Math.Min(10, Math.Max(1, stage));
            if (stage <= 2)
            {
                TurretNest(b, m, stage, seed);
                return;
            }

            TurretCore(b, m, seed, stage >= 5, stage >= 7 ? 4 : (stage >= 4 ? 2 : 1), stage >= 7 ? 3 : 2, stage >= 4, stage >= 6, stage >= 7, 0.08f + (Math.Min(stage - 2, 5) * 0.01f), stage);
        }

        /// <param name="stage">SPEC-045 stage for the stage-only extras (8 bunker, 9 armour, 10 laser); 0 for the level models.</param>
        private static void TurretCore(MeshBuilder b, Model m, uint seed, bool tower, int guns, int layers, bool pole, bool searchRadar, bool topLight, float sweep, int stage)
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
            if (tower)
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

            Shapes.SandbagRing(b, new Vector3(0, top, 0), tower ? 1.3f : 1.85f, tower ? 14 : 18, layers, 70f, 40f);
            b.Frustum(new Vector3(0, top, 0), 0.8f, 0.65f, 0.45f, 14, Mat.DarkSteel, 0.05f);
            float pivotY = top + 0.45f;

            // pintle-mounted gun behind an angled, patched gunner shield (no box housing)
            var head = new MeshBuilder(seed + 7) { GroundOffset = pivotY + PadTop };
            float w = tower ? 1.5f : 1.3f;
            head.Frustum(Vector3.Zero, 0.62f, 0.6f, 0.1f, 20, Mat.DarkSteel, 0.02f);
            head.Frustum(new Vector3(0, 0.1f, 0.1f), 0.14f, 0.12f, 0.5f, 12, Mat.OliveSteel, 0.02f);
            foreach (int s in new[] { -1, 1 })
            {
                // cradle side plates
                head.Box(new Vector3(s * 0.2f, 0.62f, 0.05f), new Vector3(0.03f, 0.34f, 0.7f), Mat.OliveSteel, 0.01f);
            }

            for (int i = 0; i < guns; i++)
            {
                float x = guns == 1 ? 0f : (-0.13f * (guns - 1)) + (i * 0.26f);
                float y = guns == 4 ? (i % 2 == 0 ? 0.56f : 0.74f) : 0.65f;
                float xx = x * (guns == 4 ? 0.62f : 1f);
                head.Box(new Vector3(xx, y, 0.15f), new Vector3(0.13f, 0.15f, 0.75f), Mat.DarkSteel, 0.015f);
                head.CylinderZ(new Vector3(xx, y, -0.55f), 0.055f, 0.65f, 12, Mat.DarkSteel, 0.008f);
                head.CylinderZ(new Vector3(xx, y, -1.15f), 0.028f, 0.6f, 8, Mat.DarkSteel, 0.004f);
                head.CylinderZ(new Vector3(xx, y, -1.48f), 0.042f, 0.09f, 10, Mat.DarkSteel, 0.006f);
                // ammo can with a belt into the receiver
                Vector3 can = new Vector3(xx + (xx >= 0 ? 0.17f : -0.17f), y - 0.12f, 0.25f);
                head.Box(can, new Vector3(0.11f, 0.19f, 0.3f), Mat.OliveSteel, 0.01f);
                head.Strut(can + new Vector3(0, 0.1f, -0.05f), new Vector3(xx, y + 0.02f, 0.1f), 0.025f, Mat.Copper);
            }

            // shield: center plate leaning back, two angled wings, welded patch, sight slit
            head.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-16f)) * Matrix4x4.CreateTranslation(new Vector3(0, 0.38f, -0.38f)));
            head.Box(new Vector3(0, 0.38f, 0), new Vector3(w * 0.62f, 0.8f, 0.035f), Mat.OliveSteel, 0.01f);
            head.Box(new Vector3(-w * 0.12f, 0.24f, -0.03f), new Vector3(0.36f, 0.26f, 0.02f), Mat.Rust, 0.004f);
            head.Box(new Vector3(w * 0.12f, 0.66f, -0.025f), new Vector3(0.22f, 0.04f, 0.02f), Mat.Rubber, 0f);
            head.Pop();
            foreach (int s in new[] { -1, 1 })
            {
                head.Push(Matrix4x4.CreateRotationY(MeshBuilder.Deg(s * 34f)) * Matrix4x4.CreateTranslation(new Vector3(s * w * 0.3f, 0.38f, -0.36f)));
                head.Box(new Vector3(s * w * 0.15f, 0.34f, 0), new Vector3(w * 0.3f, 0.7f, 0.03f), Mat.OliveSteel, 0.01f);
                head.Pop();
            }

            // optics box with the red status lamp, grips and a seat behind
            head.Box(new Vector3(w * 0.22f, 0.98f, -0.1f), new Vector3(0.16f, 0.14f, 0.22f), Mat.DarkSteel, 0.02f);
            head.Box(new Vector3(w * 0.22f, 0.98f, -0.22f), new Vector3(0.09f, 0.06f, 0.02f), Mat.LampRed, 0f);
            if (stage >= 10)
            {
                // AI fire control: a cyan rangefinder housing and its aiming line along the guns
                head.Box(new Vector3(-w * 0.22f, 0.98f, -0.1f), new Vector3(0.16f, 0.14f, 0.3f), Mat.DarkSteel, 0.02f);
                head.Box(new Vector3(-w * 0.22f, 0.98f, -1.4f), new Vector3(0.02f, 0.02f, 2.3f), Mat.NeonCyan, 0f);
            }
            head.Strut(new Vector3(-0.16f, 0.62f, 0.55f), new Vector3(-0.16f, 0.5f, 0.75f), 0.03f, Mat.Rubber);
            head.Strut(new Vector3(0.16f, 0.62f, 0.55f), new Vector3(0.16f, 0.5f, 0.75f), 0.03f, Mat.Rubber);
            head.Strut(new Vector3(0, 0.1f, 0.2f), new Vector3(0, 0.42f, 0.9f), 0.04f, Mat.DarkSteel);
            head.Box(new Vector3(0, 0.44f, 0.95f), new Vector3(0.34f, 0.05f, 0.3f), Mat.Rubber, 0.02f);
            if (tower)
            {
                head.Box(new Vector3(0, 0.12f, 0.75f), new Vector3(0.5f, 0.2f, 0.32f), Mat.OliveSteel, 0.02f);
            }

            m.Parts.Add(new AnimPart(head.Mesh, new Vector3(0, pivotY, 0), AnimKind.SweepY, sweep, 55f));
            m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(0.5f, pivotY + 0.7f, -0.9f)), Model.Red, 0.7f, 2.2f, LightRole.Status));

            // ammo store and camo net behind the gun, tank traps out front
            b.Push(new Vector3(-1.9f, 0, 2.2f), 20f);
            b.BoxOn(0, 0, 0, 1.6f, 0.9f, 1.0f, Mat.OliveSteel, 0.04f);
            b.BoxOn(0, 0.9f, 0, 1.2f, 0.4f, 0.8f, Mat.OliveSteel, 0.04f);
            b.Pop();
            KitModules.LeanTo(b, new Vector3(-1.6f, 0, 3.15f), 2.4f, 1.5f, 1.9f, 1.4f, Mat.Tarp);
            KitModules.Hedgehog(b, new Vector3(-2.6f, 0, -2.6f), 20f);
            KitModules.Hedgehog(b, new Vector3(2.65f, 0, -2.5f), -35f);
            Shapes.SandbagWall(b, new Vector3(-1.3f, 0, -2.75f), new Vector3(1.4f, 0, -2.85f), 2);
            KitModules.CrateStack(b, new Vector3(2.3f, 0, 2.3f), rng);

            if (pole)
            {
                Vector3 lightPole = new Vector3(2.6f, 0, 0.6f);
                b.Frustum(lightPole, 0.1f, 0.08f, 4.6f, 8, Mat.DarkSteel, 0.01f);
                KitParts.Spotlight(b, m, lightPole + new Vector3(0, 4.5f, -0.25f), 10f);
                KitParts.Cable(b, lightPole + new Vector3(0, 4.2f, 0), new Vector3(1.0f, top + 0.3f, 1.0f), 0.4f);
            }

            if (searchRadar)
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

            if (topLight)
            {
                KitParts.Spotlight(b, m, new Vector3(-1.0f, top + 1.2f, -1.0f), -20f);
                b.Strut(new Vector3(-1.0f, top, -1.0f), new Vector3(-1.0f, top + 1.1f, -1.0f), 0.06f, Mat.DarkSteel);
            }

            if (stage >= 8)
            {
                // concrete ammo bunker with a blast door and a caged ladder up the tower
                b.BoxOn(2.2f, 0, -0.9f, 1.4f, 1.3f, 1.8f, Mat.ConcreteDark, 0.06f);
                b.Box(new Vector3(1.48f, 0.6f, -0.9f), new Vector3(0.04f, 1.0f, 0.8f), Mat.OliveSteel, 0.01f);
                Shapes.Hazard(b, 1.5f, 2.9f, 1.15f, 1.3f, -1.81f, 6);
            }

            if (stage >= 9)
            {
                // heavier armour: a second ring of plates around the gun deck and extra tank traps
                for (int i = 0; i < 8; i++)
                {
                    b.Push(new Vector3(0, top, 0), (i * 45f) + 22.5f);
                    KitParts.Plate(b, new Vector3(0, 0.45f, -1.75f), 1.3f, 0.9f, 8f, Mat.DarkSteel);
                    b.Pop();
                }

                KitModules.Hedgehog(b, new Vector3(0.2f, 0, -3.0f), 10f);
            }

            Beacon(b, m, new Vector3(-1.5f, top + 0.5f, -1.4f));
        }

        /// <summary>Turret stages 1-2: a machine gun on a tripod behind sandbags, then a full sandbag ring and ammo crates.</summary>
        private static void TurretNest(MeshBuilder b, Model m, int stage, uint seed)
        {
            var rng = new ArtRandom(seed + 5);
            if (stage >= 2)
            {
                Shapes.SandbagRing(b, Vector3.Zero, 1.5f, 16, 2, 70f, 40f);
                KitModules.CrateStack(b, new Vector3(2.0f, 0, 1.8f), rng);
                KitModules.Hedgehog(b, new Vector3(-2.4f, 0, -2.4f), 20f);
            }
            else
            {
                Shapes.SandbagWall(b, new Vector3(-1.2f, 0, -1.0f), new Vector3(1.2f, 0, -1.0f), 2);
            }

            // tripod and the gun on a pivot, sweeping slowly
            foreach (float a in new[] { 0f, 120f, 240f })
            {
                float r = MeshBuilder.Deg(a + 30f);
                b.Strut(new Vector3((float)Math.Cos(r) * 0.55f, 0, (float)Math.Sin(r) * 0.55f), new Vector3(0, 0.75f, 0), 0.03f, Mat.DarkSteel);
            }

            var head = new MeshBuilder(seed + 7) { GroundOffset = 0.8f + PadTop };
            head.Box(new Vector3(0, 0.05f, 0.15f), new Vector3(0.13f, 0.15f, 0.7f), Mat.DarkSteel, 0.015f);
            head.CylinderZ(new Vector3(0, 0.05f, -0.5f), 0.05f, 0.65f, 12, Mat.DarkSteel, 0.008f);
            head.Box(new Vector3(0.17f, -0.05f, 0.2f), new Vector3(0.11f, 0.19f, 0.3f), Mat.OliveSteel, 0.01f);
            m.Parts.Add(new AnimPart(head.Mesh, new Vector3(0, 0.8f, 0), AnimKind.SweepY, 0.06f, 40f));
            m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(0.4f, 1.3f, -0.6f)), Model.Red, 0.5f, 2.0f, LightRole.Status));
            KitModules.Barrels(b, new Vector3(-2.2f, 0, 1.8f), 2, rng);
            Beacon(b, m, new Vector3(-1.0f, 1.2f, -1.0f));
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

        /// <summary>Outdoor server rack cabinet (perforated door, small display, phosphor status lights), facing -Z.</summary>
        private static void Rack(MeshBuilder b, Vector3 baseCenter)
        {
            b.BoxOn(baseCenter.X, baseCenter.Y, baseCenter.Z, 0.56f, 1.95f, 0.7f, Mat.DarkSteel, 0.03f);
            // perforated steel door: vent slot rows, a small status display, hinges and a handle
            b.Box(new Vector3(baseCenter.X, baseCenter.Y + 1.0f, baseCenter.Z - 0.36f), new Vector3(0.5f, 1.8f, 0.02f), Mat.OliveSteel, 0.005f);
            for (int r = 0; r < 9; r++)
            {
                b.Box(new Vector3(baseCenter.X, baseCenter.Y + 0.25f + (r * 0.12f), baseCenter.Z - 0.372f), new Vector3(0.36f, 0.035f, 0.006f), Mat.Rubber, 0f);
            }

            b.Box(new Vector3(baseCenter.X, baseCenter.Y + 1.62f, baseCenter.Z - 0.372f), new Vector3(0.26f, 0.14f, 0.006f), Mat.Screen, 0f);
            b.Box(new Vector3(baseCenter.X + 0.2f, baseCenter.Y + 1.0f, baseCenter.Z - 0.38f), new Vector3(0.03f, 0.22f, 0.03f), Mat.DarkSteel, 0.005f);
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
            KitParts.SolarModule(b, w, d);
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
