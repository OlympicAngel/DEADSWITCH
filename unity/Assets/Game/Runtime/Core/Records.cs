using Deadswitch.Host.Online;
using Deadswitch.Sim.Events;
using UnityEngine;

namespace Deadswitch.Game.Core
{
    /// <summary>
    /// Personal bests on this device (F-049), from the same submission a leaderboard would receive. Updated when a
    /// cycle ends and when the legacy screen opens; the save itself stays the verifiable proof (RunVerifier).
    /// </summary>
    public static class Records
    {
        private const string KeyScore = "ds.best.score";
        private const string KeyTier = "ds.best.tier";
        private const string KeyCycle = "ds.best.cycle";
        private static bool _hooked;

        public static int BestScore => PlayerPrefs.GetInt(KeyScore, 0);

        public static int BestTier => PlayerPrefs.GetInt(KeyTier, 0);

        public static int BestCycle => PlayerPrefs.GetInt(KeyCycle, 0);

        public static void Hook(GameHost host)
        {
            if (_hooked)
            {
                return;
            }

            _hooked = true;
            host.EventRaised += e =>
            {
                if (e.Kind == EventKind.CycleEnded || e.Kind == EventKind.TierAdvanced)
                {
                    Update(host);
                }
            };
        }

        public static void Update(GameHost host)
        {
            RunSubmission run = RunVerifier.Read(host.Sim);
            bool changed = false;
            if (run.Score > BestScore)
            {
                PlayerPrefs.SetInt(KeyScore, run.Score);
                changed = true;
            }

            if (run.HighestTier > BestTier)
            {
                PlayerPrefs.SetInt(KeyTier, run.HighestTier);
                changed = true;
            }

            if (run.Cycle + 1 > BestCycle)
            {
                PlayerPrefs.SetInt(KeyCycle, run.Cycle + 1);
                changed = true;
            }

            if (changed)
            {
                PlayerPrefs.Save();
            }
        }
    }
}
