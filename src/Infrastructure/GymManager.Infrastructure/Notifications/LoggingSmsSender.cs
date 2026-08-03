using GymManager.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace GymManager.Infrastructure.Notifications;

/// <summary>
/// Default <see cref="ISmsSender"/> implementation that only logs the outbound message. Stands in for a
/// real provider (Twilio, Vonage, etc.) until one is configured — callers depend only on the abstraction.
/// </summary>
public sealed class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SMS to {PhoneNumber}: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }
}
