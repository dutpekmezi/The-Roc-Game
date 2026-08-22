using Game.Systems;
using UnityEngine;
using UnityEngine.UI;
using Utils.Popup;
using VContainer;

namespace Game.UI
{
    public class QRPopUp : PopupBase
    {
        public const string PopupKey = "QR";
        public override string PopupId => PopupKey;

        [SerializeField] private Image productImage;
        [SerializeField] private int qrTextureSize = 256;

        public void Init(string productId)
        {
            var storeManager = StoreManager.Instance;
            if (storeManager == null)
            {
                return;
            }

            string payload = storeManager.GetQRCodePayloadByProductId(productId);
            productImage.sprite = GenerateQrSprite(payload);
        }

        public void OnClick()
        {
            Disappear();
        }

        private Sprite GenerateQrSprite(string payload)
        {
            Texture2D texture = QRCodeGenerator.GenerateTexture(payload, qrTextureSize);

            return Sprite.Create(texture, new Rect(0, 0, qrTextureSize, qrTextureSize), new Vector2(0.5f, 0.5f));
        }
    }
}
