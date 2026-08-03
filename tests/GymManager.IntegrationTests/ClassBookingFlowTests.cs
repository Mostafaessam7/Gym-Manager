using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

public sealed class ClassBookingFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record BranchResponse(Guid Id);

    private sealed record MemberResponse(Guid Id);

    private sealed record TrainerResponse(Guid Id);

    private sealed record GymClassResponse(Guid Id, int Capacity, bool IsActive);

    private sealed record PlanResponse(Guid Id);

    private sealed record ClassBookingResponse(Guid MemberId, string Status, DateTimeOffset BookedOnUtc);

    private sealed record ClassSessionResponse(
        Guid Id, Guid GymClassId, int Capacity, int ActiveBookingsCount, string Status, IReadOnlyCollection<ClassBookingResponse> Bookings);

    // Booking requires the member to have a currently-active membership (see BookSessionCommandHandler),
    // so every test member is given one via this fixed permission set on the setup client.
    private static readonly string[] SetupPermissions =
    [
        Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Trainers.Manage,
        Permissions.Classes.Manage, Permissions.Memberships.Manage,
    ];

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

    private async Task<Guid> CreateActiveMemberAsync(HttpClient client, Guid branchId)
    {
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
        var memberId = (await memberResponse.Content.ReadFromJsonAsync<MemberResponse>())!.Id;

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
            memberId,
            membershipPlanId = planId,
            startDate = DateOnly.FromDateTime(DateTime.UtcNow),
        });
        purchaseResponse.EnsureSuccessStatusCode();

        return memberId;
    }

    private async Task<Guid> CreateTrainerAsync(HttpClient client, Guid branchId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/trainers", new
        {
            branchId,
            firstName = "Sam",
            lastName = "Trainer",
            specialization = "Strength",
            bio = (string?)null,
            phoneNumber = (string?)null,
            email = (string?)null,
            userId = (Guid?)null,
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TrainerResponse>())!.Id;
    }

    private async Task<Guid> CreateClassAsync(HttpClient client, Guid branchId, Guid trainerId, int capacity = 1)
    {
        var response = await client.PostAsJsonAsync("/api/v1/classes", new
        {
            name = $"Class-{Guid.NewGuid():N}",
            description = "A test class",
            branchId,
            trainerId,
            capacity,
            durationMinutes = 60,
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GymClassResponse>())!.Id;
    }

    private async Task<Guid> ScheduleSessionAsync(HttpClient client, Guid gymClassId)
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var response = await client.PostAsJsonAsync("/api/v1/class-sessions", new
        {
            gymClassId,
            startUtc = start,
            endUtc = start.AddHours(1),
            capacityOverride = (int?)null,
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClassSessionResponse>())!.Id;
    }

    [Fact]
    public async Task BookSession_With_Available_Capacity_Should_Add_Confirmed_Booking()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, [.. SetupPermissions, Permissions.Classes.Book, Permissions.Classes.View]);

        var branchId = await CreateBranchAsync(client);
        var memberId = await CreateActiveMemberAsync(client, branchId);
        var trainerId = await CreateTrainerAsync(client, branchId);
        var classId = await CreateClassAsync(client, branchId, trainerId, capacity: 2);
        var sessionId = await ScheduleSessionAsync(client, classId);

        var bookResponse = await client.PostAsJsonAsync($"/api/v1/class-sessions/{sessionId}/book", new { memberId });

        Assert.Equal(HttpStatusCode.OK, bookResponse.StatusCode);
        var session = await bookResponse.Content.ReadFromJsonAsync<ClassSessionResponse>();
        Assert.Equal(1, session!.ActiveBookingsCount);
        Assert.Contains(session.Bookings, b => b.MemberId == memberId && b.Status == "Booked");
    }

    [Fact]
    public async Task BookSession_Beyond_Capacity_Should_Fail_For_Second_Member()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, [.. SetupPermissions, Permissions.Classes.Book, Permissions.Classes.View]);

        var branchId = await CreateBranchAsync(client);
        var firstMemberId = await CreateActiveMemberAsync(client, branchId);
        var secondMemberId = await CreateActiveMemberAsync(client, branchId);
        var trainerId = await CreateTrainerAsync(client, branchId);
        var classId = await CreateClassAsync(client, branchId, trainerId, capacity: 1);
        var sessionId = await ScheduleSessionAsync(client, classId);

        var firstBooking = await client.PostAsJsonAsync($"/api/v1/class-sessions/{sessionId}/book", new { memberId = firstMemberId });
        Assert.Equal(HttpStatusCode.OK, firstBooking.StatusCode);

        var secondBooking = await client.PostAsJsonAsync($"/api/v1/class-sessions/{sessionId}/book", new { memberId = secondMemberId });
        Assert.False(secondBooking.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CancelBooking_Should_Free_Up_Capacity_For_Another_Member()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, [.. SetupPermissions, Permissions.Classes.Book, Permissions.Classes.View]);

        var branchId = await CreateBranchAsync(client);
        var firstMemberId = await CreateActiveMemberAsync(client, branchId);
        var secondMemberId = await CreateActiveMemberAsync(client, branchId);
        var trainerId = await CreateTrainerAsync(client, branchId);
        var classId = await CreateClassAsync(client, branchId, trainerId, capacity: 1);
        var sessionId = await ScheduleSessionAsync(client, classId);

        await client.PostAsJsonAsync($"/api/v1/class-sessions/{sessionId}/book", new { memberId = firstMemberId });

        var cancelResponse = await client.PostAsJsonAsync($"/api/v1/class-sessions/{sessionId}/cancel-booking", new { memberId = firstMemberId });
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        var secondBooking = await client.PostAsJsonAsync($"/api/v1/class-sessions/{sessionId}/book", new { memberId = secondMemberId });
        Assert.Equal(HttpStatusCode.OK, secondBooking.StatusCode);
    }

    [Fact]
    public async Task BookSession_Without_Book_Permission_Should_Return_Forbidden()
    {
        var setupClient = await TestAuthHelper.CreateAuthorizedClientAsync(factory, SetupPermissions);

        var branchId = await CreateBranchAsync(setupClient);
        var memberId = await CreateActiveMemberAsync(setupClient, branchId);
        var trainerId = await CreateTrainerAsync(setupClient, branchId);
        var classId = await CreateClassAsync(setupClient, branchId, trainerId);
        var sessionId = await ScheduleSessionAsync(setupClient, classId);

        var unauthorizedClient = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Classes.View);

        var bookResponse = await unauthorizedClient.PostAsJsonAsync($"/api/v1/class-sessions/{sessionId}/book", new { memberId });

        Assert.Equal(HttpStatusCode.Forbidden, bookResponse.StatusCode);
    }
}
