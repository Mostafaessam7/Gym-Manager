using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

public sealed class LeadsControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record BranchResponse(Guid Id);

    private sealed record FollowUpResponse(Guid Id, string Type, bool IsCompleted);

    private sealed record LeadResponse(Guid Id, string Name, string Stage, Guid? ConvertedMemberId, IReadOnlyList<FollowUpResponse> FollowUps);

    private sealed record PagedLeads(IReadOnlyList<LeadResponse> Items, int TotalCount);

    private sealed record MemberResponse(Guid Id);

    private static object NewLeadBody(string name = "Jane Prospect") => new
    {
        name,
        email = "jane@example.com",
        phone = "555-0100",
        source = 0, // Website
        branchId = (Guid?)null,
        assignedToUserId = (Guid?)null,
        notes = (string?)null,
    };

    [Fact]
    public async Task CreateLead_Should_Default_To_New_Stage()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Crm.Manage, Permissions.Crm.View);

        var response = await client.PostAsJsonAsync("/api/v1/leads", NewLeadBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var lead = await response.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.Equal("New", lead!.Stage);
    }

    [Fact]
    public async Task MoveStage_Should_Update_The_Leads_Stage()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Crm.Manage, Permissions.Crm.View);
        var lead = await (await client.PostAsJsonAsync("/api/v1/leads", NewLeadBody())).Content.ReadFromJsonAsync<LeadResponse>();

        var moveResponse = await client.PostAsJsonAsync($"/api/v1/leads/{lead!.Id}/stage", new { stage = 1 }); // Contacted
        Assert.Equal(HttpStatusCode.NoContent, moveResponse.StatusCode);

        var reloaded = await (await client.GetAsync($"/api/v1/leads/{lead.Id}")).Content.ReadFromJsonAsync<LeadResponse>();
        Assert.Equal("Contacted", reloaded!.Stage);
    }

    [Fact]
    public async Task MoveStage_To_Won_Should_Return_BadRequest()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Crm.Manage, Permissions.Crm.View);
        var lead = await (await client.PostAsJsonAsync("/api/v1/leads", NewLeadBody())).Content.ReadFromJsonAsync<LeadResponse>();

        var response = await client.PostAsJsonAsync($"/api/v1/leads/{lead!.Id}/stage", new { stage = 4 }); // Won

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MarkLost_Then_Reopen_Should_Move_Back_To_Contacted()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Crm.Manage, Permissions.Crm.View);
        var lead = await (await client.PostAsJsonAsync("/api/v1/leads", NewLeadBody())).Content.ReadFromJsonAsync<LeadResponse>();

        var lostResponse = await client.PostAsJsonAsync($"/api/v1/leads/{lead!.Id}/mark-lost", new { reason = "No budget" });
        Assert.Equal(HttpStatusCode.NoContent, lostResponse.StatusCode);

        var afterLost = await (await client.GetAsync($"/api/v1/leads/{lead.Id}")).Content.ReadFromJsonAsync<LeadResponse>();
        Assert.Equal("Lost", afterLost!.Stage);

        var reopenResponse = await client.PostAsync($"/api/v1/leads/{lead.Id}/reopen", content: null);
        Assert.Equal(HttpStatusCode.NoContent, reopenResponse.StatusCode);

        var afterReopen = await (await client.GetAsync($"/api/v1/leads/{lead.Id}")).Content.ReadFromJsonAsync<LeadResponse>();
        Assert.Equal("Contacted", afterReopen!.Stage);
    }

    [Fact]
    public async Task AddFollowUp_Then_Complete_Should_Mark_It_Completed()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Crm.Manage, Permissions.Crm.View);
        var lead = await (await client.PostAsJsonAsync("/api/v1/leads", NewLeadBody())).Content.ReadFromJsonAsync<LeadResponse>();

        var addResponse = await client.PostAsJsonAsync($"/api/v1/leads/{lead!.Id}/follow-ups", new
        {
            type = 0, // Call
            scheduledOnUtc = DateTimeOffset.UtcNow.AddDays(1),
            notes = "Discuss pricing",
        });
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        var followUp = await addResponse.Content.ReadFromJsonAsync<FollowUpResponse>();

        var completeResponse = await client.PostAsJsonAsync(
            $"/api/v1/leads/{lead.Id}/follow-ups/{followUp!.Id}/complete", new { completedOnUtc = DateTimeOffset.UtcNow, notes = "Went well" });
        Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);

        var reloaded = await (await client.GetAsync($"/api/v1/leads/{lead.Id}")).Content.ReadFromJsonAsync<LeadResponse>();
        Assert.True(reloaded!.FollowUps.Single().IsCompleted);
    }

    [Fact]
    public async Task ConvertToMember_Should_Create_A_Member_And_Mark_The_Lead_Won()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Crm.Manage, Permissions.Crm.View, Permissions.Branches.Manage, Permissions.Members.Create);
        var lead = await (await client.PostAsJsonAsync("/api/v1/leads", NewLeadBody())).Content.ReadFromJsonAsync<LeadResponse>();

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

        var convertResponse = await client.PostAsJsonAsync($"/api/v1/leads/{lead!.Id}/convert", new
        {
            branchId = branch!.Id,
            firstName = "Jane",
            lastName = "Prospect",
            phoneNumber = "555-0100",
            email = (string?)null,
            dateOfBirth = (DateOnly?)null,
            gender = 2,
        });
        Assert.Equal(HttpStatusCode.OK, convertResponse.StatusCode);
        var member = await convertResponse.Content.ReadFromJsonAsync<MemberResponse>();

        var reloaded = await (await client.GetAsync($"/api/v1/leads/{lead.Id}")).Content.ReadFromJsonAsync<LeadResponse>();
        Assert.Equal("Won", reloaded!.Stage);
        Assert.Equal(member!.Id, reloaded.ConvertedMemberId);
    }

    [Fact]
    public async Task ConvertToMember_Twice_Should_Return_Conflict_The_Second_Time()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Crm.Manage, Permissions.Crm.View, Permissions.Branches.Manage, Permissions.Members.Create);
        var lead = await (await client.PostAsJsonAsync("/api/v1/leads", NewLeadBody())).Content.ReadFromJsonAsync<LeadResponse>();

        var branch = await (await client.PostAsJsonAsync("/api/v1/branches", new
        {
            name = $"Branch-{Guid.NewGuid():N}",
            country = "USA",
            street = (string?)null,
            city = (string?)null,
            state = (string?)null,
            postalCode = (string?)null,
            phoneNumber = (string?)null,
            email = (string?)null,
        })).Content.ReadFromJsonAsync<BranchResponse>();

        var convertBody = new
        {
            branchId = branch!.Id, firstName = "Jane", lastName = "Prospect", phoneNumber = "555-0100",
            email = (string?)null, dateOfBirth = (DateOnly?)null, gender = 2,
        };

        await client.PostAsJsonAsync($"/api/v1/leads/{lead!.Id}/convert", convertBody);
        var secondAttempt = await client.PostAsJsonAsync($"/api/v1/leads/{lead.Id}/convert", convertBody);

        Assert.Equal(HttpStatusCode.Conflict, secondAttempt.StatusCode);
    }

    [Fact]
    public async Task GetLeads_Filtered_By_Stage_Should_Only_Return_Matching_Leads()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Crm.Manage, Permissions.Crm.View);
        var lead1 = await (await client.PostAsJsonAsync("/api/v1/leads", NewLeadBody("Lead One"))).Content.ReadFromJsonAsync<LeadResponse>();
        var lead2 = await (await client.PostAsJsonAsync("/api/v1/leads", NewLeadBody("Lead Two"))).Content.ReadFromJsonAsync<LeadResponse>();

        await client.PostAsJsonAsync($"/api/v1/leads/{lead1!.Id}/stage", new { stage = 1 }); // Contacted

        var page = await (await client.GetAsync("/api/v1/leads?stage=0&pageNumber=1&pageSize=50")) // New
            .Content.ReadFromJsonAsync<PagedLeads>();

        Assert.Contains(page!.Items, l => l.Id == lead2!.Id);
        Assert.DoesNotContain(page.Items, l => l.Id == lead1.Id);
    }

    [Fact]
    public async Task GetLeads_Without_Permission_Should_Return_Forbidden()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Members.View);

        var response = await client.GetAsync("/api/v1/leads");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
