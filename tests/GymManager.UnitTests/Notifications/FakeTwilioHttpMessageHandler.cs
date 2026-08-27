using System.Net;
using System.Text;
using System.Text.Json;

namespace GymManager.UnitTests.Notifications;

/// <summary>Stands in for Twilio's REST API over HTTP, so <c>TwilioSmsSender</c>'s real request-building and
/// response-parsing can be exercised without a network call or real Twilio credentials — same purpose as the
/// payment-gateway fakes in <c>GymManager.UnitTests.PaymentGateways</c>.</summary>
public sealed class FakeTwilioHttpMessageHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastRequestBody { get; private set; }

    public bool FailNextRequest { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        if (FailNextRequest)
        {
            var errorJson = JsonSerializer.Serialize(new { code = 21211, message = "The 'To' number is not a valid phone number.", status = 400 });
            return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(errorJson, Encoding.UTF8, "application/json") };
        }

        var json = JsonSerializer.Serialize(new { sid = "SMfake1234567890", status = "queued", to = "+15551234567", from = "+15557654321" });
        return new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
