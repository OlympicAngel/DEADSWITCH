using System.Numerics;

namespace Deadswitch.Art.Geometry
{
    /// <summary>
    /// PBR look of one material slot plus its procedural wear (doc 11 Master art direction): the shader layers
    /// chipped paint over a bare substrate, rust, dirt and mud from the ground up, dust on top faces, rain streaks
    /// on walls, and a height bump. Colors are sRGB 0..1; emission is sRGB color x intensity.
    /// </summary>
    public readonly struct MaterialDef
    {
        public MaterialDef(string name, uint rgb, float metallic, float smoothness, Wear wear, uint emissionRgb = 0, float emission = 0f, bool isLamp = false)
        {
            Name = name;
            BaseColor = Rgb(rgb);
            Metallic = metallic;
            Smoothness = smoothness;
            Wear = wear;
            EmissionColor = Rgb(emissionRgb);
            EmissionIntensity = emission;
            IsLamp = isLamp;
        }

        public string Name { get; }

        public Vector3 BaseColor { get; }

        public float Metallic { get; }

        public float Smoothness { get; }

        public Wear Wear { get; }

        public Vector3 EmissionColor { get; }

        public float EmissionIntensity { get; }

        /// <summary>Lamps switch off when their facility is unpowered.</summary>
        public bool IsLamp { get; }

        internal static Vector3 Rgb(uint rgb)
        {
            return new Vector3(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
        }
    }

    /// <summary>Procedural wear amounts (0..1) and the substrate revealed under chipped paint.</summary>
    public readonly struct Wear
    {
        public Wear(float chip, float rust, float dirt, float streak, float bump, float scale, uint bare, bool ground = false)
        {
            Chip = chip;
            Rust = rust;
            Dirt = dirt;
            Streak = streak;
            Bump = bump;
            Scale = scale;
            Bare = MaterialDef.Rgb(bare);
            Ground = ground;
        }

        public static Wear None => new Wear(0, 0, 0, 0, 0, 1, 0x808080);

        public float Chip { get; }

        public float Rust { get; }

        public float Dirt { get; }

        public float Streak { get; }

        public float Bump { get; }

        /// <summary>Noise frequency (per meter).</summary>
        public float Scale { get; }

        public Vector3 Bare { get; }

        /// <summary>Terrain mode: mud, gravel, ruts and wet patches instead of object wear.</summary>
        public bool Ground { get; }
    }

    /// <summary>
    /// The world's material palette (ADR-0007: grounded PBR, weathered but colored world, warm localized light).
    /// All values are provisional (tune) and shared by Unity and the preview so both look the same.
    /// </summary>
    public static class Palette
    {
        public static int Count => Defs.Length;

        // Wear presets: chip, rust, dirt, streak, bump, scale, bare substrate.
        private static readonly Wear ConcreteWear = new Wear(0.15f, 0.12f, 0.75f, 0.85f, 0.65f, 0.9f, 0x4E4C47);
        private static readonly Wear PaintedSteel = new Wear(0.55f, 0.45f, 0.65f, 0.6f, 0.35f, 1.3f, 0x3A3633);
        private static readonly Wear BareSteel = new Wear(0.2f, 0.4f, 0.55f, 0.35f, 0.3f, 1.6f, 0x2A2826);
        private static readonly Wear Fabric = new Wear(0f, 0f, 0.7f, 0.35f, 0.35f, 2.2f, 0x3A3A33);
        private static readonly Wear Markings = new Wear(0.5f, 0.3f, 0.5f, 0.5f, 0.25f, 1.5f, 0x4A4640);

        private static readonly MaterialDef[] Defs =
        {
            new MaterialDef("Concrete", 0x827C70, 0f, 0.14f, ConcreteWear),
            new MaterialDef("ConcreteDark", 0x5D5851, 0f, 0.12f, ConcreteWear),
            new MaterialDef("Rust", 0x7C4628, 0.35f, 0.18f, new Wear(0.1f, 1f, 0.55f, 0.4f, 0.75f, 1.4f, 0x2E241E)),
            new MaterialDef("OliveSteel", 0x4E5D38, 0.25f, 0.3f, PaintedSteel),
            new MaterialDef("SandSteel", 0x907E56, 0.25f, 0.3f, PaintedSteel),
            new MaterialDef("DarkSteel", 0x30373F, 0.6f, 0.34f, BareSteel),
            new MaterialDef("Tarp", 0x566536, 0f, 0.2f, Fabric),
            new MaterialDef("Sandbag", 0x7C6C48, 0f, 0.08f, new Wear(0f, 0f, 0.85f, 0.2f, 0.6f, 3f, 0x5A5040)),
            new MaterialDef("Wood", 0x6C4A2A, 0f, 0.16f, new Wear(0f, 0f, 0.55f, 0.35f, 0.6f, 2.4f, 0x3A2C20)),
            new MaterialDef("Rubber", 0x1D1F20, 0f, 0.3f, new Wear(0f, 0f, 0.6f, 0f, 0.2f, 2f, 0x1D1F20)),
            new MaterialDef("Ground", 0x4F4331, 0f, 0.1f, new Wear(0f, 0f, 0f, 0f, 0.7f, 0.6f, 0x2E2820, ground: true)),
            new MaterialDef("Paint", 0xA68A36, 0.1f, 0.26f, Markings),
            new MaterialDef("Copper", 0xC0703A, 0.85f, 0.5f, new Wear(0.1f, 0.2f, 0.3f, 0.2f, 0.2f, 2f, 0x3F7A5E)),
            new MaterialDef("LampAmber", 0x3A2A12, 0f, 0.6f, Wear.None, 0xFFB347, 6f, true),
            new MaterialDef("LampPhosphor", 0x1E2A18, 0f, 0.6f, Wear.None, 0xA8D58A, 5f, true),
            new MaterialDef("LampRed", 0x2A1210, 0f, 0.6f, Wear.None, 0xFF5A48, 5f, true),
            new MaterialDef("Screen", 0x0E1612, 0.2f, 0.85f, Wear.None, 0x4FC27A, 1.3f, true),
            new MaterialDef("Glass", 0x243A4C, 0.1f, 0.85f, new Wear(0f, 0f, 0.45f, 0.5f, 0f, 2f, 0x26323A)),
            new MaterialDef("TarpBlue", 0x2C5A80, 0f, 0.22f, Fabric),
            new MaterialDef("PaintRed", 0xA6352A, 0.05f, 0.28f, Markings),
            new MaterialDef("PaintWhite", 0xB5B0A2, 0.05f, 0.26f, Markings),
            new MaterialDef("PaintGreen", 0x4A8656, 0.05f, 0.3f, Markings),
            new MaterialDef("Foliage", 0x2E4224, 0f, 0.1f, new Wear(0f, 0f, 0.2f, 0f, 0.6f, 3f, 0x2C3527)),
            new MaterialDef("Water", 0x3A4446, 0.2f, 0.96f, Wear.None),
            new MaterialDef("Skin", 0x9C7860, 0f, 0.3f, Wear.None),
            new MaterialDef("Interior", 0x3A2E1E, 0f, 0.4f, Wear.None, 0xFFB86A, 1.6f, true),
            new MaterialDef("Rock", 0x5A564E, 0f, 0.12f, new Wear(0f, 0.05f, 0.45f, 0.3f, 0.9f, 1.1f, 0x46433D)),
            new MaterialDef("LightCone", 0x000000, 0f, 0f, Wear.None, 0xFFC27A, 0.22f, true),
            new MaterialDef("Char", 0x1E1C1A, 0.1f, 0.08f, new Wear(0f, 0.3f, 0.5f, 0.6f, 0.7f, 1.8f, 0x2E2A26)),
            new MaterialDef("Ember", 0x2A140A, 0f, 0.3f, Wear.None, 0xFF6A1E, 7f, false),
            new MaterialDef("LampMagenta", 0x2A1226, 0f, 0.6f, Wear.None, 0xE07AD2, 5f, true),
            new MaterialDef("LampCold", 0x1E2428, 0f, 0.7f, Wear.None, 0xD8F0FF, 5f, true),
            new MaterialDef("NeonCyan", 0x16302E, 0f, 0.7f, Wear.None, 0x3FE6D8, 4.5f, true),
            new MaterialDef("Portal", 0x090C0A, 0.1f, 0.2f, Wear.None, 0x5FC48A, 0.5f, true),
            new MaterialDef("MapGround", 0x5A4D3A, 0f, 0.08f, new Wear(0f, 0f, 0f, 1f, 0.5f, 0.3f, 0x2E2820, ground: true)),
            new MaterialDef("SolarCell", 0x1B3C78, 0.3f, 0.82f, new Wear(0f, 0f, 0.35f, 0.3f, 0f, 2f, 0x1B3C78)),
            new MaterialDef("PaintBlue", 0x2A5C96, 0.05f, 0.3f, Markings),
            new MaterialDef("PaintYellow", 0xC09A2A, 0.05f, 0.3f, Markings),
        };

        public static MaterialDef Get(Mat m)
        {
            return Defs[(int)m];
        }
    }
}
