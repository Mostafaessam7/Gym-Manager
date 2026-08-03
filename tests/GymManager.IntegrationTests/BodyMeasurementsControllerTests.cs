using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.IntegrationTests;

public sealed class BodyMeasurementsControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record BranchResponse(Guid Id);

    private sealed record MemberResponse(Guid Id);

    private sealed record MeasurementResponse(Guid Id, Guid MemberId, decimal? WeightKg, decimal? HeightCm, decimal? Bmi);

    private sealed record PagedMeasurements(IReadOnlyList<MeasurementResponse> Items, int TotalCount);

    private async Task<(HttpClient Client, Guid MemberId)> CreateMemberAsync()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Branches.Manage, Permissions.Members.Create, Permissions.Members.Update,
            Permissions.Members.Delete, Permissions.Members.View);

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
    public async Task RecordMeasurement_Should_Return_The_Created_Measurement_With_A_Computed_Bmi()
    {
        var (client, memberId) = await CreateMemberAsync();

        var response = await client.PostAsJsonAsync("/api/v1/body-measurements", new
        {
            memberId,
            recordedOnUtc = DateTimeOffset.UtcNow,
            heightCm = 180,
            weightKg = 81,
            bodyFatPercentage = (decimal?)null,
            chestCm = (decimal?)null,
            waistCm = (decimal?)null,
            hipsCm = (decimal?)null,
            armCm = (decimal?)null,
            thighCm = (decimal?)null,
            notes = "First check-in",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var measurement = await response.Content.ReadFromJsonAsync<MeasurementResponse>();
        Assert.Equal(memberId, measurement!.MemberId);
        Assert.Equal(25.0m, measurement.Bmi);
    }

    [Fact]
    public async Task RecordMeasurement_For_An_Unknown_Member_Should_Return_NotFound()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Members.Update);

        var response = await client.PostAsJsonAsync("/api/v1/body-measurements", new
        {
            memberId = Guid.NewGuid(),
            recordedOnUtc = DateTimeOffset.UtcNow,
            heightCm = (decimal?)null,
            weightKg = (decimal?)null,
            bodyFatPercentage = (decimal?)null,
            chestCm = (decimal?)null,
            waistCm = (decimal?)null,
            hipsCm = (decimal?)null,
            armCm = (decimal?)null,
            thighCm = (decimal?)null,
            notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RecordMeasurement_With_A_Negative_Weight_Should_Return_BadRequest()
    {
        var (client, memberId) = await CreateMemberAsync();

        var response = await client.PostAsJsonAsync("/api/v1/body-measurements", new
        {
            memberId,
            recordedOnUtc = DateTimeOffset.UtcNow,
            heightCm = (decimal?)null,
            weightKg = -5,
            bodyFatPercentage = (decimal?)null,
            chestCm = (decimal?)null,
            waistCm = (decimal?)null,
            hipsCm = (decimal?)null,
            armCm = (decimal?)null,
            thighCm = (decimal?)null,
            notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMeasurements_Should_Return_Only_The_Requested_Members_Records_Newest_First()
    {
        var (client, memberId) = await CreateMemberAsync();
        var (_, otherMemberId) = await CreateMemberAsync();

        await client.PostAsJsonAsync("/api/v1/body-measurements", NewMeasurementBody(memberId, weightKg: 82));
        await Task.Delay(10);
        await client.PostAsJsonAsync("/api/v1/body-measurements", NewMeasurementBody(memberId, weightKg: 80));
        await client.PostAsJsonAsync("/api/v1/body-measurements", NewMeasurementBody(otherMemberId, weightKg: 70));

        var response = await client.GetAsync($"/api/v1/body-measurements?memberId={memberId}&pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedMeasurements>();
        Assert.Equal(2, page!.TotalCount);
        Assert.All(page.Items, m => Assert.Equal(memberId, m.MemberId));
        Assert.Equal(80, page.Items[0].WeightKg);
        Assert.Equal(82, page.Items[1].WeightKg);
    }

    [Fact]
    public async Task UpdateMeasurement_Should_Change_The_Recorded_Values()
    {
        var (client, memberId) = await CreateMemberAsync();
        var created = await (await client.PostAsJsonAsync("/api/v1/body-measurements", NewMeasurementBody(memberId, weightKg: 82)))
            .Content.ReadFromJsonAsync<MeasurementResponse>();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/body-measurements/{created!.Id}", new
        {
            recordedOnUtc = DateTimeOffset.UtcNow,
            heightCm = 180,
            weightKg = 79,
            bodyFatPercentage = (decimal?)null,
            chestCm = (decimal?)null,
            waistCm = (decimal?)null,
            hipsCm = (decimal?)null,
            armCm = (decimal?)null,
            thighCm = (decimal?)null,
            notes = "Updated",
        });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var page = await (await client.GetAsync($"/api/v1/body-measurements?memberId={memberId}&pageNumber=1&pageSize=10"))
            .Content.ReadFromJsonAsync<PagedMeasurements>();
        Assert.Equal(79, page!.Items.Single().WeightKg);
    }

    [Fact]
    public async Task DeleteMeasurement_Should_Remove_It()
    {
        var (client, memberId) = await CreateMemberAsync();
        var created = await (await client.PostAsJsonAsync("/api/v1/body-measurements", NewMeasurementBody(memberId, weightKg: 82)))
            .Content.ReadFromJsonAsync<MeasurementResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/body-measurements/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var page = await (await client.GetAsync($"/api/v1/body-measurements?memberId={memberId}&pageNumber=1&pageSize=10"))
            .Content.ReadFromJsonAsync<PagedMeasurements>();
        Assert.Empty(page!.Items);
    }

    [Fact]
    public async Task DeleteMeasurement_With_An_Unknown_Id_Should_Return_NotFound()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Members.Delete);

        var response = await client.DeleteAsync($"/api/v1/body-measurements/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static object NewMeasurementBody(Guid memberId, decimal weightKg) => new
    {
        memberId,
        recordedOnUtc = DateTimeOffset.UtcNow,
        heightCm = (decimal?)null,
        weightKg,
        bodyFatPercentage = (decimal?)null,
        chestCm = (decimal?)null,
        waistCm = (decimal?)null,
        hipsCm = (decimal?)null,
        armCm = (decimal?)null,
        thighCm = (decimal?)null,
        notes = (string?)null,
    };
}
