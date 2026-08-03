namespace GymManager.Application.Nutrition.Contracts;

public sealed record NutritionMealInput(
    string Name, int Order, string? TimeOfDay, int? Calories, decimal? ProteinG, decimal? CarbsG, decimal? FatG, string? Notes);

public sealed record NutritionPlanMealResponse(
    Guid Id, string Name, int Order, string? TimeOfDay, int? Calories, decimal? ProteinG, decimal? CarbsG, decimal? FatG, string? Notes);

public sealed record NutritionPlanResponse(
    Guid Id,
    Guid MemberId,
    Guid? TrainerId,
    string Name,
    string? Description,
    int? DailyCalorieTarget,
    decimal? ProteinTargetG,
    decimal? CarbsTargetG,
    decimal? FatTargetG,
    bool IsActive,
    IReadOnlyCollection<NutritionPlanMealResponse> Meals,
    DateTimeOffset CreatedOnUtc);

public sealed record NutritionLogEntryResponse(
    Guid Id, string FoodName, int? Calories, decimal? ProteinG, decimal? CarbsG, decimal? FatG, string? Notes);

public sealed record NutritionLogResponse(
    Guid Id,
    Guid MemberId,
    Guid? NutritionPlanId,
    DateOnly LoggedOn,
    string? Notes,
    int TotalCalories,
    decimal TotalProteinG,
    decimal TotalCarbsG,
    decimal TotalFatG,
    IReadOnlyCollection<NutritionLogEntryResponse> Entries);
