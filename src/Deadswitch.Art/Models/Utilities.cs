using System;
using System.Numerics;
using Deadswitch.Art.Geometry;

namespace Deadswitch.Art.Models
{
    /// <summary>
    /// Support facilities (SPEC-038): Solar Field, Fuel Depot, Cooling Tower and Memory Restoration Chamber. Kit-built
    /// like the rest; each level adds equipment rather than scaling. Local space: plot centre, front toward -Z.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>Rows of salvaged panels on welded racks, tilted to the sun; an inverter and cable trays as it grows.</summary>
        public static void SolarField(MeshBuilder b, Model m, int level, uint seed)
        {
            SolarFieldCore(b, m, level, seed, 0);
        }

        /// <summary>
        /// Solar Field by visual stage 1-10 (SPEC-045 stage plan), preview only. Stages 1-2 are loose panels and a first timber row; stages 3-5 are the level 1-3
        /// models and 6-10 add the stage-only extras.
        /// </summary>
        public static void SolarFieldStage(MeshBuilder b, Model m, int stage, uint seed)
        {
            stage = Math.Min(10, Math.Max(1, stage));
            if (stage <= 2)
            {
                SolarFieldYard(b, m, stage, seed);
                return;
            }

            SolarFieldCore(b, m, Math.Min(3, stage - 2), seed, stage);
        }

