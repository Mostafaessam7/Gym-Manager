using FluentValidation;

namespace GymManager.Application.Memberships.Plans.UpdatePlan;

public sealed class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
{
    public UpdatePlanCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(150);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(1000);
        RuleFor(c => c.Price).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.DurationInDays).GreaterThan(0);
        RuleFor(c => c.MaxFreezeDays).GreaterThanOrEqualTo(0);
    }
}
