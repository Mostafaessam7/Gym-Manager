using GymManager.Domain.Nutrition;

namespace GymManager.Application.Nutrition.Contracts;

public static class NutritionMappingExtensions
{
    public static NutritionPlanResponse ToResponse(this NutritionPlan plan) => new(
        plan.Id,
        plan.MemberId,
        plan.TrainerId,
        plan.Name,
        plan.Description,
        plan.DailyCalorieTarget,
        plan.ProteinTargetG,
        plan.CarbsTargetG,
        plan.FatTargetG,
        plan.IsActive,
        [.. plan.Meals
            .OrderBy(m => m.Order)
            .Select(m => new NutritionPlanMealResponse(m.Id, m.Name, m.Order, m.TimeOfDay, m.Calories, m.ProteinG, m.CarbsG, m.FatG, m.Notes))],
        plan.CreatedOnUtc);

    public static NutritionLogResponse ToResponse(this NutritionLog log) => new(
        log.Id,
        log.MemberId,
        log.NutritionPlanId,
        log.LoggedOn,
        log.Notes,
        log.TotalCalories,
        log.TotalProteinG,
        log.TotalCarbsG,
        log.TotalFatG,
        [.. log.Entries.Select(e => new NutritionLogEntryResponse(e.Id, e.FoodName, e.Calories, e.ProteinG, e.CarbsG, e.FatG, e.Notes))]);
}
