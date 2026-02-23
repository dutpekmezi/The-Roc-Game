using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Systems;
using Utils.Currency;
using Utils.Popup;

namespace Game.UI
{
    public class ProductCard : MonoBehaviour
    {
        [SerializeField] private Image productImage;
        [SerializeField] private Image priceImage;
        [SerializeField] private Image specialPriceImage;

        [SerializeField] private TextMeshProUGUI productTitle;
        [SerializeField] private TextMeshProUGUI priceAmount;
        [SerializeField] private TextMeshProUGUI specialPriceAmount;

        private ProductConfig productConfig;

        public void Init(ProductConfig productConfig)
        {
            this.productConfig = productConfig;

            productImage.sprite = productConfig.Sprite;
            priceImage.sprite = CurrencyService.Instance.GetCurrencyConfig(productConfig.PriceCurrency).currencySprite;
            specialPriceImage.sprite = CurrencyService.Instance.GetCurrencyConfig(productConfig.specialPriceCurrency).currencySprite;

            productTitle.text = $"{productConfig.Name}";
            priceAmount.text = $"{productConfig.PriceAmount}";
            specialPriceAmount.text = $"{productConfig.specialPriceAmount}";
        }

        public void OnClick()
        {
            var popupService = PopupService.Instance;
            if (popupService != null && popupService.Get(ProductCardPopUp.PopupKey) == null)
            {
                ProductCardPopUp instance = (ProductCardPopUp)popupService.Create(ProductCardPopUp.PopupKey);
                instance.Init(productConfig);
            }
        }
    }
}
