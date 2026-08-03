using FluentValidation;

namespace GymManager.Application.Memberships.Subscriptions.RenewMembership;

public sealed class RenewMembershipCommandValidator : AbstractValidator<RenewMembershipCommand>
{
    public RenewMembershipCommandValidator()
    {
        RuleFor(c => c.AdditionalDays).GreaterThan(0);
        RuleFor(c => c.AmountPaid).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
    }
}
