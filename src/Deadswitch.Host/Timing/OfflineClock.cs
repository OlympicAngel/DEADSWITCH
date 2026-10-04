using System;
using System.Globalization;
using System.IO;
using Deadswitch.Sim.Config;

namespace Deadswitch.Host.Timing
{
    /// <summary>What to simulate when the game comes back (ADR-0004).</summary>
    public readonly struct CatchUpPlan
    {
        public CatchUpPlan(long minutes, bool capped, bool desync, long elapsedMinutes)
        {
            Minutes = minutes;
            Capped = capped;
            Desync = desync;
            ElapsedMinutes = elapsedMinutes;
        }

        /// <summary>Ticks to simulate now.</summary>
        public long Minutes { get; }

        /// <summary>The absence was longer than the cap; the rest is forfeited (no free progress).</summary>
        public bool Capped { get; }

        /// <summary>The device clock went backwards: shown in-story as an AI "time desync" glitch.</summary>
        public bool Desync { get; }

        /// <summary>Raw wall-clock minutes since the last session stamp (negative when the clock went back).</summary>
        public long ElapsedMinutes { get; }
    }

    /// <summary>
    /// Offline time policy (ADR-0004). Pure: wall-clock values are passed in so the rules are testable.
    /// Whole minutes only; the sub-minute remainder is carried by keeping the stamp aligned to simulated minutes.
    /// </summary>
    public static class OfflineClock
    {
        public const long MsPerMinute = 60_000;

        public static CatchUpPlan Plan(long lastStampUnixMs, long nowUnixMs, HostConfig config)
        {
            long elapsedMs = nowUnixMs - lastStampUnixMs;
            long elapsedMinutes = elapsedMs >= 0 ? elapsedMs / MsPerMinute : -((-elapsedMs) / MsPerMinute);
            if (elapsedMinutes < -config.DesyncToleranceMinutes)
            {
                return new CatchUpPlan(0, false, true, elapsedMinutes);
            }

            if (elapsedMinutes <= 0)
            {
                return new CatchUpPlan(0, false, false, elapsedMinutes);
            }

            long cap = config.MaxCatchUpHours * 60L;
            return elapsedMinutes > cap
                ? new CatchUpPlan(cap, true, false, elapsedMinutes)
                : new CatchUpPlan(elapsedMinutes, false, false, elapsedMinutes);
        }

        /// <summary>
        /// The stamp to store after simulating <paramref name="simulatedMinutes"/>: advances the old stamp by exactly
        /// the simulated time so partial minutes are not lost, or rebases to now after a cap or desync.
        /// </summary>
        public static long NextStamp(long lastStampUnixMs, long nowUnixMs, CatchUpPlan plan)
        {
            if (plan.Desync || plan.Capped)
            {
                return nowUnixMs;
            }

            return lastStampUnixMs + (plan.Minutes * MsPerMinute);
        }
    }

    /// <summary>Stores the wall-clock stamp of the last simulated minute next to the save.</summary>
    public sealed class SessionStampFile
    {
        public SessionStampFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public bool TryRead(out long unixMs)
        {
            unixMs = 0;
            try
            {
                return File.Exists(Path) && long.TryParse(File.ReadAllText(Path).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out unixMs);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        public void Write(long unixMs)
        {
            string tmp = Path + ".tmp";
            File.WriteAllText(tmp, unixMs.ToString(CultureInfo.InvariantCulture));
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }

            File.Move(tmp, Path);
        }
    }
}
