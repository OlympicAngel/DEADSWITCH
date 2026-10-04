using System;
using System.IO;
using Deadswitch.Sim.Config;

namespace Deadswitch.Sim.Tests
{
    /// <summary>Configs used by tests. Feel guards run against both the doc 10 baseline and the shipped balance file.</summary>
    public static class TestConfigs
    {
        public const string Defaults = "defaults";
        public const string Shipped = "shipped";

        public static string ShippedPath
        {
            get
            {
                DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null)
                {
                    string candidate = Path.Combine(dir.FullName, "src", "Deadswitch.Sim", "Resources", BalanceText.FileName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }

                    dir = dir.Parent;
                }

                throw new FileNotFoundException("Shipped balance file not found above " + AppContext.BaseDirectory);
            }
        }

        public static SimConfig Get(string name)
        {
            return name == Shipped ? BalanceText.Parse(File.ReadAllText(ShippedPath)) : SimConfig.Tier1();
        }
    }
}
