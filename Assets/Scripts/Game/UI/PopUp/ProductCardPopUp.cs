using Game.Systems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Utils.Popup;
using Utils.Currency;
using VContainer;

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
        private ICurrencyService _currencyService;

        [Inject]
        private void Construct(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public void Init(ProductConfig productConfig)
        {
            this.productConfig = productConfig;
            var currencyService = _currencyService ?? CurrencyService.Instance;
            var storeManager = StoreManager.Instance;
            var prices = storeManager != null
                ? storeManager.GetProductPrices(productConfig)
                : productConfig.Prices;

            productImage.sprite = productConfig.Sprite;
            if (prices.Count > 0)
            {
                priceImage.sprite = currencyService.GetCurrencyConfig(prices[0].currency).currencySprite;
                priceAmountText.text = $"{prices[0].amount}";
            }
            else
            {
                priceAmountText.text = "0";
            }

            if (prices.Count > 1)
            {
                specialPriceImage.sprite = currencyService.GetCurrencyConfig(prices[1].currency).currencySprite;
                specialPriceAmountText.text = $"{prices[1].amount}";
            }
            else
            {
                specialPriceAmountText.text = "0";
            }

            if (storeManager != null &&
                storeManager.StoreSettings.ProductConfigs.TryGetSectionColor(productConfig.section, out Color sectionColor))
            {
                //cardImage.color = sectionColor;
            }

            productTitle.text = productConfig.Name;
        }
    }
}
