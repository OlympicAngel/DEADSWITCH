using Deadswitch.Game.Core;
using Deadswitch.Host.Seasons;
using Deadswitch.Sim.Events;
using UnityEngine;

namespace Deadswitch.Game.Cosmetics
{
    /// <summary>
    /// Reward-track progress on this device (F-048). XP comes from live sim events (each dispatched once); every reward
    /// unlocks when its rank is reached, also retroactively. Free, cosmetic only: themes, AI voices and codex logs.
    /// </summary>
    public static class SeasonPass
    {
        private const string KeyXp = "ds.season." + SeasonTrack.Id + ".xp";
        private static bool _hooked;

        public static event System.Action Changed;

        public static int Xp => Mathf.Max(0, PlayerPrefs.GetInt(KeyXp, 0));

        public static int Rank => SeasonTrack.Rank(Xp);

        public static bool Unlocked(SeasonReward r)
        {
            return Unlocks.Owns(r.Id);
        }

        /// <summary>Starts counting XP from the host's events. Safe to call more than once.</summary>
        public static void Hook(GameHost host)
        {
            if (_hooked)
            {
                return;
            }

            _hooked = true;
            host.EventRaised += OnEvent;
            GrantReached();
        }

        /// <summary>Unlocks every reward the handler has reached.</summary>
        public static void GrantReached()
        {
            bool granted = false;
            foreach (SeasonReward r in SeasonTrack.Rewards)
            {
                if (r.Rank <= Rank && !Unlocked(r))
                {
                    Unlocks.Grant(r.Id);
                    granted = true;
                }
            }

            if (granted)
            {
                Changed?.Invoke();
            }
        }

        private static void OnEvent(SimEvent e)
        {
            int xp = SeasonTrack.Xp(e);
            if (xp <= 0)
            {
                return;
            }

            int before = Rank;
            PlayerPrefs.SetInt(KeyXp, Mathf.Min(SeasonTrack.Ranks * SeasonTrack.XpPerRank, Xp + xp));
            if (Rank != before)
            {
                PlayerPrefs.Save();
                GrantReached();
            }

            Changed?.Invoke();
        }
    }
}
