using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLift.Popup;
using GameLift.Signal;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Purchasing;
using VContainer.Unity;

namespace GameLift.Purchasing
{
    public class PurchasingService : IPurchasingService, IAsyncStartable
    {
        private readonly ISignalBus _signalBus;
        private readonly PurchasingSettings _settings;
        private readonly IPopupService _popupService;

        private StoreController _storeController;
        private List<Product> _products = new();
        private PurchasingLoadingOverlay _loadingOverlay;

        public bool IsInitialized { get; private set; }

        public PurchasingService(ISignalBus signalBus, PurchasingSettings settings, IPopupService popupService)
        {
            _signalBus = signalBus;
            _settings = settings;
            _popupService = popupService;
        }

        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            if (IsInitialized)
                return;

            try
            {
                var options = new InitializationOptions().SetEnvironmentName(_settings.Environment);
                await UnityServices.InitializeAsync(options);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PurchasingService] Unity Gaming Services initialization failed: {e.Message}");
                _signalBus.Get<OnStoreInitializeFailedSignal>().Invoke(e.Message);
                return;
            }

            _storeController = UnityIAPServices.StoreController();

            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _storeController.OnPurchaseFailed += OnPurchaseFailed;
            _storeController.OnPurchaseDeferred += OnPurchaseDeferred;
            _storeController.OnStoreConnected += OnStoreConnected;
            _storeController.OnStoreDisconnected += OnStoreDisconnected;
            _storeController.OnProductsFetched += OnProductsFetched;
            _storeController.OnProductsFetchFailed += OnProductsFetchFailed;

            Debug.Log("[PurchasingService] Connecting to store.");
            await _storeController.Connect();
        }

        // RESTORE PURCHASES

        public void RestorePurchases()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[PurchasingService] Cannot restore — store not initialized.");
                return;
            }

            FetchProducts(); // Unity IAP 5.x restores entitlements on product fetch
        }

        // GET PRODUCTS FUNCTIONS

        public Product GetProduct(string productId)
        {
            return _products.FirstOrDefault(p => p.definition.id == productId);
        }

        public List<Product> GetAllProducts()
        {
            return _products;
        }

        public List<ProductConfig> GetAllProductConfigs()
        {
            return _settings.Products;
        }

        private void OnStoreConnected()
        {
            Debug.Log("[PurchasingService] Store connected.");
            FetchProducts();
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            Debug.LogError($"[PurchasingService] Store disconnected: {description.message}");
            _signalBus.Get<OnStoreInitializeFailedSignal>().Invoke(description.message);
        }

        private void FetchProducts()
        {
            var productDefinitions = new List<ProductDefinition>();
            foreach (var config in _settings.Products)
            {
                productDefinitions.Add(new ProductDefinition(config.ProductId, config.Type));
            }

            _storeController.FetchProducts(productDefinitions);
        }

        private void OnProductsFetched(List<Product> products)
        {
            _products = products;
            IsInitialized = true;
            Debug.Log($"[PurchasingService] Products fetched: {products.Count}");
            _signalBus.Get<OnStoreInitializedSignal>().Invoke();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogError($"[PurchasingService] Products fetch failed: {failure.FailureReason}");
            _signalBus.Get<OnStoreInitializeFailedSignal>().Invoke(failure.FailureReason.ToString());
        }

        // PURCHASE FUNCTIONS

        public void PurchaseProduct(string productId)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[PurchasingService] Cannot purchase — store not initialized.");
                return;
            }

            ShowLoadingOverlay();
            _storeController.PurchaseProduct(productId);
        }

        private void OnPurchasePending(PendingOrder order)
        {
            var product = order.CartOrdered.Items().First()?.Product;
            if (product == null)
            {
                Debug.LogWarning("[PurchasingService] Could not find product in pending order.");
                HideLoadingOverlay();
                return;
            }

            Debug.Log($"[PurchasingService] Purchase pending: {product.definition.id}");
            _storeController.ConfirmPurchase(order);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case ConfirmedOrder confirmedOrder:
                    {
                        var product = confirmedOrder.CartOrdered.Items().First()?.Product;
                        Debug.Log($"[PurchasingService] Purchase confirmed: {product?.definition.id}");

                        _signalBus.Get<OnPurchaseCompletedSignal>().Invoke(product);

                        HideLoadingOverlay();
                        break;
                    }
                case FailedOrder failedOrder:
                    {
                        var product = failedOrder.CartOrdered.Items().First()?.Product;
                        Debug.LogError($"[PurchasingService] Purchase confirmation failed: {product?.definition.id} - {failedOrder.FailureReason}");
                        _signalBus.Get<OnPurchaseFailedSignal>().Invoke(
                            product ?? null,
                            failedOrder.FailureReason.ToString());
                        HideLoadingOverlay();
                        break;
                    }
            }
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            var product = order.CartOrdered.Items().First()?.Product;
            Debug.LogError($"[PurchasingService] Purchase failed: {product?.definition.id} - {order.FailureReason}");
            _signalBus.Get<OnPurchaseFailedSignal>().Invoke(
                product ?? null,
                order.FailureReason.ToString());
            HideLoadingOverlay();
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            var product = order.CartOrdered.Items().First()?.Product;
            Debug.Log($"[PurchasingService] Purchase deferred: {product?.definition.id}");
            HideLoadingOverlay();
        }

        // PURCHASE PENDING POPUP

        private void ShowLoadingOverlay()
        {
            try
            {
                _loadingOverlay = _popupService.Create<PurchasingLoadingOverlay>(forceShow: true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PurchasingService] Could not show loading overlay: {e.Message}");
            }
        }

        private void HideLoadingOverlay()
        {
            try
            {
                if (_loadingOverlay != null)
                {
                    _loadingOverlay.Disappear();
                    _loadingOverlay = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PurchasingService] Could not hide loading overlay: {e.Message}");
            }
        }

    }
}
