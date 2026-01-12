using QRCoder;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace DestinyPOS2026.Wpf.Helpers;

public static class QrCodeHelper
{
    public static BitmapImage GenerateQRCode(string content, int pixelsPerModule = 10)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new QRCode(qrCodeData);
        using Bitmap bitmap = qrCode.GetGraphic(pixelsPerModule);

        using var memory = new MemoryStream();
        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
        memory.Position = 0;

        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = memory;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();

        return image;
    }
}
