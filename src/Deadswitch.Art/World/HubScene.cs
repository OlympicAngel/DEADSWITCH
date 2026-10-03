using System;
using System.Collections.Generic;
using System.Numerics;
using Deadswitch.Art.Geometry;
using Deadswitch.Art.Models;
using Deadswitch.Sim.State;

namespace Deadswitch.Art.World
{
    /// <summary>What the scene needs to know about one slot (mapped from the sim by the host).</summary>
    public readonly struct SlotView
    {
        public SlotView(FacilityKind kind, int level, bool powered, bool unmanned, FacilityKind buildingKind, int buildingLevel)
        {
            Kind = kind;
            Level = level;
            Powered = powered;
            Unmanned = unmanned;
            BuildingKind = buildingKind;
            BuildingLevel = buildingLevel;
        }

        public FacilityKind Kind { get; }

        public int Level { get; }

        public bool Powered { get; }

        public bool Unmanned { get; }

        /// <summary>Kind under construction on this slot (None if no job).</summary>
        public FacilityKind BuildingKind { get; }

        public int BuildingLevel { get; }

        public bool UnderConstruction => BuildingKind != FacilityKind.None;

        public static SlotView From(GameState s, int slot)
        {
            FacilitySlot f = s.Slots[slot];
            BuildJob? job = s.JobForSlot(slot);
            return new SlotView(f.Kind, f.Level, f.Enabled && f.Powered, !f.IsEmpty && f.Enabled && !f.Staffed, job?.Kind ?? FacilityKind.None, job?.TargetLevel ?? 0);
        }
    }

    /// <summary>
    /// Hub layout and the static world (SPEC-003 rule 3): terrain, the core, pads, perimeter, props.
    /// Unity and the preview place facility models (see <see cref="Facilities"/>) at <see cref="SlotPosition"/>.
    /// </summary>
    public static class HubScene
    {
        public const float SlotRadius = 8.6f;
        public const float FlatRadius = 15f;
        public const float WorldRadius = 42f;

        /// <summary>Pad center for a slot: a ring around the core, slot 0 front-left of the camera.</summary>
        public static Vector3 SlotPosition(int slot, int slotCount)
        {
            float deg = 210f - (slot * 360f / slotCount);
            float a = MeshBuilder.Deg(deg);
            var p = new Vector3((float)Math.Cos(a) * SlotRadius, 0, (float)Math.Sin(a) * SlotRadius);
            p.Y = Height(p.X, p.Z, 0);
            return p;
        }

        /// <summary>Terrain height (meters): flat inside the perimeter, rolling ruined ground outside.</summary>
        public static float Height(float x, float z, uint seed)
        {
            float r = (float)Math.Sqrt((x * x) + (z * z));
            float n = (Noise.Fbm(x * 0.07f, z * 0.07f, seed + 5, 4) - 0.5f) * 3.2f;
            float t = Smooth(FlatRadius - 2f, FlatRadius + 8f, r);
            float ditch = -0.35f * (float)Math.Exp(-Math.Pow((r - (FlatRadius + 1.5f)) / 1.2f, 2));
            float flat = (Noise.Fbm(x * 0.3f, z * 0.3f, seed + 9, 2) - 0.5f) * 0.12f;
            return (flat * (1 - t)) + (n * t) + ditch;
        }

