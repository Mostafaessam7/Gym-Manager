using FluentValidation;
using GymManager.Domain.Payments;

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

        // Guards against the request JSON simply omitting "provider" — System.Text.Json leaves a missing
        // enum property at its default (0/None) rather than failing model binding, which would otherwise
        // reach PaymentGatewayServiceResolver.Resolve and surface as an opaque 500 (GatewayNotConfigured)
        // instead of a clear 400 naming the real problem.
        RuleFor(c => c.Provider).NotEqual(PaymentGatewayProvider.None)
            .WithMessage("A payment gateway provider (Stripe, Paymob, or Fawry) must be specified.");
    }
}
