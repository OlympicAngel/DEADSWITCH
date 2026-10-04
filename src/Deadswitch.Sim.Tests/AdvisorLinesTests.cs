using System.Linq;
using Deadswitch.Host.Narrative;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    /// <summary>The shipped line table must parse cleanly and cover every trigger (a gap would silently mute the AI).</summary>
    public class AdvisorLinesTests
    {
        [Fact]
        public void ShippedLines_ParseCleanly_AndEveryTriggerHasANeutralLine()
        {
            AdvisorLines? lines = AdvisorLines.LoadEmbedded();

            Assert.NotNull(lines);
            Assert.Empty(lines!.Issues);
            Assert.True(lines.All.Count >= 50);
            foreach (string trigger in Advisor.Triggers)
            {
                Assert.True(lines.For(trigger).Any(l => l.Tone == Tone.Neutral), "no neutral line for " + trigger);
            }
        }
    }
}
