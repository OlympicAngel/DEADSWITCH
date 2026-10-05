using System;
using Deadswitch.Game.Core;
using UnityEngine;
using UnityEngine.Advertisements;

namespace Deadswitch.Game.Store
{
    /// <summary>
    /// Rewarded ads through Unity Ads 4.x (doc 10 s1.1, ADR-0006): only the convenience whitelist is ever granted,
    /// and only on a completed view. Compiles only when com.unity.ads 4.4-4.x is installed; stays off until a game id
    /// is set in StoreIds.json. Premium players never see ads (RewardedAds.Blocked).
    /// </summary>
    public sealed class UnityAdsBackend : IAdBackend, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
    {
        private readonly StoreIds _ids = StoreIds.Load();
        private bool _loaded;
        private Action<bool> _completed;

        public bool Ready => _loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            Bootstrap.Booted += _ =>
            {
                var backend = new UnityAdsBackend();
                if (backend._ids.AdsGameId.Length == 0 || Entitlements.Instance.HasPremium)
                {
                    return;
                }

                RewardedAds.Backend = backend;
                Advertisement.Initialize(backend._ids.AdsGameId, backend._ids.adsTestMode, backend);
            };
        }

        public void Show(Action<bool> completed)
        {
            if (!_loaded)
            {
                completed(false);
                return;
            }

            _completed = completed;
            _loaded = false;
            Advertisement.Show(_ids.AdsRewardedPlacement, this);
        }

        public void OnInitializationComplete()
        {
            Advertisement.Load(_ids.AdsRewardedPlacement, this);
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            Debug.LogWarning("[DEADSWITCH] Ads unavailable: " + error + " " + message);
        }

        public void OnUnityAdsAdLoaded(string placementId)
        {
            _loaded = true;
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            _loaded = false;
        }

        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            Finish(false);
        }

        public void OnUnityAdsShowStart(string placementId)
        {
        }

        public void OnUnityAdsShowClick(string placementId)
        {
        }

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
        {
            Finish(showCompletionState == UnityAdsShowCompletionState.COMPLETED);
        }

        private void Finish(bool completed)
        {
            Action<bool> done = _completed;
            _completed = null;
            done?.Invoke(completed);

            // queue the next one
            Advertisement.Load(_ids.AdsRewardedPlacement, this);
        }
    }
}
