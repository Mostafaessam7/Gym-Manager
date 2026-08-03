using GymManager.Domain.Nutrition;
using Xunit;

namespace GymManager.UnitTests.Nutrition;

public sealed class NutritionLogTests
{
    [Fact]
    public void Record_Should_Capture_Member_Plan_And_Date()
    {
        var memberId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var loggedOn = new DateOnly(2026, 7, 29);

        var log = NutritionLog.Record(memberId, planId, loggedOn, "Felt good today");

        Assert.Equal(memberId, log.MemberId);
        Assert.Equal(planId, log.NutritionPlanId);
        Assert.Equal(loggedOn, log.LoggedOn);
        Assert.Equal("Felt good today", log.Notes);
        Assert.Empty(log.Entries);
    }

    [Fact]
    public void AddEntry_Should_Add_It_To_The_Log()
    {
        var log = NutritionLog.Record(Guid.NewGuid(), null, DateOnly.FromDateTime(DateTime.UtcNow), null);

        var entry = log.AddEntry("Chicken Breast", calories: 200, proteinG: 40, carbsG: 0, fatG: 5, notes: null);

        Assert.Single(log.Entries);
        Assert.Equal(entry.Id, log.Entries.Single().Id);
        Assert.Equal("Chicken Breast", log.Entries.Single().FoodName);
    }

    [Fact]
    public void Totals_Should_Sum_Across_All_Entries()
    {
        var log = NutritionLog.Record(Guid.NewGuid(), null, DateOnly.FromDateTime(DateTime.UtcNow), null);
        log.AddEntry("Chicken Breast", 200, 40, 0, 5, null);
        log.AddEntry("Rice", 300, 6, 65, 1, null);

        Assert.Equal(500, log.TotalCalories);
        Assert.Equal(46, log.TotalProteinG);
        Assert.Equal(65, log.TotalCarbsG);
        Assert.Equal(6, log.TotalFatG);
    }

    [Fact]
    public void Totals_Should_Be_Zero_For_An_Empty_Log()
    {
        var log = NutritionLog.Record(Guid.NewGuid(), null, DateOnly.FromDateTime(DateTime.UtcNow), null);

        Assert.Equal(0, log.TotalCalories);
        Assert.Equal(0, log.TotalProteinG);
    }
}
