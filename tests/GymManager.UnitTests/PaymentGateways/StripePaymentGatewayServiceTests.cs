using System.Security.Cryptography;
using System.Text;
using GymManager.Domain.Common;
using GymManager.Infrastructure.PaymentGateways;
using Stripe;
using Xunit;

namespace GymManager.UnitTests.PaymentGateways;

/// <summary>
/// Exercises the real Stripe.net SDK wiring inside <see cref="StripePaymentGatewayService"/> — request
/// building, response parsing, and webhook signature verification — against a fake HTTP handler standing in
/// for Stripe's API, using the exact same test-format secret/publishable/webhook keys a real Stripe test-mode
/// account would provide. No network call and no real Stripe account are involved; what's proven here is
/// that the SDK integration itself is wired correctly, which the CQRS-level tests in
/// <c>PaymentGatewayTests</c> (against a fully-faked <c>IPaymentGatewayService</c>) don't cover.
/// </summary>
public sealed class StripePaymentGatewayServiceTests
{
    private const string WebhookSecret = "whsec_test_fake_secret_for_signature_verification";

    private static (StripePaymentGatewayService Service, FakeStripeHttpMessageHandler Handler) CreateService()
    {
        var handler = new FakeStripeHttpMessageHandler();
        var httpClient = new SystemNetHttpClient(new HttpClient(handler));
        var options = new StripeOptions
        {
            SecretKey = "sk_test_fake_secret_key",
            PublishableKey = "pk_test_fake_publishable_key",
            WebhookSecret = WebhookSecret,
        };
        var service = new StripePaymentGatewayService(options, httpClient);
        return (service, handler);
    }

