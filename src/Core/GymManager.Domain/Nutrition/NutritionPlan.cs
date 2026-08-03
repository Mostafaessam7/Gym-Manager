using GymManager.Domain.Nutrition.Errors;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Nutrition;

/// <summary>A prescribed daily nutrition target for a member — overall macro targets plus a named collection
/// of meals — assigned by a trainer/dietitian (or self-assigned, if <see cref="TrainerId"/> is null).</summary>
public sealed class NutritionPlan : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<NutritionPlanMeal> _meals = [];

    private NutritionPlan()
    {
        Name = string.Empty;
    }

    private NutritionPlan(
        Guid id, Guid memberId, Guid? trainerId, string name, string? description,
        int? dailyCalorieTarget, decimal? proteinTargetG, decimal? carbsTargetG, decimal? fatTargetG)
        : base(id)
    {
        MemberId = memberId;
        TrainerId = trainerId;
        Name = name;
        Description = description;
        DailyCalorieTarget = dailyCalorieTarget;
        ProteinTargetG = proteinTargetG;
        CarbsTargetG = carbsTargetG;
        FatTargetG = fatTargetG;
        IsActive = true;
    }

    public Guid MemberId { get; private set; }

    public Guid? TrainerId { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public int? DailyCalorieTarget { get; private set; }

    public decimal? ProteinTargetG { get; private set; }

    public decimal? CarbsTargetG { get; private set; }

    public decimal? FatTargetG { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<NutritionPlanMeal> Meals => _meals.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static NutritionPlan Create(
        Guid memberId, Guid? trainerId, string name, string? description,
        int? dailyCalorieTarget, decimal? proteinTargetG, decimal? carbsTargetG, decimal? fatTargetG) =>
        new(Guid.NewGuid(), memberId, trainerId, name.Trim(), description?.Trim(), dailyCalorieTarget, proteinTargetG, carbsTargetG, fatTargetG);

    public void UpdateDetails(
        string name, string? description, int? dailyCalorieTarget, decimal? proteinTargetG, decimal? carbsTargetG, decimal? fatTargetG)
    {
        Name = name.Trim();
        Description = description?.Trim();
        DailyCalorieTarget = dailyCalorieTarget;
        ProteinTargetG = proteinTargetG;
        CarbsTargetG = carbsTargetG;
        FatTargetG = fatTargetG;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public NutritionPlanMeal AddMeal(
        string name, int order, string? timeOfDay, int? calories, decimal? proteinG, decimal? carbsG, decimal? fatG, string? notes)
    {
        var meal = new NutritionPlanMeal(name.Trim(), order, timeOfDay?.Trim(), calories, proteinG, carbsG, fatG, notes?.Trim());
        _meals.Add(meal);
        return meal;
    }

    public Result UpdateMeal(
        Guid mealId, string name, int order, string? timeOfDay, int? calories, decimal? proteinG, decimal? carbsG, decimal? fatG, string? notes)
    {
        var meal = _meals.FirstOrDefault(m => m.Id == mealId);
        if (meal is null)
            return Result.Failure(NutritionErrors.MealNotFound);

        meal.Update(name.Trim(), order, timeOfDay?.Trim(), calories, proteinG, carbsG, fatG, notes?.Trim());
        return Result.Success();
    }

    public Result RemoveMeal(Guid mealId)
    {
        var meal = _meals.FirstOrDefault(m => m.Id == mealId);
        if (meal is null)
            return Result.Failure(NutritionErrors.MealNotFound);

        _meals.Remove(meal);
        return Result.Success();
    }

    public void SetCreated(DateTimeOffset onUtc, string? by)
    {
        CreatedOnUtc = onUtc;
        CreatedBy = by;
    }

    public void SetModified(DateTimeOffset onUtc, string? by)
    {
        ModifiedOnUtc = onUtc;
        ModifiedBy = by;
    }
}
