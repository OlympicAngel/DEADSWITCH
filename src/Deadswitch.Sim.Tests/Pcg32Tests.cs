using Deadswitch.Sim.Rng;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    public class Pcg32Tests
    {
        [Fact]
        public void MatchesReferenceVector_Seed42_Stream54()
        {
            var rng = Pcg32.Create(42UL, 54UL);
            uint[] expected = { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };
            foreach (uint e in expected)
            {
                Assert.Equal(e, rng.NextUInt());
            }
        }

        [Fact]
        public void RestoreContinuesTheSameStream()
        {
            var a = Pcg32.Create(7UL);
            for (int i = 0; i < 100; i++)
            {
                a.NextUInt();
            }

            var b = Pcg32.Restore(a.State, a.Inc);
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(a.NextUInt(), b.NextUInt());
            }
        }

        [Fact]
        public void NextBelowStaysInRange()
        {
            var rng = Pcg32.Create(1UL);
            for (int i = 0; i < 10_000; i++)
            {
                Assert.True(rng.NextBelow(7) < 7);
            }
        }
    }
}
