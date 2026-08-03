using FluentValidation;

namespace GymManager.Application.Classes.UpdateGymClass;

public sealed class UpdateGymClassCommandValidator : AbstractValidator<UpdateGymClassCommand>
{
    public UpdateGymClassCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(150);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(1000);
        RuleFor(c => c.TrainerId).NotEmpty();
        RuleFor(c => c.Capacity).GreaterThan(0);
        RuleFor(c => c.DurationMinutes).GreaterThan(0);
    }
}
