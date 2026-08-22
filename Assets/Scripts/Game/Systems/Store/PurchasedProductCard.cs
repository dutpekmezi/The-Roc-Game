using Game.Systems;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Currency;
using Utils.Popup;
using VContainer;

namespace Game.UI
{
    public class PurchasedProductCard : MonoBehaviour
    {
        [SerializeField] private Image productImage;

        private ProductConfig productConfig;
        private PopupService _popupService;

        [Inject]
        private void Construct(PopupService popupService)
        {
            _popupService = popupService;
        }

        public void Init(ProductConfig productConfig)
        {
            this.productConfig = productConfig;

            productImage.sprite = productConfig.Sprite;
        }

        public void OnClick()
        {
            var popupService = _popupService ?? PopupService.Instance;
            if (popupService != null && popupService.Get(QRPopUp.PopupKey) == null)
            {
                QRPopUp instance = (QRPopUp)popupService.Create(QRPopUp.PopupKey);
                instance.Init(productConfig.Id);
            }
        }
    }
}
