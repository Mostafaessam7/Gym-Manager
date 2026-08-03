using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

public sealed class AttendanceControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record BranchResponse(Guid Id);

    private sealed record MemberResponse(Guid Id, string CheckInCode);

    private sealed record PlanResponse(Guid Id);

    private sealed record AttendanceRecordResponse(Guid Id, Guid MemberId, string Method, DateTimeOffset CheckInUtc, DateTimeOffset? CheckOutUtc);

    private static readonly string[] SetupPermissions =
    [
        Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Memberships.Manage,
    ];

    private async Task<MemberResponse> CreateActiveMemberAsync(HttpClient client)
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
        var branchId = (await branchResponse.Content.ReadFromJsonAsync<BranchResponse>())!.Id;

        var memberResponse = await client.PostAsJsonAsync("/api/v1/members", new
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
        });
        memberResponse.EnsureSuccessStatusCode();
        var member = (await memberResponse.Content.ReadFromJsonAsync<MemberResponse>())!;

        var planResponse = await client.PostAsJsonAsync("/api/v1/membership-plans", new
        {
            name = $"Plan-{Guid.NewGuid():N}",
            description = "Standard monthly plan",
            price = 49.99m,
            currency = "USD",
            durationInDays = 30,
            maxFreezeDays = 7,
            branchId = (Guid?)null,
        });
        planResponse.EnsureSuccessStatusCode();
        var planId = (await planResponse.Content.ReadFromJsonAsync<PlanResponse>())!.Id;

        var purchaseResponse = await client.PostAsJsonAsync("/api/v1/memberships", new
        {
            memberId = member.Id,
            membershipPlanId = planId,
            startDate = DateOnly.FromDateTime(DateTime.UtcNow),
        });
        purchaseResponse.EnsureSuccessStatusCode();

        return member;
    }

    [Fact]
    public async Task CheckIn_With_Valid_Code_Should_Return_Open_AttendanceRecord()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, [.. SetupPermissions, Permissions.Attendance.CheckIn]);
        var member = await CreateActiveMemberAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/attendance/check-in", new { checkInCode = member.CheckInCode, method = 0 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var record = await response.Content.ReadFromJsonAsync<AttendanceRecordResponse>();
        Assert.Equal(member.Id, record!.MemberId);
        Assert.Null(record.CheckOutUtc);
    }

    [Fact]
    public async Task CheckIn_With_Invalid_Code_Should_Return_NotFound()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Attendance.CheckIn);

        var response = await client.PostAsJsonAsync("/api/v1/attendance/check-in", new { checkInCode = "NOPE-CODE", method = 0 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CheckIn_Twice_Without_CheckOut_Should_Fail_Second_Time()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, [.. SetupPermissions, Permissions.Attendance.CheckIn]);
        var member = await CreateActiveMemberAsync(client);

        var first = await client.PostAsJsonAsync("/api/v1/attendance/check-in", new { checkInCode = member.CheckInCode, method = 0 });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/v1/attendance/check-in", new { checkInCode = member.CheckInCode, method = 0 });
        Assert.False(second.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CheckIn_Then_CheckOut_Should_Close_The_Session()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, [.. SetupPermissions, Permissions.Attendance.CheckIn, Permissions.Attendance.View]);
        var member = await CreateActiveMemberAsync(client);

        await client.PostAsJsonAsync("/api/v1/attendance/check-in", new { checkInCode = member.CheckInCode, method = 0 });

        var checkOutResponse = await client.PostAsJsonAsync("/api/v1/attendance/check-out", new { memberId = member.Id });
        Assert.Equal(HttpStatusCode.NoContent, checkOutResponse.StatusCode);

        var listResponse = await client.GetAsync($"/api/v1/attendance?memberId={member.Id}");
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedAttendance>();
        Assert.NotNull(page!.Items.Single(r => r.MemberId == member.Id).CheckOutUtc);
    }

    [Fact]
    public async Task CheckOut_Without_Open_Session_Should_Fail()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Attendance.CheckIn);

        var response = await client.PostAsJsonAsync("/api/v1/attendance/check-out", new { memberId = Guid.NewGuid() });

        Assert.False(response.IsSuccessStatusCode);
    }

    private sealed record PagedAttendance(IReadOnlyList<AttendanceRecordResponse> Items);
}
