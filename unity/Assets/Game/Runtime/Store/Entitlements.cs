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

    /// <summary>Platform store (IAP). The store service assembly replaces the default when its package is installed.</summary>
    public interface IStoreBackend
    {
        /// <summary>True when real purchases can complete.</summary>
        bool Available { get; }

        /// <summary>Buys a product; calls back with null on success or a player-facing problem.</summary>
        void Buy(string productId, System.Action<string> done);

        /// <summary>Restores non-consumables; calls back once with the owned product ids (or a problem).</summary>
        void Restore(System.Action<System.Collections.Generic.IReadOnlyList<string>, string> done);
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
        private const string KeySeason = "ds.seasonpass.";

        /// <summary>Product id of the premium unlock (StoreIds.json).</summary>
        public const string PremiumProduct = "premium";

        public static Entitlements Instance { get; } = new Entitlements();

        /// <summary>Set by the store service assembly; development builds fall back to granting directly.</summary>
        public static IStoreBackend Backend { get; set; }

        public event System.Action Changed;

        public bool HasPremium => PlayerPrefs.GetInt(KeyPremium, 0) == 1;

        /// <summary>True when purchases can complete in this build (a real store, or a development build).</summary>
        public static bool StoreAvailable => (Backend != null && Backend.Available) || Debug.isDebugBuild || Application.isEditor;

        public bool OwnsSeason(string id)
        {
            return PlayerPrefs.GetInt(KeySeason + id, 0) == 1;
        }

        public bool OwnsCosmetic(string id)
        {
            return PlayerPrefs.GetInt(KeyCosmetic + id, 0) == 1;
        }

        /// <summary>Starts the premium purchase; calls back with a player-facing result line.</summary>
        public void BuyPremium(System.Action<string> done)
        {
            if (HasPremium)
            {
                done("Already unlocked.");
                return;
            }

            Purchase(PremiumProduct, KeyPremium, "Full game unlocked. Thank you, handler.", done);
        }

        /// <summary>Buys the season's premium track (cosmetics only, never expires); calls back with a line.</summary>
        public void BuySeason(string id, System.Action<string> done)
        {
            if (OwnsSeason(id))
            {
                done("Season pass already owned.");
                return;
            }

            Purchase("season." + id, KeySeason + id, "Season pass unlocked. Everything you reached is yours.", done);
        }

        /// <summary>Restores purchases from the platform store (development builds keep the local record).</summary>
        public void Restore(System.Action<string> done)
        {
            if (Backend == null || !Backend.Available)
            {
                Changed?.Invoke();
                done(HasPremium ? "Purchases restored." : "Nothing to restore.");
                return;
            }

            Backend.Restore((owned, problem) =>
            {
                if (problem != null)
                {
                    done(problem);
                    return;
                }

                foreach (string product in owned)
                {
                    Grant(product == PremiumProduct ? KeyPremium : product.StartsWith("season.", System.StringComparison.Ordinal) ? KeySeason + product.Substring(7) : null);
                }

                done(HasPremium ? "Purchases restored." : "Nothing to restore.");
            });
        }

        private void Purchase(string product, string key, string thanks, System.Action<string> done)
        {
            if (Backend != null && Backend.Available)
            {
                Backend.Buy(product, problem =>
                {
                    if (problem == null)
                    {
                        Grant(key);
                    }

                    done(problem ?? thanks);
                });
                return;
            }

            if (!StoreAvailable)
            {
                done("The store is not reachable from this build.");
                return;
            }

            // development builds: no store linked, grant directly so the full game can be played
            Grant(key);
            done(thanks);
        }

        internal void GrantCosmetic(string id)
        {
            Grant(KeyCosmetic + id);
        }

        private void Grant(string key)
        {
            if (key == null)
            {
                return;
            }

            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
