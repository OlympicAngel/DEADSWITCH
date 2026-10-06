using System;
using System.Collections.Generic;
using System.Numerics;
using Deadswitch.Art.Geometry;
using Deadswitch.Art.Models;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Art.World
{
    /// <summary>A fixed camera: position, look-at target and vertical field of view (Unity space).</summary>
    public readonly struct CameraPose
    {
        public CameraPose(Vector3 position, Vector3 target, float fov)
        {
            Position = position;
            Target = target;
            Fov = fov;
        }

        public Vector3 Position { get; }

        public Vector3 Target { get; }

        /// <summary>Vertical field of view in degrees.</summary>
        public float Fov { get; }

        /// <summary>
        /// Where a world point lands on the picture: x 0..1 left to right, y 0..1 top to bottom (UI convention).
        /// Same math as Unity's WorldToViewportPoint, so the UI and the headless preview agree.
        /// </summary>
        public Vector2 Project(Vector3 world, float aspect)
        {
            Vector3 f = Vector3.Normalize(Target - Position);
            Vector3 r = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, f));
            Vector3 u = Vector3.Cross(f, r);
            Vector3 d = world - Position;
            float z = Math.Max(0.01f, Vector3.Dot(d, f));
            float t = (float)Math.Tan(Fov * Math.PI / 360.0);
            float x = Vector3.Dot(d, r) / (z * t * aspect);
            float y = Vector3.Dot(d, u) / (z * t);
            return new Vector2(0.5f + (0.5f * x), 0.5f - (0.5f * y));
        }
    }

    /// <summary>One placed object of the sector map: a model at a world position, and the site it stands for (-1: none, -2: the Hub).</summary>
    public sealed class SectorObject
    {
        public SectorObject(string name, Model model, Vector3 position, int site)
        {
            Name = name;
            Model = model;
            Position = position;
            Site = site;
        }

        public string Name { get; }

        public Model Model { get; }

        public Vector3 Position { get; }

        public int Site { get; }
    }

    /// <summary>
    /// The sector map (SPEC-033): low-relief ruined ground around the Hub, seen from one fixed recon angle. Map
    /// coordinates (-100..100, north +Y) become world X/Z at <see cref="Unit"/> meters each (north is +Z, the camera
    /// looks north from the south). Engine-agnostic: Unity and the headless preview build the same meshes.
    /// </summary>
    public static class SectorScene
    {
        /// <summary>Meters per map unit (F-112: doubled so the sites stand well apart).</summary>
        public const float Unit = 1.2f;

        /// <summary>
        /// Map-plane meters the zoom-1 view frames on each side of the Hub. The zoom range in the look file is a fraction
        /// of this view, so it stays put when the sites spread out.
        /// </summary>
        public const float ViewFit = 60f;

        /// <summary>Ground mesh bounds: well past the outermost sites, so the view can center any of them and never shows the edge.</summary>
        public const float MinX = -200f;
        public const float MaxX = 200f;
        public const float MinZ = -190f;
        public const float MaxZ = 330f;

        /// <summary>How far inside the ground bounds the corners of the picture must stay.</summary>
        private const float EdgeMargin = 6f;

        /// <summary>How far past the outermost site the focus may go (meters), so the view never drifts into empty waste.</summary>
        private const float ContentMargin = 18f;

        /// <summary>Scale of the kit landmarks on the map (life-size kit pieces would crowd the sites).</summary>
        public const float LandmarkScale = 0.75f;

        private const float Step = 2f;

        /// <summary>Blast bowl: flat floor out to this radius (the glow pool), walls up to <see cref="CraterWall"/>.</summary>
        public const float CraterFloor = 4.5f;

        public const float CraterWall = 10f;

        /// <summary>Everything on the map (static for the run): terrain, dressing, the Hub and one landmark per site.</summary>
        public static List<SectorObject> Build(uint seed)
        {
            var list = new List<SectorObject>
            {
                new SectorObject("Terrain", new Model { Static = Terrain(seed) }, Vector3.Zero, -1),
                new SectorObject("Dressing", SectorLandmarks.Dressing(seed), Vector3.Zero, -1),
                new SectorObject("Hub", SectorLandmarks.Hub(seed), new Vector3(0, Height(0, 0, seed), 0), -2),
            };
            for (int i = 0; i < WorldSystem.Sites.Count; i++)
            {
                list.Add(new SectorObject("Site " + i, SectorLandmarks.Site(i, seed), SitePosition(i, seed), i));
            }

            return list;
        }

        public static Vector3 SitePosition(int site, uint seed)
        {
            SiteDef d = WorldSystem.Sites[site];
            float x = d.MapX * Unit;
            float z = d.MapY * Unit;
            return new Vector3(x, Height(x, z, seed), z);
        }

        /// <summary>
        /// The recon angle (no free camera): elevation and field of view from the look file, pulled back until the
        /// whole map fits a picture of this aspect (width / height).
        /// </summary>
        public static CameraPose Camera(float aspect, float elevationDeg = 47f, float fov = 28f, float margin = 1.04f)
        {
            return Fit(aspect, 100f * Unit * margin, elevationDeg, fov);
        }

        private static CameraPose Fit(float aspect, float extent, float elevationDeg, float fov)
        {
            var target = new Vector3(0, 0, 3f);
            float el = elevationDeg * (float)Math.PI / 180f;
            var dir = new Vector3(0, (float)Math.Sin(el), -(float)Math.Cos(el));
            var corners = new[]
            {
                new Vector3(-extent, 0, -extent), new Vector3(extent, 0, -extent), new Vector3(-extent, 0, extent), new Vector3(extent, 0, extent),
                new Vector3(-extent, 4f, extent), new Vector3(extent, 4f, extent),
            };
            float dist = 40f;
            for (int i = 0; i < 400; i++)
            {
                var pose = new CameraPose(target + (dir * dist), target, fov);
                bool inside = true;
                foreach (Vector3 c in corners)
                {
                    Vector2 p = pose.Project(c, aspect);
                    inside &= p.X >= 0f && p.X <= 1f && p.Y >= 0f && p.Y <= 1f;
                }

                if (inside)
                {
                    return pose;
                }

                dist += 2f;
            }

            return new CameraPose(target + (dir * dist), target, fov);
        }

        /// <summary>
        /// The same recon angle moved over the map (F-107): looking at a map-plane point (world X/Z meters) from a
        /// fraction of the <see cref="ViewFit"/> distance (smaller is closer). The focus is clamped with
        /// <see cref="ClampFocus"/>, so the picture never runs past the edge of the ground.
        /// </summary>
        public static CameraPose View(float aspect, float focusX, float focusZ, float zoom, float elevationDeg = 47f, float fov = 28f)
        {
            Vector2 f = ClampFocus(aspect, focusX, focusZ, zoom, elevationDeg, fov);
            return Pose(aspect, f.X, f.Y, zoom, elevationDeg, fov);
        }

        /// <summary>
        /// The nearest focus to (focusX, focusZ) whose picture stays on the ground: every corner of the frame must hit
        /// the map plane inside the ground bounds. The camera only slides (its angle is fixed), so the footprint at the
        /// Hub sets the range once per zoom.
        /// </summary>
        public static Vector2 ClampFocus(float aspect, float focusX, float focusZ, float zoom, float elevationDeg = 47f, float fov = 28f)
        {
            CameraPose pose = Pose(aspect, 0f, 0f, zoom, elevationDeg, fov);
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 hit = Ground(pose, aspect, i % 2, i / 2);
                minX = Math.Min(minX, hit.X);
                maxX = Math.Max(maxX, hit.X);
                minZ = Math.Min(minZ, hit.Z);
                maxZ = Math.Max(maxZ, hit.Z);
            }

            // first the sites' own spread, then the ground: the picture stays where there is something to see
            float cx = 0f, cX = 0f, cz = 0f, cZ = 0f;
            foreach (SiteDef d in WorldSystem.Sites)
            {
                cx = Math.Min(cx, d.MapX * Unit);
                cX = Math.Max(cX, d.MapX * Unit);
                cz = Math.Min(cz, d.MapY * Unit);
                cZ = Math.Max(cZ, d.MapY * Unit);
            }

            focusX = Math.Clamp(focusX, cx - ContentMargin, cX + ContentMargin);
            focusZ = Math.Clamp(focusZ, cz - ContentMargin, cZ + ContentMargin);
            return new Vector2(Range(focusX, MinX + EdgeMargin - minX, MaxX - EdgeMargin - maxX), Range(focusZ, MinZ + EdgeMargin - minZ, MaxZ - EdgeMargin - maxZ));
        }

        private static CameraPose Pose(float aspect, float focusX, float focusZ, float zoom, float elevationDeg, float fov)
        {
            CameraPose fit = Fit(aspect, ViewFit * 1.04f, elevationDeg, fov);
            Vector3 back = fit.Position - fit.Target;
            var target = new Vector3(focusX, 0, focusZ);
            return new CameraPose(target + (back * zoom), target, fov);
        }

        /// <summary>Where a picture corner (0/1, 0/1; y down) meets the map plane. A ray above the horizon reaches the far edge.</summary>
        private static Vector3 Ground(CameraPose pose, float aspect, float sx, float sy)
        {
            Vector3 f = Vector3.Normalize(pose.Target - pose.Position);
            Vector3 r = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, f));
            Vector3 u = Vector3.Cross(f, r);
            float t = (float)Math.Tan(pose.Fov * Math.PI / 360.0);
            Vector3 dir = f + (r * (((2f * sx) - 1f) * t * aspect)) + (u * ((1f - (2f * sy)) * t));
            if (dir.Y > -0.01f)
            {
                return new Vector3(pose.Position.X, 0f, MaxZ * 4f);
            }

            return pose.Position + (dir * (-pose.Position.Y / dir.Y));
        }

        /// <summary>Clamps to [lo, hi]; when the frame is wider than the ground, the middle.</summary>
        private static float Range(float v, float lo, float hi)
        {
            return lo > hi ? (lo + hi) * 0.5f : Math.Clamp(v, lo, hi);
        }

        /// <summary>Ground height: rolling waste, a dry riverbed in the west, flat pads at the Hub and the sites, bowls at the craters.</summary>
        public static float Height(float x, float z, uint seed)
        {
            float h = (Noise.Fbm(x * 0.018f, z * 0.018f, seed + 101, 4) - 0.5f) * 14f;
            h += (Noise.Fbm(x * 0.07f, z * 0.07f, seed + 107, 3) - 0.5f) * 1.6f;

            // ridge along the north-east edge, rising away from the camera
            h += Smooth(20f, 70f, z + (x * 0.4f)) * 5f;

            // the dry riverbed: a sinuous channel from north to south, west of the Hub
            float riverX = -30f + ((float)Math.Sin(z * 0.045f) * 9f);
            float river = (float)Math.Exp(-Math.Pow((x - riverX) / 5.5f, 2));
            h -= river * 2.6f;

            // the old highway: a shallow cut running west to east, south of the Hub
            h -= Highway(x, z) * 0.5f;

            // pads: the Hub and every site sit on level ground
            float pad = Smooth(9f, 5f, Dist(x, z, 0f, 0f));
            float level = 0f;
            for (int i = 0; i < WorldSystem.Sites.Count; i++)
            {
                SiteDef d = WorldSystem.Sites[i];
                float k = d.Kind == SiteKind.Radiation ? 0f : Smooth(8f, 4f, Dist(x, z, d.MapX * Unit, d.MapY * Unit));
                if (k > pad)
                {
                    pad = k;
                    level = RawPadLevel(d.MapX * Unit, d.MapY * Unit, seed);
                }

                if (d.Kind == SiteKind.Radiation)
                {
                    // blast bowl with a raised lip
                    float r = Dist(x, z, d.MapX * Unit, d.MapY * Unit);
                    h -= Smooth(CraterWall, CraterFloor, r) * 3.4f;
                    h += (float)Math.Exp(-Math.Pow((r - CraterWall - 0.5f) / 2.2f, 2)) * 1.1f;
                }
            }

            return pad <= 0f ? h : h + ((level - h) * pad);
        }

        /// <summary>0..1 how strongly the old highway runs here.</summary>
        public static float Highway(float x, float z)
        {
            float hz = -14f + ((float)Math.Sin(x * 0.03f) * 6f);
            return (float)Math.Exp(-Math.Pow((z - hz) / 2.4f, 2));
        }

        /// <summary>One smooth-shaded grid (shared vertices, normals from the height field): no facets at map range.</summary>
        public static MeshData Terrain(uint seed)
        {
            var m = new MeshData();
            int nx = (int)((MaxX - MinX) / Step) + 1;
            int nz = (int)((MaxZ - MinZ) / Step) + 1;
            for (int j = 0; j < nz; j++)
            {
                for (int i = 0; i < nx; i++)
                {
                    float x = MinX + (i * Step);
                    float z = MinZ + (j * Step);
                    var p = new Vector3(x, Height(x, z, seed), z);
                    float dx = Height(x + 0.5f, z, seed) - Height(x - 0.5f, z, seed);
                    float dz = Height(x, z + 0.5f, seed) - Height(x, z - 0.5f, seed);
                    var n = Vector3.Normalize(new Vector3(-dx, 1f, -dz));
                    m.AddVertex(p, n, new Vector4(Tone(p, seed), Ash(p.X, p.Z), Dust(p, n, seed), 1f));
                }
            }

            for (int j = 0; j < nz - 1; j++)
            {
                for (int i = 0; i < nx - 1; i++)
                {
                    int a = (j * nx) + i;
                    int c = a + nx;
                    m.AddTriangle(Mat.MapGround, a, c + 1, a + 1);
                    m.AddTriangle(Mat.MapGround, a, c, c + 1);
                }
            }

            return m;
        }

        private static float Tone(Vector3 p, uint seed)
        {
            float mud = Noise.Fbm(p.X * 0.05f, p.Z * 0.05f, seed + 131, 4);
            float grit = Noise.Value(p.X * 0.9f, p.Z * 0.9f, seed + 137);
            float slope = Math.Min(1f, (Math.Abs(Height(p.X + 0.75f, p.Z, seed) - Height(p.X - 0.75f, p.Z, seed)) + Math.Abs(Height(p.X, p.Z + 0.75f, seed) - Height(p.X, p.Z - 0.75f, seed))) * 0.6f);
            float riverX = -30f + ((float)Math.Sin(p.Z * 0.045f) * 9f);
            float river = (float)Math.Exp(-Math.Pow((p.X - riverX) / 3.5f, 2));
            float tone = 0.82f + ((mud - 0.5f) * 0.9f) + ((grit - 0.5f) * 0.18f) + (slope * 0.22f) - (river * 0.18f) + (Highway(p.X, p.Z) * 0.16f);
            for (int i = 0; i < WorldSystem.Sites.Count; i++)
            {
                SiteDef d = WorldSystem.Sites[i];
                float r = Dist(p.X, p.Z, d.MapX * Unit, d.MapY * Unit);
                tone += Track(p.X, p.Z, d.MapX * Unit, d.MapY * Unit) * 0.1f;
                if (d.Kind == SiteKind.Radiation)
                {
                    // scorched ground around the blast
                    tone -= Smooth(16f, 6f, r) * 0.35f;
                }
            }

            return Math.Max(0.28f, tone);
        }

        /// <summary>Soot and ash (vertex G in the arid ground mode): the blast field and the riverbed silt.</summary>
        private static float Ash(float x, float z)
        {
            float ash = 0f;
            for (int i = 0; i < WorldSystem.Sites.Count; i++)
            {
                SiteDef d = WorldSystem.Sites[i];
                if (d.Kind == SiteKind.Radiation || d.Kind == SiteKind.Graveyard)
                {
                    float r = Dist(x, z, d.MapX * Unit, d.MapY * Unit);
                    ash = Math.Max(ash, Smooth(d.Kind == SiteKind.Radiation ? 22f : 14f, 4f, r) * (d.Kind == SiteKind.Radiation ? 0.85f : 0.5f));
                }
            }

            float riverX = -30f + ((float)Math.Sin(z * 0.045f) * 9f);
            ash = Math.Max(ash, (float)Math.Exp(-Math.Pow((x - riverX) / 3.2f, 2)) * 0.45f);
            return ash;
        }

        /// <summary>Wind-blown dust (vertex B above 0.5 in the arid ground mode): ridges and the open plain, broken up by noise.</summary>
        private static float Dust(Vector3 p, Vector3 n, uint seed)
        {
            float plain = Noise.Fbm(p.X * 0.03f, p.Z * 0.03f, seed + 151, 3);
            float flat = Smooth(0.92f, 0.99f, n.Y) * Smooth(9f, 14f, Dist(p.X, p.Z, 0f, 0f));
            return 0.5f + (Smooth(0.45f, 0.7f, plain) * flat * 0.5f);
        }

        /// <summary>A worn dirt track from the Hub toward a site (tone only).</summary>
        private static float Track(float x, float z, float sx, float sz)
        {
            float len2 = (sx * sx) + (sz * sz);
            float t = Math.Max(0f, Math.Min(1f, ((x * sx) + (z * sz)) / Math.Max(1f, len2)));
            float dx = x - (sx * t);
            float dz = z - (sz * t);
            float wobble = (float)Math.Sin(t * 9f + sx) * 0.6f;
            float d = (float)Math.Sqrt((dx * dx) + (dz * dz)) - wobble;
            return (float)Math.Exp(-Math.Pow(d / 0.9f, 2)) * Smooth(0f, 0.1f, t) * Smooth(1f, 0.9f, t);
        }

        private static float RawPadLevel(float x, float z, uint seed)
        {
            float h = (Noise.Fbm(x * 0.018f, z * 0.018f, seed + 101, 4) - 0.5f) * 14f;
            h += Smooth(20f, 70f, z + (x * 0.4f)) * 5f;
            return h;
        }

        private static float Dist(float x, float z, float cx, float cz)
        {
            return (float)Math.Sqrt(((x - cx) * (x - cx)) + ((z - cz) * (z - cz)));
        }

        internal static float Smooth(float edge0, float edge1, float x)
        {
            float t = Math.Max(0f, Math.Min(1f, (x - edge0) / (edge1 - edge0)));
            return t * t * (3f - (2f * t));
        }
    }
}
