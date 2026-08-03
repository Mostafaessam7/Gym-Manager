using GymManager.Application.Abstractions;
using QRCoder;

namespace GymManager.Infrastructure.Attendance;

/// <inheritdoc cref="IQrCodeGenerator"/>
public sealed class QrCodeGenerator : IQrCodeGenerator
{
    public byte[] GeneratePng(string content, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var pngQrCode = new PngByteQRCode(data);

        return pngQrCode.GetGraphic(pixelsPerModule);
    }
}
