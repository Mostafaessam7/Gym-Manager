using GymManager.Domain.Notifications.Errors;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Notifications;

/// <summary>A single outbound message queued for delivery to a member or user over a specific channel.</summary>
public sealed class Notification : AggregateRoot<Guid>
{
    private Notification()
    {
        RecipientAddress = string.Empty;
        Subject = string.Empty;
        Body = string.Empty;
    }

    private Notification(Guid id, NotificationChannel channel, string recipientAddress, string subject, string body, Guid? recipientUserId, Guid? recipientMemberId)
        : base(id)
    {
        Channel = channel;
        RecipientAddress = recipientAddress;
        Subject = subject;
        Body = body;
        RecipientUserId = recipientUserId;
        RecipientMemberId = recipientMemberId;
        Status = NotificationStatus.Pending;
        CreatedOnUtc = DateTimeOffset.UtcNow;
    }

    public NotificationChannel Channel { get; private set; }

    public string RecipientAddress { get; private set; }

    public string Subject { get; private set; }

    public string Body { get; private set; }

    public Guid? RecipientUserId { get; private set; }

    public Guid? RecipientMemberId { get; private set; }

    public NotificationStatus Status { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public DateTimeOffset? SentOnUtc { get; private set; }

    public static Notification Create(
        NotificationChannel channel, string recipientAddress, string subject, string body, Guid? recipientUserId, Guid? recipientMemberId) =>
        new(Guid.NewGuid(), channel, recipientAddress, subject, body, recipientUserId, recipientMemberId);

    public Result MarkSent()
    {
        if (Status != NotificationStatus.Pending)
            return Result.Failure(NotificationErrors.NotPending);

        Status = NotificationStatus.Sent;
        SentOnUtc = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    public Result MarkFailed(string errorMessage)
    {
        if (Status != NotificationStatus.Pending)
            return Result.Failure(NotificationErrors.NotPending);

        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;

        return Result.Success();
    }
}
