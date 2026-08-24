using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

/// <summary>
/// Verifies <c>IBranchAccessGuard</c> actually stops a branch-scoped caller (a Manager/Front-Desk/Trainer
/// account created with a specific <c>BranchId</c>) from reading or writing another branch's data — the gap
/// PROJECT_STATUS.md flagged as unenforced multi-branch isolation.
/// </summary>
public sealed class BranchIsolationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record BranchResponse(Guid Id);

    private sealed record MemberResponse(Guid Id, Guid BranchId);

    private sealed record PagedMembers(IReadOnlyList<MemberResponse> Items);

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
        return (await response.Content.ReadFromJsonAsync<BranchResponse>())!.Id;
    }

    private static object MemberRequest(Guid branchId) => new
    {
        branchId,
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
    };

    [Fact]
    public async Task CreateMember_For_Another_Branch_Should_Return_Forbidden()
    {
        var hqClient = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Branches.Manage);
        var otherBranchId = await CreateBranchAsync(hqClient);

        var scopedClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, branchId: Guid.NewGuid(), Permissions.Members.Create);

        var response = await scopedClient.PostAsJsonAsync("/api/v1/members", MemberRequest(otherBranchId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateMember_For_Own_Branch_Should_Succeed()
    {
        var ownBranchId = Guid.NewGuid();
        var scopedClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, branchId: ownBranchId, Permissions.Members.Create);

        var response = await scopedClient.PostAsJsonAsync("/api/v1/members", MemberRequest(ownBranchId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetMembers_Should_Only_Return_Callers_Own_Branch_Even_When_Another_BranchId_Is_Requested()
    {
        var hqClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Members.View);

        var branchAId = await CreateBranchAsync(hqClient);
        var branchBId = await CreateBranchAsync(hqClient);

        var memberAResponse = await hqClient.PostAsJsonAsync("/api/v1/members", MemberRequest(branchAId));
        var memberA = await memberAResponse.Content.ReadFromJsonAsync<MemberResponse>();

        var memberBResponse = await hqClient.PostAsJsonAsync("/api/v1/members", MemberRequest(branchBId));
        var memberB = await memberBResponse.Content.ReadFromJsonAsync<MemberResponse>();

        var branchAScopedClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, branchId: branchAId, Permissions.Members.View);

        // Even though the caller explicitly asks for Branch B's data, the guard must override it with their own branch.
        var response = await branchAScopedClient.GetAsync($"/api/v1/members?branchId={branchBId}");
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedMembers>();
        Assert.Contains(page!.Items, m => m.Id == memberA!.Id);
        Assert.DoesNotContain(page.Items, m => m.Id == memberB!.Id);
    }

    [Fact]
    public async Task FreezeMember_In_Another_Branch_Should_Return_NotFound()
    {
        var hqClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create);

        var otherBranchId = await CreateBranchAsync(hqClient);
        var memberResponse = await hqClient.PostAsJsonAsync("/api/v1/members", MemberRequest(otherBranchId));
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberResponse>();

        var scopedClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, branchId: Guid.NewGuid(), Permissions.Members.Update);

        var response = await scopedClient.PostAsync($"/api/v1/members/{member!.Id}/freeze", content: null);

        // Phase 14: a global EF Core query filter now scopes every Member query to the caller's branch at
        // the DB layer (see BranchIsolationFilterFactory) as a safety net on top of this handler's own
        // IBranchAccessGuard check. Another branch's member is now filtered out of the fetch itself, so the
        // handler never even reaches its explicit guard — the caller sees NotFound rather than Forbidden,
        // which is the more secure behavior anyway (it doesn't confirm the id belongs to *some* member).
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HqCaller_Without_A_Branch_Claim_Can_Still_Access_Every_Branch()
    {
        var hqClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Members.Update);

        var branchId = await CreateBranchAsync(hqClient);
        var memberResponse = await hqClient.PostAsJsonAsync("/api/v1/members", MemberRequest(branchId));
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberResponse>();

        var freezeResponse = await hqClient.PostAsync($"/api/v1/members/{member!.Id}/freeze", content: null);

        Assert.Equal(HttpStatusCode.NoContent, freezeResponse.StatusCode);
    }

    // The four regression tests below cover a gap found during a later frontend-coverage review: several
    // handlers added after the original branch-isolation sweep (member medical info/documents/timeline,
    // body measurements) never called IBranchAccessGuard at all — a branch-scoped caller could read or
    // write another branch's member medical data purely by knowing the member's id. Fixed alongside these
    // tests; see PROJECT_STATUS.md's "Backend gap remediation" section for the full list of affected handlers.

    [Fact]
    public async Task UpdateMedicalInfo_For_Another_Branchs_Member_Should_Return_NotFound()
    {
        var hqClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create);

        var otherBranchId = await CreateBranchAsync(hqClient);
        var memberResponse = await hqClient.PostAsJsonAsync("/api/v1/members", MemberRequest(otherBranchId));
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberResponse>();

        var scopedClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, branchId: Guid.NewGuid(), Permissions.Members.Update);

        var response = await scopedClient.PutAsJsonAsync($"/api/v1/members/{member!.Id}/medical-info", new
        {
            bloodType = "O+",
            conditions = (string?)null,
            allergies = (string?)null,
            medications = (string?)null,
            notes = (string?)null,
        });

        // Phase 14: see the comment on FreezeMember_In_Another_Branch_Should_Return_NotFound above — the
        // global query filter now hides another branch's member from the fetch itself.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMemberTimeline_For_Another_Branchs_Member_Should_Return_NotFound()
    {
        var hqClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create);

        var otherBranchId = await CreateBranchAsync(hqClient);
        var memberResponse = await hqClient.PostAsJsonAsync("/api/v1/members", MemberRequest(otherBranchId));
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberResponse>();

        var scopedClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, branchId: Guid.NewGuid(), Permissions.Members.View);

        var response = await scopedClient.GetAsync($"/api/v1/members/{member!.Id}/timeline");

        // Phase 14: see the comment on FreezeMember_In_Another_Branch_Should_Return_NotFound above.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RecordBodyMeasurement_For_Another_Branchs_Member_Should_Return_NotFound()
    {
        var hqClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create);

        var otherBranchId = await CreateBranchAsync(hqClient);
        var memberResponse = await hqClient.PostAsJsonAsync("/api/v1/members", MemberRequest(otherBranchId));
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberResponse>();

        var scopedClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, branchId: Guid.NewGuid(), Permissions.Members.Update);

        var response = await scopedClient.PostAsJsonAsync("/api/v1/body-measurements", new
        {
            memberId = member!.Id,
            recordedOnUtc = DateTimeOffset.UtcNow,
            heightCm = 180m,
            weightKg = 80m,
            bodyFatPercentage = (decimal?)null,
            chestCm = (decimal?)null,
            waistCm = (decimal?)null,
            hipsCm = (decimal?)null,
            armCm = (decimal?)null,
            thighCm = (decimal?)null,
            notes = (string?)null,
        });

        // Phase 14: see the comment on FreezeMember_In_Another_Branch_Should_Return_NotFound above.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBodyMeasurements_For_Another_Branchs_Member_Should_Return_Empty_Page()
    {
        var hqClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Members.Update);

        var otherBranchId = await CreateBranchAsync(hqClient);
        var memberResponse = await hqClient.PostAsJsonAsync("/api/v1/members", MemberRequest(otherBranchId));
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberResponse>();

        var recordResponse = await hqClient.PostAsJsonAsync("/api/v1/body-measurements", new
        {
            memberId = member!.Id,
            recordedOnUtc = DateTimeOffset.UtcNow,
            heightCm = 180m,
            weightKg = 80m,
            bodyFatPercentage = (decimal?)null,
            chestCm = (decimal?)null,
            waistCm = (decimal?)null,
            hipsCm = (decimal?)null,
            armCm = (decimal?)null,
            thighCm = (decimal?)null,
            notes = (string?)null,
        });
        recordResponse.EnsureSuccessStatusCode();

        var scopedClient = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, branchId: Guid.NewGuid(), Permissions.Members.View);

        var response = await scopedClient.GetAsync($"/api/v1/body-measurements?memberId={member.Id}");
        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedMembers>();
        Assert.Empty(page!.Items);
    }
}
