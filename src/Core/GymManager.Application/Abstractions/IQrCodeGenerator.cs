namespace GymManager.Application.Abstractions;

/// <summary>Renders a check-in code as a scannable QR code image.</summary>
public interface IQrCodeGenerator
{
    /// <summary>Returns a PNG-encoded QR code for <paramref name="content"/>.</summary>
    byte[] GeneratePng(string content, int pixelsPerModule = 10);
}
