using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using ZXing;

namespace GymManager.IntegrationTests;

/// <summary>
/// Verifies the barcode endpoint doesn't just return PNG bytes but a barcode that actually decodes back to
/// the member's check-in code — the gap PROJECT_STATUS.md flagged ("only QR PNG generation was built").
/// </summary>
public sealed class BarcodeGenerationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record BranchResponse(Guid Id);

    private sealed record MemberResponse(Guid Id, string CheckInCode);

    [Fact]
    public async Task GetMemberBarcode_Should_Return_A_Code128_Image_That_Decodes_To_The_CheckInCode()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Members.View);

        var branchResponse = await client.PostAsJsonAsync("/api/v1/branches", new
        {
            name = $"Branch-{Guid.NewGuid():N}",
            country = "USA",
            street = (string?)null,
            city = (string?)null,
            state = (string?)null,
            postalCode = (string?)null,
            phoneNumber = (string?)null,
            email = (string?)null,
        });
        var branchId = (await branchResponse.Content.ReadFromJsonAsync<BranchResponse>())!.Id;

        var memberResponse = await client.PostAsJsonAsync("/api/v1/members", new
        {
            branchId,
            firstName = "Jane",
            lastName = "Doe",
            phoneNumber = $"+1555{Random.Shared.Next(1000000, 9999999)}",
            email = (string?)null,
            dateOfBirth = (DateOnly?)null,
            gender = 2,
            street = (string?)null,
            city = (string?)null,
            state = (string?)null,
            postalCode = (string?)null,
            country = (string?)null,
            emergencyContactName = (string?)null,
            emergencyContactPhone = (string?)null,
        });
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberResponse>();

        var barcodeResponse = await client.GetAsync($"/api/v1/attendance/members/{member!.Id}/barcode");

        Assert.Equal(HttpStatusCode.OK, barcodeResponse.StatusCode);
        Assert.Equal("image/png", barcodeResponse.Content.Headers.ContentType?.MediaType);

        var pngBytes = await barcodeResponse.Content.ReadAsByteArrayAsync();
        Assert.True(pngBytes.Length > 0);

        using var image = Image.Load<Rgba32>(pngBytes);
        var pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);

        var luminanceSource = new RGBLuminanceSource(pixels, image.Width, image.Height, RGBLuminanceSource.BitmapFormat.RGBA32);
        var reader = new BarcodeReaderGeneric { AutoRotate = true, Options = { PossibleFormats = [BarcodeFormat.CODE_128] } };
        var result = reader.Decode(luminanceSource);

        Assert.NotNull(result);
        Assert.Equal(member.CheckInCode, result.Text);
    }
}
