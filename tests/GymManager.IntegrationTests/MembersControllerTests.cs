using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

public sealed class MembersControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record MemberResponse(Guid Id, string MemberCode, Guid BranchId, string FirstName, string LastName, string Status);

    private sealed record PagedMembers(IReadOnlyList<MemberResponse> Items, int TotalCount);

    private sealed record BranchResponse(Guid Id, string Name);

    private static object BuildMemberRequest(Guid branchId, string firstName = "Jane", string lastName = "Doe") => new
    {
        branchId,
        firstName,
        lastName,
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
    };

    private async Task<Guid> CreateBranchAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/branches", new
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
        response.EnsureSuccessStatusCode();
        var branch = await response.Content.ReadFromJsonAsync<BranchResponse>();
        return branch!.Id;
    }

    [Fact]
    public async Task CreateMember_With_Valid_Data_Should_Return_Created()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create);
        var branchId = await CreateBranchAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/members", BuildMemberRequest(branchId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var member = await response.Content.ReadFromJsonAsync<MemberResponse>();
        Assert.Equal(branchId, member!.BranchId);
        Assert.False(string.IsNullOrWhiteSpace(member.MemberCode));
        Assert.Equal("Active", member.Status);
    }

    [Fact]
    public async Task CreateMember_Without_Permission_Should_Return_Forbidden()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Members.View);

        var response = await client.PostAsJsonAsync("/api/v1/members", BuildMemberRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateMember_With_Missing_Required_Fields_Should_Return_BadRequest()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create);
        var branchId = await CreateBranchAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/members", new
        {
            branchId,
            firstName = "",
            lastName = "Doe",
            phoneNumber = "",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMemberById_For_Unknown_Id_Should_Return_NotFound()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Members.View);

        var response = await client.GetAsync($"/api/v1/members/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMembers_Should_Return_Created_Member()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Members.View);
        var branchId = await CreateBranchAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/v1/members", BuildMemberRequest(branchId, "Alice", "Smith"));
        var created = await createResponse.Content.ReadFromJsonAsync<MemberResponse>();

        var listResponse = await client.GetAsync($"/api/v1/members?branchId={branchId}");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedMembers>();
        Assert.Contains(page!.Items, m => m.Id == created!.Id);
    }

    [Fact]
    public async Task FreezeMember_Then_UnfreezeMember_Should_Toggle_Status()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Members.Update, Permissions.Members.View);
        var branchId = await CreateBranchAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/v1/members", BuildMemberRequest(branchId));
        var created = await createResponse.Content.ReadFromJsonAsync<MemberResponse>();

        var freezeResponse = await client.PostAsync($"/api/v1/members/{created!.Id}/freeze", content: null);
        Assert.Equal(HttpStatusCode.NoContent, freezeResponse.StatusCode);

        var afterFreeze = await (await client.GetAsync($"/api/v1/members/{created.Id}")).Content.ReadFromJsonAsync<MemberResponse>();
        Assert.Equal("Frozen", afterFreeze!.Status);

        var unfreezeResponse = await client.PostAsync($"/api/v1/members/{created.Id}/unfreeze", content: null);
        Assert.Equal(HttpStatusCode.NoContent, unfreezeResponse.StatusCode);

        var afterUnfreeze = await (await client.GetAsync($"/api/v1/members/{created.Id}")).Content.ReadFromJsonAsync<MemberResponse>();
        Assert.Equal("Active", afterUnfreeze!.Status);
    }

    [Fact]
    public async Task DeleteMember_Should_Remove_Member_From_Listing()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Members.Delete, Permissions.Members.View);
        var branchId = await CreateBranchAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/v1/members", BuildMemberRequest(branchId));
        var created = await createResponse.Content.ReadFromJsonAsync<MemberResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/members/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/v1/members/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
