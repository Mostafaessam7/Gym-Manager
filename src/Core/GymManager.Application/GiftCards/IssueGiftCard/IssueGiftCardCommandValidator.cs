using FluentValidation;

namespace GymManager.Application.GiftCards.IssueGiftCard;

public sealed class IssueGiftCardCommandValidator : AbstractValidator<IssueGiftCardCommand>
{
    public IssueGiftCardCommandValidator()
    {
        RuleFor(c => c.InitialBalance).GreaterThan(0);
        RuleFor(c => c.Code).MaximumLength(30);
    }
}
