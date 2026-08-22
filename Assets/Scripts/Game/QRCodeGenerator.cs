using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

public class QRCodeGenerator : MonoBehaviour
{
    [SerializeField] private RawImage targetImage;
    [SerializeField] private string text = "https://google.com";
    [SerializeField] private int size = 512;

    private void Start()
    {
        GenerateQR();
    }

    public void GenerateQR()
    {
        if (targetImage == null)
        {
            return;
        }

        targetImage.texture = GenerateTexture(text, size);
    }

    public static Texture2D GenerateTexture(string payload, int textureSize, int margin = 1)
    {
        if (string.IsNullOrEmpty(payload))
        {
            payload = "empty";
        }

        int size = Mathf.Max(32, textureSize);
        var writer = new BarcodeWriterGeneric
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                CharacterSet = "UTF-8",
                Width = size,
                Height = size,
                Margin = Mathf.Max(0, margin)
            }
        };

        BitMatrix matrix = writer.Encode(payload);
        int width = matrix.Width;
        int height = matrix.Height;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[width * height];
        Color32 dark = new Color32(0, 0, 0, 255);
        Color32 light = new Color32(255, 255, 255, 255);

        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                pixels[row + x] = matrix[x, y] ? dark : light;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        return texture;
    }
}
