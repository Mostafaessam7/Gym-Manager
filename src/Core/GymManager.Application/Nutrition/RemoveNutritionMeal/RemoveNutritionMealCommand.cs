using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Nutrition.RemoveNutritionMeal;

public sealed record RemoveNutritionMealCommand(Guid PlanId, Guid MealId) : ICommand;
