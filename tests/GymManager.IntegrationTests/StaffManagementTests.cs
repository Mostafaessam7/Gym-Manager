using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

/// <summary>Covers staff shift scheduling, leave requests, and commission tracking.</summary>
public sealed class StaffManagementTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record ShiftResponse(Guid Id, Guid UserId, string Status);

    private sealed record PagedShifts(IReadOnlyList<ShiftResponse> Items, int TotalCount);

    private sealed record LeaveResponse(Guid Id, Guid UserId, string Status, string? DecisionNotes);

    private sealed record PagedLeaveRequests(IReadOnlyList<LeaveResponse> Items, int TotalCount);

    private sealed record CommissionResponse(Guid Id, Guid UserId, decimal Amount, string Status);

    private sealed record PagedCommissions(IReadOnlyList<CommissionResponse> Items, int TotalCount);

    private async Task<Guid> GetSeededUserIdAsync(HttpClient client)
    {
        // Any authorized caller's own user id works fine as "a User that exists" for these tests.
        var meResponse = await client.GetAsync("/api/v1/users");
        meResponse.EnsureSuccessStatusCode();
        var page = await meResponse.Content.ReadFromJsonAsync<PagedUsers>();
        return page!.Items.First().Id;
    }

    private sealed record UserSummary(Guid Id);

    private sealed record PagedUsers(IReadOnlyList<UserSummary> Items);

    [Fact]
    public async Task ScheduleShift_Should_Return_A_Scheduled_Shift()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Staff.Manage, Permissions.Users.View);
        var userId = await GetSeededUserIdAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/staff-shifts", new
        {
            userId, branchId = Guid.NewGuid(), startUtc = DateTimeOffset.UtcNow.AddDays(1),
            endUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(8), notes = "Opening shift",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var shift = await response.Content.ReadFromJsonAsync<ShiftResponse>();
        Assert.Equal("Scheduled", shift!.Status);
    }

    [Fact]
    public async Task ScheduleShift_With_End_Before_Start_Should_Return_BadRequest()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Staff.Manage, Permissions.Users.View);
        var userId = await GetSeededUserIdAsync(client);
        var start = DateTimeOffset.UtcNow.AddDays(1);

        var response = await client.PostAsJsonAsync("/api/v1/staff-shifts", new
        {
            userId, branchId = Guid.NewGuid(), startUtc = start, endUtc = start.AddHours(-1), notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteShift_Then_CancelShift_Should_Fail_Since_Already_Finalized()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Staff.Manage, Permissions.Users.View);
        var userId = await GetSeededUserIdAsync(client);

        var shift = await (await client.PostAsJsonAsync("/api/v1/staff-shifts", new
        {
            userId, branchId = Guid.NewGuid(), startUtc = DateTimeOffset.UtcNow.AddDays(1),
            endUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(8), notes = (string?)null,
        })).Content.ReadFromJsonAsync<ShiftResponse>();

        var completeResponse = await client.PostAsync($"/api/v1/staff-shifts/{shift!.Id}/complete", content: null);
        Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);

        var cancelResponse = await client.PostAsync($"/api/v1/staff-shifts/{shift.Id}/cancel", content: null);
        Assert.Equal(HttpStatusCode.Conflict, cancelResponse.StatusCode);
    }

    [Fact]
    public async Task GetShifts_Filtered_By_User_Should_Only_Return_That_Users_Shifts()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Staff.Manage, Permissions.Staff.View, Permissions.Users.View);
        var userId = await GetSeededUserIdAsync(client);

        await client.PostAsJsonAsync("/api/v1/staff-shifts", new
        {
            userId, branchId = Guid.NewGuid(), startUtc = DateTimeOffset.UtcNow.AddDays(1),
            endUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(8), notes = (string?)null,
        });

        var page = await (await client.GetAsync($"/api/v1/staff-shifts?userId={userId}&pageNumber=1&pageSize=10"))
            .Content.ReadFromJsonAsync<PagedShifts>();

        Assert.All(page!.Items, s => Assert.Equal(userId, s.UserId));
        Assert.NotEmpty(page.Items);
    }

    [Fact]
    public async Task RequestLeave_Then_Approve_Should_Set_Status_Approved()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Staff.View, Permissions.Staff.Manage, Permissions.Users.View);
        var userId = await GetSeededUserIdAsync(client);

        var leaveResponse = await client.PostAsJsonAsync("/api/v1/leave-requests", new
        {
            userId, type = 0, startDate = new DateOnly(2026, 9, 1), endDate = new DateOnly(2026, 9, 5), reason = "Vacation",
        });
        Assert.Equal(HttpStatusCode.OK, leaveResponse.StatusCode);
        var leave = await leaveResponse.Content.ReadFromJsonAsync<LeaveResponse>();
        Assert.Equal("Pending", leave!.Status);

        var approveResponse = await client.PostAsJsonAsync($"/api/v1/leave-requests/{leave.Id}/approve", new { notes = "Approved, enjoy!" });
        Assert.Equal(HttpStatusCode.NoContent, approveResponse.StatusCode);

        var page = await (await client.GetAsync($"/api/v1/leave-requests?userId={userId}&pageNumber=1&pageSize=10"))
            .Content.ReadFromJsonAsync<PagedLeaveRequests>();
        var reloaded = page!.Items.Single(l => l.Id == leave.Id);
        Assert.Equal("Approved", reloaded.Status);
        Assert.Equal("Approved, enjoy!", reloaded.DecisionNotes);
    }

    [Fact]
    public async Task RequestLeave_With_EndDate_Before_StartDate_Should_Return_BadRequest()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Staff.View, Permissions.Users.View);
        var userId = await GetSeededUserIdAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/leave-requests", new
        {
            userId, type = 1, startDate = new DateOnly(2026, 9, 5), endDate = new DateOnly(2026, 9, 1), reason = (string?)null,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApproveLeave_Twice_Should_Return_Conflict_The_Second_Time()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Staff.View, Permissions.Staff.Manage, Permissions.Users.View);
        var userId = await GetSeededUserIdAsync(client);

        var leave = await (await client.PostAsJsonAsync("/api/v1/leave-requests", new
        {
            userId, type = 0, startDate = new DateOnly(2026, 9, 1), endDate = new DateOnly(2026, 9, 5), reason = (string?)null,
        })).Content.ReadFromJsonAsync<LeaveResponse>();

        await client.PostAsJsonAsync($"/api/v1/leave-requests/{leave!.Id}/approve", new { notes = (string?)null });
        var secondApprove = await client.PostAsJsonAsync($"/api/v1/leave-requests/{leave.Id}/reject", new { notes = (string?)null });

        Assert.Equal(HttpStatusCode.Conflict, secondApprove.StatusCode);
    }

    [Fact]
    public async Task RecordCommission_Then_MarkPaid_Should_Update_Status()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Staff.Manage, Permissions.Staff.View, Permissions.Users.View);
        var userId = await GetSeededUserIdAsync(client);

        var commissionResponse = await client.PostAsJsonAsync("/api/v1/commissions", new
        {
            userId, amount = 25.50m, sourceType = 1, sourceReferenceId = (Guid?)null, earnedOnUtc = DateTimeOffset.UtcNow, notes = (string?)null,
        });
        Assert.Equal(HttpStatusCode.OK, commissionResponse.StatusCode);
        var commission = await commissionResponse.Content.ReadFromJsonAsync<CommissionResponse>();
        Assert.Equal("Pending", commission!.Status);

        var markPaidResponse = await client.PostAsync($"/api/v1/commissions/{commission.Id}/mark-paid", content: null);
        Assert.Equal(HttpStatusCode.NoContent, markPaidResponse.StatusCode);

        var page = await (await client.GetAsync($"/api/v1/commissions?userId={userId}&pageNumber=1&pageSize=10"))
            .Content.ReadFromJsonAsync<PagedCommissions>();
        Assert.Equal("Paid", page!.Items.Single(c => c.Id == commission.Id).Status);
    }

    [Fact]
    public async Task RecordCommission_With_A_Negative_Amount_Should_Return_BadRequest()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Staff.Manage, Permissions.Users.View);
        var userId = await GetSeededUserIdAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/commissions", new
        {
            userId, amount = -10m, sourceType = 2, sourceReferenceId = (Guid?)null, earnedOnUtc = DateTimeOffset.UtcNow, notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetShifts_Without_Permission_Should_Return_Forbidden()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Members.View);

        var response = await client.GetAsync("/api/v1/staff-shifts");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
