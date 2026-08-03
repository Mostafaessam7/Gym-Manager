namespace GymManager.Infrastructure.Notifications;

/// <summary>Binds the <c>Email</c> configuration section used to send outbound SMTP mail.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public required string SmtpHost { get; init; }

    public int SmtpPort { get; init; } = 587;

    public string? Username { get; init; }

    public string? Password { get; init; }

    public bool UseSsl { get; init; } = true;

    public required string FromAddress { get; init; }

    public required string FromName { get; init; }
}
