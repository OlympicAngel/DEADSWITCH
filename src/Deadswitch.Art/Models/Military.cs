using System;
using System.Numerics;
using Deadswitch.Art.Geometry;

namespace Deadswitch.Art.Models
{
    /// <summary>
    /// Unit facilities (SPEC-035): the Drone Bay and the Motor Pool, built from the kit like the other facilities.
    /// Each level adds machines and gear (never just scale). Local space: plot centre on the pad, front toward -Z.
    /// </summary>
    internal static class Military
    {
        /// <summary>Launch pad with landing marks, an open hangar for the racks, drones parked by level, a control mast.</summary>
        public static void DroneBay(MeshBuilder b, Model m, int level, uint seed)
        {
            var rng = new ArtRandom(seed + 41);

            // launch pad: a cracked concrete slab with a painted landing ring and hazard edges
            b.BoxOn(0, 0, -0.6f, 5.4f, 0.16f, 4.2f, Mat.ConcreteDark, 0.04f);
            for (int i = 0; i < 12; i++)
            {
                if (i % 4 == 3)
                {
                    continue;
                }

                float a = MeshBuilder.Deg(i * 30f);
                b.Push(new Vector3((float)Math.Cos(a) * 1.25f, 0.165f, -0.6f + ((float)Math.Sin(a) * 1.25f)), -i * 30f);
                b.Box(Vector3.Zero, new Vector3(0.12f, 0.01f, 0.55f), Mat.PaintWhite, 0f);
                b.Pop();
            }

            b.Box(new Vector3(0, 0.165f, -0.6f), new Vector3(0.9f, 0.01f, 0.12f), Mat.PaintWhite, 0f);
            b.Box(new Vector3(0, 0.165f, -0.6f), new Vector3(0.12f, 0.01f, 0.9f), Mat.PaintWhite, 0f);
            Shapes.Hazard(b, -2.6f, 2.6f, 0.0f, 0.16f, -2.71f, 10);

            // the hangar at the back: open front, racks of spare frames and batteries inside
            KitModules.Shed(b, new Vector3(0, 0, 2.2f), 5.2f, 2.2f, 2.8f, 2.4f, Mat.Rust, true);
            for (int i = 0; i < 3; i++)
            {
                float x = -1.7f + (i * 1.7f);
                b.BoxOn(x, 0, 2.5f, 1.2f, 1.6f, 0.5f, Mat.DarkSteel, 0.02f);
                b.Box(new Vector3(x, 1.0f, 2.24f), new Vector3(1.0f, 0.08f, 0.02f), Mat.Screen, 0f);
            }

            KitParts.Cable(b, new Vector3(-2.4f, 2.4f, 1.2f), new Vector3(1.2f, 0.2f, -0.4f), 0.4f);
            KitModules.Barrels(b, new Vector3(2.4f, 0, 1.0f), 2, rng);

            // parked drones on the pad: more as the bay grows
            int parked = Math.Min(4, level);
            for (int i = 0; i < parked; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * (i < 2 ? 1.5f : 0.6f);
                float z = i < 2 ? -1.7f : 0.3f;
                Drone(b, new Vector3(x, 0.17f, z), rng.Range(0f, 90f), rng);
            }

            if (level >= 2)
            {
                // operator console and the control mast with its dish
                KitModules.Workbench(b, m, new Vector3(-2.3f, 0, -2.0f), 90f);
                KitParts.Mast(b, m, new Vector3(2.5f, 0, 2.9f), 5.2f, true);
                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-40f)) * Matrix4x4.CreateTranslation(new Vector3(2.5f, 4.4f, 2.6f)));
                b.Frustum(Vector3.Zero, 0.08f, 0.6f, 0.25f, 14, Mat.SandSteel, 0.02f);
                b.Pop();
            }

            if (level >= 3)
            {
                // one machine in the air over the pad, rotors turning
                var hover = new MeshBuilder(seed + 43) { GroundOffset = 2.6f };
                Drone(hover, Vector3.Zero, 20f, rng, false);
                m.Parts.Add(new AnimPart(hover.Mesh, new Vector3(0, 2.6f, -0.6f), AnimKind.SweepY, 0.15f, 25f));
            }

            if (level >= 4)
            {
                Shapes.SandbagWall(b, new Vector3(-2.8f, 0, -3.0f), new Vector3(-1.2f, 0, -3.0f), 2);
                Shapes.SandbagWall(b, new Vector3(1.2f, 0, -3.0f), new Vector3(2.8f, 0, -3.0f), 2);
            }

            Shapes.Lamp(b, m, new Vector3(-2.5f, 2.6f, 1.2f), Mat.LampAmber, Model.Amber, 1.2f, 5f, LightRole.Status);
        }

        /// <summary>A quadcopter about a metre across: hull, four arms with motor pods and rotor discs, a sensor pod.</summary>
        private static void Drone(MeshBuilder b, Vector3 at, float yaw, ArtRandom rng, bool landed = true)
        {
            b.Push(at, yaw);
            if (landed)
            {
                foreach (int s in new[] { -1, 1 })
                {
                    b.Box(new Vector3(s * 0.22f, 0.07f, 0), new Vector3(0.03f, 0.03f, 0.5f), Mat.DarkSteel, 0.005f);
                }
            }

            float y = landed ? 0.2f : 0f;
            b.Box(new Vector3(0, y, 0), new Vector3(0.36f, 0.14f, 0.5f), rng.Next() < 0.5f ? Mat.OliveSteel : Mat.SandSteel, 0.03f);
            b.Box(new Vector3(0, y - 0.1f, -0.2f), new Vector3(0.12f, 0.1f, 0.12f), Mat.DarkSteel, 0.02f);
            b.Box(new Vector3(0, y - 0.1f, -0.27f), new Vector3(0.06f, 0.05f, 0.02f), Mat.LampRed, 0f);
            for (int k = 0; k < 4; k++)
            {
                float a = MeshBuilder.Deg(45f + (k * 90f));
                var tip = new Vector3((float)Math.Cos(a) * 0.5f, y + 0.04f, (float)Math.Sin(a) * 0.5f);
                b.Strut(new Vector3(0, y, 0), tip, 0.035f, Mat.DarkSteel);
                b.Frustum(tip - new Vector3(0, 0.05f, 0), 0.05f, 0.045f, 0.09f, 10, Mat.DarkSteel, 0.01f);
                // two thin blades and a faint guard ring, not a solid disc
                b.Push(tip + new Vector3(0, 0.07f, 0), k * 37f);
                b.Box(Vector3.Zero, new Vector3(0.46f, 0.012f, 0.045f), Mat.Rubber, 0f);
                b.Pop();
                b.Frustum(tip + new Vector3(0, 0.05f, 0), 0.26f, 0.26f, 0.03f, 16, Mat.DarkSteel, 0f, false);
            }

            b.Pop();
        }

        /// <summary>Open garage with an armoured carrier; gun trucks, fuel and a crane gantry as it grows.</summary>
        public static void MotorPool(MeshBuilder b, Model m, int level, uint seed)
        {
            var rng = new ArtRandom(seed + 51);

            // oil-stained apron and the garage: a deep shed over the back of the plot
            b.BoxOn(0, 0, -0.4f, 5.8f, 0.12f, 5.0f, Mat.ConcreteDark, 0.04f);
            KitModules.Shed(b, new Vector3(0, 0, 1.0f), 5.6f, 3.6f, 3.3f, 2.9f, Mat.Rust, true);
            KitModules.RetainingWall(b, new Vector3(0, 0, 2.95f), 5.6f, 1.2f, rng);

            Carrier(b, new Vector3(-0.9f, 0.12f, 0.6f), 0f);
            KitModules.Barrels(b, new Vector3(2.4f, 0.12f, 2.4f), 3, rng);
            KitModules.CrateStack(b, new Vector3(-2.4f, 0.12f, 2.3f), rng);

            if (level >= 2)
            {
                // a gun truck on the apron and a fuel bowser by the wall
                KitModules.Pickup(b, new Vector3(1.75f, 0.12f, -1.6f), 0f, Mat.SandSteel, false);
                b.Strut(new Vector3(1.75f, 1.2f, -0.6f), new Vector3(1.75f, 1.9f, -0.6f), 0.08f, Mat.DarkSteel);
                b.CylinderZ(new Vector3(1.75f, 1.95f, -1.1f), 0.05f, 1.1f, 8, Mat.DarkSteel);
                KitParts.TankH(b, new Vector3(2.35f, 0.75f, 1.6f), 0.5f, 1.6f, Mat.OliveSteel);
            }

            if (level >= 3)
            {
                // crane gantry over the carrier, an engine block hanging on the chain
                KitParts.Truss(b, new Vector3(-2.4f, 3.0f, 0.6f), new Vector3(0.6f, 3.0f, 0.6f), 0.4f, 0.05f, Mat.DarkSteel);
                foreach (float x in new[] { -2.4f, 0.6f })
                {
                    KitParts.IBeam(b, new Vector3(x, 0.12f, 0.6f), new Vector3(x, 3.0f, 0.6f), 0.18f, 0.16f, Mat.DarkSteel);
                }

                KitParts.Cable(b, new Vector3(-0.9f, 3.0f, -0.9f), new Vector3(-0.9f, 2.4f, -0.9f), 0.02f, 0.04f, Mat.DarkSteel);
                b.Box(new Vector3(-0.9f, 2.2f, -0.9f), new Vector3(0.7f, 0.45f, 0.5f), Mat.DarkSteel, 0.04f);
            }

            Shapes.Lamp(b, m, new Vector3(0, 3.0f, -0.8f), Mat.LampAmber, Model.Amber, 1.4f, 6f, LightRole.Status);
        }

        /// <summary>An eight-wheeled armoured carrier: sloped hull, wheel wells, a ring-mounted gun, stowage.</summary>
        private static void Carrier(MeshBuilder b, Vector3 at, float yaw)
        {
            b.Push(at, yaw);
            foreach (int s in new[] { -1, 1 })
            {
                for (int k = 0; k < 4; k++)
                {
                    b.CylinderX(new Vector3(s * 0.95f, 0.45f, -1.35f + (k * 0.9f)), 0.45f, 0.35f, 14, Mat.Rubber);
                }
            }

            b.BoxOn(0, 0.45f, 0, 1.9f, 0.9f, 4.2f, Mat.OliveSteel, 0.08f);
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-30f)) * Matrix4x4.CreateTranslation(new Vector3(0, 1.25f, -2.25f)));
            b.Box(Vector3.Zero, new Vector3(1.85f, 0.08f, 0.9f), Mat.OliveSteel, 0.03f);
            b.Pop();
            b.BoxOn(0, 1.35f, 0.4f, 1.7f, 0.35f, 2.6f, Mat.OliveSteel, 0.06f);
            b.Frustum(new Vector3(0, 1.7f, 0.1f), 0.5f, 0.45f, 0.25f, 14, Mat.DarkSteel, 0.03f);
            b.CylinderZ(new Vector3(0, 2.05f, -0.6f), 0.05f, 1.3f, 8, Mat.DarkSteel);
            b.BoxOn(0, 1.95f, 0.1f, 0.3f, 0.2f, 0.5f, Mat.DarkSteel, 0.02f);
            b.BoxOn(-0.6f, 1.7f, 1.3f, 0.5f, 0.3f, 0.7f, Mat.Tarp, 0.05f);
            b.Pop();
        }
    }
}