    /// <summary>Signs a payload exactly the way Stripe signs real webhook deliveries (documented at
    /// stripe.com/docs/webhooks/signatures), so <see cref="EventUtility.ConstructEvent(string,string,string,long,bool)"/>
    /// — the real verification code <c>StripePaymentGatewayService</c> calls — genuinely validates it.</summary>
    private static string SignPayload(string payload, long timestamp)
    {
        var signedPayload = $"{timestamp}.{payload}";
        var signatureBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(WebhookSecret), Encoding.UTF8.GetBytes(signedPayload));
        var signatureHex = Convert.ToHexString(signatureBytes).ToLowerInvariant();
        return $"t={timestamp},v1={signatureHex}";
    }

    private static string BuildPaymentIntentEventJson(string eventType, string paymentIntentId, string status) => $$"""
        {
          "id": "evt_fake_1",
          "object": "event",
          "type": "{{eventType}}",
          "created": 1700000000,
          "data": {
            "object": {
              "id": "{{paymentIntentId}}",
              "object": "payment_intent",
              "status": "{{status}}",
              "amount": 4999,
              "currency": "usd"
            }
          }
        }
        """;

    [Fact]
    public async Task CreatePaymentIntentAsync_Should_Parse_A_Real_Stripe_Shaped_Response()
    {
        var (service, handler) = CreateService();

        var result = await service.CreatePaymentIntentAsync(
            Money.Create(49.99m).Value, "member@example.com", new Dictionary<string, string> { ["orderId"] = "123" });

        Assert.True(result.IsSuccess);
        Assert.Equal("pi_fake_1234567890", result.Value.GatewayReferenceId);
        Assert.Equal("pi_fake_1234567890_secret_fakeSecret", result.Value.ClientSecret);
        Assert.Equal("requires_payment_method", result.Value.Status);
        Assert.Contains("/v1/payment_intents", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_Should_Send_The_Amount_In_The_Smallest_Currency_Unit()
    {
        var (service, handler) = CreateService();

        await service.CreatePaymentIntentAsync(Money.Create(49.99m).Value, null, null);

        // $49.99 -> 4999 cents.
        Assert.Contains("amount=4999", handler.LastRequestBody);
        Assert.Contains("currency=usd", handler.LastRequestBody);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_Should_Authenticate_With_The_Configured_Secret_Key()
    {
        var (service, handler) = CreateService();

        await service.CreatePaymentIntentAsync(Money.Create(10m).Value, null, null);

        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("sk_test_fake_secret_key", handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_When_The_Gateway_Declines_Should_Return_A_Failure()
    {
        var (service, handler) = CreateService();
        handler.FailNextRequest = true;

        var result = await service.CreatePaymentIntentAsync(Money.Create(10m).Value, null, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.GatewayRequestFailed", result.Error.Code);
    }

    [Fact]
    public async Task RefundAsync_Should_Parse_A_Real_Stripe_Shaped_Response()
    {
        var (service, handler) = CreateService();

        var result = await service.RefundAsync("pi_fake_1234567890", amount: null);

        Assert.True(result.IsSuccess);
        Assert.Equal("re_fake_0987654321", result.Value.GatewayRefundId);
        Assert.Equal("succeeded", result.Value.Status);
        Assert.Contains("/v1/refunds", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task RefundAsync_When_The_Gateway_Rejects_Should_Return_A_Failure()
    {
        var (service, handler) = CreateService();
        handler.FailNextRequest = true;

        var result = await service.RefundAsync("pi_fake_1234567890", amount: null);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.GatewayRequestFailed", result.Error.Code);
    }

    [Fact]
    public void ParseWebhookEvent_With_A_Genuinely_Valid_Signature_Should_Succeed()
    {
        var (service, _) = CreateService();
        var payload = BuildPaymentIntentEventJson("payment_intent.succeeded", "pi_fake_1234567890", "succeeded");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signatureHeader = SignPayload(payload, timestamp);

        var result = service.ParseWebhookEvent(payload, signatureHeader);

        Assert.True(result.IsSuccess);
        Assert.Equal("pi_fake_1234567890", result.Value.GatewayReferenceId);
        Assert.Equal(GymManager.Application.Abstractions.PaymentGatewayEventOutcome.Succeeded, result.Value.Outcome);
    }

    [Fact]
    public void ParseWebhookEvent_With_A_PaymentFailed_Type_Should_Map_To_Failed_Outcome()
    {
        var (service, _) = CreateService();
        var payload = BuildPaymentIntentEventJson("payment_intent.payment_failed", "pi_fake_1234567890", "requires_payment_method");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signatureHeader = SignPayload(payload, timestamp);

        var result = service.ParseWebhookEvent(payload, signatureHeader);

        Assert.True(result.IsSuccess);
        Assert.Equal(GymManager.Application.Abstractions.PaymentGatewayEventOutcome.Failed, result.Value.Outcome);
    }

    [Fact]
    public void ParseWebhookEvent_With_An_Unrelated_Event_Type_Should_Map_To_Other_Outcome()
    {
        var (service, _) = CreateService();
        var payload = BuildPaymentIntentEventJson("payment_intent.created", "pi_fake_1234567890", "requires_payment_method");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signatureHeader = SignPayload(payload, timestamp);

        var result = service.ParseWebhookEvent(payload, signatureHeader);

        Assert.True(result.IsSuccess);
        Assert.Equal(GymManager.Application.Abstractions.PaymentGatewayEventOutcome.Other, result.Value.Outcome);
    }

    [Fact]
    public void ParseWebhookEvent_With_A_Forged_Signature_Should_Fail()
    {
        var (service, _) = CreateService();
        var payload = BuildPaymentIntentEventJson("payment_intent.succeeded", "pi_fake_1234567890", "succeeded");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var forgedHeader = $"t={timestamp},v1={new string('0', 64)}";

        var result = service.ParseWebhookEvent(payload, forgedHeader);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.WebhookSignatureInvalid", result.Error.Code);
    }

    [Fact]
    public void ParseWebhookEvent_With_A_Tampered_Payload_Should_Fail()
    {
        var (service, _) = CreateService();
        var originalPayload = BuildPaymentIntentEventJson("payment_intent.succeeded", "pi_fake_1234567890", "succeeded");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signatureHeader = SignPayload(originalPayload, timestamp);

        // Same signature, different payload — simulates a man-in-the-middle altering the request body.
        var tamperedPayload = BuildPaymentIntentEventJson("payment_intent.succeeded", "pi_attacker_controlled", "succeeded");

        var result = service.ParseWebhookEvent(tamperedPayload, signatureHeader);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.WebhookSignatureInvalid", result.Error.Code);
    }
}
