using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

public sealed class LockersControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record LockerResponse(Guid Id, string Number, string Status, Guid? AssignedMemberId);

    private async Task<Guid> CreateLockerAsync(HttpClient client, Guid branchId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/lockers", new { branchId, number = $"L-{Guid.NewGuid():N}"[..8] });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LockerResponse>())!.Id;
    }

    [Fact]
    public async Task CreateLocker_Should_Return_Available_Locker()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Lockers.Manage);

        var response = await client.PostAsJsonAsync("/api/v1/lockers", new { branchId = Guid.NewGuid(), number = "A-101" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var locker = await response.Content.ReadFromJsonAsync<LockerResponse>();
        Assert.Equal("Available", locker!.Status);
        Assert.Null(locker.AssignedMemberId);
    }

    [Fact]
    public async Task AssignLocker_Then_Release_Should_Toggle_Status()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Lockers.Manage, Permissions.Lockers.View);
        var branchId = Guid.NewGuid();
        var lockerId = await CreateLockerAsync(client, branchId);
        var memberId = Guid.NewGuid();

        var assignResponse = await client.PostAsJsonAsync($"/api/v1/lockers/{lockerId}/assign", new { memberId });
        Assert.Equal(HttpStatusCode.NoContent, assignResponse.StatusCode);

        var afterAssign = await GetLockerAsync(client, branchId, lockerId);
        Assert.Equal("Assigned", afterAssign.Status);
        Assert.Equal(memberId, afterAssign.AssignedMemberId);

        var releaseResponse = await client.PostAsync($"/api/v1/lockers/{lockerId}/release", content: null);
        Assert.Equal(HttpStatusCode.NoContent, releaseResponse.StatusCode);

        var afterRelease = await GetLockerAsync(client, branchId, lockerId);
        Assert.Equal("Available", afterRelease.Status);
        Assert.Null(afterRelease.AssignedMemberId);
    }

    [Fact]
    public async Task AssignLocker_Already_Assigned_Should_Fail()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Lockers.Manage);
        var branchId = Guid.NewGuid();
        var lockerId = await CreateLockerAsync(client, branchId);

        await client.PostAsJsonAsync($"/api/v1/lockers/{lockerId}/assign", new { memberId = Guid.NewGuid() });
        var secondAssign = await client.PostAsJsonAsync($"/api/v1/lockers/{lockerId}/assign", new { memberId = Guid.NewGuid() });

        Assert.False(secondAssign.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SetMaintenance_On_Available_Locker_Should_Succeed()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Lockers.Manage, Permissions.Lockers.View);
        var branchId = Guid.NewGuid();
        var lockerId = await CreateLockerAsync(client, branchId);

        var response = await client.PostAsync($"/api/v1/lockers/{lockerId}/maintenance", content: null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var locker = await GetLockerAsync(client, branchId, lockerId);
        Assert.Equal("Maintenance", locker.Status);
    }

    private sealed record PagedLockers(IReadOnlyList<LockerResponse> Items);

    private static async Task<LockerResponse> GetLockerAsync(HttpClient client, Guid branchId, Guid lockerId)
    {
        var response = await client.GetAsync($"/api/v1/lockers?branchId={branchId}");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedLockers>();
        return page!.Items.Single(l => l.Id == lockerId);
    }
}
