using FluentValidation;

namespace GymManager.Application.Classes.Sessions.ScheduleSession;

public sealed class ScheduleSessionCommandValidator : AbstractValidator<ScheduleSessionCommand>
{
    public ScheduleSessionCommandValidator()
    {
        RuleFor(c => c.GymClassId).NotEmpty();
        RuleFor(c => c.EndUtc).GreaterThan(c => c.StartUtc);
        RuleFor(c => c.CapacityOverride).GreaterThan(0).When(c => c.CapacityOverride.HasValue);
    }
}
