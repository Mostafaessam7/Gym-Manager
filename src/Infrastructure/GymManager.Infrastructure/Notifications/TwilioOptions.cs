namespace GymManager.Infrastructure.Notifications;

/// <summary>Binds the <c>Twilio</c> configuration section. All three values come from a Twilio account
/// console (Account SID/Auth Token from the dashboard root; the from-number from Phone Numbers → Manage).
/// Deliberately optional, unlike the payment gateways' configuration sections: SMS is an auxiliary
/// notification channel, not a core flow, so the app must still start and work with no Twilio configuration
/// at all — see <c>DependencyInjection.AddInfrastructure</c>, which falls back to <c>LoggingSmsSender</c>
/// whenever any of these three is missing, rather than failing startup the way a missing Stripe section
/// does.</summary>
public sealed class TwilioOptions
{
    public const string SectionName = "Twilio";

    public string? AccountSid { get; init; }

    public string? AuthToken { get; init; }

    /// <summary>The Twilio phone number (or messaging-service sender id) messages are sent from.</summary>
    public string? FromPhoneNumber { get; init; }
}
