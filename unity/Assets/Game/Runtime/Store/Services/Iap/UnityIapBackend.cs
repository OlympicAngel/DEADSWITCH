using System;
using System.Collections.Generic;
using Deadswitch.Game.Core;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Deadswitch.Game.Store
{
    /// <summary>
    /// Store purchases through Unity IAP 4.x (ADR-0006): the premium unlock and season passes are non-consumables.
    /// Compiles only when com.unity.purchasing 4.9-4.x is installed; product ids come from StoreIds.json, and a
    /// product without an id for this platform is simply not offered.
    /// </summary>
    public sealed class UnityIapBackend : IStoreBackend, IStoreListener
    {
        private readonly StoreIds _ids = StoreIds.Load();
        private IStoreController _controller;
        private IExtensionProvider _extensions;
        private Action<string> _pending;

        public bool Available => _controller != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            Bootstrap.Booted += _ =>
            {
                var backend = new UnityIapBackend();
                Entitlements.Backend = backend;
                backend.Initialize();
            };
        }

        public void Buy(string productId, Action<string> done)
        {
            string id = _ids.ProductId(productId);
            Product product = _controller != null && id.Length > 0 ? _controller.products.WithID(id) : null;
            if (product == null || !product.availableToPurchase)
            {
                done("That is not on sale right now.");
                return;
            }

            _pending = done;
            _controller.InitiatePurchase(product);
        }

        public void Restore(Action<IReadOnlyList<string>, string> done)
        {
            if (_controller == null)
            {
                done(new string[0], "The store is not reachable.");
                return;
            }

            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                _extensions.GetExtension<IAppleExtensions>().RestoreTransactions((ok, message) => done(Owned(), ok ? null : "Restore failed."));
                return;
            }

            done(Owned(), null);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;

            // Google Play restores on start: grant whatever already has a receipt
            Entitlements.Instance.Restore(_ => { });
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogWarning("[DEADSWITCH] Store unavailable: " + error);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogWarning("[DEADSWITCH] Store unavailable: " + error + " " + message);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchase)
        {
            Action<string> done = _pending;
            _pending = null;
            done?.Invoke(null);
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            Action<string> done = _pending;
            _pending = null;
            done?.Invoke(reason == PurchaseFailureReason.UserCancelled ? "Cancelled." : "The purchase did not go through.");
        }

        private void Initialize()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            int added = 0;
            foreach (ProductIds p in _ids.products)
            {
                string id = _ids.ProductId(p.key);
                if (id.Length > 0)
                {
                    builder.AddProduct(id, ProductType.NonConsumable);
                    added++;
                }
            }

            if (added > 0)
            {
                UnityPurchasing.Initialize(this, builder);
            }
        }

        private IReadOnlyList<string> Owned()
        {
            var owned = new List<string>();
            foreach (Product p in _controller.products.all)
            {
                string key = _ids.KeyOf(p.definition.id);
                if (p.hasReceipt && key != null)
                {
                    owned.Add(key);
                }
            }

            return owned;
        }
    }
}
