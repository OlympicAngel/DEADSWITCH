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
        public const float FacadeZ = 9.5f;

        /// <summary>Where the core's light and the "CORE" label sit (in front of the door).</summary>
        public static readonly Vector3 DoorPoint = new Vector3(0, 0, FacadeZ - 0.6f);

        public static Model Build(uint seed)
        {
            var m = new Model();
            var b = new MeshBuilder(seed) { AoHeight = 4.5f, AoFloor = 0.45f, FaceJitter = 0.1f };
            var rng = new ArtRandom(seed + 3);
            const float z = FacadeZ;

            // main mass, sunk into the hill
            b.BoxOn(0, -0.5f, z + 6f, 30f, 9f, 12f, Mat.ConcreteDark, 0.3f);
            b.BoxOn(0, -0.2f, z + 0.35f, 30.5f, 1.0f, 0.9f, Mat.Concrete, 0.12f);

            // facade bays (pilasters) and horizontal bands
            for (int i = -3; i <= 3; i++)
            {
                if (i == 0)
                {
                    continue;
                }

                b.BoxOn(i * 4.3f, 0, z - 0.25f, 0.9f, 7.6f, 0.6f, Mat.Concrete, 0.12f);
            }

            b.BoxOn(0, 6.6f, z - 0.2f, 30f, 0.7f, 0.5f, Mat.Concrete, 0.1f);
            for (int i = 0; i < 22; i++)
            {
                float gx = rng.Range(-14.5f, 14.5f);
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
                b.Strut(new Vector3(sx * 3.1f, 3.6f, z - 1.8f), new Vector3(sx * 3.1f, 3.6f, z - 2.4f), 0.08f, Mat.DarkSteel);
                Shapes.Lamp(b, m, new Vector3(sx * 3.1f, 3.5f, z - 2.45f), Mat.LampAmber, Model.Amber, 2.6f, 7f, LightRole.Ambient, 0.2f);
            }

            // stencil designation, faded
            Props.Stencil(b, "S-17", new Vector3(-12.6f, 3.4f, z - 0.03f), 0.34f, Mat.PaintWhite);
            Shapes.Hazard(b, 8.8f, 12.6f, 0.3f, 0.8f, z - 0.03f, 10);

            // collapsed roof slab overhang with broken edge and rebar
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(3.5f)) * Matrix4x4.CreateTranslation(new Vector3(0, 8.3f, z + 1.0f)));
            b.Box(new Vector3(-3f, 0, 0), new Vector3(22f, 1.1f, 5.0f), Mat.Concrete, 0.2f);
            for (int i = 0; i < 9; i++)
            {
                float x = -13.5f + (i * 2.4f) + rng.Range(-0.4f, 0.4f);
                b.Box(new Vector3(x, rng.Range(-0.2f, 0.1f), -2.6f - rng.Range(0f, 0.6f)), new Vector3(rng.Range(1.0f, 2.2f), rng.Range(0.7f, 1.1f), rng.Range(0.6f, 1.4f)), Mat.Concrete, 0.12f);
                b.Strut(new Vector3(x, 0, -2.4f), new Vector3(x + rng.Range(-0.3f, 0.3f), rng.Range(-0.6f, 0.4f), -3.6f - rng.Range(0f, 0.8f)), 0.05f, Mat.Rust);
            }

            b.Pop();
            b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(-14f)) * Matrix4x4.CreateTranslation(new Vector3(11.5f, 7.0f, z + 1.6f)));
            b.Box(Vector3.Zero, new Vector3(7f, 0.9f, 4.6f), Mat.Concrete, 0.18f);
            b.Pop();
            for (int i = 0; i < 9; i++)
            {
                Props.Boulder(b, new Vector3(rng.Range(7f, 14f), rng.Range(0.3f, 1.2f), z - rng.Range(0.6f, 2.2f)), new Vector3(rng.Range(0.6f, 1.3f), rng.Range(0.4f, 0.8f), rng.Range(0.5f, 1.0f)), seed + (uint)i, Mat.Concrete);
            }

            // pipes and cables along the facade
            b.CylinderX(new Vector3(-8f, 5.8f, z - 0.7f), 0.18f, 12f, 10, Mat.Rust, 0.03f);
            b.CylinderX(new Vector3(-8f, 5.35f, z - 0.65f), 0.1f, 12f, 8, Mat.DarkSteel, 0.02f);
            b.Strut(new Vector3(-2.1f, 5.35f, z - 0.65f), new Vector3(-2.6f, 0.2f, z - 1.2f), 0.09f, Mat.Rubber);

            // scaffold tower with a blue tarp, left of the door
            float sx0 = -7.6f;
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
            return m;
        }
    }
}
