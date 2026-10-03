using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Deadswitch.Host.Persistence;
using Deadswitch.Sim;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Cli
{
    /// <summary>
    /// Headless tools for balancing and verification.
    /// <code>
    /// run [--seed N] [--hours N] [--config PATH] [--load SAVE] [--save SAVE]
    ///                                              simulate and print the end state (default: shipped balance file)
    /// config dump [--defaults] [--out PATH]        print the shipped balance file normalized, or the code defaults
    /// config check [PATH]                          strictly validate a balance file
    /// config diff [PATH]                           list values that differ from the code defaults (doc 10 baseline)
    /// [seed] [hours]                               legacy shorthand for run
    /// </code>
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0 || char.IsDigit(args[0][0]))
                {
                    return Run(LegacyArgs(args));
                }

                string[] rest = args.Skip(1).ToArray();
                switch (args[0])
                {
                    case "run":
                        return Run(Options.Parse(rest));
                    case "config":
                        return ConfigCommand(rest);
                    case "help":
                    case "--help":
                    case "-h":
                        return Usage(0);
                    default:
                        Console.Error.WriteLine("Unknown command '" + args[0] + "'.");
                        return Usage(2);
                }
            }
            catch (UsageException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return Usage(2);
            }
            catch (BalanceConfigException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static int Usage(int code)
        {
            TextWriter w = code == 0 ? Console.Out : Console.Error;
            w.WriteLine("usage:");
            w.WriteLine("  run [--seed N] [--hours N] [--config PATH] [--load SAVE] [--save SAVE]");
            w.WriteLine("  config dump [--defaults] [--out PATH]");
            w.WriteLine("  config check [PATH]");
            w.WriteLine("  config diff [PATH]");
            w.WriteLine("  [seed] [hours]            (shorthand for run)");
            return code;
        }

        private static Options LegacyArgs(string[] args)
        {
            var o = new Options();
            if (args.Length > 0)
            {
                o.Seed = ParseULong(args[0], "seed");
            }

            if (args.Length > 1)
            {
                o.Hours = ParseLong(args[1], "hours");
            }

            return o;
        }

        private static int Run(Options o)
        {
            SimConfig config = LoadConfig(o.ConfigPath, out string source);
            Simulation sim;
            if (o.LoadPath != null)
            {
                SaveLoadReport report = new SaveFileStore(o.LoadPath).Load(config);
                foreach (string problem in report.Problems)
                {
                    Console.Error.WriteLine("warning: " + problem);
                }

                if (report.Game == null)
                {
                    Console.Error.WriteLine("No readable save at " + o.LoadPath + ".");
                    return 1;
                }

                sim = report.Game.Simulation;
                Console.WriteLine("loaded " + o.LoadPath + " (" + report.Source + ", tick " + sim.State.Tick
                    + (report.Game.ConfigChanged ? ", balance changed since save" : string.Empty) + ")");
            }
            else
            {
                sim = new Simulation(o.Seed, config);
            }

            sim.Run(o.Hours * SimConfig.TicksPerHour);
            if (o.SavePath != null)
            {
                new SaveFileStore(o.SavePath).Save(sim);
                Console.WriteLine("saved " + o.SavePath);
            }

            GameState s = sim.State;
            Console.WriteLine("config=" + source + " hash=" + Hex(config.ComputeHash()));
            Console.WriteLine("seed=" + sim.Seed + " hours=" + o.Hours + " tick=" + s.Tick);
            Console.WriteLine("energy=" + s.Energy + " fuel=" + s.Fuel + " compute=" + s.Compute
                + " people=" + s.People + " corruption=" + s.Corruption);
            Console.WriteLine("raids=" + sim.Log.Events.Count(e => e.Kind == EventKind.RaidStarted)
                + " blackouts=" + sim.Log.Events.Count(e => e.Kind == EventKind.BlackoutStarted));
            Console.WriteLine("hash=" + Hex(StateHasher.Hash(s)));
            return 0;
        }

        private static int ConfigCommand(string[] args)
        {
            if (args.Length == 0)
            {
                throw new UsageException("config needs a subcommand: dump, check or diff.");
            }

            string[] rest = args.Skip(1).ToArray();
            switch (args[0])
            {
                case "dump":
                    {
                        bool defaults = rest.Contains("--defaults");
                        string? outPath = ValueAfter(rest, "--out");
                        SimConfig config = defaults ? SimConfig.Tier1() : LoadForDump();
                        string text = BalanceText.Write(config);
                        if (outPath != null)
                        {
                            File.WriteAllText(outPath, text);
                            Console.WriteLine("wrote " + outPath);
                        }
                        else
                        {
                            Console.Write(text);
                        }

                        return 0;
                    }

                case "check":
                    {
                        string path = rest.Length > 0 ? rest[0] : BalanceFile.LocateShipped();
                        BalanceReadResult result = BalanceText.Read(File.ReadAllText(path), BalanceReadMode.Strict);
                        foreach (ConfigIssue issue in result.Issues)
                        {
                            Console.WriteLine(path + ": " + issue);
                        }

                        Console.WriteLine(result.HasErrors ? "FAIL" : "OK  hash=" + Hex(result.Config.ComputeHash()));
                        return result.HasErrors ? 1 : 0;
                    }

                case "diff":
                    {
                        SimConfig current = LoadConfig(rest.Length > 0 ? rest[0] : null, out string source);
                        List<KeyValuePair<ConfigEntry, ConfigEntry>> diff = ConfigEntries.Diff(SimConfig.Tier1(), current);
                        Console.WriteLine(source + " vs code defaults: " + diff.Count + " difference(s)");
                        foreach (KeyValuePair<ConfigEntry, ConfigEntry> d in diff)
                        {
                            Console.WriteLine("  " + d.Key.Key.PadRight(44) + d.Key.Value + " -> " + d.Value.Value);
                        }

                        return 0;
                    }

                default:
                    throw new UsageException("Unknown config subcommand '" + args[0] + "'.");
            }
        }

        /// <summary>Lenient read so `dump` can migrate the shipped file after keys were added or removed, keeping edited values.</summary>
        private static SimConfig LoadForDump()
        {
            string path = BalanceFile.LocateShipped();
            BalanceReadResult result = BalanceText.Read(File.ReadAllText(path), BalanceReadMode.Lenient);
            foreach (ConfigIssue issue in result.Issues)
            {
                Console.Error.WriteLine(path + ": " + issue);
            }

            if (result.HasErrors)
            {
                throw new BalanceConfigException(result.Issues);
            }

            return result.Config;
        }

        private static SimConfig LoadConfig(string? path, out string source)
        {
            source = path ?? BalanceFile.LocateShipped();
            return BalanceText.Parse(File.ReadAllText(source));
        }

        private static string? ValueAfter(string[] args, string flag)
        {
            int i = Array.IndexOf(args, flag);
            if (i < 0)
            {
                return null;
            }

            if (i + 1 >= args.Length)
            {
                throw new UsageException(flag + " needs a value.");
            }

            return args[i + 1];
        }

        private static ulong ParseULong(string s, string name)
        {
            if (!ulong.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out ulong v))
            {
                throw new UsageException(name + " must be a non-negative integer, got '" + s + "'.");
            }

            return v;
        }

        private static long ParseLong(string s, string name)
        {
            if (!long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out long v))
            {
                throw new UsageException(name + " must be a non-negative integer, got '" + s + "'.");
            }

            return v;
        }

        private static string Hex(ulong v)
        {
            return v.ToString("x16", CultureInfo.InvariantCulture);
        }

        private sealed class Options
        {
            public ulong Seed { get; set; } = 42UL;

            public long Hours { get; set; } = 24L;

            public string? ConfigPath { get; set; }

            public string? LoadPath { get; set; }

            public string? SavePath { get; set; }

            public static Options Parse(string[] args)
            {
                var o = new Options();
                for (int i = 0; i < args.Length; i++)
                {
                    string flag = args[i];
                    string Next()
                    {
                        if (i + 1 >= args.Length)
                        {
                            throw new UsageException(flag + " needs a value.");
                        }

                        return args[++i];
                    }

                    switch (flag)
                    {
                        case "--seed":
                            o.Seed = ParseULong(Next(), "seed");
                            break;
                        case "--hours":
                            o.Hours = ParseLong(Next(), "hours");
                            break;
                        case "--config":
                            o.ConfigPath = Next();
                            break;
                        case "--load":
                            o.LoadPath = Next();
                            break;
                        case "--save":
                            o.SavePath = Next();
                            break;
                        default:
                            throw new UsageException("Unknown option '" + flag + "'.");
                    }
                }

                return o;
            }
        }

        private sealed class UsageException : Exception
        {
            public UsageException(string message)
                : base(message)
            {
            }
        }
    }
}
