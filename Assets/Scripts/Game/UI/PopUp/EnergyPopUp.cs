using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Utils.Popup;

namespace Game.UI
{
    public class EnergyPopUp : PopupBase
    {
        public const string PopupKey = "energy_popup";
        public override string PopupId => PopupKey;

        private const string FallbackUserIdPrefsKey = "EnergyPopUp.FallbackUserId";

        [SerializeField] private Image qrImage;
        [SerializeField] private int qrTextureSize = 512;

        private Sprite generatedSprite;

        protected override void Awake()
        {
            base.Awake();
            PostAppear += RefreshQrAsync;
        }

        private void OnDestroy()
        {
            PostAppear -= RefreshQrAsync;
            DestroyGeneratedSprite();
        }

        public void OnClick()
        {
            Disappear();
        }

        private async void RefreshQrAsync()
        {
            FirestoreGameSecurityService firestoreService =
                await WaitForFirestoreServiceAsync();

            if (this == null || qrImage == null)
            {
                return;
            }

            string userId = firestoreService != null
                ? firestoreService.GetUserId()
                : string.Empty;

            if (string.IsNullOrEmpty(userId))
            {
                userId = LoadOrCreateFallbackUserId();
                Debug.LogWarning("[EnergyPopUp] Firebase user id bulunamadigi icin gecici QR user id kullaniliyor.");
            }

            string payload = $"rocenergy:v1:{userId}";
            Texture2D texture = QRCodeGenerator.GenerateTexture(payload, qrTextureSize, 2);

            DestroyGeneratedSprite();
            generatedSprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);

            qrImage.enabled = true;
            qrImage.color = Color.white;
            qrImage.type = Image.Type.Simple;
            qrImage.preserveAspect = true;
            qrImage.sprite = generatedSprite;
        }

        private static async Task<FirestoreGameSecurityService> WaitForFirestoreServiceAsync()
        {
            for (;;)
            {
                FirestoreGameSecurityService service = FirestoreGameSecurityService.Instance;
#if UNITY_WEBGL && !UNITY_EDITOR
                if (service != null)
                {
                    return service;
                }
#else
                if (service != null)
                {
                    bool isReady = service.IsReady || await service.InitializeServiceAsync();
                    return isReady ? service : null;
                }
#endif

                await UniTask.Delay(100);
            }
        }

        private static string LoadOrCreateFallbackUserId()
        {
            string savedUserId = PlayerPrefs.GetString(FallbackUserIdPrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(savedUserId))
            {
                return savedUserId;
            }

            string fallbackUserId = "qr-" + Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(FallbackUserIdPrefsKey, fallbackUserId);
            PlayerPrefs.Save();

            return fallbackUserId;
        }

        private void DestroyGeneratedSprite()
        {
            if (generatedSprite == null)
            {
                return;
            }

            Texture texture = generatedSprite.texture;
            Destroy(generatedSprite);
            generatedSprite = null;

            if (texture != null)
            {
                Destroy(texture);
            }
        }
    }
}
