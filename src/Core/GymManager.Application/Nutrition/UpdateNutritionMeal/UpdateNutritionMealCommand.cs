using GymManager.Application.Nutrition.Contracts;
using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Nutrition.UpdateNutritionMeal;

public sealed record UpdateNutritionMealCommand(Guid PlanId, Guid MealId, NutritionMealInput Meal) : ICommand;