        public static MeshData Terrain(uint seed)
        {
            var b = new MeshBuilder(seed) { AoFloor = 1f, FaceJitter = 0.035f };
            const int n = 70;
            float step = (WorldRadius * 2f) / n;
            var crater = new List<Vector3>();
            var rng = new ArtRandom(seed + 77);
            for (int i = 0; i < 7; i++)
            {
                float a = rng.Range(0f, 6.28f);
                float rr = rng.Range(FlatRadius + 6f, WorldRadius - 6f);
                crater.Add(new Vector3((float)Math.Cos(a) * rr, rng.Range(1.6f, 3.4f), (float)Math.Sin(a) * rr));
            }

            float H(float x, float z)
            {
                float h = Height(x, z, seed);
                foreach (Vector3 c in crater)
                {
                    float d = (float)Math.Sqrt(((x - c.X) * (x - c.X)) + ((z - c.Z) * (z - c.Z))) / c.Y;
                    h += (d < 1f ? -0.9f * (1 - (d * d)) : 0f) + (0.35f * (float)Math.Exp(-Math.Pow((d - 1.05f) / 0.18f, 2)));
                }

                return h;
            }

            for (int j = 0; j < n; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    float x0 = -WorldRadius + (i * step);
                    float z0 = -WorldRadius + (j * step);
                    float x1 = x0 + step;
                    float z1 = z0 + step;
                    float cx = (x0 + x1) * 0.5f;
                    float cz = (z0 + z1) * 0.5f;
                    if (((cx * cx) + (cz * cz)) > WorldRadius * WorldRadius)
                    {
                        continue;
                    }

                    var p00 = new Vector3(x0, H(x0, z0), z0);
                    var p10 = new Vector3(x1, H(x1, z0), z0);
                    var p11 = new Vector3(x1, H(x1, z1), z1);
                    var p01 = new Vector3(x0, H(x0, z1), z1);
                    Vector3 below = new Vector3(cx, -50f, cz);
                    b.FaceTinted(below, Mat.Ground, new[] { p00, p10, p11 }, new[] { Tone(p00), Tone(p10), Tone(p11) });
                    b.FaceTinted(below, Mat.Ground, new[] { p00, p11, p01 }, new[] { Tone(p00), Tone(p11), Tone(p01) });
                }
            }

            float Tone(Vector3 p)
            {
                float r = (float)Math.Sqrt((p.X * p.X) + (p.Z * p.Z));
                float mud = Noise.Fbm(p.X * 0.11f, p.Z * 0.11f, seed + 31, 4);
                float grit = Noise.Value(p.X * 1.3f, p.Z * 1.3f, seed + 3);
                float inside = 1f - Smooth(FlatRadius - 1f, FlatRadius + 1f, r);
                float wet = Smooth(0.55f, 0.75f, Noise.Fbm(p.X * 0.05f, p.Z * 0.05f, seed + 41, 3));
                float tone = 0.7f + ((mud - 0.5f) * 0.9f) + ((grit - 0.5f) * 0.14f);
                tone += Road(p.X, p.Z) * 0.32f;
                tone += inside * 0.12f;
                tone -= wet * 0.22f;
                tone -= Math.Max(0f, -p.Y) * 0.35f;
                return Math.Max(0.25f, tone);
            }

            return b.Mesh;
        }

