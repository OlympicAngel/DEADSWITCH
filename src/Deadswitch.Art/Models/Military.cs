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
            DroneBayCore(b, m, seed, Math.Min(4, level), level >= 2, level >= 3, level >= 4, 0);
        }

        /// <summary>
        /// Drone Bay by visual stage 1-10 (SPEC-045 stage plan), preview only. Stages 1-2 are a workbench and a painted
        /// pad in the open; 3-6 are the level models; then a launch rail, blast plates, a second flyer and cyan pad lights.
        /// </summary>
        public static void DroneBayStage(MeshBuilder b, Model m, int stage, uint seed)
        {
            stage = Math.Min(10, Math.Max(1, stage));
            if (stage <= 2)
            {
                DroneBayYard(b, m, stage, seed);
                return;
            }

            DroneBayCore(b, m, seed, Math.Min(4, stage - 2), stage >= 4, stage >= 5, stage >= 6, stage);
        }

        /// <param name="stage">SPEC-045 stage for the stage-only extras (7 rail, 8 plates, 9 flyer, 10 lights); 0 for the level models.</param>
        private static void DroneBayCore(MeshBuilder b, Model m, uint seed, int parked, bool console, bool hovering, bool sandbags, int stage)
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
            for (int i = 0; i < parked; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * (i < 2 ? 1.5f : 0.6f);
                float z = i < 2 ? -1.7f : 0.3f;
                Drone(b, new Vector3(x, 0.17f, z), rng.Range(0f, 90f), rng);
            }

            if (console)
            {
                // operator console and the control mast with its dish
                KitModules.Workbench(b, m, new Vector3(-2.3f, 0, -2.0f), 90f);
                KitParts.Mast(b, m, new Vector3(2.5f, 0, 2.9f), 5.2f, true);
                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-40f)) * Matrix4x4.CreateTranslation(new Vector3(2.5f, 4.4f, 2.6f)));
                b.Frustum(Vector3.Zero, 0.08f, 0.6f, 0.25f, 14, Mat.SandSteel, 0.02f);
                b.Pop();
            }

            if (hovering)
            {
                // one machine in the air over the pad, rotors turning
                var hover = new MeshBuilder(seed + 43) { GroundOffset = 2.6f };
                Drone(hover, Vector3.Zero, 20f, rng, false);
                m.Parts.Add(new AnimPart(hover.Mesh, new Vector3(0, 2.6f, -0.6f), AnimKind.SweepY, 0.15f, 25f));
            }

            if (sandbags)
            {
                Shapes.SandbagWall(b, new Vector3(-2.8f, 0, -3.0f), new Vector3(-1.2f, 0, -3.0f), 2);
                Shapes.SandbagWall(b, new Vector3(1.2f, 0, -3.0f), new Vector3(2.8f, 0, -3.0f), 2);
            }

            if (stage >= 7)
            {
                // launch rail: an inclined track on trestles along the pad's right edge, a carriage at the low end
                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-12f)) * Matrix4x4.CreateTranslation(new Vector3(2.45f, 0.75f, -0.9f)));
                b.Box(Vector3.Zero, new Vector3(0.3f, 0.12f, 3.0f), Mat.DarkSteel, 0.02f);
                b.Box(new Vector3(0, 0.12f, 0.6f), new Vector3(0.45f, 0.1f, 0.5f), Mat.OliveSteel, 0.02f);
                b.Pop();
                foreach (float z in new[] { -2.0f, -0.9f, 0.2f })
                {
                    b.Strut(new Vector3(2.45f, 0.16f, z), new Vector3(2.45f, 0.75f + ((-0.9f - z) * 0.21f), z), 0.05f, Mat.DarkSteel);
                }
            }

            if (stage >= 8)
            {
                // blast plates hung across the hangar ends
                foreach (int side in new[] { -1, 1 })
                {
                    KitParts.Plate(b, new Vector3(side * 2.2f, 1.2f, 1.15f), 0.9f, 2.2f, 3f, Mat.DarkSteel);
                }
            }

            if (stage >= 9)
            {
                // a second machine on station over the hangar and a floodlight on the mast
                var flyer = new MeshBuilder(seed + 47) { GroundOffset = 4.2f };
                Drone(flyer, Vector3.Zero, -30f, rng, false);
                m.Parts.Add(new AnimPart(flyer.Mesh, new Vector3(-1.2f, 4.2f, 1.6f), AnimKind.SweepY, 0.1f, 30f));
                KitParts.Spotlight(b, m, new Vector3(2.5f, 4.0f, 2.6f), 200f);
            }

            if (stage >= 10)
            {
                // integrated: cyan landing lights on the pad ring
                for (int i = 0; i < 8; i++)
                {
                    float a = MeshBuilder.Deg((i * 45f) + 22.5f);
                    b.Box(new Vector3((float)Math.Cos(a) * 1.7f, 0.18f, -0.6f + ((float)Math.Sin(a) * 1.7f)), new Vector3(0.14f, 0.03f, 0.14f), Mat.NeonCyan, 0f);
                }

                Shapes.Lamp(b, m, new Vector3(0, 0.4f, -0.6f), Mat.NeonCyan, Model.Neon, 0.7f, 3f, LightRole.Status, 0.06f);
            }

            Shapes.Lamp(b, m, new Vector3(-2.5f, 2.6f, 1.2f), Mat.LampAmber, Model.Amber, 1.2f, 5f, LightRole.Status);
        }

        /// <summary>Drone Bay stages 1-2: a crate workbench with one quadcopter, then a painted pad and a charging lead.</summary>
        private static void DroneBayYard(MeshBuilder b, Model m, int stage, uint seed)
        {
            var rng = new ArtRandom(seed + 41);
            KitModules.Workbench(b, m, new Vector3(-1.6f, 0, 0.8f), 0f);
            KitModules.CrateStack(b, new Vector3(-2.5f, 0, 2.2f), rng);
            KitParts.Barrel(b, new Vector3(1.9f, 0, 2.0f), Mat.Rust);
            if (stage >= 2)
            {
                // a painted pad on the dirt, a charging lead from a battery box
                b.BoxOn(0.6f, 0, -1.0f, 2.6f, 0.06f, 2.6f, Mat.ConcreteDark, 0.03f);
                b.Box(new Vector3(0.6f, 0.065f, -1.0f), new Vector3(1.4f, 0.01f, 0.12f), Mat.PaintWhite, 0f);
                b.Box(new Vector3(0.6f, 0.065f, -1.0f), new Vector3(0.12f, 0.01f, 1.4f), Mat.PaintWhite, 0f);
                b.BoxOn(2.2f, 0, -0.2f, 0.5f, 0.4f, 0.35f, Mat.Rubber, 0.02f);
                KitParts.Cable(b, new Vector3(2.0f, 0.3f, -0.2f), new Vector3(0.9f, 0.1f, -1.0f), 0.05f, 0.025f);
                Drone(b, new Vector3(0.6f, 0.06f, -1.0f), 15f, rng);
            }
            else
            {
                Drone(b, new Vector3(0.4f, 0, -0.6f), 30f, rng);
            }

            Props.Lamp(b, m, new Vector3(-1.6f, 1.9f, 0.4f), false, 0.8f, LightRole.Status);
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
            MotorPoolCore(b, m, seed, level >= 2, level >= 3, 0);
        }

        /// <summary>
        /// Motor Pool by visual stage 1-10 (SPEC-045 stage plan), preview only. Stages 1-2 are a pickup in the open; 3-5
        /// are the level models; then a second bay, armour racks, a ramp and pump, a fortified front and cyan lights.
        /// </summary>
        public static void MotorPoolStage(MeshBuilder b, Model m, int stage, uint seed)
        {
            stage = Math.Min(10, Math.Max(1, stage));
            if (stage <= 2)
            {
                MotorPoolYard(b, m, stage, seed);
                return;
            }

            MotorPoolCore(b, m, seed, stage >= 4, stage >= 5, stage);
        }

        /// <param name="stage">SPEC-045 stage for the stage-only extras (6-10); 0 for the level models.</param>
        private static void MotorPoolCore(MeshBuilder b, Model m, uint seed, bool gunTruck, bool gantry, int stage)
        {
            var rng = new ArtRandom(seed + 51);

            // oil-stained apron and the garage: a deep shed over the back of the plot
            b.BoxOn(0, 0, -0.4f, 5.8f, 0.12f, 5.0f, Mat.ConcreteDark, 0.04f);
            KitModules.Shed(b, new Vector3(0, 0, 1.0f), 5.6f, 3.6f, 3.3f, 2.9f, Mat.Rust, true);
            KitModules.RetainingWall(b, new Vector3(0, 0, 2.95f), 5.6f, 1.2f, rng);

            Carrier(b, new Vector3(-0.9f, 0.12f, 0.6f), 0f);
            KitModules.Barrels(b, new Vector3(2.4f, 0.12f, 2.4f), 3, rng);
            KitModules.CrateStack(b, new Vector3(-2.4f, 0.12f, 2.3f), rng);

            if (gunTruck)
            {
                // a gun truck on the apron and a fuel bowser by the wall
                KitModules.Pickup(b, new Vector3(1.75f, 0.12f, -1.6f), 0f, Mat.SandSteel, false);
                b.Strut(new Vector3(1.75f, 1.2f, -0.6f), new Vector3(1.75f, 1.9f, -0.6f), 0.08f, Mat.DarkSteel);
                b.CylinderZ(new Vector3(1.75f, 1.95f, -1.1f), 0.05f, 1.1f, 8, Mat.DarkSteel);
                KitParts.TankH(b, new Vector3(2.35f, 0.75f, 1.6f), 0.5f, 1.6f, Mat.OliveSteel);
            }

            if (gantry)
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

            if (stage >= 6)
            {
                // a second bay: a tarp-covered pickup beside the shed
                KitModules.Pickup(b, new Vector3(-2.2f, 0.12f, -1.9f), 90f, Mat.OliveSteel, false);
                KitParts.Tarp(b, new Vector3(-2.2f, 1.2f, -1.9f), 1.6f, 2.4f, 0.5f, Mat.Tarp, seed + 53);
            }

            if (stage >= 7)
            {
                // armour plates racked against the back wall
                for (int i = 0; i < 4; i++)
                {
                    KitParts.Plate(b, new Vector3(0.6f + (i * 0.5f), 0.9f, 2.55f), 0.9f, 1.5f, -12f, i % 2 == 0 ? Mat.OliveSteel : Mat.DarkSteel);
                }
            }

            if (stage >= 8)
            {
                // service ramp on the apron and a fuel pump
                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(10f)) * Matrix4x4.CreateTranslation(new Vector3(0.4f, 0.35f, -2.5f)));
                b.Box(Vector3.Zero, new Vector3(1.6f, 0.12f, 1.4f), Mat.DarkSteel, 0.02f);
                b.Pop();
                b.BoxOn(2.7f, 0.12f, -0.2f, 0.4f, 1.3f, 0.35f, Mat.PaintRed, 0.03f);
                KitParts.Cable(b, new Vector3(2.55f, 1.0f, -0.35f), new Vector3(2.0f, 0.2f, -0.6f), 0.2f, 0.03f);
            }

            if (stage >= 9)
            {
                // fortified front: sandbags and a tank trap flank the apron mouth
                Shapes.SandbagWall(b, new Vector3(-2.9f, 0, -3.1f), new Vector3(-1.4f, 0, -3.1f), 3);
                Shapes.SandbagWall(b, new Vector3(1.6f, 0, -3.1f), new Vector3(2.9f, 0, -3.1f), 3);
                KitModules.Hedgehog(b, new Vector3(-3.0f, 0, 0.4f), 30f);
            }

            if (stage >= 10)
            {
                // integrated: cyan running lights on the carrier and a shielded conduit along the wall
                foreach (int side in new[] { -1, 1 })
                {
                    b.Box(new Vector3(-0.9f + (side * 0.96f), 1.0f, 0.6f), new Vector3(0.03f, 0.04f, 3.6f), Mat.NeonCyan, 0f);
                }

                b.BoxOn(2.9f, 0, 0.0f, 0.4f, 0.3f, 4.6f, Mat.DarkSteel, 0.02f);
                b.Box(new Vector3(2.9f, 0.31f, 0.0f), new Vector3(0.1f, 0.02f, 4.4f), Mat.NeonCyan, 0f);
            }

            Shapes.Lamp(b, m, new Vector3(0, 3.0f, -0.8f), Mat.LampAmber, Model.Amber, 1.4f, 6f, LightRole.Status);
        }

        /// <summary>Motor Pool stages 1-2: a pickup under a tarp with jerrycans, then a tool rack and an engine hoist.</summary>
        private static void MotorPoolYard(MeshBuilder b, Model m, int stage, uint seed)
        {
            var rng = new ArtRandom(seed + 51);
            KitModules.Pickup(b, new Vector3(-0.6f, 0, 0.2f), 10f, Mat.SandSteel, false);
            KitParts.Tarp(b, new Vector3(-0.4f, 1.3f, 0.9f), 1.8f, 1.6f, 0.4f, Mat.TarpBlue, seed + 52);
            for (int i = 0; i < 3; i++)
            {
                b.BoxOn(1.6f + (i * 0.3f), 0, -1.6f, 0.22f, 0.45f, 0.14f, i == 1 ? Mat.OliveSteel : Mat.PaintRed, 0.02f);
            }

            KitModules.Barrels(b, new Vector3(2.4f, 0, 2.0f), 2, rng);
            Props.Lamp(b, m, new Vector3(1.0f, 1.8f, -0.8f), false, 0.8f, LightRole.Status);
            if (stage >= 2)
            {
                // tool rack and an A-frame hoist with an engine on the chain
                b.BoxOn(-2.4f, 0, 2.3f, 1.6f, 1.6f, 0.4f, Mat.DarkSteel, 0.02f);
                foreach (int side in new[] { -1, 1 })
                {
                    b.Strut(new Vector3(1.8f + (side * 0.8f), 0, 0.9f), new Vector3(1.8f, 2.4f, 0.9f), 0.06f, Mat.Rust);
                }

                KitParts.Cable(b, new Vector3(1.8f, 2.4f, 0.9f), new Vector3(1.8f, 1.2f, 0.9f), 0.02f, 0.03f, Mat.DarkSteel);
                b.Box(new Vector3(1.8f, 1.0f, 0.9f), new Vector3(0.6f, 0.4f, 0.45f), Mat.DarkSteel, 0.04f);
                KitModules.CrateStack(b, new Vector3(-2.4f, 0, -2.2f), rng);
            }
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
