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
    }
}
