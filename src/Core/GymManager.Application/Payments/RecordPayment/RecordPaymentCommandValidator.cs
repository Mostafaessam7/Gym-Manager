using FluentValidation;

namespace GymManager.Application.Payments.RecordPayment;

public sealed class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(c => c.MemberId).NotEmpty();
        RuleFor(c => c.BranchId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
    }
}