        /// <param name="stage">SPEC-045 stage for the stage-only extras (6-10); 0 for the level models.</param>
        private static void SolarFieldCore(MeshBuilder b, Model m, int level, uint seed, int stage)
        {
            var rng = new ArtRandom(seed + 61);
            int rows = 1 + level;
            for (int r = 0; r < rows; r++)
            {
                float z = 2.2f - (r * (4.4f / Math.Max(1, rows - 1 + 1)) * 1.05f);
                for (int c = 0; c < 3; c++)
                {
                    float x = -1.95f + (c * 1.95f);
                    // rack: two posts, the back one taller
                    b.Strut(new Vector3(x - 0.7f, 0, z + 0.45f), new Vector3(x - 0.7f, 1.15f, z + 0.45f), 0.06f, Mat.DarkSteel);
                    b.Strut(new Vector3(x + 0.7f, 0, z + 0.45f), new Vector3(x + 0.7f, 1.15f, z + 0.45f), 0.06f, Mat.DarkSteel);
                    b.Strut(new Vector3(x - 0.7f, 0, z - 0.45f), new Vector3(x - 0.7f, 0.55f, z - 0.45f), 0.06f, Mat.DarkSteel);
                    b.Strut(new Vector3(x + 0.7f, 0, z - 0.45f), new Vector3(x + 0.7f, 0.55f, z - 0.45f), 0.06f, Mat.DarkSteel);
                    b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-32f)) * Matrix4x4.CreateTranslation(new Vector3(x, 0.9f, z)));
                    KitParts.SolarModule(b, 1.8f, 1.15f, rng.Next() < 0.3f);
                    b.Pop();
                }
            }

            KitParts.UtilityBox(b, new Vector3(2.8f, 0.6f, -2.6f));
            KitParts.Cable(b, new Vector3(2.8f, 0.9f, -2.4f), new Vector3(1.9f, 0.5f, -1.5f), 0.2f);
            if (level >= 2)
            {
                // inverter cabinet and a cable tray along the rows
                b.BoxOn(-2.9f, 0, -2.6f, 0.7f, 1.4f, 0.6f, Mat.SandSteel, 0.04f);
                b.Box(new Vector3(-2.9f, 1.0f, -2.91f), new Vector3(0.3f, 0.12f, 0.02f), Mat.Screen, 0f);
                b.BoxOn(0, 0, -2.9f, 5.2f, 0.12f, 0.3f, Mat.DarkSteel, 0.02f);
            }

            if (level >= 3)
            {
                KitModules.Barrels(b, new Vector3(2.9f, 0, 2.9f), 2, rng);
                Shapes.Lamp(b, m, new Vector3(-2.9f, 1.55f, -2.6f), Mat.LampAmber, Model.Amber, 1.0f, 4f, LightRole.Status);
            }

            if (stage >= 6)
            {
                // a dual-axis tracker on a pole at the back corner
                b.Frustum(new Vector3(2.7f, 0, 2.8f), 0.1f, 0.08f, 2.4f, 8, Mat.DarkSteel, 0.01f);
                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-40f)) * Matrix4x4.CreateTranslation(new Vector3(2.7f, 2.5f, 2.8f)));
                KitParts.SolarModule(b, 1.6f, 1.6f);
                b.Pop();
            }

            if (stage >= 7)
            {
                KitModules.Fence(b, new Vector3(-3.15f, 0, -3.15f), new Vector3(-0.6f, 0, -3.15f), 1.4f);
                KitModules.Fence(b, new Vector3(0.6f, 0, -3.15f), new Vector3(3.15f, 0, -3.15f), 1.4f);
            }

            if (stage >= 8)
            {
                // inverter house: a short container at the front left
                Props.Container(b, m, new Vector3(-2.2f, 0, -2.0f), 1.8f, 2.0f, 1.3f, Mat.SandSteel, true, seed + 63, "DC");
            }

            if (stage >= 9)
            {
                foreach (float x in new[] { -2.9f, -1.5f })
                {
                    KitParts.Plate(b, new Vector3(x, 0.9f, -2.75f), 1.3f, 1.6f, 6f, Mat.DarkSteel);
                }

                KitModules.Hedgehog(b, new Vector3(2.9f, 0, -2.5f), 20f);
            }

            if (stage >= 10)
            {
                // integrated: cyan output strips under each row and a shielded conduit
                for (int r = 0; r < 4; r++)
                {
                    float z = 2.2f - (r * (4.4f / 4f) * 1.05f) - 0.5f;
                    b.Box(new Vector3(0, 0.55f, z), new Vector3(5.6f, 0.03f, 0.03f), Mat.NeonCyan, 0f);
                }

                b.BoxOn(0, 0, -2.9f, 5.2f, 0.3f, 0.3f, Mat.DarkSteel, 0.02f);
                b.Box(new Vector3(0, 0.31f, -2.9f), new Vector3(5.0f, 0.02f, 0.08f), Mat.NeonCyan, 0f);
            }
        }

        /// <summary>Solar Field stages 1-2: three panels propped on bricks, then a first row on a timber frame.</summary>
        private static void SolarFieldYard(MeshBuilder b, Model m, int stage, uint seed)
        {
            var rng = new ArtRandom(seed + 61);
            for (int c = 0; c < 3; c++)
            {
                float x = -1.95f + (c * 1.95f);
                if (stage >= 2)
                {
                    b.Strut(new Vector3(x - 0.7f, 0, 0.45f), new Vector3(x - 0.7f, 0.9f, 0.45f), 0.06f, Mat.Wood);
                    b.Strut(new Vector3(x + 0.7f, 0, 0.45f), new Vector3(x + 0.7f, 0.9f, 0.45f), 0.06f, Mat.Wood);
                }
                else
                {
                    b.BoxOn(x, 0, 0.45f, 0.4f, 0.4f, 0.25f, Mat.ConcreteDark, 0.03f);
                }

                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(stage >= 2 ? -30f : -38f)) * Matrix4x4.CreateTranslation(new Vector3(x, stage >= 2 ? 0.65f : 0.4f, 0)));
                KitParts.SolarModule(b, 1.7f, 1.1f, rng.Next() < 0.5f);
                b.Pop();
            }

            // car batteries on a crate, wired to the panels
            b.BoxOn(1.8f, 0, -2.0f, 0.9f, 0.5f, 0.6f, Mat.Wood, 0.03f);
            for (int i = 0; i < (stage >= 2 ? 3 : 1); i++)
            {
                b.BoxOn(1.5f + (i * 0.3f), 0.5f, -2.0f, 0.24f, 0.2f, 0.16f, Mat.Rubber, 0.01f);
            }

            KitParts.Cable(b, new Vector3(1.8f, 0.6f, -1.8f), new Vector3(1.4f, 0.3f, -0.3f), 0.1f, 0.025f);
            Props.Lamp(b, m, new Vector3(1.8f, 1.2f, -2.0f), false, 0.6f, LightRole.Status);
        }

        /// <summary>Horizontal tanks on cradles inside a sandbag berm; a tall tank and a pump stand as it grows.</summary>
        public static void FuelDepot(MeshBuilder b, Model m, int level, uint seed)
        {
            FuelDepotCore(b, m, level, seed, 0);
        }

        /// <summary>
        /// Fuel Depot by visual stage 1-10 (SPEC-045 stage plan), preview only. Stages 1-2 are drums on pallets and a hand pump; stages 3-5 are the level 1-3
        /// models and 6-10 add the stage-only extras.
        /// </summary>
        public static void FuelDepotStage(MeshBuilder b, Model m, int stage, uint seed)
        {
            stage = Math.Min(10, Math.Max(1, stage));
            if (stage <= 2)
            {
                FuelDepotYard(b, m, stage, seed);
                return;
            }

            FuelDepotCore(b, m, Math.Min(3, stage - 2), seed, stage);
        }

        /// <param name="stage">SPEC-045 stage for the stage-only extras (6-10); 0 for the level models.</param>
        private static void FuelDepotCore(MeshBuilder b, Model m, int level, uint seed, int stage)
        {
            var rng = new ArtRandom(seed + 71);
            b.BoxOn(0, 0, 0, 5.8f, 0.12f, 5.6f, Mat.ConcreteDark, 0.04f);
            Shapes.SandbagWall(b, new Vector3(-2.9f, 0, -2.8f), new Vector3(-0.9f, 0, -2.8f), 2);
            Shapes.SandbagWall(b, new Vector3(0.9f, 0, -2.8f), new Vector3(2.9f, 0, -2.8f), 2);
            int tanks = Math.Min(3, level + 1);
            for (int i = 0; i < tanks; i++)
            {
                // tanks run front to back, side by side, each on two concrete cradles
                float x = -1.8f + (i * 1.8f);
                foreach (float z in new[] { -0.9f, 0.9f })
                {
                    b.BoxOn(x, 0.12f, z, 1.0f, 0.5f, 0.25f, Mat.Concrete, 0.04f);
                }

                b.Push(new Vector3(x, 1.25f, 0.1f), 90f);
                KitParts.TankH(b, Vector3.Zero, 0.72f, 3.2f, i % 2 == 0 ? Mat.OliveSteel : Mat.SandSteel);
                b.Pop();
                b.Strut(new Vector3(x, 1.95f, 1.2f), new Vector3(x, 2.4f, 1.2f), 0.08f, Mat.DarkSteel);

                // a level gauge on a post before each tank: amber segments light as the fuel store fills (SPEC-039 idea 18)
                b.Strut(new Vector3(x + 0.5f, 0.12f, -1.75f), new Vector3(x + 0.5f, 1.6f, -1.75f), 0.04f, Mat.DarkSteel);
                b.Box(new Vector3(x + 0.5f, 1.05f, -1.8f), new Vector3(0.22f, 0.9f, 0.04f), Mat.DarkSteel, 0.01f);
                for (int j = 0; j < 4; j++)
                {
                    var seg = new MeshBuilder(seed + (uint)((i * 4) + j));
                    seg.Box(Vector3.Zero, new Vector3(0.14f, 0.16f, 0.02f), Mat.LampAmber, 0f);
                    m.Parts.Add(new AnimPart(seg.Mesh, new Vector3(x + 0.5f, 0.72f + (j * 0.22f), -1.83f), AnimKind.Gauge, 0f, (j + 0.5f) / 4f));
                }
            }

            KitParts.Pipe(b, new[] { new Vector3(-1.8f, 0.5f, 1.8f), new Vector3(1.8f, 0.5f, 1.8f), new Vector3(2.6f, 0.5f, 1.8f), new Vector3(2.6f, 0.5f, -2.0f) }, 0.08f, Mat.Rust);
            KitParts.UtilityBox(b, new Vector3(2.6f, 0.7f, -2.2f), Mat.PaintRed);
            Shapes.Hazard(b, -0.9f, 0.9f, 0.15f, 0.55f, -2.95f, 6);
            if (level >= 2)
            {
                KitParts.TankV(b, new Vector3(-2.4f, 0.12f, 2.3f), 0.8f, 3.2f, Mat.Rust, seed + 3);
                KitParts.Ladder(b, new Vector3(-1.55f, 0.12f, 2.3f), 3.1f, 90f);
            }

            if (level >= 3)
            {
                KitModules.Barrels(b, new Vector3(2.4f, 0.12f, 2.4f), 4, rng);
                Shapes.Lamp(b, m, new Vector3(2.6f, 1.6f, -2.2f), Mat.LampRed, Model.Red, 1.0f, 5f, LightRole.Status);
            }

            if (stage >= 6)
            {
                // a pump house by the front right corner
                KitModules.Shed(b, new Vector3(2.2f, 0.12f, -1.2f), 1.5f, 1.2f, 2.0f, 1.8f, Mat.Rust, false);
                b.BoxOn(2.2f, 0.12f, -1.2f, 0.8f, 0.7f, 0.6f, Mat.OliveSteel, 0.03f);
            }

            if (stage >= 7)
            {
                // a loading arm swung out over the front
                b.Strut(new Vector3(-2.7f, 0.12f, -2.2f), new Vector3(-2.7f, 2.6f, -2.2f), 0.1f, Mat.DarkSteel);
                KitParts.Pipe(b, new[] { new Vector3(-2.7f, 2.6f, -2.2f), new Vector3(-1.5f, 2.6f, -2.6f), new Vector3(-1.5f, 1.6f, -2.6f) }, 0.07f, Mat.PaintYellow);
            }

            if (stage >= 8)
            {
                // fire suppression: a red foam tank and a hydrant
                KitParts.TankV(b, new Vector3(2.5f, 0.12f, 0.9f), 0.35f, 1.6f, Mat.PaintRed, seed + 73);
                b.Frustum(new Vector3(-0.2f, 0.12f, -2.3f), 0.1f, 0.09f, 0.7f, 10, Mat.PaintRed, 0.01f);
            }

            if (stage >= 9)
            {
                KitModules.RetainingWall(b, new Vector3(0, 0, 2.95f), 5.6f, 1.6f, new ArtRandom(seed + 75));
            }

            if (stage >= 10)
            {
                // integrated: cyan level gauges on each tank end and a shielded conduit
                for (int i = 0; i < 3; i++)
                {
                    b.Box(new Vector3(-1.8f + (i * 1.8f), 1.25f, -1.52f), new Vector3(0.05f, 0.9f, 0.03f), Mat.NeonCyan, 0f);
                }

                b.BoxOn(-2.95f, 0, 0.2f, 0.4f, 0.3f, 4.6f, Mat.DarkSteel, 0.02f);
                b.Box(new Vector3(-2.95f, 0.31f, 0.2f), new Vector3(0.08f, 0.02f, 4.4f), Mat.NeonCyan, 0f);
            }
        }

        /// <summary>Fuel Depot stages 1-2: drums on pallets, then a hand pump, jerrycans and a first small tank.</summary>
        private static void FuelDepotYard(MeshBuilder b, Model m, int stage, uint seed)
        {
            var rng = new ArtRandom(seed + 71);
            KitModules.Pallet(b, new Vector3(-1.2f, 0, 0.6f), 0f, 1);
            KitModules.Barrels(b, new Vector3(-1.2f, 0.14f, 0.6f), 4, rng);
            KitModules.Pallet(b, new Vector3(0.6f, 0, 1.6f), 8f, 1);
            KitModules.Barrels(b, new Vector3(0.6f, 0.14f, 1.6f), 3, rng);
            Props.Lamp(b, m, new Vector3(1.6f, 1.3f, -0.8f), false, 0.6f, LightRole.Status);
            if (stage >= 2)
            {
                b.Strut(new Vector3(-0.1f, 0, -0.4f), new Vector3(-0.1f, 1.0f, -0.4f), 0.05f, Mat.DarkSteel);
                b.Strut(new Vector3(-0.1f, 1.0f, -0.4f), new Vector3(-0.4f, 1.05f, -0.4f), 0.03f, Mat.DarkSteel);
                for (int i = 0; i < 4; i++)
                {
                    b.BoxOn(1.5f + (i * 0.3f), 0, -2.0f, 0.22f, 0.45f, 0.14f, Mat.PaintRed, 0.02f);
                }

                foreach (float z in new[] { -0.6f, 0.6f })
                {
                    b.BoxOn(2.2f, 0, z + 0.4f, 0.8f, 0.4f, 0.22f, Mat.Concrete, 0.03f);
                }

                b.Push(new Vector3(2.2f, 0.9f, 0.4f), 90f);
                KitParts.TankH(b, Vector3.Zero, 0.5f, 2.0f, Mat.Rust);
                b.Pop();
            }
        }

        /// <summary>A squat concrete cooling stack with a fan deck; more fan units, then a second stack with a turning fan.</summary>
        public static void CoolingTower(MeshBuilder b, Model m, int level, uint seed)
        {
            CoolingTowerCore(b, m, level, seed, 0);
        }

        /// <summary>
        /// Cooling Tower by visual stage 1-10 (SPEC-045 stage plan), preview only. Stages 1-2 are a fan box and a coil frame; stages 3-5 are the level 1-3
        /// models and 6-10 add the stage-only extras.
        /// </summary>
        public static void CoolingTowerStage(MeshBuilder b, Model m, int stage, uint seed)
        {
            stage = Math.Min(10, Math.Max(1, stage));
            if (stage <= 2)
            {
                CoolingTowerYard(b, m, stage, seed);
                return;
            }

            CoolingTowerCore(b, m, Math.Min(3, stage - 2), seed, stage);
        }

        /// <param name="stage">SPEC-045 stage for the stage-only extras (6-10); 0 for the level models.</param>
        private static void CoolingTowerCore(MeshBuilder b, Model m, int level, uint seed, int stage)
        {
            b.BoxOn(0, 0, 0, 5.6f, 0.15f, 5.6f, Mat.ConcreteDark, 0.04f);
            Stack(b, new Vector3(-0.8f, 0.15f, 0.6f), 1.8f, 4.2f);
            for (int i = 0; i < 2 + level; i++)
            {
                KitParts.AcUnit(b, new Vector3(1.8f, 0.15f, -2.2f + (i * 1.1f)));
            }

            KitParts.Pipe(b, new[] { new Vector3(1.8f, 1.2f, -2.4f), new Vector3(0.4f, 1.2f, -2.4f), new Vector3(0.4f, 1.2f, -0.4f) }, 0.18f, Mat.Rust);
            if (level >= 3)
            {
                // a second stack and a big exhaust fan turning on its deck
                Stack(b, new Vector3(1.6f, 0.15f, 1.9f), 1.0f, 2.6f);
                var fan = new MeshBuilder(seed + 81) { GroundOffset = 4.5f };
                for (int k = 0; k < 4; k++)
                {
                    fan.Push(Vector3.Zero, k * 45f);
                    fan.Box(Vector3.Zero, new Vector3(2.2f, 0.04f, 0.28f), Mat.DarkSteel, 0.01f);
                    fan.Pop();
                }

                fan.Frustum(new Vector3(0, -0.1f, 0), 0.22f, 0.22f, 0.2f, 12, Mat.DarkSteel, 0.02f);
                m.Parts.Add(new AnimPart(fan.Mesh, new Vector3(-0.8f, 4.45f, 0.6f), AnimKind.SpinY, 90f));
            }

            if (stage >= 6)
            {
                // a cold-water basin with two pumps
                b.BoxOn(-1.8f, 0.15f, -2.1f, 2.2f, 0.4f, 1.3f, Mat.Concrete, 0.03f);
                b.BoxOn(-1.8f, 0.5f, -2.1f, 2.0f, 0.02f, 1.1f, Mat.Water, 0f);
                foreach (float x in new[] { -2.4f, -1.2f })
                {
                    b.BoxOn(x, 0.55f, -2.1f, 0.4f, 0.35f, 0.4f, Mat.OliveSteel, 0.03f);
                }
            }

            if (stage >= 7)
            {
                // pipe bridge toward the server racks
                KitParts.Truss(b, new Vector3(-2.9f, 3.0f, 2.6f), new Vector3(1.6f, 3.0f, 2.6f), 0.35f, 0.04f, Mat.DarkSteel);
                KitParts.Pipe(b, new[] { new Vector3(-2.9f, 3.2f, 2.6f), new Vector3(1.6f, 3.2f, 2.6f) }, 0.12f, Mat.Rust);
                foreach (float x in new[] { -2.9f, 1.6f })
                {
                    b.Strut(new Vector3(x, 0.15f, 2.6f), new Vector3(x, 3.0f, 2.6f), 0.1f, Mat.DarkSteel);
                }
            }

            if (stage >= 8)
            {
                // louvered armour around the stack skirt
                for (int i = 0; i < 6; i++)
                {
                    b.Push(new Vector3(-0.8f, 0.15f, 0.6f), (i * 60f) + 30f);
                    KitParts.Plate(b, new Vector3(0, 0.6f, -2.05f), 1.6f, 1.1f, 10f, Mat.DarkSteel);
                    b.Pop();
                }
            }

            if (stage >= 9)
            {
                Shapes.SandbagWall(b, new Vector3(-2.9f, 0, -2.95f), new Vector3(0.6f, 0, -2.95f), 2);
                Shapes.Lamp(b, m, new Vector3(1.6f, 4.9f, 1.9f), Mat.LampRed, Model.Red, 0.6f, 3f, LightRole.Status);
            }

            if (stage >= 10)
            {
                // integrated: cyan plume ring on the stack lip and a shielded conduit
                b.Frustum(new Vector3(-0.8f, 0.15f + 0.5f + 4.2f - 0.14f, 0.6f), 1.58f, 1.58f, 0.05f, 20, Mat.NeonCyan, 0f, false);
                b.BoxOn(2.9f, 0, 0.0f, 0.4f, 0.3f, 4.6f, Mat.DarkSteel, 0.02f);
                b.Box(new Vector3(2.9f, 0.31f, 0.0f), new Vector3(0.08f, 0.02f, 4.4f), Mat.NeonCyan, 0f);
            }

            Shapes.Lamp(b, m, new Vector3(-0.8f, 4.5f, -0.6f), Mat.LampRed, Model.Red, 0.8f, 4f, LightRole.Status);
        }

        /// <summary>Cooling Tower stages 1-2: a fan box over a water barrel, then a cooling coil on a frame.</summary>
        private static void CoolingTowerYard(MeshBuilder b, Model m, int stage, uint seed)
        {
            KitParts.AcUnit(b, new Vector3(-0.6f, 0, 0.2f));
            KitParts.Barrel(b, new Vector3(0.6f, 0, 0.3f), Mat.TarpBlue);
            KitParts.Cable(b, new Vector3(-0.6f, 0.5f, -0.2f), new Vector3(-0.6f, 0.05f, -2.6f), 0.05f, 0.03f);
            Props.Lamp(b, m, new Vector3(0.0f, 1.3f, -0.6f), false, 0.6f, LightRole.Status);
            if (stage >= 2)
            {
                // a radiator coil on a welded frame, piped to the barrel
                foreach (int side in new[] { -1, 1 })
                {
                    b.Strut(new Vector3(1.8f + (side * 0.8f), 0, 1.8f), new Vector3(1.8f + (side * 0.8f), 2.0f, 1.8f), 0.06f, Mat.Rust);
                }

                b.BoxOn(1.8f, 0.6f, 1.8f, 1.5f, 1.3f, 0.25f, Mat.Copper, 0.02f);
                for (int i = 0; i < 6; i++)
                {
                    b.Box(new Vector3(1.8f, 0.7f + (i * 0.2f), 1.66f), new Vector3(1.4f, 0.04f, 0.03f), Mat.DarkSteel, 0f);
                }

                KitParts.Pipe(b, new[] { new Vector3(1.1f, 0.8f, 1.8f), new Vector3(0.6f, 0.8f, 1.8f), new Vector3(0.6f, 0.8f, 0.6f) }, 0.05f, Mat.Rubber);
                KitModules.CrateStack(b, new Vector3(-2.4f, 0, 2.2f), new ArtRandom(seed + 81));
            }
        }

        /// <summary>A waisted cooling stack: wide skirt on legs, narrowing throat, flared lip.</summary>
        private static void Stack(MeshBuilder b, Vector3 at, float radius, float height)
        {
            for (int k = 0; k < 8; k++)
            {
                float a = MeshBuilder.Deg(k * 45f);
                b.Strut(at + new Vector3((float)Math.Cos(a) * radius, 0, (float)Math.Sin(a) * radius), at + new Vector3((float)Math.Cos(a) * radius * 0.95f, 0.5f, (float)Math.Sin(a) * radius * 0.95f), 0.08f, Mat.Concrete);
            }

            b.Frustum(at + new Vector3(0, 0.5f, 0), radius, radius * 0.72f, height * 0.55f, 20, Mat.Concrete, 0.04f, false);
            b.Frustum(at + new Vector3(0, 0.5f + (height * 0.55f), 0), radius * 0.72f, radius * 0.82f, height * 0.45f, 20, Mat.Concrete, 0.04f, false);
            b.Frustum(at + new Vector3(0, 0.5f + height - 0.12f, 0), radius * 0.86f, radius * 0.86f, 0.14f, 20, Mat.ConcreteDark, 0.02f, false);
        }

        /// <summary>An armoured vault where the core rebuilds itself: blast door with a phosphor seam, cable looms, cold cylinders.</summary>
        public static void MemoryChamber(MeshBuilder b, Model m, int level, uint seed)
        {
            MemoryChamberCore(b, m, level, seed, 0);
        }

        /// <summary>
        /// Memory Chamber by visual stage 1-10 (SPEC-045 stage plan), preview only. Stages 1-2 are a terminal on a crate and a shielded drive cabinet; stages 3-5 are the level 1-3
        /// models and 6-10 add the stage-only extras.
        /// </summary>
        public static void MemoryChamberStage(MeshBuilder b, Model m, int stage, uint seed)
        {
            stage = Math.Min(10, Math.Max(1, stage));
            if (stage <= 2)
            {
                MemoryChamberYard(b, m, stage, seed);
                return;
            }

            MemoryChamberCore(b, m, Math.Min(3, stage - 2), seed, stage);
        }

        /// <param name="stage">SPEC-045 stage for the stage-only extras (6-10); 0 for the level models.</param>
        private static void MemoryChamberCore(MeshBuilder b, Model m, int level, uint seed, int stage)
        {
            b.BoxOn(0, 0, 0.6f, 4.8f, 3.0f, 3.8f, Mat.ConcreteDark, 0.15f);
            b.BoxOn(0, 3.0f, 0.6f, 3.6f, 0.4f, 2.8f, Mat.Concrete, 0.08f);
            b.BoxOn(0, 0, -1.4f, 2.0f, 2.4f, 0.3f, Mat.DarkSteel, 0.04f);
            b.Box(new Vector3(0, 1.2f, -1.57f), new Vector3(0.06f, 2.2f, 0.02f), Mat.Portal, 0f);
            Shapes.Hazard(b, -1.0f, 1.0f, 0.0f, 0.3f, -1.57f, 6);
            foreach (int s in new[] { -1, 1 })
            {
                KitParts.Pipe(b, new[] { new Vector3(s * 2.45f, 2.4f, -0.8f), new Vector3(s * 2.45f, 0.4f, -0.8f), new Vector3(s * 2.9f, 0.2f, -1.6f) }, 0.12f, Mat.Rubber);
            }

            b.Box(new Vector3(-1.6f, 2.0f, -1.27f), new Vector3(0.6f, 0.3f, 0.03f), Mat.Screen, 0f);
            if (level >= 2)
            {
                // cold cylinders beside the vault, frosted glass and steel bands
                foreach (float z in new[] { -0.3f, 1.4f })
                {
                    KitParts.TankV(b, new Vector3(2.9f, 0, z), 0.42f, 2.2f, Mat.Glass, seed + 5);
                }
            }

            if (level >= 3)
            {
                KitParts.Mast(b, m, new Vector3(-1.2f, 3.4f, 1.4f), 3.0f, true);
                KitParts.Spotlight(b, m, new Vector3(1.6f, 3.4f, -0.6f), 180f);
            }

            if (stage >= 6)
            {
                // cable crown: looms rising from the roof to a ring frame
                for (int i = 0; i < 6; i++)
                {
                    float a = MeshBuilder.Deg(i * 60f);
                    var foot = new Vector3((float)Math.Cos(a) * 1.4f, 3.4f, 0.6f + ((float)Math.Sin(a) * 1.0f));
                    KitParts.Cable(b, foot, new Vector3(foot.X * 0.4f, 4.6f, 0.6f + ((foot.Z - 0.6f) * 0.4f)), 0.1f, 0.06f);
                }

                b.Frustum(new Vector3(0, 4.55f, 0.6f), 0.62f, 0.62f, 0.1f, 16, Mat.DarkSteel, 0.01f, false);
            }

            if (stage >= 7)
            {
                // a second restoration pod beside the vault
                b.BoxOn(-2.75f, 0, 1.6f, 1.0f, 2.2f, 1.6f, Mat.ConcreteDark, 0.1f);
                b.Box(new Vector3(-2.24f, 1.1f, 1.6f), new Vector3(0.02f, 1.8f, 0.05f), Mat.Portal, 0f);
            }

            if (stage >= 8)
            {
                // Faraday cage over the roof
                foreach (float x in new[] { -1.8f, 0f, 1.8f })
                {
                    b.Strut(new Vector3(x, 3.4f, -0.8f), new Vector3(x, 4.2f, 0.6f), 0.03f, Mat.Copper);
                    b.Strut(new Vector3(x, 4.2f, 0.6f), new Vector3(x, 3.4f, 2.0f), 0.03f, Mat.Copper);
                }

                b.Strut(new Vector3(-1.8f, 4.2f, 0.6f), new Vector3(1.8f, 4.2f, 0.6f), 0.03f, Mat.Copper);
            }

            if (stage >= 9)
            {
                foreach (int side in new[] { -1, 1 })
                {
                    KitParts.Plate(b, new Vector3(side * 1.5f, 1.3f, -1.75f), 1.0f, 2.4f, 4f, Mat.DarkSteel);
                }

                Shapes.SandbagWall(b, new Vector3(-2.9f, 0, -2.9f), new Vector3(-1.2f, 0, -2.9f), 2);
                Shapes.SandbagWall(b, new Vector3(1.2f, 0, -2.9f), new Vector3(2.9f, 0, -2.9f), 2);
            }

            if (stage >= 10)
            {
                // integrated: memory-violet data rings on the crown and cyan status line over the door
                foreach (float y in new[] { 4.7f, 5.0f })
                {
                    b.Frustum(new Vector3(0, y, 0.6f), 0.5f, 0.5f, 0.04f, 16, Mat.Portal, 0f, false);
                }

                b.Box(new Vector3(0, 2.45f, -1.57f), new Vector3(1.9f, 0.04f, 0.03f), Mat.NeonCyan, 0f);
            }

            Shapes.Lamp(b, m, new Vector3(1.2f, 2.6f, -1.6f), Mat.LampPhosphor, Model.Phosphor, 1.2f, 5f, LightRole.Status);
        }

        /// <summary>Memory Chamber stages 1-2: a terminal on a crate wired to drives, then a shielded drive cabinet.</summary>
        private static void MemoryChamberYard(MeshBuilder b, Model m, int stage, uint seed)
        {
            var rng = new ArtRandom(seed + 91);
            b.BoxOn(-0.6f, 0, 0.2f, 1.0f, 0.8f, 0.7f, Mat.Wood, 0.04f);
            b.BoxOn(-0.6f, 0.8f, 0.3f, 0.6f, 0.45f, 0.4f, Mat.DarkSteel, 0.03f);
            b.Box(new Vector3(-0.6f, 1.05f, 0.09f), new Vector3(0.45f, 0.3f, 0.02f), Mat.Screen, 0f);
            for (int i = 0; i < 3; i++)
            {
                b.BoxOn(0.6f, i * 0.22f, 0.2f, 0.5f, 0.2f, 0.4f, Mat.DarkSteel, 0.02f);
            }

            KitParts.Cable(b, new Vector3(0.4f, 0.4f, 0.2f), new Vector3(-0.3f, 0.6f, 0.2f), 0.1f, 0.03f);
            KitModules.CrateStack(b, new Vector3(-2.4f, 0, 2.2f), rng);
            Shapes.Lamp(b, m, new Vector3(-0.6f, 1.5f, -0.2f), Mat.LampPhosphor, Model.Phosphor, 0.6f, 3f, LightRole.Status, 0.06f);
            if (stage >= 2)
            {
                // a shielded steel cabinet with a violet seam, cabled to a small genset
                b.BoxOn(1.9f, 0, 1.6f, 1.2f, 2.0f, 0.9f, Mat.DarkSteel, 0.04f);
                b.Box(new Vector3(1.9f, 1.0f, 1.14f), new Vector3(0.04f, 1.7f, 0.02f), Mat.Portal, 0f);
                KitParts.Genset(b, new Vector3(-1.4f, 0, -2.0f), 0f);
                KitParts.Cable(b, new Vector3(-0.6f, 0.9f, -1.8f), new Vector3(1.4f, 0.4f, 1.2f), 0.2f, 0.04f);
            }
        }
    }
}
