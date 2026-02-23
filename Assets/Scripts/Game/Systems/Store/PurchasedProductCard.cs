using Game.Systems;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Currency;
using Utils.Popup;

namespace Game.UI
{
    public class PurchasedProductCard : MonoBehaviour
    {
        [SerializeField] private Image productImage;

        private ProductConfig productConfig;

        public void Init(ProductConfig productConfig)
        {
            this.productConfig = productConfig;

            productImage.sprite = productConfig.Sprite;
        }

        public void OnClick()
        {
            var popupService = PopupService.Instance;
            if (popupService != null && popupService.Get(QRPopUp.PopupKey) == null)
            {
                QRPopUp instance = (QRPopUp)popupService.Create(QRPopUp.PopupKey);
                instance.Init(productConfig.Id);
            }
        }
    }
}