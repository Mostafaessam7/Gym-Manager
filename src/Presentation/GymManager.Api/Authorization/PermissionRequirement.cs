using Microsoft.AspNetCore.Authorization;

namespace GymManager.Api.Authorization;

/// <summary>Authorization requirement satisfied when the caller's JWT carries the given <c>permission</c> claim.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
