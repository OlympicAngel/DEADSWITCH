using System.IO;
using Deadswitch.Sim.Config;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    public class ShippedBalanceTests
    {
        [Fact]
        public void ShippedFile_LoadsStrictlyWithNoIssues()
        {
            BalanceReadResult result = BalanceText.Read(File.ReadAllText(TestConfigs.ShippedPath), BalanceReadMode.Strict);

            Assert.True(result.Issues.Count == 0, string.Join("\n", result.Issues));
        }

        [Fact]
        public void ShippedFile_IsNormalized()
        {
            // The file must stay in writer format so comments and ranges never go stale.
            // After editing values by hand this still passes; after adding keys run: config dump --out <file>.
            string text = File.ReadAllText(TestConfigs.ShippedPath).Replace("\r\n", "\n");
            SimConfig config = BalanceText.Parse(text);

            Assert.Equal(BalanceText.Write(config), text);
        }
    }
}
