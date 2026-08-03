using GymManager.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace GymManager.Infrastructure.Notifications;

/// <inheritdoc cref="IEmailSender"/>
public sealed class EmailSender(EmailOptions options, ILogger<EmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string toAddress, string subject, string body, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();

        await client.ConnectAsync(
            options.SmtpHost, options.SmtpPort, options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);

        if (!string.IsNullOrWhiteSpace(options.Username))
            await client.AuthenticateAsync(options.Username, options.Password ?? string.Empty, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Sent email to {Recipient} with subject {Subject}", toAddress, subject);
    }
}
