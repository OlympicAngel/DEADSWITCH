namespace Deadswitch.Sim.State
{
    /// <summary>
    /// FNV-1a 64-bit over every field of <see cref="GameState"/> (via <see cref="GameState.Visit"/>).
    /// Used by determinism tests, save verification and later server verification.
    /// </summary>
    public static class StateHasher
    {
        public static ulong Hash(GameState s)
        {
            var v = new Visitor();
            s.Visit(v);
            return v.Value;
        }

        private sealed class Visitor : IStateVisitor
        {
            private const ulong Prime = 1099511628211UL;
            private ulong _h = 14695981039346656037UL;

            public ulong Value => _h;

            public bool IsReading => false;

            public void Int(ref int value)
            {
                Mix(unchecked((ulong)(uint)value), 4);
            }

            public void Long(ref long value)
            {
                Mix(unchecked((ulong)value), 8);
            }

            public void ULong(ref ulong value)
            {
                Mix(value, 8);
            }

            public void Bool(ref bool value)
            {
                Mix(value ? 1UL : 0UL, 1);
            }

            public int Count(int count)
            {
                Mix(unchecked((ulong)(uint)count), 4);
                return count;
            }

            private void Mix(ulong v, int bytes)
            {
                unchecked
                {
                    for (int i = 0; i < bytes; i++)
                    {
                        _h ^= (v >> (i * 8)) & 0xFFUL;
                        _h *= Prime;
                    }
                }
            }
        }
    }
}
