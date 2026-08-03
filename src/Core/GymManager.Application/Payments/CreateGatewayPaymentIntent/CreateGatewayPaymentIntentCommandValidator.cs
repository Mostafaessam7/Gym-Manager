using FluentValidation;

namespace GymManager.Application.Payments.CreateGatewayPaymentIntent;

public sealed class CreateGatewayPaymentIntentCommandValidator : AbstractValidator<CreateGatewayPaymentIntentCommand>
{
    public CreateGatewayPaymentIntentCommandValidator()
    {
        RuleFor(c => c.MemberId).NotEmpty();
        RuleFor(c => c.BranchId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.ReceiptEmail).EmailAddress().When(c => !string.IsNullOrWhiteSpace(c.ReceiptEmail));
    }
}
