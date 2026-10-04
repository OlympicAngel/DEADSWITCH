namespace Deadswitch.Art.Geometry
{
    /// <summary>Small deterministic RNG for art variation (never used by game rules).</summary>
    public sealed class ArtRandom
    {
        private uint _s;

        public ArtRandom(uint seed)
        {
            _s = seed == 0 ? 0x9E3779B9u : seed;
        }

        public uint NextUInt()
        {
            _s ^= _s << 13;
            _s ^= _s >> 17;
            _s ^= _s << 5;
            return _s;
        }

        /// <summary>0..1.</summary>
        public float Next()
        {
            return (NextUInt() & 0xFFFFFF) / 16777216f;
        }

        public float Range(float lo, float hi)
        {
            return lo + ((hi - lo) * Next());
        }

        public int Range(int lo, int hiExclusive)
        {
            return lo + (int)(NextUInt() % (uint)(hiExclusive - lo));
        }

        public static uint Hash(uint a, uint b)
        {
            uint h = (a * 0x85EBCA6Bu) ^ (b * 0xC2B2AE35u);
            h ^= h >> 16;
            h *= 0x7FEB352Du;
            h ^= h >> 15;
            return h == 0 ? 1u : h;
        }
    }
}
