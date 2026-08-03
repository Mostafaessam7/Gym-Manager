using GymManager.Application.Nutrition.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Nutrition.CreateNutritionPlan;

public sealed record CreateNutritionPlanCommand(
    Guid MemberId,
    Guid? TrainerId,
    string Name,
    string? Description,
    int? DailyCalorieTarget,
    decimal? ProteinTargetG,
    decimal? CarbsTargetG,
    decimal? FatTargetG,
    IReadOnlyCollection<NutritionMealInput> Meals) : ICommand<Result<NutritionPlanResponse>>;
