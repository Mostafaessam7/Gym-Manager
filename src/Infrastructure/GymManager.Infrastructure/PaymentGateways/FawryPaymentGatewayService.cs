using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GymManager.Application.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Payments;
using GymManager.Domain.Payments.Errors;
using GymManager.SharedKernel.Results;

namespace GymManager.Infrastructure.PaymentGateways;

/// <summary>
/// <inheritdoc cref="IPaymentGatewayService"/>
/// </summary>
/// <remarks>
/// <para><b>Implemented from FawryPay's publicly documented "Charge Request" API, not verified against a live
/// Fawry merchant sandbox</b> — same caveat as <c>PaymobPaymentGatewayService</c>: Fawry requires real
/// merchant onboarding to obtain credentials, so this was built and self-tested (see
/// <c>FawryPaymentGatewayServiceTests</c>) without a real account to confirm end-to-end. Diff the request/
/// response shapes and the signature field order below against your own merchant pack before relying on this
/// in production.</para>
///
/// <para>Deliberately implements Fawry's <c>PAYATFAWRY</c> payment method — a reference number the member
/// pays in cash at any Fawry retail outlet, ATM, or mobile wallet, confirmed later by an asynchronous
/// notification — rather than Fawry's card-processing flow, which would just duplicate what Stripe/Paymob
/// already cover. This is Fawry's actual differentiator for a gym membership use case (a member without a
/// card can still pay). <see cref="PaymentGatewayIntentResult.ClientSecret"/> here is the reference number
/// itself, meant to be displayed to the member ("pay this number at any Fawry outlet"), not a URL to redirect
/// to.</para>
/// </remarks>
public sealed class FawryPaymentGatewayService : IPaymentGatewayService, IDisposable
{
    private readonly FawryOptions _options;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public PaymentGatewayProvider Provider => PaymentGatewayProvider.Fawry;

    /// <summary>The PAYATFAWRY reference-number flow has no client-side SDK to key.</summary>
    public string PublishableKey => string.Empty;

    public FawryPaymentGatewayService(FawryOptions options)
        : this(options, httpMessageHandler: null)
    {
    }

    /// <summary>Accepts an explicit <see cref="HttpMessageHandler"/> so tests can point this service's HTTP
    /// calls at an in-memory fake instead of a real network call — see the identical seam on
    /// <c>PaymobPaymentGatewayService</c>.</summary>
    public FawryPaymentGatewayService(FawryOptions options, HttpMessageHandler? httpMessageHandler)
    {
        _options = options;
        _ownsHttpClient = httpMessageHandler is null;
        _httpClient = httpMessageHandler is null
            ? new HttpClient { BaseAddress = new Uri(options.BaseUrl) }
            : new HttpClient(httpMessageHandler, disposeHandler: false) { BaseAddress = new Uri(options.BaseUrl) };
    }

