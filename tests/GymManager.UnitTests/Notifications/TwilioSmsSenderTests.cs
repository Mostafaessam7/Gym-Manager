using GymManager.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GymManager.UnitTests.Notifications;

/// <summary>
/// Exercises <see cref="TwilioSmsSender"/>'s request-building and response/error-parsing against a fake HTTP
/// handler standing in for Twilio's REST API — the same style of proof used for the payment gateways in
/// <c>GymManager.UnitTests.PaymentGateways</c>. See that service's own class remarks for the verification
/// caveat this shares with Paymob/Fawry (no real account was available to this session either).
/// </summary>
public sealed class TwilioSmsSenderTests
{
    private static (TwilioSmsSender Sender, FakeTwilioHttpMessageHandler Handler) CreateSender()
    {
        var handler = new FakeTwilioHttpMessageHandler();
        var options = new TwilioOptions { AccountSid = "ACfake1234567890", AuthToken = "fake_auth_token", FromPhoneNumber = "+15557654321" };
        var sender = new TwilioSmsSender(options, NullLogger<TwilioSmsSender>.Instance, handler);
        return (sender, handler);
    }

    [Fact]
    public async Task SendAsync_Should_Post_To_The_Documented_Twilio_Messages_Endpoint()
    {
        var (sender, handler) = CreateSender();

        await sender.SendAsync("+15551234567", "Your membership expires soon.");

        Assert.Contains("/2010-04-01/Accounts/ACfake1234567890/Messages.json", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task SendAsync_Should_Authenticate_With_Basic_Auth_Using_The_Configured_Credentials()
    {
        var (sender, handler) = CreateSender();

        await sender.SendAsync("+15551234567", "Hello");

        Assert.Equal("Basic", handler.LastRequest!.Headers.Authorization!.Scheme);
        var decoded = Convert.FromBase64String(handler.LastRequest.Headers.Authorization.Parameter!);
        Assert.Equal("ACfake1234567890:fake_auth_token", System.Text.Encoding.UTF8.GetString(decoded));
    }

    [Fact]
    public async Task SendAsync_Should_Send_The_From_To_And_Body_Fields()
    {
        var (sender, handler) = CreateSender();

        await sender.SendAsync("+15551234567", "Your membership expires soon.");

        Assert.Contains("From=%2B15557654321", handler.LastRequestBody);
        Assert.Contains("To=%2B15551234567", handler.LastRequestBody);
        Assert.Contains("Body=Your", handler.LastRequestBody);
    }

    [Fact]
    public async Task SendAsync_When_Twilio_Rejects_The_Request_Should_Throw_With_Twilios_Error_Message()
    {
        var (sender, handler) = CreateSender();
        handler.FailNextRequest = true;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("not-a-number", "Hello"));

        Assert.Contains("not a valid phone number", exception.Message);
    }
}
