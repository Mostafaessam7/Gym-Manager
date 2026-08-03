using FluentValidation;

namespace GymManager.Application.Workouts.CreateWorkoutPlan;

public sealed class CreateWorkoutPlanCommandValidator : AbstractValidator<CreateWorkoutPlanCommand>
{
    public CreateWorkoutPlanCommandValidator()
    {
        RuleFor(c => c.MemberId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(2000);

        RuleForEach(c => c.Exercises).ChildRules(exercise =>
        {
            exercise.RuleFor(e => e.ExerciseName).NotEmpty().MaximumLength(200);
            exercise.RuleFor(e => e.DayNumber).GreaterThan(0);
            exercise.RuleFor(e => e.Order).GreaterThan(0);
            exercise.RuleFor(e => e.Sets).GreaterThan(0).When(e => e.Sets.HasValue);
            exercise.RuleFor(e => e.Reps).GreaterThan(0).When(e => e.Reps.HasValue);
        });
    }
}