    public async Task<Result<PaymentGatewayIntentResult>> CreatePaymentIntentAsync(
        Money amount, string? receiptEmail, IReadOnlyDictionary<string, string>? metadata, CancellationToken cancellationToken = default)
    {
        try
        {
            // Our own Payment.Id, if the caller supplied it via metadata (every current caller does — see
            // CreateGatewayPaymentIntentCommandHandler) — used as Fawry's merchantRefNumber so their
            // notification callback can be tied back to our record even before we know Fawry's own
            // referenceNumber. Falls back to a fresh id if a caller ever omits it.
            var merchantRefNumber = metadata is not null && metadata.TryGetValue("gymManagerPaymentId", out var id)
                ? id
                : Guid.NewGuid().ToString();

            var formattedAmount = amount.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

            var signature = ComputeSignature(_options.MerchantCode, merchantRefNumber, formattedAmount, "PAYATFAWRY", _options.SecurityKey);

            var request = new
            {
                merchantCode = _options.MerchantCode,
                merchantRefNum = merchantRefNumber,
                customerEmail = receiptEmail,
                paymentMethod = "PAYATFAWRY",
                amount = formattedAmount,
                currencyCode = amount.Currency.ToUpperInvariant(),
                description = "Gym Manager payment",
                signature,
            };

            using var response = await _httpClient.PostAsJsonAsync("/ECommerceWeb/Fawry/payments/charge", request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Result.Failure<PaymentGatewayIntentResult>(PaymentErrors.GatewayRequestFailed($"Fawry returned {(int)response.StatusCode}: {body}"));

            var json = JsonDocument.Parse(body).RootElement;

            var statusCode = json.TryGetProperty("statusCode", out var statusProp) ? statusProp.GetInt32() : 200;
            if (statusCode != 200)
            {
                var description = json.TryGetProperty("statusDescription", out var descProp) ? descProp.GetString() : "Unknown Fawry error";
                return Result.Failure<PaymentGatewayIntentResult>(PaymentErrors.GatewayRequestFailed(description ?? "Unknown Fawry error"));
            }

            var referenceNumber = json.GetProperty("referenceNumber").GetString()
                ?? throw new JsonException("Charge response had no referenceNumber.");
            var orderStatus = json.TryGetProperty("orderStatus", out var statusValue) ? statusValue.GetString() ?? "NEW" : "NEW";

            return Result.Success(new PaymentGatewayIntentResult(referenceNumber, referenceNumber, orderStatus));
        }
        catch (JsonException ex)
        {
            return Result.Failure<PaymentGatewayIntentResult>(PaymentErrors.GatewayRequestFailed(ex.Message));
        }
    }

    /// <remarks>Fawry's merchant portal is the documented way to process a refund for PAYATFAWRY collections
    /// (cash already physically collected at a retail outlet isn't reversible via a simple API call the way a
    /// card charge is) — this calls Fawry's server-to-server refund endpoint on the assumption a merchant
    /// account has it enabled; if a given account doesn't, Fawry's own API returns a clear rejection, which
    /// surfaces here as <c>Payment.GatewayRequestFailed</c> rather than a silent no-op.</remarks>
    public async Task<Result<PaymentGatewayRefundResult>> RefundAsync(
        string gatewayReferenceId, Money? amount, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                merchantCode = _options.MerchantCode,
                referenceNumber = gatewayReferenceId,
                refundAmount = amount?.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                reason = "Refund requested",
                signature = ComputeSignature(_options.MerchantCode, gatewayReferenceId, _options.SecurityKey),
            };

            using var response = await _httpClient.PostAsJsonAsync("/ECommerceWeb/Fawry/payments/refund", request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Result.Failure<PaymentGatewayRefundResult>(PaymentErrors.GatewayRequestFailed($"Fawry returned {(int)response.StatusCode}: {body}"));

            var json = JsonDocument.Parse(body).RootElement;
            var statusCode = json.TryGetProperty("statusCode", out var statusProp) ? statusProp.GetInt32() : 200;

            return statusCode == 200
                ? Result.Success(new PaymentGatewayRefundResult(gatewayReferenceId, "refunded"))
                : Result.Failure<PaymentGatewayRefundResult>(PaymentErrors.GatewayRequestFailed(
                    json.TryGetProperty("statusDescription", out var d) ? d.GetString() ?? "Refund rejected" : "Refund rejected"));
        }
        catch (JsonException ex)
        {
            return Result.Failure<PaymentGatewayRefundResult>(PaymentErrors.GatewayRequestFailed(ex.Message));
        }
    }

    /// <remarks>
    /// Recomputes FawryPay's documented SHA-256 signature over the notification's
    /// <c>fawryRefNumber + merchantRefNumber + paymentAmount + orderStatus + paymentMethod + SecurityKey</c>
    /// (concatenated as plain strings, <c>paymentAmount</c> formatted to two decimals) and compares it against
    /// the <c>signature</c> field carried inside the notification's own JSON body — Fawry does not sign via a
    /// header or query parameter, so the calling controller must extract this field from the parsed payload
    /// before calling this method (see <c>FawryWebhookController</c>).
    /// </remarks>
    public Result<PaymentGatewayWebhookEvent> ParseWebhookEvent(string payload, string signatureHeader)
    {
        JsonElement root;
        try
        {
            root = JsonDocument.Parse(payload).RootElement;
        }
        catch (JsonException ex)
        {
            return Result.Failure<PaymentGatewayWebhookEvent>(PaymentErrors.WebhookSignatureInvalid($"Malformed payload: {ex.Message}"));
        }

        string Field(string name) => root.TryGetProperty(name, out var v) ? (v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.GetRawText()) : "";

        var fawryRefNumber = Field("fawryRefNumber");
        var merchantRefNumber = Field("merchantRefNumber");
        var paymentAmount = Field("paymentAmount");
        var orderStatus = Field("orderStatus");
        var paymentMethod = Field("paymentMethod");

        var computedSignature = ComputeSignature(fawryRefNumber, merchantRefNumber, paymentAmount, orderStatus, paymentMethod, _options.SecurityKey);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedSignature.ToUpperInvariant()),
                Encoding.UTF8.GetBytes(signatureHeader.ToUpperInvariant())))
        {
            return Result.Failure<PaymentGatewayWebhookEvent>(PaymentErrors.WebhookSignatureInvalid("signature mismatch"));
        }

        var outcome = orderStatus.ToUpperInvariant() switch
        {
            "PAID" => PaymentGatewayEventOutcome.Succeeded,
            "FAILED" or "EXPIRED" or "CANCELED" or "CANCELLED" => PaymentGatewayEventOutcome.Failed,
            _ => PaymentGatewayEventOutcome.Other,
        };

        return Result.Success(new PaymentGatewayWebhookEvent(fawryRefNumber, outcome, orderStatus));
    }

    private static string ComputeSignature(params string[] fields)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(string.Concat(fields)));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}
