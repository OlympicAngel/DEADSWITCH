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
    }
}
