using FluentValidation;

namespace GymManager.Application.Staff.RecordCommission;

public sealed class RecordCommissionCommandValidator : AbstractValidator<RecordCommissionCommand>
{
    public RecordCommissionCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
        RuleFor(c => c.Notes).MaximumLength(1000);
    }
}
