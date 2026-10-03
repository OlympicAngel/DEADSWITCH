namespace Deadswitch.Sim.Config
{
    /// <summary>
    /// FNV-1a 64 over every key and value in visit order. Saves and replays store it so a run can be
    /// re-simulated with the exact config that produced it (ADR-0007).
    /// </summary>
    public static class ConfigHasher
    {
        public static ulong Hash(SimConfig config)
        {
            var visitor = new Visitor();
            config.Visit(visitor);
            return visitor.Value;
        }

        private sealed class Visitor : IConfigVisitor
        {
            private const ulong Prime = 1099511628211UL;
            private ulong _h = 14695981039346656037UL;

            public ulong Value => _h;

            public void BeginSection(string name, string description)
            {
                MixString("[" + name + "]");
            }

            public void EndSection()
            {
            }

            public void Int(string key, ref int value, int min, int max, string description)
            {
                MixString(key);
                MixInt(value);
            }

            public void Bool(string key, ref bool value, string description)
            {
                MixString(key);
                MixInt(value ? 1 : 0);
            }

            public void IntList(string key, ref int[] values, int min, int max, int minCount, int maxCount, string description)
            {
                MixString(key);
                MixInt(values.Length);
                foreach (int v in values)
                {
                    MixInt(v);
                }
            }

            private void MixString(string s)
            {
                foreach (char c in s)
                {
                    MixByte((byte)(c & 0xFF));
                    MixByte((byte)(c >> 8));
                }

                MixByte(0);
            }

            private void MixInt(int v)
            {
                unchecked
                {
                    uint u = (uint)v;
                    MixByte((byte)u);
                    MixByte((byte)(u >> 8));
                    MixByte((byte)(u >> 16));
                    MixByte((byte)(u >> 24));
                }
            }

            private void MixByte(byte b)
            {
                unchecked
                {
                    _h ^= b;
                    _h *= Prime;
                }
            }
        }
    }
}
