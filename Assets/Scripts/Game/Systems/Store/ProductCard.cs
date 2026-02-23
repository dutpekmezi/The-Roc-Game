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
            var prices = productConfig.Prices;

            productImage.sprite = productConfig.Sprite;
            if (prices.Count > 0)
            {
                priceImage.sprite = CurrencyService.Instance.GetCurrencyConfig(prices[0].currency).currencySprite;
                priceAmount.text = $"{prices[0].amount}";
            }
            else
            {
                priceAmount.text = "0";
            }

            if (prices.Count > 1)
            {
                specialPriceImage.sprite = CurrencyService.Instance.GetCurrencyConfig(prices[1].currency).currencySprite;
                specialPriceAmount.text = $"{prices[1].amount}";
            }
            else
            {
                specialPriceAmount.text = "0";
            }

            productTitle.text = $"{productConfig.Name}";
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
