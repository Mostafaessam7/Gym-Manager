using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

public sealed class WorkoutsControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record BranchResponse(Guid Id);

    private sealed record MemberResponse(Guid Id);

    private sealed record ExerciseResponse(Guid Id, string ExerciseName, int DayNumber, int Order, int? Sets, int? Reps);

    private sealed record PlanResponse(Guid Id, Guid MemberId, string Name, bool IsActive, IReadOnlyList<ExerciseResponse> Exercises);

    private sealed record PagedPlans(IReadOnlyList<PlanResponse> Items, int TotalCount);

    private sealed record LogResponse(Guid Id, Guid MemberId, Guid? WorkoutPlanId, int? DurationMinutes);

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

    private static object NewPlanBody(Guid memberId, string name = "Push/Pull/Legs") => new
    {
        memberId,
        trainerId = (Guid?)null,
        name,
        description = "A 3-day split",
        exercises = new[]
        {
            new
            {
                exerciseName = "Bench Press", dayNumber = 1, order = 1, sets = 4, reps = 8,
                weightKg = (decimal?)60, durationSeconds = (int?)null, restSeconds = (int?)90, notes = (string?)null,
            },
        },
    };

    [Fact]
    public async Task CreatePlan_Should_Return_The_Created_Plan_With_Its_Exercises()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Workouts.Manage, Permissions.Workouts.View);
        var memberId = await CreateMemberAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/workout-plans", NewPlanBody(memberId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var plan = await response.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.Equal(memberId, plan!.MemberId);
        Assert.Single(plan.Exercises);
        Assert.Equal("Bench Press", plan.Exercises[0].ExerciseName);
    }

    [Fact]
    public async Task CreatePlan_For_An_Unknown_Member_Should_Return_NotFound()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Workouts.Manage);

        var response = await client.PostAsJsonAsync("/api/v1/workout-plans", NewPlanBody(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddExercise_Should_Append_To_The_Plans_Exercise_List()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Workouts.Manage, Permissions.Workouts.View);
        var memberId = await CreateMemberAsync(client);
        var plan = await (await client.PostAsJsonAsync("/api/v1/workout-plans", NewPlanBody(memberId)))
            .Content.ReadFromJsonAsync<PlanResponse>();

        var addResponse = await client.PostAsJsonAsync($"/api/v1/workout-plans/{plan!.Id}/exercises", new
        {
            exercise = new
            {
                exerciseName = "Overhead Press", dayNumber = 1, order = 2, sets = 3, reps = 10,
                weightKg = (decimal?)30, durationSeconds = (int?)null, restSeconds = (int?)60, notes = (string?)null,
            },
        });
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var reloaded = await (await client.GetAsync($"/api/v1/workout-plans/{plan.Id}")).Content.ReadFromJsonAsync<PlanResponse>();
        Assert.Equal(2, reloaded!.Exercises.Count);
    }

    [Fact]
    public async Task RemoveExercise_With_An_Unknown_Id_Should_Return_NotFound()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Workouts.Manage, Permissions.Workouts.View);
        var memberId = await CreateMemberAsync(client);
        var plan = await (await client.PostAsJsonAsync("/api/v1/workout-plans", NewPlanBody(memberId)))
            .Content.ReadFromJsonAsync<PlanResponse>();

        var response = await client.DeleteAsync($"/api/v1/workout-plans/{plan!.Id}/exercises/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePlan_Should_Change_Name_Description_And_Active_State()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Workouts.Manage, Permissions.Workouts.View);
        var memberId = await CreateMemberAsync(client);
        var plan = await (await client.PostAsJsonAsync("/api/v1/workout-plans", NewPlanBody(memberId)))
            .Content.ReadFromJsonAsync<PlanResponse>();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/workout-plans/{plan!.Id}", new
        {
            name = "Renamed Plan", description = "Updated", isActive = false,
        });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var reloaded = await (await client.GetAsync($"/api/v1/workout-plans/{plan.Id}")).Content.ReadFromJsonAsync<PlanResponse>();
        Assert.Equal("Renamed Plan", reloaded!.Name);
        Assert.False(reloaded.IsActive);
    }

    [Fact]
    public async Task DeletePlan_Should_Remove_It_From_The_Members_List()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Workouts.Manage, Permissions.Workouts.View);
        var memberId = await CreateMemberAsync(client);
        var plan = await (await client.PostAsJsonAsync("/api/v1/workout-plans", NewPlanBody(memberId)))
            .Content.ReadFromJsonAsync<PlanResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/workout-plans/{plan!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var page = await (await client.GetAsync($"/api/v1/workout-plans?memberId={memberId}&pageNumber=1&pageSize=10"))
            .Content.ReadFromJsonAsync<PagedPlans>();
        Assert.Empty(page!.Items);
    }

    [Fact]
    public async Task RecordLog_Should_Be_Retrievable_Via_GetLogs()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Workouts.Manage, Permissions.Workouts.View);
        var memberId = await CreateMemberAsync(client);

        var recordResponse = await client.PostAsJsonAsync("/api/v1/workout-logs", new
        {
            memberId,
            workoutPlanId = (Guid?)null,
            completedOnUtc = DateTimeOffset.UtcNow,
            durationMinutes = 50,
            notes = "Good session",
            exercises = new[]
            {
                new { exerciseName = "Deadlift", setsCompleted = (int?)3, repsCompleted = (int?)5, weightKg = (decimal?)100, notes = (string?)null },
            },
        });
        Assert.Equal(HttpStatusCode.OK, recordResponse.StatusCode);

        var page = await (await client.GetAsync($"/api/v1/workout-logs?memberId={memberId}&pageNumber=1&pageSize=10"))
            .Content.ReadFromJsonAsync<PagedLogs>();
        Assert.Single(page!.Items);
        Assert.Equal(50, page.Items[0].DurationMinutes);
    }

    [Fact]
    public async Task RecordLog_For_An_Unknown_Member_Should_Return_NotFound()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Workouts.Manage);

        var response = await client.PostAsJsonAsync("/api/v1/workout-logs", new
        {
            memberId = Guid.NewGuid(),
            workoutPlanId = (Guid?)null,
            completedOnUtc = DateTimeOffset.UtcNow,
            durationMinutes = (int?)null,
            notes = (string?)null,
            exercises = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPlans_Without_Permission_Should_Return_Forbidden()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Members.View);

        var response = await client.GetAsync($"/api/v1/workout-plans?memberId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
