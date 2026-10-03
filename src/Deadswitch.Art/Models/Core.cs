using System;
using System.Numerics;
using Deadswitch.Art.Geometry;

namespace Deadswitch.Art.Models
{
    /// <summary>The AI core bunker at the Hub's center: the thing everything protects.</summary>
    public static class Core
    {
        public static Model Build(uint seed)
        {
            var m = new Model();
            var b = new MeshBuilder(seed);

            b.Frustum(new Vector3(0, -0.2f, 0), 3.3f, 3.0f, 0.5f, 6, Mat.ConcreteDark, 0.1f);
            b.Frustum(new Vector3(0, 0.3f, 0), 2.9f, 2.6f, 1.5f, 6, Mat.Concrete, 0.16f);
            b.Frustum(new Vector3(0, 1.8f, 0), 2.2f, 1.25f, 0.9f, 6, Mat.ConcreteDark, 0.12f);
            b.Frustum(new Vector3(0, 2.7f, 0), 1.0f, 0.85f, 0.35f, 6, Mat.DarkSteel, 0.05f);

            // blast door facing the camera (-Z), with hazard frame
            b.BoxOn(0, 0.3f, -2.55f, 1.6f, 1.3f, 0.3f, Mat.DarkSteel, 0.06f);
            b.BoxOn(0, 0.3f, -2.72f, 1.1f, 1.05f, 0.08f, Mat.Rust, 0.03f);
            Shapes.Hazard(b, -0.8f, 0.8f, 1.6f, 1.72f, -2.72f, 10);
            Shapes.Lamp(b, m, new Vector3(-0.95f, 1.45f, -2.75f), Mat.LampAmber, Model.Amber, 1.2f, 4f, LightRole.Ambient, 0.12f);
            Shapes.Lamp(b, m, new Vector3(0.95f, 1.45f, -2.75f), Mat.LampAmber, Model.Amber, 1.2f, 4f, LightRole.Ambient, 0.12f);

            // the AI's eye: a phosphor lens on the dome, looking at the handler
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-62f)) * Matrix4x4.CreateTranslation(new Vector3(0, 2.25f, -1.5f)));
            b.Frustum(Vector3.Zero, 0.55f, 0.5f, 0.12f, 12, Mat.DarkSteel, 0.02f, true);
            b.Frustum(new Vector3(0, 0.12f, 0), 0.36f, 0.3f, 0.05f, 12, Mat.Screen, 0.01f, true);
            b.Pop();
            m.Lights.Add(new LightSpec(new Vector3(0, 2.6f, -2.1f), Model.Phosphor, 2.2f, 6f, LightRole.Ambient));

            // phosphor status lamps around the crown
            for (int i = 0; i < 6; i++)
            {
                float a = MeshBuilder.Deg(30f + (i * 60f));
                var p = new Vector3((float)Math.Cos(a) * 1.95f, 2.0f, (float)Math.Sin(a) * 1.95f);
                b.Box(p, new Vector3(0.14f, 0.1f, 0.14f), Mat.LampPhosphor, 0.02f);
            }

            // antenna mast with cross arms and a red tip
            b.Strut(new Vector3(0, 3.0f, 0.3f), new Vector3(0, 6.4f, 0.3f), 0.12f, Mat.DarkSteel);
            for (int i = 0; i < 3; i++)
            {
                float y = 3.9f + (i * 0.85f);
                float w = 0.9f - (i * 0.2f);
                b.Strut(new Vector3(-w, y, 0.3f), new Vector3(w, y, 0.3f), 0.05f, Mat.DarkSteel);
                b.Strut(new Vector3(0, y, 0.3f - w), new Vector3(0, y, 0.3f + w), 0.05f, Mat.DarkSteel);
            }

            Shapes.Lamp(b, m, new Vector3(0, 6.5f, 0.3f), Mat.LampRed, Model.Red, 1.5f, 4f, LightRole.Ambient, 0.15f);

            // vents, pipes and a dish on the shoulder
            b.BoxOn(1.6f, 1.8f, 0.6f, 0.7f, 0.35f, 0.5f, Mat.DarkSteel, 0.05f);
            b.BoxOn(-1.5f, 1.8f, 0.9f, 0.5f, 0.5f, 0.5f, Mat.DarkSteel, 0.05f);
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(30f)) * Matrix4x4.CreateRotationY(MeshBuilder.Deg(30f)) * Matrix4x4.CreateTranslation(new Vector3(-1.5f, 2.35f, 0.9f)));
            b.Frustum(Vector3.Zero, 0.1f, 0.55f, 0.2f, 12, Mat.SandSteel, 0.02f, true, Mat.Concrete);
            b.Pop();

            Shapes.SandbagWall(b, new Vector3(-2.2f, 0.3f, -3.4f), new Vector3(-1.1f, 0.3f, -3.6f), 2);
            Shapes.SandbagWall(b, new Vector3(1.1f, 0.3f, -3.6f), new Vector3(2.2f, 0.3f, -3.4f), 2);

            m.Static = b.Mesh;
            return m;
        }
    }
}
