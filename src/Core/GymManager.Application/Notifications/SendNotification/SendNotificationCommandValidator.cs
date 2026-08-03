using FluentValidation;

namespace GymManager.Application.Notifications.SendNotification;

public sealed class SendNotificationCommandValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationCommandValidator()
    {
        RuleFor(c => c.RecipientAddress).NotEmpty().MaximumLength(256);
        RuleFor(c => c.Subject).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Body).NotEmpty();
    }
}
