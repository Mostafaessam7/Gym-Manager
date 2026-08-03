using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Nutrition.Errors;

public static class NutritionErrors
{
    public static readonly Error PlanNotFound = Error.NotFound("Nutrition.PlanNotFound", "The nutrition plan was not found.");

    public static readonly Error MealNotFound = Error.NotFound("Nutrition.MealNotFound", "The meal was not found on this plan.");

    public static readonly Error LogNotFound = Error.NotFound("Nutrition.LogNotFound", "The nutrition log was not found.");
}
