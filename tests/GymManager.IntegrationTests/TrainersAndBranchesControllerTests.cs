using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

public sealed class TrainersAndBranchesControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record BranchResponse(Guid Id, string Name, bool IsActive);

    private sealed record TrainerResponse(Guid Id, string FirstName, string LastName, bool IsActive);

    private static object BranchRequest(string? name = null) => new
    {
        name = name ?? $"Branch-{Guid.NewGuid():N}",
        country = "USA",
        street = (string?)null,
        city = (string?)null,
        state = (string?)null,
        postalCode = (string?)null,
        phoneNumber = (string?)null,
        email = (string?)null,
    };

    [Fact]
    public async Task CreateBranch_Then_GetById_Should_Return_The_Branch()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Branches.Manage, Permissions.Branches.View);

        var createResponse = await client.PostAsJsonAsync("/api/v1/branches", BranchRequest("Downtown Branch"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var branch = await createResponse.Content.ReadFromJsonAsync<BranchResponse>();

        var getResponse = await client.GetAsync($"/api/v1/branches/{branch!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<BranchResponse>();
        Assert.Equal("Downtown Branch", fetched!.Name);
    }

    [Fact]
    public async Task DeactivateBranch_Should_Exclude_It_From_Default_Listing()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Branches.Manage, Permissions.Branches.View);

        var createResponse = await client.PostAsJsonAsync("/api/v1/branches", BranchRequest());
        var branch = await createResponse.Content.ReadFromJsonAsync<BranchResponse>();

        var deactivateResponse = await client.PostAsync($"/api/v1/branches/{branch!.Id}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/v1/branches");
        var branches = await listResponse.Content.ReadFromJsonAsync<List<BranchResponse>>();
        Assert.DoesNotContain(branches!, b => b.Id == branch.Id);

        var includeInactiveResponse = await client.GetAsync("/api/v1/branches?includeInactive=true");
        var allBranches = await includeInactiveResponse.Content.ReadFromJsonAsync<List<BranchResponse>>();
        Assert.Contains(allBranches!, b => b.Id == branch.Id && !b.IsActive);
    }

    [Fact]
    public async Task CreateBranch_Without_Permission_Should_Return_Forbidden()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Branches.View);

        var response = await client.PostAsJsonAsync("/api/v1/branches", BranchRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTrainer_Then_UpdateTrainer_Should_Persist_Changes()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Trainers.Manage, Permissions.Trainers.View);

        var createResponse = await client.PostAsJsonAsync("/api/v1/trainers", new
        {
            branchId = Guid.NewGuid(),
            firstName = "Sam",
            lastName = "Trainer",
            specialization = "Strength",
            bio = (string?)null,
            phoneNumber = (string?)null,
            email = (string?)null,
            userId = (Guid?)null,
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var trainer = await createResponse.Content.ReadFromJsonAsync<TrainerResponse>();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/trainers/{trainer!.Id}", new
        {
            firstName = "Samantha",
            lastName = "Trainer",
            specialization = "Cardio",
            bio = (string?)null,
            phoneNumber = (string?)null,
            email = (string?)null,
        });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/v1/trainers/{trainer.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<TrainerResponse>();
        Assert.Equal("Samantha", updated!.FirstName);
    }

    [Fact]
    public async Task DeactivateTrainer_Should_Set_IsActive_False()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Trainers.Manage, Permissions.Trainers.View);

        var createResponse = await client.PostAsJsonAsync("/api/v1/trainers", new
        {
            branchId = Guid.NewGuid(),
            firstName = "Sam",
            lastName = "Trainer",
            specialization = "Strength",
            bio = (string?)null,
            phoneNumber = (string?)null,
            email = (string?)null,
            userId = (Guid?)null,
        });
        var trainer = await createResponse.Content.ReadFromJsonAsync<TrainerResponse>();

        var deactivateResponse = await client.PostAsync($"/api/v1/trainers/{trainer!.Id}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/v1/trainers/{trainer.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<TrainerResponse>();
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task GetTrainerById_For_Unknown_Id_Should_Return_NotFound()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Trainers.View);

        var response = await client.GetAsync($"/api/v1/trainers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
