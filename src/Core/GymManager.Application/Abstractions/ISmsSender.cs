namespace GymManager.Application.Abstractions;

/// <summary>
/// Sends SMS messages. This is a provider-agnostic seam — the default Infrastructure implementation only
/// logs the message, and a real provider (Twilio, Vonage, etc.) can be swapped in without touching callers.
/// </summary>
public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
