using FluentValidation;

namespace GymManager.Application.Trainers.CreateTrainer;

public sealed class CreateTrainerCommandValidator : AbstractValidator<CreateTrainerCommand>
{
    public CreateTrainerCommandValidator()
    {
        RuleFor(c => c.BranchId).NotEmpty();
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Specialization).NotEmpty().MaximumLength(150);
        RuleFor(c => c.Email).EmailAddress().When(c => !string.IsNullOrWhiteSpace(c.Email));
    }
}
