namespace Deadswitch.Art.Geometry
{
    /// <summary>Material slots. Every mesh draws with these; <see cref="Palette"/> defines how each looks.</summary>
    public enum Mat
    {
        Concrete = 0,
        ConcreteDark = 1,
        Rust = 2,
        OliveSteel = 3,
        SandSteel = 4,
        DarkSteel = 5,
        Tarp = 6,
        Sandbag = 7,
        Wood = 8,
        Rubber = 9,
        Ground = 10,
        Paint = 11,
        Copper = 12,
        LampAmber = 13,
        LampPhosphor = 14,
        LampRed = 15,
        Screen = 16,
        Glass = 17,
        TarpBlue = 18,
        PaintRed = 19,
        PaintWhite = 20,
        PaintGreen = 21,
        Foliage = 22,
        Water = 23,
        Skin = 24,
        Interior = 25,
        Rock = 26,
        LightCone = 27,

        /// <summary>Soot and burnt metal (battle scars).</summary>
        Char = 28,

        /// <summary>Glowing embers in burning wreckage (emissive).</summary>
        Ember = 29,

        /// <summary>The Church of the Last Signal's magenta signal lamps (emissive).</summary>
        LampMagenta = 30,

        /// <summary>Halcyon Dynamics' cold white visors (emissive).</summary>
        LampCold = 31,

        /// <summary>Thin salvaged neon tubes and signs on the base (emissive, cool cyan accent).</summary>
        NeonCyan = 32,

        /// <summary>Dark recessed bunker portal; its phosphor glow reads only at night (emissive).</summary>
        Portal = 33,
    }
}
