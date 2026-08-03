using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Nutrition.DeleteNutritionPlan;

public sealed record DeleteNutritionPlanCommand(Guid PlanId) : ICommand;
