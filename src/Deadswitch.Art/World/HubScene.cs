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
    /// The compound (SPEC-003 rule 3): a muddy courtyard against a ruined bunker set into a wooded hill, facility
    /// plots on both sides, a fence and gate toward the camera. Unity and the preview place facility models at
    /// <see cref="SlotPosition"/> rotated by <see cref="SlotYaw"/>. World space is Unity's (camera on -Z).
    /// </summary>
    public static class HubScene
    {
        public const float FenceZ = -13.6f;
        public const float HalfWidth = 12.0f;
        public const float MinX = -40f;
        public const float MaxX = 40f;
        public const float MinZ = -42f;
        public const float MaxZ = 44f;

        private static readonly Vector3[] Plots =
        {
            new Vector3(-7.6f, 0, 7.0f),
            new Vector3(7.6f, 0, 7.0f),
            new Vector3(-7.9f, 0, -0.8f),
            new Vector3(7.9f, 0, -0.8f),
            new Vector3(-7.5f, 0, -8.6f),
            new Vector3(7.5f, 0, -8.6f),
        };

        /// <summary>Plot center for a slot. Beyond the six designed plots, extra slots continue along the sides.</summary>
        public static Vector3 SlotPosition(int slot, int slotCount)
        {
            if (slot < Plots.Length)
            {
                return Plots[slot];
            }

            int k = slot - Plots.Length;
            return new Vector3((k % 2 == 0 ? -1 : 1) * 4.6f, 0, 3.2f - ((k / 2) * 7f));
        }

        /// <summary>Yaw (degrees) that turns a facility's front (-Z) toward the camera side of the courtyard.</summary>
        public static float SlotYaw(int slot, int slotCount)
        {
            Vector3 p = SlotPosition(slot, slotCount);
            Vector3 f = new Vector3(0, 0, -34f) - p;
            return (float)(Math.Atan2(-f.X, -f.Z) * 180.0 / Math.PI);
        }

        /// <summary>Points people walk between (courtyard paths, door, gate, plots).</summary>
        public static Vector3[] WalkPoints(int slotCount)
        {
            var pts = new List<Vector3>
            {
                new Vector3(0, 0, 9.4f), new Vector3(-1.6f, 0, 5.0f), new Vector3(1.8f, 0, 1.6f), new Vector3(-0.8f, 0, -2.8f),
                new Vector3(1.0f, 0, -7.4f), new Vector3(0, 0, -12.4f), new Vector3(-2.4f, 0, -10.0f), new Vector3(2.6f, 0, 6.2f),
            };
            for (int i = 0; i < Math.Min(slotCount, Plots.Length); i++)
            {
                Vector3 p = SlotPosition(i, slotCount);
                pts.Add(p + (Vector3.Normalize(new Vector3(-p.X, 0, -p.Z * 0.2f)) * 3.9f));
            }

            return pts.ToArray();
        }

        /// <summary>Terrain height: flat courtyard, hill rising behind the bunker, banks on the sides, road out front.</summary>
        public static float Height(float x, float z, uint seed)
        {
            float n = (Noise.Fbm(x * 0.08f, z * 0.08f, seed + 5, 4) - 0.5f) * 2.6f;
            float back = Smooth(Core.FacadeZ + 2f, 30f, z);
            float sides = Smooth(HalfWidth + 0.5f, 28f, Math.Abs(x));
            float front = Smooth(FenceZ - 1f, -34f, z);
            float road = (float)Math.Exp(-Math.Pow(x / 3.2f, 2)) * front;
            float rough = Math.Max(back, Math.Max(sides, front * 0.6f));
            float h = (back * 15f) + (sides * 6.5f) - (front * 1.4f) + (n * rough) - (road * 0.5f);
            float yard = (Noise.Fbm(x * 0.35f, z * 0.35f, seed + 9, 2) - 0.5f) * 0.1f;
            return h + (yard * (1 - rough));
        }

        public static MeshData Terrain(uint seed)
        {
            var b = new MeshBuilder(seed);
            const float step = 1.0f;
            int nx = (int)((MaxX - MinX) / step);
            int nz = (int)((MaxZ - MinZ) / step);
            for (int j = 0; j < nz; j++)
            {
                for (int i = 0; i < nx; i++)
                {
                    float x0 = MinX + (i * step);
                    float z0 = MinZ + (j * step);
                    float x1 = x0 + step;
                    float z1 = z0 + step;
                    var p00 = new Vector3(x0, Height(x0, z0, seed), z0);
                    var p10 = new Vector3(x1, Height(x1, z0, seed), z0);
                    var p11 = new Vector3(x1, Height(x1, z1, seed), z1);
                    var p01 = new Vector3(x0, Height(x0, z1, seed), z1);
                    var below = new Vector3((x0 + x1) * 0.5f, -80f, (z0 + z1) * 0.5f);
                    b.FaceTinted(below, Mat.Ground, new[] { p00, p10, p11 }, new[] { Tone(p00), Tone(p10), Tone(p11) });
                    b.FaceTinted(below, Mat.Ground, new[] { p00, p11, p01 }, new[] { Tone(p00), Tone(p11), Tone(p01) });
                }
            }

            float Tone(Vector3 p)
            {
                float mud = Noise.Fbm(p.X * 0.13f, p.Z * 0.13f, seed + 31, 4);
                float grit = Noise.Value(p.X * 1.4f, p.Z * 1.4f, seed + 3);
                float slope = Math.Min(1f, Math.Abs(Height(p.X + 0.5f, p.Z, seed) - Height(p.X - 0.5f, p.Z, seed)) + Math.Abs(Height(p.X, p.Z + 0.5f, seed) - Height(p.X, p.Z - 0.5f, seed)));
                float yard = Math.Abs(p.X) < HalfWidth && p.Z < Core.FacadeZ && p.Z > FenceZ ? 1f : 0f;
                float wet = Smooth(0.52f, 0.7f, Noise.Fbm(p.X * 0.09f, p.Z * 0.09f, seed + 41, 3));
                float tone = 0.8f + ((mud - 0.5f) * 1.1f) + ((grit - 0.5f) * 0.22f) - (wet * 0.22f);
                tone += slope * 0.25f;
                tone -= yard * 0.12f;
                tone += Track(p.X, p.Z) * 0.18f;
                return Math.Max(0.3f, tone);
            }

            return b.Mesh;
        }

        /// <summary>Fence, gate, walls, lamps, clutter, puddles, rocks, trees. Static for the whole run.</summary>
        public static Model Surroundings(uint seed, int slotCount)
        {
            var m = new Model();
            var b = new MeshBuilder(seed);
            var rng = new ArtRandom(seed + 13);

            // Puddles: dark, glossy, catching the lamps.
            for (int i = 0; i < 16; i++)
            {
                float x = rng.Range(-3.4f, 3.4f);
                float z = rng.Range(FenceZ + 1f, Core.FacadeZ - 3f);
                if (NearPlot(new Vector3(x, 0, z), slotCount, 3.6f))
                {
                    continue;
                }

                Puddle(b, new Vector3(x, Height(x, z, seed) + 0.02f, z), rng.Range(0.6f, 1.8f), rng.Range(0.4f, 1.1f), rng);
            }

            for (int i = 0; i < 6; i++)
            {
                float z = FenceZ - 3f - (i * 3.3f);
                Puddle(b, new Vector3(rng.Range(-1.6f, 1.6f), Height(0, z, seed) + 0.02f, z), rng.Range(0.8f, 1.6f), rng.Range(0.4f, 0.8f), rng);
            }

            // Front fence with a gate (one leaf swung open), lamps on the gate posts.
            for (float x = -HalfWidth; x <= HalfWidth + 0.01f; x += 1.7f)
            {
                if (Math.Abs(x) < 2.4f)
                {
                    continue;
                }

                b.Strut(new Vector3(x, 0, FenceZ), new Vector3(x, 2.1f, FenceZ), 0.1f, Mat.DarkSteel);
            }

            foreach (int side in new[] { -1, 1 })
            {
                float xa = side * 2.6f;
                float xb = side * HalfWidth;
                for (float y = 0.3f; y < 2.1f; y += 0.6f)
                {
                    b.Strut(new Vector3(xa, y, FenceZ), new Vector3(xb, y, FenceZ), 0.03f, Mat.DarkSteel);
                }

                for (float x = Math.Min(xa, xb); x < Math.Max(xa, xb); x += 0.35f)
                {
                    b.Strut(new Vector3(x, 0.15f, FenceZ), new Vector3(x + 0.35f, 2.0f, FenceZ), 0.015f, Mat.DarkSteel);
                }

                Shapes.SandbagWall(b, new Vector3(side * 9.2f, 0, FenceZ + 0.6f), new Vector3(side * 11.6f, 0, FenceZ + 0.6f), 2);
                b.BoxOn(side * 2.5f, 0, FenceZ, 0.5f, 3.2f, 0.5f, Mat.Concrete, 0.06f);
                b.Strut(new Vector3(side * 2.5f, 3.3f, FenceZ), new Vector3(side * 2.5f, 3.3f, FenceZ - 0.7f), 0.07f, Mat.DarkSteel);
                Props.Lamp(b, m, new Vector3(side * 2.5f, 3.2f, FenceZ - 0.75f), true, 1.6f);
            }

            GateLeaf(b, new Vector3(-2.3f, 0, FenceZ), 0f);
            GateLeaf(b, new Vector3(2.3f, 0, FenceZ), 115f);

            // Striped barriers and tires outside the gate.
            for (int i = 0; i < 4; i++)
            {
                float x = (i < 2 ? -1 : 1) * (4.5f + ((i % 2) * 2.2f));
                float z = FenceZ - 2.4f - (i % 2);
                StripedBarrier(b, new Vector3(x, Height(x, z, seed), z), rng.Range(-12f, 12f));
            }

            for (int i = 0; i < 2; i++)
            {
                Vector3 p = new Vector3(8.5f + i, Height(8.5f, FenceZ - 3.5f, seed), FenceZ - 3.5f + (i * 0.8f));
                b.Frustum(p, 0.42f, 0.42f, 0.26f, 10, Mat.Rubber, 0.09f);
                b.Frustum(p + new Vector3(0, 0.26f, 0), 0.42f, 0.42f, 0.26f, 10, Mat.Rubber, 0.09f);
            }

            // Side walls: sandbags and stacked spare containers in the back corners.
            foreach (int side in new[] { -1, 1 })
            {
                Shapes.SandbagWall(b, new Vector3(side * HalfWidth, 0, FenceZ + 1f), new Vector3(side * HalfWidth, 0, -7.5f), 3);
                b.Push(new Vector3(side * 13.6f, 0, 2.0f), side * 90f);
                Props.Container(b, m, Vector3.Zero, 6.0f, 2.6f, 2.44f, side < 0 ? Mat.TarpBlue : Mat.Rust, false, seed + 40);
                Props.Container(b, m, new Vector3(0.4f, 2.6f, 0.1f), 6.0f, 2.6f, 2.44f, Mat.OliveSteel, false, seed + 41);
                b.Pop();
            }

            // Courtyard: lamp posts, a burn barrel, clutter, cable runs to the plots.
            foreach (Vector3 lp in new[] { new Vector3(-3.6f, 0, 3.4f), new Vector3(3.7f, 0, -4.6f), new Vector3(-3.8f, 0, -11.6f) })
            {
                b.Frustum(lp, 0.09f, 0.07f, 4.2f, 8, Mat.DarkSteel, 0.01f);
                b.Strut(lp + new Vector3(0, 4.1f, 0), lp + new Vector3(0.9f, 4.2f, 0), 0.07f, Mat.DarkSteel);
                Props.Lamp(b, m, lp + new Vector3(0.95f, 4.05f, 0), true, 2.2f);
            }

            Vector3 barrel = new Vector3(2.2f, 0, 2.6f);
            b.Frustum(barrel, 0.32f, 0.3f, 0.9f, 10, Mat.Rust, 0.04f);
            b.Box(barrel + new Vector3(0, 0.95f, 0), new Vector3(0.4f, 0.12f, 0.4f), Mat.Interior, 0.05f);
            m.Lights.Add(new LightSpec(barrel + new Vector3(0, 1.4f, 0), new Vector3(1f, 0.55f, 0.2f), 2.4f, 6f, LightRole.Ambient));

            for (int i = 0; i < 24; i++)
            {
                float x = rng.Range(-11f, 11f);
                float z = rng.Range(FenceZ + 1.2f, Core.FacadeZ - 2.6f);
                var p = new Vector3(x, Height(x, z, seed), z);
                if (NearPlot(p, slotCount, 3.6f) || Math.Abs(x) < 3.2f)
                {
                    continue;
                }

                b.Push(p, rng.Range(0f, 360f));
                switch (i % 3)
                {
                    case 0:
                        b.BoxOn(0, 0, 0, 0.8f, 0.6f, 0.8f, Mat.Wood, 0.05f);
                        b.BoxOn(0.05f, 0.6f, 0, 0.6f, 0.45f, 0.6f, Mat.Wood, 0.05f);
                        break;
                    case 1:
                        b.Frustum(Vector3.Zero, 0.29f, 0.29f, 0.88f, 10, rng.Next() < 0.5f ? Mat.PaintRed : Mat.OliveSteel, 0.04f);
                        break;
                    default:
                        b.BoxOn(0, 0, 0, 1.1f, 0.35f, 0.7f, Mat.OliveSteel, 0.04f);
                        break;
                }

                b.Pop();
            }

            // Work clutter along the walls: pallets, sandbag piles, tarped stacks, a work light.
            foreach (Vector3 c in new[] { new Vector3(-3.2f, 0, -5.6f), new Vector3(3.4f, 0, 4.6f), new Vector3(-5.4f, 0, 10.4f), new Vector3(5.6f, 0, 10.2f), new Vector3(-3.4f, 0, -12.6f), new Vector3(3.3f, 0, -12.2f) })
            {
                b.Push(c, rng.Range(-20f, 20f));
                b.BoxOn(0, 0, 0, 1.2f, 0.14f, 1.0f, Mat.Wood, 0.02f);
                b.BoxOn(0, 0.14f, 0, 1.1f, 0.7f, 0.9f, rng.Next() < 0.5f ? Mat.TarpBlue : Mat.Tarp, 0.12f);
                b.BoxOn(1.1f, 0, 0.2f, 0.7f, 0.5f, 0.6f, Mat.Wood, 0.05f);
                Shapes.SandbagWall(b, new Vector3(-0.8f, 0, 0.9f), new Vector3(0.6f, 0, 1.2f), 2);
                b.Pop();
            }

            for (int i = 0; i < 5; i++)
            {
                float z = rng.Range(FenceZ + 1.5f, Core.FacadeZ - 3f);
                float x = (rng.Next() < 0.5f ? -1 : 1) * rng.Range(0.5f, 1.4f);
                Puddle(b, new Vector3(x, Height(x, z, seed) + 0.025f, z), rng.Range(0.3f, 0.7f), rng.Range(0.8f, 1.6f), rng);
            }

            for (int s = 0; s < Math.Min(slotCount, Plots.Length); s++)
            {
                Vector3 p = SlotPosition(s, slotCount);
                Vector3 from = new Vector3(Math.Sign(p.X) * 1.4f, 0.06f, Core.FacadeZ - 2.6f);
                b.Strut(from, new Vector3(p.X - (Math.Sign(p.X) * 3.3f), 0.04f, p.Z), 0.045f, Mat.Rubber);
            }

            // Hill: boulders at the foot of the slopes and pines climbing behind the bunker.
            for (int i = 0; i < 40; i++)
            {
                float x = rng.Range(MinX + 2f, MaxX - 2f);
                float z = rng.Range(MinZ + 2f, MaxZ - 2f);
                bool inYard = Math.Abs(x) < HalfWidth + 1f && z > FenceZ - 5f && z < Core.FacadeZ + 12f;
                if (inYard)
                {
                    continue;
                }

                var p = new Vector3(x, Height(x, z, seed), z);
                Props.Boulder(b, p, new Vector3(rng.Range(0.6f, 2.0f), rng.Range(0.5f, 1.4f), rng.Range(0.6f, 1.8f)), seed + 300 + (uint)i);
            }

            for (int i = 0; i < 120; i++)
            {
                float x = rng.Range(MinX + 1f, MaxX - 1f);
                float z = rng.Range(Core.FacadeZ + 6f, MaxZ - 1f);
                if (Math.Abs(x) < 15f && z < Core.FacadeZ + 12f)
                {
                    continue;
                }

                Props.Pine(b, new Vector3(x, Height(x, z, seed) - 0.2f, z), rng.Range(4.5f, 9f), seed + 500 + (uint)i);
            }

            for (int i = 0; i < 46; i++)
            {
                float x = (rng.Next() < 0.5f ? -1 : 1) * rng.Range(HalfWidth + 4f, MaxX - 1f);
                float z = rng.Range(MinZ + 4f, Core.FacadeZ + 8f);
                Props.Pine(b, new Vector3(x, Height(x, z, seed) - 0.2f, z), rng.Range(4f, 8f), seed + 800 + (uint)i);
            }

            // Leaning power poles along the road.
            for (int i = 0; i < 4; i++)
            {
                float z = FenceZ - 6f - (i * 7f);
                var pole = new Vector3(-6.5f, Height(-6.5f, z, seed), z);
                b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(rng.Range(-7f, 7f))) * Matrix4x4.CreateTranslation(pole));
                b.Frustum(Vector3.Zero, 0.16f, 0.12f, 6f, 6, Mat.Wood, 0.02f);
                b.Strut(new Vector3(-0.9f, 5.5f, 0), new Vector3(0.9f, 5.5f, 0), 0.1f, Mat.Wood);
                b.Pop();
            }

            Truck(b, new Vector3(8.5f, Height(8.5f, -24f, seed), -24f), 64f);

            m.Static = b.Mesh;
            m.Cones = b.Cones;
            return m;
        }

        /// <summary>Lighter tire tracks: gate to the door and loops around the plots (0..1).</summary>
        public static float Track(float x, float z)
        {
            float main = (float)Math.Exp(-Math.Pow((Math.Abs(x) - 0.9f) / 0.45f, 2)) * (z < Core.FacadeZ - 2f ? 1f : 0f);
            float loop = 0f;
            return Math.Min(1f, main + loop);
        }

        private static void Puddle(MeshBuilder b, Vector3 c, float rx, float rz, ArtRandom rng)
        {
            const int n = 10;
            var pts = new Vector3[n];
            float rot = rng.Range(0f, 6.28f);
            for (int i = 0; i < n; i++)
            {
                float a = rot + (i * 6.2832f / n);
                float k = rng.Range(0.75f, 1.15f);
                pts[i] = c + new Vector3((float)Math.Cos(a) * rx * k, 0, (float)Math.Sin(a) * rz * k);
            }

            b.Face(c - new Vector3(0, 1f, 0), Mat.Water, 1f, pts);
        }

        private static void GateLeaf(MeshBuilder b, Vector3 hinge, float openDeg)
        {
            float dir = hinge.X < 0 ? 1f : -1f;
            b.Push(Matrix4x4.CreateRotationY(MeshBuilder.Deg(openDeg * -dir)) * Matrix4x4.CreateTranslation(hinge));
            b.Strut(new Vector3(0, 0.1f, 0), new Vector3(0, 2.3f, 0), 0.09f, Mat.DarkSteel);
            b.Strut(new Vector3(dir * 2.2f, 0.1f, 0), new Vector3(dir * 2.2f, 2.3f, 0), 0.09f, Mat.DarkSteel);
            b.Strut(new Vector3(0, 2.25f, 0), new Vector3(dir * 2.2f, 2.25f, 0), 0.08f, Mat.DarkSteel);
            b.Strut(new Vector3(0, 0.15f, 0), new Vector3(dir * 2.2f, 0.15f, 0), 0.08f, Mat.DarkSteel);
            b.Strut(new Vector3(0, 0.15f, 0), new Vector3(dir * 2.2f, 2.25f, 0), 0.06f, Mat.DarkSteel);
            for (float x = 0.3f; x < 2.2f; x += 0.3f)
            {
                b.Strut(new Vector3(dir * x, 0.15f, 0), new Vector3(dir * x, 2.25f, 0), 0.03f, Mat.Rust);
            }

            b.Pop();
        }

        private static void StripedBarrier(MeshBuilder b, Vector3 p, float yaw)
        {
            b.Push(p, yaw);
            for (int i = 0; i < 4; i++)
            {
                b.BoxOn(-0.75f + (i * 0.5f), 0, 0, 0.5f, 0.82f, 0.45f, i % 2 == 0 ? Mat.PaintRed : Mat.PaintWhite, 0.06f);
            }

            b.BoxOn(0, 0, 0, 2.0f, 0.22f, 0.7f, Mat.Concrete, 0.06f);
            b.Pop();
        }

        private static void Truck(MeshBuilder b, Vector3 p, float yaw)
        {
            b.Push(p, yaw);
            b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(6f)));
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

        private static bool NearPlot(Vector3 p, int slotCount, float radius)
        {
            for (int s = 0; s < slotCount; s++)
            {
                Vector3 c = SlotPosition(s, slotCount);
                if (Math.Abs(p.X - c.X) < radius && Math.Abs(p.Z - c.Z) < radius)
                {
                    return true;
                }
            }

            return Math.Abs(p.X) < 6f && p.Z > Core.FacadeZ - 4.5f;
        }

        private static float Smooth(float e0, float e1, float x)
        {
            float t = Math.Max(0f, Math.Min(1f, (x - e0) / (e1 - e0)));
            return t * t * (3f - (2f * t));
        }
    }
}
