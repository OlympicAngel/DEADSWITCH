using System;
using Deadswitch.Sim.Systems;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    public class FixedMathTests
    {
        [Fact]
        public void PowPermille_MatchesRealPow_OverGameRanges()
        {
            foreach (int p in new[] { 500, 700, 1000, 1500 })
            {
                for (long x = 1; x < 2_000_000; x = (x * 3 / 2) + 1)
                {
                    double expected = Math.Pow(x, p / 1000.0);
                    long actual = FixedMath.PowPermille(x, p);
                    Assert.True(Math.Abs(actual - expected) <= Math.Max(1.0, expected * 0.0005), "x=" + x + " p=" + p + " got " + actual + " want " + expected);
                }
            }
        }
    }
}
