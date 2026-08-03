using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Nutrition.UpdateNutritionPlan;

public sealed record UpdateNutritionPlanCommand(
    Guid PlanId,
    string Name,
    string? Description,
    int? DailyCalorieTarget,
    decimal? ProteinTargetG,
    decimal? CarbsTargetG,
    decimal? FatTargetG,
    bool IsActive) : ICommand;
