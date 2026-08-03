using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

/// <summary>
/// Verifies domain events raised by aggregates actually reach a handler that does something (queues a
/// notification) — the gap PROJECT_STATUS.md flagged as "dispatched but nothing subscribes to any of them".
/// Notification delivery itself (real SMTP/SMS) is not asserted, since there's no mail server in this sandbox
/// and the notification is recorded either way (Sent or Failed) — what's under test is that the handler ran
/// and left a record behind, proving the wiring from aggregate → domain event → dispatcher → handler works.
/// </summary>
public sealed class DomainEventConsumerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record BranchResponse(Guid Id);

    private sealed record MemberResponse(Guid Id);

    private sealed record NotificationResponse(Guid Id, string Channel, string RecipientAddress, string Subject, string Status);

    private sealed record PagedNotifications(IReadOnlyList<NotificationResponse> Items);

    [Fact]
    public async Task RegisteringMemberWithEmail_Should_Queue_A_Welcome_Notification()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Notifications.Manage);

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
        var branchId = (await branchResponse.Content.ReadFromJsonAsync<BranchResponse>())!.Id;

        var email = $"member-{Guid.NewGuid():N}@example.com";
        var memberResponse = await client.PostAsJsonAsync("/api/v1/members", new
        {
            branchId,
            firstName = "Jane",
            lastName = "Doe",
            phoneNumber = $"+1555{Random.Shared.Next(1000000, 9999999)}",
            email,
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
        Assert.Equal(HttpStatusCode.Created, memberResponse.StatusCode);
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberResponse>();

        var notificationsResponse = await client.GetAsync($"/api/v1/notifications?recipientMemberId={member!.Id}");
        notificationsResponse.EnsureSuccessStatusCode();
        var page = await notificationsResponse.Content.ReadFromJsonAsync<PagedNotifications>();

        var welcomeNotification = Assert.Single(page!.Items);
        Assert.Equal("Email", welcomeNotification.Channel);
        Assert.Equal(email, welcomeNotification.RecipientAddress);
        Assert.Equal("Welcome to Gym Manager!", welcomeNotification.Subject);
        Assert.True(welcomeNotification.Status is "Sent" or "Failed");
    }

    [Fact]
    public async Task RecordingPayment_Should_Queue_A_Receipt_Notification()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Payments.Process, Permissions.Notifications.Manage);

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
        var branchId = (await branchResponse.Content.ReadFromJsonAsync<BranchResponse>())!.Id;

        var email = $"member-{Guid.NewGuid():N}@example.com";
        var memberResponse = await client.PostAsJsonAsync("/api/v1/members", new
        {
            branchId,
            firstName = "Jane",
            lastName = "Doe",
            phoneNumber = $"+1555{Random.Shared.Next(1000000, 9999999)}",
            email,
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

        await client.PostAsJsonAsync("/api/v1/payments", new
        {
            memberId = member!.Id,
            branchId,
            amount = 49.99m,
            currency = "USD",
            method = 0,
            referenceType = 4,
            referenceId = (Guid?)null,
        });

        var notificationsResponse = await client.GetAsync($"/api/v1/notifications?recipientMemberId={member.Id}");
        notificationsResponse.EnsureSuccessStatusCode();
        var page = await notificationsResponse.Content.ReadFromJsonAsync<PagedNotifications>();

        Assert.Contains(page!.Items, n => n.Subject == "Payment receipt");
    }
}
