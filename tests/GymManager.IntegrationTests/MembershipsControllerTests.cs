using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

public sealed class MembershipsControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record BranchResponse(Guid Id);

    private sealed record MemberResponse(Guid Id);

    private sealed record PlanResponse(Guid Id, string Name, decimal Price, int DurationInDays, bool IsActive);

    private sealed record MembershipResponse(Guid Id, Guid MemberId, Guid MembershipPlanId, DateOnly StartDate, DateOnly EndDate, string Status);

    private async Task<(Guid BranchId, Guid MemberId)> CreateBranchAndMemberAsync(HttpClient client)
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
        branchResponse.EnsureSuccessStatusCode();
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
        memberResponse.EnsureSuccessStatusCode();
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberResponse>();

        return (branch.Id, member!.Id);
    }

    private static async Task<Guid> CreatePlanAsync(HttpClient client, Guid? branchId = null, int durationInDays = 30)
    {
        var response = await client.PostAsJsonAsync("/api/v1/membership-plans", new
        {
            name = $"Plan-{Guid.NewGuid():N}",
            description = "Standard monthly plan",
            price = 49.99m,
            currency = "USD",
            durationInDays,
            maxFreezeDays = 7,
            branchId,
        });
        response.EnsureSuccessStatusCode();
        var plan = await response.Content.ReadFromJsonAsync<PlanResponse>();
        return plan!.Id;
    }

    [Fact]
    public async Task CreatePlan_Then_GetPlans_Should_Include_Created_Plan()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Memberships.Manage, Permissions.Memberships.View);

        var planId = await CreatePlanAsync(client);

        var response = await client.GetAsync("/api/v1/membership-plans");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedPlans>();
        Assert.Contains(page!.Items, p => p.Id == planId);
    }

    private sealed record PagedPlans(IReadOnlyList<PlanResponse> Items);

    [Fact]
    public async Task PurchaseMembership_With_Valid_Plan_Should_Return_Active_Membership()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Memberships.Manage);
        var (_, memberId) = await CreateBranchAndMemberAsync(client);
        var planId = await CreatePlanAsync(client, durationInDays: 30);

        var response = await client.PostAsJsonAsync("/api/v1/memberships", new
        {
            memberId,
            membershipPlanId = planId,
            startDate = DateOnly.FromDateTime(DateTime.UtcNow),
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var membership = await response.Content.ReadFromJsonAsync<MembershipResponse>();
        Assert.Equal(memberId, membership!.MemberId);
        Assert.Equal("Active", membership.Status);
        Assert.Equal(30, (membership.EndDate.ToDateTime(TimeOnly.MinValue) - membership.StartDate.ToDateTime(TimeOnly.MinValue)).Days);
    }

    [Fact]
    public async Task PurchaseMembership_With_Unknown_Member_Should_Return_NotFound()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Memberships.Manage, Permissions.Memberships.View);
        var planId = await CreatePlanAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/memberships", new
        {
            memberId = Guid.NewGuid(),
            membershipPlanId = planId,
            startDate = DateOnly.FromDateTime(DateTime.UtcNow),
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PurchaseMembership_With_Inactive_Plan_Should_Return_Conflict_Or_BadRequest()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Memberships.Manage);
        var (_, memberId) = await CreateBranchAndMemberAsync(client);
        var planId = await CreatePlanAsync(client);

        var deactivateResponse = await client.PostAsync($"/api/v1/membership-plans/{planId}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        var response = await client.PostAsJsonAsync("/api/v1/memberships", new
        {
            memberId,
            membershipPlanId = planId,
            startDate = DateOnly.FromDateTime(DateTime.UtcNow),
        });

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Purchase_Then_Freeze_Then_Unfreeze_Then_Cancel_Should_Transition_Status()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Memberships.Manage, Permissions.Memberships.View);
        var (_, memberId) = await CreateBranchAndMemberAsync(client);
        var planId = await CreatePlanAsync(client);

        var purchaseResponse = await client.PostAsJsonAsync("/api/v1/memberships", new
        {
            memberId,
            membershipPlanId = planId,
            startDate = DateOnly.FromDateTime(DateTime.UtcNow),
        });
        var membership = await purchaseResponse.Content.ReadFromJsonAsync<MembershipResponse>();

        var freezeResponse = await client.PostAsync($"/api/v1/memberships/{membership!.Id}/freeze", content: null);
        Assert.Equal(HttpStatusCode.NoContent, freezeResponse.StatusCode);

        var afterFreeze = await GetByMemberAsync(client, memberId);
        Assert.Equal("Frozen", afterFreeze.Single(m => m.Id == membership.Id).Status);

        var unfreezeResponse = await client.PostAsync($"/api/v1/memberships/{membership.Id}/unfreeze", content: null);
        Assert.Equal(HttpStatusCode.NoContent, unfreezeResponse.StatusCode);

        var afterUnfreeze = await GetByMemberAsync(client, memberId);
        Assert.Equal("Active", afterUnfreeze.Single(m => m.Id == membership.Id).Status);

        var cancelResponse = await client.PostAsync($"/api/v1/memberships/{membership.Id}/cancel", content: null);
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        var afterCancel = await GetByMemberAsync(client, memberId);
        Assert.Equal("Cancelled", afterCancel.Single(m => m.Id == membership.Id).Status);
    }

    [Fact]
    public async Task RenewMembership_Should_Extend_EndDate()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Memberships.Manage, Permissions.Memberships.Renew);
        var (_, memberId) = await CreateBranchAndMemberAsync(client);
        var planId = await CreatePlanAsync(client);

        var purchaseResponse = await client.PostAsJsonAsync("/api/v1/memberships", new
        {
            memberId,
            membershipPlanId = planId,
            startDate = DateOnly.FromDateTime(DateTime.UtcNow),
        });
        var membership = await purchaseResponse.Content.ReadFromJsonAsync<MembershipResponse>();

        var renewResponse = await client.PostAsJsonAsync($"/api/v1/memberships/{membership!.Id}/renew", new
        {
            additionalDays = 30,
            amountPaid = 49.99m,
            currency = "USD",
        });

        Assert.Equal(HttpStatusCode.OK, renewResponse.StatusCode);
        var renewed = await renewResponse.Content.ReadFromJsonAsync<MembershipResponse>();
        Assert.Equal(membership.EndDate.AddDays(30), renewed!.EndDate);
    }

    private static async Task<List<MembershipResponse>> GetByMemberAsync(HttpClient client, Guid memberId)
    {
        var response = await client.GetAsync($"/api/v1/memberships/by-member/{memberId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<MembershipResponse>>())!;
    }
}
