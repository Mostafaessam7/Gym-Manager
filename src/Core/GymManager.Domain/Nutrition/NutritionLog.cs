using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Nutrition;

/// <summary>A record of what a member actually ate on a given day, optionally tied back to a
/// <see cref="NutritionPlan"/> they were following.</summary>
public sealed class NutritionLog : AggregateRoot<Guid>
{
    private readonly List<NutritionLogEntry> _entries = [];

    private NutritionLog()
    {
    }

    private NutritionLog(Guid id, Guid memberId, Guid? nutritionPlanId, DateOnly loggedOn, string? notes)
        : base(id)
    {
        MemberId = memberId;
        NutritionPlanId = nutritionPlanId;
        LoggedOn = loggedOn;
        Notes = notes;
    }

    public Guid MemberId { get; private set; }

    public Guid? NutritionPlanId { get; private set; }

    public DateOnly LoggedOn { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyCollection<NutritionLogEntry> Entries => _entries.AsReadOnly();

    public int TotalCalories => _entries.Sum(e => e.Calories ?? 0);

    public decimal TotalProteinG => _entries.Sum(e => e.ProteinG ?? 0);

    public decimal TotalCarbsG => _entries.Sum(e => e.CarbsG ?? 0);

    public decimal TotalFatG => _entries.Sum(e => e.FatG ?? 0);

    public static NutritionLog Record(Guid memberId, Guid? nutritionPlanId, DateOnly loggedOn, string? notes) =>
        new(Guid.NewGuid(), memberId, nutritionPlanId, loggedOn, notes?.Trim());

    public NutritionLogEntry AddEntry(string foodName, int? calories, decimal? proteinG, decimal? carbsG, decimal? fatG, string? notes)
    {
        var entry = new NutritionLogEntry(foodName.Trim(), calories, proteinG, carbsG, fatG, notes?.Trim());
        _entries.Add(entry);
        return entry;
    }
}
