using FluentValidation;

namespace GymManager.Application.Expenses.UpdateExpense;

public sealed class UpdateExpenseCommandValidator : AbstractValidator<UpdateExpenseCommand>
{
    public UpdateExpenseCommandValidator()
    {
        RuleFor(c => c.Description).NotEmpty().MaximumLength(500);
        RuleFor(c => c.Amount).GreaterThan(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.PaidTo).NotEmpty().MaximumLength(200);
    }
}
