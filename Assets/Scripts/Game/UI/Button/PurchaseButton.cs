using Game.Systems;
using UnityEngine;
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

            if (CurrencyService.Instance.TryPurchase(productConfig.priceCurrency, productConfig.priceAmount))
            {
                StoreManager.Instance.RegisterPurchasedProduct(productConfig.Id);
            }
        }
    }
}