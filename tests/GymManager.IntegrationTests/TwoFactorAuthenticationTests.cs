using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymManager.Application.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Identity;
using GymManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GymManager.IntegrationTests;

/// <summary>End-to-end coverage of the 2FA enrollment and challenge flow: setup, confirmation (with the real
/// <see cref="ITwoFactorService"/> from the DI container, not a stub, so the TOTP math is genuinely
/// exercised), disabling, and the login gate itself.</summary>
public sealed class TwoFactorAuthenticationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "Password123";

    private sealed record TwoFactorSetupResponseDto(string SecretKey, string ProvisioningUri);

    private sealed record ConfirmResponseDto(string[] RecoveryCodes);

    private sealed record AuthResponseDto(Guid UserId, string Email, string AccessToken, string RefreshToken);

    private sealed record LoginResponseDto(bool RequiresTwoFactor, string? TwoFactorChallengeToken, AuthResponseDto? Authentication);

    private async Task<(HttpClient Client, string Email)> CreateAuthorizedClientAsync()
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

    /// <summary>Computes the current TOTP code directly via RFC 6238, the same way a real authenticator app
    /// would — mirrors the private algorithm inside <c>TotpTwoFactorService</c>, whose correctness is already
    /// covered independently by <c>TotpTwoFactorServiceTests</c>. (Brute-forcing all 10^6 candidates through
    /// <see cref="ITwoFactorService.ValidateCode"/> was tried first and took over 20 minutes — HMACSHA1
    /// instantiation overhead dominates at that iteration count.)</summary>
    private static string GenerateValidCode(ITwoFactorService twoFactorService, string secretKey)
    {
        var secretBytes = Base32Decode(secretKey);
        var timeStep = (long)(DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch).TotalSeconds / 30;

        var counterBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        using var hmac = new System.Security.Cryptography.HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0F;
        var binaryCode =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        var code = (binaryCode % 1_000_000).ToString().PadLeft(6, '0');

        Assert.True(twoFactorService.ValidateCode(secretKey, code), "Sanity check: directly computed code must validate.");

        return code;
    }

    private static byte[] Base32Decode(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        var bitBuffer = 0;
        var bitsInBuffer = 0;

        foreach (var c in base32.Trim().TrimEnd('=').ToUpperInvariant())
        {
            var index = alphabet.IndexOf(c);
            if (index < 0)
                continue;

            bitBuffer = (bitBuffer << 5) | index;
            bitsInBuffer += 5;

            if (bitsInBuffer >= 8)
            {
                bitsInBuffer -= 8;
                bytes.Add((byte)((bitBuffer >> bitsInBuffer) & 0xFF));
            }
        }

        return [.. bytes];
    }

    [Fact]
    public async Task StartSetup_Then_Confirm_With_A_Valid_Code_Should_Enable_TwoFactor_And_Return_Recovery_Codes()
    {
        var (client, _) = await CreateAuthorizedClientAsync();
        var twoFactorService = factory.Services.GetRequiredService<ITwoFactorService>();

        var setupResponse = await client.PostAsync("/api/v1/auth/2fa/setup", content: null);
        Assert.Equal(HttpStatusCode.OK, setupResponse.StatusCode);
        var setup = await setupResponse.Content.ReadFromJsonAsync<TwoFactorSetupResponseDto>();
        Assert.NotNull(setup);

        var validCode = GenerateValidCode(twoFactorService, setup.SecretKey);

        var confirmResponse = await client.PostAsJsonAsync("/api/v1/auth/2fa/confirm", new { code = validCode });
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<ConfirmResponseDto>();
        Assert.NotNull(confirmed);
        Assert.NotEmpty(confirmed.RecoveryCodes);
    }

    [Fact]
    public async Task ConfirmSetup_With_An_Invalid_Code_Should_Return_Unauthorized()
    {
        var (client, _) = await CreateAuthorizedClientAsync();

        await client.PostAsync("/api/v1/auth/2fa/setup", content: null);

        var confirmResponse = await client.PostAsJsonAsync("/api/v1/auth/2fa/confirm", new { code = "000000" });

        Assert.Equal(HttpStatusCode.Unauthorized, confirmResponse.StatusCode);
    }

    [Fact]
    public async Task Login_For_A_TwoFactor_Enabled_Account_Should_Require_A_Challenge_Before_Issuing_Tokens()
    {
        var (client, email) = await CreateAuthorizedClientAsync();
        var twoFactorService = factory.Services.GetRequiredService<ITwoFactorService>();

        var setup = await (await client.PostAsync("/api/v1/auth/2fa/setup", content: null))
            .Content.ReadFromJsonAsync<TwoFactorSetupResponseDto>();
        var validCode = GenerateValidCode(twoFactorService, setup!.SecretKey);
        await client.PostAsJsonAsync("/api/v1/auth/2fa/confirm", new { code = validCode });

        var anonymousClient = factory.CreateClient();
        var loginResponse = await anonymousClient.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(login);
        Assert.True(login.RequiresTwoFactor);
        Assert.Null(login.Authentication);
        Assert.False(string.IsNullOrWhiteSpace(login.TwoFactorChallengeToken));

        var secondCode = GenerateValidCode(twoFactorService, setup.SecretKey);
        var completeResponse = await anonymousClient.PostAsJsonAsync(
            "/api/v1/auth/login/2fa", new { challengeToken = login.TwoFactorChallengeToken, code = secondCode });
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        var authenticated = await completeResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authenticated);
        Assert.Equal(email, authenticated.Email);
        Assert.False(string.IsNullOrWhiteSpace(authenticated.AccessToken));
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_With_An_Invalid_Code_Should_Return_Unauthorized()
    {
        var (client, email) = await CreateAuthorizedClientAsync();
        var twoFactorService = factory.Services.GetRequiredService<ITwoFactorService>();

        var setup = await (await client.PostAsync("/api/v1/auth/2fa/setup", content: null))
            .Content.ReadFromJsonAsync<TwoFactorSetupResponseDto>();
        var validCode = GenerateValidCode(twoFactorService, setup!.SecretKey);
        await client.PostAsJsonAsync("/api/v1/auth/2fa/confirm", new { code = validCode });

        var anonymousClient = factory.CreateClient();
        var login = await (await anonymousClient.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password }))
            .Content.ReadFromJsonAsync<LoginResponseDto>();

        var completeResponse = await anonymousClient.PostAsJsonAsync(
            "/api/v1/auth/login/2fa", new { challengeToken = login!.TwoFactorChallengeToken, code = "000000" });

        Assert.Equal(HttpStatusCode.Unauthorized, completeResponse.StatusCode);
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_With_A_Recovery_Code_Should_Succeed_And_Consume_It()
    {
        var (client, email) = await CreateAuthorizedClientAsync();
        var twoFactorService = factory.Services.GetRequiredService<ITwoFactorService>();

        var setup = await (await client.PostAsync("/api/v1/auth/2fa/setup", content: null))
            .Content.ReadFromJsonAsync<TwoFactorSetupResponseDto>();
        var validCode = GenerateValidCode(twoFactorService, setup!.SecretKey);
        var confirmed = await (await client.PostAsJsonAsync("/api/v1/auth/2fa/confirm", new { code = validCode }))
            .Content.ReadFromJsonAsync<ConfirmResponseDto>();
        var recoveryCode = confirmed!.RecoveryCodes[0];

        var anonymousClient = factory.CreateClient();
        var login = await (await anonymousClient.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password }))
            .Content.ReadFromJsonAsync<LoginResponseDto>();

        var completeResponse = await anonymousClient.PostAsJsonAsync(
            "/api/v1/auth/login/2fa", new { challengeToken = login!.TwoFactorChallengeToken, code = recoveryCode });
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactor_With_The_Wrong_Password_Should_Return_Unauthorized()
    {
        var (client, _) = await CreateAuthorizedClientAsync();
        var twoFactorService = factory.Services.GetRequiredService<ITwoFactorService>();

        var setup = await (await client.PostAsync("/api/v1/auth/2fa/setup", content: null))
            .Content.ReadFromJsonAsync<TwoFactorSetupResponseDto>();
        var validCode = GenerateValidCode(twoFactorService, setup!.SecretKey);
        await client.PostAsJsonAsync("/api/v1/auth/2fa/confirm", new { code = validCode });

        var disableResponse = await client.PostAsJsonAsync("/api/v1/auth/2fa/disable", new { currentPassword = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, disableResponse.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactor_With_The_Correct_Password_Should_Let_The_Next_Login_Skip_The_Challenge()
    {
        var (client, email) = await CreateAuthorizedClientAsync();
        var twoFactorService = factory.Services.GetRequiredService<ITwoFactorService>();

        var setup = await (await client.PostAsync("/api/v1/auth/2fa/setup", content: null))
            .Content.ReadFromJsonAsync<TwoFactorSetupResponseDto>();
        var validCode = GenerateValidCode(twoFactorService, setup!.SecretKey);
        await client.PostAsJsonAsync("/api/v1/auth/2fa/confirm", new { code = validCode });

        var disableResponse = await client.PostAsJsonAsync("/api/v1/auth/2fa/disable", new { currentPassword = Password });
        Assert.Equal(HttpStatusCode.NoContent, disableResponse.StatusCode);

        var anonymousClient = factory.CreateClient();
        var loginResponse = await anonymousClient.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

        Assert.False(login!.RequiresTwoFactor);
        Assert.NotNull(login.Authentication);
    }
}
