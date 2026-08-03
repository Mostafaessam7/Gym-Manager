using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace GymManager.IntegrationTests;

public sealed class AuthenticationFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private sealed record AuthResponse(Guid UserId, string Email, string AccessToken, string RefreshToken);

    private sealed record LoginResponseDto(bool RequiresTwoFactor, string? TwoFactorChallengeToken, AuthResponse? Authentication);

    // A returning user's Login issuing a second RefreshToken alongside the one issued at Register (i.e. a
    // *second* write adding to an already-persisted owned collection) used to throw a spurious
    // DbUpdateConcurrencyException — verified against real SQL Server, not just this sandbox's InMemory
    // provider. Root cause: RefreshToken/UserRole/ClassBooking/InvoiceLine/MembershipRenewal/SaleLine all
    // use a client-assigned Guid key (Guid.NewGuid() in the domain constructor) that wasn't marked
    // ValueGeneratedNever() in their EF configuration, so EF's default "value already set on a
    // value-generated-on-add key means this must already exist" heuristic misclassified brand-new entries
    // as Modified instead of Added. Fixed in every affected *Configuration.cs by adding
    // `.Property(x => x.Id).ValueGeneratedNever()`, matching the convention already used for every
    // aggregate root's own Id. This test (and the equivalent scenarios in ClassBookingFlowTests,
    // MembershipsControllerTests, and AuthSecurityFlowTests) now passes.
    [Fact]
    public async Task Register_Then_Login_Should_Both_Succeed_And_Each_Issue_A_Refresh_Token()
    {
        var client = factory.CreateClient();
        var email = $"member-{Guid.NewGuid():N}@gym.io";
        var request = new { email, password = "Password123", firstName = "Test", lastName = "User" };

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Password123" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loggedIn = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

        Assert.False(loggedIn!.RequiresTwoFactor);
        Assert.NotEqual(registered!.RefreshToken, loggedIn.Authentication!.RefreshToken);
    }

    [Fact]
    public async Task Register_Should_Return_An_Access_Token()
    {
        var client = factory.CreateClient();
        var email = $"member-{Guid.NewGuid():N}@gym.io";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Password123",
            firstName = "Test",
            lastName = "User",
        });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(registered!.AccessToken));
        Assert.Equal(email, registered.Email);
    }

    [Fact]
    public async Task Register_With_Duplicate_Email_Should_Return_Conflict()
    {
        var client = factory.CreateClient();
        var email = $"member-{Guid.NewGuid():N}@gym.io";

        var request = new { email, password = "Password123", firstName = "Test", lastName = "User" };

        await client.PostAsJsonAsync("/api/v1/auth/register", request);
        var secondResponse = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Should_Return_Unauthorized()
    {
        var client = factory.CreateClient();
        var email = $"member-{Guid.NewGuid():N}@gym.io";

        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Password123",
            firstName = "Test",
            lastName = "User",
        });

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_With_Unknown_Email_Should_Return_Unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "nobody@gym.io", password = "Password123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_Endpoint_Without_Token_Should_Return_Unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_Endpoint_Without_Required_Permission_Should_Return_Forbidden()
    {
        var client = factory.CreateClient();
        var email = $"member-{Guid.NewGuid():N}@gym.io";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Password123",
            firstName = "Test",
            lastName = "User",
        });
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered!.AccessToken);

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
