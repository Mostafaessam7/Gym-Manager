using System.Text.Json;
using GymManager.Application.Abstractions;
using GymManager.Domain.Common;
using GymManager.SharedKernel.Results;

namespace GymManager.IntegrationTests.Fakes;

/// <summary>
/// A controllable <see cref="IPaymentGatewayService"/> double for exercising the CQRS command orchestration
/// (create-intent → persist → webhook → complete/fail, refund) without depending on real Stripe network
/// calls or cryptographic webhook-signature verification — that plumbing is covered separately, against the
/// real <c>StripePaymentGatewayService</c> and a local fake HTTP server, in
/// <c>StripePaymentGatewayServiceTests</c>. Here, "signature verification" is simulated: a signature header
/// of <c>"invalid"</c> simulates a bad signature, and otherwise the payload is a small JSON test format
/// (<c>{"gatewayReferenceId": "...", "outcome": "Succeeded"}</c>) rather than a real Stripe event.
/// </summary>
public sealed class FakePaymentGatewayService : IPaymentGatewayService
{
    public string PublishableKey => "pk_test_fake";

    public bool FailNextCreate { get; set; }

    public bool FailNextRefund { get; set; }

    public List<(string GatewayReferenceId, Money? Amount)> RefundCalls { get; } = [];

    public Task<Result<PaymentGatewayIntentResult>> CreatePaymentIntentAsync(
        Money amount, string? receiptEmail, IReadOnlyDictionary<string, string>? metadata, CancellationToken cancellationToken = default)
    {
        if (FailNextCreate)
        {
            return Task.FromResult(Result.Failure<PaymentGatewayIntentResult>(
                Error.Failure("Payment.GatewayRequestFailed", "Simulated gateway failure.")));
        }

        var referenceId = $"pi_fake_{Guid.NewGuid():N}";
        return Task.FromResult(Result.Success(new PaymentGatewayIntentResult(referenceId, $"{referenceId}_secret_fake", "requires_payment_method")));
    }

    public Task<Result<PaymentGatewayRefundResult>> RefundAsync(
        string gatewayReferenceId, Money? amount, CancellationToken cancellationToken = default)
    {
        RefundCalls.Add((gatewayReferenceId, amount));

        if (FailNextRefund)
        {
            return Task.FromResult(Result.Failure<PaymentGatewayRefundResult>(
                Error.Failure("Payment.GatewayRequestFailed", "Simulated gateway failure.")));
        }

        return Task.FromResult(Result.Success(new PaymentGatewayRefundResult($"re_fake_{Guid.NewGuid():N}", "succeeded")));
    }

    public Result<PaymentGatewayWebhookEvent> ParseWebhookEvent(string payload, string signatureHeader)
    {
        if (signatureHeader == "invalid")
            return Result.Failure<PaymentGatewayWebhookEvent>(Error.Validation("Payment.WebhookSignatureInvalid", "Simulated bad signature."));

        var testEvent = JsonSerializer.Deserialize<TestWebhookPayload>(payload, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Malformed test webhook payload.");

        return Result.Success(new PaymentGatewayWebhookEvent(
            testEvent.GatewayReferenceId, Enum.Parse<PaymentGatewayEventOutcome>(testEvent.Outcome), "payment_intent.test"));
    }

    private sealed record TestWebhookPayload(string GatewayReferenceId, string Outcome);
}
