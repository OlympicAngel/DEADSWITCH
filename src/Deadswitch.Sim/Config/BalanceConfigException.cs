using System;
using System.Collections.Generic;
using System.Text;

namespace Deadswitch.Sim.Config
{
    /// <summary>Thrown by <see cref="BalanceText.Parse"/> when a balance file has errors. The message lists every issue.</summary>
    public sealed class BalanceConfigException : Exception
    {
        public BalanceConfigException(IReadOnlyList<ConfigIssue> issues)
            : base(Format(issues))
        {
            Issues = issues;
        }

        public IReadOnlyList<ConfigIssue> Issues { get; }

        private static string Format(IReadOnlyList<ConfigIssue> issues)
        {
            var sb = new StringBuilder("Invalid balance file:");
            foreach (ConfigIssue issue in issues)
            {
                sb.Append("\n  ").Append(issue.ToString());
            }

            return sb.ToString();
        }
    }
}
