using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

public sealed class NutritionControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record BranchResponse(Guid Id);

    private sealed record MemberResponse(Guid Id);

    private sealed record MealResponse(Guid Id, string Name, int Order, int? Calories);

    private sealed record PlanResponse(Guid Id, Guid MemberId, string Name, bool IsActive, IReadOnlyList<MealResponse> Meals);

    private sealed record PagedPlans(IReadOnlyList<PlanResponse> Items, int TotalCount);

    private sealed record LogResponse(Guid Id, Guid MemberId, int TotalCalories, decimal TotalProteinG);

    private sealed record PagedLogs(IReadOnlyList<LogResponse> Items, int TotalCount);

    private async Task<Guid> CreateMemberAsync(HttpClient client)
    {
        var branchResponse = await client.PostAsJsonAsync("/api/v1/branches", new
        {
            name = $"Branch-{Guid.NewGuid():N}",
            country = "USA",
            street = (string?)null,
            city = (string?)null,
            state = (string?)null,
            postalCode = (string?)null,
            phoneNumber = (string?)null,
            email = (string?)null,
        });
        var branch = await branchResponse.Content.ReadFromJsonAsync<BranchResponse>();

        var memberResponse = await client.PostAsJsonAsync("/api/v1/members", new
        {
            branchId = branch!.Id,
            firstName = "Jane",
            lastName = "Doe",
            phoneNumber = $"+1555{Random.Shared.Next(1000000, 9999999)}",
            email = (string?)null,
            dateOfBirth = (DateOnly?)null,
            gender = 2,
            street = (string?)null,
            city = (string?)null,
            state = (string?)null,
            postalCode = (string?)null,
            country = (string?)null,
            emergencyContactName = (string?)null,
            emergencyContactPhone = (string?)null,
        });
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberResponse>();
        return member!.Id;
    }

    private static object NewPlanBody(Guid memberId, string name = "Cutting Plan") => new
    {
        memberId,
        trainerId = (Guid?)null,
        name,
        description = "500 cal deficit",
        dailyCalorieTarget = 2000,
        proteinTargetG = (decimal?)180,
        carbsTargetG = (decimal?)150,
        fatTargetG = (decimal?)60,
        meals = new[]
        {
            new
            {
                name = "Breakfast", order = 1, timeOfDay = "7:00 AM", calories = (int?)500,
                proteinG = (decimal?)40, carbsG = (decimal?)50, fatG = (decimal?)15, notes = (string?)null,
            },
        },
    };

    [Fact]
    public async Task CreatePlan_Should_Return_The_Created_Plan_With_Its_Meals()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Nutrition.Manage, Permissions.Nutrition.View);
        var memberId = await CreateMemberAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/nutrition-plans", NewPlanBody(memberId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var plan = await response.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.Equal(memberId, plan!.MemberId);
        Assert.Single(plan.Meals);
        Assert.Equal("Breakfast", plan.Meals[0].Name);
    }

    [Fact]
    public async Task CreatePlan_For_An_Unknown_Member_Should_Return_NotFound()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Nutrition.Manage);

        var response = await client.PostAsJsonAsync("/api/v1/nutrition-plans", NewPlanBody(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddMeal_Should_Append_To_The_Plans_Meal_List()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Nutrition.Manage, Permissions.Nutrition.View);
        var memberId = await CreateMemberAsync(client);
        var plan = await (await client.PostAsJsonAsync("/api/v1/nutrition-plans", NewPlanBody(memberId)))
            .Content.ReadFromJsonAsync<PlanResponse>();

        var addResponse = await client.PostAsJsonAsync($"/api/v1/nutrition-plans/{plan!.Id}/meals", new
        {
            meal = new
            {
                name = "Lunch", order = 2, timeOfDay = "1:00 PM", calories = (int?)700,
                proteinG = (decimal?)50, carbsG = (decimal?)70, fatG = (decimal?)20, notes = (string?)null,
            },
        });
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var reloaded = await (await client.GetAsync($"/api/v1/nutrition-plans/{plan.Id}")).Content.ReadFromJsonAsync<PlanResponse>();
        Assert.Equal(2, reloaded!.Meals.Count);
    }

    [Fact]
    public async Task RemoveMeal_With_An_Unknown_Id_Should_Return_NotFound()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Nutrition.Manage, Permissions.Nutrition.View);
        var memberId = await CreateMemberAsync(client);
        var plan = await (await client.PostAsJsonAsync("/api/v1/nutrition-plans", NewPlanBody(memberId)))
            .Content.ReadFromJsonAsync<PlanResponse>();

        var response = await client.DeleteAsync($"/api/v1/nutrition-plans/{plan!.Id}/meals/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePlan_Should_Change_Name_Targets_And_Active_State()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Nutrition.Manage, Permissions.Nutrition.View);
        var memberId = await CreateMemberAsync(client);
        var plan = await (await client.PostAsJsonAsync("/api/v1/nutrition-plans", NewPlanBody(memberId)))
            .Content.ReadFromJsonAsync<PlanResponse>();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/nutrition-plans/{plan!.Id}", new
        {
            name = "Renamed Plan",
            description = "Updated",
            dailyCalorieTarget = (int?)2200,
            proteinTargetG = (decimal?)190,
            carbsTargetG = (decimal?)160,
            fatTargetG = (decimal?)65,
            isActive = false,
        });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var reloaded = await (await client.GetAsync($"/api/v1/nutrition-plans/{plan.Id}")).Content.ReadFromJsonAsync<PlanResponse>();
        Assert.Equal("Renamed Plan", reloaded!.Name);
        Assert.False(reloaded.IsActive);
    }

    [Fact]
    public async Task DeletePlan_Should_Remove_It_From_The_Members_List()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Nutrition.Manage, Permissions.Nutrition.View);
        var memberId = await CreateMemberAsync(client);
        var plan = await (await client.PostAsJsonAsync("/api/v1/nutrition-plans", NewPlanBody(memberId)))
            .Content.ReadFromJsonAsync<PlanResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/nutrition-plans/{plan!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var page = await (await client.GetAsync($"/api/v1/nutrition-plans?memberId={memberId}&pageNumber=1&pageSize=10"))
            .Content.ReadFromJsonAsync<PagedPlans>();
        Assert.Empty(page!.Items);
    }

    [Fact]
    public async Task RecordLog_Should_Be_Retrievable_Via_GetLogs_With_Computed_Totals()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Nutrition.Manage, Permissions.Nutrition.View);
        var memberId = await CreateMemberAsync(client);

        var recordResponse = await client.PostAsJsonAsync("/api/v1/nutrition-logs", new
        {
            memberId,
            nutritionPlanId = (Guid?)null,
            loggedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            notes = "Good day",
            entries = new[]
            {
                new { foodName = "Chicken Breast", calories = (int?)200, proteinG = (decimal?)40, carbsG = (decimal?)0, fatG = (decimal?)5, notes = (string?)null },
                new { foodName = "Rice", calories = (int?)300, proteinG = (decimal?)6, carbsG = (decimal?)65, fatG = (decimal?)1, notes = (string?)null },
            },
        });
        Assert.Equal(HttpStatusCode.OK, recordResponse.StatusCode);

        var page = await (await client.GetAsync($"/api/v1/nutrition-logs?memberId={memberId}&pageNumber=1&pageSize=10"))
            .Content.ReadFromJsonAsync<PagedLogs>();
        Assert.Single(page!.Items);
        Assert.Equal(500, page.Items[0].TotalCalories);
        Assert.Equal(46, page.Items[0].TotalProteinG);
    }

    [Fact]
    public async Task RecordLog_For_An_Unknown_Member_Should_Return_NotFound()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Nutrition.Manage);

        var response = await client.PostAsJsonAsync("/api/v1/nutrition-logs", new
        {
            memberId = Guid.NewGuid(),
            nutritionPlanId = (Guid?)null,
            loggedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            notes = (string?)null,
            entries = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPlans_Without_Permission_Should_Return_Forbidden()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Members.View);

        var response = await client.GetAsync($"/api/v1/nutrition-plans?memberId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
