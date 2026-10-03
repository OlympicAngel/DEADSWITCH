using System;
using System.IO;
using Deadswitch.Sim.Config;

namespace Deadswitch.Cli
{
    /// <summary>Finds the shipped balance file by walking up from the working directory and the binary.</summary>
    public static class BalanceFile
    {
        public static readonly string RelativePath = Path.Combine("src", "Deadswitch.Sim", "Resources", BalanceText.FileName);

        public static string LocateShipped()
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                DirectoryInfo? dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    string candidate = Path.Combine(dir.FullName, RelativePath);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }

                    dir = dir.Parent;
                }
            }

            throw new FileNotFoundException("Shipped balance file not found (" + RelativePath + "). Pass --config PATH.");
        }
    }
}
