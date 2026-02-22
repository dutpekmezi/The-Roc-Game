using Game.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Currency;
using Utils.Popup;

namespace Game.UI
{
    public class QRPopUp : PopupBase
    {
        public const string PopupKey = "product_card";
        public override string PopupId => PopupKey;

        [SerializeField] private Image productImage;

        private ProductConfig productConfig;
        public ProductConfig ProductConfig => productConfig;

        public void Init(string productId)
        {
            //this.productConfig = productConfig;

            //productImage.sprite = productConfig.Sprite;
        }
    }
}
