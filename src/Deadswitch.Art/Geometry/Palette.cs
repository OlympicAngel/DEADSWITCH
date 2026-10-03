using System.Numerics;

namespace Deadswitch.Art.Geometry
{
    /// <summary>PBR look of one material slot. Colors are sRGB 0..1; emission is sRGB color x intensity.</summary>
    public readonly struct MaterialDef
    {
        public MaterialDef(string name, uint rgb, float metallic, float smoothness, uint emissionRgb = 0, float emission = 0f, bool isLamp = false)
        {
            Name = name;
            BaseColor = Rgb(rgb);
            Metallic = metallic;
            Smoothness = smoothness;
            EmissionColor = Rgb(emissionRgb);
            EmissionIntensity = emission;
            IsLamp = isLamp;
        }

        public string Name { get; }

        public Vector3 BaseColor { get; }

        public float Metallic { get; }

        public float Smoothness { get; }

        public Vector3 EmissionColor { get; }

        public float EmissionIntensity { get; }

        /// <summary>Lamps switch off when their facility is unpowered.</summary>
        public bool IsLamp { get; }

        private static Vector3 Rgb(uint rgb)
        {
            return new Vector3(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
        }
    }

    /// <summary>
    /// The world's material palette (ADR-0007: grounded PBR, cold desaturated world, warm localized light).
    /// All values are provisional (tune) and shared by Unity and the preview so both look the same.
    /// </summary>
    public static class Palette
    {
        public static readonly int Count = 18;

        private static readonly MaterialDef[] Defs =
        {
            new MaterialDef("Concrete", 0x8C8A82, 0f, 0.18f),
            new MaterialDef("ConcreteDark", 0x5B5A55, 0f, 0.14f),
            new MaterialDef("Rust", 0x7A4A30, 0.55f, 0.28f),
            new MaterialDef("OliveSteel", 0x56603F, 0.45f, 0.38f),
            new MaterialDef("SandSteel", 0x9C8D6A, 0.4f, 0.34f),
            new MaterialDef("DarkSteel", 0x34383A, 0.75f, 0.42f),
            new MaterialDef("Tarp", 0x5E6B4E, 0f, 0.22f),
            new MaterialDef("Sandbag", 0x9A8A66, 0f, 0.08f),
            new MaterialDef("Wood", 0x6B5038, 0f, 0.2f),
            new MaterialDef("Rubber", 0x1F2122, 0f, 0.3f),
            new MaterialDef("Ground", 0x4A443A, 0f, 0.06f),
            new MaterialDef("Paint", 0xC9A443, 0.1f, 0.3f),
            new MaterialDef("Copper", 0xB06A3E, 0.9f, 0.55f),
            new MaterialDef("LampAmber", 0x3A2A12, 0f, 0.6f, 0xFFB347, 6f, true),
            new MaterialDef("LampPhosphor", 0x1E2A18, 0f, 0.6f, 0xA8D58A, 5f, true),
            new MaterialDef("LampRed", 0x2A1210, 0f, 0.6f, 0xFF5A48, 5f, true),
            new MaterialDef("Screen", 0x0E1612, 0.2f, 0.85f, 0x7FD0A0, 2.5f, true),
            new MaterialDef("Glass", 0x2B3A40, 0.1f, 0.9f),
        };

        public static MaterialDef Get(Mat m)
        {
            return Defs[(int)m];
        }
    }
}
