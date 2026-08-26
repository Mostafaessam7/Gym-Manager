using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using GymManager.Domain.Common;
using GymManager.Infrastructure.PaymentGateways;
using Xunit;

namespace GymManager.UnitTests.PaymentGateways;

/// <summary>
/// Exercises <see cref="FawryPaymentGatewayService"/>'s request-building, response-parsing, and signature
/// verification against a fake HTTP handler — same style and same caveat as
/// <c>PaymobPaymentGatewayServiceTests</c>: proves internal consistency, not confirmation against a real
/// Fawry merchant sandbox (none was available).
/// </summary>
public sealed class FawryPaymentGatewayServiceTests
{
    private const string SecurityKey = "fake_fawry_security_key_for_signature_verification";
    private const string MerchantCode = "fake_merchant_code";

    private static (FawryPaymentGatewayService Service, FakeFawryHttpMessageHandler Handler) CreateService()
    {
        var handler = new FakeFawryHttpMessageHandler();
        var options = new FawryOptions { MerchantCode = MerchantCode, SecurityKey = SecurityKey, BaseUrl = "https://fake.fawry.test" };
        var service = new FawryPaymentGatewayService(options, handler);
        return (service, handler);
    }

    private static string ComputeSignature(params string[] fields) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Concat(fields)))).ToLowerInvariant();

    [Fact]
    public async Task CreatePaymentIntentAsync_Should_Return_The_Fawry_Reference_Number()
    {
        var (service, handler) = CreateService();

        var result = await service.CreatePaymentIntentAsync(
            Money.Create(49.99m).Value, "member@example.com",
            new Dictionary<string, string> { ["gymManagerPaymentId"] = "11111111-1111-1111-1111-111111111111" });

        Assert.True(result.IsSuccess);
        Assert.Equal("9988776655", result.Value.GatewayReferenceId);
        Assert.Equal("9988776655", result.Value.ClientSecret);
        Assert.Equal("NEW", result.Value.Status);
        Assert.Contains("/payments/charge", handler.Requests[0].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_Should_Use_Our_Own_Payment_Id_As_The_MerchantRefNumber()
    {
        var (service, handler) = CreateService();
        const string ourPaymentId = "22222222-2222-2222-2222-222222222222";

        await service.CreatePaymentIntentAsync(
            Money.Create(10m).Value, null, new Dictionary<string, string> { ["gymManagerPaymentId"] = ourPaymentId });

        Assert.Contains(ourPaymentId, handler.RequestBodies[0]);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_Should_Sign_The_Request_With_The_Documented_Field_Order()
    {
        var (service, handler) = CreateService();
        const string ourPaymentId = "33333333-3333-3333-3333-333333333333";

        await service.CreatePaymentIntentAsync(
            Money.Create(49.99m).Value, null, new Dictionary<string, string> { ["gymManagerPaymentId"] = ourPaymentId });

        var expectedSignature = ComputeSignature(MerchantCode, ourPaymentId, "49.99", "PAYATFAWRY", SecurityKey);
        Assert.Contains(expectedSignature, handler.RequestBodies[0]);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_When_The_Gateway_Rejects_Should_Return_A_Failure()
    {
        var (service, handler) = CreateService();
        handler.FailNextRequest = true;

        var result = await service.CreatePaymentIntentAsync(Money.Create(10m).Value, null, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.GatewayRequestFailed", result.Error.Code);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_When_Fawry_Returns_A_NonSuccess_StatusCode_Should_Return_A_Failure()
    {
        var (service, handler) = CreateService();
        handler.NextStatusCode = 500;

        var result = await service.CreatePaymentIntentAsync(Money.Create(10m).Value, null, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.GatewayRequestFailed", result.Error.Code);
    }

    [Fact]
    public async Task RefundAsync_Should_Report_Success_On_A_200_StatusCode()
    {
        var (service, _) = CreateService();

        var result = await service.RefundAsync("9988776655", amount: null);

        Assert.True(result.IsSuccess);
        Assert.Equal("refunded", result.Value.Status);
    }

    [Fact]
    public void ParseWebhookEvent_With_A_Genuinely_Valid_Signature_Should_Succeed()
    {
        var (service, _) = CreateService();
        var signature = ComputeSignature("9988776655", "our-ref", "49.99", "PAID", "PAYATFAWRY", SecurityKey);
        var payload = $$"""
            {
              "fawryRefNumber": "9988776655",
              "merchantRefNumber": "our-ref",
              "paymentAmount": "49.99",
              "orderStatus": "PAID",
              "paymentMethod": "PAYATFAWRY",
              "signature": "{{signature}}"
            }
            """;

        var result = service.ParseWebhookEvent(payload, signature);

        Assert.True(result.IsSuccess);
        Assert.Equal("9988776655", result.Value.GatewayReferenceId);
        Assert.Equal(GymManager.Application.Abstractions.PaymentGatewayEventOutcome.Succeeded, result.Value.Outcome);
    }

    [Theory]
    [InlineData("FAILED")]
    [InlineData("EXPIRED")]
    [InlineData("CANCELED")]
    public void ParseWebhookEvent_With_A_Failure_OrderStatus_Should_Map_To_Failed_Outcome(string orderStatus)
    {
        var (service, _) = CreateService();
        var signature = ComputeSignature("9988776655", "our-ref", "49.99", orderStatus, "PAYATFAWRY", SecurityKey);
        var payload = $$"""
            {
              "fawryRefNumber": "9988776655",
              "merchantRefNumber": "our-ref",
              "paymentAmount": "49.99",
              "orderStatus": "{{orderStatus}}",
              "paymentMethod": "PAYATFAWRY",
              "signature": "{{signature}}"
            }
            """;

        var result = service.ParseWebhookEvent(payload, signature);

        Assert.True(result.IsSuccess);
        Assert.Equal(GymManager.Application.Abstractions.PaymentGatewayEventOutcome.Failed, result.Value.Outcome);
    }

    [Fact]
    public void ParseWebhookEvent_With_A_Forged_Signature_Should_Fail()
    {
        var (service, _) = CreateService();
        var payload = $$"""
            {
              "fawryRefNumber": "9988776655",
              "merchantRefNumber": "our-ref",
              "paymentAmount": "49.99",
              "orderStatus": "PAID",
              "paymentMethod": "PAYATFAWRY",
              "signature": "forged"
            }
            """;

        var result = service.ParseWebhookEvent(payload, "forged");

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.WebhookSignatureInvalid", result.Error.Code);
    }

    [Fact]
    public void ParseWebhookEvent_With_A_Tampered_Payload_Should_Fail()
    {
        var (service, _) = CreateService();
        var validSignature = ComputeSignature("9988776655", "our-ref", "49.99", "PAID", "PAYATFAWRY", SecurityKey);

        // Same signature, different orderStatus — simulates a man-in-the-middle altering the outcome.
        var tamperedPayload = $$"""
            {
              "fawryRefNumber": "9988776655",
              "merchantRefNumber": "our-ref",
              "paymentAmount": "49.99",
              "orderStatus": "FAILED",
              "paymentMethod": "PAYATFAWRY",
              "signature": "{{validSignature}}"
            }
            """;

        var result = service.ParseWebhookEvent(tamperedPayload, validSignature);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.WebhookSignatureInvalid", result.Error.Code);
    }
}
