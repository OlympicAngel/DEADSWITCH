namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Turns per-hour rates into exact per-tick amounts (SPEC-002 rule 1). Over every game hour the
    /// delivered total is exactly the rate, spread evenly, with no remainder state: the amount depends
    /// only on the tick, so chunked runs and save/load are unaffected.
    /// </summary>
    public static class Rates
    {
        public static int PerTick(int perHour, long tick)
        {
            long k = tick % SimConfig.TicksPerHour;
            if (k < 0)
            {
                k += SimConfig.TicksPerHour;
            }

            long r = perHour;
            return (int)(((r * (k + 1)) / SimConfig.TicksPerHour) - ((r * k) / SimConfig.TicksPerHour));
        }
    }
}
