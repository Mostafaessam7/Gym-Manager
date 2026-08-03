using GymManager.Domain.Common;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Abstractions;

/// <summary>An external payment processor capable of collecting a card payment and reporting its outcome
/// asynchronously via webhook. Implemented by <c>StripePaymentGatewayService</c> in the infrastructure layer;
/// kept here as an abstraction so the application layer never depends on a specific gateway's SDK.</summary>
public interface IPaymentGatewayService
{
    /// <summary>The gateway's publishable/public key — safe to hand to the frontend as-is (it authorizes no
    /// server-side operations on its own), needed alongside a payment intent's client secret to initialize
    /// the gateway's client-side SDK (Stripe.js, etc.).</summary>
    string PublishableKey { get; }

    /// <summary>Starts a gateway-hosted payment. The returned <see cref="PaymentGatewayIntentResult.ClientSecret"/>
    /// is handed to the frontend, which uses the gateway's own client-side SDK to collect card details and
    /// confirm the payment — card data itself never passes through this backend.</summary>
    Task<Result<PaymentGatewayIntentResult>> CreatePaymentIntentAsync(
        Money amount, string? receiptEmail, IReadOnlyDictionary<string, string>? metadata, CancellationToken cancellationToken = default);

    /// <summary>Refunds a previously-collected gateway payment, in full if <paramref name="amount"/> is null.</summary>
    Task<Result<PaymentGatewayRefundResult>> RefundAsync(
        string gatewayReferenceId, Money? amount, CancellationToken cancellationToken = default);

    /// <summary>Verifies the webhook's signature and parses it into a provider-agnostic event. Returns a
    /// failure if the signature doesn't check out or the payload isn't a recognized event type — callers
    /// should treat that as "reject the webhook", not "the payment failed".</summary>
    Result<PaymentGatewayWebhookEvent> ParseWebhookEvent(string payload, string signatureHeader);
}

public sealed record PaymentGatewayIntentResult(string GatewayReferenceId, string ClientSecret, string Status);

public sealed record PaymentGatewayRefundResult(string GatewayRefundId, string Status);

public enum PaymentGatewayEventOutcome
{
    Succeeded,
    Failed,
    Other,
}

public sealed record PaymentGatewayWebhookEvent(string GatewayReferenceId, PaymentGatewayEventOutcome Outcome, string RawEventType);
