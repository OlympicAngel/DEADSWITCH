using System;
using System.Numerics;
using Deadswitch.Art.Geometry;

namespace Deadswitch.Art.Models
{
    /// <summary>
    /// The ruined pre-war bunker the compound is built against. Its blast door is the way down to the AI core:
    /// green phosphor light spills out into the courtyard. World space, facade at z = <see cref="FacadeZ"/>.
    /// </summary>
    public static class Core
    {
        public const float FacadeZ = 12.5f;

        /// <summary>Where the core's light and the "CORE" label sit (in front of the door).</summary>
        public static readonly Vector3 DoorPoint = new Vector3(0, 0, FacadeZ - 0.6f);

        public static Model Build(uint seed)
        {
            var m = new Model();
            var b = new MeshBuilder(seed) { AoHeight = 4.5f, AoFloor = 0.45f, FaceJitter = 0.1f };
            var rng = new ArtRandom(seed + 3);
            const float z = FacadeZ;

            // main mass, sunk into the hill
            b.BoxOn(0, -0.5f, z + 6f, 38f, 9.5f, 12f, Mat.ConcreteDark, 0.25f);
            b.BoxOn(0, -0.2f, z + 0.35f, 38.5f, 1.0f, 0.9f, Mat.Concrete, 0.1f);

            // facade bays (pilasters) and horizontal bands
            for (int i = -4; i <= 4; i++)
            {
                if (i == 0)
                {
                    continue;
                }

                b.BoxOn(i * 4.3f, 0, z - 0.25f, 0.9f, 7.6f, 0.6f, Mat.Concrete, 0.12f);
            }

            b.BoxOn(0, 6.6f, z - 0.2f, 38f, 0.7f, 0.5f, Mat.Concrete, 0.08f);
            for (int i = 0; i < 22; i++)
            {
                float gx = rng.Range(-18.5f, 18.5f);
                if (Math.Abs(gx) < 4f)
                {
                    continue;
                }

                float gh = rng.Range(1.2f, 4.5f);
                b.Box(new Vector3(gx, 6.4f - (gh * 0.5f), z - 0.02f), new Vector3(rng.Range(0.3f, 1.4f), gh, 0.03f), rng.Next() < 0.6f ? Mat.ConcreteDark : Mat.Rust, 0f);
            }

            // door portal: thick frame, green-lit interior, half-open blast door slid aside
            b.BoxOn(0, 0, z - 0.9f, 7.4f, 6.2f, 1.6f, Mat.Concrete, 0.2f);
            b.BoxOn(0, 0.2f, z - 1.75f, 4.6f, 4.4f, 0.2f, Mat.Screen, 0.02f);
            b.BoxOn(0, 4.6f, z - 1.8f, 5.0f, 0.5f, 0.3f, Mat.DarkSteel, 0.05f);
            b.BoxOn(-2.45f, 0, z - 1.8f, 0.3f, 4.6f, 0.3f, Mat.DarkSteel, 0.05f);
            b.BoxOn(2.45f, 0, z - 1.8f, 0.3f, 4.6f, 0.3f, Mat.DarkSteel, 0.05f);
            b.BoxOn(-1.25f, 0.05f, z - 2.15f, 2.6f, 4.3f, 0.45f, Mat.DarkSteel, 0.1f);
            Shapes.Hazard(b, -2.5f, 2.5f, 4.65f, 5.0f, z - 1.98f, 14);
            for (int i = 0; i < 4; i++)
            {
                b.Box(new Vector3(-1.25f, 0.7f + (i * 1.0f), z - 2.4f), new Vector3(2.3f, 0.12f, 0.05f), Mat.Rust, 0.01f);
            }

            m.Lights.Add(new LightSpec(new Vector3(0.9f, 1.8f, z - 3.2f), Model.Phosphor, 3.2f, 11f, LightRole.Ambient));
            m.Lights.Add(new LightSpec(new Vector3(0.9f, 2.4f, z - 1.4f), Model.Phosphor, 2.0f, 5f, LightRole.Ambient));

            // the AI's eye above the door
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-90f)) * Matrix4x4.CreateTranslation(new Vector3(0, 5.6f, z - 1.75f)));
            b.Frustum(Vector3.Zero, 0.55f, 0.5f, 0.2f, 12, Mat.DarkSteel, 0.03f);
            b.Frustum(new Vector3(0, 0.2f, 0), 0.32f, 0.28f, 0.06f, 12, Mat.LampPhosphor, 0.01f);
            b.Pop();

            // wall lamps by the door
            foreach (int sx in new[] { -1, 1 })
            {
                b.Strut(new Vector3(sx * 3.1f, 3.7f, z - 1.8f), new Vector3(sx * 3.1f, 3.7f, z - 2.5f), 0.08f, Mat.DarkSteel);
                Props.Lamp(b, m, new Vector3(sx * 3.1f, 3.6f, z - 2.55f), true, 2.2f);
            }

            // stencil designation, faded
            Props.Stencil(b, "S-17", new Vector3(-15.4f, 3.4f, z - 0.03f), 0.36f, Mat.PaintWhite);
            Shapes.Hazard(b, 10.8f, 14.6f, 0.3f, 0.8f, z - 0.03f, 10);

            // facade panel joints (formwork seams) and a stepped, broken roofline
            for (int i = 0; i < 18; i++)
            {
                float jx = -18.5f + (i * 2.2f);
                if (Math.Abs(jx) > 3.8f)
                {
                    b.Box(new Vector3(jx, 3.3f, z - 0.02f), new Vector3(0.05f, 6.4f, 0.03f), Mat.ConcreteDark, 0f);
                }
            }

            b.Box(new Vector3(0, 3.3f, z - 0.02f), new Vector3(38f, 0.05f, 0.03f), Mat.ConcreteDark, 0f);
            b.BoxOn(0, 8.0f, z + 0.9f, 38.6f, 0.9f, 2.2f, Mat.Concrete, 0.1f);
            for (int i = 0; i < 12; i++)
            {
                float x = -18f + (i * 3.3f) + rng.Range(-0.5f, 0.5f);
                float h = rng.Range(0.3f, 1.4f);
                b.BoxOn(x, 8.9f, z + rng.Range(0.3f, 1.6f), rng.Range(1.6f, 3.0f), h, rng.Range(0.8f, 1.8f), Mat.Concrete, 0.1f);
                if (rng.Next() < 0.6f)
                {
                    b.Strut(new Vector3(x, 8.9f + h, z + 0.6f), new Vector3(x + rng.Range(-0.4f, 0.4f), 9.6f + h, z + rng.Range(0f, 0.8f)), 0.04f, Mat.Rust);
                    b.Strut(new Vector3(x + 0.3f, 8.9f + h, z + 0.6f), new Vector3(x + 0.3f + rng.Range(-0.4f, 0.4f), 9.4f + h, z + rng.Range(-0.4f, 0.4f)), 0.04f, Mat.Rust);
                }
            }

            // collapsed corner: slab sheared down onto a rubble slope
            b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(-22f)) * Matrix4x4.CreateRotationY(MeshBuilder.Deg(6f)) * Matrix4x4.CreateTranslation(new Vector3(15.5f, 5.6f, z + 0.9f)));
            b.Box(Vector3.Zero, new Vector3(8f, 0.9f, 4.4f), Mat.Concrete, 0.12f);
            b.Pop();
            for (int i = 0; i < 16; i++)
            {
                Props.Boulder(b, new Vector3(rng.Range(11f, 19f), rng.Range(0.2f, 2.6f) * (1f - (i / 20f)), z - rng.Range(0.2f, 2.8f)), new Vector3(rng.Range(0.5f, 1.3f), rng.Range(0.35f, 0.8f), rng.Range(0.5f, 1.1f)), seed + (uint)i, i % 3 == 0 ? Mat.ConcreteDark : Mat.Concrete);
            }

            // rooftop: comms shack, antenna mast with guy wires, old gun emplacement, sandbags, moss
            b.BoxOn(-6f, 8.9f, z + 3.0f, 3.2f, 2.2f, 2.6f, Mat.ConcreteDark, 0.08f);
            b.BoxOn(-6f, 11.1f, z + 3.0f, 3.5f, 0.15f, 2.9f, Mat.Rust, 0.02f);
            b.Box(new Vector3(-6.5f, 10.1f, z + 1.68f), new Vector3(0.9f, 0.6f, 0.04f), Mat.Interior, 0f);
            Vector3 mast = new Vector3(-3.6f, 8.9f, z + 3.4f);
            b.Frustum(mast, 0.16f, 0.07f, 9f, 8, Mat.DarkSteel, 0.01f);
            for (int i = 1; i <= 5; i++)
            {
                b.Strut(mast + new Vector3(-0.6f, i * 1.6f, 0), mast + new Vector3(0.6f, i * 1.6f, 0), 0.03f, Mat.DarkSteel);
            }

            foreach (Vector3 g in new[] { new Vector3(-6.6f, 0, -2.4f), new Vector3(3.2f, 0, -1.8f), new Vector3(-0.6f, 0, 4.4f) })
            {
                b.Strut(mast + new Vector3(0, 8.6f, 0), mast + g, 0.015f, Mat.DarkSteel);
            }

            Shapes.Lamp(b, m, mast + new Vector3(0, 9.1f, 0), Mat.LampRed, Model.Red, 1.2f, 5f, LightRole.Ambient, 0.18f);
            Shapes.SandbagRing(b, new Vector3(7f, 8.9f, z + 2.8f), 1.8f, 16, 2, 250f, 40f);
            b.Frustum(new Vector3(7f, 8.9f, z + 2.8f), 0.6f, 0.5f, 0.6f, 12, Mat.DarkSteel, 0.03f);
            b.CylinderZ(new Vector3(7f, 9.8f, z + 1.7f), 0.09f, 2.0f, 8, Mat.DarkSteel, 0.01f);
            for (int i = 0; i < 26; i++)
            {
                Props.Boulder(b, new Vector3(rng.Range(-18f, 18f), 8.85f, z + rng.Range(0.6f, 7f)), new Vector3(rng.Range(0.4f, 1.1f), rng.Range(0.15f, 0.3f), rng.Range(0.4f, 1.0f)), seed + 40 + (uint)i, Mat.Foliage);
            }

            // pipes and cables along the facade
            b.CylinderX(new Vector3(-9f, 5.8f, z - 0.7f), 0.18f, 14f, 12, Mat.Rust, 0.03f);
            b.CylinderX(new Vector3(-9f, 5.35f, z - 0.65f), 0.1f, 14f, 10, Mat.DarkSteel, 0.02f);
            b.Strut(new Vector3(-2.1f, 5.35f, z - 0.65f), new Vector3(-2.6f, 0.2f, z - 1.2f), 0.09f, Mat.Rubber);

            // scaffold tower with a blue tarp, left of the door
            float sx0 = -8.6f;
            foreach (float x in new[] { sx0 - 1.2f, sx0 + 1.2f })
            {
                foreach (float dz in new[] { -2.2f, -0.6f })
                {
                    b.Strut(new Vector3(x, 0, z + dz), new Vector3(x, 6.2f, z + dz), 0.09f, Mat.Rust);
                }
            }

            for (float y = 1.6f; y < 6.2f; y += 1.5f)
            {
                b.BoxOn(sx0, y, z - 1.4f, 2.6f, 0.07f, 1.7f, Mat.Wood, 0.01f);
                b.Strut(new Vector3(sx0 - 1.2f, y + 0.9f, z - 2.2f), new Vector3(sx0 + 1.2f, y + 0.9f, z - 2.2f), 0.04f, Mat.Paint);
            }

            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(8f)) * Matrix4x4.CreateTranslation(new Vector3(sx0 + 0.3f, 4.6f, z - 2.35f)));
            b.Box(Vector3.Zero, new Vector3(2.4f, 2.4f, 0.05f), Mat.TarpBlue, 0.01f);
            b.Pop();
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-18f)) * Matrix4x4.CreateTranslation(new Vector3(3.6f, 6.1f, z - 0.9f)));
            b.Box(Vector3.Zero, new Vector3(3.2f, 0.05f, 1.6f), Mat.TarpBlue, 0.01f);
            b.Pop();

            // sandbag walls flanking the door
            Shapes.SandbagWall(b, new Vector3(-4.6f, 0, z - 2.6f), new Vector3(-3.2f, 0, z - 3.4f), 3);
            Shapes.SandbagWall(b, new Vector3(3.2f, 0, z - 3.4f), new Vector3(4.6f, 0, z - 2.6f), 3);

            // floodlight on the roof edge
            b.Strut(new Vector3(5.5f, 7.2f, z + 0.3f), new Vector3(5.5f, 9.6f, z + 0.3f), 0.1f, Mat.DarkSteel);
            Shapes.Lamp(b, m, new Vector3(5.5f, 9.7f, z - 0.05f), Mat.LampAmber, Model.Amber, 2.0f, 10f, LightRole.Ambient, 0.3f);

            m.Static = b.Mesh;
            m.Cones = b.Cones;
            return m;
        }
    }
}
