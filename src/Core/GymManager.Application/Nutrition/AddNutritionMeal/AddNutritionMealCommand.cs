using GymManager.Application.Nutrition.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Nutrition.AddNutritionMeal;

public sealed record AddNutritionMealCommand(Guid PlanId, NutritionMealInput Meal) : ICommand<Result<NutritionPlanMealResponse>>;
