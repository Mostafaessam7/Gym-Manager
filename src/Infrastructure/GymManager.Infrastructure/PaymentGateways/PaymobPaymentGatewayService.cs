using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GymManager.Application.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Payments;
using GymManager.Domain.Payments.Errors;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Infrastructure.PaymentGateways;

/// <summary>
/// <inheritdoc cref="IPaymentGatewayService"/>
/// </summary>
/// <remarks>
/// <para><b>Implemented from Paymob's publicly documented "Accept" API, not verified against a live Paymob
/// merchant sandbox</b> — unlike Stripe, Paymob requires real merchant registration/KYC to obtain even test
/// credentials, so no account was available to exercise this against Paymob's actual servers. The request
/// shapes, response fields, and the HMAC field order below reflect Paymob's documentation as of when this was
/// written; diff them against the exact copy on your own merchant dashboard (Developers → HMAC Calculation)
/// before relying on this in production. <c>PaymobPaymentGatewayServiceTests</c> in the test project
/// proves this code's request-building/response-parsing/signature-verification is internally consistent
/// (it verifies exactly what it itself would produce), which is the same level of proof Stripe's integration
/// had before a real account confirmed it end-to-end — that final live confirmation is what's still missing
/// here.</para>
///
/// <para>Paymob's flow is three sequential calls, not one: (1) exchange the account's API key for a
/// short-lived auth token (<c>POST /api/auth/tokens</c>); (2) register an "order" for the amount
/// (<c>POST /api/ecommerce/orders</c>); (3) request a "payment key" for that order
/// (<c>POST /api/acceptance/payment_keys</c>). The payment key is embedded in the iframe URL handed back as
/// this service's <see cref="PaymentGatewayIntentResult.ClientSecret"/> — the frontend redirects the member
/// there to enter card details; Paymob never gives this backend the card data directly. The order id (known
/// immediately) is stored as <see cref="PaymentGatewayIntentResult.GatewayReferenceId"/> so the later webhook
/// — which reports both an order id and a transaction id — can be matched back to the pending
/// <c>Payment</c>; <see cref="RefundAsync"/> needs the *transaction* id instead, which only exists once the
/// webhook confirms the charge, so the caller must swap the stored reference to the transaction id after a
/// successful webhook (see <c>HandlePaymobWebhookCommandHandler</c>).</para>
/// </remarks>
public sealed class PaymobPaymentGatewayService : IPaymentGatewayService, IDisposable
{
    private readonly PaymobOptions _options;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public PaymentGatewayProvider Provider => PaymentGatewayProvider.Paymob;

    /// <summary>Paymob's flow redirects to a server-rendered iframe rather than using a client-side JS SDK
    /// keyed by a publishable key, so there's nothing to hand the frontend here.</summary>
    public string PublishableKey => string.Empty;

    public PaymobPaymentGatewayService(PaymobOptions options)
        : this(options, httpMessageHandler: null)
    {
    }

    /// <summary>Accepts an explicit <see cref="HttpMessageHandler"/> so tests can point this service's HTTP
    /// calls at an in-memory fake instead of a real network call, mirroring the pattern
    /// <c>StripePaymentGatewayService</c> uses via Stripe.net's <c>IHttpClient</c> seam. DI (the
    /// single-argument constructor above) always passes <see langword="null"/>, which creates a normal
    /// <see cref="HttpClient"/>.</summary>
    public PaymobPaymentGatewayService(PaymobOptions options, HttpMessageHandler? httpMessageHandler)
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
            var amountCents = ToSmallestCurrencyUnit(amount);
            var currency = amount.Currency.ToUpperInvariant();

            var authToken = await RequestAuthTokenAsync(cancellationToken);

            var orderId = await RegisterOrderAsync(authToken, amountCents, currency, cancellationToken);

            var paymentKey = await RequestPaymentKeyAsync(authToken, orderId, amountCents, currency, receiptEmail, cancellationToken);

            var iframeUrl = $"{_options.BaseUrl}/api/acceptance/iframes/{_options.IframeId}?payment_token={paymentKey}";

