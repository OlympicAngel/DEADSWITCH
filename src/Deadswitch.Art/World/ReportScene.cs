using System;
using System.Collections.Generic;
using System.Numerics;
using Deadswitch.Art.Geometry;
using Deadswitch.Art.Models;
using Deadswitch.Sim.State;

namespace Deadswitch.Art.World
{
    /// <summary>A camera for one report panel (Unity world space).</summary>
    public readonly struct ShotSpec
    {
        public ShotSpec(Vector3 position, Vector3 target, float fov)
        {
            Position = position;
            Target = target;
            Fov = fov;
        }

        public Vector3 Position { get; }

        public Vector3 Target { get; }

        public float Fov { get; }
    }

    /// <summary>A raider figure placed for one panel.</summary>
    public readonly struct RaiderSpec
    {
        public RaiderSpec(Vector3 position, float yaw, uint seed)
        {
            Position = position;
            Yaw = yaw;
            Seed = seed;
        }

        public Vector3 Position { get; }

        public float Yaw { get; }

        public uint Seed { get; }
    }

    /// <summary>
    /// Battle report stills (SPEC-006 rule 6): four cinematic cameras around the gate a raid really used
    /// (approach from outside, contact at the wall, outcome, aftermath over the yard) and where the raiders stand.
    /// Shot index: 0 approach, 1 contact, 2 outcome, 3 aftermath.
    /// </summary>
    public static class ReportScene
    {
        public const int Shots = 4;

        /// <summary>Ground point at the middle of a gate and the unit direction pointing out of the compound.</summary>
        public static void Gate(RaidGate gate, uint seed, out Vector3 point, out Vector3 outward)
        {
            switch (gate)
            {
                case RaidGate.North:
                    point = new Vector3(3f, 0, Core.FacadeZ + 9f);
                    outward = Vector3.UnitZ;
                    break;
                case RaidGate.East:
                    point = new Vector3(HubScene.HalfWidth, 0, -4.5f);
                    outward = Vector3.UnitX;
                    break;
                case RaidGate.West:
                    point = new Vector3(-HubScene.HalfWidth, 0, -4.5f);
                    outward = -Vector3.UnitX;
                    break;
                default:
                    point = new Vector3(0, 0, HubScene.FenceZ);
                    outward = -Vector3.UnitZ;
                    break;
            }

            point = Ground(point, seed);
        }

        public static ShotSpec Shot(RaidGate gate, int shot, RaidOutcome outcome, uint seed)
        {
            Gate(gate, seed, out Vector3 g, out Vector3 o);
            Vector3 side = new Vector3(o.Z, 0, -o.X);
            switch (shot)
            {
                case 0:
                    return new ShotSpec(Ground(g + (o * 17f) + (side * 5f), seed) + Up(2.6f), Ground(g + (o * 5f), seed) + Up(1.4f), 36f);
                case 1:
                    return new ShotSpec(Ground(g - (o * 6f) - (side * 4f), seed) + Up(3.4f), Ground(g + (o * 3f), seed) + Up(1.0f), 42f);
                case 2:
                    return Inside(outcome)
                        ? new ShotSpec(Ground(g - (o * 13f) + (side * 3f), seed) + Up(4.6f), Ground(g - (o * 4f), seed) + Up(0.8f), 40f)
                        : new ShotSpec(Ground(g - (o * 1f) + (side * 4f), seed) + Up(4.8f), Ground(g + (o * 9f), seed) + Up(0.9f), 38f);
                default:
                    Vector3 yard = gate == RaidGate.North ? new Vector3(0, 6f, Core.FacadeZ - 2f) : new Vector3(0, 1f, -2f);
                    return new ShotSpec(Ground(g - (o * 5f) + (side * 8f), seed) + Up(13f), yard, 44f);
            }
        }

        /// <summary>Raiders for a panel: massing outside, at the wall, then inside (breach) or falling back.</summary>
        public static List<RaiderSpec> Raiders(RaidGate gate, int shot, RaidOutcome outcome, uint seed)
        {
            var list = new List<RaiderSpec>();
            if (shot == 3 || (outcome == RaidOutcome.Lockdown && shot == 2))
            {
                return list;
            }

            Gate(gate, seed, out Vector3 g, out Vector3 o);
            Vector3 side = new Vector3(o.Z, 0, -o.X);
            var rng = new ArtRandom(seed + (uint)(shot * 97));
            float near = shot == 0 ? 6f : 1.8f;
            float far = shot == 0 ? 12f : 4.5f;
            bool retreat = shot == 2 && !Inside(outcome);
            if (shot == 2 && Inside(outcome))
            {
                near = -7f;
                far = -2f;
            }
            else if (retreat)
            {
                near = 6f;
                far = 11f;
            }

            int count = shot == 0 ? 7 : 6;
            // Models face -Z; this yaw turns them toward the compound (against the outward direction).
            float facing = (float)(Math.Atan2(o.X, o.Z) * 180.0 / Math.PI);
            for (int i = 0; i < count; i++)
            {
                Vector3 p = g + (o * rng.Range(near, far)) + (side * rng.Range(-3.6f, 3.6f));
                float yaw = (retreat ? facing + 180f : facing) + rng.Range(-25f, 25f);
                list.Add(new RaiderSpec(Ground(p, seed), yaw, seed + 4000 + (uint)i));
            }

            return list;
        }

        private static bool Inside(RaidOutcome outcome)
        {
            return outcome == RaidOutcome.Breached;
        }

        private static Vector3 Up(float h)
        {
            return new Vector3(0, h, 0);
        }

        private static Vector3 Ground(Vector3 p, uint seed)
        {
            return new Vector3(p.X, HubScene.Height(p.X, p.Z, seed), p.Z);
        }
    }
}
