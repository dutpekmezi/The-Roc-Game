using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.QrCode;

public class QRCodeGenerator : MonoBehaviour
{
    public RawImage targetImage;
    public string text = "https://google.com";
    public int size = 512;

    void Start()
    {
        GenerateQR();
    }

    public void GenerateQR()
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Width = size,
                Height = size,
                Margin = 1
            }
        };

        var pixelData = writer.Write(text);

        Texture2D texture = new Texture2D(pixelData.Width, pixelData.Height);
        texture.LoadRawTextureData(pixelData.Pixels);
        texture.Apply();

        targetImage.texture = texture;
    }
}