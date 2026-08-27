using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GymManager.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace GymManager.Infrastructure.Notifications;

/// <summary>
/// <inheritdoc cref="ISmsSender"/>
/// </summary>
/// <remarks>
/// Calls Twilio's real REST API directly (<c>POST /2010-04-01/Accounts/{AccountSid}/Messages.json</c>,
/// documented at twilio.com/docs/sms/api) via a plain <see cref="HttpClient"/> rather than pulling in the
/// official Twilio SDK — the API surface this needs is one simple, stable, Basic-Auth-protected POST, not
/// enough to justify the extra dependency (consistent with how <c>PaymobPaymentGatewayService</c>/
/// <c>FawryPaymentGatewayService</c> were built directly against their HTTP APIs rather than an SDK). Unlike
/// those two, Twilio offers a genuine free trial account with real (if rate/feature-limited) credentials, so
/// this is closer to Stripe's verifiability than Paymob/Fawry's — but no account was available to this
/// session either way; see <c>TwilioSmsSenderTests</c> for the same fake-HTTP-handler proof technique used
/// for every other externally-integrated service in this codebase.
/// </remarks>
public sealed class TwilioSmsSender : ISmsSender, IDisposable
{
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioSmsSender> _logger;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public TwilioSmsSender(TwilioOptions options, ILogger<TwilioSmsSender> logger)
        : this(options, logger, httpMessageHandler: null)
    {
    }

    /// <summary>Accepts an explicit <see cref="HttpMessageHandler"/> so tests can point this at an in-memory
    /// fake instead of a real network call — the same seam <c>PaymobPaymentGatewayService</c>/
    /// <c>FawryPaymentGatewayService</c> use.</summary>
    public TwilioSmsSender(TwilioOptions options, ILogger<TwilioSmsSender> logger, HttpMessageHandler? httpMessageHandler)
    {
        _options = options;
        _logger = logger;
        _ownsHttpClient = httpMessageHandler is null;
        _httpClient = httpMessageHandler is null
            ? new HttpClient { BaseAddress = new Uri("https://api.twilio.com") }
            : new HttpClient(httpMessageHandler, disposeHandler: false) { BaseAddress = new Uri("https://api.twilio.com") };

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.AccountSid}:{options.AuthToken}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var form = new Dictionary<string, string>
        {
            ["From"] = _options.FromPhoneNumber!,
            ["To"] = phoneNumber,
            ["Body"] = message,
        };

        using var response = await _httpClient.PostAsync(
            $"/2010-04-01/Accounts/{_options.AccountSid}/Messages.json", new FormUrlEncodedContent(form), cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = TryExtractErrorMessage(body) ?? $"Twilio returned {(int)response.StatusCode}.";
            throw new InvalidOperationException($"Failed to send SMS via Twilio: {errorMessage}");
        }

        var sid = JsonDocument.Parse(body).RootElement.TryGetProperty("sid", out var sidProp) ? sidProp.GetString() : null;
        _logger.LogInformation("Sent SMS to {PhoneNumber} via Twilio (sid {Sid})", phoneNumber, sid);
    }

    private static string? TryExtractErrorMessage(string body)
    {
        try
        {
            var json = JsonDocument.Parse(body).RootElement;
            return json.TryGetProperty("message", out var messageProp) ? messageProp.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}
