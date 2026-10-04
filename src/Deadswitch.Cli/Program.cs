using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Deadswitch.Art.World;
using Deadswitch.Host.Persistence;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

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
                    case "art":
                        return ArtCommand(rest);
                    case "advisor":
                        return AdvisorCommand(rest);
                    case "report":
                        return ReportCommand(rest);
                    case "balance":
                        return BalanceCommand(rest);
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
            w.WriteLine("  run [--seed N] [--hours N] [--config PATH] [--load SAVE] [--save SAVE] [--garrison N] [--posture none|turtle|dark|evacuate] [--away]");
            w.WriteLine("  report [--days N] [--seed N] [--raid ID] [--verify]");
            w.WriteLine("  advisor [--days N] [--seed N] [--delegation manual|delegated|autopilot] [--away]");
            w.WriteLine("  balance [--seeds N] [--days N] [--profile active|casual|autopilot|idle|all] [--config PATH] [--out PATH]");
            w.WriteLine("  config dump [--defaults] [--out PATH]");
            w.WriteLine("  config check [PATH]");
            w.WriteLine("  config diff [PATH]");
            w.WriteLine("  art export [--days N] [--seed N] [--out PATH] [--layout Kind:Level,...] [--tier N] [--report RAID|last] [--faction N]");
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

            if (o.Garrison > 0)
            {
                Console.WriteLine("garrison: " + sim.Execute(Deadswitch.Sim.Commands.Command.SetGarrison(o.Garrison)));
            }

            if (o.Posture != Posture.None)
            {
                Console.WriteLine("posture: " + sim.Execute(Deadswitch.Sim.Commands.Command.SetPosture(o.Posture)));
            }

            if (o.Away)
            {
                sim.Execute(Deadswitch.Sim.Commands.Command.SetPresence(true));
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
                + " people=" + s.People + " corruption=" + CorruptionSystem.Percent(s.CorruptionMilli) + "%");
            EconomyFlows f = Economy.Flows(s, sim.Config);
            Console.WriteLine("energy/h: +" + f.GenerationPerHour + " -" + f.CoreUpkeepPerHour + " core -" + f.FacilityUpkeepPerHour
                + " facilities = " + f.NetEnergyPerHour + "  compute/h=" + f.ComputePerHour + "  caps: energy " + f.EnergyCap
                + " people " + f.PopulationCap + "  crew " + f.CrewAssigned + "/" + f.CrewNeeded + "  unmanned=" + f.AutomationLoad);
            for (int i = 0; i < s.Slots.Count; i++)
            {
                FacilitySlot slot = s.Slots[i];
                Console.WriteLine("  slot " + i + ": " + (slot.IsEmpty ? "-" : slot.Kind + " L" + slot.Level
                    + (slot.Enabled ? string.Empty : " OFF") + (slot.Powered ? string.Empty : " SHED") + (slot.Staffed ? string.Empty : " UNMANNED")));
            }

            Console.WriteLine("raids=" + sim.Log.Events.Count(e => e.Kind == EventKind.RaidWarning)
                + " repelled=" + sim.Log.Events.Count(e => e.Kind == EventKind.RaidResolved && e.B == (int)RaidOutcome.Repelled)
                + " breached=" + sim.Log.Events.Count(e => e.Kind == EventKind.RaidResolved && e.B == (int)RaidOutcome.Breached)
                + " energy lost=" + sim.Log.Events.Where(e => e.Kind == EventKind.LossLine && e.B == (int)LossResource.Energy).Sum(e => e.C)
                + " compute lost=" + sim.Log.Events.Where(e => e.Kind == EventKind.LossLine && e.B == (int)LossResource.Compute).Sum(e => e.C)
                + " blackouts=" + sim.Log.Events.Count(e => e.Kind == EventKind.BlackoutStarted));
            Console.WriteLine("hash=" + Hex(StateHasher.Hash(s)));
            return 0;
        }

        /// <summary>
        /// art export [--days N] [--seed N] [--out PATH] [--layout Kind:Level,...]: a scripted base after N days, for
        /// tools/basepreview. --layout overrides the slots (e.g. Generator:5,ServerRack:3,None:0) to review assets;
        /// --tier overrides the Hub tier the surroundings are drawn for (SPEC-013).
        /// </summary>
        private static int ArtCommand(string[] args)
        {
            if (args.Length == 0 || args[0] != "export")
            {
                throw new UsageException("art needs a subcommand: export.");
            }

            string[] rest = args.Skip(1).ToArray();
            double days = double.Parse(ValueAfter(rest, "--days") ?? "7", CultureInfo.InvariantCulture);
            ulong seed = ParseULong(ValueAfter(rest, "--seed") ?? "42", "seed");
            string outPath = ValueAfter(rest, "--out") ?? Path.Combine("artifacts", "basepreview", "scene.json");
            SimConfig config = LoadConfig(null, out _);
            var sim = new Simulation(seed, config);
            Deadswitch.Host.Dev.ScriptedPlayer.Play(sim, (long)(days * SimConfig.TicksPerDay));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            SlotView[]? layout = null;
            string? layoutArg = ValueAfter(rest, "--layout");
            if (layoutArg != null)
            {
                layout = layoutArg.Split(',').Select(e =>
                {
                    string[] kv = e.Split(':');
                    int damage = 0;
                    if (kv.Length < 2 || kv.Length > 3 || !Enum.TryParse(kv[0], out FacilityKind kind) || !int.TryParse(kv[1], out int level) || (kv.Length == 3 && !int.TryParse(kv[2], out damage)))
                    {
                        throw new UsageException("--layout entries look like Generator:3 or Generator:3:2 (level, battle damage).");
                    }

                    return new SlotView(kind, level, true, false, FacilityKind.None, 0, damage);
                }).ToArray();
            }

            Deadswitch.Host.Reports.BattleReport? report = null;
            string? reportArg = ValueAfter(rest, "--report");
            if (reportArg != null)
            {
                int raid = reportArg == "last" ? Deadswitch.Host.Reports.BattleReport.LatestRaidId(sim.Log.Events) : int.Parse(reportArg, CultureInfo.InvariantCulture);
                report = Deadswitch.Host.Reports.BattleReport.Build(sim.Log.Events, raid) ?? throw new UsageException("Raid " + raid + " has not resolved in this run.");
            }

            int tier = int.Parse(ValueAfter(rest, "--tier") ?? sim.State.Tier.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            int wreckage = int.Parse(ValueAfter(rest, "--wreckage") ?? sim.State.Wreckage.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            bool burning = rest.Contains("--burning");
            int faction = int.Parse(ValueAfter(rest, "--faction") ?? "-1", CultureInfo.InvariantCulture);
            ArtExport.Write(sim, outPath, (uint)seed, layout, report, tier, wreckage, burning, faction);
            Console.WriteLine("wrote " + outPath + " (day " + (sim.State.Tick / SimConfig.TicksPerDay) + ")");
            for (int i = 0; i < sim.State.Slots.Count; i++)
            {
                FacilitySlot slot = sim.State.Slots[i];
                Console.WriteLine("  slot " + i + ": " + (slot.IsEmpty ? "-" : slot.Kind + " L" + slot.Level));
            }

            return 0;
        }

        /// <summary>balance [--seeds N] [--days N] [--profile NAME|all] [--config PATH] [--out PATH]: the SPEC-014 report; exit 1 on a failed guard.</summary>
        private static int BalanceCommand(string[] args)
        {
            int seeds = (int)ParseLong(ValueAfter(args, "--seeds") ?? "100", "seeds");
            int days = (int)ParseLong(ValueAfter(args, "--days") ?? "30", "days");
            string profile = ValueAfter(args, "--profile") ?? "all";
            string[] profiles = profile == "all" ? BalanceRunner.Profiles : new[] { profile };
            if (seeds < 1 || days < 1 || profiles.Any(p => !BalanceRunner.Profiles.Contains(p)))
            {
                throw new UsageException("balance needs --seeds >= 1, --days >= 1 and --profile " + string.Join("|", BalanceRunner.Profiles) + "|all.");
            }

            var report = new StringWriter(CultureInfo.InvariantCulture);
            bool ok = BalanceRunner.Run(LoadConfig(ValueAfter(args, "--config"), out _), seeds, days, profiles, report);
            string? outPath = ValueAfter(args, "--out");
            if (outPath != null)
            {
                File.WriteAllText(outPath, report.ToString());
            }

            Console.Write(report.ToString());
            return ok ? 0 : 1;
        }

        /// <summary>advisor [--days N] [--seed N] [--delegation manual|delegated|autopilot] [--away]: the AI's lines over a run.</summary>
        private static int AdvisorCommand(string[] args)
        {
            double days = double.Parse(ValueAfter(args, "--days") ?? "3", CultureInfo.InvariantCulture);
            ulong seed = ParseULong(ValueAfter(args, "--seed") ?? "42", "seed");
            string level = ValueAfter(args, "--delegation") ?? "manual";
            if (!Enum.TryParse(level, true, out DelegationLevel delegation))
            {
                throw new UsageException("--delegation is manual, delegated or autopilot.");
            }

            var sim = new Simulation(seed, LoadConfig(null, out _));
            AdvisorTranscript.Write(sim, (long)(days * SimConfig.TicksPerDay), delegation, args.Contains("--away"), Console.Out);
            return 0;
        }

        /// <summary>report [--days N] [--seed N] [--raid ID] [--verify]: a battle report after a scripted run (SPEC-006).</summary>
        private static int ReportCommand(string[] args)
        {
            double days = double.Parse(ValueAfter(args, "--days") ?? "2", CultureInfo.InvariantCulture);
            ulong seed = ParseULong(ValueAfter(args, "--seed") ?? "1", "seed");
            var sim = new Simulation(seed, LoadConfig(null, out _));
            sim.Run((long)(days * SimConfig.TicksPerDay));
            int raid = int.Parse(ValueAfter(args, "--raid") ?? Deadswitch.Host.Reports.BattleReport.LatestRaidId(sim.Log.Events).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            if (args.Contains("--verify"))
            {
                Console.WriteLine("verify: " + sim.Execute(Command.VerifyReport(raid)).Reason);
            }

            Deadswitch.Host.Reports.BattleReport? r = Deadswitch.Host.Reports.BattleReport.Build(sim.Log.Events, raid);
            if (r == null)
            {
                Console.WriteLine("no resolved raid " + raid);
                return 1;
            }

            Console.WriteLine("RAID " + r.RaidId + " // " + r.Outcome.ToString().ToUpperInvariant() + " // PREDICTED " + r.PredictedGate + " // CONTACT " + r.ContactGate);
            foreach (Deadswitch.Host.Reports.ReportPanel p in r.Panels)
            {
                Console.WriteLine("  [" + p.Shot + "] " + p.Caption);
            }

            Console.WriteLine("  AI: " + r.Summary);
            Console.WriteLine("  LEDGER: " + r.LossText(false));
            foreach (string f in r.Findings)
            {
                Console.WriteLine("  VERIFY: " + f);
            }

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

            public int Garrison { get; set; }

            public Posture Posture { get; set; }

            public bool Away { get; set; }

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
                        case "--garrison":
                            o.Garrison = (int)ParseLong(Next(), "garrison");
                            break;
                        case "--posture":
                            o.Posture = (Posture)Enum.Parse(typeof(Posture), Next(), true);
                            break;
                        case "--away":
                            o.Away = true;
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
