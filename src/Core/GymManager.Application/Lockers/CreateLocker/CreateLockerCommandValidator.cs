using FluentValidation;

namespace GymManager.Application.Lockers.CreateLocker;

public sealed class CreateLockerCommandValidator : AbstractValidator<CreateLockerCommand>
{
    public CreateLockerCommandValidator()
    {
        RuleFor(c => c.BranchId).NotEmpty();
        RuleFor(c => c.Number).NotEmpty().MaximumLength(20);
    }
}
