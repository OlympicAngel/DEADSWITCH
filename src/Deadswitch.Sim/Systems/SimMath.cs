namespace Deadswitch.Sim.Systems
{
    /// <summary>Small integer helpers shared by systems. Rounding is always explicit.</summary>
    public static class SimMath
    {
        public static int Clamp(int v, int lo, int hi)
        {
            return v < lo ? lo : (v > hi ? hi : v);
        }

        /// <summary>value * pct / 100, rounded down (non-negative inputs).</summary>
        public static int PctFloor(int value, int pct)
        {
            return (int)(((long)value * pct) / 100);
        }

        /// <summary>value * pct / 100, rounded up (non-negative inputs).</summary>
        public static int PctCeil(int value, int pct)
        {
            return (int)((((long)value * pct) + 99) / 100);
        }

        /// <summary>Deterministic 32-bit mix of two values (murmur3 finalizer); for decisions that must not draw RNG.</summary>
        public static uint Hash(uint a, uint b)
        {
            unchecked
            {
                uint h = (a * 0x9E3779B1u) ^ (b + 0x7F4A7C15u + (a << 6) + (a >> 2));
                h ^= h >> 16;
                h *= 0x85EBCA6Bu;
                h ^= h >> 13;
                h *= 0xC2B2AE35u;
                h ^= h >> 16;
                return h;
            }
        }
    }
}
