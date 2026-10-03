using System.Collections.Generic;
using System.Numerics;
using Deadswitch.Art.Geometry;

namespace Deadswitch.Art.Models
{
    public enum AnimKind
    {
        None = 0,

        /// <summary>Continuous spin around local Y (fans, rotors).</summary>
        SpinY = 1,

        /// <summary>Back-and-forth sweep around local Y (turret heads, dishes).</summary>
        SweepY = 2,

        /// <summary>Continuous spin around local Z (wind turbine rotors facing -Z).</summary>
        SpinZ = 3,
    }

    /// <summary>A moving part: its own mesh around a pivot, animated only while the facility is powered.</summary>
    public sealed class AnimPart
    {
        public AnimPart(MeshData mesh, Vector3 pivot, AnimKind kind, float speed, float range = 0f)
        {
            Mesh = mesh;
            Pivot = pivot;
            Kind = kind;
            Speed = speed;
            Range = range;
        }

        public MeshData Mesh { get; }

        public Vector3 Pivot { get; }

        public AnimKind Kind { get; }

        /// <summary>Degrees per second (spin) or cycles per second (sweep).</summary>
        public float Speed { get; }

        /// <summary>Sweep half-angle in degrees.</summary>
        public float Range { get; }
    }

    public enum LightRole
    {
        /// <summary>Always on (perimeter lamps, the core's eye).</summary>
        Ambient = 0,

        /// <summary>On while the facility is powered.</summary>
        Status = 1,

        /// <summary>Blinks amber while the facility is unmanned (AI-run).</summary>
        Beacon = 2,
    }

    public readonly struct LightSpec
    {
        public LightSpec(Vector3 position, Vector3 color, float intensity, float range, LightRole role)
        {
            Position = position;
            Color = color;
            Intensity = intensity;
            Range = range;
            Role = role;
        }

        public Vector3 Position { get; }

        public Vector3 Color { get; }

        public float Intensity { get; }

        public float Range { get; }

        public LightRole Role { get; }
    }

    /// <summary>Everything one object needs: a static mesh, moving parts and point lights, in local space.</summary>
    public sealed class Model
    {
        public MeshData Static { get; set; } = new MeshData();

        public List<AnimPart> Parts { get; } = new List<AnimPart>();

        public List<LightSpec> Lights { get; } = new List<LightSpec>();

        public static readonly Vector3 Amber = new Vector3(1f, 0.7f, 0.32f);
        public static readonly Vector3 Phosphor = new Vector3(0.62f, 0.9f, 0.55f);
        public static readonly Vector3 Red = new Vector3(1f, 0.35f, 0.28f);
    }
}