            return Result.Success(new PaymentGatewayIntentResult(orderId.ToString(CultureInfo.InvariantCulture), iframeUrl, "pending"));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or PaymobApiException)
        {
            return Result.Failure<PaymentGatewayIntentResult>(PaymentErrors.GatewayRequestFailed(ex.Message));
        }
    }

    public async Task<Result<PaymentGatewayRefundResult>> RefundAsync(
        string gatewayReferenceId, Money? amount, CancellationToken cancellationToken = default)
    {
        try
        {
            var authToken = await RequestAuthTokenAsync(cancellationToken);

            var body = new Dictionary<string, object?>
            {
                ["auth_token"] = authToken,
                ["transaction_id"] = gatewayReferenceId,
            };
            if (amount is not null)
                body["amount_cents"] = ToSmallestCurrencyUnit(amount);

            using var response = await _httpClient.PostAsJsonAsync("/api/acceptance/void_refund/refund", body, cancellationToken);
            var json = await ParseJsonOrThrowAsync(response, cancellationToken);

            var transactionId = json.GetProperty("id").GetRawText();
            var success = json.TryGetProperty("success", out var successProp) && successProp.GetBoolean();

            return Result.Success(new PaymentGatewayRefundResult(transactionId, success ? "succeeded" : "failed"));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or PaymobApiException)
        {
            return Result.Failure<PaymentGatewayRefundResult>(PaymentErrors.GatewayRequestFailed(ex.Message));
        }
    }

    /// <remarks>
    /// Recomputes Paymob's documented HMAC-SHA512 over the transaction ("obj") fields, concatenated as plain
    /// (non-JSON) string values in this exact order — <c>amount_cents, created_at, currency, error_occured,
    /// has_parent_transaction, id, integration_id, is_3d_secure, is_auth, is_capture, is_refunded,
    /// is_standalone_payment, is_voided, order.id, owner, pending, source_data.pan, source_data.sub_type,
    /// source_data.type, success</c> — and compares it against the <c>hmac</c> value the caller extracted from
    /// the webhook request's query string (Paymob does not sign via a header). Booleans are lowercased
    /// (<c>"true"</c>/<c>"false"</c>) to match Paymob's own field-order documentation.
    /// </remarks>
    public Result<PaymentGatewayWebhookEvent> ParseWebhookEvent(string payload, string signatureHeader)
    {
        JsonElement obj;
        try
        {
            var root = JsonDocument.Parse(payload).RootElement;
            obj = root.TryGetProperty("obj", out var o) ? o : root;
        }
        catch (JsonException ex)
        {
            return Result.Failure<PaymentGatewayWebhookEvent>(PaymentErrors.WebhookSignatureInvalid($"Malformed payload: {ex.Message}"));
        }

        string Field(string name) => obj.TryGetProperty(name, out var v) ? RawFieldValue(v) : string.Empty;

        var orderId = obj.TryGetProperty("order", out var order) && order.TryGetProperty("id", out var orderIdProp)
            ? RawFieldValue(orderIdProp)
            : string.Empty;

        var sourceData = obj.TryGetProperty("source_data", out var sd) ? sd : default;
        string SourceField(string name) => sourceData.ValueKind == JsonValueKind.Object && sourceData.TryGetProperty(name, out var v)
            ? RawFieldValue(v)
            : string.Empty;

        var concatenated = string.Concat(
            Field("amount_cents"), Field("created_at"), Field("currency"), Field("error_occured"),
            Field("has_parent_transaction"), Field("id"), Field("integration_id"), Field("is_3d_secure"),
            Field("is_auth"), Field("is_capture"), Field("is_refunded"), Field("is_standalone_payment"),
            Field("is_voided"), orderId, Field("owner"), Field("pending"),
            SourceField("pan"), SourceField("sub_type"), SourceField("type"), Field("success"));

        var computedHmac = Convert.ToHexString(
            HMACSHA512.HashData(Encoding.UTF8.GetBytes(_options.HmacSecret), Encoding.UTF8.GetBytes(concatenated)));

        // Reuses the same constant-time comparison already shared by every other secret comparison in this
        // codebase (User's token-hash checks, TotpTwoFactorService) rather than a third independently-written
        // one — see ConstantTimeComparer's own remarks.
        if (!ConstantTimeComparer.Equals(computedHmac.ToUpperInvariant(), signatureHeader.ToUpperInvariant()))
            return Result.Failure<PaymentGatewayWebhookEvent>(PaymentErrors.WebhookSignatureInvalid("hmac mismatch"));

        var success = obj.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
        var pending = obj.TryGetProperty("pending", out var pendingProp) && pendingProp.GetBoolean();

        var outcome = (success, pending) switch
        {
            (true, false) => PaymentGatewayEventOutcome.Succeeded,
            (false, false) => PaymentGatewayEventOutcome.Failed,
            _ => PaymentGatewayEventOutcome.Other,
        };

        // Null (not empty string) when the payload is missing "id" — HandlePaymobWebhookCommandHandler checks
        // SecondaryReferenceId for null before overwriting the stored GatewayReferenceId, so an empty string
        // here would defeat that guard and could wipe the reference to a real, refundable transaction id.
        var transactionId = Field("id") is { Length: > 0 } id ? id : null;

        return Result.Success(new PaymentGatewayWebhookEvent(orderId, outcome, "TRANSACTION", transactionId));
    }

    private async Task<string> RequestAuthTokenAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("/api/auth/tokens", new { api_key = _options.ApiKey }, cancellationToken);
        var json = await ParseJsonOrThrowAsync(response, cancellationToken);
        return json.GetProperty("token").GetString() ?? throw new PaymobApiException("Auth response had no token.");
    }

    private async Task<long> RegisterOrderAsync(string authToken, long amountCents, string currency, CancellationToken cancellationToken)
    {
        var request = new
        {
            auth_token = authToken,
            delivery_needed = false,
            amount_cents = amountCents,
            currency,
            items = Array.Empty<object>(),
        };

        using var response = await _httpClient.PostAsJsonAsync("/api/ecommerce/orders", request, cancellationToken);
        var json = await ParseJsonOrThrowAsync(response, cancellationToken);
        return json.GetProperty("id").GetInt64();
    }

    private async Task<string> RequestPaymentKeyAsync(
        string authToken, long orderId, long amountCents, string currency, string? receiptEmail, CancellationToken cancellationToken)
    {
        // Paymob requires a full billing_data block even when this application has no matching fields for a
        // given member (e.g. no street address on file) — "NA" is Paymob's own documented placeholder for an
        // unavailable required field.
        var billingData = new
        {
            email = receiptEmail ?? "NA",
            first_name = "NA",
            last_name = "NA",
            phone_number = "NA",
            street = "NA",
            building = "NA",
            floor = "NA",
            apartment = "NA",
            city = "NA",
            state = "NA",
            country = "NA",
            postal_code = "NA",
        };

        var request = new
        {
            auth_token = authToken,
            amount_cents = amountCents,
            expiration = 3600,
            order_id = orderId,
            billing_data = billingData,
            currency,
            integration_id = _options.IntegrationId,
        };

        using var response = await _httpClient.PostAsJsonAsync("/api/acceptance/payment_keys", request, cancellationToken);
        var json = await ParseJsonOrThrowAsync(response, cancellationToken);
        return json.GetProperty("token").GetString() ?? throw new PaymobApiException("Payment-key response had no token.");
    }

    private static async Task<JsonElement> ParseJsonOrThrowAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new PaymobApiException($"Paymob returned {(int)response.StatusCode}: {body}");

        return JsonDocument.Parse(body).RootElement;
    }

    /// <summary>Renders a JSON scalar the same way Paymob's own HMAC documentation expects: booleans as
    /// lowercase <c>"true"</c>/<c>"false"</c>, numbers and strings as their plain text form, with no quoting.</summary>
    private static string RawFieldValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.String => element.GetString() ?? string.Empty,
        JsonValueKind.Null => string.Empty,
        _ => element.GetRawText(),
    };

    /// <summary>Paymob amounts are integers in the currency's smallest unit (piastres for EGP), same
    /// two-decimal assumption <c>StripePaymentGatewayService</c> makes.</summary>
    private static long ToSmallestCurrencyUnit(Money amount) => (long)Math.Round(amount.Amount * 100m, MidpointRounding.AwayFromZero);

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }

    private sealed class PaymobApiException(string message) : Exception(message);
}
