using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymManager.Application.Abstractions;
using GymManager.Application.Identity;
using GymManager.Domain.Common;
using GymManager.Domain.Identity;
using GymManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GymManager.IntegrationTests;

/// <summary>
/// Covers the email-verification, resend, change-password and password-history flows added on top of the
/// Identity aggregate. Tokens are seeded directly via <see cref="SecureTokenHasher"/> rather than captured
/// from a real email send, mirroring the existing pattern in <see cref="AuthSecurityFlowTests"/>.
/// </summary>
public sealed class EmailVerificationAndPasswordHistoryTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "Password123";

    private async Task<(string Email, User User)> CreateUnverifiedUserAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var email = Email.Create($"test-{Guid.NewGuid():N}@gym.io").Value;
        var user = User.Register(email, passwordHasher.Hash(Password), "Test", "User");

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return (email.Value, user);
    }

    [Fact]
    public async Task VerifyEmail_With_Invalid_Token_Should_Return_Unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = "not-a-real-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_With_Valid_Token_Should_Mark_The_Account_Verified()
    {
        const string verificationToken = "integration-test-verification-token";
        var (email, _) = await CreateUnverifiedUserAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
            var user = await dbContext.Users.SingleAsync(u => u.Email.Value == email);
            user.SetEmailVerificationToken(SecureTokenHasher.Hash(verificationToken), DateTimeOffset.UtcNow.AddHours(24));
            await dbContext.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = verificationToken });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
        var reloaded = await verifyDbContext.Users.AsNoTracking().SingleAsync(u => u.Email.Value == email);
        Assert.True(reloaded.IsEmailVerified);
    }

    [Fact]
    public async Task VerifyEmail_Should_Be_Single_Use()
    {
        const string verificationToken = "integration-test-verification-token-2";
        var (email, _) = await CreateUnverifiedUserAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
            var user = await dbContext.Users.SingleAsync(u => u.Email.Value == email);
            user.SetEmailVerificationToken(SecureTokenHasher.Hash(verificationToken), DateTimeOffset.UtcNow.AddHours(24));
            await dbContext.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        var firstResponse = await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = verificationToken });
        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = verificationToken });
        Assert.Equal(HttpStatusCode.Unauthorized, secondResponse.StatusCode);
    }

    [Fact]
    public async Task ResendVerificationEmail_Should_Return_NoContent_For_Both_Known_And_Unknown_Emails()
    {
        var (email, _) = await CreateUnverifiedUserAsync();
        var client = factory.CreateClient();

        var knownResponse = await client.PostAsJsonAsync("/api/v1/auth/verify-email/resend", new { email });
        var unknownResponse = await client.PostAsJsonAsync("/api/v1/auth/verify-email/resend", new { email = "nobody@gym.io" });

        Assert.Equal(HttpStatusCode.NoContent, knownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, unknownResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Without_A_Token_Should_Return_Unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password", new { currentPassword = Password, newPassword = "NewPassword456" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_With_Wrong_Current_Password_Should_Return_Unauthorized()
    {
        var client = await CreateAuthorizedClientForNewUserAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password", new { currentPassword = "WrongPassword!", newPassword = "NewPassword456" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_With_Correct_Current_Password_Should_Allow_Login_With_The_New_Password()
    {
        var (client, email) = await CreateAuthorizedClientForNewUserWithEmailAsync();

        const string newPassword = "NewPassword456";
        var changeResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password", new { currentPassword = Password, newPassword });
        Assert.Equal(HttpStatusCode.NoContent, changeResponse.StatusCode);

        var anonymousClient = factory.CreateClient();
        var oldPasswordLogin = await anonymousClient.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);

        var newPasswordLogin = await anonymousClient.PostAsJsonAsync("/api/v1/auth/login", new { email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newPasswordLogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Reusing_A_Recent_Password_Should_Be_Rejected()
    {
        var client = await CreateAuthorizedClientForNewUserAsync();

        var firstChange = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password", new { currentPassword = Password, newPassword = "NewPassword456" });
        Assert.Equal(HttpStatusCode.NoContent, firstChange.StatusCode);

        var reuseAttempt = await client.PostAsJsonAsync(
            "/api/v1/auth/change-password", new { currentPassword = "NewPassword456", newPassword = Password });
        Assert.Equal(HttpStatusCode.BadRequest, reuseAttempt.StatusCode);
    }

    private async Task<HttpClient> CreateAuthorizedClientForNewUserAsync()
    {
        var (client, _) = await CreateAuthorizedClientForNewUserWithEmailAsync();
        return client;
    }

    private async Task<(HttpClient Client, string Email)> CreateAuthorizedClientForNewUserWithEmailAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var email = Email.Create($"test-{Guid.NewGuid():N}@gym.io").Value;
        var user = User.Register(email, passwordHasher.Hash(Password), "Test", "User");

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var accessToken = jwtTokenService.GenerateAccessToken(user, []);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return (client, email.Value);
    }
}
