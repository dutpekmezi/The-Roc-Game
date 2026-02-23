using Game.Systems;
using UnityEngine;
using UnityEngine.UI;
using Utils.Popup;

namespace Game.UI
{
    public class QRPopUp : PopupBase
    {
        public const string PopupKey = "QR";
        public override string PopupId => PopupKey;

        [SerializeField] private Image productImage;
        [SerializeField] private int qrTextureSize = 256;
        [SerializeField] private int gridSize = 29;
        [SerializeField] private Color darkColor = Color.black;
        [SerializeField] private Color lightColor = Color.white;


        public void Init(string productId)
        {
            string payload = StoreManager.Instance.GetQRCodePayloadByProductId(productId);
            productImage.sprite = GenerateQrSprite(payload);
        }

        public void OnClick()
        {
            Disappear();
        }

        private Sprite GenerateQrSprite(string payload)
        {
            int safeGridSize = Mathf.Max(21, gridSize);
            Texture2D texture = new Texture2D(qrTextureSize, qrTextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[qrTextureSize * qrTextureSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = lightColor;
            }

            int cellSize = Mathf.Max(1, qrTextureSize / safeGridSize);
            int contentSize = cellSize * safeGridSize;
            int margin = Mathf.Max(0, (qrTextureSize - contentSize) / 2);

            DrawFinderPattern(pixels, qrTextureSize, margin, margin, cellSize);
            DrawFinderPattern(pixels, qrTextureSize, margin + (safeGridSize - 7) * cellSize, margin, cellSize);
            DrawFinderPattern(pixels, qrTextureSize, margin, margin + (safeGridSize - 7) * cellSize, cellSize);

            int seed = payload.GetHashCode();
            for (int y = 0; y < safeGridSize; y++)
            {
                for (int x = 0; x < safeGridSize; x++)
                {
                    if (IsInsideFinderPattern(x, y, safeGridSize))
                    {
                        continue;
                    }

                    seed = seed * 1664525 + 1013904223;
                    bool isDark = (seed & 1) == 0;
                    if (isDark)
                    {
                        FillCell(pixels, qrTextureSize, margin + x * cellSize, margin + y * cellSize, cellSize, darkColor);
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, qrTextureSize, qrTextureSize), new Vector2(0.5f, 0.5f));
        }

        private bool IsInsideFinderPattern(int x, int y, int size)
        {
            return (x < 7 && y < 7) || (x >= size - 7 && y < 7) || (x < 7 && y >= size - 7);
        }

        private void DrawFinderPattern(Color[] pixels, int textureSize, int startX, int startY, int cellSize)
        {
            for (int y = 0; y < 7; y++)
            {
                for (int x = 0; x < 7; x++)
                {
                    bool isOuter = x == 0 || y == 0 || x == 6 || y == 6;
                    bool isInner = x >= 2 && x <= 4 && y >= 2 && y <= 4;
                    Color color = (isOuter || isInner) ? darkColor : lightColor;

                    FillCell(pixels, textureSize, startX + x * cellSize, startY + y * cellSize, cellSize, color);
                }
            }
        }

        private void FillCell(Color[] pixels, int textureSize, int startX, int startY, int cellSize, Color color)
        {
            for (int y = 0; y < cellSize; y++)
            {
                for (int x = 0; x < cellSize; x++)
                {
                    int pixelX = startX + x;
                    int pixelY = startY + y;

                    if (pixelX < 0 || pixelX >= textureSize || pixelY < 0 || pixelY >= textureSize)
                    {
                        continue;
                    }

                    pixels[pixelY * textureSize + pixelX] = color;
                }
            }
        }
    }
}
