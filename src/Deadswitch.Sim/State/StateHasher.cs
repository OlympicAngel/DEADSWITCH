namespace Deadswitch.Sim.State
{
    /// <summary>FNV-1a 64-bit over every field of GameState. Used for determinism tests and later server verification.</summary>
    public static class StateHasher
    {
        private const ulong Offset = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        public static ulong Hash(GameState s)
        {
            ulong h = Offset;
            h = Mix(h, unchecked((ulong)s.Tick));
            h = Mix(h, unchecked((ulong)s.Energy));
            h = Mix(h, unchecked((ulong)s.Fuel));
            h = Mix(h, unchecked((ulong)s.Compute));
            h = Mix(h, unchecked((ulong)s.People));
            h = Mix(h, unchecked((ulong)s.Corruption));
            h = Mix(h, unchecked((ulong)s.RaidsToday));
            h = Mix(h, (ulong)s.Delegation);
            h = Mix(h, s.Rng.State);
            h = Mix(h, s.Rng.Inc);
            return h;
        }

        private static ulong Mix(ulong h, ulong v)
        {
            unchecked
            {
                for (int i = 0; i < 8; i++)
                {
                    h ^= (v >> (i * 8)) & 0xFFUL;
                    h *= Prime;
                }

                return h;
            }
        }
    }
}
