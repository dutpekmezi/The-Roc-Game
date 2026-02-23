using UnityEngine;
using UnityEngine.UI;
using ZXing;
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
            payload = string.Empty;
        }

        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Width = textureSize,
                Height = textureSize,
                Margin = Mathf.Max(0, margin)
            }
        };

        var pixelData = writer.Write(payload);

        Texture2D texture = new Texture2D(pixelData.Width, pixelData.Height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        texture.LoadRawTextureData(pixelData.Pixels);
        texture.Apply();

        return texture;
    }
}
