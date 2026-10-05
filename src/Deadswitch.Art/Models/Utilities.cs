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
        }

        /// <summary>Horizontal tanks on cradles inside a sandbag berm; a tall tank and a pump stand as it grows.</summary>
        public static void FuelDepot(MeshBuilder b, Model m, int level, uint seed)
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
        }

        /// <summary>A squat concrete cooling stack with a fan deck; more fan units, then a second stack with a turning fan.</summary>
        public static void CoolingTower(MeshBuilder b, Model m, int level, uint seed)
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

            Shapes.Lamp(b, m, new Vector3(-0.8f, 4.5f, -0.6f), Mat.LampRed, Model.Red, 0.8f, 4f, LightRole.Status);
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

            Shapes.Lamp(b, m, new Vector3(1.2f, 2.6f, -1.6f), Mat.LampPhosphor, Model.Phosphor, 1.2f, 5f, LightRole.Status);
        }
    }
}
