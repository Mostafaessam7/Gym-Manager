using FluentValidation;

namespace GymManager.Application.Trainers.UpdateTrainer;

public sealed class UpdateTrainerCommandValidator : AbstractValidator<UpdateTrainerCommand>
{
    public UpdateTrainerCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Specialization).NotEmpty().MaximumLength(150);
        RuleFor(c => c.Email).EmailAddress().When(c => !string.IsNullOrWhiteSpace(c.Email));
    }
}
