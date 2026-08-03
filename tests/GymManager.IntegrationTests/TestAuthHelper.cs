using System.Net.Http.Headers;
using GymManager.Application.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Identity;
using GymManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GymManager.IntegrationTests;

/// <summary>
/// Builds an <see cref="HttpClient"/> authenticated as a user with a specific set of permissions, for tests
/// that need an authorized caller (registration alone never grants permissions — see
/// <see cref="AuthenticationFlowTests"/> — and the seeded Owner account is skipped in the "Testing"
/// environment). The user, its role and its permissions are all inserted in a single <c>SaveChangesAsync</c>
/// and the access token is minted directly via <see cref="IJwtTokenService"/> rather than a real login —
/// purely for test-setup speed (skips a real HTTP round trip), not to work around any provider limitation;
/// see the comment on <see cref="AuthenticationFlowTests.Register_Then_Login_Should_Both_Succeed_And_Each_Issue_A_Refresh_Token"/>
/// for the real bug this used to be conflated with.
/// </summary>
internal static class TestAuthHelper
{
    public static Task<HttpClient> CreateAuthorizedClientAsync(
        WebApplicationFactory<Program> factory, params string[] permissions) =>
        CreateAuthorizedClientAsync(factory, branchId: null, permissions);

    /// <summary>
    /// Same as the branch-agnostic overload, but pins the caller to a specific branch (mirroring a
    /// Manager/Front-Desk/Trainer account created with a <c>BranchId</c>) so tests can verify
    /// <see cref="IBranchAccessGuard"/> enforcement. Passing <c>null</c> mints an HQ-level caller who is not
    /// branch-restricted, matching how <see cref="ICurrentUserService.BranchId"/> works today.
    /// </summary>
    public static async Task<HttpClient> CreateAuthorizedClientAsync(
        WebApplicationFactory<Program> factory, Guid? branchId, params string[] permissions)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GymManagerDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var role = Role.Create($"TestRole-{Guid.NewGuid():N}", "Role granted for integration test purposes.");
        foreach (var permission in permissions)
            role.GrantPermission(permission);

        var email = Email.Create($"test-{Guid.NewGuid():N}@gym.io").Value;
        var user = User.Register(email, passwordHasher.Hash("Password123"), "Test", "User", branchId);
        user.AssignRole(role.Id);

        dbContext.Roles.Add(role);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var accessToken = jwtTokenService.GenerateAccessToken(user, permissions);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
