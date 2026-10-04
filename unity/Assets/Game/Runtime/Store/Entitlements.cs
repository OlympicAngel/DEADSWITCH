using UnityEngine;

namespace Deadswitch.Game.Store
{
    /// <summary>What the player owns (ADR-0006). The sim never sees purchases; only presentation gates read this.</summary>
    public interface IEntitlements
    {
        /// <summary>One-time premium unlock: Tier 2 and beyond, Ironman, future content.</summary>
        bool HasPremium { get; }

        bool OwnsSeason(string id);

        /// <summary>Cosmetics earned from rewarded ads (convenience grants), by id.</summary>
        bool OwnsCosmetic(string id);

        event System.Action Changed;
    }

    /// <summary>
    /// Local entitlement store (ADR-0006, v1 has no server): persisted in PlayerPrefs and restored from the platform
    /// store. Without a store plugin in this build, development builds grant the unlock directly so the full game
    /// can be played; release builds report the store as unavailable instead of faking a purchase.
    /// </summary>
    public sealed class Entitlements : IEntitlements
    {
        private const string KeyPremium = "ds.premium";
        private const string KeyCosmetic = "ds.cosmetic.";

        public static Entitlements Instance { get; } = new Entitlements();

        public event System.Action Changed;

        public bool HasPremium => PlayerPrefs.GetInt(KeyPremium, 0) == 1;

        /// <summary>True when purchases can complete in this build.</summary>
        public static bool StoreAvailable => Debug.isDebugBuild || Application.isEditor;

        public bool OwnsSeason(string id)
        {
            return false;
        }

        public bool OwnsCosmetic(string id)
        {
            return PlayerPrefs.GetInt(KeyCosmetic + id, 0) == 1;
        }

        /// <summary>Starts the premium purchase. Returns a player-facing result line.</summary>
        public string BuyPremium()
        {
            if (HasPremium)
            {
                return "Already unlocked.";
            }

            if (!StoreAvailable)
            {
                return "The store is not reachable from this build.";
            }

            Grant(KeyPremium);
            return "Full game unlocked. Thank you, handler.";
        }

        /// <summary>Restores purchases from the platform store (dev builds keep the local record).</summary>
        public string Restore()
        {
            Changed?.Invoke();
            return HasPremium ? "Purchases restored." : "Nothing to restore.";
        }

        internal void GrantCosmetic(string id)
        {
            Grant(KeyCosmetic + id);
        }

        private void Grant(string key)
        {
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
