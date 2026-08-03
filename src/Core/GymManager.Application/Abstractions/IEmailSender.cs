namespace GymManager.Application.Abstractions;

/// <summary>Sends transactional email. Implemented in Infrastructure over SMTP.</summary>
public interface IEmailSender
{
    Task SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default);
}