        /// <summary>Perimeter barriers, fences, lamps, wrecks and debris. Static for the whole run.</summary>
        public static Model Surroundings(uint seed, int slotCount)
        {
            var m = new Model();
            var b = new MeshBuilder(seed);
            var rng = new ArtRandom(seed + 13);

            // Perimeter: concrete jersey barriers with a gate toward the camera, sandbag sections, fence and lamps.
            const float pr = FlatRadius - 0.8f;
            int segments = 44;
            for (int i = 0; i < segments; i++)
            {
                float deg = i * 360f / segments;
                if (deg > 255f && deg < 285f)
                {
                    continue;
                }

                float a = MeshBuilder.Deg(deg);
                var p = new Vector3((float)Math.Cos(a) * pr, 0, (float)Math.Sin(a) * pr);
                p.Y = Height(p.X, p.Z, seed);
                b.Push(Matrix4x4.CreateRotationY(-a + MeshBuilder.Deg(90)) * Matrix4x4.CreateTranslation(p));
                if (i % 7 == 3)
                {
                    Shapes.SandbagWall(b, new Vector3(-0.9f, 0, 0), new Vector3(0.9f, 0, 0), 3);
                }
                else
                {
                    b.BoxOn(0, 0, 0, 1.85f, 0.85f, 0.5f, i % 5 == 0 ? Mat.ConcreteDark : Mat.Concrete, 0.12f);
                    b.BoxOn(0, 0, 0, 1.7f, 0.25f, 0.75f, Mat.Concrete, 0.08f);
                }

                b.Pop();

                if (i % 4 == 0)
                {
                    var post = new Vector3((float)Math.Cos(a) * (pr + 1.0f), p.Y, (float)Math.Sin(a) * (pr + 1.0f));
                    b.Strut(post, post + new Vector3(0, 2.0f, 0), 0.08f, Mat.Rust);
                }
            }

            for (int i = 0; i < 6; i++)
            {
                float deg = i * 60f;
                float a = MeshBuilder.Deg(deg);
                var p = new Vector3((float)Math.Cos(a) * (pr - 1.6f), 0, (float)Math.Sin(a) * (pr - 1.6f));
                b.Strut(p, p + new Vector3(0, 3.4f, 0), 0.12f, Mat.DarkSteel);
                b.Strut(p + new Vector3(0, 3.3f, 0), p + new Vector3(-(float)Math.Cos(a) * 0.6f, 3.4f, -(float)Math.Sin(a) * 0.6f), 0.07f, Mat.DarkSteel);
                Shapes.Lamp(b, m, p + new Vector3(-(float)Math.Cos(a) * 0.65f, 3.3f, -(float)Math.Sin(a) * 0.65f), Mat.LampAmber, Model.Amber, 3.2f, 9f, LightRole.Ambient, 0.22f);
            }

            // Ground cables from the core to every pad.
            for (int s = 0; s < slotCount; s++)
            {
                Vector3 pad = SlotPosition(s, slotCount);
                Vector3 dir = Vector3.Normalize(new Vector3(pad.X, 0, pad.Z));
                b.Strut(dir * 3.2f + new Vector3(0, 0.05f, 0), pad - (dir * 2.5f) + new Vector3(0, 0.05f, 0), 0.09f, Mat.Rubber);
            }

            // Clutter inside the walls: crates, barrels, tires (kept off pads and roads).
            for (int i = 0; i < 26; i++)
            {
                float a = rng.Range(0f, 6.283f);
                float r = rng.Range(4.2f, pr - 1.8f);
                var p = new Vector3((float)Math.Cos(a) * r, 0, (float)Math.Sin(a) * r);
                if (NearPad(p, slotCount, 3.3f) || Road(p.X, p.Z) > 0.4f)
                {
                    continue;
                }

                p.Y = Height(p.X, p.Z, seed);
                float yaw = rng.Range(0f, 360f);
                b.Push(p, yaw);
                switch (i % 4)
                {
                    case 0:
                        b.BoxOn(0, 0, 0, 0.8f, 0.6f, 0.8f, Mat.Wood, 0.05f);
                        b.BoxOn(0.1f, 0.6f, 0.05f, 0.6f, 0.45f, 0.6f, Mat.Wood, 0.05f);
                        break;
                    case 1:
                        b.Frustum(Vector3.Zero, 0.3f, 0.3f, 0.9f, 10, i % 3 == 0 ? Mat.Rust : Mat.OliveSteel, 0.04f);
                        b.Frustum(new Vector3(0.65f, 0, 0.1f), 0.3f, 0.3f, 0.9f, 10, Mat.Rust, 0.04f);
                        break;
                    case 2:
                        for (int k = 0; k < 3; k++)
                        {
                            b.Frustum(new Vector3(0, k * 0.24f, 0), 0.38f, 0.38f, 0.22f, 10, Mat.Rubber, 0.08f);
                        }

                        break;
                    default:
                        Shapes.SandbagWall(b, new Vector3(-0.8f, 0, 0), new Vector3(0.8f, 0, 0), 2);
                        break;
                }

                b.Pop();
            }

            // Outside: rubble, dead trees, a wrecked truck.
            for (int i = 0; i < 70; i++)
            {
                float a = rng.Range(0f, 6.283f);
                float r = rng.Range(FlatRadius + 1.5f, WorldRadius - 3f);
                var p = new Vector3((float)Math.Cos(a) * r, 0, (float)Math.Sin(a) * r);
                p.Y = Height(p.X, p.Z, seed) - 0.1f;
                b.Push(p, rng.Range(0f, 360f));
                if (i % 5 == 0)
                {
                    float h = rng.Range(2.5f, 4.5f);
                    b.Frustum(Vector3.Zero, 0.16f, 0.09f, h, 6, Mat.Wood, 0.02f);
                    b.Strut(new Vector3(0, h * 0.6f, 0), new Vector3(0.9f, h * 0.85f, 0.2f), 0.07f, Mat.Wood);
                    b.Strut(new Vector3(0, h * 0.75f, 0), new Vector3(-0.6f, h * 1.05f, -0.3f), 0.06f, Mat.Wood);
                }
                else
                {
                    int pieces = rng.Range(2, 5);
                    for (int k = 0; k < pieces; k++)
                    {
                        b.Push(new Vector3(rng.Range(-0.8f, 0.8f), 0, rng.Range(-0.8f, 0.8f)), rng.Range(0f, 90f));
                        b.BoxOn(0, -0.1f, 0, rng.Range(0.4f, 1.3f), rng.Range(0.2f, 0.6f), rng.Range(0.4f, 1.0f), k % 2 == 0 ? Mat.Concrete : Mat.ConcreteDark, 0.06f);
                        b.Pop();
                    }
                }

                b.Pop();
            }

            Ruin(b, new Vector3(-17f, Height(-17f, 27f, seed), 27f), 18f, 5.5f, rng);
            Ruin(b, new Vector3(16f, Height(16f, 30f, seed), 30f), -12f, 7.5f, rng);
            Ruin(b, new Vector3(30f, Height(30f, 15f, seed), 15f), -60f, 4.2f, rng);
            Ruin(b, new Vector3(-31f, Height(-31f, 12f, seed), 12f), 75f, 4.8f, rng);
            for (int i = 0; i < 9; i++)
            {
                float a = MeshBuilder.Deg(20f + (i * 38f));
                var pole = new Vector3((float)Math.Cos(a) * 24f, 0, (float)Math.Sin(a) * 24f);
                pole.Y = Height(pole.X, pole.Z, seed);
                float lean = rng.Range(-8f, 8f);
                b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(lean)) * Matrix4x4.CreateTranslation(pole));
                b.Frustum(Vector3.Zero, 0.15f, 0.11f, 5.5f, 6, Mat.Wood, 0.02f);
                b.Strut(new Vector3(-0.8f, 5.0f, 0), new Vector3(0.8f, 5.0f, 0), 0.1f, Mat.Wood);
                b.Pop();
            }

            Truck(b, new Vector3(-21f, Height(-21f, -9f, seed), -9f), 32f);
            Truck(b, new Vector3(23f, Height(23f, 6f, seed), 6f), -140f);

            m.Static = b.Mesh;
            return m;
        }

        /// <summary>Distance-based road mask (0..1) from the gate to the core and around the pad ring.</summary>
        public static float Road(float x, float z)
        {
            float r = (float)Math.Sqrt((x * x) + (z * z));
            float ring = (float)Math.Exp(-Math.Pow((r - SlotRadius) / 1.1f, 2)) * 0.6f;
            float gate = z < 0f ? (float)Math.Exp(-Math.Pow(x / 1.3f, 2)) * Smooth(2.5f, 4f, -z) : 0f;
            return Math.Min(1f, ring + gate);
        }

        /// <summary>A ruined building shell: broken concrete walls with window holes and rebar.</summary>
        private static void Ruin(MeshBuilder b, Vector3 p, float yaw, float height, ArtRandom rng)
        {
            b.Push(p, yaw);
            float w = rng.Range(7f, 10f);
            float d = rng.Range(5f, 7f);
            for (int side = 0; side < 3; side++)
            {
                float len = side == 1 ? w : d;
                b.Push(side == 0 ? Matrix4x4.CreateRotationY(MeshBuilder.Deg(90)) * Matrix4x4.CreateTranslation(new Vector3(-w / 2f, 0, 0))
                    : side == 1 ? Matrix4x4.CreateTranslation(new Vector3(0, 0, d / 2f))
                    : Matrix4x4.CreateRotationY(MeshBuilder.Deg(90)) * Matrix4x4.CreateTranslation(new Vector3(w / 2f, 0, 0)));
                int cols = (int)(len / 1.6f);
                for (int c = 0; c < cols; c++)
                {
                    float x = -len / 2f + ((c + 0.5f) * len / cols);
                    float hTop = height * rng.Range(0.35f, 1f) * (side == 1 ? 1f : 0.8f);
                    float colW = len / cols;
                    b.BoxOn(x, -0.2f, 0, colW * 0.98f, 1.1f, 0.4f, Mat.Concrete, 0.04f);
                    if (hTop > 2.2f)
                    {
                        b.BoxOn(x - (colW * 0.32f), 0.9f, 0, colW * 0.34f, hTop - 0.9f, 0.4f, Mat.Concrete, 0.04f);
                        b.BoxOn(x + (colW * 0.32f), 0.9f, 0, colW * 0.34f, (hTop * rng.Range(0.6f, 1f)) - 0.9f, 0.4f, Mat.ConcreteDark, 0.04f);
                        b.BoxOn(x, 2.3f, 0, colW * 0.98f, 0.4f, 0.4f, Mat.Concrete, 0.04f);
                        b.Strut(new Vector3(x, hTop, 0), new Vector3(x + 0.2f, hTop + 0.8f, 0.1f), 0.04f, Mat.Rust);
                    }
                }

                b.Pop();
            }

            for (int i = 0; i < 6; i++)
            {
                b.Push(new Vector3(rng.Range(-w / 2f, w / 2f), 0, rng.Range(-d / 2f, d / 2f)), rng.Range(0f, 90f));
                b.BoxOn(0, -0.15f, 0, rng.Range(0.8f, 2.2f), rng.Range(0.3f, 0.8f), rng.Range(0.6f, 1.6f), Mat.Concrete, 0.08f);
                b.Pop();
            }

            b.Pop();
        }

        private static void Truck(MeshBuilder b, Vector3 p, float yaw)
        {
            b.Push(p, yaw);
            b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(8f)));
            b.BoxOn(0, 0.3f, 0, 4.2f, 0.25f, 1.7f, Mat.Rust, 0.05f);
            b.BoxOn(1.5f, 0.55f, 0, 1.2f, 1.1f, 1.6f, Mat.Rust, 0.12f);
            b.BoxOn(-0.7f, 0.55f, 0, 2.6f, 0.9f, 1.7f, Mat.OliveSteel, 0.06f);
            b.Box(new Vector3(2.05f, 1.2f, 0), new Vector3(0.05f, 0.45f, 1.2f), Mat.Glass, 0.01f);
            foreach (float x in new[] { -1.4f, 1.4f })
            {
                foreach (float z in new[] { -0.85f, 0.85f })
                {
                    b.CylinderZ(new Vector3(x, 0.35f, z), 0.38f, 0.28f, 10, Mat.Rubber, 0.06f);
                }
            }

            b.Pop();
            b.Pop();
        }

        private static bool NearPad(Vector3 p, int slotCount, float radius)
        {
            for (int s = 0; s < slotCount; s++)
            {
                if (Vector3.Distance(new Vector3(p.X, 0, p.Z), SlotPosition(s, slotCount) * new Vector3(1, 0, 1)) < radius)
                {
                    return true;
                }
            }

            return new Vector2(p.X, p.Z).Length() < 4.2f;
        }

        private static float Smooth(float e0, float e1, float x)
        {
            float t = Math.Max(0f, Math.Min(1f, (x - e0) / (e1 - e0)));
            return t * t * (3f - (2f * t));
        }
    }
}
