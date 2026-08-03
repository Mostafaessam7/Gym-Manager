using GymManager.Domain.Nutrition;
using Xunit;

namespace GymManager.UnitTests.Nutrition;

public sealed class NutritionPlanTests
{
    private static NutritionPlan CreatePlan() =>
        NutritionPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Cutting Plan", "500 cal deficit", 2000, 180, 150, 60);

    [Fact]
    public void Create_Should_Set_Targets_And_Default_To_Active()
    {
        var plan = CreatePlan();

        Assert.Equal("Cutting Plan", plan.Name);
        Assert.Equal(2000, plan.DailyCalorieTarget);
        Assert.Equal(180, plan.ProteinTargetG);
        Assert.True(plan.IsActive);
        Assert.Empty(plan.Meals);
    }

    [Fact]
    public void AddMeal_Should_Add_It_To_The_Plan()
    {
        var plan = CreatePlan();

        var meal = plan.AddMeal("Breakfast", order: 1, "7:00 AM", calories: 500, proteinG: 40, carbsG: 50, fatG: 15, notes: null);

        Assert.Single(plan.Meals);
        Assert.Equal(meal.Id, plan.Meals.Single().Id);
        Assert.Equal("Breakfast", plan.Meals.Single().Name);
    }

    [Fact]
    public void UpdateMeal_Should_Fail_For_An_Unknown_Id()
    {
        var plan = CreatePlan();

        var result = plan.UpdateMeal(Guid.NewGuid(), "Lunch", 2, null, 600, null, null, null, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Nutrition.MealNotFound", result.Error.Code);
    }

    [Fact]
    public void UpdateMeal_Should_Replace_The_Fields_Of_The_Matching_Meal()
    {
        var plan = CreatePlan();
        var meal = plan.AddMeal("Breakfast", 1, "7:00 AM", 500, 40, 50, 15, null);

        var result = plan.UpdateMeal(meal.Id, "Big Breakfast", 1, "7:30 AM", 700, 50, 60, 20, "More filling");

        Assert.True(result.IsSuccess);
        var updated = plan.Meals.Single();
        Assert.Equal("Big Breakfast", updated.Name);
        Assert.Equal(700, updated.Calories);
        Assert.Equal("More filling", updated.Notes);
    }

    [Fact]
    public void RemoveMeal_Should_Remove_The_Matching_Meal()
    {
        var plan = CreatePlan();
        var meal = plan.AddMeal("Breakfast", 1, null, 500, null, null, null, null);

        var result = plan.RemoveMeal(meal.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(plan.Meals);
    }

    [Fact]
    public void RemoveMeal_Should_Fail_For_An_Unknown_Id()
    {
        var plan = CreatePlan();

        var result = plan.RemoveMeal(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Nutrition.MealNotFound", result.Error.Code);
    }

    [Fact]
    public void Deactivate_Then_Activate_Should_Toggle_IsActive()
    {
        var plan = CreatePlan();

        plan.Deactivate();
        Assert.False(plan.IsActive);

        plan.Activate();
        Assert.True(plan.IsActive);
    }
}
