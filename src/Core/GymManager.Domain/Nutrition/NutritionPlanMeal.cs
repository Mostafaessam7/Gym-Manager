using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Nutrition;

/// <summary>One prescribed meal within a <see cref="NutritionPlan"/> — a target, not a record of what was
/// actually eaten (see <see cref="NutritionLog"/> for that).</summary>
public sealed class NutritionPlanMeal : Entity<Guid>
{
    private NutritionPlanMeal()
    {
        Name = string.Empty;
    }

    internal NutritionPlanMeal(
        string name, int order, string? timeOfDay, int? calories, decimal? proteinG, decimal? carbsG, decimal? fatG, string? notes)
        : base(Guid.NewGuid())
    {
        Name = name;
        Order = order;
        TimeOfDay = timeOfDay;
        Calories = calories;
        ProteinG = proteinG;
        CarbsG = carbsG;
        FatG = fatG;
        Notes = notes;
    }

    public string Name { get; private set; }

    public int Order { get; private set; }

    /// <summary>Free-text suggested time (e.g. "7:00 AM", "Post-workout") rather than a strict clock time —
    /// meal timing is typically flexible relative to a member's day, not fixed.</summary>
    public string? TimeOfDay { get; private set; }

    public int? Calories { get; private set; }

    public decimal? ProteinG { get; private set; }

    public decimal? CarbsG { get; private set; }

    public decimal? FatG { get; private set; }

    public string? Notes { get; private set; }

    internal void Update(
        string name, int order, string? timeOfDay, int? calories, decimal? proteinG, decimal? carbsG, decimal? fatG, string? notes)
    {
        Name = name;
        Order = order;
        TimeOfDay = timeOfDay;
        Calories = calories;
        ProteinG = proteinG;
        CarbsG = carbsG;
        FatG = fatG;
        Notes = notes;
    }
}
