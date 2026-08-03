using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

/// <summary>Covers the medical-info, document-attachment, and activity-timeline additions to the member
/// profile.</summary>
public sealed class MemberProfileDepthTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record MemberResponse(Guid Id, Guid BranchId);

    private sealed record BranchResponse(Guid Id);

    private sealed record MedicalInfoResponse(string? BloodType, string? Conditions, string? Allergies, string? Medications, string? Notes);

    private sealed record MemberDocumentResponse(Guid Id, string FileName, string FileUrl, string DocumentType, DateTimeOffset UploadedOnUtc);

    private sealed record MemberDetailResponse(Guid Id, MedicalInfoResponse? MedicalInfo, IReadOnlyList<MemberDocumentResponse> Documents);

    private sealed record TimelineEntry(DateTimeOffset OccurredOnUtc, string EventType, string Description);

    private async Task<(HttpClient Client, Guid MemberId)> CreateMemberAsync()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Members.Update, Permissions.Members.View);

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
        var member = await memberResponse.Content.ReadFromJsonAsync<MemberResponse>();

        return (client, member!.Id);
    }

    [Fact]
    public async Task UpdateMedicalInfo_Should_Be_Reflected_On_The_Member()
    {
        var (client, memberId) = await CreateMemberAsync();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/members/{memberId}/medical-info", new
        {
            bloodType = "O+",
            conditions = "Asthma",
            allergies = "Peanuts",
            medications = "Inhaler",
            notes = "Carries an EpiPen",
        });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var member = await (await client.GetAsync($"/api/v1/members/{memberId}")).Content.ReadFromJsonAsync<MemberDetailResponse>();
        Assert.NotNull(member!.MedicalInfo);
        Assert.Equal("O+", member.MedicalInfo!.BloodType);
        Assert.Equal("Peanuts", member.MedicalInfo.Allergies);
    }

    [Fact]
    public async Task UpdateMedicalInfo_With_All_Fields_Blank_Should_Clear_It()
    {
        var (client, memberId) = await CreateMemberAsync();
        await client.PutAsJsonAsync($"/api/v1/members/{memberId}/medical-info", new
        {
            bloodType = "O+", conditions = (string?)null, allergies = (string?)null, medications = (string?)null, notes = (string?)null,
        });

        await client.PutAsJsonAsync($"/api/v1/members/{memberId}/medical-info", new
        {
            bloodType = (string?)null, conditions = (string?)null, allergies = (string?)null, medications = (string?)null, notes = (string?)null,
        });

        var member = await (await client.GetAsync($"/api/v1/members/{memberId}")).Content.ReadFromJsonAsync<MemberDetailResponse>();
        Assert.Null(member!.MedicalInfo);
    }

    [Fact]
    public async Task UploadDocument_Should_Add_It_To_The_Members_Document_List()
    {
        var (client, memberId) = await CreateMemberAsync();

        var uploadResponse = await client.PostAsJsonAsync($"/api/v1/members/{memberId}/documents", new
        {
            fileName = "waiver.pdf",
            fileUrl = "/files/waiver-abc123.pdf",
            documentType = 1, // Waiver
        });
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<MemberDocumentResponse>();
        Assert.Equal("waiver.pdf", uploaded!.FileName);
        Assert.Equal("Waiver", uploaded.DocumentType);

        var member = await (await client.GetAsync($"/api/v1/members/{memberId}")).Content.ReadFromJsonAsync<MemberDetailResponse>();
        Assert.Single(member!.Documents);
        Assert.Equal(uploaded.Id, member.Documents[0].Id);
    }

    [Fact]
    public async Task DeleteDocument_Should_Remove_It_From_The_Members_Document_List()
    {
        var (client, memberId) = await CreateMemberAsync();
        var uploaded = await (await client.PostAsJsonAsync($"/api/v1/members/{memberId}/documents", new
        {
            fileName = "id-card.jpg",
            fileUrl = "/files/id-card-xyz.jpg",
            documentType = 0, // IdCard
        })).Content.ReadFromJsonAsync<MemberDocumentResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/members/{memberId}/documents/{uploaded!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var member = await (await client.GetAsync($"/api/v1/members/{memberId}")).Content.ReadFromJsonAsync<MemberDetailResponse>();
        Assert.Empty(member!.Documents);
    }

    [Fact]
    public async Task DeleteDocument_With_An_Unknown_Id_Should_Return_NotFound()
    {
        var (client, memberId) = await CreateMemberAsync();

        var response = await client.DeleteAsync($"/api/v1/members/{memberId}/documents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTimeline_For_An_Unknown_Member_Should_Return_NotFound()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Members.View);

        var response = await client.GetAsync($"/api/v1/members/{Guid.NewGuid()}/timeline");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTimeline_For_A_New_Member_Should_Return_An_Empty_List()
    {
        var (client, memberId) = await CreateMemberAsync();

        var response = await client.GetAsync($"/api/v1/members/{memberId}/timeline");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var entries = await response.Content.ReadFromJsonAsync<TimelineEntry[]>();
        Assert.NotNull(entries);
        Assert.Empty(entries);
    }
}
