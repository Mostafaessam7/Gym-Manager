using GymManager.Application.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using ZXing;
using ZXing.Common;

namespace GymManager.Infrastructure.Attendance;

/// <inheritdoc cref="IBarcodeGenerator"/>
public sealed class BarcodeGenerator : IBarcodeGenerator
{
    public byte[] GeneratePng(string content, int width = 300, int height = 100)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions { Width = width, Height = height, Margin = 10, PureBarcode = false },
        };

        var pixelData = writer.Write(content);

        using var image = Image.LoadPixelData<Bgra32>(pixelData.Pixels, pixelData.Width, pixelData.Height);
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());

        return stream.ToArray();
    }
}
