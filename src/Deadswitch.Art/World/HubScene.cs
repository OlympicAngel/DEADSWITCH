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
        public const float MinZ = -58f;
        public const float MaxZ = 44f;

        /// <summary>Outer district wall line (SPEC-013), in front of the gate.</summary>
        public const float DistrictZ = -35.5f;

        /// <summary>Plots 6..9: the district outside the gate, on cut-and-fill pads either side of the road.</summary>
        private static readonly Vector3[] DistrictPlots =
        {
            new Vector3(-9.4f, 0, -22.8f),
            new Vector3(9.4f, 0, -22.8f),
            new Vector3(-9.4f, 0, -30.2f),
            new Vector3(9.4f, 0, -30.2f),
        };

        private static readonly Vector3[] Plots =
        {
            new Vector3(-7.6f, 0, 7.0f),
            new Vector3(7.6f, 0, 7.0f),
            new Vector3(-7.9f, 0, -0.8f),
            new Vector3(7.9f, 0, -0.8f),
            new Vector3(-7.5f, 0, -8.6f),
            new Vector3(7.5f, 0, -8.6f),
        };

        /// <summary>Plot center for a slot: six in the yard, four in the district, then along the lane.</summary>
        public static Vector3 SlotPosition(int slot, int slotCount)
        {
            if (slot < Plots.Length)
            {
                return Plots[slot];
            }

            if (slot < Plots.Length + DistrictPlots.Length)
            {
                return DistrictPlots[slot - Plots.Length];
            }

            int k = slot - Plots.Length - DistrictPlots.Length;
            return new Vector3((k % 2 == 0 ? -1 : 1) * 4.6f, 0, 3.2f - ((k / 2) * 7f));
        }

        /// <summary>Yaw (degrees) that turns a facility's front (-Z) toward the camera side of the courtyard.</summary>
        public static float SlotYaw(int slot, int slotCount)
        {
            Vector3 p = SlotPosition(slot, slotCount);
            bool district = slot >= Plots.Length && slot < Plots.Length + DistrictPlots.Length;
            Vector3 f = (district ? new Vector3(0, 0, p.Z - 5f) : new Vector3(0, 0, -34f)) - p;
            return (float)(Math.Atan2(-f.X, -f.Z) * 180.0 / Math.PI);
        }

        /// <summary>Points people walk between (courtyard paths, door, gate, plots).</summary>
        public static Vector3[] WalkPoints(int slotCount)
        {
            var pts = new List<Vector3>
            {
                new Vector3(0, 0, 9.4f), new Vector3(-1.6f, 0, 5.0f), new Vector3(1.8f, 0, 1.6f), new Vector3(-0.8f, 0, -2.8f),
                new Vector3(1.0f, 0, -7.4f), new Vector3(0, 0, -12.4f), new Vector3(-2.4f, 0, -10.0f), new Vector3(1.9f, 0, 6.2f),
            };
            for (int i = 0; i < Math.Min(slotCount, Plots.Length); i++)
            {
                Vector3 p = SlotPosition(i, slotCount);
                pts.Add(p + (Vector3.Normalize(new Vector3(-p.X, 0, -p.Z * 0.2f)) * 3.9f));
            }

            for (int i = Plots.Length; i < Math.Min(slotCount, Plots.Length + DistrictPlots.Length); i++)
            {
                Vector3 p = SlotPosition(i, slotCount);
                pts.Add(new Vector3(p.X * 0.42f, 0, p.Z + 1.5f));
            }

            return pts.ToArray();
        }

        /// <summary>Terrain height: flat courtyard, hill rising behind the bunker, banks on the sides, road out front, district pads cut flat.</summary>
        public static float Height(float x, float z, uint seed)
        {
            float pad = PadMask(x, z);
            return pad <= 0f ? RawHeight(x, z, seed) : RawHeight(x, z, seed) * (1f - pad);
        }

        /// <summary>0..1: how much the district pads flatten the ground here (pads sit at y = 0).</summary>
        public static float PadMask(float x, float z)
        {
            float m = 0f;
            foreach (Vector3 p in DistrictPlots)
            {
                float k = (1f - Smooth(3.4f, 4.6f, Math.Abs(x - p.X))) * (1f - Smooth(3.4f, 4.6f, Math.Abs(z - p.Z)));
                m = Math.Max(m, k);
            }

            return m;
        }

        private static float RawHeight(float x, float z, uint seed)
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

        /// <summary>
        /// Everything around the plots, static for the whole run: perimeter, gate checkpoint, courtyard work areas,
        /// utility poles with cables strung across the yard, side container stacks, hill, trees and the road.
        /// </summary>
        public static Model Surroundings(uint seed, int slotCount, int tier = 1)
        {
            var m = new Model();
            var b = new MeshBuilder(seed);
            var rng = new ArtRandom(seed + 13);

            Puddles(b, rng, seed, slotCount);
            Perimeter(b, m, rng);
            Checkpoint(b, m, rng, seed);
            SideStacks(b, m, rng);
            Courtyard(b, m, rng, seed);
            Utilities(b, m, slotCount);
            Hill(b, rng, seed);
            Road(b, rng, seed);
            District(b, m, new ArtRandom(seed + 71), seed, tier);

            m.Static = b.Mesh;
            m.Cones = b.Cones;
            return m;
        }

        private static void Puddles(MeshBuilder b, ArtRandom rng, uint seed, int slotCount)
        {
            for (int i = 0; i < 18; i++)
            {
                float x = rng.Range(-3.0f, 3.0f);
                float z = rng.Range(FenceZ + 1f, Core.FacadeZ - 3f);
                if (NearPlot(new Vector3(x, 0, z), slotCount, 3.6f))
                {
                    continue;
                }

                Puddle(b, new Vector3(x, Height(x, z, seed) + 0.02f, z), rng.Range(0.5f, 1.6f), rng.Range(0.4f, 1.1f), rng);
            }

            for (int i = 0; i < 7; i++)
            {
                float z = FenceZ - 3f - (i * 3.3f);
                Puddle(b, new Vector3(rng.Range(-1.6f, 1.6f), Height(0, z, seed) + 0.02f, z), rng.Range(0.8f, 1.6f), rng.Range(0.4f, 0.8f), rng);
            }
        }

        /// <summary>Front fence runs with concrete gate posts and a sign gantry, sandbagged side walls.</summary>
        private static void Perimeter(MeshBuilder b, Model m, ArtRandom rng)
        {
            foreach (int side in new[] { -1, 1 })
            {
                KitModules.Fence(b, new Vector3(side * 3.0f, 0, FenceZ), new Vector3(side * HalfWidth, 0, FenceZ), 2.1f);
                KitModules.Fence(b, new Vector3(side * HalfWidth, 0, FenceZ), new Vector3(side * HalfWidth, 0, -7.0f), 2.1f);
                Shapes.SandbagWall(b, new Vector3(side * 8.6f, 0, FenceZ + 0.6f), new Vector3(side * 11.6f, 0, FenceZ + 0.6f), 3);
                Shapes.SandbagWall(b, new Vector3(side * (HalfWidth - 0.6f), 0, FenceZ + 1f), new Vector3(side * (HalfWidth - 0.6f), 0, -7.5f), 3);

                // gate posts: concrete blocks with steel caps and lamps
                b.BoxOn(side * 2.6f, 0, FenceZ, 0.7f, 4.4f, 0.7f, Mat.Concrete, 0.08f);
                b.BoxOn(side * 2.6f, 4.4f, FenceZ, 0.8f, 0.12f, 0.8f, Mat.DarkSteel, 0.02f);
                b.Strut(new Vector3(side * 2.6f, 3.3f, FenceZ), new Vector3(side * 2.6f, 3.3f, FenceZ - 0.7f), 0.07f, Mat.DarkSteel);
                Props.Lamp(b, m, new Vector3(side * 2.6f, 3.2f, FenceZ - 0.75f), true, 1.6f);
                Shapes.Hazard(b, (side * 2.6f) - 0.35f, (side * 2.6f) + 0.35f, 0.3f, 1.3f, FenceZ - 0.36f, 4);
            }

            // sign gantry across the gate
            KitParts.Truss(b, new Vector3(-2.6f, 4.55f, FenceZ), new Vector3(2.6f, 4.55f, FenceZ), 0.45f, 0.05f, Mat.DarkSteel);
            b.Box(new Vector3(0, 4.25f, FenceZ - 0.1f), new Vector3(2.6f, 0.6f, 0.05f), Mat.OliveSteel, 0.02f);
            Props.Stencil(b, "S-17", new Vector3(-0.66f, 4.08f, FenceZ - 0.14f), 0.066f, Mat.PaintWhite);
            KitParts.Spotlight(b, m, new Vector3(1.8f, 5.2f, FenceZ - 0.2f), 180f);

            GateLeaf(b, new Vector3(-2.3f, 0, FenceZ), 0f);
            GateLeaf(b, new Vector3(2.3f, 0, FenceZ), 115f);
        }

        /// <summary>Outside the gate: watchtower, guard booth, barrier chicane, tank traps, wreck, debris.</summary>
        private static void Checkpoint(MeshBuilder b, Model m, ArtRandom rng, uint seed)
        {
            KitModules.Watchtower(b, m, new Vector3(-5.2f, Height(-5.2f, FenceZ - 2.6f, seed), FenceZ - 2.6f), 5.2f);

            // guard booth: welded plate walls under a shed roof, lit window
            Vector3 booth = new Vector3(4.8f, Height(4.8f, FenceZ - 2.2f, seed), FenceZ - 2.2f);
            b.BoxOn(booth.X, booth.Y, booth.Z, 2.0f, 2.3f, 1.8f, Mat.OliveSteel, 0.05f);
            b.Box(booth + new Vector3(-0.2f, 1.45f, -0.92f), new Vector3(1.1f, 0.6f, 0.03f), Mat.Interior, 0f);
            b.Box(booth + new Vector3(-0.2f, 1.1f, -0.98f), new Vector3(1.3f, 0.06f, 0.14f), Mat.DarkSteel, 0.01f);
            KitParts.Plate(b, booth + new Vector3(0.6f, 0.7f, -0.93f), 0.6f, 0.5f, 4f);
            KitModules.Shed(b, booth + new Vector3(0, 0, 0.1f), 2.4f, 2.2f, 2.5f, 2.7f, Mat.Rust, false);
            m.Lights.Add(new LightSpec(booth + new Vector3(-0.2f, 1.5f, -1.4f), Model.Amber, 1.2f, 4f, LightRole.Ambient));
            Shapes.SandbagWall(b, booth + new Vector3(-1.3f, 0, -1.4f), booth + new Vector3(1.2f, 0, -1.5f), 2);

            // barrier chicane and tank traps
            for (int i = 0; i < 4; i++)
            {
                float x = (i < 2 ? -1 : 1) * (2.6f + ((i % 2) * 2.4f));
                float z = FenceZ - 4.6f - ((i % 2) * 1.2f) - (i < 2 ? 0f : 2.4f);
                StripedBarrier(b, new Vector3(x, Height(x, z, seed), z), rng.Range(-8f, 8f));
            }

            foreach (Vector3 p in new[] { new Vector3(-8.6f, 0, FenceZ - 3.2f), new Vector3(-10.4f, 0, FenceZ - 5.0f), new Vector3(7.0f, 0, FenceZ - 4.6f), new Vector3(10.6f, 0, FenceZ - 3.6f), new Vector3(-6.0f, 0, FenceZ - 4.4f) })
            {
                KitModules.Hedgehog(b, new Vector3(p.X, Height(p.X, p.Z, seed), p.Z), rng.Range(0f, 90f));
            }

            KitModules.Pickup(b, new Vector3(-16.4f, Height(-16.4f, -27.5f, seed), -27.5f), 28f, Mat.SandSteel, true);
            KitModules.Debris(b, new Vector3(-15.6f, Height(-15.6f, -20.5f, seed), -20.5f), 1.4f, rng);
            KitModules.Debris(b, new Vector3(15.2f, Height(15.2f, -18.6f, seed), -18.6f), 1.0f, rng);
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = new Vector3(8.2f + (i * 0.9f), Height(8.2f, FenceZ - 2.0f, seed), FenceZ - 2.0f + ((i % 2) * 0.7f));
                for (int k = 0; k < 3 - i; k++)
                {
                    b.Frustum(p + new Vector3(0, k * 0.26f, 0), 0.42f, 0.42f, 0.26f, 12, Mat.Rubber, 0.09f);
                }
            }

            Shapes.SandbagWall(b, new Vector3(-11.4f, Height(-11.4f, FenceZ - 1.6f, seed), FenceZ - 1.6f), new Vector3(-7.6f, Height(-7.6f, FenceZ - 1.4f, seed), FenceZ - 1.4f), 2);
        }

        /// <summary>Back corners: stacked, lived-in containers behind the side walls.</summary>
        private static void SideStacks(MeshBuilder b, Model m, ArtRandom rng)
        {
            foreach (int side in new[] { -1, 1 })
            {
                b.Push(new Vector3(side * 13.8f, 0, 2.0f), side * 90f);
                KitModules.ContainerBlock(b, m, Vector3.Zero, 6.0f, side < 0 ? Mat.TarpBlue : Mat.Rust, side > 0, string.Empty, rng, 2, false);
                b.Push(new Vector3(0.5f, 2.6f, 0.15f), 4f);
                KitModules.ContainerBlock(b, m, Vector3.Zero, 6.0f, Mat.OliveSteel, false, string.Empty, rng, 1, true);
                b.Pop();
                KitModules.LeanTo(b, new Vector3(-0.8f, 0, -1.22f), 3.0f, 1.6f, 2.5f, 2.1f, Mat.Rust);
                KitModules.Barrels(b, new Vector3(-2.2f, 0, -2.6f), 3, rng);
                b.Pop();
            }
        }

        /// <summary>Purposeful work areas along both sides of the central lane (kept clear for walking).</summary>
        private static void Courtyard(MeshBuilder b, Model m, ArtRandom rng, uint seed)
        {
            Vector3 G(float x, float z) => new Vector3(x, Height(x, z, seed), z);

            // left: tool bench, crate store, gate-side pallets
            KitModules.Workbench(b, m, G(-3.3f, 1.4f), 90f);
            b.Frustum(G(-3.0f, 0.1f), 0.14f, 0.14f, 1.2f, 10, Mat.PaintRed, 0.03f);
            b.Frustum(G(-2.75f, 0.25f), 0.14f, 0.14f, 1.2f, 10, Mat.OliveSteel, 0.03f);
            KitModules.CrateStack(b, G(-3.4f, -3.8f), rng);
            KitModules.TarpPile(b, G(-3.2f, -5.4f), 1.2f, rng);
            KitModules.Pallet(b, G(-3.3f, -11.2f), 80f, 2);
            KitModules.Barrels(b, G(-4.0f, -12.6f), 3, rng);

            // right: burn barrel with benches, mobile genset feeding the lamps, parked pickup, water tank
            Vector3 barrel = G(2.9f, 2.6f);
            b.Frustum(barrel, 0.32f, 0.3f, 0.9f, 12, Mat.Rust, 0.04f);
            b.Box(barrel + new Vector3(0, 0.95f, 0), new Vector3(0.4f, 0.12f, 0.4f), Mat.Interior, 0.05f);
            m.Lights.Add(new LightSpec(barrel + new Vector3(0, 1.4f, 0), new Vector3(1f, 0.55f, 0.2f), 2.4f, 6f, LightRole.Ambient));
            foreach (float dz in new[] { -1.1f, 1.1f })
            {
                b.BoxOn(barrel.X + 0.4f, barrel.Y, barrel.Z + dz, 0.25f, 0.4f, 0.3f, Mat.Concrete, 0.04f);
                b.BoxOn(barrel.X + 1.1f, barrel.Y, barrel.Z + dz, 0.25f, 0.4f, 0.3f, Mat.Concrete, 0.04f);
                b.BoxOn(barrel.X + 0.75f, barrel.Y + 0.4f, barrel.Z + dz, 1.2f, 0.06f, 0.32f, Mat.Wood, 0.01f);
            }

            KitParts.Genset(b, G(3.4f, -3.2f), 90f);
            KitModules.Pickup(b, G(3.35f, -8.3f), 90f, Mat.OliveSteel, false);
            KitParts.TankH(b, G(3.3f, -11.6f) + new Vector3(0, 0.75f, 0), 0.45f, 1.8f, Mat.SandSteel);
            KitModules.Barrels(b, G(2.6f, -12.9f), 2, rng);
            KitModules.CrateStack(b, G(3.3f, 6.3f), rng);

            // cable runs on the ground from the bunker to the plots
            foreach (int side in new[] { -1, 1 })
            {
                for (int k = 0; k < 2; k++)
                {
                    float x = side * (2.3f + (k * 0.12f));
                    b.Strut(new Vector3(x, 0.05f, Core.FacadeZ - 2.6f), new Vector3(x + (side * 0.3f), 0.05f, FenceZ + 2.0f), 0.045f, Mat.Rubber);
                }
            }
        }

        /// <summary>
        /// Utility poles at each plot's lane-side corner, chained along both sides, strung across the lane and up to
        /// the bunker; lamps on the lane poles; one festoon of work bulbs over the yard.
        /// </summary>
        private static void Utilities(MeshBuilder b, Model m, int slotCount)
        {
            const float poleH = 6.4f;
            var left = new List<Vector3>();
            var right = new List<Vector3>();
            for (int s = 0; s < Math.Min(slotCount, Plots.Length); s++)
            {
                Vector3 p = SlotPosition(s, slotCount);
                var pole = new Vector3(p.X - (Math.Sign(p.X) * 3.75f), 0, p.Z + 2.9f);
                if (pole.Z > Core.FacadeZ - 4f)
                {
                    pole.Z = Core.FacadeZ - 4.3f;
                }
                Pole(b, pole, poleH, s % 2 == 0 ? 6f : -6f);
                (p.X < 0 ? left : right).Add(pole);
            }

            Vector3 top = new Vector3(0, poleH - 0.3f, 0);
            foreach (List<Vector3> run in new[] { left, right })
            {
                run.Sort((a, c) => c.Z.CompareTo(a.Z));
                for (int i = 0; i + 1 < run.Count; i++)
                {
                    KitParts.Cable(b, run[i] + top, run[i + 1] + top, 0.7f, 0.035f);
                    KitParts.Cable(b, run[i] + top + new Vector3(0.4f, 0, 0), run[i + 1] + top + new Vector3(0.4f, 0, 0), 0.8f, 0.025f);
                }

                if (run.Count > 0)
                {
                    float sx = Math.Sign(run[0].X);
                    KitParts.Cable(b, run[0] + top, new Vector3(sx * 4.4f, 6.4f, Core.FacadeZ - 0.4f), 0.5f, 0.04f);
                }
            }

            for (int i = 0; i < Math.Min(left.Count, right.Count); i++)
            {
                KitParts.Cable(b, left[i] + top, right[i] + top, 1.1f, 0.03f);
            }

            // festoon of work bulbs across the yard (emissive only; the burn barrel and lamps light the yard)
            if (left.Count > 1 && right.Count > 1)
            {
                Vector3 a = left[1] + new Vector3(0, 4.6f, 0);
                Vector3 c = right[1] + new Vector3(0, 4.6f, 0);
                KitParts.Cable(b, a, c, 1.4f, 0.015f);
                for (int i = 1; i < 12; i++)
                {
                    float t = i / 12f;
                    Vector3 bulb = Vector3.Lerp(a, c, t) - new Vector3(0, 1.4f * 4f * t * (1 - t), 0);
                    b.Box(bulb - new Vector3(0, 0.08f, 0), new Vector3(0.08f, 0.1f, 0.08f), Mat.LampAmber, 0.02f);
                }
            }

            // lamp posts on the lane
            foreach (Vector3 lp in new[] { new Vector3(-2.6f, 0, 5.2f), new Vector3(2.5f, 0, -1.4f), new Vector3(-2.5f, 0, -8.4f) })
            {
                b.Frustum(lp, 0.09f, 0.07f, 4.4f, 8, Mat.DarkSteel, 0.01f);
                float dir = lp.X < 0 ? 1f : -1f;
                b.Strut(lp + new Vector3(0, 4.3f, 0), lp + new Vector3(dir * 0.9f, 4.4f, 0), 0.07f, Mat.DarkSteel);
                Props.Lamp(b, m, lp + new Vector3(dir * 0.95f, 4.25f, 0), true, 2.2f);
            }
        }

        /// <summary>Wooden utility pole: crossarm, insulators, a pole-top transformer can, leaning slightly.</summary>
        private static void Pole(MeshBuilder b, Vector3 at, float h, float leanDeg)
        {
            b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(leanDeg * 0.3f)) * Matrix4x4.CreateTranslation(at));
            b.Frustum(Vector3.Zero, 0.15f, 0.11f, h, 8, Mat.Wood, 0.02f);
            b.Strut(new Vector3(-0.8f, h - 0.4f, 0), new Vector3(0.8f, h - 0.4f, 0), 0.1f, Mat.Wood);
            for (int i = 0; i < 3; i++)
            {
                b.Frustum(new Vector3(-0.6f + (i * 0.6f), h - 0.35f, 0), 0.05f, 0.04f, 0.18f, 8, Mat.PaintWhite, 0.01f);
            }

            b.Frustum(new Vector3(0, h - 2.2f, 0.28f), 0.22f, 0.22f, 0.6f, 12, Mat.DarkSteel, 0.02f);
            b.Strut(new Vector3(0, h - 1.4f, 0), new Vector3(0, 0.2f, 0.18f), 0.03f, Mat.Rubber);
            b.Pop();
        }

        /// <summary>Boulders at the foot of the slopes and pines climbing the hill and the flanks.</summary>
        private static void Hill(MeshBuilder b, ArtRandom rng, uint seed)
        {
            for (int i = 0; i < 40; i++)
            {
                float x = rng.Range(MinX + 2f, MaxX - 2f);
                float z = rng.Range(MinZ + 2f, MaxZ - 2f);
                bool inYard = Math.Abs(x) < HalfWidth + 1f && z > FenceZ - 5f && z < Core.FacadeZ + 12f;
                inYard |= Math.Abs(x) < 15.5f && z > DistrictZ - 3f && z < FenceZ;
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
        }

        /// <summary>Leaning power poles with sagging lines along the road, an old truck in the ditch.</summary>
        private static void Road(MeshBuilder b, ArtRandom rng, uint seed)
        {
            Vector3? prev = null;
            for (int i = 0; i < 4; i++)
            {
                float z = FenceZ - 6f - (i * 7f);
                var pole = new Vector3(-4.7f, Height(-4.7f, z, seed), z);
                Pole(b, pole, 6.0f, rng.Range(-7f, 7f));
                Vector3 top = pole + new Vector3(0, 5.65f, 0);
                if (prev.HasValue)
                {
                    KitParts.Cable(b, prev.Value, top, 0.6f, 0.03f);
                    KitParts.Cable(b, prev.Value + new Vector3(0.6f, 0, 0), top + new Vector3(0.6f, 0, 0), 0.7f, 0.025f);
                }

                prev = top;
            }

            Truck(b, new Vector3(17.0f, Height(17.0f, -27f, seed), -27f), 64f);
        }

        /// <summary>
        /// The district (SPEC-013). Tier 1: cleared pads outside the gate, staked out with tape and rubble, the next
        /// goal in plain view. Tier 2+: a scrap palisade and jersey wall enclose them, with a second gate, corner
        /// towers and floodlight masts. Pads always get retaining walls where the ground falls away.
        /// </summary>
        private static void District(MeshBuilder b, Model m, ArtRandom rng, uint seed, int tier)
        {
            // the tree line south of the district closes the view (every tier)
            var trees = new ArtRandom(seed + 77);
            for (int i = 0; i < 44; i++)
            {
                float x = (trees.Next() < 0.5f ? -1 : 1) * trees.Range(5.5f, MaxX - 1f);
                float z = trees.Range(MinZ + 1f, DistrictZ - 8f);
                Props.Pine(b, new Vector3(x, Height(x, z, seed) - 0.2f, z), trees.Range(4.5f, 8.5f), seed + 1500 + (uint)i);
            }

            foreach (Vector3 p in DistrictPlots)
            {
                // cut-and-fill: retaining wall on the low (front) edge
                float drop = -RawHeight(p.X, p.Z - 4.3f, seed);
                if (drop > 0.35f)
                {
                    KitModules.RetainingWall(b, new Vector3(p.X, -drop, p.Z - 3.9f), 7.4f, drop + 0.15f, rng);
                }

                if (tier >= 2)
                {
                    continue;
                }

                // staked lot: corner stakes with hazard tape, rubble, a survey flag
                var corners = new[]
                {
                    p + new Vector3(-2.9f, 0, -2.9f), p + new Vector3(2.9f, 0, -2.9f), p + new Vector3(2.9f, 0, 2.9f), p + new Vector3(-2.9f, 0, 2.9f),
                };
                for (int i = 0; i < 4; i++)
                {
                    Vector3 a = corners[i];
                    Vector3 c = corners[(i + 1) % 4];
                    b.Strut(a, a + new Vector3(rng.Range(-0.08f, 0.08f), 1.05f, 0), 0.05f, Mat.Wood);
                    b.Box(a + new Vector3(0, 1.0f, 0), new Vector3(0.1f, 0.12f, 0.1f), Mat.PaintRed, 0.01f);
                    if (rng.Next() < 0.8f)
                    {
                        KitParts.Cable(b, a + new Vector3(0, 0.9f, 0), c + new Vector3(0, 0.9f, 0), 0.25f, 0.018f, i % 2 == 0 ? Mat.PaintRed : Mat.PaintWhite);
                    }
                }

                KitModules.Debris(b, p + new Vector3(rng.Range(-1.2f, 1.2f), 0, rng.Range(-1.2f, 1.2f)), rng.Range(0.8f, 1.3f), rng);
                Props.Boulder(b, p + new Vector3(rng.Range(-1.8f, 1.8f), 0, rng.Range(-1.8f, 1.8f)), new Vector3(0.9f, 0.5f, 0.8f), seed + 1200 + (uint)rng.Range(0f, 100f), Mat.Rock);
                Vector3 flag = p + new Vector3(Math.Sign(p.X) * -2.2f, 0, -2.2f);
                b.Strut(flag, flag + new Vector3(0, 1.7f, 0), 0.03f, Mat.DarkSteel);
                b.Box(flag + new Vector3(0.18f, 1.55f, 0), new Vector3(0.34f, 0.22f, 0.01f), Mat.PaintRed, 0f);
            }

            if (tier < 2)
            {
                return;
            }

            const float side = 13.8f;
            Vector3 G(float x, float z) => new Vector3(x, Height(x, z, seed), z);

            // side palisades from the old fence to the outer wall, sandbags at the foot on the inside
            foreach (int s in new[] { -1, 1 })
            {
                Palisade(b, G(s * side, FenceZ - 0.4f), G(s * side, DistrictZ), s < 0, rng, seed);
                Shapes.SandbagWall(b, G(s * (side - 0.8f), -19.5f), G(s * (side - 0.8f), -26.5f), 2);
            }

            // front wall: palisade behind a line of hazard-striped jerseys, open at the road
            foreach (int s in new[] { -1, 1 })
            {
                Palisade(b, G(s * 3.4f, DistrictZ), G(s * side, DistrictZ), s > 0, rng, seed);
                for (float x = 4.6f; x < side - 0.6f; x += 2.1f)
                {
                    StripedBarrier(b, G(s * x, DistrictZ - 0.8f), rng.Range(-4f, 4f));
                }

                // gate pillars: stacked concrete blocks with lamps and hazard paint
                float gx = s * 3.0f;
                Vector3 gp = G(gx, DistrictZ);
                b.BoxOn(gp.X, gp.Y, gp.Z, 1.0f, 2.4f, 1.0f, Mat.Concrete, 0.08f);
                b.BoxOn(gp.X, gp.Y + 2.4f, gp.Z, 0.8f, 2.6f, 0.8f, Mat.ConcreteDark, 0.06f);
                b.BoxOn(gp.X, gp.Y + 5.0f, gp.Z, 0.9f, 0.14f, 0.9f, Mat.DarkSteel, 0.02f);
                Shapes.Hazard(b, gp.X - 0.5f, gp.X + 0.5f, gp.Y + 0.3f, gp.Y + 1.6f, gp.Z - 0.51f, 4);
                Props.Lamp(b, m, gp + new Vector3(0, 3.6f, -0.6f), true, 1.8f);
                GateLeaf(b, gp + new Vector3(-s * 0.5f, 0, 0.1f), s < 0 ? 100f : 72f);

                // corner tower and floodlight mast aimed into the district
                KitModules.Watchtower(b, m, G(s * (side - 1.6f), DistrictZ + 1.9f), 5.6f);
                Vector3 mast = G(s * (side - 0.4f), -17.8f);
                KitParts.Mast(b, m, mast, 8.5f, true);
                KitParts.Spotlight(b, m, mast + new Vector3(-s * 0.4f, 8.2f, -0.3f), s * 140f);
                KitParts.Spotlight(b, m, mast + new Vector3(-s * 0.4f, 7.8f, 0.3f), s * 110f);
            }

            // sign gantry over the outer gate
            float gy = Height(0, DistrictZ, seed);
            KitParts.Truss(b, new Vector3(-3.0f, gy + 5.2f, DistrictZ), new Vector3(3.0f, gy + 5.2f, DistrictZ), 0.5f, 0.05f, Mat.DarkSteel);
            b.Box(new Vector3(0, gy + 4.85f, DistrictZ - 0.12f), new Vector3(3.4f, 0.62f, 0.05f), Mat.OliveSteel, 0.02f);
            Props.Stencil(b, "DISTRICT", new Vector3(-1.48f, gy + 4.66f, DistrictZ - 0.16f), 0.066f, Mat.PaintWhite);
            KitParts.Spotlight(b, m, new Vector3(-2.2f, gy + 5.8f, DistrictZ - 0.2f), 180f);

            // MG nest beside the road and tank traps beyond the wall
            Vector3 nest = G(-5.6f, DistrictZ - 3.2f);
            Shapes.SandbagRing(b, nest, 1.3f, 14, 3, 20f, 70f);
            b.Strut(nest + new Vector3(0, 0.9f, 0), nest + new Vector3(0.2f, 1.0f, -1.2f), 0.06f, Mat.DarkSteel);
            for (int i = 0; i < 5; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * rng.Range(5.5f, 12f);
                float z = DistrictZ - rng.Range(3.5f, 6f);
                KitModules.Hedgehog(b, G(x, z), rng.Range(0f, 90f));
            }

            // power from the road poles into the district
            for (int i = 0; i < DistrictPlots.Length; i++)
            {
                Vector3 p = DistrictPlots[i];
                Vector3 box = new Vector3(p.X - (Math.Sign(p.X) * 3.5f), 0, p.Z + 2.2f);
                KitParts.UtilityBox(b, box + new Vector3(0, 0.6f, 0));
                b.Strut(box + new Vector3(0, 0.05f, 0), new Vector3(Math.Sign(p.X) * 1.6f, 0.05f, p.Z + 2.2f), 0.04f, Mat.Rubber);
            }
        }

        /// <summary>
        /// Scrap palisade from a to c: steel posts, corrugated sheets of uneven height (some patched, some leaning),
        /// a top rail and raking braces on the inside. <paramref name="flip"/> turns the corrugated face the other way.
        /// </summary>
        private static void Palisade(MeshBuilder b, Vector3 a, Vector3 c, bool flip, ArtRandom rng, uint seed)
        {
            float len = Vector3.Distance(new Vector3(a.X, 0, a.Z), new Vector3(c.X, 0, c.Z));
            int n = Math.Max(1, (int)(len / 1.4f));
            Vector3 d = Vector3.Normalize(new Vector3(c.X - a.X, 0, c.Z - a.Z));
            float yaw = (float)(Math.Atan2(-d.Z, d.X) * 180.0 / Math.PI) + (flip ? 180f : 0f);
            Vector3 inside = Vector3.Cross(Vector3.UnitY, d) * (flip ? -1f : 1f);
            for (int i = 0; i < n; i++)
            {
                Vector3 p0 = Vector3.Lerp(a, c, i / (float)n);
                Vector3 p1 = Vector3.Lerp(a, c, (i + 1) / (float)n);
                Vector3 mid = (p0 + p1) * 0.5f;
                mid.Y = Height(mid.X, mid.Z, seed);
                float seg = Vector3.Distance(p0, p1);
                float h = rng.Range(2.3f, 3.2f);
                float lean = rng.Next() < 0.2f ? rng.Range(-6f, 6f) : 0f;

                Vector3 g0 = new Vector3(p0.X, Height(p0.X, p0.Z, seed), p0.Z);
                b.Strut(g0 - new Vector3(0, 0.3f, 0), g0 + new Vector3(0, 3.3f, 0), 0.07f, Mat.DarkSteel);

                b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(lean)) * Matrix4x4.CreateRotationY(MeshBuilder.Deg(yaw)) * Matrix4x4.CreateTranslation(mid));
                Mat sheet = rng.Next() < 0.3f ? Mat.OliveSteel : (rng.Next() < 0.5f ? Mat.Rust : Mat.SandSteel);
                b.Corrugated(-seg * 0.5f, seg * 0.5f, -0.2f, h, -0.03f, sheet, 0.24f, 0.05f);
                b.Box(new Vector3(0, (h * 0.5f) - 0.1f, 0.02f), new Vector3(seg, h + 0.2f, 0.03f), Mat.Rust, 0f);
                if (rng.Next() < 0.25f)
                {
                    KitParts.Plate(b, new Vector3(rng.Range(-0.3f, 0.3f), rng.Range(0.6f, 1.6f), -0.1f), 0.7f, 0.6f, rng.Range(-12f, 12f), Mat.DarkSteel);
                }

                b.Pop();

                if (i % 2 == 0)
                {
                    b.Strut(g0 + new Vector3(0, 2.4f, 0), g0 + (inside * 1.3f), 0.05f, Mat.DarkSteel);
                }
            }

            Vector3 rail = new Vector3(0, 3.1f, 0);
            b.Strut(new Vector3(a.X, Height(a.X, a.Z, seed), a.Z) + rail, new Vector3(c.X, Height(c.X, c.Z, seed), c.Z) + rail, 0.035f, Mat.DarkSteel);
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

        /// <summary>Concrete jersey barrier with faded red-white hazard paint and a lifting loop.</summary>
        private static void StripedBarrier(MeshBuilder b, Vector3 p, float yaw)
        {
            b.Push(p, yaw);
            b.BoxOn(0, 0, 0, 2.0f, 0.3f, 0.62f, Mat.Concrete, 0.04f);
            b.BoxOn(0, 0.3f, 0, 1.97f, 0.14f, 0.44f, Mat.Concrete, 0.04f);
            b.BoxOn(0, 0.44f, 0, 1.96f, 0.4f, 0.24f, Mat.Concrete, 0.03f);
            for (int i = 0; i < 4; i++)
            {
                b.Box(new Vector3(-0.72f + (i * 0.48f), 0.55f, -0.13f), new Vector3(0.24f, 0.3f, 0.02f), i % 2 == 0 ? Mat.PaintRed : Mat.PaintWhite, 0f);
            }

            b.Strut(new Vector3(-0.15f, 0.8f, 0), new Vector3(0.15f, 0.8f, 0), 0.03f, Mat.Rust);
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
