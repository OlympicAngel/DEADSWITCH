using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Deadswitch.Host.Dev;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Cli
{
    /// <summary>
    /// Balance scenario runner (SPEC-014): many seeds x a few player profiles x a month of game time, summarized as
    /// percentiles plus hard guards. Seeds run in parallel; results are ordered by seed so the report is stable.
    /// </summary>
    public static class BalanceRunner
    {
        public static readonly string[] Profiles = { "active", "casual", "autopilot", "idle" };

        private static readonly int[] SessionHours = { 8, 13, 20 };
        private const int SessionMinutes = 45;

        /// <summary>Runs the profiles and writes the report. Returns false when a hard guard failed.</summary>
        public static bool Run(SimConfig config, int seeds, int days, IReadOnlyList<string> profiles, TextWriter w)
        {
            bool ok = true;
            w.WriteLine("# Balance report");
            w.WriteLine();
            w.WriteLine("Seeds 1.." + seeds + ", " + days + " days each, config hash " + config.ComputeHash().ToString("x16", CultureInfo.InvariantCulture) + ".");
            w.WriteLine("Values are median [p10-p90] over seeds. Days count from 1; levels are summed over all plots at the end of a day.");
            w.WriteLine();
            w.WriteLine("| Profile | Levels d1 / d3 / d7 / end | Tier 2 day | Reached T2 | Raids/day | Breach % | People end | People min | Blackout h | Core % end | Core % max | Glitchy+ h/day | Stage | Climax |");
            w.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            var guards = new List<string>();
            foreach (string profile in profiles)
            {
                RunResult[] runs = new RunResult[seeds];
                Parallel.For(0, seeds, i => runs[i] = Play(config, (ulong)(i + 1), days, profile));

                int reached = runs.Count(r => r.Tier2Day >= 0);
                w.WriteLine("| " + profile
                    + " | " + string.Join(" / ", new[] { 1, 3, 7, days }.Select(d => Median(runs.Select(r => r.LevelsByDay[Math.Min(d, days) - 1]))))
                    + " | " + (reached > 0 ? Spread(runs.Where(r => r.Tier2Day >= 0).Select(r => (double)r.Tier2Day)) : "-")
                    + " | " + Pct(reached, seeds)
                    + " | " + Spread(runs.Select(r => r.Raids / (double)days), 1)
                    + " | " + Spread(runs.Select(r => r.Raids == 0 ? 0 : 100.0 * r.Breaches / r.Raids))
                    + " | " + Spread(runs.Select(r => (double)r.PeopleEnd))
                    + " | " + Spread(runs.Select(r => (double)r.PeopleMin))
                    + " | " + Spread(runs.Select(r => (double)r.BlackoutHours))
                    + " | " + Spread(runs.Select(r => (double)r.CorruptionEndPct))
                    + " | " + Spread(runs.Select(r => (double)r.CorruptionMaxPct))
                    + " | " + Spread(runs.Select(r => r.GlitchyHours / (double)days), 1)
                    + " | " + StageMix(runs)
                    + " | " + Pct(runs.Count(r => r.Climax), seeds) + " |");

                Guard(guards, ref ok, profile + ": daily attack cap never exceeded", runs.All(r => r.CapOk), runs.Where(r => !r.CapOk).Select(r => r.Seed));
                if (profile == "active" || profile == "casual")
                {
                    int floor = config.PeopleChoices.MinPeople;
                    Guard(guards, ref ok, profile + ": people never below " + floor, runs.All(r => r.PeopleMin >= floor), runs.Where(r => r.PeopleMin < floor).Select(r => r.Seed));
                }

                if (profile == "active")
                {
                    Guard(guards, ref ok, profile + ": no blackout longer than 24h", runs.All(r => r.LongestBlackoutHours <= 24), runs.Where(r => r.LongestBlackoutHours > 24).Select(r => r.Seed));
                }
            }

            w.WriteLine();
            w.WriteLine("## Guards");
            foreach (string g in guards)
            {
                w.WriteLine("- " + g);
            }

            return ok;
        }

        private static RunResult Play(SimConfig config, ulong seed, int days, string profile)
        {
            var sim = new Simulation(seed, config);
            var r = new RunResult { Seed = seed, PeopleMin = sim.State.People };
            switch (profile)
            {
                case "casual":
                    sim.Execute(Command.SetDelegation(DelegationLevel.Delegated));
                    break;
                case "autopilot":
                    sim.Execute(Command.SetDelegation(DelegationLevel.Autopilot));
                    sim.Execute(Command.SetPresence(true));
                    break;
                case "idle":
                    sim.Execute(Command.SetPresence(true));
                    break;
            }

            int blackoutRun = 0;
            long end = (long)days * SimConfig.TicksPerDay;
            while (sim.State.Tick < end)
            {
                long minuteOfDay = sim.State.Tick % SimConfig.TicksPerDay;
                bool session = profile == "active" || (profile == "casual" && InSession(minuteOfDay));
                if (profile == "casual" && sim.State.Away == session)
                {
                    sim.Execute(Command.SetPresence(!session));
                }

                if (session)
                {
                    ScriptedPlayer.Think(sim);
                }

                sim.Run(20);
                GameState s = sim.State;
                if (s.Tick % SimConfig.TicksPerDay == 0)
                {
                    r.LevelsByDay.Add(s.Slots.Sum(x => x.Level));
                }

                r.PeopleMin = Math.Min(r.PeopleMin, s.People);
                if (s.Tick % SimConfig.TicksPerHour == 0)
                {
                    blackoutRun = s.Blackout ? blackoutRun + 1 : 0;
                    r.BlackoutHours += s.Blackout ? 1 : 0;
                    r.LongestBlackoutHours = Math.Max(r.LongestBlackoutHours, blackoutRun);
                    int core = CorruptionSystem.Percent(s.CorruptionMilli);
                    r.CorruptionMaxPct = Math.Max(r.CorruptionMaxPct, core);
                    r.GlitchyHours += core >= config.Corruption.GlitchyFrom ? 1 : 0;
                }
            }

            GameState f = sim.State;
            r.PeopleEnd = f.People;
            r.CorruptionEndPct = CorruptionSystem.Percent(f.CorruptionMilli);
            r.Tier2Day = -1;
            var raidsPerDay = new Dictionary<long, int>();
            var tierAt = new SortedDictionary<long, int> { { 0, 1 } };
            foreach (SimEvent e in sim.Log.Events)
            {
                switch (e.Kind)
                {
                    case EventKind.TierAdvanced:
                        tierAt[e.Tick] = e.A;
                        if (e.A == 2)
                        {
                            r.Tier2Day = (int)(e.Tick / SimConfig.TicksPerDay) + 1;
                        }

                        break;
                    case EventKind.RaidWarning:
                        long day = e.Tick / SimConfig.TicksPerDay;
                        raidsPerDay[day] = raidsPerDay.TryGetValue(day, out int n) ? n + 1 : 1;
                        r.Raids++;
                        break;
                    case EventKind.RaidResolved:
                        r.Breaches += e.B == (int)RaidOutcome.Breached ? 1 : 0;
                        break;
                    case EventKind.ProjectStage:
                        r.Stage = Math.Max(r.Stage, e.A);
                        break;
                    case EventKind.Climax:
                        r.Climax = true;
                        break;
                }
            }

            // the cap applies per game day at the tier in force at the end of that day (doc 10 s4)
            int[] extra = config.Tier.ExtraRaidsPerDay;
            r.CapOk = raidsPerDay.All(kv =>
            {
                int tier = tierAt.Where(t => t.Key <= ((kv.Key + 1) * SimConfig.TicksPerDay)).Last().Value;
                return kv.Value <= config.Raid.MaxPerDay + extra[Math.Min(tier, extra.Length) - 1];
            });
            return r;
        }

        private static bool InSession(long minuteOfDay)
        {
            foreach (int h in SessionHours)
            {
                long start = h * SimConfig.TicksPerHour;
                if (minuteOfDay >= start && minuteOfDay < start + SessionMinutes)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Guard(List<string> lines, ref bool ok, string name, bool pass, IEnumerable<ulong> failing)
        {
            ok &= pass;
            lines.Add((pass ? "PASS " : "FAIL ") + name + (pass ? string.Empty : " (seeds " + string.Join(", ", failing.Take(8)) + ")"));
        }

        private static string Spread(IEnumerable<double> values, int decimals = 0)
        {
            double[] v = values.OrderBy(x => x).ToArray();
            if (v.Length == 0)
            {
                return "-";
            }

            string F(double x) => x.ToString("F" + decimals, CultureInfo.InvariantCulture);
            return F(At(v, 0.5)) + " [" + F(At(v, 0.1)) + "-" + F(At(v, 0.9)) + "]";
        }

        private static string Median(IEnumerable<int> values)
        {
            int[] v = values.OrderBy(x => x).ToArray();
            return v[v.Length / 2].ToString(CultureInfo.InvariantCulture);
        }

        private static double At(double[] sorted, double q)
        {
            return sorted[Math.Min(sorted.Length - 1, (int)(q * sorted.Length))];
        }

        private static string Pct(int n, int of)
        {
            return (100 * n / Math.Max(1, of)).ToString(CultureInfo.InvariantCulture) + "%";
        }

        private static string StageMix(RunResult[] runs)
        {
            string[] names = { "D", "A", "Adv", "Imm" };
            return string.Join(" ", Enumerable.Range(0, 4).Where(i => runs.Any(r => r.Stage == i)).Select(i => names[i] + " " + Pct(runs.Count(r => r.Stage == i), runs.Length)));
        }

        private sealed class RunResult
        {
            public ulong Seed;
            public int Tier2Day;
            public int Raids;
            public int Breaches;
            public int PeopleEnd;
            public int PeopleMin;
            public int BlackoutHours;
            public int LongestBlackoutHours;
            public int CorruptionEndPct;
            public int CorruptionMaxPct;
            public int GlitchyHours;
            public int Stage;
            public bool Climax;
            public bool CapOk;
            public List<int> LevelsByDay = new List<int>();
        }
    }
}
