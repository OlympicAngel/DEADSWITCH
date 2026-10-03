using System;

namespace Deadswitch.Sim.Rng
{
    /// <summary>
    /// PCG32 (XSH-RR). Deterministic across platforms. The ONLY randomness source allowed in the sim.
    /// Verified against the reference vector (seed 42, stream 54) in Pcg32Tests.
    /// </summary>
    public sealed class Pcg32
    {
        private const ulong Multiplier = 6364136223846793005UL;
        private ulong _state;
        private readonly ulong _inc;

        private Pcg32(ulong state, ulong inc)
        {
            _state = state;
            _inc = inc;
        }

        public ulong State => _state;

        public ulong Inc => _inc;

        public static Pcg32 Create(ulong seed, ulong stream = 54UL)
        {
            var rng = new Pcg32(0UL, unchecked((stream << 1) | 1UL));
            rng.NextUInt();
            rng._state = unchecked(rng._state + seed);
            rng.NextUInt();
            return rng;
        }

        /// <summary>Restore from a snapshot (state + inc).</summary>
        public static Pcg32 Restore(ulong state, ulong inc)
        {
            return new Pcg32(state, inc);
        }

        public uint NextUInt()
        {
            unchecked
            {
                ulong old = _state;
                _state = (old * Multiplier) + _inc;
                uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
                int rot = (int)(old >> 59);
                return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
            }
        }

        /// <summary>Unbiased integer in [0, bound).</summary>
        public uint NextBelow(uint bound)
        {
            if (bound == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bound), "bound must be > 0");
            }

            uint threshold = unchecked(0u - bound) % bound;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold)
                {
                    return r % bound;
                }
            }
        }
    }
}
