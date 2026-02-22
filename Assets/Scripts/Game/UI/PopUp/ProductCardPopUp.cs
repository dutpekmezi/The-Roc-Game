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

            productImage.sprite = productConfig.Sprite;
            priceImage.sprite = CurrencyService.Instance.GetCurrencyConfig(productConfig.priceCurrency).currencySprite;
            specialPriceImage.sprite = CurrencyService.Instance.GetCurrencyConfig(productConfig.specialPriceCurrency).currencySprite;

            if (StoreManager.Instance.StoreSettings.ProductConfigs.TryGetSectionColor(productConfig.section, out Color sectionColor))
            {
                //cardImage.color = sectionColor;
            }

            productTitle.text = productConfig.Name;
            priceAmountText.text = $"{productConfig.priceAmount}";
            specialPriceAmountText.text = $"{productConfig.specialPriceAmount}";
        }
    }
}