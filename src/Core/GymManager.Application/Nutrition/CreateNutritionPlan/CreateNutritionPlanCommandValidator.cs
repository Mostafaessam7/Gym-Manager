using FluentValidation;

namespace GymManager.Application.Nutrition.CreateNutritionPlan;

public sealed class CreateNutritionPlanCommandValidator : AbstractValidator<CreateNutritionPlanCommand>
{
    public CreateNutritionPlanCommandValidator()
    {
        RuleFor(c => c.MemberId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.DailyCalorieTarget).GreaterThan(0).When(c => c.DailyCalorieTarget.HasValue);

        RuleForEach(c => c.Meals).ChildRules(meal =>
        {
            meal.RuleFor(m => m.Name).NotEmpty().MaximumLength(200);
            meal.RuleFor(m => m.Order).GreaterThan(0);
            meal.RuleFor(m => m.Calories).GreaterThan(0).When(m => m.Calories.HasValue);
        });
    }
}
