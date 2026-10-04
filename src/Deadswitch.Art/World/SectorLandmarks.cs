using System;
using System.Numerics;
using Deadswitch.Art.Geometry;
using Deadswitch.Art.Models;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Art.World
{
    /// <summary>
    /// What stands on the sector map (SPEC-033 rule 2): one kit landmark per site kind (built life-size, placed at
    /// <see cref="SectorScene.LandmarkScale"/>), the Hub, the hazard zones' ground treatments and the dressing
    /// between them (broken highway, pylons, collapsed blocks, rocks, dead trees). Only beacons keep point lights.
    /// </summary>
    public static class SectorLandmarks
    {
        /// <summary>Landmark for a site, in local space (origin on the site's ground, scale applied).</summary>
        public static Model Site(int site, uint seed)
        {
            SiteDef d = WorldSystem.Sites[site];
            var m = new Model();
            var b = new MeshBuilder(seed + (uint)(site * 7919)) { AoHeight = 1.2f * SectorScene.LandmarkScale };
            var rng = new ArtRandom(seed ^ (uint)(site * 104729));
            // hazard zones are ground features at full size; built sites are kit landmarks at map scale
            bool wild = HazardSystem.Wild(d.Kind);
            b.Push(Vector3.Zero, wild ? 0f : rng.Range(-25f, 25f), wild ? 1f : SectorScene.LandmarkScale);
            bool strong = d.Defense >= 100;
            switch (d.Kind)
            {
                case SiteKind.Outpost:
                    Outpost(b, m, rng, d.Owner, strong);
                    break;
                case SiteKind.DataCenter:
                    DataCenter(b, m, rng, d.Owner, strong);
                    break;
                case SiteKind.Ruins:
                    Ruins(b, rng, 7f, 8);
                    break;
                case SiteKind.Convoy:
                    Convoy(b, m, rng, d.Owner);
                    break;
                case SiteKind.Radiation:
                    Crater(b, m, rng, SectorScene.SitePosition(site, seed), seed);
                    break;
                case SiteKind.Plague:
                    Quarantine(b, m, rng);
                    break;
                default:
                    WreckField(b, m, rng, seed + (uint)site);
                    break;
            }

            b.Pop();
            m.Static = b.Mesh;
            KeepBeacons(m);
            return m;
        }

        /// <summary>Kit pieces bring their own small lamps; on the map only beacons (range 9 m and up) light the ground.</summary>
        private static void KeepBeacons(Model m)
        {
            m.Lights.RemoveAll(l => l.Intensity <= 0f || l.Range < 9f);
        }

        /// <summary>The Hub: a sandbagged bunker mound with its phosphor beacon mast.</summary>
        public static Model Hub(uint seed)
        {
            var m = new Model();
            var b = new MeshBuilder(seed + 31) { AoHeight = 1.2f * SectorScene.LandmarkScale };
            var rng = new ArtRandom(seed + 37);
            b.Push(Vector3.Zero, 0f, SectorScene.LandmarkScale);
            Shapes.SandbagRing(b, Vector3.Zero, 9f, 44, 2, 250f, 40f);
            Shapes.ArchX(b, new Vector3(0, 0, 2f), 3.2f, 8f, 12, Mat.Concrete, Mat.ConcreteDark);
            b.BoxOn(0, 0, -1.9f, 2.2f, 2.4f, 0.3f, Mat.Portal, 0.02f);
            KitModules.ContainerBlock(b, m, new Vector3(-5.5f, 0, -2f), 5f, Mat.OliveSteel, true, string.Empty, rng, 1, true);
            KitModules.Shed(b, new Vector3(5.2f, 0, -2.5f), 3.6f, 3f, 2.6f, 2.2f, Mat.Rust, false);
            KitModules.Barrels(b, new Vector3(3f, 0, -5.5f), 3, rng);
            KitParts.Mast(b, m, new Vector3(3.5f, 0, 4.5f), 13f, false);
            Beacon(b, m, new Vector3(3.5f, 13.2f, 4.5f), Mat.LampPhosphor, Model.Phosphor, 1.6f);
            b.Pop();
            m.Static = b.Mesh;
            KeepBeacons(m);
            return m;
        }

        /// <summary>Everything between the sites, in world space (one static mesh).</summary>
        public static Model Dressing(uint seed)
        {
            var m = new Model();
            var b = new MeshBuilder(seed + 211);
            var rng = new ArtRandom(seed + 223);
            Highway(b, rng, seed);
            Pylons(b, rng, seed);
            Blocks(b, rng, seed);
            Rocks(b, rng, seed);
            Trees(b, rng, seed);
            m.Static = b.Mesh;
            return m;
        }

        private static Mat FactionLamp(Faction f)
        {
            switch (f)
            {
                case Faction.Vanguard: return Mat.LampRed;
                case Faction.Church: return Mat.LampMagenta;
                case Faction.Holdouts: return Mat.LampCold;
                default: return Mat.LampAmber;
            }
        }

        private static Vector3 FactionColor(Faction f)
        {
            switch (f)
            {
                case Faction.Vanguard: return Model.Red;
                case Faction.Church: return new Vector3(0.88f, 0.48f, 0.82f);
                case Faction.Holdouts: return Model.ColdWhite;
                default: return Model.Amber;
            }
        }

        /// <summary>A lamp that reads from the map camera (bigger housing) with the landmark's one point light.</summary>
        private static void Beacon(MeshBuilder b, Model m, Vector3 p, Mat lamp, Vector3 color, float intensity)
        {
            Shapes.Lamp(b, m, p, lamp, color, intensity, 9f, LightRole.Ambient, 0.55f);
        }

        private static void Outpost(MeshBuilder b, Model m, ArtRandom rng, Faction owner, bool strong)
        {
            float s = strong ? 9f : 7f;
            KitModules.Fence(b, new Vector3(-s, 0, -s), new Vector3(-2f, 0, -s), 2.4f);
            KitModules.Fence(b, new Vector3(2f, 0, -s), new Vector3(s, 0, -s), 2.4f);
            KitModules.Fence(b, new Vector3(s, 0, -s), new Vector3(s, 0, s), 2.4f);
            KitModules.Fence(b, new Vector3(s, 0, s), new Vector3(-s, 0, s), 2.4f);
            KitModules.Fence(b, new Vector3(-s, 0, s), new Vector3(-s, 0, -s), 2.4f);
            Shapes.SandbagWall(b, new Vector3(-3.4f, 0, -s - 1.2f), new Vector3(3.4f, 0, -s - 1.2f), 3);
            KitModules.Watchtower(b, m, new Vector3(-s + 1.6f, 0, -s + 1.6f), 6.5f);
            KitModules.ContainerBlock(b, m, new Vector3(1.5f, 0, 3f), 6f, owner == Faction.Holdouts ? Mat.PaintWhite : Mat.Rust, true, string.Empty, rng, 1, true);
            KitModules.Shed(b, new Vector3(-3f, 0, 2.5f), 4f, 3.6f, 3f, 2.6f, Mat.Rust, true);
            KitModules.CrateStack(b, new Vector3(3.5f, 0, -3f), rng);
            KitModules.Barrels(b, new Vector3(-2f, 0, -3.5f), 3, rng);
            KitModules.Pickup(b, new Vector3(0.5f, 0, -1.5f), 80f, Mat.SandSteel, false);
            KitParts.Mast(b, m, new Vector3(s - 2f, 0, -s + 2f), 10f, false);
            Beacon(b, m, new Vector3(s - 2f, 10.2f, -s + 2f), FactionLamp(owner), FactionColor(owner), 1.4f);
            if (strong)
            {
                // a dug-in camp: second tower, more containers, a gun pit
                KitModules.Watchtower(b, m, new Vector3(s - 1.6f, 0, s - 1.6f), 7f);
                KitModules.ContainerBlock(b, m, new Vector3(-4f, 0, 6.5f), 6f, Mat.OliveSteel, false, string.Empty, rng, 0, true);
                Shapes.SandbagRing(b, new Vector3(5f, 0, 1f), 1.8f, 12, 2, 180f, 60f);
            }
        }

        private static void DataCenter(MeshBuilder b, Model m, ArtRandom rng, Faction owner, bool strong)
        {
            float w = strong ? 13f : 10f;
            const float H = 6f;
            const float D = 8f;

            // main hall with pilasters, a parapet and a lower plant annex
            b.BoxOn(0, 0, 0, w, H, D, Mat.ConcreteDark, 0.12f);
            for (float x = -w * 0.5f; x <= w * 0.5f + 0.01f; x += w / 5f)
            {
                b.BoxOn(x, 0, -D * 0.5f - 0.2f, 0.5f, H + 0.3f, 0.45f, Mat.Concrete, 0.04f);
            }

            b.BoxOn(0, H, -D * 0.5f + 0.15f, w + 0.2f, 0.5f, 0.35f, Mat.Concrete, 0.04f);
            b.BoxOn(0, H, D * 0.5f - 0.15f, w + 0.2f, 0.5f, 0.35f, Mat.Concrete, 0.04f);
            b.BoxOn(-w * 0.5f - 2.4f, 0, 1.2f, 4.4f, 3.4f, 5.2f, Mat.Concrete, 0.08f);
            for (int i = 0; i < 4; i++)
            {
                // narrow lit slits between the pilasters: the racks inside still run
                b.Box(new Vector3((-w * 0.4f) + ((i + 0.5f) * w * 0.2f), 3.8f, -D * 0.5f - 0.03f), new Vector3(w * 0.12f, 0.35f, 0.05f), Mat.Screen, 0.01f);
            }

            b.BoxOn(-w * 0.2f, H, 0.4f, w * 0.35f, 1.3f, 4.4f, Mat.Concrete, 0.08f);
            for (int i = 0; i < 3; i++)
            {
                KitParts.AcUnit(b, new Vector3((w * 0.12f) + (i * 1.6f), H, -1.8f));
                KitParts.Vent(b, new Vector3((w * 0.12f) + (i * 1.6f), H, 2f), 0.8f, 0.9f);
            }

            // cooling tanks and the pipe run into the hall
            KitParts.TankV(b, new Vector3(w * 0.5f + 2.2f, 0, -1.5f), 1.3f, 4.2f, Mat.SandSteel, rng.NextUInt());
            KitParts.TankV(b, new Vector3(w * 0.5f + 2.2f, 0, 1.8f), 1.3f, 4.2f, Mat.SandSteel, rng.NextUInt());
            KitParts.Pipe(b, new[] { new Vector3(w * 0.5f + 2.2f, 4.2f, 0.2f), new Vector3(w * 0.5f + 2.2f, 5f, 0.2f), new Vector3(w * 0.5f, 5f, 0.2f) }, 0.25f, Mat.Rust);

            foreach (float z in new[] { -2.2f, 2.2f })
            {
                // satellite dishes on the annex roof, tipped toward the sky
                float x = -w * 0.5f - 2.4f;
                b.Strut(new Vector3(x, 3.4f, z * 0.8f), new Vector3(x, 4.6f, z * 0.8f), 0.18f, Mat.DarkSteel);
                b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-35f)) * Matrix4x4.CreateTranslation(new Vector3(x, 4.6f, z * 0.8f)));
                b.Frustum(Vector3.Zero, 0.2f, 1.4f, 0.55f, 18, Mat.SandSteel, 0.03f);
                b.Pop();
            }

            KitParts.Transformer(b, new Vector3(-w * 0.5f - 1.5f, 0, -5.5f), 1f);
            KitModules.Fence(b, new Vector3(-w * 0.5f - 5f, 0, -7f), new Vector3(w * 0.5f + 4.5f, 0, -7f), 2.2f);
            KitParts.Mast(b, m, new Vector3(w * 0.25f, H, 1f), 8f, false);
            Beacon(b, m, new Vector3(w * 0.25f, H + 8.2f, 1f), FactionLamp(owner), FactionColor(owner), 1.4f);
            KitModules.Debris(b, new Vector3(w * 0.5f + 1f, 0, -4.5f), 1.2f, rng);
        }

        private static void Ruins(MeshBuilder b, ArtRandom rng, float radius, int walls)
        {
            for (int i = 0; i < walls; i++)
            {
                float a = (i / (float)walls * 360f) + rng.Range(-15f, 15f);
                float r = rng.Range(radius * 0.3f, radius);
                var at = new Vector3((float)Math.Cos(MeshBuilder.Deg(a)) * r, 0, (float)Math.Sin(MeshBuilder.Deg(a)) * r);
                b.Push(at, rng.Range(0f, 180f));
                float len = rng.Range(3f, 7f);
                float h = rng.Range(1.5f, 6.5f);
                b.BoxOn(0, 0, 0, len, h, 0.5f, rng.Next() < 0.5f ? Mat.Concrete : Mat.ConcreteDark, 0.06f);
                b.BoxOn(len * 0.2f, h, 0, len * 0.35f, rng.Range(0.6f, 1.6f), 0.5f, Mat.Concrete, 0.06f);
                KitParts.Rebar(b, new Vector3(-len * 0.3f, h, 0), Vector3.UnitY, 4, rng.NextUInt());
                b.Pop();
                KitModules.Debris(b, at + new Vector3(rng.Range(-1.5f, 1.5f), 0, rng.Range(-1.5f, 1.5f)), rng.Range(0.8f, 1.6f), rng);
            }

            for (int i = 0; i < 6; i++)
            {
                Props.Boulder(b, new Vector3(rng.Range(-radius, radius), 0.2f, rng.Range(-radius, radius)), new Vector3(rng.Range(0.6f, 1.4f), rng.Range(0.4f, 0.8f), rng.Range(0.6f, 1.2f)), rng.NextUInt(), Mat.ConcreteDark);
            }
        }

        private static void Convoy(MeshBuilder b, Model m, ArtRandom rng, Faction owner)
        {
            for (int i = 0; i < 6; i++)
            {
                b.BoxOn(-12f + (i * 4.1f), -0.25f, 0, 4f, 0.3f, 5f, Mat.ConcreteDark, 0.04f);
            }

            KitModules.Pickup(b, new Vector3(-6f, 0, -1f), 90f, Mat.SandSteel, false);
            KitModules.Pickup(b, new Vector3(5f, 0, 1f), 95f, Mat.OliveSteel, false);
            Props.Container(b, m, new Vector3(-0.5f, 0.9f, 0f), 6f, 2.4f, 2.4f, Mat.Rust, false, rng.NextUInt());
            b.BoxOn(-0.5f, 0, 0, 6.2f, 0.9f, 2.2f, Mat.DarkSteel, 0.04f);
            KitModules.Barrels(b, new Vector3(9f, 0, -2.5f), 3, rng);
            Beacon(b, m, new Vector3(-0.5f, 3.5f, 0), FactionLamp(owner), FactionColor(owner), 1.0f);
        }

        /// <summary>The blast bowl (the terrain carries the shape): a pool of blue glow, glowing cracks, dead ground, warning signs.</summary>
        private static void Crater(MeshBuilder b, Model m, ArtRandom rng, Vector3 origin, uint seed)
        {
            // local ground height (the zone is built unrotated at full size, so local x/z are world offsets)
            float G(float x, float z) => SectorScene.Height(origin.X + x, origin.Z + z, seed) - origin.Y;

            // a dark, glassy pool on the floor with a hot core showing through
            b.Frustum(new Vector3(0, -0.5f, 0), SectorScene.CraterFloor - 0.2f, SectorScene.CraterFloor - 0.6f, 0.56f, 32, Mat.Water, 0.02f);
            b.Frustum(new Vector3(0.6f, 0.02f, -0.4f), 1.7f, 1.2f, 0.1f, 18, Mat.NeonCyan, 0.02f);
            for (int i = 0; i < 6; i++)
            {
                // glowing veins climbing the bowl walls, hugging the ground in short kinked runs
                float a = MeshBuilder.Deg((i * 60f) + rng.Range(-18f, 18f));
                float len = rng.Range(SectorScene.CraterWall + 2f, SectorScene.CraterWall + 8f);
                Vector3 prev = Vector3.Zero;
                bool first = true;
                for (float r = SectorScene.CraterFloor - 0.5f; r < len; r += 1.5f)
                {
                    float k = a + (rng.Range(-0.12f, 0.12f) * (r / len));
                    float x = (float)Math.Cos(k) * r;
                    float z = (float)Math.Sin(k) * r;
                    var p = new Vector3(x, G(x, z) + 0.05f, z);
                    if (!first)
                    {
                        b.Strut(prev, p, (0.14f * (1f - (r / (len + 2f)))) + 0.04f, Mat.NeonCyan);
                    }

                    prev = p;
                    first = false;
                }
            }

            for (int i = 0; i < 5; i++)
            {
                float a = MeshBuilder.Deg(i * 72f + 20f);
                float sx = (float)Math.Cos(a) * 17f;
                float sz = (float)Math.Sin(a) * 17f;
                var p = new Vector3(sx, G(sx, sz), sz);
                b.Strut(p, p + new Vector3(0, 2.2f, 0), 0.12f, Mat.DarkSteel);
                b.Box(p + new Vector3(0, 2.4f, 0), new Vector3(1.4f, 1f, 0.08f), Mat.PaintGreen, 0.02f);
            }

            m.Lights.Add(new LightSpec(b.TransformPoint(new Vector3(0, 2f, 0)), new Vector3(0.3f, 0.95f, 0.9f), 3f, 22f, LightRole.Ambient));
        }

        /// <summary>A quarantine camp: fence ring with warning lamps, white tents, a burn pit.</summary>
        private static void Quarantine(MeshBuilder b, Model m, ArtRandom rng)
        {
            const int Segments = 14;
            const float R = 10f;
            for (int i = 0; i < Segments; i++)
            {
                if (i == 3)
                {
                    continue;
                }

                float a0 = MeshBuilder.Deg(i * 360f / Segments);
                float a1 = MeshBuilder.Deg((i + 1) * 360f / Segments);
                KitModules.Fence(b, new Vector3((float)Math.Cos(a0) * R, 0, (float)Math.Sin(a0) * R), new Vector3((float)Math.Cos(a1) * R, 0, (float)Math.Sin(a1) * R), 2.6f);
                if (i % 3 == 0)
                {
                    Shapes.Lamp(b, m, new Vector3((float)Math.Cos(a0) * R, 2.9f, (float)Math.Sin(a0) * R), Mat.LampRed, Model.Red, 0f, 0f, LightRole.Ambient, 0.4f);
                }
            }

            // isolation tents: white tarps draped over frames
            KitParts.Tarp(b, new Vector3(-3f, 0, 2f), 5f, 3.6f, 2.4f, Mat.PaintWhite, rng.NextUInt());
            KitParts.Tarp(b, new Vector3(3.5f, 0, 1.5f), 4.2f, 3.2f, 2.2f, Mat.PaintWhite, rng.NextUInt());
            KitParts.Tarp(b, new Vector3(-0.5f, 0, -4f), 4.2f, 3.2f, 2.2f, Mat.PaintWhite, rng.NextUInt());
            // the burn pit: a soot-black heap with a few embers in it
            Props.Boulder(b, new Vector3(5f, 0f, -5f), new Vector3(1.6f, 0.5f, 1.4f), rng.NextUInt(), Mat.Char);
            for (int i = 0; i < 4; i++)
            {
                b.Box(new Vector3(5f + rng.Range(-0.9f, 0.9f), 0.35f, -5f + rng.Range(-0.8f, 0.8f)), new Vector3(0.22f, 0.12f, 0.18f), Mat.Ember, 0.03f);
            }
            KitModules.Barrels(b, new Vector3(-5f, 0, -4.5f), 3, rng);
            KitModules.TarpPile(b, new Vector3(-6f, 0, 5f), 1.4f, rng);
            Beacon(b, m, new Vector3(0, 3.2f, 6.5f), Mat.LampRed, Model.Red, 1.0f);
        }

        /// <summary>The machine graveyard: burnt hulks, armour plates, a toppled walker leg, drone eyes still lit.</summary>
        private static void WreckField(MeshBuilder b, Model m, ArtRandom rng, uint seed)
        {
            // dead war machines where they fell, burnt trucks between them, torn armour in the dirt
            WarMachine(b, new Vector3(2f, 0, 3f), 30f, rng);
            WarMachine(b, new Vector3(-7f, 0, -4f), -65f, rng);
            WarMachine(b, new Vector3(8f, 0, -6f), 160f, rng);
            for (int i = 0; i < 4; i++)
            {
                KitModules.Pickup(b, new Vector3(rng.Range(-11f, 11f), 0, rng.Range(-9f, 9f)), rng.Range(0f, 360f), rng.Next() < 0.5f ? Mat.Rust : Mat.Char, true);
            }

            for (int i = 0; i < 8; i++)
            {
                KitParts.Plate(b, new Vector3(rng.Range(-12f, 12f), 0.4f, rng.Range(-10f, 10f)), rng.Range(1.0f, 2.2f), rng.Range(0.6f, 1.2f), rng.Range(-70f, 70f), i % 2 == 0 ? Mat.Rust : Mat.DarkSteel);
            }

            for (int i = 0; i < 5; i++)
            {
                Shapes.Lamp(b, m, new Vector3(rng.Range(-10f, 10f), rng.Range(0.6f, 1.6f), rng.Range(-8f, 8f)), Mat.LampRed, Model.Red, 0f, 0f, LightRole.Ambient, 0.3f);
            }
        }

        /// <summary>A tracked war machine: sloped hull, tracks, a scorched turret, its gun dipped into the dirt.</summary>
        private static void WarMachine(MeshBuilder b, Vector3 at, float yaw, ArtRandom rng)
        {
            b.Push(at, yaw);
            b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(rng.Range(-6f, 6f))));
            foreach (float x in new[] { -1.8f, 1.8f })
            {
                b.BoxOn(x, 0, 0, 0.9f, 1.1f, 6.6f, Mat.Rubber, 0.2f);
                for (float z = -2.6f; z <= 2.7f; z += 1.3f)
                {
                    b.CylinderX(new Vector3(x, 0.45f, z), 0.42f, 0.95f, 10, Mat.DarkSteel);
                }
            }

            b.BoxOn(0, 0.7f, 0.3f, 2.8f, 1.0f, 5.6f, Mat.OliveSteel, 0.12f);
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(-28f)) * Matrix4x4.CreateTranslation(new Vector3(0, 1.2f, -2.75f)));
            b.Box(Vector3.Zero, new Vector3(2.7f, 0.12f, 1.5f), Mat.OliveSteel, 0.04f);
            b.Pop();
            b.Frustum(new Vector3(0.2f, 1.7f, 0.6f), 1.25f, 0.95f, 0.75f, 10, Mat.Char, 0.08f);
            b.Push(Matrix4x4.CreateRotationX(MeshBuilder.Deg(rng.Range(4f, 12f))) * Matrix4x4.CreateTranslation(new Vector3(0.2f, 2.05f, -0.4f)));
            b.CylinderZ(new Vector3(0, 0, -2.6f), 0.13f, 4.8f, 8, Mat.DarkSteel);
            b.Pop();
            b.Pop();
            b.Pop();
        }

        private static bool NearCrater(float x, float z, float r)
        {
            for (int i = 0; i < WorldSystem.Sites.Count; i++)
            {
                SiteDef d = WorldSystem.Sites[i];
                float dx = x - (d.MapX * SectorScene.Unit);
                float dz = z - (d.MapY * SectorScene.Unit);
                if (d.Kind == SiteKind.Radiation && (dx * dx) + (dz * dz) < r * r)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Clear of the Hub, the sites and the riverbed.</summary>
        private static bool Clear(float x, float z, float margin)
        {
            if ((x * x) + (z * z) < (14f + margin) * (14f + margin))
            {
                return false;
            }

            for (int i = 0; i < WorldSystem.Sites.Count; i++)
            {
                SiteDef d = WorldSystem.Sites[i];
                float dx = x - (d.MapX * SectorScene.Unit);
                float dz = z - (d.MapY * SectorScene.Unit);
                float r = (HazardSystem.Wild(d.Kind) ? 14f : 10f) + margin;
                if ((dx * dx) + (dz * dz) < r * r)
                {
                    return false;
                }
            }

            return true;
        }

        private static void Place(MeshBuilder b, float x, float z, float yaw, float scale, uint seed)
        {
            float h = SectorScene.Height(x, z, seed);
            b.GroundOffset = -h;
            b.AoHeight = 1.2f * scale;
            b.Push(new Vector3(x, h, z), yaw, scale);
        }

        private static void Done(MeshBuilder b)
        {
            b.Pop();
            b.GroundOffset = 0f;
            b.AoHeight = 1.2f;
        }

        /// <summary>The old highway: broken slabs along the cut, gaps where it fell in, wrecks left where they stopped.</summary>
        private static void Highway(MeshBuilder b, ArtRandom rng, uint seed)
        {
            const float Slab = 6f;
            for (float x = SectorScene.MinX + 4f; x < SectorScene.MaxX - 4f; x += Slab)
            {
                // a collapsed span here and there; otherwise a continuous, cracked carriageway
                bool broken = rng.Next() < 0.08f;
                float z = -14f + ((float)Math.Sin(x * 0.03f) * 6f);
                if (NearCrater(x, z, SectorScene.CraterWall + 4f))
                {
                    // the blast took this span
                    continue;
                }

                float slope = (float)Math.Cos(x * 0.03f) * 0.18f;
                float yaw = -(float)(Math.Atan(slope) * 180.0 / Math.PI);
                // rest on the highest corner so the deck never sinks into the ground
                float h = Math.Max(Math.Max(SectorScene.Height(x - 3f, z - 3f, seed), SectorScene.Height(x + 3f, z - 3f, seed)), Math.Max(SectorScene.Height(x - 3f, z + 3f, seed), SectorScene.Height(x + 3f, z + 3f, seed)));
                b.Push(new Vector3(x, h - 0.3f - (broken ? 0.5f : 0f), z), yaw);
                b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(broken ? rng.Range(-9f, 9f) : rng.Range(-0.6f, 0.6f))));
                b.BoxOn(0, -0.6f, 0, Slab + 0.12f, 0.9f, 6f, Mat.Rubber, 0.03f);
                b.BoxOn(0, -0.6f, -3.25f, Slab + 0.12f, 0.96f, 0.5f, Mat.ConcreteDark, 0.03f);
                b.BoxOn(0, -0.6f, 3.25f, Slab + 0.12f, 0.96f, 0.5f, Mat.ConcreteDark, 0.03f);
                if (!broken && ((int)(x / Slab) % 2 == 0))
                {
                    b.Box(new Vector3(0, 0.31f, 0), new Vector3(2.4f, 0.02f, 0.16f), Mat.PaintWhite, 0f);
                }

                b.Pop();
                b.Pop();
                if (rng.Next() < 0.12f && Clear(x, z, -6f))
                {
                    Place(b, x, z + rng.Range(-1.4f, 1.4f), yaw + 90f + rng.Range(-30f, 30f), 0.8f, seed);
                    KitModules.Pickup(b, Vector3.Zero, 0f, rng.Next() < 0.5f ? Mat.SandSteel : Mat.Rust, true);
                    Done(b);
                }
            }
        }

        /// <summary>Dead power pylons marching north of the highway, cables sagging between them.</summary>
        private static void Pylons(MeshBuilder b, ArtRandom rng, uint seed)
        {
            Vector3? last = null;
            for (float x = -100f; x <= 100f; x += 26f)
            {
                float z = -4f + ((float)Math.Sin(x * 0.03f) * 6f);
                if (!Clear(x, z, -4f))
                {
                    last = null;
                    continue;
                }

                float h = SectorScene.Height(x, z, seed);
                bool down = rng.Next() < 0.2f;
                var top = new Vector3(x, h + (down ? 3f : 11f), z);
                for (int k = 0; k < 4; k++)
                {
                    float sx = k < 2 ? -1.2f : 1.2f;
                    float sz = k % 2 == 0 ? -1.2f : 1.2f;
                    b.Strut(new Vector3(x + sx, h, z + sz), top + new Vector3(sx * 0.2f, 0, sz * 0.2f), 0.14f, Mat.DarkSteel);
                }

                b.Strut(top + new Vector3(-3f, 0, 0), top + new Vector3(3f, 0, 0), 0.16f, Mat.DarkSteel);
                if (last.HasValue && !down)
                {
                    foreach (float o in new[] { -2.8f, 2.8f })
                    {
                        KitParts.Cable(b, last.Value + new Vector3(o, -0.2f, 0), top + new Vector3(o, -0.2f, 0), 2.2f, 0.06f, Mat.Rubber);
                    }
                }

                last = down ? (Vector3?)null : top;
            }
        }

        /// <summary>Collapsed blocks: shells of concrete buildings in a few clusters of the old town.</summary>
        private static void Blocks(MeshBuilder b, ArtRandom rng, uint seed)
        {
            var towns = new[] { new Vector3(-6f, 0, 30f), new Vector3(45f, 0, 8f), new Vector3(-60f, 0, -55f), new Vector3(70f, 0, 60f), new Vector3(-75f, 0, 20f) };
            foreach (Vector3 town in towns)
            {
                for (int i = 0; i < 9; i++)
                {
                    float x = town.X + rng.Range(-13f, 13f);
                    float z = town.Z + rng.Range(-11f, 11f);
                    if (!Clear(x, z, 1f))
                    {
                        continue;
                    }

                    Place(b, x, z, rng.Range(-8f, 8f) + (i % 2 * 90f), 0.7f, seed);
                    Shell(b, rng, rng.Range(6f, 10f), rng.Range(6f, 9f), rng.Range(6f, 15f));
                    KitModules.Debris(b, new Vector3(0, 0, 0), rng.Range(1.6f, 2.6f), rng);
                    Done(b);
                }
            }
        }

        /// <summary>A gutted concrete frame: jagged broken walls stepping down, floor slabs jutting out, a rubble mound inside.</summary>
        private static void Shell(MeshBuilder b, ArtRandom rng, float w, float d, float h)
        {
            Mat mat = rng.Next() < 0.5f ? Mat.Concrete : Mat.ConcreteDark;
            const float Floor = 3.2f;
            int floors = Math.Max(1, (int)(h / Floor));
            for (int side = 0; side < 4; side++)
            {
                bool alongX = side % 2 == 0;
                float len = alongX ? w : d;
                float off = (alongX ? d : w) * 0.5f * (side < 2 ? -1f : 1f);
                int cols = Math.Max(2, (int)(len / 2.2f));
                float cw = len / cols;
                float keep = side == 0 ? 1f : rng.Range(0.25f, 0.9f);
                for (int c = 0; c < cols; c++)
                {
                    // each column breaks off at its own floor; the far side and the corners stand tallest
                    float edge = Math.Abs((c + 0.5f) - (cols * 0.5f)) / (cols * 0.5f);
                    int f = Math.Max(0, (int)Math.Round(floors * keep * (0.45f + (0.55f * edge)) * rng.Range(0.6f, 1.1f)));
                    if (f == 0)
                    {
                        continue;
                    }

                    float colH = (f * Floor) - rng.Range(0f, 1.4f);
                    float along = -len * 0.5f + ((c + 0.5f) * cw);
                    Vector3 at = alongX ? new Vector3(along, 0, off) : new Vector3(off, 0, along);
                    Vector3 size = alongX ? new Vector3(cw * 0.96f, colH, 0.45f) : new Vector3(0.45f, colH, cw * 0.96f);
                    b.BoxOn(at.X, 0, at.Z, size.X, size.Y, size.Z, mat, 0.04f);
                    KitParts.Rebar(b, at + new Vector3(0, colH, 0), Vector3.UnitY, 2, rng.NextUInt());
                }
            }

            for (int f = 1; f < floors; f++)
            {
                if (rng.Next() < 0.55f)
                {
                    float sw = w * rng.Range(0.3f, 0.7f);
                    b.Push(new Vector3(rng.Range(-w * 0.2f, w * 0.2f), f * Floor, rng.Range(-d * 0.2f, d * 0.2f)), rng.Range(-6f, 6f));
                    b.Push(Matrix4x4.CreateRotationZ(MeshBuilder.Deg(rng.Range(-14f, 14f))));
                    b.Box(Vector3.Zero, new Vector3(sw, 0.3f, d * rng.Range(0.4f, 0.8f)), Mat.ConcreteDark, 0.04f);
                    b.Pop();
                    b.Pop();
                }
            }

            Props.Boulder(b, new Vector3(rng.Range(-1f, 1f), 0f, rng.Range(-1f, 1f)), new Vector3(w * 0.35f, h * 0.12f + 0.6f, d * 0.35f), rng.NextUInt(), Mat.ConcreteDark);
        }

        private static void Rocks(MeshBuilder b, ArtRandom rng, uint seed)
        {
            for (int i = 0; i < 40; i++)
            {
                float x = rng.Range(SectorScene.MinX + 6f, SectorScene.MaxX - 6f);
                float z = rng.Range(SectorScene.MinZ + 6f, 120f);
                float slope = Math.Abs(SectorScene.Height(x + 1f, z, seed) - SectorScene.Height(x - 1f, z, seed)) + Math.Abs(SectorScene.Height(x, z + 1f, seed) - SectorScene.Height(x, z - 1f, seed));
                if (slope < 0.35f || !Clear(x, z, 0f))
                {
                    continue;
                }

                float s = rng.Range(0.7f, 2.0f);
                Props.Boulder(b, new Vector3(x, SectorScene.Height(x, z, seed) - (s * 0.15f), z), new Vector3(s, s * 0.6f, s * rng.Range(0.7f, 1.2f)), rng.NextUInt(), Mat.Rock);
            }
        }

        /// <summary>Dead trees along the dry river and in scattered stands.</summary>
        private static void Trees(MeshBuilder b, ArtRandom rng, uint seed)
        {
            for (int i = 0; i < 46; i++)
            {
                float z = rng.Range(SectorScene.MinZ + 6f, 110f);
                float x = i < 30 ? -30f + ((float)Math.Sin(z * 0.045f) * 9f) + rng.Range(-9f, 9f) : rng.Range(SectorScene.MinX + 6f, SectorScene.MaxX - 6f);
                if (!Clear(x, z, -2f))
                {
                    continue;
                }

                Place(b, x, z, rng.Range(0f, 360f), rng.Range(0.7f, 1.1f), seed);
                float h = rng.Range(4f, 7f);
                var top = new Vector3(rng.Range(-0.4f, 0.4f), h, rng.Range(-0.4f, 0.4f));
                b.Strut(Vector3.Zero, top, 0.28f, Mat.Wood);
                for (int k = 0; k < 4; k++)
                {
                    float a = MeshBuilder.Deg(k * 95f + rng.Range(0f, 40f));
                    Vector3 from = top * rng.Range(0.45f, 0.85f);
                    b.Strut(from, from + new Vector3((float)Math.Cos(a) * 1.8f, rng.Range(0.6f, 1.6f), (float)Math.Sin(a) * 1.8f), 0.1f, rng.Next() < 0.3f ? Mat.Char : Mat.Wood);
                }

                Done(b);
            }
        }
    }
}
