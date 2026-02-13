using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Systems;
using Utils.Currency;

namespace Game.UI
{
    public class ProductCard : MonoBehaviour
    {
        [SerializeField] private Image productImage;
        [SerializeField] private Image priceImage;
        [SerializeField] private Image specialPriceImage;

        [SerializeField] private TextMeshProUGUI priceAmount;
        [SerializeField] private TextMeshProUGUI specialPriceAmount;

        private ProductConfig productConfig;

        public void Init(ProductConfig productConfig)
        {
            this.productConfig = productConfig;

            productImage.sprite = productConfig.Sprite;
            priceImage.sprite = CurrencyService.Instance.GetCurrencyConfig(productConfig.priceCurrency).currencySprite;
            specialPriceImage.sprite = CurrencyService.Instance.GetCurrencyConfig(productConfig.specialPriceCurrency).currencySprite;

            priceAmount.text = $"{productConfig.priceAmount}";
            specialPriceAmount.text = $"{productConfig.specialPriceAmount}";
        }
    }
}