using System.Security.Cryptography;
using System.Text;
using GymManager.Domain.Common;
using GymManager.Infrastructure.PaymentGateways;
using Xunit;

namespace GymManager.UnitTests.PaymentGateways;

/// <summary>
/// Exercises <see cref="PaymobPaymentGatewayService"/>'s request-building, response-parsing, and HMAC
/// verification against a fake HTTP handler standing in for Paymob's "Accept" API — the same style of proof
/// <c>StripePaymentGatewayServiceTests</c> gives for Stripe. See that service's own class remarks: this proves
/// the code is internally consistent (it correctly verifies signatures it itself would produce, and correctly
/// builds/parses the requests/responses it targets), not that it has been confirmed against Paymob's real
/// servers — no merchant sandbox account was available to this session.
/// </summary>
public sealed class PaymobPaymentGatewayServiceTests
{
    private const string HmacSecret = "fake_paymob_hmac_secret_for_signature_verification";

    private static (PaymobPaymentGatewayService Service, FakePaymobHttpMessageHandler Handler) CreateService()
    {
        var handler = new FakePaymobHttpMessageHandler();
        var options = new PaymobOptions
        {
            ApiKey = "fake_api_key",
            IntegrationId = 1234,
            IframeId = 999,
            HmacSecret = HmacSecret,
            BaseUrl = "https://fake.paymob.test",
        };
        var service = new PaymobPaymentGatewayService(options, handler);
        return (service, handler);
    }

    /// <summary>Computes Paymob's documented HMAC exactly the way <c>PaymobPaymentGatewayService</c> itself
    /// does, from a fixed, hand-built field list, so a genuinely valid signature can be produced without
    /// depending on the production code's own computation (which would prove nothing).</summary>
    private static string ComputeHmac(string concatenatedFields) =>
        Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes(HmacSecret), Encoding.UTF8.GetBytes(concatenatedFields)));

    private const string OrderId = "555444";
    private const string TransactionId = "987654321";

    private static string BuildTransactionJson(bool success, bool pending) => $$"""
        {
          "type": "TRANSACTION",
          "obj": {
            "id": {{TransactionId}},
            "pending": {{pending.ToString().ToLowerInvariant()}},
            "amount_cents": 10000,
            "success": {{success.ToString().ToLowerInvariant()}},
            "is_auth": false,
            "is_capture": false,
            "is_standalone_payment": true,
            "is_voided": false,
            "is_refunded": false,
            "is_3d_secure": true,
            "integration_id": 1234,
            "has_parent_transaction": false,
            "order": { "id": {{OrderId}} },
            "created_at": "2026-08-26T10:00:00.000000",
            "currency": "EGP",
            "error_occured": false,
            "owner": 5678,
            "source_data": { "pan": "1234", "type": "card", "sub_type": "MasterCard" }
          }
        }
        """;

    private static string ValidHmacFor(bool success, bool pending)
    {
        var fields = string.Concat(
            "10000", "2026-08-26T10:00:00.000000", "EGP", "false", "false", TransactionId, "1234", "true",
            "false", "false", "false", "true", "false", OrderId, "5678", pending.ToString().ToLowerInvariant(),
            "1234", "MasterCard", "card", success.ToString().ToLowerInvariant());
        return ComputeHmac(fields);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_Should_Walk_The_Auth_Order_PaymentKey_Sequence()
    {
        var (service, handler) = CreateService();

        var result = await service.CreatePaymentIntentAsync(Money.Create(49.99m).Value, "member@example.com", null);

        Assert.True(result.IsSuccess);
        Assert.Equal("555444", result.Value.GatewayReferenceId);
        Assert.Contains("fake_payment_key_token", result.Value.ClientSecret);
        Assert.Contains("iframes/999", result.Value.ClientSecret);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Contains("/api/auth/tokens", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("/api/ecommerce/orders", handler.Requests[1].RequestUri!.AbsolutePath);
        Assert.Contains("/api/acceptance/payment_keys", handler.Requests[2].RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CreatePaymentIntentAsync_Should_Send_The_Amount_In_The_Smallest_Currency_Unit()
    {
        var (service, handler) = CreateService();

        await service.CreatePaymentIntentAsync(Money.Create(49.99m).Value, null, null);

        // $49.99 -> 4999 piastres, sent to both the order and payment-key requests.
        Assert.Contains("\"amount_cents\":4999", handler.RequestBodies[1]);
        Assert.Contains("\"amount_cents\":4999", handler.RequestBodies[2]);
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
    public async Task RefundAsync_Should_Parse_A_Real_Paymob_Shaped_Response()
    {
        var (service, handler) = CreateService();

        var result = await service.RefundAsync(TransactionId, amount: null);

        Assert.True(result.IsSuccess);
        Assert.Equal("succeeded", result.Value.Status);
        Assert.Contains("/api/acceptance/void_refund/refund", handler.Requests[^1].RequestUri!.AbsolutePath);
    }

    [Fact]
    public void ParseWebhookEvent_With_A_Genuinely_Valid_Hmac_Should_Succeed()
    {
        var (service, _) = CreateService();
        var payload = BuildTransactionJson(success: true, pending: false);
        var hmac = ValidHmacFor(success: true, pending: false);

        var result = service.ParseWebhookEvent(payload, hmac);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderId, result.Value.GatewayReferenceId);
        Assert.Equal(TransactionId, result.Value.SecondaryReferenceId);
        Assert.Equal(GymManager.Application.Abstractions.PaymentGatewayEventOutcome.Succeeded, result.Value.Outcome);
    }

    [Fact]
    public void ParseWebhookEvent_With_Success_False_Should_Map_To_Failed_Outcome()
    {
        var (service, _) = CreateService();
        var payload = BuildTransactionJson(success: false, pending: false);
        var hmac = ValidHmacFor(success: false, pending: false);

        var result = service.ParseWebhookEvent(payload, hmac);

        Assert.True(result.IsSuccess);
        Assert.Equal(GymManager.Application.Abstractions.PaymentGatewayEventOutcome.Failed, result.Value.Outcome);
    }

    [Fact]
    public void ParseWebhookEvent_While_Still_Pending_Should_Map_To_Other_Outcome()
    {
        var (service, _) = CreateService();
        var payload = BuildTransactionJson(success: false, pending: true);
        var hmac = ValidHmacFor(success: false, pending: true);

        var result = service.ParseWebhookEvent(payload, hmac);

        Assert.True(result.IsSuccess);
        Assert.Equal(GymManager.Application.Abstractions.PaymentGatewayEventOutcome.Other, result.Value.Outcome);
    }

    [Fact]
    public void ParseWebhookEvent_With_A_Forged_Hmac_Should_Fail()
    {
        var (service, _) = CreateService();
        var payload = BuildTransactionJson(success: true, pending: false);

        var result = service.ParseWebhookEvent(payload, new string('0', 128));

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.WebhookSignatureInvalid", result.Error.Code);
    }

    [Fact]
    public void ParseWebhookEvent_With_A_Tampered_Payload_Should_Fail()
    {
        var (service, _) = CreateService();
        var validHmac = ValidHmacFor(success: true, pending: false);

        // Same hmac, different payload — simulates a man-in-the-middle altering the transaction outcome.
        var tamperedPayload = BuildTransactionJson(success: false, pending: false);

        var result = service.ParseWebhookEvent(tamperedPayload, validHmac);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.WebhookSignatureInvalid", result.Error.Code);
    }
}
