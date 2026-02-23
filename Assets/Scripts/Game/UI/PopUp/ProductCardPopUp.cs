using Game.Systems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Utils.Popup;
using Utils.Currency;

namespace Game.UI
{
    public class ProductCardPopUp : PopupBase
    {
        public const string PopupKey = "product_card";
        public override string PopupId => PopupKey;

        [SerializeField] private Image productImage;
        [SerializeField] private Image priceImage;
        [SerializeField] private Image specialPriceImage;
        [SerializeField] private Image cardImage;

        [SerializeField] private TextMeshProUGUI productTitle;
        [SerializeField] private TextMeshProUGUI priceAmountText;
        [SerializeField] private TextMeshProUGUI specialPriceAmountText;

        private ProductConfig productConfig;
        public ProductConfig ProductConfig => productConfig;

        public void Init(ProductConfig productConfig)
        {
            this.productConfig = productConfig;
            var prices = productConfig.Prices;

            productImage.sprite = productConfig.Sprite;
            if (prices.Count > 0)
            {
                priceImage.sprite = CurrencyService.Instance.GetCurrencyConfig(prices[0].currency).currencySprite;
                priceAmountText.text = $"{prices[0].amount}";
            }
            else
            {
                priceAmountText.text = "0";
            }

            if (prices.Count > 1)
            {
                specialPriceImage.sprite = CurrencyService.Instance.GetCurrencyConfig(prices[1].currency).currencySprite;
                specialPriceAmountText.text = $"{prices[1].amount}";
            }
            else
            {
                specialPriceAmountText.text = "0";
            }

            if (StoreManager.Instance.StoreSettings.ProductConfigs.TryGetSectionColor(productConfig.section, out Color sectionColor))
            {
                //cardImage.color = sectionColor;
            }

            productTitle.text = productConfig.Name;
        }
    }
}
