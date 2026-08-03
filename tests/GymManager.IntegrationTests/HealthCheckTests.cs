using System.Net;
using Xunit;

namespace GymManager.IntegrationTests;

public sealed class HealthCheckTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Liveness_Endpoint_Should_Return_Ok()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
