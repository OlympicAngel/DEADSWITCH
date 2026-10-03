using System.Linq;
using Deadswitch.Sim.Config;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    public class BalanceConfigTests
    {
        private static string DefaultText => BalanceText.Write(SimConfig.Tier1());

        [Fact]
        public void DefaultsRoundTripWithoutIssues()
        {
            BalanceReadResult result = BalanceText.Read(DefaultText, BalanceReadMode.Strict);

            Assert.Empty(result.Issues);
            Assert.Equal(SimConfig.Tier1().ComputeHash(), result.Config.ComputeHash());
        }

        [Fact]
        public void EditedValueIsApplied()
        {
            string text = DefaultText.Replace("gen_per_tick = 8", "gen_per_tick = 11");

            SimConfig config = BalanceText.Parse(text);

            Assert.Equal(11, config.Energy.GenPerTick);
        }

        [Fact]
        public void CommentsWhitespaceAndUnderscoresAreAccepted()
        {
            string text = DefaultText.Replace("cap = 500", "  cap   =   1_500   # trailing comment");

            SimConfig config = BalanceText.Parse(text);

            Assert.Equal(1500, config.Energy.Cap);
        }

        [Fact]
        public void UnknownKey_IsAnErrorWithLineNumber()
        {
            string text = DefaultText.Replace("[energy]\n", "[energy]\ngen_per_tik = 9\n");
            int expectedLine = text.Split('\n').ToList().FindIndex(l => l.StartsWith("gen_per_tik", System.StringComparison.Ordinal)) + 1;

            BalanceReadResult result = BalanceText.Read(text, BalanceReadMode.Strict);

            ConfigIssue issue = Assert.Single(result.Issues);
            Assert.Equal(ConfigIssueSeverity.Error, issue.Severity);
            Assert.Equal("energy.gen_per_tik", issue.Key);
            Assert.Equal(expectedLine, issue.Line);
        }

        [Fact]
        public void DuplicateKey_IsAnError()
        {
            string text = DefaultText.Replace("[energy]\n", "[energy]\ncap = 9\n");

            BalanceReadResult result = BalanceText.Read(text, BalanceReadMode.Strict);

            Assert.Contains(result.Issues, i => i.Key == "energy.cap" && i.Message.Contains("duplicate"));
        }

        [Fact]
        public void MissingKey_StrictIsError_LenientKeepsDefault()
        {
            string text = DefaultText.Replace("gen_per_tick = 8\n", string.Empty);

            BalanceReadResult strict = BalanceText.Read(text, BalanceReadMode.Strict);
            BalanceReadResult lenient = BalanceText.Read(text, BalanceReadMode.Lenient);

            Assert.True(strict.HasErrors);
            Assert.False(lenient.HasErrors);
            ConfigIssue warning = Assert.Single(lenient.Issues);
            Assert.Equal("energy.gen_per_tick", warning.Key);
            Assert.Equal(new SimConfig().Energy.GenPerTick, lenient.Config.Energy.GenPerTick);
        }

        [Theory]
        [InlineData("gen_per_tick = -1", "out of range")]
        [InlineData("gen_per_tick = 10001", "out of range")]
        [InlineData("gen_per_tick = eight", "not an integer")]
        [InlineData("gen_per_tick = 8.5", "not an integer")]
        [InlineData("gen_per_tick = 99999999999", "not an integer")]
        [InlineData("gen_per_tick = _8", "not an integer")]
        public void InvalidValue_IsAnErrorAndKeepsDefault(string line, string expected)
        {
            string text = DefaultText.Replace("gen_per_tick = 8", line);

            BalanceReadResult result = BalanceText.Read(text, BalanceReadMode.Lenient);

            ConfigIssue issue = Assert.Single(result.Issues);
            Assert.Equal(ConfigIssueSeverity.Error, issue.Severity);
            Assert.Contains(expected, issue.Message);
            Assert.Equal(8, result.Config.Energy.GenPerTick);
        }

        [Theory]
        [InlineData("just some words")]
        [InlineData("[Energy]")]
        [InlineData("[energy")]
        [InlineData("Bad-Key = 1")]
        public void MalformedLines_AreErrors(string line)
        {
            string text = DefaultText + "\n" + line + "\n";

            BalanceReadResult result = BalanceText.Read(text, BalanceReadMode.Strict);

            Assert.True(result.HasErrors);
        }

        [Fact]
        public void KeyBeforeAnySection_IsAnError()
        {
            BalanceReadResult result = BalanceText.Read("cap = 1\n" + DefaultText, BalanceReadMode.Strict);

            Assert.Contains(result.Issues, i => i.Message.Contains("outside of any [section]"));
        }

        [Fact]
        public void Parse_ThrowsWithEveryIssueInTheMessage()
        {
            string text = DefaultText.Replace("gen_per_tick = 8", "gen_per_tick = x").Replace("[fuel]\n", "[fuel]\nnope = 1\n");

            var ex = Assert.Throws<BalanceConfigException>(() => BalanceText.Parse(text));

            Assert.Equal(2, ex.Issues.Count);
            Assert.Contains("energy.gen_per_tick", ex.Message);
            Assert.Contains("fuel.nope", ex.Message);
        }

        [Fact]
        public void WindowsLineEndingsAreAccepted()
        {
            string text = DefaultText.Replace("\n", "\r\n");

            BalanceReadResult result = BalanceText.Read(text, BalanceReadMode.Strict);

            Assert.Empty(result.Issues);
        }

        [Fact]
        public void Hash_IsStableForClones_AndChangesWithAnyValue()
        {
            SimConfig a = SimConfig.Tier1();
            SimConfig b = a.Clone();
            Assert.Equal(a.ComputeHash(), b.ComputeHash());

            b.Raid.LootCap++;
            Assert.NotEqual(a.ComputeHash(), b.ComputeHash());
        }

        [Fact]
        public void Clone_IsIndependent()
        {
            SimConfig a = SimConfig.Tier1();
            SimConfig b = a.Clone();

            b.Energy.Cap = 1;

            Assert.Equal(500, a.Energy.Cap);
        }

        [Fact]
        public void EveryKeyIsUniqueAndDocumented()
        {
            var keys = new KeyCollector();
            SimConfig.Tier1().Visit(keys);

            Assert.Equal(keys.Keys.Count, keys.Keys.Distinct().Count());
            Assert.All(keys.Descriptions, d => Assert.False(string.IsNullOrWhiteSpace(d)));
        }

        private sealed class KeyCollector : IConfigVisitor
        {
            private string _section = string.Empty;

            public System.Collections.Generic.List<string> Keys { get; } = new System.Collections.Generic.List<string>();

            public System.Collections.Generic.List<string> Descriptions { get; } = new System.Collections.Generic.List<string>();

            public void BeginSection(string name, string description)
            {
                _section = name;
                Descriptions.Add(description);
            }

            public void EndSection()
            {
            }

            public void Int(string key, ref int value, int min, int max, string description)
            {
                Add(key, description);
            }

            public void Bool(string key, ref bool value, string description)
            {
                Add(key, description);
            }

            public void IntList(string key, ref int[] values, int min, int max, int minCount, int maxCount, string description)
            {
                Add(key, description);
            }

            private void Add(string key, string description)
            {
                Keys.Add(_section + "." + key);
                Descriptions.Add(description);
            }
        }
    }
}
