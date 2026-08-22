using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Systems;
using Utils.Currency;
using Utils.Popup;
using VContainer;

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
        private ICurrencyService _currencyService;
        private PopupService _popupService;

        [Inject]
        private void Construct(ICurrencyService currencyService, PopupService popupService)
        {
            _currencyService = currencyService;
            _popupService = popupService;
        }

        public void Init(ProductConfig productConfig)
        {
            this.productConfig = productConfig;
            var storeManager = StoreManager.Instance;
            var prices = storeManager != null
                ? storeManager.GetProductPrices(productConfig)
                : productConfig.Prices;
            var currencyService = _currencyService ?? CurrencyService.Instance;

            productImage.sprite = productConfig.Sprite;
            if (prices.Count > 0)
            {
                priceImage.sprite = currencyService.GetCurrencyConfig(prices[0].currency).currencySprite;
                priceAmount.text = $"{prices[0].amount}";
            }
            else
            {
                priceAmount.text = "0";
            }

            if (prices.Count > 1)
            {
                specialPriceImage.sprite = currencyService.GetCurrencyConfig(prices[1].currency).currencySprite;
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
            var popupService = _popupService ?? PopupService.Instance;
            if (popupService != null && popupService.Get(ProductCardPopUp.PopupKey) == null)
            {
                ProductCardPopUp instance = (ProductCardPopUp)popupService.Create(ProductCardPopUp.PopupKey);
                instance.Init(productConfig);
            }
        }
    }
}
