using Game.Systems;
using UnityEngine;
using Utils.Buttons;
using Utils.Currency;
using Utils.Popup;
using VContainer;

namespace Game.UI
{
    public class PurchaseButton : BaseButton
    {
        private static bool isPurchaseInProgress;
        private PopupService _popupService;
        private CurrencyService _currencyService;
        private FirestoreGameSecurityService _firestoreService;

        [Inject]
        private void Construct(
            PopupService popupService,
            CurrencyService currencyService,
            FirestoreGameSecurityService firestoreService)
        {
            _popupService = popupService;
            _currencyService = currencyService;
            _firestoreService = firestoreService;
        }

        public override async void BaseOnClick()
        {
            base.BaseOnClick();

            if (isPurchaseInProgress)
            {
                return;
            }

            var popupService = _popupService ?? PopupService.Instance;
            var currencyService = _currencyService ?? CurrencyService.Instance;
            var storeManager = StoreManager.Instance;

            if (popupService == null || currencyService == null || storeManager == null)
            {
                return;
            }

            var productPopup = popupService.Get<ProductCardPopUp>();
            if (productPopup == null)
            {
                return;
            }

            var productConfig = productPopup.ProductConfig;

            if (storeManager.IsProductPurchased(productConfig.Id))
            {
                return;
            }

            var prices = storeManager.GetProductPrices(productConfig);

            isPurchaseInProgress = true;

            try
            {
                var firebaseService = _firestoreService != null
                    ? _firestoreService
                    : FirestoreGameSecurityService.Instance;

#if UNITY_WEBGL && !UNITY_EDITOR
                if (firebaseService != null)
                {
                    await currencyService.RefreshFromFirebaseAsync();
                    PurchaseResult result = await firebaseService.TryPurchaseProductAsync(productConfig);
                    if (!result.IsSuccess)
                    {
                        await currencyService.RefreshFromFirebaseAsync();
                        Debug.LogWarning(result.Error);
                        return;
                    }

                    await currencyService.RefreshFromFirebaseAsync();
                    storeManager.RegisterPurchasedProduct(productConfig.Id, result.QrPayload);
                    return;
                }
#else
                if (firebaseService != null && firebaseService.IsReady)
                {
                    await currencyService.RefreshFromFirebaseAsync();
                    PurchaseResult result = await firebaseService.TryPurchaseProductAsync(productConfig);
                    if (!result.IsSuccess)
                    {
                        await currencyService.RefreshFromFirebaseAsync();
                        Debug.LogWarning(result.Error);
                        return;
                    }

                    await currencyService.RefreshFromFirebaseAsync();
                    storeManager.RegisterPurchasedProduct(productConfig.Id, result.QrPayload);
                    return;
                }
#endif

                for (int i = 0; i < prices.Count; i++)
                {
                    if (!currencyService.CanPurchase(prices[i].currency, prices[i].amount))
                    {
                        return;
                    }
                }

                for (int i = 0; i < prices.Count; i++)
                {
                    currencyService.TryPurchase(prices[i].currency, prices[i].amount);
                }

                storeManager.RegisterPurchasedProduct(productConfig.Id);
            }
            finally
            {
                isPurchaseInProgress = false;
            }
        }
    }
}
