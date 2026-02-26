using Game.Systems;
using UnityEngine;
using Utils.Buttons;
using Utils.Currency;
using Utils.Popup;

namespace Game.UI
{
    public class PurchaseButton : BaseButton
    {
        private static bool isPurchaseInProgress;

        public override async void BaseOnClick()
        {
            base.BaseOnClick();

            if (isPurchaseInProgress)
            {
                return;
            }

            var productConfig = PopupService.Instance.Get<ProductCardPopUp>().ProductConfig;

            if (StoreManager.Instance.IsProductPurchased(productConfig.Id))
            {
                return;
            }

            var prices = productConfig.Prices;

            for (int i = 0; i < prices.Count; i++)
            {
                if (!CurrencyService.Instance.CanPurchase(prices[i].currency, prices[i].amount))
                {
                    return;
                }
            }

            isPurchaseInProgress = true;

            try
            {
                var firebaseService = FirestoreGameSecurityService.Instance;
                if (firebaseService != null && firebaseService.IsReady)
                {
                    PurchaseResult result = await firebaseService.TryPurchaseProductAsync(productConfig);
                    if (!result.IsSuccess)
                    {
                        Debug.LogWarning(result.Error);
                        return;
                    }

                    for (int i = 0; i < prices.Count; i++)
                    {
                        CurrencyService.Instance.TryPurchase(prices[i].currency, prices[i].amount);
                    }

                    StoreManager.Instance.RegisterPurchasedProduct(productConfig.Id, result.QrPayload);
                    return;
                }

                for (int i = 0; i < prices.Count; i++)
                {
                    CurrencyService.Instance.TryPurchase(prices[i].currency, prices[i].amount);
                }

                StoreManager.Instance.RegisterPurchasedProduct(productConfig.Id);
            }
            finally
            {
                isPurchaseInProgress = false;
            }
        }
    }
}
