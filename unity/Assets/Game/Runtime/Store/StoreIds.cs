using UnityEngine;

namespace Deadswitch.Game.Store
{
    /// <summary>Platform ids for the store and ad services (Resources/Store/StoreIds.json, filled in by the owner).</summary>
    [System.Serializable]
    public sealed class StoreIds
    {
        public ProductIds[] products = new ProductIds[0];
        public string adsGameIdAndroid = string.Empty;
        public string adsGameIdIos = string.Empty;
        public string adsRewardedAndroid = "Rewarded_Android";
        public string adsRewardedIos = "Rewarded_iOS";
        public bool adsTestMode = true;

        private static StoreIds _cached;

        /// <summary>This platform's ad game id; empty means ads stay off.</summary>
        public string AdsGameId => Application.platform == RuntimePlatform.IPhonePlayer ? adsGameIdIos : adsGameIdAndroid;

        public string AdsRewardedPlacement => Application.platform == RuntimePlatform.IPhonePlayer ? adsRewardedIos : adsRewardedAndroid;

        public static StoreIds Load()
        {
            if (_cached == null)
            {
                var asset = Resources.Load<TextAsset>("Store/StoreIds");
                _cached = asset != null ? JsonUtility.FromJson<StoreIds>(asset.text) : new StoreIds();
            }

            return _cached;
        }

        /// <summary>The platform product id for a game key ("premium", "season.S1"), or empty when not configured.</summary>
        public string ProductId(string key)
        {
            foreach (ProductIds p in products)
            {
                if (p.key == key)
                {
                    return Application.platform == RuntimePlatform.IPhonePlayer ? p.ios : p.android;
                }
            }

            return string.Empty;
        }

        /// <summary>The game key for a platform product id, or null.</summary>
        public string KeyOf(string productId)
        {
            foreach (ProductIds p in products)
            {
                if (p.android == productId || p.ios == productId)
                {
                    return p.key;
                }
            }

            return null;
        }
    }

    [System.Serializable]
    public sealed class ProductIds
    {
        public string key = string.Empty;
        public string android = string.Empty;
        public string ios = string.Empty;
    }
}
