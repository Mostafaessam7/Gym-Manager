using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Nutrition;

/// <summary>One food item actually consumed as part of a logged day — independent of what any assigned plan
/// prescribed.</summary>
public sealed class NutritionLogEntry : Entity<Guid>
{
    private NutritionLogEntry()
    {
        FoodName = string.Empty;
    }

    internal NutritionLogEntry(string foodName, int? calories, decimal? proteinG, decimal? carbsG, decimal? fatG, string? notes)
        : base(Guid.NewGuid())
    {
        FoodName = foodName;
        Calories = calories;
        ProteinG = proteinG;
        CarbsG = carbsG;
        FatG = fatG;
        Notes = notes;
    }

    public string FoodName { get; private set; }

    public int? Calories { get; private set; }

    public decimal? ProteinG { get; private set; }

    public decimal? CarbsG { get; private set; }

    public decimal? FatG { get; private set; }

    public string? Notes { get; private set; }
}
