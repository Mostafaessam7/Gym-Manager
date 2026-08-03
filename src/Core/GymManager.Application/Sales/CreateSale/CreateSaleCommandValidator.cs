using FluentValidation;
using GymManager.Domain.Payments;

namespace GymManager.Application.Sales.CreateSale;

public sealed class CreateSaleCommandValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleCommandValidator()
    {
        RuleFor(c => c.BranchId).NotEmpty();
        RuleFor(c => c.Lines).NotEmpty();
        RuleForEach(c => c.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });

        When(c => c.SplitPayments is { Count: > 0 }, () =>
        {
            RuleForEach(c => c.SplitPayments!).ChildRules(payment =>
            {
                payment.RuleFor(p => p.Amount).GreaterThan(0);
                payment.RuleFor(p => p.GiftCardCode)
                    .NotEmpty()
                    .When(p => p.Method == PaymentMethod.GiftCard)
                    .WithMessage("A gift card code is required when the payment method is GiftCard.");
            });
        });
    }
}
