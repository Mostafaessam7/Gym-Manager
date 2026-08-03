using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Notifications.Errors;

public static class NotificationErrors
{
    public static readonly Error NotFound = Error.NotFound("Notification.NotFound", "The notification was not found.");

    public static readonly Error NotPending = Error.Conflict("Notification.NotPending", "Only a pending notification can be marked sent or failed.");
}
