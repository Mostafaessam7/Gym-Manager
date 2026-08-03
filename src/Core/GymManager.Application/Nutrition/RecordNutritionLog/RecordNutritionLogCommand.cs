using GymManager.Application.Nutrition.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Nutrition.RecordNutritionLog;

public sealed record NutritionLogEntryInput(string FoodName, int? Calories, decimal? ProteinG, decimal? CarbsG, decimal? FatG, string? Notes);

public sealed record RecordNutritionLogCommand(
    Guid MemberId, Guid? NutritionPlanId, DateOnly LoggedOn, string? Notes, IReadOnlyCollection<NutritionLogEntryInput> Entries)
    : ICommand<Result<NutritionLogResponse>>;
