using Game.Systems;
using Utils.Buttons;
using Utils.Currency;
using Utils.Popup;

namespace Game.UI
{
    public class PurchaseButton : BaseButton
    {
        public override void BaseOnClick()
        {
            base.BaseOnClick();

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

            for (int i = 0; i < prices.Count; i++)
            {
                CurrencyService.Instance.TryPurchase(prices[i].currency, prices[i].amount);
            }

            StoreManager.Instance.RegisterPurchasedProduct(productConfig.Id);
        }
    }
}
