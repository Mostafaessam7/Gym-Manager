using System.Net;
using System.Net.Http.Json;
using GymManager.Application.Abstractions;
using GymManager.Application.Identity;
using GymManager.Domain.Common;
using GymManager.Domain.Identity;
using GymManager.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GymManager.IntegrationTests;

/// <summary>
/// Covers the three auth security gaps PROJECT_STATUS.md flagged as missing: logout/refresh-token
/// revocation, the password-reset flow, and account lockout after repeated failed logins.
/// </summary>
public sealed class AuthSecurityFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "Password123";

    private sealed record AuthResponse(Guid UserId, string Email, string AccessToken, string RefreshToken);

    private async Task<(string Email, string RefreshToken)> CreateUserWithRefreshTokenAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var email = Email.Create($"test-{Guid.NewGuid():N}@gym.io").Value;
        var user = User.Register(email, passwordHasher.Hash(Password), "Test", "User");
        var refreshToken = jwtTokenService.GenerateRefreshToken();
        user.IssueRefreshToken(refreshToken, DateTimeOffset.UtcNow.AddDays(7));

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return (email.Value, refreshToken);
    }

    [Fact]
    public async Task Logout_Should_Revoke_The_Refresh_Token_So_A_Later_Refresh_Fails()
    {
        var (_, refreshToken) = await CreateUserWithRefreshTokenAsync();
        var client = factory.CreateClient();

        var logoutResponse = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_With_Unknown_Token_Should_Still_Return_NoContent()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = "not-a-real-token" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RequestPasswordReset_Should_Return_NoContent_For_Both_Known_And_Unknown_Emails()
    {
        var (email, _) = await CreateUserWithRefreshTokenAsync();
        var client = factory.CreateClient();

        var knownResponse = await client.PostAsJsonAsync("/api/v1/auth/password-reset/request", new { email });
        var unknownResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/request", new { email = "nobody@gym.io" });

        Assert.Equal(HttpStatusCode.NoContent, knownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, unknownResponse.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_With_Invalid_Token_Should_Return_Unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/confirm", new { token = "not-a-real-token", newPassword = "NewPassword123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_With_Valid_Token_Should_Invalidate_The_Old_Password()
    {
        const string resetToken = "integration-test-reset-token";
        string email;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var emailValue = Email.Create($"test-{Guid.NewGuid():N}@gym.io").Value;
            var user = User.Register(emailValue, passwordHasher.Hash(Password), "Test", "User");
            user.SetPasswordResetToken(SecureTokenHasher.Hash(resetToken), DateTimeOffset.UtcNow.AddHours(1));

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            email = emailValue.Value;
        }

        var client = factory.CreateClient();

        var resetResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/confirm", new { token = resetToken, newPassword = "BrandNewPassword456" });
        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        var loginWithOldPassword = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, loginWithOldPassword.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_With_Valid_Token_Should_Allow_Login_With_The_New_Password()
    {
        const string resetToken = "integration-test-reset-token-2";
        string email;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var emailValue = Email.Create($"test-{Guid.NewGuid():N}@gym.io").Value;
            var user = User.Register(emailValue, passwordHasher.Hash(Password), "Test", "User");
            user.SetPasswordResetToken(SecureTokenHasher.Hash(resetToken), DateTimeOffset.UtcNow.AddHours(1));

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            email = emailValue.Value;
        }

        var client = factory.CreateClient();

        const string newPassword = "BrandNewPassword456";
        await client.PostAsJsonAsync("/api/v1/auth/password-reset/confirm", new { token = resetToken, newPassword });

        var loginWithNewPassword = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, loginWithNewPassword.StatusCode);
    }

    [Fact]
    public async Task Login_Should_Lock_The_Account_Out_After_Five_Failed_Attempts()
    {
        var (email, _) = await CreateUserWithRefreshTokenAsync();
        var client = factory.CreateClient();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "WrongPassword!" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // Even the correct password should now be rejected — the account is locked out, not the credential.
        var lockedOutResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.Forbidden, lockedOutResponse.StatusCode);
    }

    [Fact]
    public async Task Login_With_Fewer_Than_Five_Failed_Attempts_Should_Still_Allow_The_Correct_Password()
    {
        var (email, _) = await CreateUserWithRefreshTokenAsync();
        var client = factory.CreateClient();

        for (var attempt = 0; attempt < 3; attempt++)
            await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "WrongPassword!" });

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
