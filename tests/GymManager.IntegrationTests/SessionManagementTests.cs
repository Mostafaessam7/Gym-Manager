using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymManager.Application.Abstractions;
using GymManager.Application.Identity.Sessions;
using GymManager.Domain.Common;
using GymManager.Domain.Identity;
using GymManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GymManager.IntegrationTests;

/// <summary>Covers the "manage my devices" endpoints backed by <see cref="User.RefreshTokens"/>: listing
/// sessions, revoking one by id, and revoking every active session at once.</summary>
public sealed class SessionManagementTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "Password123";

    private async Task<(HttpClient Client, Guid FirstSessionId, Guid SecondSessionId)> CreateUserWithTwoSessionsAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var email = Email.Create($"test-{Guid.NewGuid():N}@gym.io").Value;
        var user = User.Register(email, passwordHasher.Hash(Password), "Test", "User");

        var firstSession = user.IssueRefreshToken("token-1", DateTimeOffset.UtcNow.AddDays(7), "127.0.0.1", "TestAgent/1.0");
        var secondSession = user.IssueRefreshToken("token-2", DateTimeOffset.UtcNow.AddDays(7), "10.0.0.5", "TestAgent/2.0");

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var accessToken = jwtTokenService.GenerateAccessToken(user, []);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return (client, firstSession.Id, secondSession.Id);
    }

    [Fact]
    public async Task GetSessions_Without_A_Token_Should_Return_Unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/sessions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSessions_Should_List_Every_Session_With_Its_IpAddress_And_UserAgent()
    {
        var (client, firstSessionId, secondSessionId) = await CreateUserWithTwoSessionsAsync();

        var response = await client.GetAsync("/api/v1/auth/sessions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sessions = await response.Content.ReadFromJsonAsync<SessionResponse[]>();
        Assert.NotNull(sessions);
        Assert.Equal(2, sessions!.Length);
        Assert.Contains(sessions, s => s.Id == firstSessionId && s.IpAddress == "127.0.0.1" && s.UserAgent == "TestAgent/1.0" && s.IsActive);
        Assert.Contains(sessions, s => s.Id == secondSessionId && s.IpAddress == "10.0.0.5" && s.UserAgent == "TestAgent/2.0" && s.IsActive);
    }

    [Fact]
    public async Task RevokeSession_Should_Deactivate_Only_The_Targeted_Session()
    {
        var (client, firstSessionId, secondSessionId) = await CreateUserWithTwoSessionsAsync();

        var revokeResponse = await client.DeleteAsync($"/api/v1/auth/sessions/{firstSessionId}");
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        var sessions = await (await client.GetAsync("/api/v1/auth/sessions")).Content.ReadFromJsonAsync<SessionResponse[]>();
        Assert.NotNull(sessions);
        Assert.False(sessions.Single(s => s.Id == firstSessionId).IsActive);
        Assert.True(sessions.Single(s => s.Id == secondSessionId).IsActive);
    }

    [Fact]
    public async Task RevokeSession_With_Unknown_Id_Should_Return_Unauthorized()
    {
        var (client, _, _) = await CreateUserWithTwoSessionsAsync();

        var response = await client.DeleteAsync($"/api/v1/auth/sessions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RevokeAllSessions_Should_Deactivate_Every_Session()
    {
        var (client, firstSessionId, secondSessionId) = await CreateUserWithTwoSessionsAsync();

        var response = await client.PostAsync("/api/v1/auth/sessions/revoke-all", content: null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var sessions = await (await client.GetAsync("/api/v1/auth/sessions")).Content.ReadFromJsonAsync<SessionResponse[]>();
        Assert.NotNull(sessions);
        Assert.All(sessions, s => Assert.False(s.IsActive));
        Assert.Contains(sessions, s => s.Id == firstSessionId);
        Assert.Contains(sessions, s => s.Id == secondSessionId);
    }

    [Fact]
    public async Task Login_Should_Record_The_Callers_UserAgent_On_The_New_Session()
    {
        string email;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var emailValue = Email.Create($"test-{Guid.NewGuid():N}@gym.io").Value;
            var user = User.Register(emailValue, passwordHasher.Hash(Password), "Test", "User");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            email = emailValue.Value;
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("IntegrationTestAgent/1.0");

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
        var reloaded = await verifyDbContext.Users.SingleAsync(u => u.Email.Value == email);
        Assert.Contains(reloaded.RefreshTokens, t => t.UserAgent == "IntegrationTestAgent/1.0");
    }
}
