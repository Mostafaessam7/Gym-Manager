using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace GymManager.Api.Authorization;

/// <summary>
/// Dynamically builds an authorization policy for any policy name of the form <c>permission:&lt;code&gt;</c>,
/// so a new <see cref="Domain.Identity.Permissions"/> code never requires a corresponding policy registration.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private const string PolicyPrefix = "permission:";
    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
            return _fallbackProvider.GetPolicyAsync(policyName);

        var permission = policyName[PolicyPrefix.Length..];

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
