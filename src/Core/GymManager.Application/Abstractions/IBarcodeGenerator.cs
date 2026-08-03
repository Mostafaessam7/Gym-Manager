namespace GymManager.Application.Abstractions;

/// <summary>Renders a check-in code as a scannable Code128 barcode image, for <c>CheckInMethod.Barcode</c>
/// check-ins (as opposed to the QR code produced by <see cref="IQrCodeGenerator"/>).</summary>
public interface IBarcodeGenerator
{
    /// <summary>Returns a PNG-encoded Code128 barcode for <paramref name="content"/>.</summary>
    byte[] GeneratePng(string content, int width = 300, int height = 100);
}
