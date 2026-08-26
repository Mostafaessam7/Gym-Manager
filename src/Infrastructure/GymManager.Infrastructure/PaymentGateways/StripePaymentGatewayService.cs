using GymManager.Application.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Payments;
using GymManager.Domain.Payments.Errors;
using GymManager.SharedKernel.Results;
using Stripe;

namespace GymManager.Infrastructure.PaymentGateways;

/// <inheritdoc cref="IPaymentGatewayService"/>
public sealed class StripePaymentGatewayService : IPaymentGatewayService
{
    private readonly StripeOptions _options;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly RefundService _refundService;

    public PaymentGatewayProvider Provider => PaymentGatewayProvider.Stripe;

    public string PublishableKey => _options.PublishableKey;

    public StripePaymentGatewayService(StripeOptions options)
        : this(options, httpClient: null)
    {
    }

    /// <summary>Accepts an explicit <see cref="IHttpClient"/> so tests can point Stripe.net's request
    /// pipeline at an in-memory fake handler instead of a real network call, while exercising the exact same
    /// request-building/response-parsing code every real call goes through. DI (the single-argument
    /// constructor above) always passes <c>null</c>, which lets Stripe.net create its normal HTTP client.</summary>
    public StripePaymentGatewayService(StripeOptions options, IHttpClient? httpClient)
    {
        _options = options;

        var client = new StripeClient(options.SecretKey, httpClient: httpClient);
        _paymentIntentService = new PaymentIntentService(client);
        _refundService = new RefundService(client);
    }

    public async Task<Result<PaymentGatewayIntentResult>> CreatePaymentIntentAsync(
        Money amount, string? receiptEmail, IReadOnlyDictionary<string, string>? metadata, CancellationToken cancellationToken = default)
    {
        try
        {
            var createOptions = new PaymentIntentCreateOptions
            {
                Amount = ToSmallestCurrencyUnit(amount),
                Currency = amount.Currency.ToLowerInvariant(),
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                ReceiptEmail = receiptEmail,
                Metadata = metadata is null ? null : new Dictionary<string, string>(metadata),
            };

            var intent = await _paymentIntentService.CreateAsync(createOptions, cancellationToken: cancellationToken);

            return Result.Success(new PaymentGatewayIntentResult(intent.Id, intent.ClientSecret, intent.Status));
        }
        catch (StripeException ex)
        {
            return Result.Failure<PaymentGatewayIntentResult>(PaymentErrors.GatewayRequestFailed(ex.Message));
        }
    }

    public async Task<Result<PaymentGatewayRefundResult>> RefundAsync(
        string gatewayReferenceId, Money? amount, CancellationToken cancellationToken = default)
    {
        try
        {
            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = gatewayReferenceId,
                Amount = amount is null ? null : ToSmallestCurrencyUnit(amount),
            };

            var refund = await _refundService.CreateAsync(refundOptions, cancellationToken: cancellationToken);

            return Result.Success(new PaymentGatewayRefundResult(refund.Id, refund.Status));
        }
        catch (StripeException ex)
        {
            return Result.Failure<PaymentGatewayRefundResult>(PaymentErrors.GatewayRequestFailed(ex.Message));
        }
    }

    public Result<PaymentGatewayWebhookEvent> ParseWebhookEvent(string payload, string signatureHeader)
    {
        Event stripeEvent;
        try
        {
            // throwOnApiVersionMismatch: false — Stripe.net's default API version will drift from whatever
            // API version the account's webhook was configured with over time; that mismatch says nothing
            // about whether the event itself is valid, so it shouldn't fail signature-verified parsing.
            stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _options.WebhookSecret, 300, throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            return Result.Failure<PaymentGatewayWebhookEvent>(PaymentErrors.WebhookSignatureInvalid(ex.Message));
        }

        if (stripeEvent.Data.Object is not PaymentIntent paymentIntent)
            return Result.Failure<PaymentGatewayWebhookEvent>(PaymentErrors.WebhookEventUnrecognized);

        var outcome = stripeEvent.Type switch
        {
            "payment_intent.succeeded" => PaymentGatewayEventOutcome.Succeeded,
            "payment_intent.payment_failed" => PaymentGatewayEventOutcome.Failed,
            _ => PaymentGatewayEventOutcome.Other,
        };

        return Result.Success(new PaymentGatewayWebhookEvent(paymentIntent.Id, outcome, stripeEvent.Type));
    }

    /// <summary>Stripe amounts are integers in the currency's smallest unit (cents for USD/EUR, etc.).
    /// Zero-decimal currencies (JPY, KRW, ...) aren't handled here — this integration assumes a 2-decimal
    /// currency, matching every currency actually used elsewhere in this codebase (USD by default).</summary>
    private static long ToSmallestCurrencyUnit(Money amount) => (long)Math.Round(amount.Amount * 100m, MidpointRounding.AwayFromZero);
}
